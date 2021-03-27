Imports cv = OpenCvSharp
Imports OpenCvSharp.XFeatures2D
'https://github.com/shimat/opencvsharp/wiki/ORB-and-FREAK
Public Class FREAK_Basics
    Inherits VBparent
    Dim orb As ORB_Basics
    Public Sub New()
        initParent()
        orb = New ORB_Basics()
        task.desc = "Find keypoints using ORB and FREAK algorithm"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        orb.src = src
        orb.Run()

        Dim freak = cv.XFeatures2D.FREAK.Create()
        Dim fdesc = New cv.Mat
        freak.Compute(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), orb.keypoints, fDesc)

        dst1 = src.Clone()

        For Each kpt In orb.keypoints
            Dim r = kpt.Size / 2
            dst1.Circle(kpt.Pt, r, cv.Scalar.Green)
            dst1.Line(New cv.Point(kpt.Pt.X + r, kpt.Pt.Y + r), New cv.Point(kpt.Pt.X - r, kpt.Pt.Y - r), cv.Scalar.Green)
            dst1.Line(New cv.Point(kpt.Pt.X + r, kpt.Pt.Y - r), New cv.Point(kpt.Pt.X - r, kpt.Pt.Y + r), cv.Scalar.Green)
        Next
        label1 = CStr(orb.keypoints.Count) + " key points were identified"
        label2 = CStr(orb.keypoints.Count) + " FREAK Descriptors (resized) One row = keypoint"
        If fDesc.Width > 0 And fDesc.Height > 0 Then dst2 = fDesc.Resize(dst2.Size())
    End Sub
End Class

