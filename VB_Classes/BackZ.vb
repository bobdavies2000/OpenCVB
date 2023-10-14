Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class BackZ_HistogramTop : Inherits VB_Algorithm
    Public options As New Options_BackYZ
    Public histogram As New cv.Mat
    Public Sub New()
        labels(3) = "Top View all samples with toggled walls"
        desc = "Find concentrations in the Top View."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        cv.Cv2.CalcHist({src}, options.channelsTop, New cv.Mat, histogram, 2,
                        {src.Height, src.Width}, options.rangesTop)
        histogram.Row(0).SetTo(0)

        Dim threshold = options.histogramThreshold
        dst2 = histogram.Threshold(threshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        dst1 = histogram.InRange(threshold, threshold).ConvertScaleAbs
        dst3.SetTo(0)
        dst0 = dst1 And task.pcSplit(1).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        dst3.SetTo(cv.Scalar.White, dst1)
        dst0 = dst1 And task.pcSplit(1).Threshold(0, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs
        dst3.SetTo(cv.Scalar.Gray, dst0)

        If task.toggleEverySecond Then dst3.SetTo(cv.Scalar.Red, dst2)

        labels(2) = "Top View after threshold of " + CStr(threshold) + " sample"
        setTrueText("camera at top", New cv.Point(dst2.Width / 2 + 5, 0), 2)
        setTrueText("camera at top", New cv.Point(dst2.Width / 2 + 5, 0), 3)
    End Sub
End Class






Public Class BackZ_HistogramSide : Inherits VB_Algorithm
    Public options As New Options_BackYZ
    Public histogram As New cv.Mat
    Public Sub New()
        desc = "Find concentrations in the Side View."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        cv.Cv2.CalcHist({src}, options.channelsSide, New cv.Mat, histogram, 2,
                        {src.Height, src.Width}, options.rangesSide)
        histogram.Col(0).SetTo(0)

        Dim threshold = options.histogramThreshold
        dst2 = histogram.Threshold(threshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        If standalone Or testIntermediate(traceName) Then
            dst1 = histogram.InRange(threshold, threshold).ConvertScaleAbs
            dst3.SetTo(0)
            dst0 = dst1 And task.pcSplit(1).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            dst3.SetTo(cv.Scalar.Yellow, dst1)
            dst0 = dst1 And task.pcSplit(1).Threshold(0, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs
            dst3.SetTo(cv.Scalar.Gray, dst0)

            If task.toggleEverySecond Then dst3.SetTo(cv.Scalar.Red, dst2)

            setTrueText("camera at left", New cv.Point(0, dst2.Height * 3 / 4 + 5), 2)
            setTrueText("camera at left", New cv.Point(0, dst2.Height * 3 / 4 + 5), 3)
        End If
    End Sub
End Class











Public Class BackZ_Density2D : Inherits VB_Algorithm
    Dim topZ As New BackZ_HistogramTop
    Dim dense As New Density_Basics
    Public Sub New()
        desc = "Find walls using the high density metric"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dense.Run(src)

        topZ.Run(task.pointCloud.SetTo(0, dense.dst2))
        dst2 = topZ.dst2
        dst3 = topZ.dst3
    End Sub
End Class