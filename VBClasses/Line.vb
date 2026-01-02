Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Line_Basics : Inherits TaskParent
        Public lpList As New List(Of lpData)
        Public rawLines As New Line_Core
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            If standalone Then task.gOptions.showMotionMask.Checked = True
            desc = "If line is NOT in motion mask, then keep it.  If line is in motion mask, add it."
        End Sub
        Private Function lpMotion(lp As lpData) As Boolean
            ' return true if either line endpoint was in the motion mask.
            If task.motionMask.Get(Of Byte)(lp.p1.Y, lp.p1.X) Then Return True
            If task.motionMask.Get(Of Byte)(lp.p2.Y, lp.p2.X) Then Return True
            Return False
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.algorithmPrep = False Then Exit Sub ' only run as a task algorithm.

            If src.Channels <> 1 Or src.Type <> cv.MatType.CV_8U Then src = task.gray.Clone
            If lpList.Count <= 1 Then
                task.motionMask.SetTo(255)
                rawLines.Run(src)
                lpList = New List(Of lpData)(rawLines.lpList)
            End If

            Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
            Dim count As Integer
            For Each lp In lpList
                If lpMotion(lp) = False Then
                    lp.age += 1
                    sortlines.Add(lp.length, lp)
                    count += 1
                End If
            Next

            rawLines.Run(src)

            For Each lp In rawLines.lpList
                If lpMotion(lp) Then
                    lp.age = 1
                    sortlines.Add(lp.length, lp)
                End If

                If lp.ptCenter.X > task.workRes.Width Then Dim k = 0
            Next

            lpList.Clear()
            For Each lp In sortlines.Values
                lp.index = lpList.Count
                lpList.Add(lp)

                If lp.ptCenter.X > task.workRes.Width Then Dim k = 0
            Next

            dst1.SetTo(0)
            dst2.SetTo(0)
            For Each lp In lpList
                dst1.Line(lp.p1, lp.p2, lp.index + 1, 1, cv.LineTypes.Link4)
                dst2.Line(lp.p1, lp.p2, lp.color, task.lineWidth, task.lineType)
            Next

            ' so we don't have to check the lplist.count every time we need the longest line...
            If lpList.Count = 0 Then
                lpList.Add(task.gravityIMU)
            End If
            If task.frameCount > 10 Then If task.lpD.rect.Width = 0 Then task.lpD = lpList(0)
            task.lineLongest = lpList(0)

            labels(2) = CStr(lpList.Count) + " lines - " + CStr(lpList.Count - count) + " were new"
        End Sub
    End Class






    Public Class Line_Core : Inherits TaskParent
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
            If standaloneTest() Then input = task.gravityIMU
            dst2.SetTo(0)
            dst2.Line(input.p1, input.p2, white, task.lineWidth, task.lineType)

            output = computePerp(input)
            DrawCircle(dst2, input.ptCenter, task.DotSize + 2, cv.Scalar.Red)
            dst2.Line(output.p1, output.p2, yellow, task.lineWidth, task.lineType)

            If standaloneTest() Then SetTrueText("The line displayed at left is the gravity vector.", 3)
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
                vbc.DrawLine(dst2, lp)
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
                lp = task.lineLongest
                If lp.length = 0 Then Exit Sub
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
            vbc.DrawLine(dst2, lpOutput)
            lpOutput.drawRoRect(dst2)

            If standalone Then lp = lpOutput

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






    Public Class Line_LeftRight : Inherits TaskParent
        Public leftLines As New List(Of lpData)
        Public rightLines As New List(Of lpData)
        Dim lines As New Line_Core
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






    Public Class Line_Select : Inherits TaskParent
        Public delaunay As New Delaunay_LineSelect
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Select a line with mouse movement and put the selection into task.lpD."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            delaunay.Run(src)
            dst2 = delaunay.dst1
            labels(2) = delaunay.labels(2)
            strOut = task.lpD.displayCell(dst3)
            SetTrueText(strOut, 1) ' the line info is already prepped in strout in delaunay.
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
                        vbc.DrawLine(dst3, lp)
                    Next
                End If
            End If

            labels(2) = "There were " + CStr(lpList.Count) + " neighbors that formed good lines."
        End Sub
    End Class
End Namespace