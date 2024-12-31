Imports cvb = OpenCvSharp
' https://github.com/shimat/opencvsharp/blob/master/test/OpenCvSharp.Tests/stitching/StitchingTest.cs
Public Class Stitch_Basics : Inherits TaskParent
    Dim options As New Options_Stitch
    Public Sub New()
        desc = "Stitch together random parts of a color image."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        options.RunOpt()

        Dim mats As New List(Of cvb.Mat)
        dst2 = src.Clone()
        For i = 0 To options.imageCount - 1
            Dim x1 = CInt(msRNG.Next(0, src.Width - options.width))
            Dim x2 = CInt(msRNG.Next(0, src.Height - options.height))
            Dim rect = New cvb.Rect(x1, x2, options.width, options.height)
            dst2.Rectangle(rect, cvb.Scalar.Red, 2)
            mats.Add(src(rect).Clone())
        Next

        If task.testAllRunning Then
            ' It runs fine but after several runs during 'Test All', it will fail with an external exception.  Only happens on 'Test All' runs.
            SetTrueText("Stitch_Basics only fails when running 'Test All'." + vbCrLf +
                                     "Skipping it during a 'Test All' just so all the other tests can be exercised.", New cvb.Point(10, 100), 3)
            Exit Sub
        End If

        Dim stitcher = cvb.Stitcher.Create(cvb.Stitcher.Mode.Scans)
        Dim pano As New cvb.Mat

        ' stitcher may fail with an external exception if you make width and height too small.
        Dim status = stitcher.Stitch(mats, pano)

        dst3.SetTo(0)
        If status = cvb.Stitcher.Status.OK Then
            Dim w = pano.Width, h = pano.Height
            If w > dst2.Width Then w = dst2.Width
            If h > dst2.Height Then h = dst2.Height
            pano.CopyTo(dst3(New cvb.Rect(0, 0, w, h)))
        Else
            If status = cvb.Stitcher.Status.ErrorNeedMoreImgs Then SetTrueText("Need more images", 3)
        End If
    End Sub
End Class