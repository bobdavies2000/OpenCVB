Imports cv = OpenCvSharp

Module Hull_module
    Public Function drawPoly(result As cv.Mat, polyPoints() As cv.Point, color As cv.Scalar) As List(Of cv.Point)
        Dim listOfPoints = New List(Of List(Of cv.Point))
        Dim points = New List(Of cv.Point)
        For j = 0 To polyPoints.Count - 1
            points.Add(New cv.Point(polyPoints(j).X, polyPoints(j).Y))
        Next
        listOfPoints.Add(points)
        cv.Cv2.DrawContours(result, listOfPoints, 0, color, 2)

        For i = 0 To polyPoints.Count - 1
            result.Circle(polyPoints(i), 5, color, -1, task.lineType)
        Next

        Return points
    End Function
End Module






Public Class Hull_Basics
    Inherits VBparent
    Public hull() As cv.Point
        Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Hull random points", 1, 20, 10)
        End If

        task.desc = "Surround a set of random points with a convex hull"
		' task.rank = 1
        label1 = "Convex Hull Output"
        label2 = "Convex Hull Input"
    End Sub
    Public Sub Run(src as cv.Mat)
        Dim Count = sliders.trackbar(0).Value
        Dim points(Count - 1) As cv.Point
        Dim pad = 4
        Dim w = src.Width - src.Width / pad
        Dim h = src.Height - src.Height / pad
        For i = 0 To Count - 1
            points(i) = New cv.Point2f(msRNG.Next(src.Width / pad, w), msRNG.Next(src.Height / pad, h))
        Next
        hull = cv.Cv2.ConvexHull(points, True)

        If standalone or task.intermediateReview = caller Then
            dst1.SetTo(0)
            dst2.SetTo(0)
            For i = 0 To hull.Count - 1
                cv.Cv2.Line(dst1, hull.ElementAt(i), hull.ElementAt((i + 1) Mod hull.Count), cv.Scalar.White, 2)
                cv.Cv2.Line(dst2, hull.ElementAt(i), hull.ElementAt((i + 1) Mod hull.Count), cv.Scalar.White, 2)
            Next

            Dim pMat As New cv.Mat(hull.Count, 1, cv.MatType.CV_32SC2, hull)
            Dim sum = pMat.Sum()
            Dim center = New cv.Point(CInt(sum.Val0 / hull.Count), CInt(sum.Val1 / hull.Count))
            Dim pixels = dst1.FloodFill(center, cv.Scalar.Yellow) ' because the shape is convex, we know the center is in the intere
            dst1.Circle(center, 10, cv.Scalar.Red, -1, task.lineType)

            For i = 0 To Count - 1
                dst1.Circle(points(i), 3, cv.Scalar.Yellow, -1, task.lineType)
                dst2.Circle(points(i), 3, cv.Scalar.Yellow, -1, task.lineType)
            Next
        End If
    End Sub
End Class



