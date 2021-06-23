Imports cv = OpenCvSharp
' https://www.codeproject.com/Articles/1093960/D-Polyline-Vertex-Smoothing
Public Class Smoothing_Exterior : Inherits VBparent
	Dim hull As New Hull_Basics
	Public inputPoints As List(Of cv.Point)
	Public smoothPoints As List(Of cv.Point)
	Public plotColor = cv.Scalar.Yellow
	Dim smOptions As New Smoothing_Options
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
		labels(2) = "Original Points (white) Smoothed (yellow)"
		labels(3) = ""
		task.desc = "Smoothing the line connecting a series of points."
	End Sub
	Public Sub Run(src As cv.Mat) ' Rank = 1
		smOptions.Run(Nothing)
		If standalone Or task.intermediateName = caller Then
			If task.frameCount Mod 30 Then Exit Sub
			dst2.SetTo(0)
			hull.Run(src)
			Dim nextHull = hull.hull
			inputPoints = drawPoly(dst2, nextHull, cv.Scalar.White)
		Else
			dst2.SetTo(0)
		End If
		If inputPoints.Count > 1 Then
			smoothPoints = getSplineInterpolationCatmullRom(inputPoints, smOptions.iterations)
			drawPoly(dst2, smoothPoints.ToArray, plotColor)
		End If
	End Sub
End Class





' https://www.codeproject.com/Articles/1093960/D-Polyline-Vertex-Smoothing
Public Class Smoothing_Interior : Inherits VBparent
	Dim hull As New Hull_Basics
	Public inputPoints As List(Of cv.Point)
	Public smoothPoints As List(Of cv.Point)
	Public plotColor = cv.Scalar.Yellow
	Dim smOptions As New Smoothing_Options
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
		If standalone Then findSlider("Hull random points").Value = 16

		labels(2) = "Original Points (white) Smoothed (yellow)"
		labels(3) = ""
		task.desc = "Smoothing the line connecting a series of points staying inside the outline."
	End Sub
	Public Sub Run(src As cv.Mat) ' Rank = 1
		smOptions.Run(Nothing)
		If standalone Or task.intermediateName = caller Then
			If task.frameCount Mod 30 Then Exit Sub
			dst2.SetTo(0)
			hull.Run(src)
			Dim nextHull = hull.hull
			inputPoints = drawPoly(dst2, nextHull, cv.Scalar.White)
		Else
			dst2.SetTo(0)
		End If
		Dim smoothPoints2d = getCurveSmoothingChaikin(inputPoints, smOptions.interiorTension, smOptions.iterations)
		smoothPoints = New List(Of cv.Point)
		For i = 0 To smoothPoints2d.Count - 1 Step smOptions.stepSize
			smoothPoints.Add(New cv.Point(CInt(smoothPoints2d.ElementAt(i).X), CInt(smoothPoints2d.ElementAt(i).Y)))
		Next
		If smoothPoints.Count > 0 Then drawPoly(dst2, smoothPoints.ToArray, plotColor)
	End Sub
End Class







Public Class Smoothing_Options : Inherits VBparent
	Public iterations As Integer
	Public interiorTension As Single
	Public stepSize As Integer
	Public Sub New()
		If sliders.Setup(caller) Then
			sliders.setupTrackBar(0, "Smoothing iterations", 1, 20, 8)
			sliders.setupTrackBar(1, "Smoothing tension X100 (Interior Only)", 1, 100, 50)
			sliders.setupTrackBar(2, "Step size when adding points (1 is identity)", 1, 500, 30)
		End If

		labels(2) = "No output - just options for smoothing..."
		task.desc = "Options for smoothing operations."
	End Sub
	Public Sub Run(src As cv.Mat) ' Rank = 1
		Static iterSlider = findSlider("Smoothing iterations")
		Static tensionSlider = findSlider("Smoothing tension X100 (Interior Only)")
		Static stepSlider = findSlider("Step size when adding points (1 is identity)")
		iterations = iterSlider.value
		interiorTension = tensionSlider.value / 100
		stepSize = stepSlider.value
	End Sub
End Class





Public Class Smoothing_Contours : Inherits VBparent
	Dim outline As New Blob_Largest
	Dim smoothE As New Smoothing_Exterior
	Dim smoothI As New Smoothing_Interior
	Dim smOptions As New Smoothing_Options
	Public Sub New()
		smoothE.plotColor = cv.Scalar.Blue
		smoothI.plotColor = cv.Scalar.Blue

		If radio.Setup(caller, 2) Then
			radio.check(0).Text = "Interior smoothing"
			radio.check(1).Text = "Exterior smoothing"
			radio.check(1).Checked = True
		End If

		task.desc = "Use Smoothing exterior or interior to get a smoother representation of a contour"
	End Sub
	Public Sub Run(src As cv.Mat) ' Rank = 1
		smOptions.Run(Nothing)

		outline.Run(src)
		dst2 = outline.dst2.Clone
		dst3 = outline.dst2

		Dim smooth = If(radio.check(0).Checked, smoothI, smoothE)
		Dim contours0 = cv.Cv2.FindContoursAsArray(outline.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY), cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
		Dim maxIndex As Integer
		Dim maxNodes As Integer
		For i = 0 To contours0.Length - 1
			Dim c = cv.Cv2.ApproxPolyDP(contours0(i), 3, True)
			If maxNodes < c.Length Then
				maxIndex = i
				maxNodes = c.Length
			End If
		Next
		cv.Cv2.DrawContours(dst3, contours0, maxIndex, New cv.Scalar(0, 255, 255), task.lineWidth)

		smooth.inputPoints = New List(Of cv.Point)
		For i = 0 To contours0(maxIndex).Count - 1 Step smOptions.stepSize
			smooth.inputPoints.Add(contours0(maxIndex)(i))
		Next

		smooth.Run(src)
		If smooth.smoothpoints IsNot Nothing Then
			If smooth.smoothPoints.Count > 0 Then drawPoly(dst2, smooth.smoothPoints.ToArray, smooth.plotColor)
			labels(2) = "Smoothing with " + If(radio.check(0).Checked, "Interior", "Exterior") + " lines"
			labels(3) = "Found " + CStr(contours0.Count) + " countours in the largest blob"
		End If
	End Sub
End Class

