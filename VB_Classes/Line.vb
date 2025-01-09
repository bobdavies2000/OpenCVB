Imports cv = OpenCvSharp
Public Class Line_Basics : Inherits TaskParent
    Dim lineCore As New Line_Core
    Public lpMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Public lpSorted As New List(Of linePoints)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Collect lines across frames using the motion mask."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        lineCore.Run(src)
        dst2 = lineCore.dst2

        lpMap.SetTo(0)
        For Each lp In lineCore.lpList
            lpMap.Line(lp.p1, lp.p2, lp.index, task.lineWidth + 1, cv.LineTypes.Link8)
        Next

        lpSorted = New List(Of linePoints)(lineCore.lpSorted.Values)
        task.lpList = New List(Of linePoints)(lineCore.lpList)
        labels(2) = lineCore.labels(2)
    End Sub
End Class





Public Class Line_Core : Inherits TaskParent
    Dim lines As New Line_Detector
    Dim options As New Options_Line
    Public lpList As New List(Of linePoints)
    Public lpMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Public lpSorted As New SortedList(Of Single, linePoints)(New compareAllowIdenticalSingleInverted)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Collect lines across frames using the motion mask."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        options.RunOpt()

        lines.Run(src.Clone)
        Static lastList As New List(Of linePoints)(lines.lpList)

        Dim newSet As New List(Of linePoints)
        ' unlike Feature_Basics, we have to check each pair, not each point
        For Each lp In lastList
            lp.p1 = validatePoint(lp.p1)
            lp.p2 = validatePoint(lp.p2)
            Dim val1 = task.motionMask.Get(Of Byte)(lp.p1.Y, lp.p1.X)
            Dim val2 = task.motionMask.Get(Of Byte)(lp.p2.Y, lp.p2.X)
            If val1 = 0 And val2 = 0 Then newSet.Add(lp)
        Next

        ' unlike Feature_Basics, we have to check each pair, not each point
        For Each lp In lines.lpList
            lp.p1 = validatePoint(lp.p1)
            lp.p2 = validatePoint(lp.p2)
            Dim val1 = task.motionMask.Get(Of Byte)(lp.p1.Y, lp.p1.X)
            Dim val2 = task.motionMask.Get(Of Byte)(lp.p2.Y, lp.p2.X)
            If val1 <> 0 Or val2 <> 0 Then newSet.Add(lp)
        Next

        lpSorted.Clear()
        For Each lp In newSet
            If lp.p1.X > lp.p2.X Then lp = New linePoints(lp.p2, lp.p1)
            lpSorted.Add(lp.length, lp)
        Next

        lpList.Clear()
        For Each lp In lpSorted.Values
            lp.index = lpList.Count
            If lp.rect.Width > 0 Then
                If lp.mask.Width > 0 Then
                    lp.mmX = GetMinMax(task.pcSplit(0)(lp.rect), lp.mask)
                    lp.mmY = GetMinMax(task.pcSplit(1)(lp.rect), lp.mask)
                    lp.mmZ = GetMinMax(task.pcSplit(2)(lp.rect), lp.mask)

                    lp.mmPerPixel = (lp.mmZ.maxVal - lp.mmZ.minVal) / lp.length

                    lpList.Add(lp)
                End If
            End If
        Next
        lastList = New List(Of linePoints)(lpList)

        dst2.SetTo(0)
        For Each lp In lpList
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next

        If task.heartBeat Then
            labels(2) = CStr(lines.lpList.Count) + " lines found in Line_Detector with " +
                        CStr(lpList.Count) + " after filtering with the motion mask."
        End If
    End Sub
End Class







Public Class Line_Detector : Inherits TaskParent
    Dim ld As cv.XImgProc.FastLineDetector
    Public lpList As New List(Of linePoints)
    Public subsetRect As cv.Rect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
    Public Sub New()
        dst2 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset " +
               "rectangle (provided externally)"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If src.Type <> cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_8U)

        Dim lines = ld.Detect(src(subsetRect))

        lpList.Clear()
        For Each v In lines
            If v(0) >= 0 And v(0) <= src.Cols And v(1) >= 0 And v(1) <= src.Rows And
               v(2) >= 0 And v(2) <= src.Cols And v(3) >= 0 And v(3) <= src.Rows Then
                Dim p1 = validatePoint(New cv.Point(CInt(v(0) + subsetRect.X), CInt(v(1) + subsetRect.Y)))
                Dim p2 = validatePoint(New cv.Point(CInt(v(2) + subsetRect.X), CInt(v(3) + subsetRect.Y)))
                Dim lp = New linePoints(p1, p2)
                lp.rect = ValidateRect(lp.rect)
                lp.mask = dst2(lp.rect)
                lpList.Add(lp)
            End If
        Next

        dst2.SetTo(0)
        For Each lp In lpList
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next
        labels(2) = CStr(lpList.Count) + " lines were detected in the current frame"
    End Sub
End Class



Public Class Line_Rects : Inherits TaskParent
    Public Sub New()
        desc = "Show the rectangle for each line"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        task.lines.Run(src)

        dst2 = task.lines.dst2

        For Each lp In task.lpList
            dst2.Rectangle(lp.rect, 255, task.lineWidth)
        Next
    End Sub
End Class







Public Class Line_InterceptsUI : Inherits TaskParent
    Dim lines As New Line_Intercepts
    Dim p2 As cv.Point
    Dim redRadio As System.Windows.Forms.RadioButton
    Dim greenRadio As System.Windows.Forms.RadioButton
    Dim yellowRadio As System.Windows.Forms.RadioButton
    Dim blueRadio As System.Windows.Forms.RadioButton
    Public Sub New()
        redRadio = optiBase.FindRadio("Show Top intercepts")
        greenRadio = optiBase.FindRadio("Show Bottom intercepts")
        yellowRadio = optiBase.FindRadio("Show Right intercepts")
        blueRadio = optiBase.FindRadio("Show Left intercepts")
        labels(2) = "Use mouse in right image to highlight lines"
        desc = "An alternative way to highlight line segments with common slope"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        lines.Run(src)
        dst3.SetTo(0)

        Dim red = New cv.Scalar(0, 0, 255)
        Dim green = New cv.Scalar(1, 128, 0)
        Dim yellow = New cv.Scalar(2, 255, 255)
        Dim blue = New cv.Scalar(254, 0, 0)

        Dim center = New cv.Point(dst3.Width / 2, dst3.Height / 2)
        dst3.Line(New cv.Point(0, 0), center, blue, task.lineWidth, cv.LineTypes.Link4)
        dst3.Line(New cv.Point(dst2.Width, 0), center, red, task.lineWidth, cv.LineTypes.Link4)
        dst3.Line(New cv.Point(0, dst2.Height), center, blue, task.lineWidth, cv.LineTypes.Link4)
        dst3.Line(New cv.Point(dst2.Width, dst2.Height), center, yellow, task.lineWidth, cv.LineTypes.Link4)

        Dim mask = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8U, cv.Scalar.All(0))
        Dim pt = New cv.Point(center.X, center.Y - 30)
        cv.Cv2.FloodFill(dst3, mask, pt, red, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))

        pt = New cv.Point(center.X, center.Y + 30)
        cv.Cv2.FloodFill(dst3, mask, pt, green, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))

        pt = New cv.Point(center.X - 30, center.Y)
        cv.Cv2.FloodFill(dst3, mask, pt, blue, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))

        pt = New cv.Point(center.X + 30, center.Y)
        cv.Cv2.FloodFill(dst3, mask, pt, yellow, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))
        Dim color = dst3.Get(Of cv.Vec3b)(task.mouseMovePoint.Y, task.mouseMovePoint.X)

        Dim p1 = task.mouseMovePoint
        If p1.X = center.X Then
            If p1.Y <= center.Y Then p2 = New cv.Point(dst3.Width / 2, 0) Else p2 = New cv.Point(dst3.Width, dst3.Height)
        Else
            Dim m = (center.Y - p1.Y) / (center.X - p1.X)
            Dim b = p1.Y - p1.X * m

            If color(0) = 0 Then p2 = New cv.Point(-b / m, 0) ' red zone
            If color(0) = 1 Then p2 = New cv.Point((dst3.Height - b) / m, dst3.Height) ' green
            If color(0) = 2 Then p2 = New cv.Point(dst3.Width, dst3.Width * m + b) ' yellow
            If color(0) = 254 Then p2 = New cv.Point(0, b) ' blue
            DrawLine(dst3, center, p2, cv.Scalar.Black)
        End If
        DrawCircle(dst3, center, task.DotSize, white)
        If color(0) = 0 Then redRadio.Checked = True
        If color(0) = 1 Then greenRadio.Checked = True
        If color(0) = 2 Then yellowRadio.Checked = True
        If color(0) = 254 Then blueRadio.Checked = True

        For Each inter In lines.intercept
            Select Case lines.options.selectedIntercept
                Case 0
                    dst3.Line(New cv.Point(inter.Key, 0), New cv.Point(inter.Key, 10), white,
                             task.lineWidth)
                Case 1
                    dst3.Line(New cv.Point(inter.Key, dst3.Height), New cv.Point(inter.Key, dst3.Height - 10),
                             white, task.lineWidth)
                Case 2
                    dst3.Line(New cv.Point(0, inter.Key), New cv.Point(10, inter.Key), white,
                             task.lineWidth)
                Case 3
                    dst3.Line(New cv.Point(dst3.Width, inter.Key), New cv.Point(dst3.Width - 10, inter.Key),
                             white, task.lineWidth)
            End Select
        Next
        dst2 = lines.dst2
    End Sub
End Class







Public Class Line_Intercepts : Inherits TaskParent
    Public extended As New LongLine_Extend
    Public lines As New Line_Basics
    Public p1List As New List(Of cv.Point2f)
    Public p2List As New List(Of cv.Point2f)
    Dim longLine As New LongLine_Basics
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
    Public Overrides sub runAlg(src As cv.Mat)
        options.RunOpt()

        lines.Run(src)
        If task.lpList.Count = 0 Then Exit Sub

        dst2 = src
        p1List.Clear()
        p2List.Clear()
        intercept = interceptArray(options.selectedIntercept)
        topIntercepts.Clear()
        botIntercepts.Clear()
        leftIntercepts.Clear()
        rightIntercepts.Clear()
        Dim index As Integer
        For Each lp In task.lpList
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

            Dim emps = longLine.BuildLongLine(lp)
            If emps.p1.X = 0 Then leftIntercepts.Add(saveP1.Y, index)
            If emps.p1.Y = 0 Then topIntercepts.Add(saveP1.X, index)
            If emps.p1.X = dst2.Width Then rightIntercepts.Add(saveP1.Y, index)
            If emps.p1.Y = dst2.Height Then botIntercepts.Add(saveP1.X, index)

            If emps.p2.X = 0 Then leftIntercepts.Add(saveP2.Y, index)
            If emps.p2.Y = 0 Then topIntercepts.Add(saveP2.X, index)
            If emps.p2.X = dst2.Width Then rightIntercepts.Add(saveP2.Y, index)
            If emps.p2.Y = dst2.Height Then botIntercepts.Add(saveP2.X, index)
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







Public Class Line_InDepthAndBGR : Inherits TaskParent
    Dim lines As New Line_Basics
    Public p1List As New List(Of cv.Point2f)
    Public p2List As New List(Of cv.Point2f)
    Public z1List As New List(Of cv.Point3f) ' the point cloud values corresponding to p1 and p2
    Public z2List As New List(Of cv.Point3f)
    Public Sub New()
        labels(2) = "Lines defined in BGR"
        labels(3) = "Lines in BGR confirmed in the point cloud"
        desc = "Find the BGR lines and confirm they are present in the cloud data."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2
        If task.lpList.Count = 0 Then Exit Sub

        Dim lineList = New List(Of cv.Rect)
        If task.optionsChanged Then dst3.SetTo(0)
        dst3.SetTo(0, task.motionMask)
        p1List.Clear()
        p2List.Clear()
        z1List.Clear()
        z2List.Clear()
        For Each lp In task.lpList
            Dim mask = New cv.Mat(New cv.Size(lp.rect.Width, lp.rect.Height), cv.MatType.CV_8U, cv.Scalar.All(0))
            mask.Line(New cv.Point(CInt(lp.p1.X - lp.rect.X), CInt(lp.p1.Y - lp.rect.Y)),
                      New cv.Point(CInt(lp.p2.X - lp.rect.X), CInt(lp.p2.Y - lp.rect.Y)), 255, task.lineWidth, cv.LineTypes.Link4)
            Dim mean = task.pointCloud(lp.rect).Mean(mask)

            If mean <> New cv.Scalar Then
                Dim mmX = GetMinMax(task.pcSplit(0)(lp.rect), mask)
                Dim mmY = GetMinMax(task.pcSplit(1)(lp.rect), mask)
                Dim len1 = mmX.minLoc.DistanceTo(mmX.maxLoc)
                Dim len2 = mmY.minLoc.DistanceTo(mmY.maxLoc)
                If len1 > len2 Then
                    lp.p1 = New cv.Point(mmX.minLoc.X + lp.rect.X, mmX.minLoc.Y + lp.rect.Y)
                    lp.p2 = New cv.Point(mmX.maxLoc.X + lp.rect.X, mmX.maxLoc.Y + lp.rect.Y)
                Else
                    lp.p1 = New cv.Point(mmY.minLoc.X + lp.rect.X, mmY.minLoc.Y + lp.rect.Y)
                    lp.p2 = New cv.Point(mmY.maxLoc.X + lp.rect.X, mmY.maxLoc.Y + lp.rect.Y)
                End If
                If lp.p1.DistanceTo(lp.p2) > 1 Then
                    DrawLine(dst3, lp.p1, lp.p2, cv.Scalar.Yellow)
                    p1List.Add(lp.p1)
                    p2List.Add(lp.p2)
                    z1List.Add(task.pointCloud.Get(Of cv.Point3f)(lp.p1.Y, lp.p1.X))
                    z2List.Add(task.pointCloud.Get(Of cv.Point3f)(lp.p2.Y, lp.p2.X))
                End If
            End If
        Next
    End Sub
End Class







Public Class Line_Movement : Inherits TaskParent
    Public p1 As cv.Point
    Public p2 As cv.Point
    Dim gradientColors(100) As cv.Scalar
    Dim kalman As New Kalman_Basics
    Dim frameCount As Integer
    Public Sub New()
        kalman.kOutput = {0, 0, 0, 0}

        Dim color1 = cv.Scalar.Yellow, color2 = cv.Scalar.Blue
        Dim f As Double = 1.0
        For i = 0 To gradientColors.Length - 1
            gradientColors(i) = New cv.Scalar(f * color2(0) + (1 - f) * color1(0), f * color2(1) + (1 - f) * color1(1), f * color2(2) + (1 - f) * color1(2))
            f -= 1 / gradientColors.Length
        Next

        labels = {"", "", "Line Movement", ""}
        desc = "Show the movement of the line provided"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        If standaloneTest() Then
            Static k1 = p1
            Static k2 = p2
            If k1.DistanceTo(p1) = 0 And k2.DistanceTo(p2) = 0 Then
                k1 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                k2 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                dst2.SetTo(0)
            End If
            kalman.kInput = {k1.X, k1.Y, k2.X, k2.Y}
            kalman.Run(src)
            p1 = New cv.Point(kalman.kOutput(0), kalman.kOutput(1))
            p2 = New cv.Point(kalman.kOutput(2), kalman.kOutput(3))
        End If
        frameCount += 1
        DrawLine(dst2, p1, p2, gradientColors(frameCount Mod gradientColors.Count))
    End Sub
End Class







Public Class Line_GCloud : Inherits TaskParent
    Public lines As New Line_Basics
    Public sortedVerticals As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
    Public sortedHorizontals As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
    Public allLines As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
    Public options As New Options_LineFinder
    Dim match As New Match_tCell
    Dim angleSlider As System.Windows.Forms.TrackBar
    Public Sub New()
        angleSlider = optiBase.FindSlider("Angle tolerance in degrees")
        labels(2) = "Line_GCloud - Blue are vertical lines using the angle thresholds."
        desc = "Find all the vertical lines using the point cloud rectified with the IMU vector for gravity."
    End Sub
    Public Function updateGLine(src As cv.Mat, gc As gravityLine, p1 As cv.Point, p2 As cv.Point) As gravityLine
        gc.tc1.center = p1
        gc.tc2.center = p2
        gc.tc1 = match.createCell(src, gc.tc1.correlation, p1)
        gc.tc2 = match.createCell(src, gc.tc2.correlation, p2)
        gc.tc1.strOut = Format(gc.tc1.correlation, fmt2) + vbCrLf + Format(gc.tc1.depth, fmt2) + "m"
        gc.tc2.strOut = Format(gc.tc2.correlation, fmt2) + vbCrLf + Format(gc.tc2.depth, fmt2) + "m"

        Dim mean = task.pointCloud(gc.tc1.rect).Mean(task.depthMask(gc.tc1.rect))
        gc.pt1 = New cv.Point3f(mean(0), mean(1), mean(2))
        gc.tc1.depth = gc.pt1.Z
        mean = task.pointCloud(gc.tc2.rect).Mean(task.depthMask(gc.tc2.rect))
        gc.pt2 = New cv.Point3f(mean(0), mean(1), mean(2))
        gc.tc2.depth = gc.pt2.Z

        gc.len3D = distance3D(gc.pt1, gc.pt2)
        If gc.pt1 = New cv.Point3f Or gc.pt2 = New cv.Point3f Then
            gc.len3D = 0
        Else
            gc.arcX = Math.Asin((gc.pt1.X - gc.pt2.X) / gc.len3D) * 57.2958
            gc.arcY = Math.Abs(Math.Asin((gc.pt1.Y - gc.pt2.Y) / gc.len3D) * 57.2958)
            If gc.arcY > 90 Then gc.arcY -= 90
            gc.arcZ = Math.Asin((gc.pt1.Z - gc.pt2.Z) / gc.len3D) * 57.2958
        End If

        Return gc
    End Function
    Public Overrides Sub runAlg(src As cv.Mat)
        options.RunOpt()

        Dim maxAngle = angleSlider.Value

        dst2 = src.Clone
        lines.Run(src.Clone)

        sortedVerticals.Clear()
        sortedHorizontals.Clear()
        For Each lp In task.lpList
            Dim gc As gravityLine
            gc = updateGLine(src, gc, lp.p1, lp.p2)
            allLines.Add(lp.p1.DistanceTo(lp.p2), gc)
            If Math.Abs(90 - gc.arcY) < maxAngle And gc.tc1.depth > 0 And gc.tc2.depth > 0 Then
                sortedVerticals.Add(lp.p1.DistanceTo(lp.p2), gc)
                DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Blue)
            End If
            If Math.Abs(gc.arcY) <= maxAngle And gc.tc1.depth > 0 And gc.tc2.depth > 0 Then
                sortedHorizontals.Add(lp.p1.DistanceTo(lp.p2), gc)
                DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Yellow)
            End If
        Next

        labels(2) = Format(sortedHorizontals.Count, "00") + " Horizontal lines were identified and " + Format(sortedVerticals.Count, "00") + " Vertical lines were identified."
    End Sub
End Class







Public Class Line_Perpendicular : Inherits TaskParent
    Public input As linePoints
    Public output As linePoints
    Dim midPoint As cv.Point2f
    Public Sub New()
        labels = {"", "", "White is the original line, red dot is midpoint, yellow is perpendicular line", ""}
        desc = "Find the line perpendicular to the line created by the points provided."
    End Sub
    Public Function computePerp(lp As linePoints) As linePoints
        SetTrueText("Computing the perpendicular")
        midPoint = New cv.Point2f((lp.p1.X + lp.p2.X) / 2, (lp.p1.Y + lp.p2.Y) / 2)

        Dim m = If(lp.slope = 0, 100000, -1 / lp.slope)

        Dim b = midPoint.Y - m * midPoint.X
        activeTask = True ' algorithms called by task algorithms don't get marked active
        Return New linePoints(New cv.Point2f(-b / m, 0), New cv.Point2f((dst2.Height - b) / m, dst2.Height))
    End Function
    Public Overrides Sub runAlg(src As cv.Mat)
        If standaloneTest() Then input = task.gravityVec
        dst2.SetTo(0)
        DrawLine(dst2, input.p1, input.p2, white)

        output = computePerp(input)
        DrawCircle(dst2, midPoint, task.DotSize + 2, cv.Scalar.Red)
        DrawLine(dst2, output.p1, output.p2, cv.Scalar.Yellow)
    End Sub
End Class







Public Class Line_ViewSide : Inherits TaskParent
    Public autoY As New OpAuto_YRange
    Public lines As New Line_Basics
    Dim histSide As New Projection_HistSide
    Public Sub New()
        labels = {"", "", "Hotspots in the Side View", "Lines found in the hotspots of the Side View."}
        desc = "Find lines in the hotspots for the side view."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        histSide.Run(src)

        autoY.Run(histSide.histogram)
        dst2 = histSide.histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs

        lines.Run(dst2.Clone)
        dst3 = lines.dst2
        labels(2) = lines.labels(2)
    End Sub
End Class






Public Class Line_ViewTop : Inherits TaskParent
    Public autoX As New OpAuto_XRange
    Public lines As New Line_Basics
    Dim histTop As New Projection_HistTop
    Public Sub New()
        labels = {"", "", "Hotspots in the Top View", "Lines found in the hotspots of the Top View."}
        desc = "Find lines in the hotspots for the Top View."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        histTop.Run(src)

        autoX.Run(histTop.histogram)
        dst2 = histTop.histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs

        lines.Run(dst2)
        dst3 = lines.dst2
        labels(2) = lines.labels(2)
    End Sub
End Class






Public Class Line_FromContours : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim lines As New Line_Basics
    Dim contours As New Contour_Gray
    Public Sub New()
        task.redOptions.ColorSource.SelectedItem() = "Reduction_Basics" ' to enable sliders.
        task.gOptions.HighlightColor.SelectedIndex = 3
        UpdateAdvice("Use the reduction sliders in the redoptions to control contours and subsequent lines found.")
        desc = "Find the lines in the contours."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        reduction.Run(src)
        contours.Run(reduction.dst2)
        dst2 = contours.dst2.Clone
        lines.Run(dst2)

        dst3.SetTo(0)
        For Each lp In task.lpList
            DrawLine(dst3, lp.p1, lp.p2, white)
        Next
    End Sub
End Class








Public Class Line_ColorClass : Inherits TaskParent
    Dim color8U As New Color8U_Basics
    Dim lines As New Line_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        labels = {"", "", "Lines for the current color class", "Color Class input"}
        desc = "Review lines in all the different color classes"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        color8U.Run(src)
        dst1 = color8U.dst3

        lines.Run(dst1 * 255 / color8U.classCount)
        dst2 = lines.dst2
        dst3 = lines.dst2

        labels(1) = "Input to Line_Basics"
        labels(2) = "Lines found in the " + color8U.classifier.traceName + " output"
    End Sub
End Class








Public Class Line_TimeView : Inherits TaskParent
    Public frameList As New List(Of List(Of linePoints))
    Public lines As New Line_Basics
    Public pixelcount As Integer
    Public lpList As New List(Of linePoints)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Collect lines over time"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        lines.Run(src)

        If task.optionsChanged Then frameList.Clear()
        Dim nextMpList = New List(Of linePoints)(task.lpList)
        frameList.Add(nextMpList)

        dst2 = src
        dst3.SetTo(0)
        lpList.Clear()
        Dim lineTotal As Integer
        For i = 0 To frameList.Count - 1
            lineTotal += frameList(i).Count
            For Each lp In frameList(i)
                DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Yellow)
                DrawLine(dst3, lp.p1, lp.p2, white)
                lpList.Add(lp)
            Next
        Next

        If frameList.Count >= task.frameHistoryCount Then frameList.RemoveAt(0)
        pixelcount = dst3.CountNonZero
        labels(3) = "There were " + CStr(lineTotal) + " lines detected using " + Format(pixelcount / 1000, "#.0") + "k pixels"
    End Sub
End Class








Public Class Line_RegionsVB : Inherits TaskParent
    Dim lines As New Line_TimeView
    Dim reduction As New Reduction_Basics
    Const lineMatch = 254
    Public Sub New()
        task.redOptions.BitwiseReduction.Checked = True
        task.redOptions.setBitReductionBar(6)

        If optiBase.FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Show intermediate vertical step results.")
            check.addCheckBox("Run horizontal without vertical step")
        End If

        desc = "Use the reduction values between lines to identify regions."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        Static noVertCheck = optiBase.FindCheckBox("Run horizontal without vertical step")
        Static verticalCheck = optiBase.FindCheckBox("Show intermediate vertical step results.")
        reduction.Run(src)
        dst2 = reduction.dst2
        dst3 = dst2.Clone

        lines.Run(src)

        Dim lineMask = lines.dst3
        dst2.SetTo(lineMatch, lineMask)
        dst3.SetTo(lineMatch, lineMask)

        Dim nextB As Byte
        Dim region As Integer = -1
        Dim indexer1 = dst2.GetGenericIndexer(Of Byte)()
        Dim indexer2 = dst3.GetGenericIndexer(Of Byte)()
        If noVertCheck.checked = False Then
            For x = 0 To dst2.Width - 1
                region = -1
                For y = 0 To dst2.Height - 1
                    nextB = indexer1(y, x)
                    If nextB = lineMatch Then
                        region = -1
                    Else
                        If region = -1 Then
                            region = nextB
                        Else
                            indexer1(y, x) = region
                        End If
                    End If
                Next
            Next
        End If

        For y = 0 To dst3.Height - 1
            region = -1
            For x = 0 To dst3.Width - 1
                nextB = indexer2(y, x)
                If nextB = lineMatch Then
                    region = -1
                Else
                    If region = -1 Then
                        If y = 0 Then
                            region = indexer1(y, x)
                        Else
                            Dim vals As New List(Of Integer)
                            Dim counts As New List(Of Integer)
                            For i = x To dst3.Width - 1
                                Dim nextVal = indexer1(y - 1, i)
                                If nextVal = lineMatch Then Exit For
                                If vals.Contains(nextVal) Then
                                    counts(vals.IndexOf(nextVal)) += 1
                                Else
                                    vals.Add(nextVal)
                                    counts.Add(1)
                                End If
                                Dim maxVal = counts.Max
                                region = vals(counts.IndexOf(maxVal))
                            Next
                        End If
                    Else
                        indexer2(y, x) = region
                    End If
                End If
            Next
        Next
        labels(2) = If(verticalCheck.checked, "Intermediate result of vertical step", "Lines detected (below) Regions detected (right image)")
        If noVertCheck.checked And verticalCheck.checked Then labels(2) = "Input to vertical step"
        If verticalCheck.checked = False Then dst2 = lines.dst2.Clone
    End Sub
End Class






Public Class Line_Nearest : Inherits TaskParent
    Public pt As cv.Point2f ' How close is this point to the input line?
    Public lp As New linePoints ' the input line.
    Public nearPoint As cv.Point2f
    Public onTheLine As Boolean
    Public distance As Single
    Public Sub New()
        labels(2) = "Yellow line is input line, white dot is the input point, and the white line is the nearest path to the input line."
        desc = "Find the nearest point on a line"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        If standaloneTest() And task.heartBeat Then
            lp.p1 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            lp.p2 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            pt = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        End If

        Dim minX = Math.Min(lp.p1.X, lp.p2.X)
        Dim minY = Math.Min(lp.p1.Y, lp.p2.Y)
        Dim maxX = Math.Max(lp.p1.X, lp.p2.X)
        Dim maxY = Math.Max(lp.p1.Y, lp.p2.Y)

        onTheLine = True
        If lp.p1.X = lp.p2.X Then
            nearPoint = New cv.Point2f(lp.p1.X, pt.Y)
            If pt.Y < minY Or pt.Y > maxY Then onTheLine = False
        Else
            Dim m = (lp.p1.Y - lp.p2.Y) / (lp.p1.X - lp.p2.X)
            If m = 0 Then
                nearPoint = New cv.Point2f(pt.X, lp.p1.Y)
                If pt.X < minX Or pt.X > maxX Then onTheLine = False
            Else
                Dim b1 = lp.p1.Y - lp.p1.X * m

                Dim b2 = pt.Y + pt.X / m
                Dim a1 = New cv.Point2f(0, b2)
                Dim a2 = New cv.Point2f(dst2.Width, b2 + dst2.Width / m)
                Dim x = m * (b2 - b1) / (m * m + 1)
                nearPoint = New cv.Point2f(x, m * x + b1)

                If nearPoint.X < minX Or nearPoint.X > maxX Or nearPoint.Y < minY Or nearPoint.Y > maxY Then onTheLine = False
            End If
        End If

        Dim distance1 = Math.Sqrt(Math.Pow(pt.X - lp.p1.X, 2) + Math.Pow(pt.Y - lp.p1.Y, 2))
        Dim distance2 = Math.Sqrt(Math.Pow(pt.X - lp.p2.X, 2) + Math.Pow(pt.Y - lp.p2.Y, 2))
        If onTheLine = False Then nearPoint = If(distance1 < distance2, lp.p1, lp.p2)
        If standaloneTest() Then
            dst2.SetTo(0)
            DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Yellow)
            DrawLine(dst2, pt, nearPoint, white)
            DrawCircle(dst2, pt, task.DotSize, white)
        End If
        distance = Math.Sqrt(Math.Pow(pt.X - nearPoint.X, 2) + Math.Pow(pt.Y - nearPoint.Y, 2))
    End Sub
End Class





' https://stackoverflow.com/questions/7446126/opencv-2d-line-intersection-helper-function
Public Class Line_Intersection : Inherits TaskParent
    Public p1 As cv.Point2f, p2 As cv.Point2f, p3 As cv.Point2f, p4 As cv.Point2f
    Public intersectionPoint As cv.Point2f
    Public Sub New()
        desc = "Determine if 2 lines intersect, where the point is, and if that point is in the image."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        If task.heartBeat Then
            p1 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            p2 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            p3 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            p4 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        End If

        intersectionPoint = IntersectTest(p1, p2, p3, p4, New cv.Rect(0, 0, src.Width, src.Height))

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







Public Class Line_KNN : Inherits TaskParent
    Dim lines As New Line_Basics
    Dim swarm As New Swarm_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Use KNN to find the nearest point to an endpoint and connect the 2 lines with a line."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        task.feat.Run(src)

        swarm.options.RunOpt()
        lines.Run(src)
        dst2 = lines.dst2

        dst3.SetTo(0)
        swarm.knn.queries.Clear()
        For Each lp In task.lpList
            swarm.knn.queries.Add(lp.p1)
            swarm.knn.queries.Add(lp.p2)
            DrawLine(dst3, lp.p1, lp.p2, 255)
        Next
        swarm.knn.trainInput = New List(Of cv.Point2f)(swarm.knn.queries)
        swarm.knn.Run(src)

        dst3 = swarm.DrawLines().Clone
        labels(2) = lines.labels(2)
    End Sub
End Class





Public Class Line_Vertical : Inherits TaskParent
    Public lines As New Line_Basics
    Public ptList As New List(Of linePoints)
    Public Sub New()
        desc = "Find all the vertical lines with gravity vector"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        dst2 = src.Clone
        lines.Run(src)
        dst3 = lines.dst2

        Dim p1 = task.gravityVec.p1, p2 = task.gravityVec.p2
        Dim sideOpposite = p2.X - p1.X
        If p1.Y = 0 Then sideOpposite = p1.X - p2.X
        Dim gAngle = Math.Atan(sideOpposite / dst2.Height) * 57.2958

        ptList.Clear()
        For Each lp In task.lpList
            If lp.p1.Y > lp.p2.Y Then lp = New linePoints(lp.p2, lp.p1)

            sideOpposite = lp.p2.X - lp.p1.X
            If lp.p1.Y < lp.p2.Y Then sideOpposite = lp.p1.X - lp.p2.X
            Dim angle = Math.Atan(sideOpposite / Math.Abs(lp.p1.Y - lp.p2.Y)) * 57.2958

            If Math.Abs(angle - gAngle) < 2 Then
                dst2.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth, task.lineType)
                ptList.Add(lp)
            End If
        Next
        labels(2) = "There are " + CStr(ptList.Count) + " lines similar to the Gravity " + Format(gAngle, fmt1) + " degrees"
    End Sub
End Class





Public Class Line_Horizontal : Inherits TaskParent
    Public lines As New Line_Basics
    Public ptList As New List(Of linePoints)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        desc = "Find all the Horizontal lines with horizon vector"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        dst2 = src.Clone
        lines.Run(src)

        Dim p1 = task.horizonVec.p1, p2 = task.horizonVec.p2
        Dim sideOpposite = p2.Y - p1.Y
        If p1.X = 0 Then sideOpposite = p1.Y - p2.Y
        Dim hAngle = Math.Atan(sideOpposite / dst2.Width) * 57.2958

        ptList.Clear()
        If task.heartBeat Then dst3.SetTo(0)
        For Each lp In task.lpList
            If lp.p1.X > lp.p2.X Then lp = New linePoints(lp.p2, lp.p1)

            sideOpposite = lp.p2.Y - lp.p1.Y
            If lp.p1.X < lp.p2.X Then sideOpposite = lp.p1.Y - lp.p2.Y
            Dim angle = Math.Atan(sideOpposite / Math.Abs(lp.p1.X - lp.p2.X)) * 57.2958

            If Math.Abs(angle - hAngle) < 2 Then
                dst2.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth, task.lineType)
                dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
                ptList.Add(lp)
            End If
        Next
        labels(2) = "There are " + CStr(ptList.Count) + " lines similar to the horizon " + Format(hAngle, fmt1) + " degrees"
    End Sub
End Class






Public Class Line_VerticalHorizontal : Inherits TaskParent
    Dim verts As New Line_Vertical
    Dim horiz As New Line_Horizontal
    Public vList As New SortedList(Of Integer, linePoints)(New compareAllowIdenticalIntegerInverted)
    Public hList As New SortedList(Of Integer, linePoints)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        task.gOptions.LineWidth.Value = 2
        labels(3) = "Vertical lines are in yellow and horizontal lines in red."
        desc = "Highlight both vertical and horizontal lines"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        dst2 = src.Clone
        verts.Run(src)
        horiz.Run(src)

        vList.Clear()
        hList.Clear()

        dst3.SetTo(0)
        For Each lp In verts.ptList
            vList.Add(lp.length, lp)
            DrawLine(dst2, lp.p1, lp.p2, task.HighlightColor)
            DrawLine(dst3, lp.p1, lp.p2, task.HighlightColor)
        Next

        For Each lp In horiz.ptList
            hList.Add(lp.length, lp)
            DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Red)
            DrawLine(dst3, lp.p1, lp.p2, cv.Scalar.Red)
        Next
        labels(2) = "Number of lines identified (vertical/horizontal): " + CStr(vList.Count) + "/" + CStr(hList.Count)
    End Sub
End Class








Public Class Line_Canny : Inherits TaskParent
    Dim canny As New Edge_Basics
    Dim lines As New Line_Basics
    Public lpList As New List(Of linePoints)
    Public Sub New()
        optiBase.FindSlider("Canny Aperture").Value = 7
        optiBase.FindSlider("Min Line Length").Value = 30
        labels(3) = "Input to Line_Basics"
        desc = "Find lines in the Canny output"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        canny.Run(src)
        dst3 = canny.dst2.Clone

        lines.Run(canny.dst2)
        dst2 = lines.dst2
        lpList = New List(Of linePoints)(task.lpList)
        labels(2) = "Number of lines identified: " + CStr(lpList.Count)
    End Sub
End Class






Public Class Line_Cells : Inherits TaskParent
    Dim lines As New Line_Basics
    Public lpList As New List(Of linePoints)
    Public Sub New()
        desc = "Identify all lines in the RedCloud_Basics cell boundaries"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        dst2 = getRedCloud(src, labels(2))

        lines.Run(dst2.Clone)
        dst3 = lines.dst2
        lpList = New List(Of linePoints)(task.lpList)
        labels(3) = "Number of lines identified: " + CStr(lpList.Count)
    End Sub
End Class









Public Class Line_VerticalHorizontalCells : Inherits TaskParent
    Dim lines As New FeatureLine_Finder
    Dim hulls As New RedCloud_Hulls
    Public Sub New()
        labels(2) = "RedCloud_Hulls output with lines highlighted"
        desc = "Identify the lines created by the RedCloud Cells and separate vertical from horizontal"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        hulls.Run(src)
        dst2 = hulls.dst2

        lines.Run(dst2.Clone)
        dst3 = src
        For i = 0 To lines.sortedHorizontals.Count - 1
            Dim index = lines.sortedHorizontals.ElementAt(i).Value
            Dim p1 = lines.lines2D(index), p2 = lines.lines2D(index + 1)
            DrawLine(dst3, p1, p2, cv.Scalar.Yellow)
        Next
        For i = 0 To lines.sortedVerticals.Count - 1
            Dim index = lines.sortedVerticals.ElementAt(i).Value
            Dim p1 = lines.lines2D(index), p2 = lines.lines2D(index + 1)
            DrawLine(dst3, p1, p2, cv.Scalar.Blue)
        Next
        labels(3) = CStr(lines.sortedVerticals.Count) + " vertical and " + CStr(lines.sortedHorizontals.Count) + " horizontal lines identified in the RedCloud output"
    End Sub
End Class






Public Class Line_VerticalHorizontal1 : Inherits TaskParent
    Dim lines As New Line_Basics
    Dim nearest As New Line_Nearest
    Public Sub New()
        task.gOptions.LineWidth.Value = 2
        desc = "Find all the lines in the color image that are parallel to gravity or the horizon using distance to the line instead of slope."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        Dim pixelDiff = task.gOptions.pixelDiffThreshold

        dst2 = src.Clone
        lines.Run(src)
        If standaloneTest() Then dst3 = lines.dst2

        nearest.lp = task.gravityVec
        DrawLine(dst2, task.gravityVec.p1, task.gravityVec.p2, white)
        For Each lp In task.lpList
            Dim ptInter = IntersectTest(lp.p1, lp.p2, task.gravityVec.p1, task.gravityVec.p2,
                                        New cv.Rect(0, 0, src.Width, src.Height))
            If ptInter.X >= 0 And ptInter.X < dst2.Width And ptInter.Y >= 0 And ptInter.Y < dst2.Height Then
                Continue For
            End If

            nearest.pt = lp.p1
            nearest.Run(Nothing)
            Dim d1 = nearest.distance

            nearest.pt = lp.p2
            nearest.Run(Nothing)
            Dim d2 = nearest.distance

            If Math.Abs(d1 - d2) <= pixelDiff Then
                DrawLine(dst2, lp.p1, lp.p2, task.HighlightColor)
            End If
        Next

        DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, white)
        nearest.lp = task.horizonVec
        For Each lp In task.lpList
            Dim ptInter = IntersectTest(lp.p1, lp.p2, task.horizonVec.p1, task.horizonVec.p2, New cv.Rect(0, 0, src.Width, src.Height))
            If ptInter.X >= 0 And ptInter.X < dst2.Width And ptInter.Y >= 0 And ptInter.Y < dst2.Height Then Continue For

            nearest.pt = lp.p1
            nearest.Run(Nothing)
            Dim d1 = nearest.distance

            nearest.pt = lp.p2
            nearest.Run(Nothing)
            Dim d2 = nearest.distance

            If Math.Abs(d1 - d2) <= pixelDiff Then
                DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Red)
            End If
        Next
        labels(2) = "Slope for gravity is " + Format(task.gravityVec.slope, fmt1) + ".  Slope for horizon is " + Format(task.horizonVec.slope, fmt1)
    End Sub
End Class






Public Class Line_DisplayInfoOld : Inherits TaskParent
    Public tcells As New List(Of tCell)
    Dim canny As New Edge_Basics
    Dim blur As New Blur_Basics
    Public distance As Integer
    Public maskCount As Integer
    Dim myCurrentFrame As Integer = -1
    Public Sub New()
        dst1 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels(2) = "When running standaloneTest(), a pair of random points is used to test the algorithm."
        desc = "Display the line provided in mp"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        dst2 = src
        If standaloneTest() And task.heartBeat Then
            Dim tc As tCell
            tcells.Clear()
            For i = 0 To 2 - 1
                tc.center = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                tcells.Add(tc)
            Next
        End If
        If tcells.Count < 2 Then Exit Sub

        If myCurrentFrame < task.frameCount Then
            canny.Run(src)
            blur.Run(canny.dst2)
            myCurrentFrame = task.frameCount
        End If
        dst1.SetTo(0)
        Dim p1 = tcells(0).center
        Dim p2 = tcells(1).center
        DrawLine(dst1, p1, p2, 255)

        dst3.SetTo(0)
        blur.dst2.Threshold(1, 255, cv.ThresholdTypes.Binary).CopyTo(dst3, dst1)
        distance = p1.DistanceTo(p2)
        maskCount = dst3.CountNonZero

        For Each tc In tcells
            'dst2.Rectangle(tc.rect, myHighlightColor)
            'dst2.Rectangle(tc.searchRect, white, task.lineWidth)
            SetTrueText(tc.strOut, New cv.Point(tc.rect.X, tc.rect.Y))
        Next

        strOut = "Mask count = " + CStr(maskCount) + ", Expected count = " + CStr(distance) + " or " + Format(maskCount / distance, "0%") + vbCrLf
        DrawLine(dst2, p1, p2, task.HighlightColor)

        strOut += "Color changes when correlation falls below threshold and new line is detected." + vbCrLf +
                  "Correlation coefficient is shown with the depth in meters."
        SetTrueText(strOut, 3)
    End Sub
End Class




Public Class Line_PointSlope : Inherits TaskParent
    Dim lines As New Line_Basics
    Dim knn As New KNN_NNBasics
    Public bestLines As New List(Of linePoints)
    Const lineCount As Integer = 3
    Const searchCount As Integer = 100
    Public Sub New()
        knn.options.knnDimension = 5 ' slope, p1.x, p1.y, p2.x, p2.y
        If standalone Then task.gOptions.setDisplay1()
        labels = {"", "TrainInput to KNN", "Tracking these lines", "Query inputs to KNN"}
        desc = "Find the 3 longest lines in the image and identify them from frame to frame using the point and slope."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        lines.Run(src)
        dst2 = src

        If bestLines.Count < lineCount Or task.heartBeat Then
            dst3.SetTo(0)
            bestLines.Clear()
            knn.queries.Clear()
            For Each lp In task.lpList
                bestLines.Add(lp)
                For j = 0 To knn.options.knnDimension - 1
                    knn.queries.Add(Choose(j + 1, lp.slope, lp.p1.X, lp.p1.Y, lp.p2.X, lp.p2.Y))
                Next
                DrawLine(dst3, lp.p1, lp.p2, task.HighlightColor)
                If bestLines.Count >= lineCount Then Exit For
            Next
        End If

        dst1.SetTo(0)
        knn.trainInput.Clear()
        For Each lp In task.lpList
            For j = 0 To knn.options.knnDimension - 1
                knn.trainInput.Add(Choose(j + 1, lp.slope, lp.p1.X, lp.p1.Y, lp.p2.X, lp.p2.Y))
            Next
            dst1.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth, task.lineType)
        Next
        If knn.trainInput.Count = 0 Then
            SetTrueText("There were no lines detected!  Were there any unusual settings for this run?", 3)
            Exit Sub
        End If

        knn.Run(src)
        If knn.result Is Nothing Then Exit Sub
        Dim nextLines As New List(Of linePoints)
        Dim usedBest As New List(Of Integer)
        Dim index As Integer
        For i = 0 To knn.result.GetUpperBound(0)
            For j = 0 To knn.result.GetUpperBound(1)
                index = knn.result(i, j)
                If usedBest.Contains(index) = False Then Exit For
            Next
            usedBest.Add(index)

            If index * knn.options.knnDimension + 4 < knn.trainInput.Count Then
                Dim mps = New linePoints(New cv.Point2f(knn.trainInput(index * knn.options.knnDimension + 0), knn.trainInput(index * knn.options.knnDimension + 1)),
                          New cv.Point2f(knn.trainInput(index * knn.options.knnDimension + 2), knn.trainInput(index * knn.options.knnDimension + 3)))
                mps.slope = knn.trainInput(index * knn.options.knnDimension)
                nextLines.Add(mps)
            End If
        Next

        bestLines = New List(Of linePoints)(nextLines)
        For Each ptS In bestLines
            DrawLine(dst2, ptS.p1, ptS.p2, task.HighlightColor)
            DrawLine(dst1, ptS.p1, ptS.p2, cv.Scalar.Red)
        Next
    End Sub
End Class





Public Class Line_TopX : Inherits TaskParent
    Dim lines As New Line_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        labels(3) = "The top X lines by length..."
        desc = "Isolate the top X lines by length - lines are already sorted by length."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2
        labels(2) = lines.labels(2)

        dst3.SetTo(0)
        For i = 0 To 9
            Dim lp = task.lpList(i)
            dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next
    End Sub
End Class




Public Class Line_Matching : Inherits TaskParent
    Public lines As New Line_Basics
    Public options As New Options_Line
    Dim lineMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Dim lpList As New List(Of linePoints)
    Public Sub New()
        optiBase.FindSlider("Min Line Length").Value = 30
        labels(2) = "Highlighted lines were combined from 2 lines.  Click on Line_Core in Treeview to see."
        desc = "Combine lines that are approximately the same line."
    End Sub
    Private Function combine2Lines(lp1 As linePoints, lp2 As linePoints) As linePoints
        If Math.Abs(lp1.slope) >= 1 Then
            If lp1.p1.Y < lp2.p1.Y Then
                Return New linePoints(lp1.p1, lp2.p2)
            Else
                Return New linePoints(lp2.p1, lp1.p2)
            End If
        Else
            If lp1.p1.X < lp2.p1.X Then
                Return New linePoints(lp1.p1, lp2.p2)
            Else
                Return New linePoints(lp2.p1, lp1.p2)
            End If
        End If
    End Function
    Public Overrides sub runAlg(src As cv.Mat)
        options.RunOpt()
        dst2 = src.Clone

        If standalone Then lines.Run(src)

        Dim tolerance = 0.1
        Dim newSet As New List(Of linePoints)
        Dim removeList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        Dim addList As New List(Of linePoints)
        Dim combineCount As Integer
        For i = 0 To task.lpList.Count - 1
            Dim lp = task.lpList(i)
            Dim lpRemove As Boolean = False
            For j = 0 To 1
                Dim pt = Choose(j + 1, lp.p1, lp.p2)
                Dim val = lineMap.Get(Of Integer)(pt.Y, pt.X)
                If val = 0 Then Continue For
                Dim mp = lpList(val - 1)
                If Math.Abs(mp.slope - lp.slope) < tolerance Then
                    Dim lpNew = combine2Lines(lp, mp)
                    If lpNew IsNot Nothing Then
                        addList.Add(lpNew)
                        DrawLine(dst2, lpNew.p1, lpNew.p2, task.HighlightColor)
                        If removeList.Values.Contains(j) = False Then removeList.Add(j, j)
                        lpRemove = True
                        combineCount += 1
                    End If
                End If
            Next
            If lpRemove Then
                If removeList.Values.Contains(i) = False Then removeList.Add(i, i)
            End If
        Next

        For i = 0 To removeList.Count - 1
            task.lpList.RemoveAt(removeList.ElementAt(i).Value)
        Next

        For Each lp In addList
            task.lpList.Add(lp)
        Next
        lpList = New List(Of linePoints)(task.lpList)
        lineMap.SetTo(0)
        For i = 0 To lpList.Count - 1
            Dim lp = lpList(i)
            If lp.length > options.minLength Then lineMap.Line(lp.p1, lp.p2, i + 1, 2, cv.LineTypes.Link8)
        Next
        lineMap.ConvertTo(dst3, cv.MatType.CV_8U)
        dst3 = dst3.Threshold(0, cv.Scalar.White, cv.ThresholdTypes.Binary)
        If task.heartBeat Then
            labels(2) = CStr(task.lpList.Count) + " lines were input and " + CStr(combineCount) +
                        " lines were matched to the previous frame"
        End If
    End Sub
End Class







Public Class Line_Info : Inherits TaskParent
    Public lpInput As New List(Of linePoints)
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        labels(2) = "Click on the oversized line to get details about the line"
        labels(3) = "Details from the point cloud for the selected line"
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Display details about the line selected."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        labels(2) = task.lines.labels(2)
        If standaloneTest() Then task.lines.Run(src)

        dst2 = src
        For Each lp In task.lpList
            dst2.Line(lp.p1, lp.p2, white, 2, cv.LineTypes.Link8)
        Next

        If task.mouseClickFlag Or task.firstPass Then
            Dim index = task.lines.lpMap.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X)
            Dim lp = task.lpList(index)

            strOut = "Click on any line (below left) to get details on that line " + vbCrLf
            strOut += "Lines identified in the image: " + CStr(task.lpList.Count) + vbCrLf + vbCrLf
            strOut += "index = " + CStr(lp.index) + vbCrLf
            strOut += "p1 = " + lp.p1.ToString + ", p2 = " + lp.p2.ToString + vbCrLf + vbCrLf
            strOut += "Pointcloud range X " + Format(lp.mmX.minVal, fmt3) + " to " +
                       Format(lp.mmX.maxVal, fmt3) + vbCrLf
            strOut += "Pointcloud range Y " + Format(lp.mmY.minVal, fmt3) + " to " +
                       Format(lp.mmY.maxVal, fmt3) + vbCrLf
            strOut += "Pointcloud range Z " + Format(lp.mmZ.minVal, fmt3) + " to " +
                       Format(lp.mmZ.maxVal, fmt3) + vbCrLf + vbCrLf

            strOut += "mm per pixel = " + Format(lp.mmPerPixel * 1000, fmt1) + vbCrLf

            strOut += "Slope = " + Format(lp.slope, fmt3) + vbCrLf
            strOut += "X-intercept = " + Format(lp.xIntercept, fmt1) + vbCrLf
            strOut += "Y-intercept = " + Format(lp.yIntercept, fmt1) + vbCrLf
            strOut += vbCrLf + "Remember: the Y-Axis is inverted - Y increases down so slopes are inverted."

            dst3.SetTo(0)
            DrawLine(dst3, lp.p1, lp.p2, 255)
            dst3.Rectangle(lp.rect, 255, task.lineWidth, task.lineType)
        End If
        SetTrueText(strOut, 1)
    End Sub
End Class





Public Class Line_ViewRight : Inherits TaskParent
    Dim lines As New Line_Basics
    Public Sub New()
        desc = "Find lines in the right image"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        lines.Run(task.rightView)
        dst2 = lines.dst2
        labels(2) = lines.labels(2)
    End Sub
End Class






Public Class Line_LeftRight : Inherits TaskParent
    Dim lineCore As New Line_Core
    Public Sub New()
        task.gOptions.displayDst1.Checked = True
        desc = "Show lines in both the right and left images."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        task.lines.Run(src) ' the default is the left view.

        dst2 = task.lines.dst2.Clone
        labels(2) = "Left view" + task.lines.labels(2)

        dst1 = task.rightView
        lineCore.Run(task.rightView)
        dst3 = lineCore.dst2.Clone
        labels(3) = "Right View: " + lineCore.labels(2)

        If standalone Then
            If task.gOptions.debugChecked Then
                dst2.SetTo(0, task.noDepthMask)
                dst3.SetTo(0, task.noDepthMask)
            End If
        Else
            If task.toggleOnOff Then
                dst2.SetTo(0, task.noDepthMask)
                dst3.SetTo(0, task.noDepthMask)
            End If
        End If
    End Sub
End Class