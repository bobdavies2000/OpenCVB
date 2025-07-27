Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Line_Basics : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public lpRectMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public lpMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public rawLines As New Line_Raw
    Dim lineAges As New Line_OrderByAge
    Public Sub New()
        desc = "Retain line from earlier image if not in motion mask.  If new line is in motion mask, add it."
    End Sub
    Private Function lpMotionCheck(lp As lpData) As Boolean
        Dim gridIndex As Integer, pt As cv.Point

        gridIndex = task.grid.gridMap.Get(Of Single)(lp.p1.Y, lp.p1.X)
        pt = task.gridRects(gridIndex).TopLeft
        If task.motionMask.Get(Of Byte)(pt.Y, pt.X) Then Return False

        gridIndex = task.grid.gridMap.Get(Of Single)(lp.p2.Y, lp.p2.X)
        pt = task.gridRects(gridIndex).TopLeft
        If task.motionMask.Get(Of Byte)(pt.Y, pt.X) Then Return False
        Return True
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.algorithmPrep = False Then Exit Sub ' only run as a task algorithm.
        If task.optionsChanged Then
            lpList.Clear()
            task.motionMask.SetTo(255)
        End If

        Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
        For Each lp In lpList
            If lpMotionCheck(lp) Then
                lp.age += 1
                sortlines.Add(lp.length, lp)
            End If
        Next

        rawLines.Run(task.grayStable)
        dst3 = rawLines.dst2
        labels(3) = rawLines.labels(2)

        For Each lp In rawLines.lpList
            If lpMotionCheck(lp) = False Then sortlines.Add(lp.length, lp)
        Next

        lpList.Clear()
        For Each lp In sortlines.Values
            lp.index = lpList.Count

            lpList.Add(lp)
            If lpList.Count >= task.FeatureSampleSize Then Exit For
        Next

        lineAges.Run(task.color.Clone)
        dst2 = lineAges.dst2
        If standaloneTest() Then
            dst3.SetTo(0)
            Dim count As Integer
            For Each lp In task.lines.lpList
                If lp.vertical Then
                    ' this was inserted to verify gravity proxy while debugging.  It is not technically needed (lpData sets gravityProxy.)
                    Dim deltaX1 = Math.Abs(task.gravityIMU.ep1.X - lp.ep1.X)
                    Dim deltaX2 = Math.Abs(task.gravityIMU.ep2.X - lp.ep2.X)
                    If Math.Sign(deltaX1) = Math.Sign(deltaX2) Then
                        lp.gravityProxy = Math.Abs(deltaX1 - deltaX2) < task.gravityBasics.options.pixelThreshold
                    End If
                End If

                If lp.gravityProxy Then
                    DrawLine(dst3, lp, white)
                    SetTrueText("Age: " + CStr(lp.age) + vbCrLf, lp.center)
                    count += 1
                End If
            Next
            labels(3) = CStr(count) + " lines are proxies for gravity."
        End If

        lpMap.SetTo(0)
        lpRectMap.SetTo(0)
        For i = lpList.Count - 1 To 0 Step -1
            lpRectMap.Rectangle(lpList(i).rect, i + 1, -1)
            lpMap.Line(lpList(i).p1, lpList(i).p2, lpList(i).index + 1, task.lineWidth, cv.LineTypes.Link8)
        Next

        labels(2) = "The " + CStr(lpList.Count) + " longest lines of the " + CStr(rawLines.lpList.Count)
    End Sub
End Class





Public Class Line_Raw : Inherits TaskParent
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
                    p1 = lpData.validatePoint(p1)
                    p2 = lpData.validatePoint(p2)
                    Dim lp = New lpData(p1, p2)
                    lpList.Add(lp)
                End If
            End If
        Next

        dst2.SetTo(0)
        For Each lp In lpList
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next

        labels(2) = CStr(lpList.Count) + " highlighted lines were detected in the current frame. Others were too similar."
    End Sub
End Class





Public Class Line_RawEPLines : Inherits TaskParent
    Dim ld As cv.XImgProc.FastLineDetector
    Public lpList As New List(Of lpData)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset " +
               "rectangle (provided externally)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If src.Type <> cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_8U)

        Dim lines = ld.Detect(src)
        Dim tmplist As New List(Of lpData)
        dst3.SetTo(0)
        For Each v In lines
            If v(0) >= 0 And v(0) <= src.Cols And v(1) >= 0 And v(1) <= src.Rows And
               v(2) >= 0 And v(2) <= src.Cols And v(3) >= 0 And v(3) <= src.Rows Then
                Dim p1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim p2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                If p1.X >= 0 And p1.X < dst2.Width And p1.Y >= 0 And p1.Y < dst2.Height And
                   p2.X >= 0 And p2.X < dst2.Width And p2.Y >= 0 And p2.Y < dst2.Height Then
                    p1 = lpData.validatePoint(p1)
                    p2 = lpData.validatePoint(p2)
                    Dim lp = New lpData(p1, p2)
                    lp.index = tmplist.Count
                    tmplist.Add(lp)
                    DrawLine(dst3, lp, white)
                End If
            End If
        Next

        Dim removeList As New List(Of Integer)
        For Each lp In tmplist
            Dim x1 = CInt(lp.ep1.X)
            Dim y1 = CInt(lp.ep1.Y)
            Dim x2 = CInt(lp.ep2.X)
            Dim y2 = CInt(lp.ep2.Y)
            For j = lp.index + 1 To tmplist.Count - 1
                If CInt(tmplist(j).ep1.X) <> x1 Then Continue For
                If CInt(tmplist(j).ep1.Y) <> y1 Then Continue For
                If CInt(tmplist(j).ep2.X) <> x2 Then Continue For
                If CInt(tmplist(j).ep2.Y) <> y2 Then Continue For
                If removeList.Contains(tmplist(j).index) = False Then removeList.Add(tmplist(j).index)
            Next
        Next

        lpList.Clear()
        For Each lp In tmplist
            If removeList.Contains(lp.index) = False Then lpList.Add(New lpData(lp.ep1, lp.ep2))
        Next

        dst2.SetTo(0)
        For Each lp In lpList
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth + 1, task.lineType)
        Next

        labels(2) = CStr(lpList.Count) + " highlighted lines were detected in the current frame. Others were too similar."
        labels(3) = "There were " + CStr(removeList.Count) + " coincident lines"
    End Sub
End Class






Public Class Line_BasicsNoAging : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public lpRectMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public rawLines As New Line_Raw
    Public Sub New()
        desc = "Retain line from earlier image if not in motion mask.  If new line is in motion mask, add it."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then
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
        lpRectMap.SetTo(0)
        For Each lp In sortlines.Values
            lpList.Add(lp)
            DrawLine(dst2, lp.p1, lp.p2)
            lpRectMap.Line(lp.p1, lp.p2, sortlines.Values.IndexOf(lp) + 1, task.lineWidth * 3, cv.LineTypes.Link8)

            If standaloneTest() Then
                dst2.Line(lp.p1, lp.p2, task.highlight, 10, cv.LineTypes.Link8)
            End If
            If lpList.Count >= task.FeatureSampleSize Then Exit For
        Next

        If standaloneTest() Then dst1 = ShowPalette(lpRectMap)
        labels(2) = "Of the " + CStr(rawLines.lpList.Count) + " raw lines found, shown below are the " + CStr(lpList.Count) + " longest."
    End Sub
End Class








Public Class Line_RawSorted : Inherits TaskParent
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
            lp.p1 = lpData.validatePoint(lp.p1)
            lp.p2 = lpData.validatePoint(lp.p2)
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









Public Class Line_Intercepts : Inherits TaskParent
    Public extended As New Line_ExtendLineTest
    Public p1List As New List(Of cv.Point2f)
    Public p2List As New List(Of cv.Point2f)
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

        If task.lines.lpList.Count = 0 Then Exit Sub

        dst2 = src
        p1List.Clear()
        p2List.Clear()
        intercept = interceptArray(options.selectedIntercept)
        topIntercepts.Clear()
        botIntercepts.Clear()
        leftIntercepts.Clear()
        rightIntercepts.Clear()
        Dim index As Integer
        For Each lp In task.lines.lpList
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











Public Class Line_Perpendicular : Inherits TaskParent
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
        Dim p2 = New cv.Point2f((task.workRes.Height - b) / m, task.workRes.Height)

        Dim w = task.workRes.Width
        Dim h = task.workRes.Height

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
        If standaloneTest() Then input = task.lineGravity
        dst2.SetTo(0)
        DrawLine(dst2, input.p1, input.p2, white)

        output = computePerp(input)
        DrawCircle(dst2, midPoint, task.DotSize + 2, cv.Scalar.Red)
        DrawLine(dst2, output.p1, output.p2, cv.Scalar.Yellow)
    End Sub
End Class






Public Class Line_Info : Inherits TaskParent
    Public Sub New()
        labels(3) = "The selected line with details."
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Display details about the line selected."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        labels(2) = task.lines.labels(2) + " - Use the global option 'DebugSlider' to select a line."

        If task.lines.lpList.Count <= 1 Then Exit Sub
        If standaloneTest() Then
            dst2.SetTo(0)
            For Each lp In task.lines.lpList
                dst2.Line(lp.p1, lp.p2, white, task.lineWidth, cv.LineTypes.Link8)
                DrawCircle(dst2, lp.p1, task.DotSize, task.highlight)
            Next
        End If

        'Dim lpIndex = Math.Abs(task.gOptions.DebugSlider.Value)
        'If lpIndex < task.lines.lpList.Count Then task.lpD = task.lines.lpList(lpIndex)

        'strOut = "Use the global options 'DebugSlider' to select the line for display " + vbCrLf + vbCrLf
        'strOut += CStr(task.lines.lpList.Count) + " lines found " + vbCrLf + vbCrLf

        dst2.Line(task.lpD.p1, task.lpD.p2, task.highlight, task.lineWidth + 1, task.lineType)

        Dim index = task.lpD.index
        strOut += "Line ID = " + CStr(index) + vbCrLf + vbCrLf
        strOut += "index = " + CStr(index) + vbCrLf
        strOut += "Age = " + CStr(task.lpD.age) + vbCrLf

        strOut += "p1 = " + task.lpD.p1.ToString + ", p2 = " + task.lpD.p2.ToString + vbCrLf + vbCrLf
        strOut += "Slope = " + Format(task.lpD.slope, fmt3) + vbCrLf
        strOut += vbCrLf + "NOTE: the Y-Axis is inverted - Y increases down so slopes are inverted." + vbCrLf + vbCrLf

        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class Line_ViewLeftRight : Inherits TaskParent
    Dim lines As New Line_Basics
    Dim linesRaw As New Line_RawSorted
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        desc = "Find lines in the left and right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lines.Run(task.leftView)
        dst2.SetTo(0)
        For Each lp In task.lines.lpList
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth)
        Next
        labels(2) = lines.labels(2)

        linesRaw.Run(task.rightView)
        dst3 = linesRaw.dst2
        labels(3) = linesRaw.labels(2)
    End Sub
End Class







Public Class Line_GCloud : Inherits TaskParent
    Public sortedVerticals As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
    Public sortedHorizontals As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
    Public allLines As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
    Public options As New Options_LineFinder
    Dim match As New Match_tCell
    Dim angleSlider As System.Windows.Forms.TrackBar
    Dim lines As New Line_RawSorted
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
            Dim brick As New gravityLine
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
Public Class Line_Intersection : Inherits TaskParent
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






Public Class Line_VerticalHorizontalRaw : Inherits TaskParent
    Dim verts As New Line_TrigVertical
    Dim horiz As New Line_TrigHorizontal
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





Public Class Line_TrigHorizontal : Inherits TaskParent
    Public horizList As New List(Of lpData)
    Public Sub New()
        desc = "Find all the Horizontal lines with horizon vector"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src

        Dim p1 = task.lineHorizon.p1, p2 = task.lineHorizon.p2
        Dim sideOpposite = p2.Y - p1.Y
        If p1.X = 0 Then sideOpposite = p1.Y - p2.Y
        Dim hAngle = Math.Atan(sideOpposite / dst2.Width) * 57.2958

        horizList.Clear()
        For Each lp In task.lines.lpList
            If lp.p1.X > lp.p2.X Then lp = New lpData(lp.p2, lp.p1)

            sideOpposite = lp.p2.Y - lp.p1.Y
            If lp.p1.X < lp.p2.X Then sideOpposite = lp.p1.Y - lp.p2.Y
            Dim angle = Math.Atan(sideOpposite / Math.Abs(lp.p1.X - lp.p2.X)) * 57.2958

            If Math.Abs(angle - hAngle) < 2 Then
                DrawLine(dst2, lp.p1, lp.p2)
                horizList.Add(lp)
            End If
        Next
        labels(2) = "There are " + CStr(horizList.Count) + " lines similar to the horizon " + Format(hAngle, fmt1) + " degrees"
    End Sub
End Class




Public Class Line_TrigVertical : Inherits TaskParent
    Public vertList As New List(Of lpData)
    Public Sub New()
        desc = "Find all the vertical lines with gravity vector"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src

        Dim p1 = task.lineGravity.p1, p2 = task.lineGravity.p2
        Dim sideOpposite = p2.X - p1.X
        If p1.Y = 0 Then sideOpposite = p1.X - p2.X
        Dim gAngle = Math.Atan(sideOpposite / dst2.Height) * 57.2958

        vertList.Clear()
        For Each lp In task.lines.rawLines.lpList
            If lp.p1.Y > lp.p2.Y Then lp = New lpData(lp.p2, lp.p1)

            sideOpposite = lp.p2.X - lp.p1.X
            If lp.p1.Y < lp.p2.Y Then sideOpposite = lp.p1.X - lp.p2.X
            Dim angle = Math.Atan(sideOpposite / Math.Abs(lp.p1.Y - lp.p2.Y)) * 57.2958

            If Math.Abs(angle - gAngle) < 2 Then
                DrawLine(dst2, lp.p1, lp.p2)
                vertList.Add(lp)
            End If
        Next
        labels(2) = "There are " + CStr(vertList.Count) + " lines similar to the Gravity " + Format(gAngle, fmt1) + " degrees"
    End Sub
End Class









Public Class Line_GravityToAverage : Inherits TaskParent
    Public vertList As New List(Of lpData)
    Public Sub New()
        desc = "Highlight both vertical and horizontal lines - not terribly good..."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim gravityDelta As Single = task.lineGravity.ep1.X - task.lineGravity.ep2.X

        dst2 = src
        If standalone Then dst3 = task.lines.dst2
        Dim deltaList As New List(Of Single)
        vertList.Clear()
        For Each lp In task.lines.rawLines.lpList
            If lp.vertical And Math.Sign(task.lineGravity.slope) = Math.Sign(lp.slope) Then
                Dim delta = lp.ep1.X - lp.ep2.X
                If Math.Abs(gravityDelta - delta) < task.gravityBasics.options.pixelThreshold Then
                    deltaList.Add(delta)
                    vertList.Add(lp)
                    DrawLine(dst2, lp.ep1, lp.ep2)
                    If standalone Then DrawLine(dst3, lp.p1, lp.p2, task.highlight)
                End If
            End If
        Next

        If task.heartBeat Then
            labels(3) = "Gravity offset at image edge = " + Format(gravityDelta, fmt3) + " and m = " +
                        Format(task.lineGravity.slope, fmt3)
            If deltaList.Count > 0 Then
                labels(2) = Format(gravityDelta, fmt3) + "/" + Format(deltaList.Average(), fmt3) + " gravity delta/line average delta"
            Else
                labels(2) = "No lines matched the gravity vector..."
            End If
        End If
    End Sub
End Class









Public Class Line_GravityToLongest : Inherits TaskParent
    Dim kalman As New Kalman_Basics
    Dim matchLine As New MatchLine_Basics
    Public Sub New()
        desc = "Highlight both vertical and horizontal lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim gravityDelta As Single = task.lineGravity.ep1.X - task.lineGravity.ep2.X

        kalman.kInput = {gravityDelta}
        kalman.Run(emptyMat)
        gravityDelta = kalman.kOutput(0)

        matchLine.lpInput = Nothing
        For Each lp In task.lines.rawLines.lpList
            If lp.vertical Then
                matchLine.lpInput = lp
                Exit For
            End If
        Next
        If matchLine.lpInput Is Nothing Then Exit Sub
        matchLine.Run(src)
        dst2 = matchLine.dst2
        dst3 = task.lines.rawLines.dst2
    End Sub
End Class







Public Class Line_MatchGravity : Inherits TaskParent
    Public gLines As New List(Of lpData)
    Public Sub New()
        desc = "Find all the lines similar to gravity."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = task.lines.dst3
        labels(3) = task.lines.labels(3)

        gLines.Clear()
        dst2 = src.Clone
        For Each lp In task.lines.lpList
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





Public Class Line_OrderByAge : Inherits TaskParent
    Dim lpListAge As New List(Of lpData)
    Public Sub New()
        desc = "Show the lines which have been around the longest"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim sortAge As New SortedList(Of Integer, lpData)(New compareAllowIdenticalIntegerInverted)
        For Each lp In task.lines.lpList
            sortAge.Add(lp.age, lp)
        Next

        lpListAge.Clear()
        dst2 = src
        Dim count As Integer
        For Each lp In sortAge.Values
            DrawLine(dst2, lp.p1, lp.p2)
            If lp.gravityProxy Then count += 1
            If task.toggleOn Then
                If lp.gravityProxy Then SetTrueText("Age: " + CStr(lp.age), lp.p1)
            Else
                If lp.gravityProxy Then SetTrueText("Age: " + CStr(lp.age), lp.p2)
            End If
            lpListAge.Add(lp)
        Next

        If task.toggleOn Then
            labels(2) = CStr(sortAge.Values.Count) + " lines were found.  Below the " + CStr(count) + " marked lines are parallel to gravity (bot)"
        Else
            labels(2) = CStr(sortAge.Values.Count) + " lines were found.  Below the " + CStr(count) + " marked lines are parallel to gravity (top)"
        End If
    End Sub
End Class






Public Class Line_FindNearest : Inherits TaskParent
    Public lpInput As lpData
    Public lpOutput As lpData
    Public distance As Single
    Public Sub New()
        desc = "Find the line that is closest to the input line"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then lpInput = task.lineLongest
        Dim lpList = task.lines.lpList
        If lpList.Count = 0 Then Exit Sub

        Dim sortDistance As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
        For Each lp In lpList
            sortDistance.Add(lpInput.center.DistanceTo(lp.center), lp.index)
        Next

        lpOutput = lpList(sortDistance.ElementAt(0).Value)

        If standaloneTest() Then
            dst2 = src
            DrawLine(dst2, lpOutput.p1, lpOutput.p2)
            labels(2) = "Distance = " + Format(sortDistance.ElementAt(0).Key, fmt1)
            SetTrueText("Age = " + CStr(lpOutput.age), lpOutput.p1)
        End If
    End Sub
End Class






Public Class Line_KNN : Inherits TaskParent
    Dim knn As New KNN_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "For each line in the current lpList, find the nearest center in the previous lpList."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lpList = task.lines.lpList

        knn.queries.Clear()
        For Each lp In lpList
            knn.queries.Add(lp.center)
        Next

        Static lastQueries As New List(Of cv.Point2f)(knn.queries)
        Static lpLastList As New List(Of lpData)(lpList)
        knn.trainInput = New List(Of cv.Point2f)(lastQueries)

        knn.Run(emptyMat)

        For i = 0 To lpList.Count - 1
            Dim lp = lpList(i)
            Dim index = knn.neighbors(i)(0)
            Dim lpLast = lpLastList(index)

            If lp.center.DistanceTo(lpLast.center) < 3 Then lp.age = lpLast.age + 1
        Next

        lastQueries = New List(Of cv.Point2f)(knn.queries)
        lpLastList = New List(Of lpData)(lpList)

        dst3.SetTo(0)
        For Each lp In lpList
            dst3.Line(lp.p1, lp.p2, task.scalarColors(lp.index), task.lineWidth * 3, cv.LineTypes.Link8)
            SetTrueText(CStr(lp.age), lp.center, 3)
        Next

        dst2 = ShowPaletteNoZero(dst3)
    End Sub
End Class






Public Class Line_Points : Inherits TaskParent
    Dim knn As New KNN_Basics
    Public Sub New()
        desc = "Display end points of the lines and map them."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lines.dst2

        knn.queries.Clear()
        For Each lp In task.lines.lpList
            Dim rect = task.gridNabeRects(task.grid.gridMap.Get(Of Single)(lp.p1.Y, lp.p1.X))
            dst2.Rectangle(rect, task.highlight, task.lineWidth)
            knn.queries.Add(lp.center)
        Next

        Static lastQueries As New List(Of cv.Point2f)(knn.queries)
        knn.trainInput = lastQueries


        knn.Run(emptyMat)

        dst3 = task.lines.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        For i = 0 To knn.neighbors.Count - 1
            Dim p1 = knn.queries(i)
            Dim p2 = knn.trainInput(knn.neighbors(i)(0))
            dst3.Line(p1, p2, task.highlight, task.lineWidth + 3, task.lineType)
        Next

        lastQueries = New List(Of cv.Point2f)(knn.queries)
    End Sub
End Class







Public Class Line_RawSubset : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public subsetRect As cv.Rect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
    Public rawLines As New Line_Raw
    Public Sub New()
        task.drawRect = New cv.Rect(25, 25, 25, 25)
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset " +
               "rectangle (provided externally)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then subsetRect = task.drawRect
        rawLines.Run(src(subsetRect))

        lpList.Clear()
        dst2 = task.lines.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        For Each lp In rawLines.lpList
            dst2(subsetRect).Line(lp.p1, lp.p2, task.highlight, task.lineWidth * 3, task.lineType)
            lpList.Add(lp)
        Next
        labels(2) = CStr(lpList.Count) + " lines were detected in src(subsetRect)"
    End Sub
End Class







Public Class Line_Grid : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public rawLines As New Line_Raw
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "find the lines in each grid rectangle"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = src
        dst2.SetTo(0)
        For Each rect In task.gridNabeRects
            rawLines.Run(src(rect))
            For Each lp In rawLines.lpList
                dst2(rect).Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
                DrawLine(dst3, lp.p1, lp.p2)
                lpList.Add(lp)
            Next
        Next
    End Sub
End Class






Public Class Line_Parallel : Inherits TaskParent
    Dim extendAll As New Line_ExtendAll
    Dim knn As New KNN_Basics
    Public parList As New List(Of coinPoints)
    Public Sub New()
        labels = {"", "", "Image output from Line_Core", "Parallel extended lines"}
        desc = "Use KNN to find which lines are near each other and parallel"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        extendAll.Run(src)
        dst3 = extendAll.dst2

        knn.queries.Clear()
        For Each lp In extendAll.lpList
            knn.queries.Add(New cv.Point2f((lp.p1.X + lp.p2.X) / 2, (lp.p1.Y + lp.p2.Y) / 2))
        Next
        knn.trainInput = New List(Of cv.Point2f)(knn.queries)

        If knn.queries.Count = 0 Then Exit Sub ' no input...possible in a dark room...

        knn.Run(src)
        dst2 = src.Clone
        parList.Clear()
        Dim checkList As New List(Of cv.Point)
        For i = 0 To knn.result.GetUpperBound(0) - 1
            For j = 0 To knn.queries.Count - 1
                Dim index = knn.result(i, j)
                If index >= extendAll.lpList.Count Or index < 0 Then Continue For
                Dim lp = extendAll.lpList(index)
                Dim elp = extendAll.lpList(i)
                Dim mid = knn.queries(i)
                Dim near = knn.trainInput(index)
                Dim distanceMid = mid.DistanceTo(near)
                Dim distance1 = lp.p1.DistanceTo(elp.p1)
                Dim distance2 = lp.p2.DistanceTo(elp.p2)
                If distance1 > distanceMid * 2 Then
                    distance1 = lp.p1.DistanceTo(elp.p2)
                    distance2 = lp.p2.DistanceTo(elp.p1)
                End If
                If distance1 < distanceMid * 2 And distance2 < distanceMid * 2 Then
                    Dim cp As coinPoints

                    Dim mps = task.lines.lpList(index)
                    cp.p1 = mps.p1
                    cp.p2 = mps.p2

                    mps = task.lines.lpList(i)
                    cp.p3 = mps.p1
                    cp.p4 = mps.p2

                    If checkList.Contains(cp.p1) = False And checkList.Contains(cp.p2) = False And checkList.Contains(cp.p3) = False And checkList.Contains(cp.p4) = False Then
                        If (cp.p1 = cp.p3 Or cp.p1 = cp.p4) And (cp.p2 = cp.p3 Or cp.p2 = cp.p4) Then
                            ' duplicate points...
                        Else
                            DrawLine(dst2, cp.p1, cp.p2, task.highlight)
                            DrawLine(dst2, cp.p3, cp.p4, cv.Scalar.Red)
                            parList.Add(cp)
                            checkList.Add(cp.p1)
                            checkList.Add(cp.p2)
                            checkList.Add(cp.p3)
                            checkList.Add(cp.p4)
                        End If
                    End If
                End If
            Next
        Next
        labels(2) = CStr(parList.Count) + " parallel lines were found in the image"
        labels(3) = CStr(extendAll.lpList.Count) + " lines were found in the image before finding the parallel lines"
    End Sub
End Class




Public Class Line_ExtendAll : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public Sub New()
        labels = {"", "", "Image output from Line_Core", "The extended line for each line found in Line_Core"}
        desc = "Create a list of all the extended lines in an image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lines.dst2

        dst3 = src.Clone
        lpList.Clear()
        For Each lp In task.lines.lpList
            DrawLine(dst3, lp.ep1, lp.ep2, task.highlight)
            lpList.Add(New lpData(lp.ep1, lp.ep2))
        Next
    End Sub
End Class






Public Class Line_ExtendLineTest : Inherits TaskParent
    Public Sub New()
        labels = {"", "", "Random Line drawn", ""}
        desc = "Test lpData constructor with random values to make sure lines are extended properly"
    End Sub

    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            Dim p1 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            Dim p2 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))

            Dim lp = New lpData(p1, p2)
            dst2 = src
            DrawLine(dst2, lp.ep1, lp.ep2, task.highlight)
            DrawCircle(dst2, p1, task.DotSize + 2, cv.Scalar.Red)
            DrawCircle(dst2, p2, task.DotSize + 2, cv.Scalar.Red)
        End If
    End Sub
End Class






Public Class Line_Gravity : Inherits TaskParent
    Dim match As New Match_Basics
    Public lp As lpData
    Public Sub New()
        desc = "Find the longest RGB line that is parallel to gravity"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lplist = task.lines.lpList
        If lplist.Count = 0 Then
            SetTrueText("There are no lines present in the image.", 3)
            Exit Sub
        End If

        ' camera is often warming up for the first few images.
        If match.correlation < task.fCorrThreshold Or task.frameCount < 10 Or lp Is Nothing Then
            lp = lplist(0)
            For Each lp In lplist
                If lp.gravityProxy Then Exit For
            Next
            match.template = src(lp.rect)
        End If

        If lp.gravityProxy = False Then
            lp = Nothing
            Exit Sub
        End If

        match.Run(src.Clone)

        If match.correlation < task.fCorrThreshold Then
            If lplist.Count > 1 Then
                Dim histogram As New cv.Mat
                cv.Cv2.CalcHist({task.lines.lpMap(lp.rect)}, {0}, emptyMat, histogram, 1, {lplist.Count},
                                 New cv.Rangef() {New cv.Rangef(1, lplist.Count)})

                Dim histArray(histogram.Total - 1) As Single
                Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

                Dim histList = histArray.ToList
                ' pick the lp that has the most pixels in the lp.rect.
                lp = lplist(histList.IndexOf(histList.Max))
                match.template = src(lp.rect)
                match.correlation = 1
            Else
                match.correlation = 0 ' force a restart
            End If
        Else
            Dim deltaX = match.newRect.X - lp.rect.X
            Dim deltaY = match.newRect.Y - lp.rect.Y
            Dim p1 = New cv.Point(lp.p1.X + deltaX, lp.p1.Y + deltaY)
            Dim p2 = New cv.Point(lp.p2.X + deltaX, lp.p2.Y + deltaY)
            lp = New lpData(p1, p2)
        End If

        If standaloneTest() Then
            dst2 = src
            dst2.Rectangle(lp.rect, task.highlight, task.lineWidth)
            DrawLine(dst2, lp.p1, lp.p2)
        End If

        labels(2) = "Selected line has a correlation of " + Format(match.correlation, fmt3) + " with the previous frame."
    End Sub
End Class







Public Class Line_LongestTest : Inherits TaskParent
    Public match As New Match_Basics
    Dim intersect As New Line_Intersection
    Public trackPoint As cv.Point2f
    Public Sub New()
        labels(2) = "White line is the last longest line and yellow is the current perpendicular to the longest line."
        desc = "Identify each line in the lpMap."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lplist = task.lines.lpList
        If lplist.Count = 0 Then
            SetTrueText("There are no lines present in the image.", 3)
            Exit Sub
        End If

        Static lpLast = New lpData(task.lineLongest.ep1, task.lineLongest.ep2)
        Dim linePerp = Line_Perpendicular.computePerp(task.lineLongest)

        dst2 = src
        DrawLine(dst2, lpLast, white)
        DrawLine(dst2, linePerp, task.highlight)

        intersect.p1 = lpLast.ep1
        intersect.p2 = lpLast.ep2
        intersect.p3 = linePerp.ep1
        intersect.p4 = linePerp.ep2
        intersect.Run(emptyMat)

        If task.heartBeatLT Then dst3.SetTo(0)
        trackPoint = intersect.intersectionPoint
        DrawCircle(dst3, trackPoint)
        DrawCircle(dst3, trackPoint)

        lpLast = New lpData(task.lineLongest.ep1, task.lineLongest.ep2)
    End Sub
End Class






Public Class Line_Matching : Inherits TaskParent
    Public match As New Match_Basics
    Public Sub New()
        desc = "For each line from the last frame, find its correlation to the current frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim correlations As New List(Of Single)
        Static lpLast = New List(Of lpData)(task.lines.lpList)
        For Each lp In lpLast
            match.template = task.gray(lp.rect)
            match.Run(task.gray.Clone)
            correlations.Add(match.correlation)
        Next

        dst2 = task.lines.dst2

        labels(2) = "Mean correlation of all the lines is " + Format(correlations.Average, fmt3)
        labels(3) = "Min/Max correlation = " + Format(correlations.Min, fmt3) + "/" + Format(correlations.Max, fmt3)
        lpLast = New List(Of lpData)(task.lines.lpList)
    End Sub
End Class









Public Class Line_Longest : Inherits TaskParent
    Public match As New Match_Basics
    Public deltaX As Single, deltaY As Single
    Dim lp As New lpData
    Public Sub New()
        desc = "Identify each line in the lpMap."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lplist = task.lines.lpList
        If lplist.Count = 0 Then
            SetTrueText("There are no lines present in the image.", 3)
            Exit Sub
        End If

        If match.correlation < task.fCorrThreshold Then Debug.WriteLine("last correlation too low.")

        ' camera is often warming up for the first few images.
        If match.correlation < task.fCorrThreshold Or task.frameCount < 10 Or task.heartBeat Then
            lp = lplist(0)
            match.template = task.gray(lp.rect)
        End If

        match.Run(task.gray.Clone)
        If match.correlation < task.fCorrThreshold Then
            Debug.WriteLine("curr correlation too low at " + Format(match.correlation, fmt3))
        End If

        If match.correlation < task.fCorrThreshold Then
            If lplist.Count > 1 Then
                Dim histogram As New cv.Mat
                cv.Cv2.CalcHist({task.lines.lpMap(lp.rect)}, {0}, emptyMat, histogram, 1, {lplist.Count},
                                 New cv.Rangef() {New cv.Rangef(1, lplist.Count)})

                Dim histArray(histogram.Total - 1) As Single
                Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

                Dim histList = histArray.ToList
                ' pick the lp that has the most pixels in the lp.rect.
                lp = lplist(histList.IndexOf(histList.Max))
                match.template = task.gray(lp.rect)
                match.correlation = 1
            Else
                Debug.WriteLine("Only 1 line present or less in the image..")
                match.correlation = 0 ' force a restart
            End If
        Else
            deltaX = match.newRect.X - lp.rect.X
            deltaY = match.newRect.Y - lp.rect.Y
            Dim p1 = New cv.Point(lp.p1.X + deltaX, lp.p1.Y + deltaY)
            Dim p2 = New cv.Point(lp.p2.X + deltaX, lp.p2.Y + deltaY)
            lp = New lpData(p1, p2)
        End If

        If standaloneTest() Then
            dst2 = src
            DrawLine(dst2, lp)
            DrawRect(dst2, lp.rect)
            dst3 = task.lines.dst2
        End If

        task.lineLongest = lp
        labels(2) = "Selected line has a correlation of " + Format(match.correlation, fmt3) + " with the previous frame."
    End Sub
End Class