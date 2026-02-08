Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class PolyLine_Basics : Inherits TaskParent
        Dim edgeline As New EdgeLine_Basics
        Public Sub New()
            desc = "Hook to Polyline usage in EdgeLine_Motion"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edgeline.Run(tsk.grayStable)
            dst2 = edgeline.dst2
            labels(2) = edgeline.labels(2)

            If standaloneTest() Then dst3 = PaletteBlackZero(dst2)
        End Sub
    End Class






    Public Class NR_PolyLine_IEnumerableExample : Inherits TaskParent
        Dim options As New Options_PolyLines
        Public Sub New()
            desc = "Manually create an ienumerable(of ienumerable(of cv.point))."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim points = Enumerable.Range(0, options.polyCount).Select(Of cv.Point)(
            Function(i)
                Return New cv.Point(CInt(msRNG.Next(0, src.Width)), CInt(msRNG.Next(0, src.Height)))
            End Function).ToList
            Dim pts As New List(Of List(Of cv.Point))
            pts.Add(points)

            dst2 = New cv.Mat(src.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            ' NOTE: when there are 2 points, there will be 1 line.
            dst2.Polylines(pts, options.polyClosed, white, tsk.lineWidth, tsk.lineType)
        End Sub
    End Class





    ' VB.Net implementation of the browse example in Opencvb.
    ' https://github.com/opencv/opencv/blob/master/samples/python/browse.py
    Public Class NR_PolyLine_Random : Inherits TaskParent
        Dim zoom As New Pixel_Zoom
        Public Sub New()
            labels(2) = "To zoom move the mouse over the image"
            desc = "Create a random procedural image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If tsk.frameCount Mod (tsk.fpsAlgorithm * 3) = 0 Then ' every x frames.
                Dim h = src.Height, w = src.Width
                Dim autorand As New Random
                Dim points2f(10000) As cv.Point2f
                Dim pts As New List(Of List(Of cv.Point))
                Dim points As New List(Of cv.Point)
                points2f(0) = New cv.Point2f(autorand.NextDouble() - 0.5, autorand.NextDouble() - 0.5)
                For i = 1 To points2f.Count - 1
                    points2f(i) = New cv.Point2f(autorand.NextDouble() - 0.5 + points2f(i - 1).X, autorand.NextDouble() - 0.5 + points2f(i - 1).Y)
                    points.Add(New cv.Point(points2f(i).X * 10 + w \ 2, points2f(i).Y * 10 + h \ 2))
                Next
                pts.Add(points)

                dst2 = New cv.Mat(src.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
                dst2.Polylines(pts, False, white, tsk.lineWidth, tsk.lineType)
                dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            End If

            zoom.Run(dst2)
            dst3 = zoom.dst2
        End Sub
    End Class
End Namespace