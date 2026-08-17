Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
' https://www.codeproject.com/Articles/1093960/D-Polyline-Vertex-Smoothing
Namespace VBClasses
    Public Class Smoothing_Interior : Inherits TaskParent
        Public inputPoints As List(Of cv.Point)
        Public smoothPoints As List(Of cv.Point)
        Public plotColor = Scalar.Yellow
        Dim Options As New Options_Smoothing
        Public Sub New()
            labels(2) = "Original Points (white) Smoothed (yellow)"
            labels(3) = ""
            desc = "Smoothing the line connecting a series of points."
        End Sub
        Public Shared Function getSplineInterpolationCatmullRom(ByVal points As List(Of cv.Point), nrOfInterpolatedPoints As Integer) As List(Of cv.Point)
            Dim spline As New List(Of cv.Point)

            Dim startPt = (points(1) + points(0)) * 0.5
            points.Insert(0, startPt)
            Dim endPt = (points.Last + points(points.Count - 2)) * 0.5
            points.Insert(points.Count, endPt)

            ' Note the nrOfInterpolatedPoints acts as a kind of tension factor between 0 and 1 because it is normalised
            ' to 1/nrOfInterpolatedPoints. It can never be 0
            Dim t As Double
            Dim spoint As cv.Point
            For i = 0 To points.Count - 4
                spoint = New cv.Point()
                For j = 0 To nrOfInterpolatedPoints - 1
                    Dim x0 = points.ElementAt((i) Mod points.Count)
                    Dim x1 = points.ElementAt((i + 1) Mod points.Count)
                    Dim x2 = points.ElementAt((i + 2) Mod points.Count)
                    Dim x3 = points.ElementAt((i + 3) Mod points.Count)
                    t = 1 / nrOfInterpolatedPoints * j
                    spoint.X = 0.5 * (2 * x1.X + (-1 * x0.X + x2.X) * t + (2 * x0.X - 5 * x1.X + 4 * x2.X - x3.X) * t ^ 2 +
                                       (-1 * x0.X + 3 * x1.X - 3 * x2.X + x3.X) * t ^ 3)
                    spoint.Y = 0.5 * (2 * x1.Y + (-1 * x0.Y + x2.Y) * t + (2 * x0.Y - 5 * x1.Y + 4 * x2.Y - x3.Y) * t ^ 2 +
                                       (-1 * x0.Y + 3 * x1.Y - 3 * x2.Y + x3.Y) * t ^ 3)
                    spline.Add(spoint)
                Next
            Next

            'add the last cv.Point, but skip the interpolated last cv.Point, so second last...
            spline.Add(points(points.Count - 2))
            Return spline
        End Function
        Public Shared Sub DrawPoly(result As Mat, polyPoints As List(Of cv.Point), color As Scalar)
            If polyPoints.Count < 3 Then Exit Sub
            Dim listOfPoints = New List(Of List(Of cv.Point))({polyPoints})
            DrawContours(result, listOfPoints, 0, color, 2)
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Options.Run()

            If standaloneTest() Then
                Static hull As New Convex_Basics
                If task.heartBeat Then
                    Dim hullList = hull.buildRandomHullPoints()
                    dst2.SetTo(0)
                    hull.Run(src)
                    Dim nextHull = ConvexHull(hullList.ToArray, True)
                    inputPoints = nextHull.ToList
                    DrawPoly(dst2, inputPoints, white)
                Else
                    Exit Sub
                End If
            Else
                dst2.SetTo(0)
            End If

            If inputPoints.Count > 1 Then
                smoothPoints = getSplineInterpolationCatmullRom(inputPoints, Options.iterations)
                DrawPoly(dst2, smoothPoints, plotColor)
            End If
        End Sub
    End Class





    ' https://www.codeproject.com/Articles/1093960/D-Polyline-Vertex-Smoothing
    Public Class Smoothing_Exterior : Inherits TaskParent
        Dim hull As New Convex_Basics
        Public inputPoints As List(Of cv.Point)
        Public smoothPoints As List(Of cv.Point)
        Public plotColor = Scalar.Yellow
        Dim options As New Options_Smoothing
        Public Shared Function getCurveSmoothingChaikin(points As List(Of cv.Point), tension As Double, nrOfIterations As Integer) As List(Of Point2d)
            'the tension factor defines a scale between corner cutting distance in segment half length, i.e. between 0.05 and 0.45
            'the opposite corner will be cut by the inverse (i.e. 1-cutting distance) to keep symmetry
            'with a tension value of 0.5 this amounts to 0.25 = 1/4 and 0.75 = 3/4 the original Chaikin values
            Dim cutdist As Double = 0.05 + (tension * 0.4)

            'make a copy of the pointlist and feed it to the iteration
            Dim nl As New List(Of Point2d)
            For i = 0 To points.Count - 1
                nl.Add(New Point2d(CDbl(points.ElementAt(i).X), CDbl(points.ElementAt(i).Y)))
            Next

            For i = 1 To nrOfIterations
                If nl.Count > 0 Then nl = getSmootherChaikin(nl, cutdist)
            Next

            Return nl
        End Function
        Private Shared Function getSmootherChaikin(points As List(Of Point2d), cuttingDist As Double) As List(Of Point2d)
            'always add the first cv.Point
            Dim nl As New List(Of Point2d)({points(0)})

            For i = 0 To points.Count - 2
                Dim pt1 = New Point2d((1 - cuttingDist) * points.ElementAt(i).X, (1 - cuttingDist) * points.ElementAt(i).Y)
                Dim pt2 = New Point2d(cuttingDist * points.ElementAt(i + 1).X, cuttingDist * points.ElementAt(i + 1).Y)
                nl.Add(pt1 + pt2)
                pt1 = New Point2d(cuttingDist * points.ElementAt(i).X, cuttingDist * points.ElementAt(i).Y)
                pt2 = New Point2d((1 - cuttingDist) * points.ElementAt(i + 1).X, (1 - cuttingDist) * points.ElementAt(i + 1).Y)
                nl.Add(pt1 + pt2)
            Next

            'always add the last cv.Point
            nl.Add(points(points.Count - 1))
            Return nl
        End Function
        Public Sub New()
            If standalone Then OptionParent.FindSlider("Hull random points").Value = 16

            labels(2) = "Original Points (white) Smoothed (yellow)"
            labels(3) = ""
            desc = "Smoothing the line connecting a series of points staying inside the outline."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            If standaloneTest() Then
                If task.heartBeat Then
                    Dim hullList = hull.buildRandomHullPoints()
                    dst2.SetTo(0)
                    hull.Run(src)
                    Dim nextHull = ConvexHull(hullList.ToArray, True)
                    inputPoints = nextHull.ToList
                    Smoothing_Interior.DrawPoly(dst2, nextHull.ToList, white)
                Else
                    Exit Sub
                End If
            Else
                dst2.SetTo(0)
            End If
            Dim smoothPoints2d = getCurveSmoothingChaikin(inputPoints, options.interiorTension, options.iterations)
            smoothPoints = New List(Of cv.Point)
            For i = 0 To smoothPoints2d.Count - 1 Step options.stepSize
                smoothPoints.Add(New cv.Point(CInt(smoothPoints2d.ElementAt(i).X), CInt(smoothPoints2d.ElementAt(i).Y)))
            Next
            If smoothPoints.Count > 0 Then Smoothing_Interior.DrawPoly(dst2, smoothPoints, plotColor)
        End Sub
    End Class
End Namespace