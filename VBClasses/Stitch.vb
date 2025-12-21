Imports cv = OpenCvSharp
' https://github.com/shimat/opencvsharp/blob/master/test/OpenCvSharp.Tests/stitching/StitchingTest.cs
Namespace VBClasses
    Public Class Stitch_Basics : Inherits TaskParent
        Dim options As New Options_Stitch
        Dim sticherObj As cv.Stitcher
        Public Sub New()
            desc = "Stitch together random parts of a color image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim mats As New List(Of cv.Mat)
            dst2 = src.Clone()
            For i = 0 To options.imageCount - 1
                Dim x1 = CInt(msRNG.Next(0, src.Width - options.width))
                Dim x2 = CInt(msRNG.Next(0, src.Height - options.height))
                Dim rect = New cv.Rect(x1, x2, options.width, options.height)
                dst2.Rectangle(rect, cv.Scalar.Red, 2)
                mats.Add(src(rect).Clone())
            Next

            If sticherObj IsNot Nothing Then sticherObj = cv.Stitcher.Create(cv.Stitcher.Mode.Scans)
            Dim pano As New cv.Mat

            SetTrueText("This algorithm stopped working.  Not sure why but it is not used anywhere.  Fix whenever...", 3)
            Exit Sub

            ' stitcher may fail with an external exception if you make width and height too small.
            Dim status = sticherObj.Stitch(mats, pano)
            dst3.SetTo(0)
            If status = cv.Stitcher.Status.OK Then
                Dim w = pano.Width, h = pano.Height
                If w > dst2.Width Then w = dst2.Width
                If h > dst2.Height Then h = dst2.Height
                pano.CopyTo(dst3(New cv.Rect(0, 0, w, h)))
            Else
                If status = cv.Stitcher.Status.ErrorNeedMoreImgs Then SetTrueText("Need more images", 3)
            End If
        End Sub
        Public Sub Close()
            If sticherObj IsNot Nothing Then sticherObj.Dispose()
        End Sub
    End Class
End Namespace