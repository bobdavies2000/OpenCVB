Imports cvb = OpenCvSharp
Public Class Linear_Input : Inherits VB_Parent
    Public plotHist As New Plot_Histogram
    Public roi As New cvb.Rect(0, 0, dst2.Width, dst2.Height)
    Public pc As cvb.Mat
    Public options As New Options_Gradient_Cloud
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        If standalone Then task.gOptions.setDisplay1()

        plotHist.createHistogram = True
        plotHist.removeZeroEntry = True

        labels = {"Mask of differences <= 0", "Mask of differences > deltaX", "Point Cloud deltaX data", ""}
        desc = "Find the gradient in the x and y direction "
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        pc = task.pointCloud(roi)
        Dim mmX = GetMinMax(task.pcSplit(0)(roi))
        Dim mmY = GetMinMax(task.pcSplit(1)(roi))
        Dim mmZ = GetMinMax(task.pcSplit(2)(roi))
        Dim pcShifted As cvb.Scalar = New cvb.Scalar(mmX.minVal, mmY.minVal, mmZ.minVal)
        pc -= pcShifted

        Dim r1 = New cvb.Rect(0, 0, pc.Width - 1, pc.Height)
        Dim r2 = New cvb.Rect(1, 0, r1.Width, r1.Height)

        dst2 = pc(r2) - pc(r1)

        dst2 = dst2.Resize(roi.Size, 0, 0, cvb.InterpolationFlags.Nearest)
        dst0 = Not dst2.Threshold(0, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs
        dst1 = dst2.Threshold(options.deltaX, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs

        dst2 = dst2.Clone

        dst2.SetTo(0, dst0)
        dst2.SetTo(0, dst1)

        'If task.optionsChanged Then
        '    plotHist.minRange = 0
        '    plotHist.maxRange = options.deltaX
        '    labels(3) = "0 to " + CStr(options.deltaX) + " mm's difference from neighbor "
        'End If
        'plotHist.Run(dst2)
        'dst3 = plotHist.dst2
    End Sub
End Class











Public Class Linear_BackProject : Inherits VB_Parent
    Dim cloudX As New Linear_Input
    Public Sub New()
        desc = "Backproject the neighbor difference values - incomplete work..."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        'cloudX.Run(src)
        'dst2 = cloudX.dst2

        'Dim r1 As New cvb.Rect(0, 0, dst2.Width - 1, dst2.Height)
        'Dim r2 As New cvb.Rect(1, 0, r1.Width, r1.Height)

        'cvb.Cv2.CalcBackProject({cloudX.pc}, {0}, cloudX.plotHist.histogram, dst0, cloudX.plotHist.ranges)
        'dst3 = task.pcSplit(0)(r1) + dst0(r2)

    End Sub
End Class





'Public Class BackProject_CloudXcell : Inherits VB_Parent
'    Public Sub New()
'        desc = "Backproject the neighbor difference values cell by cell."
'    End Sub
'    Public Sub RunAlg(src As cvb.Mat)
'        For Each roi In task.fLessRects

'        Next

'    End Sub
'End Class




Public Class Linear_InputX : Inherits VB_Parent
    Public plotHist As New Plot_Histogram
    Public roi As New cvb.Rect(0, 0, dst2.Width, dst2.Height)
    Public pc As cvb.Mat
    Public options As New Options_Gradient_Cloud
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        If standalone Then task.gOptions.setDisplay1()

        plotHist.createHistogram = True
        plotHist.removeZeroEntry = True

        labels = {"Mask of differences <= 0", "Mask of differences > deltaX", "Point Cloud deltaX data", ""}
        desc = "Provide a mask for pixels that are within x mm depth of its neighbor"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        pc = task.pcSplit(0)(roi)
        Dim mm = GetMinMax(pc)
        Dim pcShifted As cvb.Mat = pc - mm.minVal

        Dim r1 = New cvb.Rect(0, 0, dst2.Width - 1, dst2.Height)
        Dim r2 = New cvb.Rect(1, 0, r1.Width, r1.Height)

        dst2 = pcShifted(r2) - pcShifted(r1)

        dst2 = dst2.Resize(roi.Size, 0, 0, cvb.InterpolationFlags.Nearest)
        dst0 = Not dst2.Threshold(0, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs
        dst1 = dst2.Threshold(options.deltaX, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs

        dst2 = dst2.Clone

        dst2.SetTo(0, dst0)
        dst2.SetTo(0, dst1)

        If task.optionsChanged Then
            plotHist.minRange = 0
            plotHist.maxRange = options.deltaX
            labels(3) = "0 to " + CStr(options.deltaX) + " mm's difference from neighbor "
        End If
        plotHist.Run(dst2)
        dst3 = plotHist.dst2
    End Sub
End Class