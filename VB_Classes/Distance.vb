Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Distance_Basics : Inherits VB_Algorithm
    Dim options As New Options_Distance
    Public Sub New()
        labels = {"", "", "Distance transform - create a mask with threshold", ""}
        desc = "Distance algorithm basics."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If standalone Then src = task.depthRGB
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim dst0 = src.DistanceTransform(options.distanceType, options.kernelSize)
        dst1 = vbNormalize32f(dst0)
        dst1.ConvertTo(dst2, cv.MatType.CV_8UC1)
    End Sub
End Class






Public Class Distance_Labels : Inherits VB_Algorithm
    Dim options As New Options_Distance
    Public Sub New()
        labels(2) = "Distance results"
        labels(3) = "Input mask to distance transformm"
        desc = "Distance algorithm basics."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If standalone Then src = task.depthRGB
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        'Dim labels As cv.Mat
        'cv.Cv2.DistanceTransformWithLabels(src, dst0, labels, cv.DistanceTypes.L2, cv.DistanceTransformMasks.Precise)
        'Dim dist32f = dst0.Normalize(0, 255, cv.NormTypes.MinMax)
        'dist32f.ConvertTo(src, cv.MatType.CV_8UC1)
        'dst2 = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class






Public Class Distance_Foreground : Inherits VB_Algorithm
    Dim dist As New Distance_Basics
    Dim foreground As New KMeans_Foreground
    Public useBackgroundAsInput As Boolean
    Public Sub New()
        labels(2) = "Distance results"
        labels(3) = "Input mask to distance transformm"
        desc = "Distance algorithm basics."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static cRadio = findRadio("C")
        Static l1Radio = findRadio("L1")

        foreground.Run(src)
        dst3 = If(useBackgroundAsInput, foreground.dst2, foreground.dst3)

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim DistanceType = cv.DistanceTypes.L2
        If cRadio.Checked Then DistanceType = cv.DistanceTypes.C
        If l1Radio.Checked Then DistanceType = cv.DistanceTypes.L1

        src = dst3 And src
        Dim kernelSize = 0 ' this is precise distance (there is no distance of 1)

        Dim dist = src.DistanceTransform(DistanceType, kernelSize)
        Dim dist32f = dist.Normalize(0, 255, cv.NormTypes.MinMax)
        dist32f.ConvertTo(src, cv.MatType.CV_8UC1)
        dst2 = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class







Public Class Distance_Background : Inherits VB_Algorithm
    Dim dist As New Distance_Foreground
    Public Sub New()
        dist.useBackgroundAsInput = True
        desc = "Use distance algorithm on the background"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        dist.Run(src)
        dst2 = dist.dst2
        dst3 = dist.dst3
        labels(2) = dist.labels(2)
        labels(3) = dist.labels(3)
    End Sub
End Class






Public Class Distance_Point3D : Inherits VB_Algorithm
    Public inPoint1 As cv.Point3f
    Public inPoint2 As cv.Point3f
    Public distance As Single
    Public Sub New()
        desc = "Compute the distance in meters between 3D points in the point cloud"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If standalone And heartBeat() Then
            inPoint1 = New cv.Point3f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height), msRNG.Next(0, 10000))
            inPoint2 = New cv.Point3f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height), msRNG.Next(0, 10000))

            dst2.SetTo(0)
            Dim p1 = New cv.Point(inPoint1.X, inPoint1.Y)
            Dim p2 = New cv.Point(inPoint2.X, inPoint2.Y)
            dst2.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)

            Dim vec1 = task.pointCloud.Get(Of cv.Point3f)(p1.Y, p1.X)
            Dim vec2 = task.pointCloud.Get(Of cv.Point3f)(p2.Y, p2.X)
        End If

        Dim x = inPoint1.X - inPoint2.X
        Dim y = inPoint1.Y - inPoint2.Y
        Dim z = inPoint1.Z - inPoint2.Z
        distance = Math.Sqrt(x * x + y * y + z * z)

        strOut = Format(inPoint1.X, fmt3) + ", " + Format(inPoint1.Y, fmt3) + ", " + Format(inPoint1.Z, fmt3) + vbCrLf
        strOut += Format(inPoint2.X, fmt3) + ", " + Format(inPoint2.Y, fmt3) + ", " + Format(inPoint2.Z, fmt3) + vbCrLf
        strOut += "Distance = " + Format(distance, fmt3)
        setTrueText(strOut, 3)
    End Sub
End Class






Public Class Distance_Point4D : Inherits VB_Algorithm
    Public inPoint1 As cv.Vec4f
    Public inPoint2 As cv.Vec4f
    Public distance As Single
    Public Sub New()
        desc = "Compute the distance between 4D points"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            inPoint1 = New cv.Vec4f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height),
                                    msRNG.Next(0, task.maxZmeters), msRNG.Next(0, task.maxZmeters))
            inPoint2 = New cv.Vec4f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height),
                                    msRNG.Next(0, task.maxZmeters), msRNG.Next(0, task.maxZmeters))
        End If

        Dim x = inPoint1(0) - inPoint2(0)
        Dim y = inPoint1(1) - inPoint2(1)
        Dim z = inPoint1(2) - inPoint2(2)
        Dim d = inPoint1(3) - inPoint2(3)
        distance = Math.Sqrt(x * x + y * y + z * z + d * d)

        strOut = inPoint1.ToString + vbCrLf + inPoint2.ToString + vbCrLf + "Distance = " + Format(distance, fmt1)
        setTrueText(strOut, New cv.Point(10, 10), 2)
    End Sub
End Class









Public Class Distance_Threshold : Inherits VB_Algorithm
    Dim accum As New Edge_MotionAccum
    Dim dist As New Distance_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold distance", 0, 100, 20)
        desc = "Find the top pixels in the distance algorithm."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = findSlider("Threshold distance")
        Dim testSlider = thresholdSlider.value

        accum.Run(src)

        dist.Run(Not accum.dst2)
        dst2 = dist.dst2
        dst3 = dst2.Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class
