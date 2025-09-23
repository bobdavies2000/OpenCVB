Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedPrep_Basics : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Public options As New Options_RedCloud
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Reduction transform for the point cloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim split() = {New cv.Mat, New cv.Mat, New cv.Mat}
        Dim reduceAmt = options.reductionTarget
        task.pcSplit(0).ConvertTo(split(0), cv.MatType.CV_32S, 1000 / reduceAmt)
        task.pcSplit(1).ConvertTo(split(1), cv.MatType.CV_32S, 1000 / reduceAmt)
        task.pcSplit(2).ConvertTo(split(2), cv.MatType.CV_32S, 1000 / reduceAmt)

        Select Case task.reductionName
            Case "X Reduction"
                dst0 = (split(0) * reduceAmt).ToMat
            Case "Y Reduction"
                dst0 = (split(1) * reduceAmt).ToMat
            Case "Z Reduction"
                dst0 = (split(2) * reduceAmt).ToMat
            Case "XY Reduction"
                dst0 = (split(0) * reduceAmt + split(1) * reduceAmt).ToMat
            Case "XZ Reduction"
                dst0 = (split(0) * reduceAmt + split(2) * reduceAmt).ToMat
            Case "YZ Reduction"
                dst0 = (split(1) * reduceAmt + split(2) * reduceAmt).ToMat
            Case "XYZ Reduction"
                dst0 = (split(0) * reduceAmt + split(1) * reduceAmt + split(2) * reduceAmt).ToMat
        End Select

        Dim mm As mmData = GetMinMax(dst0)
        Dim dst32f As New cv.Mat
        If Math.Abs(mm.minVal) > mm.maxVal Then
            mm.minVal = -mm.maxVal
            dst0.ConvertTo(dst32f, cv.MatType.CV_32F)
            Dim mask = dst32f.Threshold(mm.minVal, mm.minVal, cv.ThresholdTypes.BinaryInv)
            mask.ConvertTo(mask, cv.MatType.CV_8U)
            dst32f.SetTo(mm.minVal, mask)
        End If
        dst2 = (dst0 - mm.minVal) * 255 / (mm.maxVal - mm.minVal)
        dst2.ConvertTo(dst2, cv.MatType.CV_8U)

        dst2.SetTo(0, task.noDepthMask)

        If standaloneTest() Then
            mm = GetMinMax(dst2)
            plot.createHistogram = True
            plot.removeZeroEntry = False
            plot.maxRange = mm.maxVal
            plot.Run(dst2)
            dst1 = plot.dst2

            For i = 0 To plot.histArray.Count - 1
                plot.histArray(i) = i
            Next
        End If
        dst3 = ShowPalette254(dst2)

        labels(2) = "Using reduction factor = " + CStr(reduceAmt)
    End Sub
End Class







Public Class RedPrep_Depth : Inherits TaskParent
    Public Sub New()
        cPtr = PrepXY_Open()
        desc = "Run the C++ PrepXY to create a list of mask, rect, and other info about image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim inputX(task.pcSplit(0).Total * task.pcSplit(0).ElemSize - 1) As Byte
        Dim inputY(task.pcSplit(1).Total * task.pcSplit(1).ElemSize - 1) As Byte

        Marshal.Copy(task.pcSplit(0).Data, inputX, 0, inputX.Length)
        Marshal.Copy(task.pcSplit(1).Data, inputY, 0, inputY.Length)

        Dim handleX = GCHandle.Alloc(inputX, GCHandleType.Pinned)
        Dim handleY = GCHandle.Alloc(inputY, GCHandleType.Pinned)

        Dim imagePtr = PrepXY_Run(cPtr, handleX.AddrOfPinnedObject(), handleY.AddrOfPinnedObject(), src.Rows, src.Cols,
                                  task.xRange, task.yRange, task.histogramBins)
        handleX.Free()
        handleY.Free()

        dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
        dst2.SetTo(0, task.noDepthMask)

        dst3 = ShowPalette254(dst2)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = PrepXY_Close(cPtr)
    End Sub
End Class







Public Class RedPrep_FloodFill : Inherits TaskParent
    Public classCount As Integer
    Public rectList As New List(Of cv.Rect)
    Public identifyCount As Integer = 255
    Public Sub New()
        cPtr = RedCloud_Open()
        desc = "Run the C++ RedCloud to create a list of mask, rect, and other info about image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim inputData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim imagePtr = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols, 0)
        handleInput.Free()
        dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone

        classCount = Math.Min(RedCloud_Count(cPtr), identifyCount * 2)
        If classCount = 0 Then Exit Sub ' no data to process.

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedCloud_Rects(cPtr))

        Dim rects(classCount * 4) As Integer
        Marshal.Copy(rectData.Data, rects, 0, rects.Length)

        rectList.Clear()
        For i = 0 To classCount * 4 - 4 Step 4
            rectList.Add(New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3)))
        Next

        If standalone Then dst3 = ShowPalette(dst2)

        If task.heartBeat Then labels(2) = "CV_8U result With " + CStr(classCount) + " regions."
        If task.heartBeat Then labels(3) = "Palette version Of the data In dst2 With " + CStr(classCount) + " regions."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedCloud_Close(cPtr)
    End Sub
End Class







Public Class RedPrep_VB : Inherits TaskParent
    Public Sub New()
        desc = "Simpler transforms for the point cloud using CalcHist instead of reduction."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim histogram As New cv.Mat

        Dim ranges As cv.Rangef() = Nothing, zeroCount As Integer
        For i = 0 To 1
            Select Case i
                Case 0 ' X Reduction
                    dst1 = task.pcSplit(0)
                    ranges = New cv.Rangef() {New cv.Rangef(-task.xRange, task.xRange)}
                Case 1 ' Y Reduction
                    dst1 = task.pcSplit(1)
                    ranges = New cv.Rangef() {New cv.Rangef(-task.yRange, task.yRange)}
            End Select

            cv.Cv2.CalcHist({dst1}, {0}, task.depthMask, histogram, 1, {task.histogramBins}, ranges)

            Dim histArray(histogram.Total - 1) As Single
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

            For j = 0 To histArray.Count - 1
                If histArray(j) = 0 Then zeroCount += 1
                histArray(j) = j
            Next

            histogram = cv.Mat.FromPixelData(histogram.Rows, 1, cv.MatType.CV_32F, histArray)
            cv.Cv2.CalcBackProject({dst1}, {0}, histogram, dst1, ranges)

            If i = 0 Then dst3 = dst1.Clone Else dst3 += dst1
        Next

        dst3.ConvertTo(dst2, cv.MatType.CV_8U)
        dst2.SetTo(0, task.noDepthMask)

        labels(2) = CStr(task.histogramBins * 2 - zeroCount) + " depth regions mapped (control with histogram bins.)"
    End Sub
End Class






Public Class RedPrep_Edges : Inherits TaskParent
    Dim prep As New RedPrep_Depth
    Dim edges As New Edge_Basics
    Public Sub New()
        desc = "Find the edges of XY depth boundaries."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst3 = prep.dst3

        edges.Run(dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst2 = edges.dst2
        labels(2) = edges.labels(2)
    End Sub
End Class






Public Class RedPrep_DepthTiers : Inherits TaskParent
    Dim prep As New RedPrep_Depth
    Dim tiers As New Depth_Tiers
    Public Sub New()
        labels(3) = "RedPrep_Depth output define regions with common XY."
        desc = "Find the edges of XY depth boundaries."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst3 = prep.dst3
        dst1 = prep.dst2

        tiers.Run(src)
        dst1 += tiers.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        dst2 = ShowPalette(dst1)
    End Sub
End Class






Public Class RedPrep_BasicsShow : Inherits TaskParent
    Public prep As New RedCloud_PrepOutline
    Public Sub New()
        desc = "Simpler transforms for the point cloud using CalcHist instead of reduction."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst2 = prep.dst2

        ' dst2.SetTo(0, task.noDepthMask)
        dst2.ConvertTo(dst2, cv.MatType.CV_8U)
        Dim mm = GetMinMax(dst2)
        dst3 = ShowPalette(dst2)
        task.setSelectedCell()

        labels(2) = CStr(mm.maxVal + 1) + " regions were mapped in the depth data - region 0 (black) has no depth."
    End Sub
End Class