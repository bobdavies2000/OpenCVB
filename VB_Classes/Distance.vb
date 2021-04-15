Imports cv = OpenCvSharp

Public Class Distance_Basics : Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "C"
            radio.check(1).Text = "L1"
            radio.check(2).Text = "L2"
            radio.check(1).Checked = True
        End If
        label1 = "Distance results"
        label2 = "Input mask to distance transformm"
        task.desc = "Distance algorithm basics."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If standalone or task.intermediateReview = caller Then src = task.RGBDepth ' to get some zeros in the image...
        Dim gray = src
        If src.Channels = 3 Then gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim DistanceType = cv.DistanceTypes.L2
        If radio.check(0).Checked Then DistanceType = cv.DistanceTypes.C
        If radio.check(1).Checked Then DistanceType = cv.DistanceTypes.L1

        Dim kernelSize = 0 ' this is precise distance (there is no distance of 1)

        Dim dist = gray.DistanceTransform(DistanceType, kernelSize)
        Dim dist32f = dist.Normalize(0, 255, cv.NormTypes.MinMax)
        dist32f.ConvertTo(gray, cv.MatType.CV_8UC1)
        dst1 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class






Public Class Distance_Foreground : Inherits VBparent
    Dim foreground As kMeans_Depth_FG_BG
    Public Sub New()
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "C"
            radio.check(1).Text = "L1"
            radio.check(2).Text = "L2"
            radio.check(2).Checked = True
        End If
        foreground = New kMeans_Depth_FG_BG()
        label1 = "Distance results"
        label2 = "Input mask to distance transformm"
        task.desc = "Distance algorithm basics."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        foreground.Run(src)
        dst2 = foreground.dst1
        Dim fg = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)

        Dim gray = src
        If src.Channels = 3 Then gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim DistanceType = cv.DistanceTypes.L2
        If radio.check(0).Checked Then DistanceType = cv.DistanceTypes.C
        If radio.check(1).Checked Then DistanceType = cv.DistanceTypes.L1

        cv.Cv2.BitwiseAnd(gray, fg, gray)
        Dim kernelSize = 0 ' this is precise distance (there is no distance of 1)

        Dim dist = gray.DistanceTransform(DistanceType, kernelSize)
        Dim dist32f = dist.Normalize(0, 255, cv.NormTypes.MinMax)
        dist32f.ConvertTo(gray, cv.MatType.CV_8UC1)
        dst1 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class



