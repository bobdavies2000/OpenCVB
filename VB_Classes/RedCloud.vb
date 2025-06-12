Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedCloud_Basics : Inherits TaskParent
    Dim prep As New RedCloud_PrepXY
    Dim redMask As New RedMask_Basics
    Dim cellGen As New RedCell_Generate
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Run the reduced pointcloud output through the RedColor_CPP algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        redMask.Run(prep.dst2)

        If redMask.mdList.Count = 0 Then Exit Sub ' no data to process.
        cellGen.mdList = redMask.mdList
        cellGen.Run(redMask.dst2)

        dst2 = cellGen.dst2

        labels(2) = cellGen.labels(2)
    End Sub
End Class








Public Class RedCloud_BasicsHist : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Public Sub New()
        task.gOptions.setHistogramBins(100)
        plot.createHistogram = True
        desc = "Display the histogram of a selected RedColor cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))
        If task.heartBeat Then
            Dim depth As cv.Mat = task.pcSplit(2)(task.rcD.rect)
            depth.SetTo(0, task.noDepthMask(task.rcD.rect))
            plot.minRange = 0
            plot.maxRange = task.MaxZmeters
            plot.Run(depth)
            labels(3) = "0 meters to " + Format(task.MaxZmeters, fmt0) + "meters - vertical lines every meter"

            Dim incr = dst2.Width / task.MaxZmeters
            For i = 1 To CInt(task.MaxZmeters - 1)
                Dim x = incr * i
                DrawLine(dst3, New cv.Point(x, 0), New cv.Point(x, dst2.Height), cv.Scalar.White)
            Next
        End If
        dst3 = plot.dst2
    End Sub
End Class





Public Class RedCloud_XYZ : Inherits TaskParent
    Dim prep As New RedCloud_PrepData
    Public redMask As New RedMask_Basics
    Dim rcMask As cv.Mat
    Public Sub New()
        task.redOptions.XYZReduction.Checked = True
        rcMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Run the reduced pointcloud output through the RedColor_CPP algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)

        dst2 = runRedC(prep.dst2, labels(2))

        If task.heartBeat Then strOut = ""
        For i = 0 To task.redC.rcList.Count - 1
            Dim rc = task.redC.rcList(i)
            rcMask.SetTo(0)
            rcMask(rc.rect).SetTo(255, rc.mask)
            rc.mdList = New List(Of maskData)
            For Each md In redMask.mdList
                Dim index = rcMask.Get(Of Byte)(md.maxDist.Y, md.maxDist.X)
                If index > 0 Then rc.mdList.Add(md)
            Next
            If rc.mdList.Count > 0 Then
                For j = 0 To rc.mdList.Count - 1
                    Dim md = rc.mdList(j)
                    rcMask(md.rect) = rcMask(md.rect) And md.mask
                    md.mask = rcMask(md.rect).Clone
                    rc.mdList(j) = md
                Next
                task.redC.rcList(i) = rc
            End If
        Next

        SetTrueText(strOut, 3)
    End Sub
End Class





Public Class RedCloud_YZ : Inherits TaskParent
    Dim prep As New RedCloud_PrepData
    Dim stats As New RedCell_Basics
    Public Sub New()
        task.redOptions.YZReduction.Checked = True
        labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Build YZ RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)

        dst2 = runRedC(prep.dst2, labels(2))

        stats.Run(src)
        dst1 = stats.dst3
        SetTrueText(stats.strOut, 1)
    End Sub
End Class






Public Class RedCloud_XZ : Inherits TaskParent
    Dim prep As New RedCloud_PrepData
    Dim stats As New RedCell_Basics
    Public Sub New()
        task.redOptions.XZReduction.Checked = True
        labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Build XZ RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)

        dst2 = runRedC(prep.dst2, labels(2))
        stats.Run(src)
        dst1 = stats.dst3
        SetTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedCloud_Z : Inherits TaskParent
    Dim prep As New RedCloud_PrepData
    Dim stats As New RedCell_Basics
    Public Sub New()
        task.redOptions.ZReduction.Checked = True
        labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Build Z RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)

        dst2 = runRedC(prep.dst2, labels(2))
        stats.Run(src)
        dst1 = stats.dst3
        SetTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedCloud_World : Inherits TaskParent
    Dim world As New Depth_World
    Dim prep As New RedCloud_PrepData
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(3) = "Generated pointcloud"
        desc = "Display the output of a generated pointcloud as RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        world.Run(src)

        prep.Run(world.dst2)

        dst2 = runRedC(prep.dst2, labels(2))
    End Sub
End Class








Public Class RedCloud_PrepData : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Public Sub New()
        desc = "Reduction transform for the point cloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim split() As cv.Mat = {New cv.Mat, New cv.Mat, New cv.Mat}
        Dim input() As cv.Mat = task.pcSplit
        If src.Type = cv.MatType.CV_32FC3 Then input = src.Split
        Dim reduceAmt = 50
        input(0).ConvertTo(split(0), cv.MatType.CV_32S, 1000 / reduceAmt)
        input(1).ConvertTo(split(1), cv.MatType.CV_32S, 1000 / reduceAmt)
        input(2).ConvertTo(split(2), cv.MatType.CV_32S, 1000 / reduceAmt)

        Select Case task.redOptions.PointCloudReduction
            Case 0 ' X Reduction
                dst0 = (split(0) * reduceAmt).ToMat
            Case 1 ' Y Reduction
                dst0 = (split(1) * reduceAmt).ToMat
            Case 2 ' Z Reduction
                dst0 = (split(2) * reduceAmt).ToMat
            Case 3 ' XY Reduction
                dst0 = (split(0) * reduceAmt + split(1) * reduceAmt).ToMat
            Case 4 ' XZ Reduction
                dst0 = (split(0) * reduceAmt + split(2) * reduceAmt).ToMat
            Case 5 ' YZ Reduction
                dst0 = (split(1) * reduceAmt + split(2) * reduceAmt).ToMat
            Case 6 ' XYZ Reduction
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
            dst3 = plot.dst2

            For i = 0 To plot.histArray.Count - 1
                plot.histArray(i) = i
            Next

            Marshal.Copy(plot.histArray, 0, plot.histogram.Data, plot.histArray.Length)
            cv.Cv2.CalcBackProject({dst2}, {0}, plot.histogram, dst1, plot.ranges)
            dst3 = ShowPalette(dst1)
            dst3.SetTo(0, task.noDepthMask)
            labels(3) = CStr(plot.histArray.Count) + " different levels in the prepared data."
        End If

        labels(2) = task.redOptions.PointCloudReductionLabel + " with reduction factor = " + CStr(reduceAmt)
    End Sub
End Class







Public Class RedCloud_PrepDataHist : Inherits TaskParent
    Public Sub New()
        desc = "Simpler transforms for the point cloud using CalcHist instead of reduction."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim histogram As New cv.Mat

        Dim ranges As cv.Rangef()
        Dim channels() As Integer = {0, 1}
        Select Case task.redOptions.PointCloudReduction
            Case 0 ' X Reduction
                dst0 = task.pcSplit(0)
            Case 1 ' Y Reduction
                dst0 = task.pcSplit(1)
            Case 2 ' Z Reduction
                dst0 = task.pcSplit(2)
            Case 3 ' XY Reduction
                dst0 = task.pcSplit(0) + task.pcSplit(1)
            Case 4 ' XZ Reduction
                dst0 = task.pcSplit(0) + task.pcSplit(2)
            Case 5 ' YZ Reduction
                dst0 = task.pcSplit(1) + task.pcSplit(2)
            Case 6 ' XYZ Reduction
                dst0 = task.pcSplit(0) + task.pcSplit(1) + task.pcSplit(2)
        End Select

        Dim mm = GetMinMax(dst0)

        ranges = New cv.Rangef() {New cv.Rangef(mm.minVal, mm.maxVal)}
        cv.Cv2.CalcHist({dst0}, {0}, task.depthMask, histogram, 1, {task.histogramBins}, ranges)

        Dim histArray(histogram.Total - 1) As Single
        Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

        For i = 0 To histArray.Count - 1
            histArray(i) = i
        Next

        histogram = cv.Mat.FromPixelData(histogram.Rows, 1, cv.MatType.CV_32F, histArray)
        cv.Cv2.CalcBackProject({dst0}, {0}, histogram, dst3, ranges)
        dst3.ConvertTo(dst1, cv.MatType.CV_8U)
        dst2 = ShowPalette(dst1)
        dst2.SetTo(0, task.noDepthMask)

        labels(2) = "Pointcloud data backprojection to " + CStr(task.histogramBins) + " classes."
    End Sub
End Class








Public Class RedCloud_Mats : Inherits TaskParent
    Dim mats As New Mat_4Click
    Public Sub New()
        desc = "Simpler transforms for the point cloud using CalcHist instead of reduction."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim histogram As New cv.Mat

        For i = 0 To 2
            Select Case i
                Case 0 ' X Reduction
                    dst0 = task.pcSplit(0)
                Case 1 ' Y Reduction
                    dst0 = task.pcSplit(1)
                Case 2 ' Z Reduction
                    dst0 = task.pcSplit(2)
            End Select

            Dim mm = GetMinMax(dst0)
            Dim ranges = New cv.Rangef() {New cv.Rangef(mm.minVal, mm.maxVal)}
            cv.Cv2.CalcHist({dst0}, {0}, task.depthMask, histogram, 1, {task.histogramBins}, ranges)

            Dim histArray(histogram.Total - 1) As Single
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

            For j = 0 To histArray.Count - 1
                histArray(j) = j
            Next

            histogram = cv.Mat.FromPixelData(histogram.Rows, 1, cv.MatType.CV_32F, histArray)
            cv.Cv2.CalcBackProject({dst0}, {0}, histogram, dst0, ranges)
            dst0.ConvertTo(dst1, cv.MatType.CV_8U)
            mats.mat(i) = ShowPalette(dst1)
            mats.mat(i).SetTo(0, task.noDepthMask)
        Next

        mats.Run(emptyMat)
        dst2 = mats.dst2
    End Sub
End Class









Public Class RedCloud_PrepDataShow : Inherits TaskParent
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
        'dst2 = ShowPalette((dst2 * 255 / mm.maxVal).ToMat)

        labels(2) = CStr(mm.maxVal + 1) + " regions were mapped in the depth data - region 0 (black) has no depth."
    End Sub
End Class






Public Class RedCloud_PrepOutline : Inherits TaskParent
    Dim prep As New RedCloud_PrepXY
    Public Sub New()
        desc = "Remove corners of RedCloud cells in the prep data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst2 = prep.dst2.Clone

        Dim val1 As Byte, val2 As Byte
        For y = 0 To dst2.Height - 2
            For x = 0 To dst2.Width - 2
                Dim zipData As Boolean = False

                val1 = dst2.Get(Of Byte)(y, x)
                val2 = dst2.Get(Of Byte)(y, x + 1)
                If val1 <> 0 And val2 <> 0 Then If val1 <> val2 Then zipData = True

                val2 = dst2.Get(Of Byte)(y + 1, x)
                If val1 <> 0 And val2 <> 0 Then If val1 <> val2 Then zipData = True

                If zipData Then
                    dst2.Set(Of Byte)(y, x, 0)
                    dst2.Set(Of Byte)(y, x + 1, 0)
                    dst2.Set(Of Byte)(y + 1, x, 0)
                    dst2.Set(Of Byte)(y + 1, x + 1, 0)
                End If
            Next
        Next

        dst3 = dst2.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
    End Sub
End Class






Public Class RedCloud_Outline : Inherits TaskParent
    Dim prep As New RedCloud_PrepOutline
    Public Sub New()
        desc = "Apply the outline of the prepared depth data to the color contours."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst3 = prep.dst3
        dst3.SetTo(0, task.noDepthMask)
        dst3.ConvertTo(dst3, cv.MatType.CV_8U)

        dst1 = task.contours.contourMap
        dst1.SetTo(0, dst3)

        dst2 = runRedC(dst1, labels(2))
    End Sub
End Class





Public Class RedCloud_PrepXY_VB : Inherits TaskParent
    Public Sub New()
        desc = "Simpler transforms for the point cloud using CalcHist instead of reduction."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim histogram As New cv.Mat

        Dim ranges As cv.Rangef(), zeroCount As Integer
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






Public Class RedCloud_Contours : Inherits TaskParent
    Dim prep As New RedCloud_PrepXY
    Dim options As New Options_Contours
    Public contourList As New List(Of contourData)
    Public contourMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Run the reduced pointcloud output through the RedColor_CPP algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        prep.Run(src)
        dst2 = ShowPalette(prep.dst2).CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim allContours As cv.Point()()
        Dim mode = options.options2.ApproximationMode
        cv.Cv2.FindContours(dst2, allContours, Nothing, cv.RetrievalModes.List, mode)
        If allContours.Count <= 1 Then Exit Sub

        contourList = Contour_Basics.sortContours(allContours, 255)
        contourMap.SetTo(0)
        For Each contour In contourList
            contourMap(contour.rect).SetTo(contour.index, contour.mask)
        Next

        dst3 = ShowPalette(contourMap)
        If task.heartBeat Then labels(2) = CStr(contourList.Count) + " depth contours were found."
    End Sub
End Class






Public Class RedCloud_PrepXY : Inherits TaskParent
    Public mdList As New List(Of maskData)
    Public classCount As Integer
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

        dst3 = ShowPalette(dst2)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = PrepXY_Close(cPtr)
    End Sub
End Class






Public Class RedCloud_RedColor : Inherits TaskParent
    Dim contours As New RedCloud_Contours
    Public Sub New()
        desc = "Show the output of the RedCloud and RedColor cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        contours.Run(src)
        dst2 = contours.dst3
        labels(2) = contours.labels(2)

        If task.heartBeat Then
            For Each contour In contours.contourList
                SetTrueText(CStr(contour.index), contour.center, 2)
            Next
        End If

        dst3 = task.contours.dst2
        dst3(task.contourD.rect).SetTo(white, task.contourD.mask)
        Contour_Info.setContourSelection(contours.contourList, contours.contourmap)
        labels(3) = task.contours.labels(2)
    End Sub
End Class
