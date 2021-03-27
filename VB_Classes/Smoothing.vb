Imports cv = OpenCvSharp
' https://www.codeproject.com/Articles/1093960/D-Polyline-Vertex-Smoothing
Public Class Smoothing_Exterior
	Inherits VBparent
	Dim hull As Hull_Basics
	Public inputPoints As List(Of cv.Point)
	Public smoothPoints As List(Of cv.Point)
	Public plotColor = cv.Scalar.Yellow
	Private Function getSplineInterpolationCatmullRom(points As List(Of cv.Point), nrOfInterpolatedPoints As Integer) As List(Of cv.Point)
		Dim spline As New List(Of cv.Point)
		' Create a new pointlist to spline.  If you don't do this, the original pointlist is included with the extrapolated points
		Dim spoints As New List(Of cv.Point)
		spoints = points

		Dim startPt = (spoints(1) + spoints(0)) * 0.5
		spoints.Insert(0, startPt)
		Dim endPt = (spoints(spoints.Count - 1) + spoints(spoints.Count - 2)) * 0.5
		spoints.Insert(spoints.Count, endPt)

		' Note the nrOfInterpolatedPoints acts as a kind of tension factor between 0 and 1 because it is normalised
		' to 1/nrOfInterpolatedPoints. It can never be 0
		Dim t As Double = 0
		Dim spoint As cv.Point
		For i = 0 To spoints.Count - 4
			spoint = New cv.Point()
			For j = 0 To nrOfInterpolatedPoints - 1
				Dim x0 = spoints.ElementAt((i) Mod spoints.Count)
				Dim x1 = spoints.ElementAt((i + 1) Mod spoints.Count)
				Dim x2 = spoints.ElementAt((i + 2) Mod spoints.Count)
				Dim x3 = spoints.ElementAt((i + 3) Mod spoints.Count)
				t = 1 / nrOfInterpolatedPoints * j
				spoint.X = 0.5 * (2 * x1.X + (-1 * x0.X + x2.X) * t + (2 * x0.X - 5 * x1.X + 4 * x2.X - x3.X) * t ^ 2 +
						   (-1 * x0.X + 3 * x1.X - 3 * x2.X + x3.X) * t ^ 3)
				spoint.Y = 0.5 * (2 * x1.Y + (-1 * x0.Y + x2.Y) * t + (2 * x0.Y - 5 * x1.Y + 4 * x2.Y - x3.Y) * t ^ 2 +
						   (-1 * x0.Y + 3 * x1.Y - 3 * x2.Y + x3.Y) * t ^ 3)
				spline.Add(spoint)
			Next
		Next

		'add the last point, but skip the interpolated last point, so second last...
		spline.Add(spoints(spoints.Count - 2))
		Return spline
	End Function
	Public Sub New()
		initParent()
		hull = New Hull_Basics()
		hull.sliders.trackbar(0).Minimum = 4 ' required minimum number of points for the algorithm.

		If findfrm(caller + " Slider Options") Is Nothing Then
			sliders.Setup(caller)
			sliders.setupTrackBar(0, "Smoothing iterations", 1, 20, 10)
		End If
		label1 = "Original Points (white) Smoothed (yellow)"
		label2 = ""
		task.desc = "Smoothing the line connecting a series of points."
	End Sub
	Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
		If standalone Or task.intermediateReview = caller Then
			If task.frameCount Mod 30 Then Exit Sub

			hull.src = src
			hull.Run()
			Dim nextHull = hull.hull

			dst1.SetTo(0)
			inputPoints = drawPoly(dst1, nextHull, cv.Scalar.White)
		End If
		If inputPoints.Count > 1 Then
			smoothPoints = getSplineInterpolationCatmullRom(inputPoints, sliders.trackbar(0).Value)
			drawPoly(dst1, smoothPoints.ToArray, plotColor)
		End If
	End Sub
End Class





' https://www.codeproject.com/Articles/1093960/D-Polyline-Vertex-Smoothing
Public Class Smoothing_Interior
	Inherits VBparent
	Dim hull As Hull_Basics
	Public inputPoints As List(Of cv.Point)
	Public smoothPoints As List(Of cv.Point)
	Public plotColor = cv.Scalar.Yellow
	Private Function getCurveSmoothingChaikin(points As List(Of cv.Point), tension As Double, nrOfIterations As Integer) As List(Of cv.Point2d)
		'the tension factor defines a scale between corner cutting distance in segment half length, i.e. between 0.05 and 0.45
		'the opposite corner will be cut by the inverse (i.e. 1-cutting distance) to keep symmetry
		'with a tension value of 0.5 this amounts to 0.25 = 1/4 and 0.75 = 3/4 the original Chaikin values
		Dim cutdist As Double = 0.05 + (tension * 0.4)

		'make a copy of the pointlist and feed it to the iteration
		Dim nl As New List(Of cv.Point2d)
		For i = 0 To points.Count - 1
			nl.Add(New cv.Point2d(CDbl(points.ElementAt(i).X), CDbl(points.ElementAt(i).Y)))
		Next

		For i = 1 To nrOfIterations
			nl = getSmootherChaikin(nl, cutdist)
		Next

		Return nl
	End Function

	Private Function getSmootherChaikin(points As List(Of cv.Point2d), cuttingDist As Double) As List(Of cv.Point2d)
		Dim nl As New List(Of cv.Point2d)
		'always add the first point
		nl.Add(points(0))

		For i = 0 To points.Count - 2
			Dim pt1 = New cv.Point2d((1 - cuttingDist) * points.ElementAt(i).X, (1 - cuttingDist) * points.ElementAt(i).Y)
			Dim pt2 = New cv.Point2d(cuttingDist * points.ElementAt(i + 1).X, cuttingDist * points.ElementAt(i + 1).Y)
			nl.Add(pt1 + pt2)
			pt1 = New cv.Point2d(cuttingDist * points.ElementAt(i).X, cuttingDist * points.ElementAt(i).Y)
			pt2 = New cv.Point2d((1 - cuttingDist) * points.ElementAt(i + 1).X, (1 - cuttingDist) * points.ElementAt(i + 1).Y)
			nl.Add(pt1 + pt2)
		Next

		'always add the last point
		nl.Add(points(points.Count - 1))
		Return nl
	End Function

	Public Sub New()
		initParent()
		hull = New Hull_Basics()

		Dim hullSlider = findSlider("Hull random points")
		hullSlider.Minimum = 4 ' required minimum number of points for the algorithm.
		hullSlider.Value = 16

		If findfrm(caller + " Slider Options") Is Nothing Then
			sliders.Setup(caller)
			sliders.setupTrackBar(0, "Smoothing iterations", 1, 20, 1)
			sliders.setupTrackBar(1, "Smoothing tension X100", 1, 100, 50)
		End If
		label1 = "Original Points (white) Smoothed (yellow)"
		label2 = ""
		task.desc = "Smoothing the line connecting a series of points staying inside the outline."
	End Sub
	Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
		If standalone or task.intermediateReview = caller Then
			If task.frameCount Mod 30 Then Exit Sub

			hull.src = src
			hull.Run()
			Dim nextHull = hull.hull

			dst1.SetTo(0)
			inputPoints = drawPoly(dst1, nextHull, cv.Scalar.White)
		End If
		Dim smoothPoints2d = getCurveSmoothingChaikin(inputPoints, sliders.trackbar(1).Value / 100, sliders.trackbar(0).Value)
		Dim smoothPoints As New List(Of cv.Point)
		For i = 0 To smoothPoints2d.Count - 1
			smoothPoints.Add(New cv.Point(CInt(smoothPoints2d.ElementAt(i).X), CInt(smoothPoints2d.ElementAt(i).Y)))
		Next
		If smoothPoints.Count > 0 Then drawPoly(dst1, smoothPoints.ToArray, plotColor)
	End Sub
End Class






Public Class Smoothing_Contours
	Inherits VBparent
	Dim outline As Contours_Depth
	Dim smoothE As Smoothing_Exterior
	Dim smoothI As Smoothing_Interior
	Public Sub New()
		initParent()
		outline = New Contours_Depth()
		smoothE = New Smoothing_Exterior()
		smoothI = New Smoothing_Interior()
		smoothE.plotColor = cv.Scalar.Blue
		smoothI.plotColor = cv.Scalar.Blue

		If findfrm(caller + " Slider Options") Is Nothing Then
			sliders.Setup(caller)
			sliders.setupTrackBar(0, "Step size when adding points (1 is identity)", 1, 500, 30)
		End If
		If findfrm(caller + " Radio Options") Is Nothing Then
			radio.Setup(caller, 2)
			radio.check(0).Text = "Interior smoothing"
			radio.check(1).Text = "Exterior smoothing"
			radio.check(1).Checked = True
		End If

		task.desc = "Use Smoothing exterior or interior to get a smoother representation of a contour"
	End Sub
	Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
		outline.Run()

		Dim stepsize = sliders.trackbar(0).Value
		Dim smooth As Object
		If radio.check(0).Checked Then smooth = smoothI Else smooth = smoothE
		smooth.inputPoints = New List(Of cv.Point)
		For i = 0 To outline.contours.Count - 1 Step stepsize
			smooth.inputPoints.Add(New cv.Point2f(outline.contours(i).X, outline.contours(i).Y))
		Next
		smooth.dst1 = outline.dst2
		smooth.Run()
		dst1 = smooth.dst1
		If standalone Then dst2.SetTo(0)
		label1 = "Smoothing with " + If(radio.check(0).Checked, "Interior", "Exterior") + " lines"
	End Sub
End Class
