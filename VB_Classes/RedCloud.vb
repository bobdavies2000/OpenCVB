Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedCloud_Basics : Inherits TaskParent
    Dim prep As New RedCloud_PrepData
    Public redMask As New RedMask_Basics
    Dim rcMask As cv.Mat
    Public Sub New()
        task.redOptions.rcReductionSlider.Value = 100
        rcMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Run the reduced pointcloud output through the RedColor_CPP algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))
        Dim redColorMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        redColorMask = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(0, 255, cv.ThresholdTypes.Binary)

        prep.Run(src)
        redMask.Run(prep.dst2 And redColorMask)
        Dim mm = GetMinMax(redMask.dst2)
        dst3 = ShowPalette(255 * redMask.dst2 / mm.maxVal)
        dst3.SetTo(0, task.noDepthMask)
        labels(1) = CStr(redMask.mdList.Count) + " maskData cells were found in the point cloud."

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
                    If rc.index = task.gOptions.DebugSlider.Value And task.heartBeat Then
                        strOut += Format(md.minDepthVec.Z, fmt1) + vbTab + Format(md.maxDepthVec.Z, fmt1) + vbCrLf
                    End If
                Next
                task.rcList(i) = rc
            End If
        Next

        If standaloneTest() Then
            For Each rc In task.rcList
                For Each md In rc.mdList
                    DrawCircle(dst3, md.maxDist, task.DotSize, task.HighlightColor)
                Next
            Next
        End If
        SetTrueText(strOut, 3)
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
    Dim plot As New Plot_Histogram
    Public Sub New()
        task.gOptions.setHistogramBins(100)
        plot.createHistogram = True
        desc = "Display the histogram of a selected RedColor cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))
        If task.heartBeat Then
            Dim depth As cv.Mat = task.pcSplit(2)(task.rc.rect)
            depth.SetTo(0, task.noDepthMask(task.rc.rect))
            plot.minRange = 0
            plot.maxRange = task.MaxZmeters
            plot.Run(depth)
            labels(3) = "0 meters to " + Format(task.MaxZmeters, fmt0) + "meters - verticals every meter"

            Dim incr = dst2.Width / task.MaxZmeters
            For i = 1 To CInt(task.MaxZmeters - 1)
                Dim x = incr * i
                DrawLine(dst3, New cv.Point(x, 0), New cv.Point(x, dst2.Height), cv.Scalar.White)
            Next
        End If
        dst3 = plot.dst2
    End Sub
End Class






Public Class RedCloud_YZ : Inherits TaskParent
    Dim stats As New Cell_Basics
    Dim rCloud As New RedCloud_Basics
    Public Sub New()
        task.redOptions.IdentifyCountBar.Value = 100
        task.redOptions.YZReduction.Checked = True
        task.gOptions.displayDst1.Checked = True
        desc = "Build horizontal RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        stats.Run(src)
        dst2 = stats.dst2
        SetTrueText(stats.strOut, 3)

        rCloud.Run(src)
        dst1 = rCloud.dst1
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
        task.gOptions.displayDst1.Checked = True
        desc = "Build vertical RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        stats.Run(src)
        dst2 = stats.dst2
        SetTrueText(stats.strOut, 3)

        rCloud.Run(src)
        dst1 = rCloud.dst1
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
        task.gOptions.displayDst1.Checked = True
        labels(3) = "Generated pointcloud"
        desc = "Display the output of a generated pointcloud as RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        world.Run(src)

        prep.Run(world.dst2)

        dst2 = runRedC(prep.dst2, labels(2))
    End Sub
End Class