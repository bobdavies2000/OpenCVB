Imports cv = OpenCvSharp
Imports System.Drawing
Module Draw_Exports
    Dim rng As System.Random
    Public Sub drawRotatedRectangle(rotatedRect As cv.RotatedRect, dst1 As cv.Mat, color As cv.Scalar)
        Dim vertices2f = rotatedRect.Points()
        Dim vertices(vertices2f.Length - 1) As cv.Point
        For j = 0 To vertices2f.Length - 1
            vertices(j) = New cv.Point(CInt(vertices2f(j).X), CInt(vertices2f(j).Y))
        Next
        cv.Cv2.FillConvexPoly(dst1, vertices, color, task.lineType)
    End Sub
    Public Sub drawRotatedOutline(rr As cv.RotatedRect, dst As cv.Mat, color As cv.Scalar)
        Dim vertices = rr.Points()
        For i = 0 To 4 - 1
            dst.Line(New cv.Point(vertices(i).X, vertices(i).Y), New cv.Point(vertices((i + 1) Mod 4).X, vertices((i + 1) Mod 4).Y),
                     color, 1, task.lineType)
        Next
        dst.Rectangle(rr.BoundingRect, color, 1, task.lineType)
    End Sub
    Public Function initRandomRect(width As Integer, height As Integer, margin As Integer) As cv.Rect
        Dim x As Integer, y As Integer, w As Integer, h As Integer
        While 1
            x = (width - margin * 2) * Rnd() + margin
            w = (width - x - margin * 2) * Rnd()
            If w > 5 Then Exit While  ' don't let the width/height get too small...
        End While

        While 1
            y = (height - margin * 2) * Rnd() + margin
            h = (height - y - margin * 2) * Rnd()
            If h > 5 Then Exit While  ' don't let the width/height get too small...
        End While

        Return New cv.Rect(x, y, w, h)
    End Function
End Module






Public Class Draw_Noise : Inherits VBparent
    Public maxNoiseWidth As Integer = 3
    Public addRandomColor As Boolean
    Public noiseMask As cv.Mat
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Noise Count", 1, 1000, 100)
            sliders.setupTrackBar(1, "Noise Width", 1, 10, 3)
        End If
        task.desc = "Add Noise to the color image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static widthSlider = findSlider("Noise Width")
        Static CountSlider = findSlider("Noise Count")
        maxNoiseWidth = widthSlider.Value
        src.CopyTo(dst1)
        noiseMask = New cv.Mat(src.Size(), cv.MatType.CV_8UC1).SetTo(0)
        Dim count = CountSlider.value
        For n = 0 To count - 1
            Dim i = msRNG.Next(0, src.Cols - 1)
            Dim j = msRNG.Next(0, src.Rows - 1)
            Dim center = New cv.Point2f(i, j)
            Dim c = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
            If addRandomColor = False Then c = cv.Scalar.Black
            Dim noiseWidth = msRNG.Next(1, maxNoiseWidth)
            dst1.Circle(center, noiseWidth, c, -1, task.lineType)
            noiseMask.Circle(center, noiseWidth, cv.Scalar.White, -1, task.lineType)
        Next
    End Sub
End Class






Public Class Draw_Options : Inherits VBparent
    Public drawCount As Integer
    Public updateFrequency As Integer
    Public drawFilled As Integer
    Public drawRotated As Boolean
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "DrawCount", 0, 20, 3)
            sliders.setupTrackBar(1, "Update Frequency", 1, 50, 1)
        End If

        If check.Setup(caller, 2) Then
            check.Box(0).Text = "Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)"
            check.Box(1).Text = "Draw filled (unchecked draw an outline)"
        End If

        task.desc = "Show the options for the draw algorithms"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static countSlider = findSlider("DrawCount")
        Static freqSlider = findSlider("Update Frequency")
        Static fillCheck = findCheckBox("Draw filled (unchecked draw an outline)")
        Static rotateCheck = findCheckBox("Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)")
        drawCount = countSlider.value
        updateFrequency = freqSlider.value
        drawFilled = If(fillCheck.checked, -1, 2)
        drawRotated = rotateCheck.checked
        If standalone Then task.trueText("This algorithm is just to consolidate the options for the Draw algorithms.")
    End Sub
End Class






Public Class Draw_Ellipses : Inherits VBparent
    Dim optDraw As New Draw_Options
    Public Sub New()
        task.desc = "Draw the requested number of ellipses."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        optDraw.Run(Nothing)
        If task.frameCount Mod optDraw.updateFrequency = 0 Then
            dst1.SetTo(cv.Scalar.Black)
            For i = 0 To optDraw.drawCount - 1
                Dim nPoint = New cv.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
                Dim eSize = New cv.Size2f(CSng(msRNG.Next(0, src.Cols - nPoint.X - 1)), CSng(msRNG.Next(0, src.Rows - nPoint.Y - 1)))
                Dim angle = 180.0F * CSng(msRNG.Next(0, 1000) / 1000.0F)
                Dim nextColor = New cv.Scalar(task.vecColors(i).Item0, task.vecColors(i).Item1, task.vecColors(i).Item2)
                dst1.Ellipse(New cv.RotatedRect(nPoint, eSize, angle), nextColor, optDraw.drawFilled)
            Next
        End If
    End Sub
End Class





Public Class Draw_Circles : Inherits VBparent
    Dim optDraw As New Draw_Options
    Public Sub New()
        task.desc = "Draw the requested number of circles."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        optDraw.Run(Nothing)
        If task.frameCount Mod optDraw.updateFrequency = 0 Then
            dst1.SetTo(cv.Scalar.Black)
            For i = 0 To optDraw.drawCount - 1
                Dim nPoint = New cv.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
                Dim radius = msRNG.Next(10, 10 + msRNG.Next(src.Cols / 4))
                Dim nextColor = New cv.Scalar(task.vecColors(i).Item0, task.vecColors(i).Item1, task.vecColors(i).Item2)
                dst1.Circle(nPoint, radius, nextColor, optDraw.drawFilled, task.lineType)
            Next
        End If
    End Sub
End Class








Public Class Draw_Line : Inherits VBparent
    Dim optDraw As New Draw_Options
    Public Sub New()
        task.desc = "Draw the requested number of Lines."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        optDraw.Run(Nothing)
        If task.frameCount Mod optDraw.updateFrequency Then Exit Sub
        dst1.SetTo(cv.Scalar.Black)
        For i = 0 To optDraw.drawCount - 1
            Dim nPoint1 = New cv.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
            Dim nPoint2 = New cv.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
            Dim nextColor = New cv.Scalar(task.vecColors(i).Item0, task.vecColors(i).Item1, task.vecColors(i).Item2)
            dst1.Line(nPoint1, nPoint2, nextColor, optDraw.drawFilled, task.lineType)
        Next
    End Sub
End Class






Public Class Draw_Polygon : Inherits VBparent
    Dim optDraw As New Draw_Options
    Public Sub New()
        task.desc = "Draw Polygon figures"
        label2 = "Convex Hull for the same polygon"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        optDraw.Run(Nothing)

        If task.frameCount Mod optDraw.updateFrequency Then Exit Sub
        Dim height = src.Height / 8
        Dim width = src.Width / 8
        Dim polyColor = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
        dst1.SetTo(cv.Scalar.Black)
        dst2 = dst1.Clone()
        For i = 0 To optDraw.drawCount - 1
            Dim points = New List(Of cv.Point)
            Dim listOfPoints = New List(Of List(Of cv.Point))
            For j = 0 To 10
                points.Add(New cv.Point(CInt(msRNG.Next(width, width * 7)), CInt(msRNG.Next(height, height * 7))))
            Next
            listOfPoints.Add(points)
            If optDraw.drawFilled <> -1 Then
                cv.Cv2.Polylines(dst1, listOfPoints, True, polyColor, 2, task.lineType)
            Else
                dst1.FillPoly(listOfPoints, New cv.Scalar(0, 0, 255))
            End If

            Dim hull() As cv.Point
            hull = cv.Cv2.ConvexHull(points, True)
            listOfPoints = New List(Of List(Of cv.Point))
            points = New List(Of cv.Point)
            For j = 0 To hull.Count - 1
                points.Add(New cv.Point(hull(j).X, hull(j).Y))
            Next
            listOfPoints.Add(points)
            dst2.SetTo(cv.Scalar.Black)
            cv.Cv2.DrawContours(dst2, listOfPoints, 0, polyColor, optDraw.drawFilled)
        Next
    End Sub
End Class





' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
Public Class Draw_Shapes : Inherits VBparent
    Public Sub New()
        task.desc = "Use RNG to draw the same set of shapes every time"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim offsetX = 50, offsetY = 25, lineLength = 50, thickness = 2

        dst1.SetTo(0)
        For i = 1 To 256
            dst1.Line(New cv.Point(thickness * i + offsetX, offsetY), New cv.Point(thickness * i + offsetX, offsetY + lineLength), New cv.Scalar(i, i, i), thickness)
        Next
        For i = 1 To 256
            Dim color = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
            Select Case msRNG.Next(0, 3)
                Case 0 ' circle
                    Dim center = New cv.Point(msRNG.Next(offsetX, dst1.Cols - offsetX), msRNG.Next(offsetY + lineLength, dst1.Rows - offsetY))
                    Dim radius = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    dst1.Circle(center, radius, color, -1, cv.LineTypes.Link8)
                Case 1 ' Rectangle
                    Dim center = New cv.Point(msRNG.Next(offsetX, dst1.Cols - offsetX), msRNG.Next(offsetY + lineLength, dst1.Rows - offsetY))
                    Dim width = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    Dim height = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    Dim rc = New cv.Rect(center.X - width, center.Y - height / 2, width, height)
                    dst1.Rectangle(rc, color, -1, cv.LineTypes.Link8)
                Case 2 ' Ellipse
                    Dim center = New cv.Point(msRNG.Next(offsetX, dst1.Cols - offsetX), msRNG.Next(offsetY + lineLength, dst1.Rows - offsetY))
                    Dim width = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    Dim height = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    Dim angle = msRNG.Next(0, 180)
                    dst1.Ellipse(center, New cv.Size(width / 2, height / 2), angle, 0, 360, color, -1, cv.LineTypes.Link8)
            End Select
        Next
    End Sub
End Class





Public Class Draw_SymmetricalShapes : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Number of points", 200, 1000, 500)
            sliders.setupTrackBar(1, "Radius 1", 1, dst1.Rows / 2, dst1.Rows / 4)
            sliders.setupTrackBar(2, "Radius 2", 1, dst1.Rows / 2, dst1.Rows / 8)
            sliders.setupTrackBar(3, "nGenPer", 1, 500, 100)
        End If
        If check.Setup(caller, 5) Then
            check.Box(0).Text = "Symmetric Ripple"
            check.Box(1).Text = "Only Regular Shapes"
            check.Box(2).Text = "Filled Shapes"
            check.Box(3).Text = "Reverse In/Out"
            check.Box(4).Text = "Use demo mode"
            check.Box(4).Checked = True
        End If
        task.desc = "Generate shapes programmatically"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static rotateAngle As Single = 0
        Static fillColor = cv.Scalar.Red
        If check.Box(4).Checked Then
            If task.frameCount Mod 30 = 0 Then
                If sliders.trackbar(0).Value < sliders.trackbar(0).Maximum - 17 Then sliders.trackbar(0).Value += 17 Else sliders.trackbar(0).Value = sliders.trackbar(0).Minimum
                If sliders.trackbar(1).Value < sliders.trackbar(1).Maximum - 10 Then sliders.trackbar(1).Value += 10 Else sliders.trackbar(1).Value = 1
                If sliders.trackbar(2).Value > 13 Then sliders.trackbar(2).Value -= 13 Else sliders.trackbar(2).Value = sliders.trackbar(2).Maximum
                If sliders.trackbar(3).Value > 27 Then sliders.trackbar(3).Value -= 27 Else sliders.trackbar(3).Value = sliders.trackbar(3).Maximum
                fillColor = task.scalarColors(task.frameCount Mod 255)
            End If
            If task.frameCount Mod 37 = 0 Then check.Box(0).Checked = Not check.Box(0).Checked
            If task.frameCount Mod 222 = 0 Then check.Box(1).Checked = Not check.Box(1).Checked
            If task.frameCount Mod 77 = 0 Then check.Box(2).Checked = Not check.Box(2).Checked
            If task.frameCount Mod 100 = 0 Then check.Box(3).Checked = Not check.Box(3).Checked
            rotateAngle += 1

        End If

        dst1.SetTo(cv.Scalar.Black)
        Dim numPoints = sliders.trackbar(0).Value
        Dim nGenPer = sliders.trackbar(3).Value
        If check.Box(1).Checked Then numPoints = CInt(numPoints / nGenPer) * nGenPer ' harmonize
        Dim radius1 = sliders.trackbar(1).Value
        Dim radius2 = sliders.trackbar(2).Value
        Dim dTheta = 2 * cv.Cv2.PI / numPoints
        Dim symmetricRipple = check.Box(0).Checked
        Dim reverseInOut = check.Box(3).Checked
        Dim pt As New cv.Point
        Dim center As New cv.Point(src.Width / 2, src.Height / 2)
        Dim points As New List(Of cv.Point)

        For i = 0 To numPoints - 1
            Dim theta = i * dTheta
            Dim ripple = radius2 * Math.Cos(nGenPer * theta)
            If symmetricRipple = False Then ripple = Math.Abs(ripple)
            If reverseInOut Then ripple = -ripple
            pt.X = Math.Truncate(center.X + (radius1 + ripple) * Math.Cos(theta + rotateAngle) + 0.5)
            pt.Y = Math.Truncate(center.Y - (radius1 + ripple) * Math.Sin(theta + rotateAngle) + 0.5)
            points.Add(pt)
        Next

        For i = 0 To numPoints - 1
            dst1.Line(points.ElementAt(i), points.ElementAt((i + 1) Mod numPoints), task.scalarColors(i Mod task.scalarColors.Count), 2, task.lineType)
        Next

        If check.Box(2).Checked Then dst1.FloodFill(center, fillColor)
    End Sub
End Class






Public Class Draw_Arc : Inherits VBparent
    Dim kalman As New Kalman_Basics
    Dim saveArcAngle As Integer
    Dim saveMargin As Integer
    Dim rect As cv.Rect

    Dim angle As Single
    Dim startAngle As Single
    Dim endAngle As Single

    Dim colorIndex As Integer
    Dim thickness As Integer
    Public Sub New()
        ReDim kalman.kInput(7 - 1)

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Clearance from image edge (margin size)", 5, dst1.Width / 8, dst1.Width / 16)
        End If
        If radio.Setup(caller, 3) Then
            radio.check(0).Text = "Draw Full Ellipse"
            radio.check(1).Text = "Draw Filled Arc"
            radio.check(2).Text = "Draw Arc"
            radio.check(1).Checked = True
        End If

        setup()

        task.desc = "Use OpenCV's ellipse function to draw an arc"
    End Sub
    Private Sub setup()
        saveMargin = sliders.trackbar(0).Value ' work in the middle of the image.

        rect = initRandomRect(dst1.Width, dst1.Height, saveMargin)
        angle = msRNG.Next(0, 360)
        colorIndex = msRNG.Next(0, 255)
        thickness = msRNG.Next(1, 5)
        startAngle = msRNG.Next(1, 360)
        endAngle = msRNG.Next(1, 360)

        kalman.kInput = {rect.X, rect.Y, rect.Width, rect.Height, angle, startAngle, endAngle}
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.useKalman Then
            kalman.kInput = {rect.X, rect.Y, rect.Width, rect.Height, angle, startAngle, endAngle}
            kalman.Run(src)
        Else
            kalman.kOutput = kalman.kInput ' do nothing...
        End If
        Dim r = New cv.Rect(kalman.kOutput(0), kalman.kOutput(1), kalman.kOutput(2), kalman.kOutput(3))
        If r.Width <= 5 Then r.Width = 5
        If r.Height <= 5 Then r.Height = 5
        Dim rr = New cv.RotatedRect(New cv.Point2f(r.X, r.Y), New cv.Size2f(r.Width, r.Height), angle)
        Dim color = task.scalarColors(colorIndex)

        dst1.SetTo(cv.Scalar.White)
        If radio.check(0).Checked Then
            dst1.Ellipse(rr, color, thickness, task.lineType)
            drawRotatedOutline(rr, dst1, task.scalarColors(colorIndex))
        Else
            Dim angle = kalman.kOutput(4)
            Dim startAngle = kalman.kOutput(5)
            Dim endAngle = kalman.kOutput(6)
            If radio.check(1).Checked Then thickness = -1
            dst1.Ellipse(New cv.Point(rr.Center.X, rr.Center.Y), New cv.Size(rr.BoundingRect.Size.Width, rr.BoundingRect.Size.Height),
                         angle, startAngle, endAngle, color, thickness, task.lineType)
        End If
        If r = rect Or sliders.trackbar(0).Value <> saveMargin Then setup()
    End Sub
End Class







Public Class Draw_ViewObjects : Inherits VBparent
    Public viewObjects As New SortedList(Of Single, viewObject)(New compareAllowIdenticalSingleInverted)
    Public Sub New()

        If check.Setup(caller, 2) Then
            check.Box(0).Text = "Draw rectangle and centroid for each mask"
            check.Box(1).Text = "Caller will handle any drawing required"
            check.Box(0).Checked = True
        End If
        task.desc = "Draw rectangles and centroids"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone Or task.intermediateReview = caller Then
            task.trueText("Draw_ViewObjects has no standalone version." + vbCrLf + "It just draws rectangles and centroids for other algorithms.")
        Else
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            Dim incr = If(viewObjects.Count < 10, 25, 255 / viewObjects.Count)  'reduces flicker of slightly different colors when < 10
            ' render masks first so they don't cover circles or rectangles below
            For i = 0 To viewObjects.Count - 1
                Dim vo = viewObjects.ElementAt(i).Value
                If vo.mask IsNot Nothing Then
                    Dim r = vo.preKalmanRect
                    If r.Width = vo.mask.Width And r.Height = vo.mask.Height Then dst1(r).SetTo(vo.LayoutColor, vo.mask)
                End If
            Next

            task.palette.Run(dst1 * cv.Scalar.All(incr))
            dst1 = task.palette.dst1

            Static drawRectangleCheck = findCheckBox("Draw rectangle and centroid for each mask")
            If drawRectangleCheck.checked Then
                For i = 0 To viewObjects.Count - 1
                    Dim vw = viewObjects.ElementAt(i).Value
                    Dim pt = vw.centroid
                    dst1.Circle(pt, task.dotSize + 2, cv.Scalar.White, -1, task.lineType, 0)
                    dst1.Circle(pt, task.dotSize + 2, cv.Scalar.Blue, -1, task.lineType, 0)
                    dst1.Rectangle(vw.rectInHist, cv.Scalar.White, 1)
                Next
            End If
        End If
    End Sub
End Class






Public Class Draw_Frustrum : Inherits VBparent
    Public xyzDepth As New Depth_WorldXYZ_MT
    Public Sub New()
        xyzDepth.depthUnitsMeters = True

        label2 = "Frustrum's shape prepared."
        task.desc = "Draw a frustrum for a camera viewport"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst1 = New cv.Mat(task.pointCloud.Height, task.pointCloud.Width, cv.MatType.CV_32F, 0)
        Dim fRect = New cv.Rect((dst2.Width - dst2.Height) / 2, 0, dst2.Height, dst2.Height)
        Dim mid = task.pointCloud.Height / 2
        Dim zIncr = task.maxZ / mid
        For i = 0 To task.pointCloud.Height / 2
            dst1(fRect).Rectangle(New cv.Rect(mid - i, mid - i, i * 2, (i + 1) * 2), cv.Scalar.All(i * zIncr), 1)
        Next
        xyzDepth.Run(dst1)
        dst2 = xyzDepth.dst2
    End Sub
End Class





Public Class Draw_ClipLine : Inherits VBparent
    Dim flow As New Font_FlowText
    Dim kalman As New Kalman_Basics
    Dim lastRect As cv.Rect
    Dim pt1 As cv.Point
    Dim pt2 As cv.Point
    Dim rect As cv.Rect
    Private Sub setup()
        ReDim kalman.kInput(8)
        Dim r = initRandomRect(dst2.Width, dst2.Height, 25)
        pt1 = New cv.Point(r.X, r.Y)
        pt2 = New cv.Point(r.X + r.Width, r.Y + r.Height)
        rect = initRandomRect(dst2.Width, dst2.Height, 25)
        If task.useKalman Then flow.msgs.Add("--------------------------- setup ---------------------------")
    End Sub
    Public Sub New()
        setup()

        task.desc = "Demonstrate the use of the ClipLine function in OpenCV. NOTE: when clipline returns true, p1/p2 are clipped by the rectangle"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst2 = src
        kalman.kInput = {pt1.X, pt1.Y, pt2.X, pt2.Y, rect.X, rect.Y, rect.Width, rect.Height}
        kalman.Run(src)
        Dim p1 = New cv.Point(CInt(kalman.kOutput(0)), CInt(kalman.kOutput(1)))
        Dim p2 = New cv.Point(CInt(kalman.kOutput(2)), CInt(kalman.kOutput(3)))

        If kalman.kOutput(6) < 5 Then kalman.kOutput(6) = 5 ' don't let the width/height get too small...
        If kalman.kOutput(7) < 5 Then kalman.kOutput(7) = 5
        Dim r = New cv.Rect(kalman.kOutput(4), kalman.kOutput(5), kalman.kOutput(6), kalman.kOutput(7))

        Dim clipped = cv.Cv2.ClipLine(r, p1, p2) ' Returns false when the line and the rectangle don't intersect.
        dst2.Line(p1, p2, If(clipped, cv.Scalar.White, cv.Scalar.Black), 2, task.lineType)
        dst2.Rectangle(r, If(clipped, cv.Scalar.Yellow, cv.Scalar.Red), 2, task.lineType)

        Static linenum = 0
        flow.msgs.Add("(" + CStr(linenum) + ") line " + If(clipped, "interects rectangle", "does not intersect rectangle"))
        linenum += 1

        Static hitCount = 0
        hitCount += If(clipped, 1, 0)
        task.trueText("There were " + Format(hitCount, "###,##0") + " intersects and " + Format(linenum - hitCount) + " misses",
                     CInt(src.Width / 2), 200)
        If r = rect Then setup()
        flow.Run(Nothing)
    End Sub
End Class






' https://stackoverflow.com/questions/7446126/opencv-2d-line-intersection-helper-function
Public Class Draw_Intersection : Inherits VBparent
    Public p1 As cv.Point2f
    Public p2 As cv.Point2f
    Public p3 As cv.Point2f
    Public p4 As cv.Point2f
    Public intersect As Boolean
    Public intersectionPoint As cv.Point2f
    Public Sub New()
        task.desc = "Determine if 2 lines intersect"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone Or task.intermediateReview = caller Then
            If task.frameCount Mod 100 <> 0 Then Exit Sub
            p1 = New cv.Point(Rnd() * src.Width, Rnd() * src.Height)
            p2 = New cv.Point(Rnd() * src.Width, Rnd() * src.Height)
            p3 = New cv.Point(Rnd() * src.Width, Rnd() * src.Height)
            p4 = New cv.Point(Rnd() * src.Width, Rnd() * src.Height)
        End If

        Dim x = p3 - p1
        Dim d1 = p2 - p1
        Dim d2 = p4 - p3
        Dim cross = d1.X * d2.Y - d1.Y * d2.X
        If Math.Abs(cross) < 0.000001 Then
            intersect = False
            intersectionPoint = New cv.Point2f
        Else
            Dim t1 = (x.X * d2.Y - x.Y * d2.X) / cross
            intersectionPoint = p1 + d1 * t1
            intersect = True
        End If

        dst1.SetTo(0)
        dst1.Line(p1, p2, cv.Scalar.Yellow, 2, task.lineType)
        dst1.Line(p3, p4, cv.Scalar.Yellow, 2, task.lineType)
        If intersectionPoint <> New cv.Point2f Then dst1.Circle(intersectionPoint, task.dotSize + 4, cv.Scalar.White, -1, task.lineType)
        If intersect Then label1 = "Intersection point = " + CStr(CInt(intersectionPoint.X)) + " x " + CStr(CInt(intersectionPoint.Y)) Else label1 = "Parallel!!!"
        If intersectionPoint.X < 0 Or intersectionPoint.X > dst1.Width Or intersectionPoint.Y < 0 Or intersectionPoint.Y > dst1.Height Then
            label1 += " (off screen)"
        End If
    End Sub
End Class







' http://www3.psych.purdue.edu/~zpizlo/GestaltCube
Public Class Draw_Hexagon : Inherits VBparent
    Dim alpha As New imageForm
    Public Sub New()
        alpha.imagePic.Image = Image.FromFile(task.parms.homeDir + "Data/GestaltCube.gif")
        alpha.Show()
        alpha.Size = New System.Drawing.Size(dst1.Width + 10, dst1.Height + 10)
        alpha.Text = "Perception is the key"
        task.desc = "What it means to recognize a cube.  Zygmunt Pizlo - UC Irvine"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
    End Sub
End Class
