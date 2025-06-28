Imports System.Text.RegularExpressions
Imports cv = OpenCvSharp
Public Class LineRGB_Basics : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public lpMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public lpLastList As New List(Of lpData)
    Public rawLines As New LineRGB_Raw
    Public minAge As Integer = 5 ' line has to be around for a little while before it is recorded as a line.
    Public Sub New()
        desc = "Retain line from earlier image if not in motion mask.  If new line is in motion mask, add it."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then
            lpLastList.Clear()
            lpList.Clear()
            task.motionMask.SetTo(255)
        End If

        Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)

        rawLines.Run(src)
        dst3 = rawLines.dst2
        labels(3) = rawLines.labels(2)

        For Each lp In rawLines.lpList
            sortlines.Add(lp.length, lp)
        Next

        lpList.Clear()
        dst2 = src
        lpMap.SetTo(0)
        For Each lp In sortlines.Values
            lp.index = lpList.Count
            lpList.Add(lp)
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
            lpMap.Line(lp.p1, lp.p2, lp.index + 1, task.lineWidth * 3, cv.LineTypes.Link8)

            If standaloneTest() Then
                For i As Integer = 0 To 3
                    dst2.Line(lp.vertices(i), lp.vertices((i + 1) Mod 4), task.highlight, task.lineWidth)
                Next
            End If
            If lpList.Count >= task.FeatureSampleSize Then Exit For
        Next

        If standaloneTest() Then dst1 = ShowPalette(lpMap)
        lpLastList = New List(Of lpData)(lpList)
        labels(2) = "Of the " + CStr(rawLines.lpList.Count) + " raw lines found, shown below are the " + CStr(lpList.Count) + " longest."
    End Sub
End Class








Public Class LineRGB_Raw : Inherits TaskParent
    Dim ld As cv.XImgProc.FastLineDetector
    Public lpList As New List(Of lpData)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset " +
               "rectangle (provided externally)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If src.Type <> cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_8U)

        Dim lines = ld.Detect(src)
        lpList.Clear()
        For Each v In lines
            If v(0) >= 0 And v(0) <= src.Cols And v(1) >= 0 And v(1) <= src.Rows And
               v(2) >= 0 And v(2) <= src.Cols And v(3) >= 0 And v(3) <= src.Rows Then
                Dim p1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim p2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                If p1.X >= 0 And p1.X < dst2.Width And p1.Y >= 0 And p1.Y < dst2.Height And
                   p2.X >= 0 And p2.X < dst2.Width And p2.Y >= 0 And p2.Y < dst2.Height Then
                    p1 = validatePoint(p1)
                    p2 = validatePoint(p2)
                    Dim lp = New lpData(p1, p2)
                    lp.index = lpList.Count
                    lpList.Add(lp)
                End If
            End If
        Next

        dst2.SetTo(0)
        For Each lp In lpList
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next

        labels(2) = CStr(lpList.Count) + " lines were detected in the current frame"
    End Sub
End Class








Public Class LineRGB_RawSorted : Inherits TaskParent
    Dim ld As cv.XImgProc.FastLineDetector
    Public lpList As New List(Of lpData)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset " +
               "rectangle (provided externally)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If src.Type <> cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_8U)

        Dim lines = ld.Detect(src)

        Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
        For Each v In lines
            If v(0) >= 0 And v(0) <= src.Cols And v(1) >= 0 And v(1) <= src.Rows And
               v(2) >= 0 And v(2) <= src.Cols And v(3) >= 0 And v(3) <= src.Rows Then
                Dim p1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim p2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                If p1.X >= 0 And p1.X < dst2.Width And p1.Y >= 0 And p1.Y < dst2.Height And
                   p2.X >= 0 And p2.X < dst2.Width And p2.Y >= 0 And p2.Y < dst2.Height Then
                    Dim lp = New lpData(p1, p2)
                    sortlines.Add(lp.length, lp)
                End If
            End If
        Next

        lpList.Clear()
        For Each lp In sortlines.Values
            lp.index = lpList.Count
            lp.p1 = validatePoint(lp.p1)
            lp.p2 = validatePoint(lp.p2)
            lpList.Add(lp)
        Next

        If standaloneTest() Then
            dst2.SetTo(0)
            For Each lp In lpList
                dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
            Next
        End If

        labels(2) = CStr(lpList.Count) + " lines were detected in the current frame"
    End Sub
End Class







Public Class LineRGB_RawSubset : Inherits TaskParent
    Dim ld As cv.XImgProc.FastLineDetector
    Public lpList As New List(Of lpData)
    Public subsetRect As cv.Rect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset " +
               "rectangle (provided externally)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If src.Type <> cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_8U)

        Dim lines = ld.Detect(src(ValidateRect(subsetRect)))

        Dim lpDataList As New List(Of lpData)
        For Each v In lines
            If v(0) >= 0 And v(0) <= src.Cols And v(1) >= 0 And v(1) <= src.Rows And
               v(2) >= 0 And v(2) <= src.Cols And v(3) >= 0 And v(3) <= src.Rows Then
                Dim p1 = New cv.Point(CInt(v(0) + subsetRect.X), CInt(v(1) + subsetRect.Y))
                Dim p2 = New cv.Point(CInt(v(2) + subsetRect.X), CInt(v(3) + subsetRect.Y))
                If p1.X >= 0 And p1.X < dst2.Width And p1.Y >= 0 And p1.Y < dst2.Height And
                   p2.X >= 0 And p2.X < dst2.Width And p2.Y >= 0 And p2.Y < dst2.Height Then
                    lpDataList.Add(New lpData(p1, p2))
                End If
            End If
        Next

        Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
        For Each lp In lpDataList
            sortlines.Add(lp.length, lp)
        Next

        lpList.Clear()
        For Each lp In sortlines.Values
            lp.index = lpList.Count
            lpList.Add(lp)
        Next

        If standaloneTest() Then
            dst2.SetTo(0)
            For Each lp In lpList
                dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
            Next
        End If
        labels(2) = CStr(lpList.Count) + " lines were detected in the current frame"
    End Sub
End Class








Public Class LineRGB_Intercepts : Inherits TaskParent
    Public extended As New LongLine_ExtendTest
    Public p1List As New List(Of cv.Point2f)
    Public p2List As New List(Of cv.Point2f)
    Dim longLine As New XO_LongLine_BasicsEx
    Public options As New Options_Intercepts
    Public intercept As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public topIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public botIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public leftIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public rightIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public interceptArray = {topIntercepts, botIntercepts, leftIntercepts, rightIntercepts}
    Public Sub New()
        labels(2) = "Highlight line x- and y-intercepts.  Move mouse over the image."
        desc = "Show lines with similar y-intercepts"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If task.lineRGB.lpList.Count = 0 Then Exit Sub

        dst2 = src
        p1List.Clear()
        p2List.Clear()
        intercept = interceptArray(options.selectedIntercept)
        topIntercepts.Clear()
        botIntercepts.Clear()
        leftIntercepts.Clear()
        rightIntercepts.Clear()
        Dim index As Integer
        For Each lp In task.lineRGB.lpList
            Dim minXX = Math.Min(lp.p1.X, lp.p2.X)
            If lp.p1.X <> minXX Then ' leftmost point is always in p1
                Dim tmp = lp.p1
                lp.p1 = lp.p2
                lp.p2 = tmp
            End If

            p1List.Add(lp.p1)
            p2List.Add(lp.p2)
            DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Yellow)

            Dim saveP1 = lp.p1, saveP2 = lp.p2

            If lp.ep1.X = 0 Then leftIntercepts.Add(saveP1.Y, index)
            If lp.ep1.Y = 0 Then topIntercepts.Add(saveP1.X, index)
            If lp.ep1.X = dst2.Width Then rightIntercepts.Add(saveP1.Y, index)
            If lp.ep1.Y = dst2.Height Then botIntercepts.Add(saveP1.X, index)

            If lp.ep2.X = 0 Then leftIntercepts.Add(saveP2.Y, index)
            If lp.ep2.Y = 0 Then topIntercepts.Add(saveP2.X, index)
            If lp.ep2.X = dst2.Width Then rightIntercepts.Add(saveP2.Y, index)
            If lp.ep2.Y = dst2.Height Then botIntercepts.Add(saveP2.X, index)
            index += 1
        Next

        If standaloneTest() Then
            For Each inter In intercept
                If Math.Abs(options.mouseMovePoint - inter.Key) < options.interceptRange Then
                    DrawLine(dst2, p1List(inter.Value), p2List(inter.Value), cv.Scalar.Blue)
                End If
            Next
        End If
    End Sub
End Class











Public Class LineRGB_Perpendicular : Inherits TaskParent
    Public input As lpData
    Public output As lpData
    Dim midPoint As cv.Point2f
    Public Sub New()
        labels = {"", "", "White is the original line, red dot is midpoint, yellow is perpendicular line", ""}
        desc = "Find the line perpendicular to the line created by the points provided."
    End Sub
    Public Shared Function computePerp(lp As lpData) As lpData
        Dim midPoint = New cv.Point2f((lp.p1.X + lp.p2.X) / 2, (lp.p1.Y + lp.p2.Y) / 2)
        Dim m = If(lp.slope = 0, 100000, -1 / lp.slope)
        Dim b = midPoint.Y - m * midPoint.X
        Dim p1 = New cv.Point2f(-b / m, 0)
        Dim p2 = New cv.Point2f((task.workingRes.Height - b) / m, task.workingRes.Height)

        Dim w = task.workingRes.Width
        Dim h = task.workingRes.Height

        If p1.X < 0 Then p1 = New cv.Point2f(0, b)
        If p1.X > w Then p1 = New cv.Point2f(w, m * w + b)
        If p1.Y < 0 Then p1 = New cv.Point2f(-b / m, 0)
        If p1.Y > h Then p1 = New cv.Point2f(w, m * w + b)

        If p2.X < 0 Then p2 = New cv.Point2f(0, b)
        If p2.X > w Then p2 = New cv.Point2f(w, m * w + b)
        If p2.Y < 0 Then p2 = New cv.Point2f(-b / m, 0)
        If p2.Y > h Then p2 = New cv.Point2f(w, m * w + b)

        Return New lpData(p1, p2)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then input = task.gravityVec
        dst2.SetTo(0)
        DrawLine(dst2, input.p1, input.p2, white)

        output = computePerp(input)
        DrawCircle(dst2, midPoint, task.DotSize + 2, cv.Scalar.Red)
        DrawLine(dst2, output.p1, output.p2, cv.Scalar.Yellow)
    End Sub
End Class







Public Class LineRGB_Info : Inherits TaskParent
    Public Sub New()
        labels(3) = "The selected line with details."
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Display details about the line selected."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        labels(2) = task.lineRGB.labels(2) + " - Use the global option 'DebugSlider' to select a line."

        If task.lineRGB.lpList.Count <= 1 Then Exit Sub
        If standaloneTest() Then
            dst2.SetTo(0)
            For Each lp In task.lineRGB.lpList
                dst2.Line(lp.p1, lp.p2, white, task.lineWidth, cv.LineTypes.Link8)
                DrawCircle(dst2, lp.p1, task.DotSize, task.highlight)
            Next
        End If

        'Dim lpIndex = Math.Abs(task.gOptions.DebugSlider.Value)
        'If lpIndex < task.lineRGB.lpList.Count Then task.lpD = task.lineRGB.lpList(lpIndex)

        'strOut = "Use the global options 'DebugSlider' to select the line for display " + vbCrLf + vbCrLf
        'strOut += CStr(task.lineRGB.lpList.Count) + " lines found " + vbCrLf + vbCrLf

        dst2.Line(task.lpD.p1, task.lpD.p2, task.highlight, task.lineWidth + 1, task.lineType)

        strOut += "Line ID = " + CStr(task.lpD.index) + vbCrLf + vbCrLf
        strOut += "index = " + CStr(task.lpD.index) + vbCrLf
        strOut += "Age = " + CStr(task.lpD.age) + vbCrLf

        strOut += "p1 = " + task.lpD.p1.ToString + ", p2 = " + task.lpD.p2.ToString + vbCrLf + vbCrLf
        strOut += "Slope = " + Format(task.lpD.slope, fmt3) + vbCrLf
        strOut += vbCrLf + "NOTE: the Y-Axis is inverted - Y increases down so slopes are inverted." + vbCrLf + vbCrLf

        strOut += "Below are the task.gridRects that the line intersects." + vbCrLf
        For Each index In task.lpD.bricks
            If index = task.lpD.bricks.Last Then strOut += CStr(index) Else strOut += CStr(index) + ", "
        Next
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class LineRGB_ViewLeftRight : Inherits TaskParent
    Dim lines As New LineRGB_Basics
    Dim linesRaw As New LineRGB_RawSorted
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        desc = "Find lines in the left and right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lines.Run(task.leftView)
        dst2.SetTo(0)
        For Each lp In task.lineRGB.lpList
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth)
        Next
        labels(2) = lines.labels(2)

        linesRaw.Run(task.rightView)
        dst3 = linesRaw.dst2
        labels(3) = linesRaw.labels(2)
    End Sub
End Class







Public Class LineRGB_GCloud : Inherits TaskParent
    Public sortedVerticals As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
    Public sortedHorizontals As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
    Public allLines As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
    Public options As New Options_LineFinder
    Dim match As New Match_tCell
    Dim angleSlider As System.Windows.Forms.TrackBar
    Dim lines As New LineRGB_RawSorted
    Public Sub New()
        angleSlider = OptionParent.FindSlider("Angle tolerance in degrees")
        labels(2) = "Line_GCloud - Blue are vertical lines using the angle thresholds."
        desc = "Find all the vertical lines using the point cloud rectified with the IMU vector for gravity."
    End Sub
    Public Function updateGLine(src As cv.Mat, brick As gravityLine, p1 As cv.Point, p2 As cv.Point) As gravityLine
        brick.tc1.center = p1
        brick.tc2.center = p2
        brick.tc1 = match.createCell(src, brick.tc1.correlation, p1)
        brick.tc2 = match.createCell(src, brick.tc2.correlation, p2)
        brick.tc1.strOut = Format(brick.tc1.correlation, fmt2) + vbCrLf + Format(brick.tc1.depth, fmt2) + "m"
        brick.tc2.strOut = Format(brick.tc2.correlation, fmt2) + vbCrLf + Format(brick.tc2.depth, fmt2) + "m"

        Dim mean = task.pointCloud(brick.tc1.rect).Mean(task.depthMask(brick.tc1.rect))
        brick.pt1 = New cv.Point3f(mean(0), mean(1), mean(2))
        brick.tc1.depth = brick.pt1.Z
        mean = task.pointCloud(brick.tc2.rect).Mean(task.depthMask(brick.tc2.rect))
        brick.pt2 = New cv.Point3f(mean(0), mean(1), mean(2))
        brick.tc2.depth = brick.pt2.Z

        brick.len3D = distance3D(brick.pt1, brick.pt2)
        If brick.pt1 = New cv.Point3f Or brick.pt2 = New cv.Point3f Then
            brick.len3D = 0
        Else
            brick.arcX = Math.Asin((brick.pt1.X - brick.pt2.X) / brick.len3D) * 57.2958
            brick.arcY = Math.Abs(Math.Asin((brick.pt1.Y - brick.pt2.Y) / brick.len3D) * 57.2958)
            If brick.arcY > 90 Then brick.arcY -= 90
            brick.arcZ = Math.Asin((brick.pt1.Z - brick.pt2.Z) / brick.len3D) * 57.2958
        End If

        Return brick
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim maxAngle = angleSlider.Value

        dst2 = src.Clone
        lines.Run(src.Clone)

        sortedVerticals.Clear()
        sortedHorizontals.Clear()
        For Each lp In lines.lpList
            Dim brick As gravityLine
            brick = updateGLine(src, brick, lp.p1, lp.p2)
            allLines.Add(lp.p1.DistanceTo(lp.p2), brick)
            If Math.Abs(90 - brick.arcY) < maxAngle And brick.tc1.depth > 0 And brick.tc2.depth > 0 Then
                sortedVerticals.Add(lp.p1.DistanceTo(lp.p2), brick)
                DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Blue)
            End If
            If Math.Abs(brick.arcY) <= maxAngle And brick.tc1.depth > 0 And brick.tc2.depth > 0 Then
                sortedHorizontals.Add(lp.p1.DistanceTo(lp.p2), brick)
                DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Yellow)
            End If
        Next

        labels(2) = Format(sortedHorizontals.Count, "00") + " Horizontal lines were identified and " +
                    Format(sortedVerticals.Count, "00") + " Vertical lines were identified."
    End Sub
End Class







' https://stackoverflow.com/questions/7446126/opencv-2d-line-intersection-helper-function
Public Class LineRGB_Intersection : Inherits TaskParent
    Public p1 As cv.Point2f, p2 As cv.Point2f, p3 As cv.Point2f, p4 As cv.Point2f
    Public intersectionPoint As cv.Point2f
    Public Sub New()
        desc = "Determine if 2 lines intersect, where the point is, and if that point is in the image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            p1 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            p2 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            p3 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            p4 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        End If

        intersectionPoint = IntersectTest(p1, p2, p3, p4)
        intersectionPoint = IntersectTest(New lpData(p1, p2), New lpData(p3, p4))

        dst2.SetTo(0)
        dst2.Line(p1, p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        dst2.Line(p3, p4, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        If intersectionPoint <> New cv.Point2f Then
            DrawCircle(dst2, intersectionPoint, task.DotSize + 4, white)
            labels(2) = "Intersection point = " + CStr(CInt(intersectionPoint.X)) + " x " + CStr(CInt(intersectionPoint.Y))
        Else
            labels(2) = "Parallel!!!"
        End If
        If intersectionPoint.X < 0 Or intersectionPoint.X > dst2.Width Or intersectionPoint.Y < 0 Or intersectionPoint.Y > dst2.Height Then
            labels(2) += " (off screen)"
        End If
    End Sub
End Class






Public Class LineRGB_Bricks : Inherits TaskParent
    Public lp As lpData
    Public Sub New()
        task.brickRunFlag = True
        desc = "Create the bricks for a given line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            If task.heartBeatLT Then
                lp = New lpData(New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)),
                                New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)))
                dst2.SetTo(0)
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth)
                Dim rotatedRect = cv.Cv2.MinAreaRect({lp.p1, lp.p2})
                dst2.Rectangle(rotatedRect.BoundingRect, task.highlight, task.lineWidth)
            End If
        End If

        For Each index In lp.bricks
            dst2.Rectangle(task.bricks.brickList(index).rect, task.highlight, task.lineWidth)
        Next
        labels(2) = CStr(lp.bricks.Count) + " bricks will cover the line."
    End Sub
End Class






Public Class LineRGB_BricksValidate : Inherits TaskParent
    Public lp As lpData
    Public Sub New()
        task.brickRunFlag = True
        desc = "Validate the bricks for a given line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            If task.heartBeatLT Then
                lp = New lpData(New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)),
                                New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)))
                dst2.SetTo(0)
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth)
            End If
        End If
        For Each index In lp.bricks
            dst2.Rectangle(task.bricks.brickList(index).rect, task.highlight, task.lineWidth)
        Next
        labels(2) = CStr(lp.bricks.Count) + " bricks will cover the line."
    End Sub
End Class










Public Class LineRGB_VerticalHorizontalRaw : Inherits TaskParent
    Dim verts As New LineRGB_VerticalTrig
    Dim horiz As New LineRGB_HorizontalTrig
    Public vertList As New List(Of lpData)
    Public horizList As New List(Of lpData)
    Public Sub New()
        task.gOptions.LineWidth.Value = 2
        labels(3) = "Vertical lines are in yellow and horizontal lines in red."
        desc = "Highlight both vertical and horizontal lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        verts.Run(src)
        horiz.Run(src)

        Dim vList As New SortedList(Of Integer, lpData)(New compareAllowIdenticalIntegerInverted)
        Dim hList As New SortedList(Of Integer, lpData)(New compareAllowIdenticalIntegerInverted)

        dst3.SetTo(0)
        For Each lp In verts.vertList
            vList.Add(lp.length, lp)
            DrawLine(dst2, lp.p1, lp.p2, task.highlight)
            DrawLine(dst3, lp.p1, lp.p2, task.highlight)
        Next

        For Each lp In horiz.horizList
            hList.Add(lp.length, lp)
            DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Red)
            DrawLine(dst3, lp.p1, lp.p2, cv.Scalar.Red)
        Next

        vertList = New List(Of lpData)(vList.Values)
        horizList = New List(Of lpData)(hList.Values)
        labels(2) = "Number of lines identified (vertical/horizontal): " + CStr(vList.Count) + "/" + CStr(hList.Count)
    End Sub
End Class





Public Class LineRGB_HorizontalTrig : Inherits TaskParent
    Public horizList As New List(Of lpData)
    Dim lines As New LineRGB_Basics
    Public Sub New()
        desc = "Find all the Horizontal lines with horizon vector"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lines.Run(src)
        dst2 = src

        Dim p1 = task.horizonVec.p1, p2 = task.horizonVec.p2
        Dim sideOpposite = p2.Y - p1.Y
        If p1.X = 0 Then sideOpposite = p1.Y - p2.Y
        Dim hAngle = Math.Atan(sideOpposite / dst2.Width) * 57.2958

        horizList.Clear()
        For Each lp In lines.rawLines.lpList
            If lp.p1.X > lp.p2.X Then lp = New lpData(lp.p2, lp.p1)

            sideOpposite = lp.p2.Y - lp.p1.Y
            If lp.p1.X < lp.p2.X Then sideOpposite = lp.p1.Y - lp.p2.Y
            Dim angle = Math.Atan(sideOpposite / Math.Abs(lp.p1.X - lp.p2.X)) * 57.2958

            If Math.Abs(angle - hAngle) < 2 Then
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
                horizList.Add(lp)
            End If
        Next
        labels(2) = "There are " + CStr(horizList.Count) + " lines similar to the horizon " + Format(hAngle, fmt1) + " degrees"
    End Sub
End Class




Public Class LineRGB_VerticalTrig : Inherits TaskParent
    Public vertList As New List(Of lpData)
    Dim lines As New LineRGB_Basics
    Public Sub New()
        desc = "Find all the vertical lines with gravity vector"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lines.Run(src)
        dst2 = src

        Dim p1 = task.gravityVec.p1, p2 = task.gravityVec.p2
        Dim sideOpposite = p2.X - p1.X
        If p1.Y = 0 Then sideOpposite = p1.X - p2.X
        Dim gAngle = Math.Atan(sideOpposite / dst2.Height) * 57.2958

        vertList.Clear()
        For Each lp In lines.lpList
            If lp.p1.Y > lp.p2.Y Then lp = New lpData(lp.p2, lp.p1)

            sideOpposite = lp.p2.X - lp.p1.X
            If lp.p1.Y < lp.p2.Y Then sideOpposite = lp.p1.X - lp.p2.X
            Dim angle = Math.Atan(sideOpposite / Math.Abs(lp.p1.Y - lp.p2.Y)) * 57.2958

            If Math.Abs(angle - gAngle) < 2 Then
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
                vertList.Add(lp)
            End If
        Next
        labels(2) = "There are " + CStr(vertList.Count) + " lines similar to the Gravity " + Format(gAngle, fmt1) + " degrees"
    End Sub
End Class









Public Class LineRGB_GravityToAverage : Inherits TaskParent
    Public vertList As New List(Of lpData)
    Dim kalman As New Kalman_Basics
    Dim lines As New LineRGB_Basics
    Public Sub New()
        desc = "Highlight both vertical and horizontal lines - not terribly good..."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lines.Run(src)
        Dim gravityDelta As Single = task.gravityVec.ep1.X - task.gravityVec.ep2.X

        kalman.kInput = {gravityDelta}
        kalman.Run(emptyMat)
        gravityDelta = kalman.kOutput(0)

        dst2 = src
        If standalone Then dst3 = lines.rawLines.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim deltaList As New List(Of Single)
        For Each lp In lines.rawLines.lpList
            If lp.vertical And Math.Sign(task.gravityVec.slope) = Math.Sign(lp.slope) Then
                Dim delta = lp.ep1.X - lp.ep2.X
                If Math.Abs(gravityDelta - delta) < 3 Then
                    deltaList.Add(delta)
                    vertList.Add(lp)
                    DrawLine(dst2, lp.ep1, lp.ep2, task.highlight)
                    If standalone Then DrawLine(dst3, lp.p1, lp.p2, task.highlight)
                End If
            End If
        Next

        If task.heartBeat Then
            labels(3) = "Gravity offset at image edge = " + Format(gravityDelta, fmt3) + " and m = " +
                        Format(task.gravityVec.slope, fmt3)
            If deltaList.Count > 0 Then
                labels(2) = Format(gravityDelta, fmt3) + "/" + Format(deltaList.Average(), fmt3) + " gravity delta/line average delta"
            Else
                labels(2) = "No lines matched the gravity vector..."
            End If
        End If
    End Sub
End Class









Public Class LineRGB_GravityToLongest : Inherits TaskParent
    Dim kalman As New Kalman_Basics
    Dim matchLine As New MatchLine_Basics
    Dim lines As New LineRGB_Basics
    Public Sub New()
        desc = "Highlight both vertical and horizontal lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lines.Run(src)

        Dim gravityDelta As Single = task.gravityVec.ep1.X - task.gravityVec.ep2.X

        kalman.kInput = {gravityDelta}
        kalman.Run(emptyMat)
        gravityDelta = kalman.kOutput(0)

        matchLine.lpInput = Nothing
        For Each lp In lines.rawLines.lpList
            If lp.vertical Then
                matchLine.lpInput = lp
                Exit For
            End If
        Next
        If matchLine.lpInput Is Nothing Then Exit Sub
        matchLine.Run(src)
        dst2 = matchLine.dst2
        dst3 = lines.rawLines.dst2
    End Sub
End Class







Public Class LineRGB_MatchGravity : Inherits TaskParent
    Public gLines As New List(Of lpData)
    Public Sub New()
        desc = "Find all the lines similar to gravity."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = task.lineRGB.dst3
        labels(3) = task.lineRGB.labels(3)

        gLines.Clear()
        dst2 = src.Clone
        For Each lp In task.lineRGB.lpList
            If lp.gravityProxy Then
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth + 1, task.lineType)
                gLines.Add(lp)
            End If
        Next

        If gLines.Count = 0 Then
            labels(2) = "There were no lines parallel to gravity in the RGB image."
        Else
            labels(2) = "Of the " + CStr(gLines.Count) + " lines found, the best line parallel to gravity was " +
                       CStr(CInt(gLines(0).length)) + " pixels in length."
        End If
    End Sub
End Class






Public Class LineRGB_Match : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public lpLastList As New List(Of lpData)
    Dim rawLines As New LineRGB_Raw
    Dim match As New Match_Line
    Public Sub New()
        task.brickRunFlag = True
        desc = "Retain earlier lines based on the end point correlations.  Line can be partially hidden but is still be there."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then
            lpLastList.Clear()
            lpList.Clear()
            task.motionMask.SetTo(255)
        End If

        lpList.Clear()
        For Each lp In lpLastList
            match.lpInput = lp
            match.Run(src)
            lp.correlation = match.correlation
            If lp.correlation > 0.98 Then lpList.Add(lp)
        Next

        If lpList.Count < task.FeatureSampleSize Then
            Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
            For Each lp In lpLastList
                Dim noMotionTest As Boolean = True
                For Each index In lp.bricks
                    Dim pt = task.bricks.brickList(index).rect.TopLeft
                    If task.motionMask.Get(Of Byte)(pt.Y, pt.X) Then
                        noMotionTest = False
                        Exit For
                    End If
                Next
                If noMotionTest And lp.bricks.Count > 1 Then
                    lp.age += 1
                    sortlines.Add(lp.length, lp)
                End If
            Next

            rawLines.Run(src)
            dst3 = rawLines.dst2
            labels(3) = rawLines.labels(2)

            For Each lp In rawLines.lpList
                Dim motionTest As Boolean = False

                For Each index In lp.bricks
                    Dim pt = task.bricks.brickList(index).rect.TopLeft
                    If task.motionMask.Get(Of Byte)(pt.Y, pt.X) Then
                        motionTest = True
                        Exit For
                    End If
                Next
                If motionTest Then sortlines.Add(lp.length, lp)
            Next

            lpList.Clear()
            For Each lp In sortlines.Values
                lp.index = lpList.Count
                lpList.Add(lp)
                If standaloneTest() Then
                    For i As Integer = 0 To 3
                        dst2.Line(lp.vertices(i), lp.vertices((i + 1) Mod 4), task.highlight, task.lineWidth)
                    Next
                End If
                If lpList.Count >= task.FeatureSampleSize * 2 Then Exit For
            Next

            dst2 = src
            For Each lp In lpList
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
            Next
        End If

        lpLastList = New List(Of lpData)(lpList)
        labels(2) = "Of the " + CStr(rawLines.lpList.Count) + " raw lines found, shown below are the " + CStr(lpList.Count) + " longest."
    End Sub
End Class
