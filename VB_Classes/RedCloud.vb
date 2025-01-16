Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedCloud_Basics : Inherits TaskParent
    Dim prep As New RedCloud_PrepData
    Public redMask As New RedCloud_Masks
    Public Sub New()
        task.redOptions.rcReductionSlider.Value = 100
        task.gOptions.displayDst1.Checked = True
        desc = "Run the reduced pointcloud output through the RedColor_CPP algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))
        dst1 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)

        prep.Run(src)
        redMask.Run(prep.dst2)

        dst0 = redMask.dst2
        dst0.SetTo(0, Not dst1)
        Dim cellMask = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary)

        Dim depthMeans As New List(Of Single)
        Dim matList(task.rcList.Count - 1) As List(Of Integer)
        Dim maskList As New List(Of cv.Mat)
        For i = 0 To redMask.masks.Count - 1
            Dim mask = redMask.masks(i)
            Dim r = redMask.rectList(i)
            mask = cellMask(r) And mask
            mask.SetTo(0, task.noDepthMask(r))

            Dim depthMean = task.pcSplit(2)(r).Mean(mask)
            depthMeans.Add(depthMean)

            maskList.Add(mask)
            mask.Rectangle(New cv.Rect(0, 0, mask.Width, mask.Height), 0, 1)
            Dim distance32f = mask.DistanceTransform(cv.DistanceTypes.L1, 0)
            Dim mm As mmData = GetMinMax(distance32f)

            Dim index = task.rcMap(r).Get(Of Byte)(mm.maxLoc.Y, mm.maxLoc.X)
            If matList(index) Is Nothing Then matList(index) = New List(Of Integer)
            matList(index).Add(i)
        Next

        dst1 = dst2.Clone
        For i = 0 To matList.Count - 1
            Dim meanList As New List(Of Single)
            If matList(i) Is Nothing Then Continue For
            For j = 0 To matList(i).Count - 1
                Dim index = matList(i)(j)
                meanList.Add(depthMeans(index))
                Dim r = redMask.rectList(index)
                dst1(r).SetTo(task.scalarColors(i), maskList(index))
            Next
            Dim k = 0
        Next
        dst3 = ShowPalette(dst0 * 255 / redMask.classCount)
        labels(3) = redMask.labels(3)
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
        Dim reduceAmt = task.redOptions.rcReductionSlider.Value
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
        dst2 = (dst0 - mm.minVal) * 255 / (mm.maxVal - mm.minVal)
        dst2.ConvertTo(dst2, cv.MatType.CV_8U)

        dst2.SetTo(0, task.noDepthMask)

        If standaloneTest() Then
            If task.heartBeat Then
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
                dst3 = ShowPalette(dst1 * 255 / task.gOptions.HistBinBar.Value)
                labels(3) = CStr(plot.histArray.Count) + " different levels in the prepared data."
            End If
        End If

        labels(2) = task.redOptions.PointCloudReductionLabel + " with reduction factor = " +
                    CStr(reduceAmt)
    End Sub
End Class







Public Class RedCloud_BasicsHist : Inherits TaskParent
    Dim rCloud As New RedCloud_Basics
    Dim plot As New Plot_Histogram
    Public Sub New()
        task.gOptions.setHistogramBins(64)
        labels(3) = "Plot of the depth of the tracking cells (in grayscale), zero to task.maxZmeters in depth"
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        plot.createHistogram = True
        desc = "Display the histogram of the RedCloud_Basics output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        rCloud.Run(src)
        If task.heartBeat Then
            dst2.SetTo(0)
            For Each rc In task.rcList
                dst2(rc.rect).SetTo(rc.depthMean, rc.mask)
            Next
            Dim mm = GetMinMax(dst2, task.depthMask)

            plot.minRange = mm.minVal
            plot.maxRange = mm.maxVal
            plot.Run(dst2)
        End If
        dst3 = plot.dst2
    End Sub
End Class






Public Class RedCloud_BasicsHist1 : Inherits TaskParent
    Dim rCloud As New RedCloud_Basics
    Dim plot As New Plot_Histogram
    Dim mm As mmData
    Public Sub New()
        task.gOptions.setHistogramBins(64)
        task.redOptions.ColorMean.Checked = True
        labels(3) = "Plot of the depth of the tracking cells (in grayscale), zero to task.maxZmeters in depth"
        plot.createHistogram = True
        desc = "Display the histogram of the RedCloud_Basics output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        rCloud.Run(src)
        If task.heartBeat Then
            dst2 = DisplayCells().CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            mm = GetMinMax(dst2, task.depthMask)

            plot.minRange = mm.minVal
            plot.maxRange = mm.maxVal
            plot.Run(dst2)
        End If
        dst3 = plot.dst2
        labels(2) = rCloud.labels(2)
    End Sub
End Class






Public Class RedCloud_BasicsTest : Inherits TaskParent
    Dim rCloud As New RedCloud_Basics
    Public Sub New()
        desc = "Run RedCloud with the depth reduction."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        rCloud.Run(src)

        dst2 = runRedC(rCloud.dst2, labels(2))
    End Sub
End Class









Public Class RedCloud_YZ : Inherits TaskParent
    Dim stats As New Cell_Basics
    Dim rCloud As New RedCloud_Basics
    Public Sub New()
        task.redOptions.IdentifyCountBar.Value = 100
        task.redOptions.YZReduction.Checked = True
        desc = "Build horizontal RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        rCloud.Run(src)

        stats.Run(src)
        dst0 = stats.dst0
        dst1 = stats.dst1
        dst2 = stats.dst2
        SetTrueText(stats.strOut, 3)

        rCloud.Run(src)
        dst2 = rCloud.dst2
        labels(2) = rCloud.labels(2)
    End Sub
End Class






Public Class RedCloud_XZ : Inherits TaskParent
    Dim stats As New Cell_Basics
    Dim rCloud As New RedCloud_Basics
    Public Sub New()
        task.redOptions.IdentifyCountBar.Value = 100
        task.redOptions.XZReduction.Checked = True
        desc = "Build vertical RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        stats.Run(src)
        dst0 = stats.dst0
        dst1 = stats.dst1
        dst2 = stats.dst2
        SetTrueText(stats.strOut, 3)

        rCloud.Run(src)
        dst2 = rCloud.dst2
        labels(2) = rCloud.labels(2)
    End Sub
End Class






Public Class RedCloud_World : Inherits TaskParent
    Dim world As New Depth_World
    Dim prep As New RedCloud_PrepData
    Public Sub New()
        task.redOptions.IdentifyCountBar.Value = 100
        task.redOptions.rcReductionSlider.Value = 1000
        labels(3) = "Generated pointcloud"
        desc = "Display the output of a generated pointcloud as RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        world.Run(src)

        prep.Run(world.dst2)

        dst2 = runRedC(prep.dst2, labels(2))
    End Sub
End Class





Public Class RedCloud_ColorAndCloud : Inherits TaskParent
    Dim redL As New RedCloud_Basics
    Public Sub New()
        desc = "Use the results of RedColor_Basics to create a mask for use with RedCloud_Basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static saveIDcount = task.redOptions.IdentifyCountBar.Value
        dst2 = runRedC(src, labels(2))

        dst1 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        redL.redMask.inputRemoved = dst1.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        redL.Run(src)

        dst3 = redL.dst2
        labels(3) = redL.labels(2)
    End Sub
End Class





Public Class RedCloud_Masks : Inherits TaskParent
    Public inputRemoved As cv.Mat
    Public classCount As Integer
    Public rectList As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public depth As New List(Of Single)
    Public Sub New()
        cPtr = RedColor_Open()
        inputRemoved = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Run the C++ RedCloud interface with or without a mask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then
            Static color As New Color8U_Basics
            color.Run(src)
            src = color.dst2
        End If
        Dim inputData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim maskData(src.Total - 1) As Byte
        Marshal.Copy(inputRemoved.Data, maskData, 0, maskData.Length)
        Dim handleMask = GCHandle.Alloc(maskData, GCHandleType.Pinned)

        Dim imagePtr = RedColor_Run(cPtr, handleInput.AddrOfPinnedObject(),
                                    handleMask.AddrOfPinnedObject(), src.Rows, src.Cols, task.rcMinSize)
        handleMask.Free()
        handleInput.Free()
        dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone

        classCount = Math.Min(RedColor_Count(cPtr), 255)
        If classCount = 0 Then Exit Sub ' no data to process.

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedColor_Rects(cPtr))

        Dim rects(classCount * 4) As Integer
        Marshal.Copy(rectData.Data, rects, 0, rects.Length)

        rectList.Clear()
        For i = 0 To classCount * 4 - 4 Step 4
            Dim r = New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3))
            ' If r.Size = dst2.Size Then Continue For ' RedColor_Run finds a cell this big.  
            If r.Width * r.Height < task.rcMinSize Then Continue For
            rectList.Add(r)
        Next

        masks.Clear()
        dst1.SetTo(0)
        Dim map = task.redC.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        map = map.Threshold(0, 255, cv.ThresholdTypes.Binary)
        For i = 0 To rectList.Count - 1
            Dim rect = rectList(i)
            Dim mask = dst2(rect).InRange(i + 1, i + 1)
            Dim contour = ContourBuild(mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            DrawContour(mask, contour, 255, -1)
            masks.Add(mask.Clone)

            Dim test As cv.Mat = map(rect) And mask
            If test.countnonzero Then dst1(rect).SetTo(255, mask)
        Next

        classCount = rectList.Count

        If standaloneTest() Then dst3 = ShowPalette(dst2 * 255 / classCount)

        If task.heartBeat Then labels(2) = "CV_8U result with " + CStr(classCount) + " regions."
        If task.heartBeat Then labels(3) = "Palette version of the data in dst2 with " + CStr(classCount) + " regions."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedColor_Close(cPtr)
    End Sub
End Class