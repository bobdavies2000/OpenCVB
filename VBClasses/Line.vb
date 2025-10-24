Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Line_Basics : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public lineCore As New Line_Core
    Public Sub New()
        desc = "If line is NOT in motion mask, then keep it.  If line is in motion mask, add it."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.algorithmPrep = False Then Exit Sub ' only run as a task algorithm.
        lineCore.Run(task.grayStable)
        dst2 = lineCore.dst2
        labels(2) = lineCore.labels(2)

        lpList = New List(Of lpData)(lineCore.lpList)
    End Sub
End Class






Public Class Line_Core : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public lpRectMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public rawLines As New Line_Raw
    Public Sub New()
        desc = "The core algorithm to find lines.  Line_Basics is a task algorithm that exits when run as a normal algorithm."
    End Sub
    Private Function lpMotion(lp As lpData) As Boolean
        ' return true if either line endpoint was in the motion mask.
        If task.motionMask.Get(Of Byte)(lp.p1.Y, lp.p1.X) Then Return True
        If task.motionMask.Get(Of Byte)(lp.p2.Y, lp.p2.X) Then Return True
        Return False
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then
            lpList.Clear()
            task.motionMask.SetTo(255)
        End If

        Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
        For Each lp In lpList
            If lpMotion(lp) = False Then
                lp.age += 1
                sortlines.Add(lp.length, lp)
            End If
        Next

        rawLines.Run(src)

        For Each lp In rawLines.lpList
            If lpMotion(lp) Then
                lp.age = 1
                sortlines.Add(lp.length, lp)
                ' If lpList.Count >= task.FeatureSampleSize Then Exit For
            End If
        Next

        lpList.Clear()
        For Each lp In sortlines.Values
            lp.index = lpList.Count
            lpList.Add(lp)
        Next

        lpRectMap.SetTo(0)
        dst2.SetTo(0)
        For i = lpList.Count - 1 To 0 Step -1
            Dim lp = lpList(i)
            lpRectMap.Rectangle(lp.rect, i + 1, -1)
            DrawLine(dst2, lp, lp.color)
        Next

        labels(2) = CStr(lpList.Count) + " lines found"
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
                    If lp.pVec1(2) > 0 And lp.pVec2(2) > 0 Then lpList.Add(lp)
                End If
            End If
        Next

        dst2.SetTo(0)
        For Each lp In lpList
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next

        labels(2) = CStr(lpList.Count) + " lines were detected."
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





Public Class Line_Motion : Inherits TaskParent
    Dim diff As New Diff_RGBAccum
    Dim lineHistory As New List(Of List(Of lpData))
    Public Sub New()
        labels(3) = "Wave at the camera to see results - "
        desc = "Track lines that are the result of motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then lineHistory.Clear()

        diff.Run(src)
        dst2 = diff.dst2

        If task.heartBeat Then dst3 = src
        lineHistory.Add(task.lines.lpList)
        For Each lplist In lineHistory
            For Each lp In lplist
                DrawLine(dst3, lp.p1, lp.p2)
            Next
        Next
        If lineHistory.Count >= task.frameHistoryCount Then lineHistory.RemoveAt(0)

        labels(2) = CStr(task.lines.lpList.Count) + " lines were found in the diff output"
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
        dst2.SetTo(0)
        For Each lp In task.lines.lpList
            dst2.Line(lp.p1, lp.p2, white, task.lineWidth, cv.LineTypes.Link8)
            DrawCircle(dst2, lp.p1, task.DotSize, task.highlight)
        Next

        dst2.Line(task.lpD.p1, task.lpD.p2, task.highlight, task.lineWidth + 1, task.lineType)

        strOut = "Line ID = " + CStr(task.lpD.gridIndex1) + " Age = " + CStr(task.lpD.age) + vbCrLf
        strOut += "Length (pixels) = " + Format(task.lpD.length, fmt1) + " index = " + CStr(task.lpD.index) + vbCrLf
        strOut += "gridIndex1 = " + CStr(task.lpD.gridIndex1) + " gridIndex2 = " + CStr(task.lpD.gridIndex2) + vbCrLf

        strOut += "p1 = " + task.lpD.p1.ToString + ", p2 = " + task.lpD.p2.ToString + vbCrLf
        strOut += "pE1 = " + task.lpD.pE1.ToString + ", pE2 = " + task.lpD.pE2.ToString + vbCrLf + vbCrLf
        strOut += "RGB Angle = " + CStr(task.lpD.angle) + vbCrLf
        strOut += "RGB Slope = " + Format(task.lpD.slope, fmt3) + vbCrLf
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
    Dim rawLines As New Line_Raw
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
        rawLines.Run(src.Clone)

        sortedVerticals.Clear()
        sortedHorizontals.Clear()
        For Each lp In rawLines.lpList
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
                cv.Cv2.CalcHist({task.lines.dst1(lp.rect)}, {0}, emptyMat, histogram, 1, {lplist.Count},
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

        Static lpLast = New lpData(task.lineLongest.pE1, task.lineLongest.pE2)
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

        lpLast = New lpData(task.lineLongest.pE1, task.lineLongest.pE2)
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
            If Math.Abs(lp.angle - degrees) < task.angleThreshold Then
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
    Dim rawLines As New Line_Raw
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        labels = {"", "", "Lines found in the back projection", "Backprojection results"}
        desc = "Find lines in the back projection"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        backP.Run(src)

        rawLines.Run(backP.dst2)
        labels(2) = rawLines.labels(2)
        dst2 = src
        dst3.SetTo(0)
        For Each lp In rawLines.lpList
            DrawLine(dst2, lp.p1, lp.p2, task.highlight)
            DrawLine(dst3, lp.p1, lp.p2, 255)
        Next
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
                If Math.Abs(lp1.angle - lp2.angle) < task.angleThreshold Then
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
                dst2.Line(lp.p1, lp.p2, lp.color, task.lineWidth * 2, task.lineType)
                SetTrueText(CStr(colorIndex), lp.ptCenter)
            Next
            colorIndex += 1
        Next

        For Each index In unParallel
            Dim lp = task.lines.lpList(index)
            DrawLine(dst2, lp)
            SetTrueText("0", lp.ptCenter)
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
        labels(3) = "The line's rotated rect and the bricks containing the line."
        dst3 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Add a bricklist to the requested lp"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            If lp Is Nothing Then lp = task.lineLongest
        End If

        dst3.SetTo(0)
        lp.drawRoRectMask(dst3)

        Dim r = lp.rect
        dst1.SetTo(0)
        sobel.Run(task.gray)
        sobel.dst2(r).CopyTo(dst1(r), dst3(r))
        DrawRect(dst1, r, black)

        Dim allPoints As New List(Of cv.Point)
        Dim brickList As New List(Of cv.Rect)
        For Each rect In task.gridRects
            Dim brick = dst1(rect)
            If brick.CountNonZero = 0 Then Continue For
            Dim mm = GetMinMax(brick)
            Dim pt = New cv.Point(mm.maxLoc.X + rect.X, mm.maxLoc.Y + rect.Y)
            If mm.maxVal = 255 Then
                allPoints.Add(pt)
                brickList.Add(rect)
            End If
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
                If Math.Abs(lp.angle - lpTest.angle) < task.angleThreshold Then
                    angles.Add(lpTest.angle)
                    ptList.Add(pt)
                    ptList.Add(allPoints(j))
                    epListX1.Add(lpTest.pE1.X)
                    epListY1.Add(lpTest.pE1.Y)
                    epListX2.Add(lpTest.pE2.X)
                    epListY2.Add(lpTest.pE2.Y)
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
        lpOutput.drawRoRect(dst2)

        If standalone Then lp = lpOutput
        'If task.gOptions.DebugCheckBox.Checked Then
        '    lp = task.lineLongest
        '    task.gOptions.DebugCheckBox.Checked = False
        'End If

        For Each r In brickList
            DrawRect(dst3, r, white)
        Next
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






Public Class Line_Intersects : Inherits TaskParent
    Public intersects As New List(Of cv.Point2f)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find any intersects in the image and track them."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        intersects.Clear()

        For i = 0 To task.lines.lpList.Count - 1
            Dim lp1 = task.lines.lpList(i)
            For j = i + 1 To task.lines.lpList.Count - 1
                Dim lp2 = task.lines.lpList(j)
                Dim intersectionPoint = Line_Intersection.IntersectTest(lp1, lp2)
                If intersectionPoint.X >= 0 And intersectionPoint.X < dst2.Width Then
                    If intersectionPoint.Y >= 0 And intersectionPoint.Y < dst2.Height Then
                        intersects.Add(intersectionPoint)
                        If intersects.Count >= task.FeatureSampleSize Then Exit For
                    End If
                End If
            Next
            If intersects.Count >= task.FeatureSampleSize Then Exit For
        Next

        dst2 = src
        If dst3.CountNonZero > task.FeatureSampleSize * 10 Then dst3.SetTo(0)
        For Each pt In intersects
            DrawCircle(dst2, pt, task.highlight)
            DrawCircle(dst3, pt, white)
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







Public Class Line_LeftRightMatch : Inherits TaskParent
    Dim lrLines As New Line_LeftRight
    Public lp As New lpData
    Public lpOutput As New lpData
    Public Sub New()
        desc = "Identify a line that is a match in the left and right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lp = task.lineLongest

        lrLines.Run(emptyMat)
        dst2 = lrLines.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = lrLines.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim r1 = task.gridRects(task.gridMap.Get(Of Integer)(lp.p1.Y, lp.p1.X))
        Dim r2 = task.gridRects(task.gridMap.Get(Of Integer)(lp.ptCenter.Y, lp.ptCenter.X))
        Dim r3 = task.gridRects(task.gridMap.Get(Of Integer)(lp.p2.Y, lp.p2.X))
        Dim depth1 = task.pcSplit(2)(r1).Mean().Val0
        Dim depth2 = task.pcSplit(2)(r2).Mean().Val0
        Dim depth3 = task.pcSplit(2)(r3).Mean().Val0
        Dim disp1 = task.calibData.baseline * task.calibData.leftIntrinsics.fx / depth1
        Dim disp2 = task.calibData.baseline * task.calibData.leftIntrinsics.fx / depth2
        Dim disp3 = task.calibData.baseline * task.calibData.leftIntrinsics.fx / depth3

        Dim lp1 = New lpData(New cv.Point2f(lp.p1.X - disp1, lp.p1.Y),
                             New cv.Point2f(lp.ptCenter.X - disp2, lp.ptCenter.Y))
        Dim lp2 = New lpData(New cv.Point2f(lp.p1.X - disp1, lp.p1.Y), New cv.Point2f(lp.p2.X - disp3, lp.p2.Y))
        If Math.Abs(lp1.angle - lp2.angle) < task.angleThreshold Then lpOutput = lp2
        DrawLine(dst3, lpOutput.p1, lpOutput.p2, task.highlight, task.lineWidth + 1)
        DrawLine(dst2, lp.p1, lp.p2, task.highlight, task.lineWidth + 1)
    End Sub
End Class







Public Class Line_LeftRightMatch3 : Inherits TaskParent
    Dim lrLines As New Line_LeftRight
    Public lp As New lpData
    Public lpOutput As New List(Of lpData)
    Public Sub New()
        desc = "Identify a line that is a match in the left and right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lrLines.Run(emptyMat)
        dst2 = lrLines.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = lrLines.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim lplist As New List(Of lpData)(task.lines.lpList)
        If lplist.Count = 0 Then
            dst2.SetTo(0)
            SetTrueText("No lines were found in the image.")
            Exit Sub
        End If

        lpOutput.Clear()
        For i = 0 To lplist.Count - 1
            lp = lplist(i)
            Dim r1 = task.gridRects(task.gridMap.Get(Of Integer)(lp.p1.Y, lp.p1.X))
            Dim r2 = task.gridRects(task.gridMap.Get(Of Integer)(lp.ptCenter.Y, lp.ptCenter.X))
            Dim r3 = task.gridRects(task.gridMap.Get(Of Integer)(lp.p2.Y, lp.p2.X))

            Dim depth1 = task.pcSplit(2)(r1).Mean().Val0
            Dim depth2 = task.pcSplit(2)(r2).Mean().Val0
            Dim depth3 = task.pcSplit(2)(r3).Mean().Val0

            If depth1 = 0 Then Continue For
            If depth2 = 0 Then Continue For
            If depth3 = 0 Then Continue For

            Dim disp1 = task.calibData.baseline * task.calibData.leftIntrinsics.fx / depth1
            Dim disp2 = task.calibData.baseline * task.calibData.leftIntrinsics.fx / depth2
            Dim disp3 = task.calibData.baseline * task.calibData.leftIntrinsics.fx / depth3

            Dim lp1 = New lpData(New cv.Point2f(lp.p1.X - disp1, lp.p1.Y),
                                 New cv.Point2f(lp.ptCenter.X - disp2, lp.ptCenter.Y))
            Dim lp2 = New lpData(New cv.Point2f(lp.p1.X - disp1, lp.p1.Y),
                                 New cv.Point2f(lp.p2.X - disp3, lp.p2.Y))
            If Math.Abs(lp1.angle - lp2.angle) >= task.angleThreshold Then Continue For

            Dim lpOut = lp2
            lp.index = lpOutput.Count
            lpOutput.Add(lp)
            DrawLine(dst3, lpOut.p1, lpOut.p2, task.highlight, task.lineWidth + 1)
            DrawLine(dst2, lp.p1, lp.p2, task.highlight, task.lineWidth + 1)
        Next
        labels(2) = CStr(lpOutput.Count) + " left image lines were matched in the right image and confirmed with the center point."
    End Sub
End Class




Public Class Line_TestAge : Inherits TaskParent
    Dim knnLine As New Line_Generations
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Are there ever frames where no line is connected to a line on a previous frame?"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        knnLine.Run(src)
        dst2 = knnLine.dst2
        labels(2) = knnLine.labels(2)

        Dim matched As New List(Of Integer)
        For Each lp In knnLine.lpOutput
            If lp.age > 1 Then matched.Add(lp.index)
            SetTrueText(CStr(lp.age), lp.ptCenter, 3)
        Next

        Static strList As New List(Of String)
        Dim stepSize = 10
        If matched.Count > 0 Then
            strList.Add(CStr(matched.Count) + ", ")
        Else
            strList.Add("    ")
        End If
        If strList.Count > 200 Then
            For i = 0 To stepSize - 1
                strList.RemoveAt(0)
            Next
        End If

        strOut = ""
        Dim missingCount As Integer
        For i = 0 To strList.Count - 1 Step stepSize
            For j = i To Math.Min(strList.Count, i + stepSize) - 1
                If strList(j) = "    " Then missingCount += 1
                strOut += vbTab + strList(j)
            Next
            strOut += vbCrLf
        Next
        SetTrueText(strOut, 1)
        SetTrueText("In the last 200 frames there were " + CStr(missingCount) +
                    " frames without a matched line to the previous frame.", 3)

        labels(3) = "Of the " + CStr(knnLine.lpOutput.Count) + " lines found " + CStr(matched.Count) +
                    " were matched to the previous frame"
    End Sub
End Class





Public Class Line_Stabilize : Inherits TaskParent
    Dim knnLine As New Line_Generations
    Dim stable As New Stable_Basics
    Public Sub New()
        desc = "Stabilize the image by identifying a line in both the current frame and the previous."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        knnLine.Run(src)
        labels(2) = knnLine.labels(2)

        If task.firstPass Then
            stable.lpLast = knnLine.lpOutput(0)
            stable.lp = stable.lpLast
        Else
            For Each stable.lp In knnLine.lpOutput
                If stable.lp.age > 1 Then Exit For
            Next
        End If

        stable.Run(src)
        dst2 = stable.dst2
        DrawLine(dst2, stable.lp)
        SetTrueText("Age = " + CStr(stable.lp.age), stable.lp.ptCenter)

        stable.lpLast = stable.lp
    End Sub
End Class





Public Class Line_BrickPoints : Inherits TaskParent
    Public sortLines As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public Sub New()
        If task.feat Is Nothing Then task.feat = New Feature_Basics
        If task.feat Is Nothing Then task.feat = New Feature_Basics
        desc = "Assign brick points to each of the lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lines.dst2

        sortLines.Clear()
        dst3.SetTo(0)
        For Each pt In task.features
            Dim lineIndex = task.lines.dst1.Get(Of Byte)(pt.Y, pt.X)
            If lineIndex = 0 Then Continue For
            Dim color = vecToScalar(task.lines.dst2.Get(Of cv.Vec3b)(pt.Y, pt.X))
            Dim index As Integer = sortLines.Keys.Contains(lineIndex)
            Dim gridindex = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
            sortLines.Add(lineIndex, gridindex)
            DrawCircle(dst3, pt, color)
        Next
    End Sub
End Class








Public Class Line_Generations : Inherits TaskParent
    Dim knn As New KNN_Basics
    Dim match3 As New Line_LeftRightMatch3
    Public lpOutput As New List(Of lpData)
    Public Sub New()
        desc = "Identify any lines in both the current and the previous frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        match3.Run(emptyMat)
        dst2 = match3.dst2
        labels(2) = match3.labels(2)

        If match3.lpOutput.Count = 0 Then Exit Sub ' nothing was matched...

        Static lplast As New List(Of lpData)(match3.lpOutput)

        knn.queries.Clear()
        For Each lp In match3.lpOutput
            Dim pt As New cv.Point(lp.gridIndex1, lp.gridIndex2)
            knn.queries.Add(pt)
        Next

        If task.firstPass Then knn.trainInput = New List(Of cv.Point2f)(knn.queries)

        knn.Run(emptyMat)

        If lplast.Count = 0 Then lplast = New List(Of lpData)(match3.lpOutput)

        lpOutput.Clear()
        For Each lp In match3.lpOutput
            Dim index = knn.result(lp.index, 0)
            If index >= match3.lpOutput.Count Then Continue For
            If index >= lplast.Count And lplast.Count > 0 Then Continue For
            Dim age As Integer = 1
            If Math.Abs(lplast(index).angle - match3.lpOutput(index).angle) < task.angleThreshold Then
                Dim index1 = match3.lpOutput(index).gridIndex1
                Dim index2 = match3.lpOutput(index).gridIndex2
                If task.grid.gridNeighbors(index1).Contains(lplast(index).gridIndex1) And
                    task.grid.gridNeighbors(index2).Contains(lplast(index).gridIndex2) Then
                    age = lplast(index).age + 1
                End If
            End If
            lp.age = age
            lpOutput.Add(lp)
            SetTrueText(CStr(lp.age), lp.ptCenter, 2)
            SetTrueText(CStr(lp.age), lp.ptCenter, 3)
        Next

        knn.trainInput = New List(Of cv.Point2f)(knn.queries)
        lplast = New List(Of lpData)(lpOutput)
    End Sub
End Class






Public Class Line_KNN : Inherits TaskParent
    Dim knn As New KNN_Basics
    Public Sub New()
        labels(2) = "The line's end points or center closest to the mouse is highlighted."
        desc = "Use KNN to determine which line is being selected with mouse."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lines.dst2.Clone
        knn.trainInput.Clear()
        knn.queries.Clear()
        For Each lp In task.lines.lpList
            knn.trainInput.Add(lp.p1)
            knn.trainInput.Add(lp.ptCenter)
            knn.trainInput.Add(lp.p2)
        Next

        knn.queries.Add(task.mouseMovePoint)
        knn.Run(emptyMat)

        Dim index = Math.Floor(knn.result(0, 0) / 3)
        Dim lpNext = task.lines.lpList(index)
        dst2.Line(lpNext.p1, lpNext.p2, task.highlight, task.lineWidth * 3, cv.LineTypes.AntiAlias)
    End Sub
End Class





Public Class Line_Select : Inherits TaskParent
    Public delaunay As New Delaunay_LineSelect
    Public Sub New()
        desc = "Select a line with mouse movement and put the selection into task.lpD."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        delaunay.Run(src)
        dst2 = delaunay.dst1
        labels(2) = delaunay.labels(2)

        SetTrueText(delaunay.info.strOut, 3) ' the line info is already prepped in strout in delaunay.
    End Sub
End Class






Public Class Line_Select3D : Inherits TaskParent
    Public delaunay As New Delaunay_LineSelect
    Public Sub New()
        desc = "Recompute each 3D pixel on the selected RGB line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        delaunay.Run(src)
        dst2 = delaunay.dst1
        labels(2) = delaunay.labels(2)
    End Sub
End Class






Public Class Line_Vertical : Inherits TaskParent
    Dim vbPoints As New BrickPoint_Vertical
    Dim knn As New KNN_Basics
    Public Sub New()
        desc = "Match points to the nearest that is also vertical"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        vbPoints.Run(src)
        dst2 = vbPoints.dst2

        knn.ptListTrain = New List(Of cv.Point)(vbPoints.ptList)
        knn.ptListQuery = New List(Of cv.Point)(vbPoints.ptList)
        knn.Run(dst2)
        labels(3) = "There are " + CStr(knn.result.GetUpperBound(0)) + " input points to KNN."

        Dim lpList As New List(Of lpData)
        For i = 0 To knn.result.GetUpperBound(0) - 1
            Dim deltaX As New List(Of Single)
            Dim ptList As New List(Of cv.Point)
            Dim p1 = vbPoints.ptList(knn.result(i, 0))
            For j = 1 To Math.Min(knn.result.Length - 1, 6) - 1
                Dim p2 = vbPoints.ptList(knn.result(i, j))
                Dim delta = Math.Abs(p1.X - p2.X)
                deltaX.Add(delta)
                ptList.Add(p2)
            Next

            Dim minVal = deltaX.Min
            Dim index = deltaX.IndexOf(minVal)
            If minVal < task.brickSize Then
                Dim lp = New lpData(p1, ptList(index))
                If lp.indexVTop < 0 Or lp.indexVBot < 0 Then Continue For
                lp.index = lpList.Count
                lpList.Add(lp)
                dst2.Line(p1, ptList(index), task.highlight, task.lineWidth, task.lineType)
            End If
        Next

        Dim topGroups(task.bricksPerRow - 1) As List(Of Integer)
        For Each lp In lpList
            If topGroups(lp.indexVTop) Is Nothing Then topGroups(lp.indexVTop) = New List(Of Integer)
            topGroups(lp.indexVTop).Add(lp.index)
        Next

        Dim indexVTop = Math.Abs(task.gOptions.DebugSlider.Value)
        dst3.SetTo(0)
        If indexVTop < topGroups.Count Then
            If topGroups(indexVTop) IsNot Nothing Then
                Dim botGroups(task.bricksPerRow - 1) As List(Of Integer)
                For Each index In topGroups(indexVTop)
                    Dim lp = lpList(index)
                    If botGroups(lp.indexVBot) Is Nothing Then botGroups(lp.indexVBot) = New List(Of Integer)
                    botGroups(lp.indexVBot).Add(lp.index)
                Next

                Dim maxIndex As Integer
                Dim maxCount As Integer
                For i = 0 To botGroups.Count - 1
                    If botGroups(i) Is Nothing Then Continue For
                    If maxCount < botGroups(i).Count Then
                        maxCount = botGroups.Count
                        maxIndex = i
                    End If
                Next
                For Each index In botGroups(maxIndex)
                    Dim lp = lpList(index)
                    DrawLine(dst3, lp)
                Next
            End If
        End If

        labels(2) = "There were " + CStr(lpList.Count) + " neighbors that formed good lines."
    End Sub
End Class
