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
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone Or task.intermediateName = caller Then src = task.RGBDepth
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim DistanceType = cv.DistanceTypes.L2
        If radio.check(0).Checked Then DistanceType = cv.DistanceTypes.C
        If radio.check(1).Checked Then DistanceType = cv.DistanceTypes.L1

        Dim kernelSize = 0 ' this is precise distance (there is no distance of 1)

        Dim dist = src.DistanceTransform(DistanceType, kernelSize)
        Dim dist32f = dist.Normalize(0, 255, cv.NormTypes.MinMax)
        dist32f.ConvertTo(src, cv.MatType.CV_8UC1)
        dst1 = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class






Public Class Distance_Foreground : Inherits VBparent
    Dim dist As New Distance_Basics
    Dim foreground As New KMeans_Depth_FG_BG
    Public useBackgroundAsInput As Boolean
    Public Sub New()
        label1 = "Distance results"
        label2 = "Input mask to distance transformm"
        task.desc = "Distance algorithm basics."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static cRadio = findRadio("C")
        Static l1Radio = findRadio("L1")

        foreground.Run(src)
        dst2 = If(useBackgroundAsInput, foreground.dst1, foreground.dst2)

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim DistanceType = cv.DistanceTypes.L2
        If cRadio.Checked Then DistanceType = cv.DistanceTypes.C
        If l1Radio.Checked Then DistanceType = cv.DistanceTypes.L1

        cv.Cv2.BitwiseAnd(src, dst2, src)
        Dim kernelSize = 0 ' this is precise distance (there is no distance of 1)

        Dim dist = src.DistanceTransform(DistanceType, kernelSize)
        Dim dist32f = dist.Normalize(0, 255, cv.NormTypes.MinMax)
        dist32f.ConvertTo(src, cv.MatType.CV_8UC1)
        dst1 = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class







Public Class Distance_Background : Inherits VBparent
    Dim dist As New Distance_Foreground
    Public Sub New()
        dist.useBackgroundAsInput = True
        task.desc = "Use distance algorithm on the background"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dist.Run(src)
        dst1 = dist.dst1
        dst2 = dist.dst2
        label1 = dist.label1
        label2 = dist.label2
    End Sub
End Class