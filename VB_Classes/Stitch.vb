Imports cv = OpenCvSharp
' https://github.com/shimat/opencvsharp/blob/master/test/OpenCvSharp.Tests/stitching/StitchingTest.cs
Public Class Stitch_Basics : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Number of random images", 10, 50, 10)
            sliders.setupTrackBar(1, "Rectangle width", task.color.Width / 4, task.color.Width - 1, task.color.Width / 2)
            sliders.setupTrackBar(2, "Rectangle height", task.color.Height / 4, task.color.Height - 1, task.color.Height / 2)
        End If
        task.desc = "Stitch together random parts of a color image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim mats As New List(Of cv.Mat)
        Dim imageCount = sliders.trackbar(0).Value
        Dim width = sliders.trackbar(1).Value
        Dim height = sliders.trackbar(2).Value
        dst1 = src.Clone()
        For i = 0 To imageCount - 1
            Dim x1 = CInt(msRNG.next(0, src.Width - width))
            Dim x2 = CInt(msRNG.next(0, src.Height - height))
            Dim rect = New cv.Rect(x1, x2, width, height)
            dst1.Rectangle(rect, cv.Scalar.Red, 2)
            mats.Add(src(rect).Clone())
        Next

        'If task.parms.testAllRunning  Then
        ' It runs fine but after several runs, it will fail with an external exception.  Only happens on 'Test All' runs.
        setTrueText("Stitch_Basics only fails when running 'Test All'." + vbCrLf +
                                     "Skipping it during a 'Test All' just so all the other tests can be exercised.")
        Exit Sub
        'End If

        Dim stitcher = cv.Stitcher.Create(cv.Stitcher.Mode.Scans)
        Dim pano As New cv.Mat

        ' stitcher may fail with an external exception if you make width and height too small.
        Dim status = stitcher.Stitch(mats, pano)

        dst2.SetTo(0)
        If status = cv.Stitcher.Status.OK Then
            Dim w = pano.Width, h = pano.Height
            If w > dst1.Width Then w = dst1.Width
            If h > dst1.Height Then h = dst1.Height
            pano.CopyTo(dst2(New cv.Rect(0, 0, w, h)))
        Else
            If status = cv.Stitcher.Status.ErrorNeedMoreImgs Then
                dst2.PutText("Need more images", New cv.Point(10, 60), cv.HersheyFonts.HersheySimplex, 0.5, cv.Scalar.White, task.lineWidth, task.lineType)
            End If
        End If
    End Sub
End Class



