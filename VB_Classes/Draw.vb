Imports cvb = OpenCvSharp
Imports System.Drawing
Public Class Draw_Noise : Inherits VB_Parent
    Public addRandomColor As Boolean
    Public noiseMask As cvb.Mat
    Public options As New Options_DrawNoise
    Public Sub New()
        desc = "Add Noise to the color image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        src.CopyTo(dst2)
        noiseMask = New cvb.Mat(src.Size(), cvb.MatType.CV_8UC1).SetTo(0)
        For n = 0 To options.noiseCount - 1
            Dim i = msRNG.Next(0, src.Cols - 1)
            Dim j = msRNG.Next(0, src.Rows - 1)
            Dim center = New cvb.Point2f(i, j)
            Dim c = New cvb.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
            If addRandomColor = False Then c = cvb.Scalar.Black
            Dim noiseWidth = msRNG.Next(1, options.noiseWidth)
            DrawCircle(dst2, center, noiseWidth, c)
            DrawCircle(noiseMask, center, noiseWidth, cvb.Scalar.White)
        Next
    End Sub
End Class







Public Class Draw_Ellipses : Inherits VB_Parent
    Dim options As New Options_Draw
    Public Sub New()
        desc = "Draw the requested number of ellipses."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Options.RunOpt()
        If task.heartBeat Then
            dst2.SetTo(cvb.Scalar.Black)
            For i = 0 To options.drawCount - 1
                Dim nPoint = New cvb.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
                Dim eSize = New cvb.Size2f(CSng(msRNG.Next(0, src.Cols - nPoint.X - 1)), CSng(msRNG.Next(0, src.Rows - nPoint.Y - 1)))
                Dim angle = 180.0F * CSng(msRNG.Next(0, 1000) / 1000.0F)
                Dim nextColor = New cvb.Scalar(task.vecColors(i)(0), task.vecColors(i)(1), task.vecColors(i)(2))
                dst2.Ellipse(New cvb.RotatedRect(nPoint, eSize, angle), nextColor, options.drawFilled)
            Next
        End If
    End Sub
End Class





Public Class Draw_Circles : Inherits VB_Parent
    Dim options As New Options_Draw
    Public Sub New()
        desc = "Draw the requested number of circles."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Options.RunOpt()
        If task.heartBeat Then
            dst2.SetTo(cvb.Scalar.Black)
            For i = 0 To options.drawCount - 1
                Dim nPoint = New cvb.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
                Dim radius = msRNG.Next(10, 10 + msRNG.Next(src.Cols / 4))
                Dim nextColor = New cvb.Scalar(task.vecColors(i)(0), task.vecColors(i)(1), task.vecColors(i)(2))
                dst2.Circle(nPoint, radius, nextColor, options.drawFilled, task.lineType)
            Next
        End If
    End Sub
End Class








Public Class Draw_Lines : Inherits VB_Parent
    ReadOnly options As New Options_Draw
    Public Sub New()
        desc = "Draw the requested number of Lines."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Options.RunOpt()
        If task.heartBeat Then
            dst2.SetTo(cvb.Scalar.Black)
            For i = 0 To options.drawCount - 1
                Dim nPoint1 = New cvb.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
                Dim nPoint2 = New cvb.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
                Dim nextColor = New cvb.Scalar(task.vecColors(i)(0), task.vecColors(i)(1), task.vecColors(i)(2))
                dst2.Line(nPoint1, nPoint2, nextColor, options.drawFilled, task.lineType)
            Next
        End If
    End Sub
End Class







Public Class Draw_Polygon : Inherits VB_Parent
    ReadOnly options As New Options_Draw
    Public Sub New()
        desc = "Draw Polygon figures"
        labels = {"", "", "Convex Hull for the same points", "Polylines output"}
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Options.RunOpt()

        If not task.heartBeat Then Exit Sub
        Dim height = src.Height / 8
        Dim width = src.Width / 8
        Dim polyColor = New cvb.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
        dst3.SetTo(cvb.Scalar.Black)
        For i = 0 To options.drawCount - 1
            Dim points = New List(Of cvb.Point)
            Dim listOfPoints = New List(Of List(Of cvb.Point))
            For j = 0 To 10
                points.Add(New cvb.Point(CInt(msRNG.Next(width, width * 7)), CInt(msRNG.Next(height, height * 7))))
            Next
            listOfPoints.Add(points)
            If options.drawFilled <> -1 Then
                cvb.Cv2.Polylines(dst3, listOfPoints, True, polyColor, task.lineWidth + 1, task.lineType)
            Else
                dst3.FillPoly(listOfPoints, New cvb.Scalar(0, 0, 255))
            End If

            Dim hull() As cvb.Point
            hull = cvb.Cv2.ConvexHull(points, True)
            listOfPoints = New List(Of List(Of cvb.Point))
            points = New List(Of cvb.Point)
            For j = 0 To hull.Count - 1
                points.Add(New cvb.Point(hull(j).X, hull(j).Y))
            Next
            listOfPoints.Add(points)
            dst2.SetTo(cvb.Scalar.Black)
            cvb.Cv2.DrawContours(dst2, listOfPoints, 0, polyColor, options.drawFilled)
        Next
    End Sub
End Class





' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
Public Class Draw_Shapes : Inherits VB_Parent
    Public Sub New()
        desc = "Use RNG to draw the same set of shapes every time"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim offsetX = 25, offsetY = 25, lineLength = 25, thickness = 2

        dst2.SetTo(0)
        For i = 1 To 256
            Dim p1 = New cvb.Point(thickness * i + offsetX, offsetY)
            Dim p2 = New cvb.Point(thickness * i + offsetX, offsetY + lineLength)
            dst2.Line(p1, p2, New cvb.Scalar(i, i, i), thickness)
        Next
        For i = 1 To 256
            Dim color = New cvb.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
            Select Case msRNG.Next(0, 3)
                Case 0 ' circle
                    Dim center = New cvb.Point(msRNG.Next(offsetX, dst2.Cols - offsetX), msRNG.Next(offsetY + lineLength, dst2.Rows - offsetY))
                    Dim radius = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    DrawCircle(dst2, center, radius, color)
                Case 1 ' Rectangle
                    Dim center = New cvb.Point(msRNG.Next(offsetX, dst2.Cols - offsetX), msRNG.Next(offsetY + lineLength, dst2.Rows - offsetY))
                    Dim width = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    Dim height = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    Dim rcenter As cvb.Rect = New cvb.Rect(center.X - width, center.Y - height / 2, width, height)
                    dst2.Rectangle(rcenter, color, -1, cvb.LineTypes.Link8)
                Case 2 ' Ellipse
                    Dim center = New cvb.Point(msRNG.Next(offsetX, dst2.Cols - offsetX), msRNG.Next(offsetY + lineLength, dst2.Rows - offsetY))
                    Dim width = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    Dim height = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    Dim angle = msRNG.Next(0, 180)
                    dst2.Ellipse(center, New cvb.Size(width / 2, height / 2), angle, 0, 360, color, -1, cvb.LineTypes.Link8)
            End Select
        Next
    End Sub
End Class





Public Class Draw_SymmetricalShapes : Inherits VB_Parent
    Dim options As New Options_SymmetricalShapes
    Public Sub New()
        desc = "Generate shapes programmatically"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        If task.heartBeat Then
            dst2.SetTo(cvb.Scalar.Black)
            Dim pt As New cvb.Point
            Dim center As New cvb.Point(src.Width / 2, src.Height / 2)
            Dim points As New List(Of cvb.Point)

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
                Dim p1 = points.ElementAt(i)
                Dim p2 = points.ElementAt((i + 1) Mod options.numPoints)
                dst2.Line(p1, p2, task.scalarColors(i Mod task.scalarColors.Count), task.lineWidth + 1, task.lineType)
            Next

            If options.fillRequest Then dst2.FloodFill(center, options.fillColor)
        End If
    End Sub
End Class






Public Class Draw_Arc : Inherits VB_Parent
    Dim kalman As New Kalman_Basics
    Dim rect As cvb.Rect

    Dim angle As Single
    Dim startAngle As Single
    Dim endAngle As Single

    Dim colorIndex As Integer
    Dim thickness As Integer
    Dim options As New Options_DrawArc
    Public Sub New()
        desc = "Use OpenCV's ellipse function to draw an arc"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Options.RunOpt()
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
        Dim r = New cvb.Rect(kalman.kOutput(0), kalman.kOutput(1), kalman.kOutput(2), kalman.kOutput(3))
        If r.Width <= 5 Then r.Width = 5
        If r.Height <= 5 Then r.Height = 5
        Dim rr = New cvb.RotatedRect(New cvb.Point2f(r.X, r.Y), New cvb.Size2f(r.Width, r.Height), angle)
        Dim color = task.scalarColors(colorIndex)

        dst2.SetTo(cvb.Scalar.White)
        If options.drawFull Then
            dst2.Ellipse(rr, color, thickness, task.lineType)
            DrawRotatedOutline(rr, dst2, task.scalarColors(colorIndex))
        Else
            Dim angle = kalman.kOutput(4)
            Dim startAngle = kalman.kOutput(5)
            Dim endAngle = kalman.kOutput(6)
            If options.drawFill Then thickness = -1
            Dim r1 = rr.BoundingRect
            dst2.Ellipse(New cvb.Point(rr.Center.X, rr.Center.Y), New cvb.Size(r1.Width, r1.Height),
                         angle, startAngle, endAngle, color, thickness, task.lineType)
        End If
    End Sub
End Class






Public Class Draw_ClipLine : Inherits VB_Parent
    Dim flow As New Font_FlowText
    Dim kalman As New Kalman_Basics
    Dim pt1 As cvb.Point
    Dim pt2 As cvb.Point
    Dim rect As cvb.Rect
    Dim linenum = 0
    Dim hitCount = 0
    Private Sub setup()
        ReDim kalman.kInput(8)
        Dim r = initRandomRect(25)
        pt1 = New cvb.Point(r.X, r.Y)
        pt2 = New cvb.Point(r.X + r.Width, r.Y + r.Height)
        rect = initRandomRect(25)
        If task.gOptions.UseKalman.Checked Then flow.flowText.Add("--------------------------- setup ---------------------------")
    End Sub
    Public Sub New()
        flow.parentData = Me
        setup()
        desc = "Demonstrate the use of the ClipLine function in Opencvb. NOTE: when clipline returns true, p1/p2 are clipped by the rectangle"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst3 = src
        kalman.kInput = {pt1.X, pt1.Y, pt2.X, pt2.Y, rect.X, rect.Y, rect.Width, rect.Height}
        kalman.Run(src)
        Dim p1 = New cvb.Point(CInt(kalman.kOutput(0)), CInt(kalman.kOutput(1)))
        Dim p2 = New cvb.Point(CInt(kalman.kOutput(2)), CInt(kalman.kOutput(3)))

        If kalman.kOutput(6) < 5 Then kalman.kOutput(6) = 5 ' don't let the width/height get too small...
        If kalman.kOutput(7) < 5 Then kalman.kOutput(7) = 5
        Dim r = New cvb.Rect(kalman.kOutput(4), kalman.kOutput(5), kalman.kOutput(6), kalman.kOutput(7))

        Dim clipped = cvb.Cv2.ClipLine(r, p1, p2) ' Returns false when the line and the rectangle don't intersect.
        dst3.Line(p1, p2, If(clipped, cvb.Scalar.White, cvb.Scalar.Black), task.lineWidth + 1, task.lineType)
        dst3.Rectangle(r, If(clipped, cvb.Scalar.Yellow, cvb.Scalar.Red), task.lineWidth + 1, task.lineType)

        flow.nextMsg = "(" + CStr(linenum) + ") line " + If(clipped, "interects rectangle", "does not intersect rectangle")
        linenum += 1

        hitCount += If(clipped, 1, 0)
        setTrueText("There were " + Format(hitCount, "###,##0") + " intersects and " + Format(linenum - hitCount) + " misses",
                     New cvb.Point(src.Width / 2, 200))
        If r = rect Then setup()
        flow.Run(empty)
    End Sub
End Class







' http://www3.psych.purdue.edu/~zpizlo/GestaltCube
Public Class Draw_Hexagon : Inherits VB_Parent
    Dim alpha As New ImageForm
    Public Sub New()
        alpha.imagePic.Image = Image.FromFile(task.homeDir + "Data/GestaltCube.gif")
        alpha.Show()
        alpha.Size = New Size(512, 512)
        alpha.Text = "Perception is the key"
        desc = "What it means to recognize a cube.  Zygmunt Pizlo - UC Irvine"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
    End Sub
End Class








Public Class Draw_Line : Inherits VB_Parent
    Public p1 As cvb.Point, p2 As cvb.Point
    Public externalUse As Boolean
    Public Sub New()
        desc = "Draw a line between the selected p1 and p2 - either by clicking twice in the image or externally providing p1 and p2."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.firstPass Then task.ClickPoint = New cvb.Point

        If p1 <> New cvb.Point And p2 <> New cvb.Point And task.clickPoint <> New cvb.Point Then
            p1 = New cvb.Point
            p2 = New cvb.Point
        End If
        dst2 = src
        If task.clickPoint <> New cvb.Point Or externalUse Then
            If p1 = New cvb.Point Then p1 = task.clickPoint Else p2 = task.clickPoint
        End If

        If p1 <> New cvb.Point And p2 = New cvb.Point Then DrawCircle(dst2, p1, task.DotSize, task.highlightColor)
        If p1 <> New cvb.Point And p2 <> New cvb.Point Then
            DrawLine(dst2, p1, p2, task.highlightColor)
        End If
        SetTrueText("Click twice in the image to provide the points below and they will be connected with a line" + vbCrLf +
                    "P1 = " + p1.ToString + vbCrLf + "P2 = " + p2.ToString, 3)
        task.clickPoint = New cvb.Point
    End Sub
End Class







Public Class Draw_LineTest : Inherits VB_Parent
    ReadOnly line As New Draw_Line
    Public Sub New()
        desc = "Test the external use of the Draw_Line algorithm - provide 2 points and draw the line..."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.heartBeat Then
            line.p1 = New cvb.Point(msRNG.Next(0, src.Width), msRNG.Next(0, src.Height))
            line.p2 = New cvb.Point(msRNG.Next(0, src.Width), msRNG.Next(0, src.Height))
        End If
        line.Run(src)
        dst2 = line.dst2
    End Sub
End Class






Public Class Draw_Frustrum : Inherits VB_Parent
    Public xyzDepth As New Depth_WorldXYZ
    Public Sub New()
        xyzDepth.depthUnitsMeters = True
        labels(3) = "Frustrum 3D pointcloud"
        desc = "Draw a frustrum for a camera viewport"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        src = New cvb.Mat(task.workingRes, cvb.MatType.CV_32F, cvb.Scalar.All(0))

        Dim mid = src.Height / 2
        Dim zIncr = task.maxZmeters / mid
        dst2 = src.Clone
        Dim fRect = New cvb.Rect((src.Width - src.Height) / 2, 0, src.Height, src.Height)
        For i = 0 To src.Height / 2
            dst2(fRect).Rectangle(New cvb.Rect(mid - i, mid - i, i * 2, (i + 1) * 2), i * zIncr, 1)
        Next
        xyzDepth.Run(dst2)
        dst3 = xyzDepth.dst2.Resize(task.workingRes)
    End Sub
End Class