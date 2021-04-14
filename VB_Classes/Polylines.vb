Imports cv = OpenCvSharp
Public Class Polylines_IEnumerableExample
    Inherits VBparent
    Public Sub New()
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Polyline closed if checked"
            check.Box(0).Checked = True
        End If

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Polyline Count", 2, 500, 100)
            sliders.setupTrackBar(1, "Polyline Thickness", 0, 10, 1)
        End If
        task.desc = "Manually create an ienumerable(of ienumerable(of cv.point))."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        Dim points = Enumerable.Range(0, sliders.trackbar(0).Value).Select(Of cv.Point)(
            Function(i)
                Return New cv.Point(CInt(msRNG.Next(0, src.Width)), CInt(msRNG.Next(0, src.Height)))
            End Function).ToList
        Dim pts As New List(Of List(Of cv.Point))
        pts.Add(points)

        dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
        ' NOTE: when there are 2 points, there will be 1 line.
        dst1.Polylines(pts, check.Box(0).Checked, cv.Scalar.White, sliders.trackbar(1).Value, task.lineType)
    End Sub
End Class





' VB.Net implementation of the browse example in OpenCV.
' https://github.com/opencv/opencv/blob/master/samples/python/browse.py
Public Class Polylines_Random
    Inherits VBparent
    Dim zoomFactor = 4
    Public Sub New()
        label1 = CStr(zoomFactor) + "X zoom around mouse movement on image"
        task.desc = "Create a random procedural image - Painterly Effect"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.frameCount Mod 150 = 0 Then ' every x frames.
            Dim h = src.Height, w = src.Width
            Dim autorand As New Random
            Dim points2f(10000) As cv.Point2f
            Dim pts As New List(Of List(Of cv.Point))
            Dim points As New List(Of cv.Point)
            points2f(0) = New cv.Point2f(autorand.NextDouble() - 0.5, autorand.NextDouble() - 0.5)
            For i = 1 To points2f.Length - 1
                points2f(i) = New cv.Point2f(autorand.NextDouble() - 0.5 + points2f(i - 1).X, autorand.NextDouble() - 0.5 + points2f(i - 1).Y)
                points.Add(New cv.Point(CInt(points2f(i).X * 10 + w / 2), CInt(points2f(i).Y * 10 + h / 2)))
            Next
            pts.Add(points)

            dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
            dst1.Polylines(pts, False, cv.Scalar.White, 1, task.lineType)
            dst1 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End If

        Dim width As Double = dst1.Width / zoomFactor
        Dim height As Double = dst1.Height / zoomFactor
        Dim x = Math.Min(task.mousePoint.X, dst1.Width - width)
        Dim y = Math.Min(task.mousePoint.Y, dst1.Height - height)
        dst2 = dst1.GetRectSubPix(New cv.Size(width, height), New cv.Point2f(x, y)).Resize(dst2.Size)
    End Sub
End Class


