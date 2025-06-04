Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedCloud_Basics : Inherits TaskParent
    Dim prep As New RedCloud_PrepData
    Public redMask As New RedMask_Basics
    Dim rcMask As cv.Mat
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        task.redOptions.rcReductionSlider.Value = 100
        rcMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Run the reduced pointcloud output through the RedColor_CPP algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))
        dst1 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(0, 255, cv.ThresholdTypes.Binary)

        prep.Run(src)
        redMask.Run(prep.dst2 And dst1)
        labels(1) = CStr(redMask.mdList.Count) + " maskData cells found in the point cloud."

        If task.heartBeat Then strOut = ""
        For i = 0 To task.rcList.Count - 1
            Dim rc = task.rcList(i)
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
                task.rcList(i) = rc
            End If
        Next

        SetTrueText(strOut, 3)
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
        For i = 0 To task.rcList.Count - 1
            Dim rc = task.rcList(i)
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
                task.rcList(i) = rc
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
        task.redOptions.IdentifyCountBar.Value = 100
        task.redOptions.rcReductionSlider.Value = 1000
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
                dst3 = ShowPalette(dst1)
                labels(3) = CStr(plot.histArray.Count) + " different levels in the prepared data."
            End If
        End If

        labels(2) = task.redOptions.PointCloudReductionLabel + " with reduction factor = " + CStr(reduceAmt)
    End Sub
End Class







Public Class RedCloud_PrepDataOld : Inherits TaskParent
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







Public Class RedCloud_PrepDataNew : Inherits TaskParent
    Public Sub New()
        task.gOptions.HistBinBar.Value = 64
        desc = "Simpler transforms for the point cloud using CalcHist instead of reduction."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim histogram As New cv.Mat

        Dim mm As mmData
        Dim ranges As cv.Rangef()
        For i = 0 To 1
            Select Case i
                Case 0 ' X Reduction
                    dst0 = task.pcSplit(0)
                    ranges = New cv.Rangef() {New cv.Rangef(-task.xRange, task.xRange)}
                Case 1 ' Y Reduction
                    dst0 = task.pcSplit(1)
                    ranges = New cv.Rangef() {New cv.Rangef(-task.yRange, task.yRange)}
                Case 2 ' Z Reduction
                    dst0 = task.pcSplit(2)
                    ranges = New cv.Rangef() {New cv.Rangef(-0.01, task.MaxZmeters + 0.01)}
            End Select

            mm = GetMinMax(dst0)
            cv.Cv2.CalcHist({dst0}, {0}, task.depthMask, histogram, 1, {task.histogramBins}, ranges)

            Dim histArray(histogram.Total - 1) As Single
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

            For j = 0 To histArray.Count - 1
                histArray(j) = j
            Next

            histogram = cv.Mat.FromPixelData(histogram.Rows, 1, cv.MatType.CV_32F, histArray)
            cv.Cv2.CalcBackProject({dst0}, {0}, histogram, dst0, ranges)
            If i = 0 Then dst1 = dst0.Clone Else dst1 += dst0
        Next

        dst1.SetTo(0, task.noDepthMask)
        mm = GetMinMax(dst1)
        dst3 = (dst1 * 255 / mm.maxVal).ToMat
        dst2 = ShowPalette(dst3)
        labels(2) = CStr(mm.maxVal + 1) + " regions were mapped in the depth data - region 0 (black) has no depth."
    End Sub
End Class







Public Class RedCloud_BasicsNew : Inherits TaskParent
    Dim prep As New RedCloud_PrepDataNew
    Public contourMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public contourList As New List(Of contourData)
    Public Sub New()
        task.gOptions.HistBinBar.Value = 64
        desc = "Identify the contours in the RedCloud_PrepDataNew output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst2 = prep.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        labels(2) = prep.labels(2)

        dst3 = runRedC(dst2, labels(3))
        dst3.SetTo(0, task.noDepthMask)

        For Each rc In task.rcList
            dst3.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
        Next
    End Sub
End Class







Public Class RedCloud_Contours : Inherits TaskParent
    Dim prep As New RedCloud_PrepDataNew
    Public contourMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public contourList As New List(Of contourData)
    Public Sub New()
        task.gOptions.HistBinBar.Value = 64
        desc = "Identify the contours in the RedCloud_PrepDataNew output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst2 = prep.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        labels(2) = prep.labels(2)

        dst3 = runRedC(dst2, labels(3))
        dst3.SetTo(0, task.noDepthMask)

        For Each rc In task.rcList
            dst3.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
        Next
    End Sub
End Class