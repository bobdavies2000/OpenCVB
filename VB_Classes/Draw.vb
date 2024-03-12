Imports cv = OpenCvSharp
Imports System.Drawing
Public Class Draw_Noise : Inherits VB_Algorithm
    Public maxNoiseWidth As Integer = 3
    Public addRandomColor As Boolean
    Public noiseMask As cv.Mat
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Noise Count", 1, 1000, 100)
            sliders.setupTrackBar("Noise Width", 1, 10, 3)
        End If
        desc = "Add Noise to the color image"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static widthSlider = findSlider("Noise Width")
        Static CountSlider = findSlider("Noise Count")
        maxNoiseWidth = widthSlider.Value
        src.CopyTo(dst2)
        noiseMask = New cv.Mat(src.Size(), cv.MatType.CV_8UC1).SetTo(0)
        Dim count = CountSlider.Value
        For n = 0 To count - 1
            Dim i = msRNG.Next(0, src.Cols - 1)
            Dim j = msRNG.Next(0, src.Rows - 1)
            Dim center = New cv.Point2f(i, j)
            Dim c = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
            If addRandomColor = False Then c = cv.Scalar.Black
            Dim noiseWidth = msRNG.Next(1, maxNoiseWidth)
            dst2.Circle(center, noiseWidth, c, -1, task.lineType)
            noiseMask.Circle(center, noiseWidth, cv.Scalar.White, -1, task.lineType)
        Next
    End Sub
End Class







Public Class Draw_Ellipses : Inherits VB_Algorithm
    Dim options As New Options_Draw
    Public Sub New()
        desc = "Draw the requested number of ellipses."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()
        If task.heartBeat Then
            dst2.SetTo(cv.Scalar.Black)
            For i = 0 To options.drawCount - 1
                Dim nPoint = New cv.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
                Dim eSize = New cv.Size2f(CSng(msRNG.Next(0, src.Cols - nPoint.X - 1)), CSng(msRNG.Next(0, src.Rows - nPoint.Y - 1)))
                Dim angle = 180.0F * CSng(msRNG.Next(0, 1000) / 1000.0F)
                Dim nextColor = New cv.Scalar(task.vecColors(i)(0), task.vecColors(i)(1), task.vecColors(i)(2))
                dst2.Ellipse(New cv.RotatedRect(nPoint, eSize, angle), nextColor, options.drawFilled)
            Next
        End If
    End Sub
End Class





Public Class Draw_Circles : Inherits VB_Algorithm
    Dim options As New Options_Draw
    Public Sub New()
        desc = "Draw the requested number of circles."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()
        If task.heartBeat Then
            dst2.SetTo(cv.Scalar.Black)
            For i = 0 To options.drawCount - 1
                Dim nPoint = New cv.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
                Dim radius = msRNG.Next(10, 10 + msRNG.Next(src.Cols / 4))
                Dim nextColor = New cv.Scalar(task.vecColors(i)(0), task.vecColors(i)(1), task.vecColors(i)(2))
                dst2.Circle(nPoint, radius, nextColor, options.drawFilled, task.lineType)
            Next
        End If
    End Sub
End Class








Public Class Draw_Lines : Inherits VB_Algorithm
    ReadOnly options As New Options_Draw
    Public Sub New()
        desc = "Draw the requested number of Lines."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()
        If task.heartBeat Then
            dst2.SetTo(cv.Scalar.Black)
            For i = 0 To options.drawCount - 1
                Dim nPoint1 = New cv.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
                Dim nPoint2 = New cv.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
                Dim nextColor = New cv.Scalar(task.vecColors(i)(0), task.vecColors(i)(1), task.vecColors(i)(2))
                dst2.Line(nPoint1, nPoint2, nextColor, options.drawFilled, task.lineType)
            Next
        End If
    End Sub
End Class







Public Class Draw_Polygon : Inherits VB_Algorithm
    ReadOnly options As New Options_Draw
    Public Sub New()
        desc = "Draw Polygon figures"
        labels = {"", "", "Convex Hull for the same points", "Polylines output"}
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()

        If not task.heartBeat Then Exit Sub
        Dim height = src.Height / 8
        Dim width = src.Width / 8
        Dim polyColor = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
        dst3.SetTo(cv.Scalar.Black)
        For i = 0 To options.drawCount - 1
            Dim points = New List(Of cv.Point)
            Dim listOfPoints = New List(Of List(Of cv.Point))
            For j = 0 To 10
                points.Add(New cv.Point(CInt(msRNG.Next(width, width * 7)), CInt(msRNG.Next(height, height * 7))))
            Next
            listOfPoints.Add(points)
            If options.drawFilled <> -1 Then
                cv.Cv2.Polylines(dst3, listOfPoints, True, polyColor, task.lineWidth + 1, task.lineType)
            Else
                dst3.FillPoly(listOfPoints, New cv.Scalar(0, 0, 255))
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
            cv.Cv2.DrawContours(dst2, listOfPoints, 0, polyColor, options.drawFilled)
        Next
    End Sub
End Class





' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
Public Class Draw_Shapes : Inherits VB_Algorithm
    Public Sub New()
        desc = "Use RNG to draw the same set of shapes every time"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim offsetX = 25, offsetY = 25, lineLength = 25, thickness = 2

        dst2.SetTo(0)
        For i = 1 To 256
            dst2.Line(New cv.Point(thickness * i + offsetX, offsetY), New cv.Point(thickness * i + offsetX, offsetY + lineLength), New cv.Scalar(i, i, i), thickness)
        Next
        For i = 1 To 256
            Dim color = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
            Select Case msRNG.Next(0, 3)
                Case 0 ' circle
                    Dim center = New cv.Point(msRNG.Next(offsetX, dst2.Cols - offsetX), msRNG.Next(offsetY + lineLength, dst2.Rows - offsetY))
                    Dim radius = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    dst2.Circle(center, radius, color, -1, cv.LineTypes.Link8)
                Case 1 ' Rectangle
                    Dim center = New cv.Point(msRNG.Next(offsetX, dst2.Cols - offsetX), msRNG.Next(offsetY + lineLength, dst2.Rows - offsetY))
                    Dim width = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    Dim height = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    Dim rcenter As cv.Rect = New cv.Rect(center.X - width, center.Y - height / 2, width, height)
                    dst2.Rectangle(rcenter, color, -1, cv.LineTypes.Link8)
                Case 2 ' Ellipse
                    Dim center = New cv.Point(msRNG.Next(offsetX, dst2.Cols - offsetX), msRNG.Next(offsetY + lineLength, dst2.Rows - offsetY))
                    Dim width = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    Dim height = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    Dim angle = msRNG.Next(0, 180)
                    dst2.Ellipse(center, New cv.Size(width / 2, height / 2), angle, 0, 360, color, -1, cv.LineTypes.Link8)
            End Select
        Next
    End Sub
End Class





Public Class Draw_SymmetricalShapes : Inherits VB_Algorithm
    Dim options As New Options_SymmetricalShapes
    Public Sub New()
        desc = "Generate shapes programmatically"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        options.RunVB()
        If task.heartBeat Then
            dst2.SetTo(cv.Scalar.Black)
            Dim pt As New cv.Point
            Dim center As New cv.Point(src.Width / 2, src.Height / 2)
            Dim points As New List(Of cv.Point)

            For i = 0 To options.numPoints - 1
                Dim theta = i * options.dTheta
                Dim ripple = options.radius2 * Math.Cos(options.nGenPer * theta)
                If options.symmetricRipple = False Then ripple = Math.Abs(ripple)
                If options.reverseInOut Then ripple = -ripple
                pt.X = Math.Truncate(center.X + (options.radius1 + ripple) * Math.Cos(theta + options.rotateAngle) + 0.5)
                pt.Y = Math.Truncate(center.Y - (options.radius1 + ripple) * Math.Sin(theta + options.rotateAngle) + 0.5)
                points.Add(pt)
            Next

            For i = 0 To options.numPoints - 1
                dst2.Line(points.ElementAt(i), points.ElementAt((i + 1) Mod options.numPoints), task.scalarColors(i Mod task.scalarColors.Count), task.lineWidth + 1, task.lineType)
            Next

            If options.fillRequest Then dst2.FloodFill(center, options.fillColor)
        End If
    End Sub
End Class






Public Class Draw_Arc : Inherits VB_Algorithm
    ReadOnly kalman As New Kalman_Basics
    ReadOnly saveArcAngle As Integer
    Dim rect As cv.Rect

    Dim angle As Single
    Dim startAngle As Single
    Dim endAngle As Single

    Dim colorIndex As Integer
    Dim thickness As Integer
    Dim options As New Options_DrawArc
    Public Sub New()
        desc = "Use OpenCV's ellipse function to draw an arc"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()
        If task.heartBeat Then
            rect = initRandomRect(options.saveMargin)
            angle = msRNG.Next(0, 360)
            colorIndex = msRNG.Next(0, 255)
            thickness = msRNG.Next(1, 5)
            startAngle = msRNG.Next(1, 360)
            endAngle = msRNG.Next(1, 360)

            kalman.kInput = {rect.X, rect.Y, rect.Width, rect.Height, angle, startAngle, endAngle}
        End If

        kalman.kInput = {rect.X, rect.Y, rect.Width, rect.Height, angle, startAngle, endAngle}
        kalman.Run(src)
        Dim r = New cv.Rect(kalman.kOutput(0), kalman.kOutput(1), kalman.kOutput(2), kalman.kOutput(3))
        If r.Width <= 5 Then r.Width = 5
        If r.Height <= 5 Then r.Height = 5
        Dim rr = New cv.RotatedRect(New cv.Point2f(r.X, r.Y), New cv.Size2f(r.Width, r.Height), angle)
        Dim color = task.scalarColors(colorIndex)

        dst2.SetTo(cv.Scalar.White)
        If options.drawFull Then
            dst2.Ellipse(rr, color, thickness, task.lineType)
            drawRotatedOutline(rr, dst2, task.scalarColors(colorIndex))
        Else
            Dim angle = kalman.kOutput(4)
            Dim startAngle = kalman.kOutput(5)
            Dim endAngle = kalman.kOutput(6)
            If options.drawFill Then thickness = -1
            dst2.Ellipse(New cv.Point(rr.Center.X, rr.Center.Y), New cv.Size(rr.BoundingRect.Size.Width, rr.BoundingRect.Size.Height),
                         angle, startAngle, endAngle, color, thickness, task.lineType)
        End If
    End Sub
End Class






Public Class Draw_ClipLine : Inherits VB_Algorithm
    Dim flow As New Font_FlowText
    Dim kalman As New Kalman_Basics
    Dim lastRect As cv.Rect
    Dim pt1 As cv.Point
    Dim pt2 As cv.Point
    Dim rect As cv.Rect
    Private Sub setup()
        ReDim kalman.kInput(8)
        Dim r = initRandomRect(25)
        pt1 = New cv.Point(r.X, r.Y)
        pt2 = New cv.Point(r.X + r.Width, r.Y + r.Height)
        rect = initRandomRect(25)
        If gOptions.UseKalman.Checked Then flow.msgs.Add("--------------------------- setup ---------------------------")
    End Sub
    Public Sub New()
        setup()
        desc = "Demonstrate the use of the ClipLine function in OpenCV. NOTE: when clipline returns true, p1/p2 are clipped by the rectangle"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        dst3 = src
        kalman.kInput = {pt1.X, pt1.Y, pt2.X, pt2.Y, rect.X, rect.Y, rect.Width, rect.Height}
        kalman.Run(src)
        Dim p1 = New cv.Point(CInt(kalman.kOutput(0)), CInt(kalman.kOutput(1)))
        Dim p2 = New cv.Point(CInt(kalman.kOutput(2)), CInt(kalman.kOutput(3)))

        If kalman.kOutput(6) < 5 Then kalman.kOutput(6) = 5 ' don't let the width/height get too small...
        If kalman.kOutput(7) < 5 Then kalman.kOutput(7) = 5
        Dim r = New cv.Rect(kalman.kOutput(4), kalman.kOutput(5), kalman.kOutput(6), kalman.kOutput(7))

        Dim clipped = cv.Cv2.ClipLine(r, p1, p2) ' Returns false when the line and the rectangle don't intersect.
        dst3.Line(p1, p2, If(clipped, cv.Scalar.White, cv.Scalar.Black), task.lineWidth + 1, task.lineType)
        dst3.Rectangle(r, If(clipped, cv.Scalar.Yellow, cv.Scalar.Red), task.lineWidth + 1, task.lineType)

        Static linenum = 0
        flow.msgs.Add("(" + CStr(linenum) + ") line " + If(clipped, "interects rectangle", "does not intersect rectangle"))
        linenum += 1

        Static hitCount = 0
        hitCount += If(clipped, 1, 0)
        setTrueText("There were " + Format(hitCount, "###,##0") + " intersects and " + Format(linenum - hitCount) + " misses",
                     New cv.Point(src.Width / 2, 200))
        If r = rect Then setup()
        flow.Run(empty)
    End Sub
End Class







' http://www3.psych.purdue.edu/~zpizlo/GestaltCube
Public Class Draw_Hexagon : Inherits VB_Algorithm
    Dim alpha As New imageForm
    Public Sub New()
        alpha.imagePic.Image = Image.FromFile(task.homeDir + "Data/GestaltCube.gif")
        alpha.Show()
        alpha.Size = New Size(512, 512)
        alpha.Text = "Perception is the key"
        desc = "What it means to recognize a cube.  Zygmunt Pizlo - UC Irvine"
    End Sub
    Public Sub RunVB(src as cv.Mat)
    End Sub
End Class








Public Class Draw_Line : Inherits VB_Algorithm
    Public p1 As cv.Point, p2 As cv.Point
    Public externalUse As Boolean
    Public Sub New()
        desc = "Draw a line between the selected p1 and p2 - either by clicking twice in the image or externally providing p1 and p2."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If firstPass Then task.clickPoint = New cv.Point

        If p1 <> New cv.Point And p2 <> New cv.Point And task.clickPoint <> New cv.Point Then
            p1 = New cv.Point
            p2 = New cv.Point
        End If
        dst2 = src
        If task.clickPoint <> New cv.Point Or externalUse Then
            If p1 = New cv.Point Then p1 = task.clickPoint Else p2 = task.clickPoint
        End If

        If p1 <> New cv.Point And p2 = New cv.Point Then dst2.Circle(p1, task.dotSize, task.highlightColor, -1, task.lineType)
        If p1 <> New cv.Point And p2 <> New cv.Point Then
            dst2.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
        End If
        setTrueText("Click twice in the image to provide the points below and they will be connected with a line" + vbCrLf +
                    "P1 = " + p1.ToString + vbCrLf + "P2 = " + p2.ToString, 3)
        task.clickPoint = New cv.Point
    End Sub
End Class







Public Class Draw_LineTest : Inherits VB_Algorithm
    ReadOnly line As New Draw_Line
    Public Sub New()
        desc = "Test the external use of the Draw_Line algorithm - provide 2 points and draw the line..."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then
            line.p1 = New cv.Point(msRNG.Next(0, src.Width), msRNG.Next(0, src.Height))
            line.p2 = New cv.Point(msRNG.Next(0, src.Width), msRNG.Next(0, src.Height))
        End If
        line.Run(src)
        dst2 = line.dst2
    End Sub
End Class






Public Class Draw_Frustrum : Inherits VB_Algorithm
    Public xyzDepth As New Depth_WorldXYZ
    Public Sub New()
        xyzDepth.depthUnitsMeters = True
        labels(3) = "Frustrum 3D pointcloud"
        desc = "Draw a frustrum for a camera viewport"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        src = New cv.Mat(task.workingRes, cv.MatType.CV_32F, 0)

        Dim mid = src.Height / 2
        Dim zIncr = task.maxZmeters / mid
        dst2 = src.Clone
        Dim fRect = New cv.Rect((src.Width - src.Height) / 2, 0, src.Height, src.Height)
        For i = 0 To src.Height / 2
            dst2(fRect).Rectangle(New cv.Rect(mid - i, mid - i, i * 2, (i + 1) * 2), i * zIncr, 1)
        Next
        xyzDepth.Run(dst2)
        dst3 = xyzDepth.dst2.Resize(task.workingRes)
    End Sub
End Class







'Public Class Draw_FrustrumNew : Inherits VB_Algorithm
'    Public xyzDepth As New Depth_WorldXYZ
'    Public Sub New()
'        xyzDepth.depthUnitsMeters = True
'        labels(3) = "Frustrum 3D pointcloud"
'        desc = "Draw a frustrum for a camera viewport"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        dst2 = New cv.Mat(src.Size, cv.MatType.CV_32F, 0)

'        Dim mid = src.Height / 2
'        Dim zIncr = task.maxZmeters / mid
'        Dim fRect = New cv.Rect((src.Width - src.Height) / 2, 0, src.Height, src.Height)
'        For i = 0 To src.Height / 2
'            dst2(fRect).Rectangle(New cv.Rect(mid - i, mid - i, i * 2, (i + 1) * 2), i * zIncr, 1)
'        Next

'        Dim mmX = vbMinMax(task.pcSplit(0))
'        Dim incrX = (mmX.maxVal - mmX.minVal) / dst2.Width
'        Dim mmY = vbMinMax(task.pcSplit(2))
'        Dim incrY = (mmY.maxVal - mmX.minVal) / dst2.Height
'        dst3.SetTo(0)
'        For y = 0 To dst2.Height - 1
'            For x = (src.Width - src.Height) / 2 To dst2.Height - 1
'                Dim xyz = New cv.Point3f(x * incrX + mmX.minVal, y * incrY + mmY.minVal, 0)
'                xyz.Z = dst2.Get(Of Single)(y, x)
'                If xyz.Z > 0 Then dst3.Set(Of cv.Point3f)(y, x, getWorldCoordinates(xyz))
'            Next
'        Next
'    End Sub
'End Class