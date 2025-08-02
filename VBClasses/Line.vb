Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Line_Basics : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public lpRectMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public lpMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public rawLines As New Line_Raw
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
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
            If lpMotionCheck(lp) Then sortlines.Add(lp.length, lp)
        Next

        rawLines.Run(task.grayStable)
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

        lpMap.SetTo(0)
        lpRectMap.SetTo(0)
        For i = lpList.Count - 1 To 0 Step -1
            lpRectMap.Rectangle(lpList(i).rect, i + 1, -1)
            lpMap.Line(lpList(i).p1, lpList(i).p2, lpList(i).index + 1, task.lineWidth, cv.LineTypes.Link8)
        Next

        If standaloneTest() Then dst2 = ShowPaletteNoZero(lpMap)
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
    Public Sub Close()
        ld.Dispose()
    End Sub
End Class








Public Class Line_PerpendicularTest : Inherits TaskParent
    Public input As lpData
    Public output As lpData
    Dim midPoint As cv.Point2f
    Public Sub New()
        labels = {"", "", "White is the line selected for display and yellow is perpendicular line", ""}
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
        If standalone Then task.lpD = task.lineGravity
        labels(2) = task.lines.labels(2) + " - Use the global option 'DebugSlider' to select a line."

        If task.lines.lpList.Count <= 1 Then Exit Sub
        If standaloneTest() Then
            dst2.SetTo(0)
            For Each lp In task.lines.lpList
                dst2.Line(lp.p1, lp.p2, white, task.lineWidth, cv.LineTypes.Link8)
                DrawCircle(dst2, lp.p1, task.DotSize, task.highlight)
            Next
        End If

        dst2.Line(task.lpD.p1, task.lpD.p2, task.highlight, task.lineWidth + 1, task.lineType)

        Dim index = task.lpD.index
        strOut = ""
        strOut += "Line ID = " + CStr(index) + vbCrLf + vbCrLf
        strOut += "index = " + CStr(index) + vbCrLf

        strOut += "p1 = " + task.lpD.p1.ToString + ", p2 = " + task.lpD.p2.ToString + vbCrLf + vbCrLf
        strOut += "Angle = " + CStr(task.lpD.angle) + vbCrLf
        strOut += "Slope = " + Format(task.lpD.slope, fmt3) + vbCrLf
        strOut += vbCrLf + "NOTE: the Y-Axis is inverted - Y increases down so slopes are inverted." + vbCrLf + vbCrLf

        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class Line_GCloud : Inherits TaskParent
    Public sortedVerticals As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
    Public sortedHorizontals As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
    Public allLines As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
    Public options As New Options_LineFinder
    Dim match As New Match_tCell
    Dim angleSlider As System.Windows.Forms.TrackBar
    Dim lines As New Line_Raw
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
    Public lp1 As lpData, lp2 As lpData
    Public intersectionPoint As cv.Point2f
    Public Sub New()
        desc = "Determine if 2 lines intersect, where the point is, and if that point is in the image."
    End Sub
    Public Shared Function IntersectTest(p1 As cv.Point2f, p2 As cv.Point2f, p3 As cv.Point2f, p4 As cv.Point2f) As cv.Point2f
        Dim x = p3 - p1
        Dim d1 = p2 - p1
        Dim d2 = p4 - p3
        Dim cross = d1.X * d2.Y - d1.Y * d2.X
        If Math.Abs(cross) < 0.000001 Then Return New cv.Point2f
        Dim t1 = (x.X * d2.Y - x.Y * d2.X) / cross
        Dim pt = p1 + d1 * t1
        Return pt
    End Function
    Public Shared Function IntersectTest(lp1 As lpData, lp2 As lpData) As cv.Point2f
        Dim x = lp2.p1 - lp1.p1
        Dim d1 = lp1.p2 - lp1.p1
        Dim d2 = lp2.p2 - lp2.p1
        Dim cross = d1.X * d2.Y - d1.Y * d2.X
        If Math.Abs(cross) < 0.000001 Then Return New cv.Point2f
        Dim t1 = (x.X * d2.Y - x.Y * d2.X) / cross
        Dim pt = lp1.p1 + d1 * t1
        Return pt
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            If task.heartBeat Then
                lp1 = New lpData(New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)),
                             New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)))
                lp2 = New lpData(New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)),
                             New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)))
            End If
        End If

        intersectionPoint = Line_Intersection.IntersectTest(lp1, lp2)

        If standaloneTest() Then
            dst2.SetTo(0)
            dst2.Line(lp1.p1, lp1.p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
            dst2.Line(lp2.p1, lp2.p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
            If intersectionPoint <> New cv.Point2f Then
                DrawCircle(dst2, intersectionPoint, task.DotSize + 4, white)
                labels(2) = "Intersection point = " + CStr(CInt(intersectionPoint.X)) + " x " + CStr(CInt(intersectionPoint.Y))
            Else
                labels(2) = "Parallel!!!"
            End If
            If intersectionPoint.X < 0 Or intersectionPoint.X > dst2.Width Or intersectionPoint.Y < 0 Or intersectionPoint.Y > dst2.Height Then
                labels(2) += " (off screen)"
            End If
        End If
    End Sub
End Class









Public Class Line_Longest : Inherits TaskParent
    Public match As New Match_Basics
    Public deltaX As Single, deltaY As Single
    Dim lp As New lpData
    Public Sub New()
        desc = "Identify the longest line in the output of line_basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lplist = task.lines.lpList
        If lplist.Count = 0 Then
            SetTrueText("There are no lines present in the image.", 3)
            Exit Sub
        End If
        task.lineLongestChanged = False
        ' camera is often warming up for the first few images.
        If match.correlation < task.fCorrThreshold Or task.frameCount < 10 Or task.heartBeat Then
            lp = lplist(0)
            match.template = task.gray(lp.rect)
            task.lineLongestChanged = True
        End If

        match.Run(task.gray.Clone)

        If match.correlation < task.fCorrThreshold Then
            task.lineLongestChanged = True
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






Public Class Line_TraceCenter : Inherits TaskParent
    Public match As New Match_Basics
    Dim intersect As New Line_Intersection
    Public trackPoint As cv.Point2f
    Public Sub New()
        labels(2) = "White line is the last longest line and yellow is the current perpendicular to the longest line."
        desc = "Trace the center of the longest line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lplist = task.lines.lpList
        If lplist.Count = 0 Then
            SetTrueText("There are no lines present in the image.", 3)
            Exit Sub
        End If

        Static lpLast = New lpData(task.lineLongest.ep1, task.lineLongest.ep2)
        Dim linePerp = Line_PerpendicularTest.computePerp(task.lineLongest)

        dst2 = src
        DrawLine(dst2, lpLast, white)
        DrawLine(dst2, linePerp, task.highlight)

        intersect.lp1 = lpLast
        intersect.lp2 = linePerp
        intersect.Run(emptyMat)

        If task.heartBeatLT Then dst3.SetTo(0)
        trackPoint = intersect.intersectionPoint
        DrawCircle(dst3, trackPoint)
        DrawCircle(dst3, trackPoint)

        lpLast = New lpData(task.lineLongest.ep1, task.lineLongest.ep2)
    End Sub
End Class






Public Class Line_Trace : Inherits TaskParent
    Public Sub New()
        labels(2) = "Move camera to see the impact"
        desc = "Trace the longestline to visualize the line over time"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then dst2.SetTo(0)
        DrawLine(dst2, task.lineLongest, task.highlight)
        labels(2) = "Longest line is " + Format(task.lineLongest.length, fmt1) + " pixels, slope = " +
                     Format(task.lineLongest.slope, fmt1)

        Static strList = New List(Of String)
        strList.Add(labels(2))
        strOut = ""
        For Each strNext In strList
            strOut += strNext + vbCrLf
        Next

        If strList.Count > 20 Then strList.RemoveAt(0)
        SetTrueText(strOut, 3)
    End Sub
End Class






Public Class Line_Degrees : Inherits TaskParent
    Public Sub New()
        desc = "Find similar lines using the angle variable."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim degrees = task.gOptions.DebugSlider.Value
        dst2 = src
        Dim count As Integer
        For Each lp In task.lines.lpList
            If Math.Abs(lp.angle - degrees) < 2 Then
                DrawLine(dst2, lp.p1, lp.p2, task.highlight, task.lineWidth * 2)
                count += 1
            Else
                DrawLine(dst2, lp, task.highlight)
            End If
        Next

        SetTrueText("Use the debug slider to identify which lines to display (value indicates degrees.)")
        labels(2) = CStr(count) + " lines were found with angle " + CStr(degrees) + " degrees"
    End Sub
End Class





Public Class Line_Backprojection : Inherits TaskParent
    Dim backP As New BackProject_DisplayColor
    Dim lines As New Line_Raw
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        labels = {"", "", "Lines found in the back projection", "Backprojection results"}
        desc = "Find lines in the back projection"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        backP.Run(src)

        lines.Run(backP.dst2)
        labels(2) = lines.labels(2)
        dst2 = src
        dst3.SetTo(0)
        For Each lp In lines.lpList
            DrawLine(dst2, lp.p1, lp.p2, task.highlight)
            DrawLine(dst3, lp.p1, lp.p2, 255)
        Next
    End Sub
End Class








Public Class Line_LeftRight : Inherits TaskParent
    Public leftLines As New List(Of lpData)
    Public rightLines As New List(Of lpData)
    Dim lines As New Line_Raw
    Public Sub New()
        labels = {"", "", "Left image lines", "Right image lines"}
        desc = "Find the lines in the Left and Right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        leftLines = New List(Of lpData)(task.lines.lpList)
        dst2 = task.leftView.Clone
        For Each lp In leftLines
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next
        labels(2) = "There were " + CStr(leftLines.Count) + " lines found in the left view"

        lines.Run(task.rightView.Clone)
        rightLines = New List(Of lpData)(lines.lpList)
        dst3 = task.rightView.Clone
        For Each lp In rightLines
            dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next
        labels(3) = "There were " + CStr(rightLines.Count) + " lines found in the right view"
    End Sub
End Class





Public Class Line_Parallel : Inherits TaskParent
    Public classes() As List(Of Integer) ' groups of lines that are parallel
    Public unParallel As New List(Of Integer) ' lines which are not parallel
    Public Sub New()
        labels(2) = "Text shows the parallel class with 0 being unparallel."
        desc = "Identify lines that are parallel (or nearly so), perpendicular, and not parallel."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        Dim parallels As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
        For Each lp In task.lines.lpList
            parallels.Add(lp.angle, lp.index)
        Next

        ReDim classes(task.lines.lpList.Count - 1)
        Dim index As Integer, j As Integer
        unParallel.Clear()
        For i = 0 To parallels.Count - 1
            Dim lp1 = task.lines.lpList(parallels.ElementAt(i).Value)
            For j = i + 1 To parallels.Count - 1
                Dim lp2 = task.lines.lpList(parallels.ElementAt(j).Value)
                If Math.Abs(lp1.angle - lp2.angle) < 2 Then
                    If classes(index) Is Nothing Then classes(index) = New List(Of Integer)({lp1.index})
                    classes(index).Add(lp2.index)
                Else
                    Exit For
                End If
            Next
            If classes(index) Is Nothing Then unParallel.Add(lp1.index)
            If j > i + 1 Then index += 1
            i = j - 1
        Next

        dst2 = src
        Dim colorIndex As Integer = 1
        For i = 0 To classes.Count - 1
            If classes(i) Is Nothing Then Exit For
            For j = 0 To classes(i).Count - 1
                Dim lp = task.lines.lpList(classes(i).ElementAt(j))
                dst2.Line(lp.p1, lp.p2, task.scalarColors(colorIndex), task.lineWidth * 2, task.lineType)
                SetTrueText(CStr(colorIndex), lp.center)
            Next
            colorIndex += 1
        Next

        For Each index In unParallel
            Dim lp = task.lines.lpList(index)
            DrawLine(dst2, lp)
            SetTrueText("0", lp.center)
        Next

        dst3 = task.lines.dst2
        labels(3) = task.lines.labels(2)
    End Sub
End Class






Public Class Line_BrickList : Inherits TaskParent
    Public lp As lpData ' set this input
    Public lpOutput As lpData ' this is the result lp
    Public sobel As New Edge_Sobel
    Public ptList As New List(Of cv.Point)
    Public Sub New()
        dst3 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Add a bricklist to the requested lp"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            If lp Is Nothing Then lp = task.lineLongest
        End If

        Dim r = lp.rect
        dst1.SetTo(0)
        sobel.Run(task.gray)
        dst3.SetTo(0)
        lp.drawRoRectMask(dst3)
        sobel.dst2(r).CopyTo(dst1(r), dst3(r))
        DrawRect(dst1, r, black)

        Dim allPoints As New List(Of cv.Point)
        For Each rect In task.gridRects
            Dim brick = dst1(rect)
            If brick.CountNonZero = 0 Then Continue For
            Dim mm = GetMinMax(brick)
            Dim pt = New cv.Point(mm.maxLoc.X + rect.X, mm.maxLoc.Y + rect.Y)
            If mm.maxVal = 255 Then allPoints.Add(pt)
        Next

        ptList.Clear()
        Dim angles As New List(Of Single)
        Dim epListX1 As New List(Of Single)
        Dim epListY1 As New List(Of Single)
        Dim epListX2 As New List(Of Single)
        Dim epListY2 As New List(Of Single)
        For i = 0 To allPoints.Count - 1
            Dim pt = allPoints(i)
            For j = i + 1 To allPoints.Count - 1
                Dim lpTest = New lpData(pt, allPoints(j))
                If Math.Abs(lp.angle - lpTest.angle) < 2 Then
                    angles.Add(lpTest.angle)
                    ptList.Add(pt)
                    ptList.Add(allPoints(j))
                    epListX1.Add(lpTest.ep1.X)
                    epListY1.Add(lpTest.ep1.Y)
                    epListX2.Add(lpTest.ep2.X)
                    epListY2.Add(lpTest.ep2.Y)
                End If
            Next
        Next

        If ptList.Count < 2 Then
            SetTrueText("No brick points were found in the area.", 3)
            lp = Nothing
            Exit Sub
        End If
        dst2 = src
        For Each pt In ptList
            DrawCircle(dst2, pt)
        Next

        Dim x1 = epListX1.Average
        Dim y1 = epListY1.Average
        Dim x2 = epListX2.Average
        Dim y2 = epListY2.Average
        lpOutput = New lpData(New cv.Point2f(x1, y1), New cv.Point2f(x2, y2))
        DrawLine(dst2, lpOutput)
        ' DrawRect(dst2, lpOutput.rect)
        lpOutput.drawRoRect(dst2)

        If standalone Then lp = lpOutput
        If task.gOptions.DebugCheckBox.Checked Then
            lp = task.lineLongest
            task.gOptions.DebugCheckBox.Checked = False
        End If
    End Sub
End Class
