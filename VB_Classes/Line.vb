Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Line_Basics : Inherits TaskParent
    Dim lines As New Line_Detector
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Collect lines across frames using the motion mask."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim histogram As New cv.Mat

        lines.Run(task.grayMotion)

        Dim lpMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Dim index As Integer = 1
        Dim tmpList As New List(Of lpData)(lines.lpList)
        For Each lp In lines.lpList
            lpMap.Line(lp.p1, lp.p2, index, task.lineWidth, cv.LineTypes.Link4)
            index += 1
        Next

        cv.Cv2.CalcHist({lpMap}, {0}, task.motionMask, histogram, 1, {lines.lpList.Count}, New cv.Rangef() {New cv.Rangef(1, lines.lpList.Count)})

        Dim histarray(lines.lpList.Count - 1) As Single
        Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)

        For i = histarray.Count - 1 To 0 Step -1
            If histarray(i) = 0 Then lines.lpList.RemoveAt(i)
        Next

        dst3.SetTo(0)
        For Each lp In lines.lpList
            dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, cv.LineTypes.Link4)
            lp.age = 1
            tmpList.Add(lp)
        Next

        index = 0
        task.lpList.Clear()
        dst2 = src
        For Each lp In tmpList
            lp.index = index
            task.lpList.Add(lp)
            dst2.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth, task.lineType)
        Next
        labels(2) = CStr(task.lpList.Count) + " lines were found."
        labels(3) = CStr(lines.lpList.Count) + " lines were in the motion mask."
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
        redRadio = optiBase.findRadio("Show Top intercepts")
        greenRadio = optiBase.findRadio("Show Bottom intercepts")
        yellowRadio = optiBase.findRadio("Show Right intercepts")
        blueRadio = optiBase.findRadio("Show Left intercepts")
        labels(2) = "Use mouse in right image to highlight lines"
        desc = "An alternative way to highlight line segments with common slope"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
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
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        task.lines.Run(src)
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






Public Class Line_Vertical : Inherits TaskParent
    Public ptList As New List(Of lpData)
    Public Sub New()
        desc = "Find all the vertical lines with gravity vector"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        task.lines.Run(src)
        dst3 = task.lines.dst2

        Dim p1 = task.gravityVec.p1, p2 = task.gravityVec.p2
        Dim sideOpposite = p2.X - p1.X
        If p1.Y = 0 Then sideOpposite = p1.X - p2.X
        Dim gAngle = Math.Atan(sideOpposite / dst2.Height) * 57.2958

        ptList.Clear()
        For Each lp In task.lpList
            If lp.p1.Y > lp.p2.Y Then lp = New lpData(lp.p2, lp.p1)

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
    Public ptList As New List(Of lpData)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        desc = "Find all the Horizontal lines with horizon vector"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        task.lines.Run(src)

        Dim p1 = task.horizonVec.p1, p2 = task.horizonVec.p2
        Dim sideOpposite = p2.Y - p1.Y
        If p1.X = 0 Then sideOpposite = p1.Y - p2.Y
        Dim hAngle = Math.Atan(sideOpposite / dst2.Width) * 57.2958

        ptList.Clear()
        If task.heartBeat Then dst3.SetTo(0)
        For Each lp In task.lpList
            If lp.p1.X > lp.p2.X Then lp = New lpData(lp.p2, lp.p1)

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
    Public vList As New SortedList(Of Integer, lpData)(New compareAllowIdenticalIntegerInverted)
    Public hList As New SortedList(Of Integer, lpData)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        task.gOptions.LineWidth.Value = 2
        labels(3) = "Vertical lines are in yellow and horizontal lines in red."
        desc = "Highlight both vertical and horizontal lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
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











Public Class Line_VerticalHorizontalCells : Inherits TaskParent
    Dim lines As New FeatureLine_Finder
    Dim hulls As New RedColor_Hulls
    Public Sub New()
        labels(2) = "RedColor_Hulls output with lines highlighted"
        desc = "Identify the lines created by the RedCloud Cells and separate vertical from horizontal"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
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
    Dim nearest As New XO_Line_Nearest
    Public Sub New()
        task.gOptions.LineWidth.Value = 2
        desc = "Find all the lines in the color image that are parallel to gravity or the horizon using distance to the line instead of slope."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim pixelDiff = task.gOptions.pixelDiffThreshold

        dst2 = src.Clone
        task.lines.Run(src)
        If standaloneTest() Then dst3 = task.lines.dst2

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





Public Class Line_PointSlope : Inherits TaskParent
    Dim knn As New KNN_NNBasics
    Public bestLines As New List(Of lpData)
    Const lineCount As Integer = 3
    Const searchCount As Integer = 100
    Public Sub New()
        knn.options.knnDimension = 5 ' slope, p1.x, p1.y, p2.x, p2.y
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "TrainInput to KNN", "Tracking these lines", "Query inputs to KNN"}
        desc = "Find the 3 longest lines in the image and identify them from frame to frame using the point and slope."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.lines.Run(src)
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
        Dim nextLines As New List(Of lpData)
        Dim usedBest As New List(Of Integer)
        Dim index As Integer
        For i = 0 To knn.result.GetUpperBound(0)
            For j = 0 To knn.result.GetUpperBound(1)
                index = knn.result(i, j)
                If usedBest.Contains(index) = False Then Exit For
            Next
            usedBest.Add(index)

            If index * knn.options.knnDimension + 4 < knn.trainInput.Count Then
                Dim mps = New lpData(New cv.Point2f(knn.trainInput(index * knn.options.knnDimension + 0), knn.trainInput(index * knn.options.knnDimension + 1)),
                          New cv.Point2f(knn.trainInput(index * knn.options.knnDimension + 2), knn.trainInput(index * knn.options.knnDimension + 3)))
                mps.slope = knn.trainInput(index * knn.options.knnDimension)
                nextLines.Add(mps)
            End If
        Next

        bestLines = New List(Of lpData)(nextLines)
        For Each ptS In bestLines
            DrawLine(dst2, ptS.p1, ptS.p2, task.HighlightColor)
            DrawLine(dst1, ptS.p1, ptS.p2, cv.Scalar.Red)
        Next
    End Sub
End Class






Public Class Line_ViewRight : Inherits TaskParent
    Public Sub New()
        desc = "Find lines in the right image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.lines.Run(task.rightView)
        dst2 = task.lines.dst2
        labels(2) = task.lines.labels(2)
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
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then input = task.gravityVec
        dst2.SetTo(0)
        DrawLine(dst2, input.p1, input.p2, white)

        output = task.gravityHorizon.computePerp(input)
        DrawCircle(dst2, midPoint, task.DotSize + 2, cv.Scalar.Red)
        DrawLine(dst2, output.p1, output.p2, cv.Scalar.Yellow)
    End Sub
End Class







Public Class Line_Stable : Inherits TaskParent
    Dim lines As New Line_Detector
    Public lpStable As New List(Of lpData)
    Public ptStable As New List(Of cv.Point)
    Public Sub New()
        desc = "Identify features that consistently present in the image - with motion ignored."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then dst1 = src.Clone Else src.CopyTo(dst1, task.motionMask)

        lines.Run(dst1)

        Static lpStable As New List(Of lpData)
        Static ptStable As New List(Of cv.Point)
        For j = 0 To lines.ptList.Count - 1
            Dim pt = lines.ptList(j)
            Dim lp = lines.lpList(j)
            Dim index = ptStable.IndexOf(pt)
            If index < 0 Then
                lpStable.Add(lp)
                ptStable.Add(pt)
            Else
                lp = lpStable(index)
                lp.pt = pt
                lp.age += 1
                lpStable(index) = lp
                ptStable(index) = pt
            End If
        Next

        Dim removelist As New List(Of Integer)
        For i = 0 To ptStable.Count - 1
            Dim pt = ptStable(i)
            If lines.ptList.Contains(pt) = False Then removelist.Add(i)
        Next

        For i = removelist.Count - 1 To 0 Step -1
            Dim index = removelist(i)
            lpStable.RemoveAt(index)
            ptStable.RemoveAt(index)
        Next

        dst2 = src
        dst3.SetTo(0)
        For Each lp In lpStable
            dst2.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth)
            dst3.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth)
            SetTrueText(CStr(lp.age), lp.pt, 3)
        Next

        labels(3) = "There were " + CStr(lpStable.Count) + " stable lines found."
    End Sub
End Class






Public Class Line_Detector : Inherits TaskParent
    Dim ld As cv.XImgProc.FastLineDetector
    Public lpList As New List(Of lpData)
    Public ptList As New List(Of cv.Point)
    Public subsetRect As cv.Rect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
    Public Sub New()
        dst2 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset " +
               "rectangle (provided externally)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If src.Type <> cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_8U)

        Dim lines = ld.Detect(src(subsetRect))

        lpList.Clear()
        ptList.Clear()
        For Each v In lines
            If v(0) >= 0 And v(0) <= src.Cols And v(1) >= 0 And v(1) <= src.Rows And
               v(2) >= 0 And v(2) <= src.Cols And v(3) >= 0 And v(3) <= src.Rows Then
                Dim p1 = validatePoint(New cv.Point(CInt(v(0) + subsetRect.X), CInt(v(1) + subsetRect.Y)))
                Dim p2 = validatePoint(New cv.Point(CInt(v(2) + subsetRect.X), CInt(v(3) + subsetRect.Y)))
                Dim lp = New lpData(p1, p2)
                lp.rect = ValidateRect(lp.rect)
                lp.mask = dst2(lp.rect)
                lp.index = lpList.Count
                lpList.Add(lp)
                ptList.Add(New cv.Point(CInt(lp.p1.X), CInt(lp.p1.Y)))
            End If
        Next

        dst2.SetTo(0)
        For Each lp In lpList
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next
        labels(2) = CStr(lpList.Count) + " lines were detected in the current frame"
    End Sub
End Class






Public Class Line_Info : Inherits TaskParent
    Public lpInput As New List(Of lpData)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(2) = "Click on the oversized line to get details about the line"
        labels(3) = "Details from the point cloud for the selected line"
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Display details about the line selected."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then runLines(src)
        labels(2) = task.lines.labels(2)

        dst2 = src
        For Each lp In task.lpList
            dst2.Line(lp.p1, lp.p2, white, 2, cv.LineTypes.Link8)
        Next

        If task.mouseClickFlag Or task.firstPass Then
            'Dim index = task.lines.lpMap.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X)
            'Dim lp = task.lpList(index)

            'strOut = "Click on any line (below left) to get details on that line " + vbCrLf
            'strOut += "Lines identified in the image: " + CStr(task.lpList.Count) + vbCrLf + vbCrLf
            'strOut += "index = " + CStr(lp.index) + vbCrLf
            'strOut += "p1 = " + lp.p1.ToString + ", p2 = " + lp.p2.ToString + vbCrLf + vbCrLf
            'strOut += "Pointcloud range X " + Format(lp.mmX.minVal, fmt3) + " to " +
            '           Format(lp.mmX.maxVal, fmt3) + vbCrLf
            'strOut += "Pointcloud range Y " + Format(lp.mmY.minVal, fmt3) + " to " +
            '           Format(lp.mmY.maxVal, fmt3) + vbCrLf
            'strOut += "Pointcloud range Z " + Format(lp.mmZ.minVal, fmt3) + " to " +
            '           Format(lp.mmZ.maxVal, fmt3) + vbCrLf + vbCrLf

            'strOut += "Slope = " + Format(lp.slope, fmt3) + vbCrLf
            'strOut += "X-intercept = " + Format(lp.xIntercept, fmt1) + vbCrLf
            'strOut += "Y-intercept = " + Format(lp.yIntercept, fmt1) + vbCrLf
            'strOut += vbCrLf + "Remember: the Y-Axis is inverted - Y increases down so slopes are inverted."

            'dst3.SetTo(0)
            'DrawLine(dst3, lp.p1, lp.p2, 255)
            'dst3.Rectangle(lp.rect, 255, task.lineWidth, task.lineType)
        End If
        SetTrueText(strOut, 1)
    End Sub
End Class