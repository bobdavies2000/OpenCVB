Imports cv = OpenCvSharp
' https://github.com/shimat/opencvsharp/blob/master/test/OpenCvSharp.Tests/stitching/StitchingTest.cs
Public Class Stitch_Basics : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Number of random images", 10, 50, 10)
            sliders.setupTrackBar("Rectangle width", task.workingRes.Width / 4, task.workingRes.Width - 1, task.workingRes.Width / 2)
            sliders.setupTrackBar("Rectangle height", task.workingRes.Height / 4, task.workingRes.Height - 1, task.workingRes.Height / 2)
        End If
        desc = "Stitch together random parts of a color image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static countSlider = findSlider("Number of random images")
        Static widthSlider = findSlider("Rectangle width")
        Static heightSlider = findSlider("Rectangle height")

        Dim mats As New List(Of cv.Mat)
        Dim imageCount = countSlider.Value
        Dim width = widthSlider.Value
        Dim height = heightSlider.Value
        dst2 = src.Clone()
        For i = 0 To imageCount - 1
            Dim x1 = CInt(msRNG.Next(0, src.Width - width))
            Dim x2 = CInt(msRNG.Next(0, src.Height - height))
            Dim rect = New cv.Rect(x1, x2, width, height)
            dst2.Rectangle(rect, cv.Scalar.Red, 2)
            mats.Add(src(rect).Clone())
        Next

        If task.testAllRunning Then
            ' It runs fine but after several runs during 'Test All', it will fail with an external exception.  Only happens on 'Test All' runs.
            setTrueText("Stitch_Basics only fails when running 'Test All'." + vbCrLf +
                                     "Skipping it during a 'Test All' just so all the other tests can be exercised.", New cv.Point(10, 100), 3)
            Exit Sub
        End If

        Dim stitcher = cv.Stitcher.Create(cv.Stitcher.Mode.Scans)
        Dim pano As New cv.Mat

        ' stitcher may fail with an external exception if you make width and height too small.
        Dim status = stitcher.Stitch(mats, pano)

        dst3.SetTo(0)
        If status = cv.Stitcher.Status.OK Then
            Dim w = pano.Width, h = pano.Height
            If w > dst2.Width Then w = dst2.Width
            If h > dst2.Height Then h = dst2.Height
            pano.CopyTo(dst3(New cv.Rect(0, 0, w, h)))
        Else
            If status = cv.Stitcher.Status.ErrorNeedMoreImgs Then setTrueText("Need more images", 3)
        End If
    End Sub
End Class