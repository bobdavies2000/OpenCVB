Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp : Imports OpenCvSharp.XImgProc
Public Class Line_Basics_TA : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public lpLast As New List(Of lpData)
    Public Sub New()
        dst3 = New Mat(dst3.Size, MatType.CV_8U, 0)
        desc = "Run FLD (Fast Line Detector) with sobel input."
    End Sub
    Public Shared Function removeDuplicates(coreList As List(Of lpData)) As List(Of lpData)
        Dim lpList As New List(Of lpData)

        Dim removeNearDuplicates As Boolean = True
        If removeNearDuplicates Then
            Dim edgeMap As New Mat(task.workRes, MatType.CV_8U, 0)
            For Each lp In coreList
                Dim val1 = edgeMap.Get(Of Byte)(lp.ptE1.Y, lp.ptE1.X)
                Dim val2 = edgeMap.Get(Of Byte)(lp.ptE1.Y, lp.ptE1.X)
                If val1 > 0 And val2 > 0 Then Continue For

                lp.index = lpList.Count + 1

                Dim gridIndex = task.gridMap.Get(Of Integer)(Math.Floor(lp.ptE1.Y), Math.Floor(lp.ptE1.X))
                edgeMap(task.gridNabeRects(gridIndex)).SetTo(lp.index)
                lpList.Add(lp)
            Next
        Else
            Return coreList
        End If
        Return lpList
    End Function
    Public Shared Function updateAgesAndLongest(inputList As List(Of lpData), lastList As List(Of lpData)) As Single
        Static lpFind As New Line_FindClosest
        lpFind.lpList = inputList
        lpFind.LastList = lastList
        For Each lp In inputList
            lpFind.inputLine = lp
            lpFind.Run(Nothing)
            Dim lpLast = lpFind.closestLine
            If lpLast IsNot Nothing Then
                Dim lpCurr = lp
                lpCurr.age = lpLast.age + 1
                If lpCurr.age >= 1000 Then lpCurr.age = 10
            End If
        Next

        Dim lpAges As New List(Of Single)
        For Each lp In inputList
            lpAges.Add(lp.age)
        Next

        Static gravity = task.lpGravity
        Dim noLineFound As Boolean = True
        If inputList.Count > 0 Then
            If task.longestLine = gravity Or task.longestLine Is Nothing Then task.longestLine = inputList(0)
            lpFind.inputLine = task.longestLine
            lpFind.Run(emptyMat)
            Dim lpTmp = lpFind.closestLine

            If lpTmp Is Nothing Then
                noLineFound = True
            Else
                task.longestLine = New lpData(lpTmp.ptE1, lpTmp.ptE2)
                task.longestLine.age = lpTmp.age
            End If
            noLineFound = False
        End If

        If noLineFound Then
            gravity = task.lpGravity
            task.longestLine = task.lpGravity
            inputList.Add(task.longestLine) ' need to always have something in lplist...
            Return 0
        End If

        Return lpAges.Average
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Or src.Type <> MatType.CV_8U Then src = task.gray.Clone

        lpLast = New List(Of lpData)(lpList)
        If task.optionsChanged Then lpLast.Clear()

        If task.fOptions.LineCombo.Text = "Fast Line Detection" Then
            Static basicsFLD As New Line_Basics
            basicsFLD.Run(src)
            dst2 = basicsFLD.dst2
            labels = basicsFLD.labels
        Else
            Static basicsLSD As New LineSeg_Basics
            basicsLSD.Run(src)
            dst2 = basicsLSD.dst2
            labels = basicsLSD.labels
        End If

        dst3.SetTo(0)
        For Each lp In lpList
            Line(dst3, lp.p1, lp.p2, 255, task.lineWidth)
        Next

        Line(dst3, task.longestLine.p1, task.longestLine.p2, 255, task.lineWidth + 1)
        labels(3) = CStr(task.lines.lpList.Count) + " lines.  Highlighted line is the current longest line."

        For Each lp In lpList
            SetTrueText(CStr(lp.age), New cv.Point(lp.ptCenter.X + 2, lp.ptCenter.Y + 2), 3)
        Next
    End Sub
End Class





Public Class Line_Basics : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public core As New Line_Core
    Public Sub New()
        desc = "Run FLD (Fast Line Detector) With sobel input."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Or src.Type <> MatType.CV_8U Then src = task.gray.Clone

        core.Run(src)
        dst2 = core.dst2

        Dim lastList = New List(Of lpData)(lpList)
        lpList = Line_Basics_TA.removeDuplicates(core.lpList)
        Dim averageAge = Line_Basics_TA.updateAgesAndLongest(lpList, lastList)

        labels(2) = "Fast Line Detector (FLD): " + CStr(task.lines.lpList.Count) + " lines found.  Line age is also shown." +
                    " Average age = " + If(task.lines.lpList.Count > 0, averageAge.ToString(fmt1), "0")

        dst3 = task.lines.dst3
        For Each lp In task.lines.lpList
            SetTrueText(CStr(lp.age), New cv.Point(lp.ptCenter.X + 2, lp.ptCenter.Y + 2), 3)
        Next

        task.lines.lpList = New List(Of lpData)(lpList)
    End Sub
End Class






Public Class XR_Line_RawFLD : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public core As New Line_Core
    Public Sub New()
        desc = "Run FLD (Fast Line Detector) With sobel input."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.color.Clone
        If src.Channels <> 1 Or src.Type <> MatType.CV_8U Then src = task.gray.Clone

        core.Run(src)
        lpList = New List(Of lpData)(core.lpList)

        If standaloneTest() Then
            For Each lp In lpList
                Line(dst2, lp.p1, lp.p2, task.highlight, task.lineWidth)
            Next
        End If

        labels(2) = CStr(lpList.Count) + " lines found."
    End Sub
End Class






Public Class Line_Core : Inherits TaskParent
    Implements IDisposable
    Public ld As FastLineDetector
    Public lpList As New List(Of lpData)
    Public Sub New()
        ld = FastLineDetector.Create
        dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
        dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
        desc = "Use FastLineDetector (OpenCV Contrib) To find all the lines inside drawRect"
    End Sub
    Public Shared Function getRawSortedLines(lines As Vec4f()) As List(Of lpData)
        Dim lpSorted As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
        For Each v In lines
            If v(0) >= 0 And v(0) <= task.workRes.Width And v(1) >= 0 And v(1) <= task.workRes.Height And
               v(2) >= 0 And v(2) <= task.workRes.Width And v(3) >= 0 And v(3) <= task.workRes.Height Then
                Dim p1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim p2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                If p1.X >= 0 And p1.X < task.workRes.Width And p1.Y >= 0 And p1.Y < task.workRes.Height And
                   p2.X >= 0 And p2.X < task.workRes.Width And p2.Y >= 0 And p2.Y < task.workRes.Height Then
                    p1 = lpData.validatePoint(p1)
                    p2 = lpData.validatePoint(p2)
                    Dim lp = New lpData(p1, p2)
                    If lp.rect.Width = 0 Then Continue For
                    lpSorted.Add(lp.length, lp)
                End If
            End If
        Next

        Dim lpList As New List(Of lpData)(lpSorted.Values)
        Return lpList
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = task.gray
        If src.Type <> MatType.CV_8U Then src.ConvertTo(src, MatType.CV_8U)

        lpList = getRawSortedLines(ld.Detect(src))

        dst2.SetTo(0)
        For Each lp In lpList
            Line(dst2, lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next

        labels(2) = CStr(lpList.Count) + " lines were detected."
    End Sub
    Protected Overrides Sub Finalize()
        ld.Dispose()
    End Sub
End Class





Public Class XR_Line_TopBottomEdges : Inherits TaskParent
    Public tops As New List(Of lpData)
    Public bottoms As New List(Of lpData)
    Public Sub New()
        desc = "Find all the lines that intersect the top AND bottom of the image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone

        tops.Clear()
        bottoms.Clear()
        For Each lp In task.lines.lpList
            If lp.ptE1.Y = 0 And lp.ptE2.Y = dst2.Height - 1 Then
                Line(dst2, lp.p1, lp.p2, task.highlight, task.lineWidth + 1)
                tops.Add(lp)
            ElseIf lp.ptE1.Y = dst2.Height - 1 And lp.ptE2.Y = 0 Then
                Line(dst2, lp.p1, lp.p2, task.highlight, task.lineWidth + 1)
                bottoms.Add(lp)
            End If
        Next
    End Sub
End Class




Public Class XR_Line_LeftRightEdges : Inherits TaskParent
    Public lefts As New List(Of lpData)
    Public rights As New List(Of lpData)
    Public Sub New()
        desc = "Find all the lines that intersect the top AND bottom of the image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone

        lefts.Clear()
        rights.Clear()
        For Each lp In task.lines.lpList
            If lp.ptE1.X = 0 And lp.ptE2.X = dst2.Width - 1 Then
                Line(dst2, lp.p1, lp.p2, task.highlight, task.lineWidth + 1)
                lefts.Add(lp)
            ElseIf lp.ptE1.X = dst2.Width - 1 And lp.ptE2.X = 0 Then
                Line(dst2, lp.p1, lp.p2, task.highlight, task.lineWidth + 1)
                rights.Add(lp)
            End If
        Next
    End Sub
End Class




Public Class XR_Line_BasicsOld : Inherits TaskParent
    Implements IDisposable
    Public lpList As New List(Of lpData)
    Public ld As FastLineDetector
    Public motionMask As Mat = New Mat(dst2.Size, MatType.CV_8U, 255)
    Dim edges As New Edge_Sobel
    Public edgeDuplicates As New List(Of lpData) ' lines that are dropped to help LineTrack algorithms.
    Dim tiers As New Depth_Tiers
    Public Sub New()
        dst1 = New Mat(dst3.Size, MatType.CV_8U, 0)
        dst3 = New Mat(dst3.Size, MatType.CV_8U, 0)
        ld = FastLineDetector.Create
        desc = "Run FLD (Fast Line Detector) With sobel input."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.color.Clone
        If src.Channels <> 1 Or src.Type <> MatType.CV_8U Then src = task.gray.Clone

        edges.Run(src)
        labels(2) = edges.labels(2)

        Dim newList = Line_Core.getRawSortedLines(ld.Detect(edges.dst2))

        dst1.SetTo(0)
        Dim lpSorted As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
        For i = 0 To newList.Count - 1
            Dim lp = newList(i)
            lpSorted.Add(lp.length, i)
        Next

        tiers.Run(src)

        lpList.Clear()
        edgeDuplicates.Clear()
        Dim edgeMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        For index = 0 To lpSorted.Values.Count - 1
            Dim lp = newList(lpSorted.Values.ElementAt(index))
            Dim val1 = edgeMap.Get(Of Byte)(lp.ptE1.Y, lp.ptE1.X)
            Dim val2 = edgeMap.Get(Of Byte)(lp.ptE1.Y, lp.ptE1.X)
            If val1 > 0 Or val2 > 0 Then
                edgeDuplicates.Add(lp)
                Continue For
            End If

            lp.index = lpList.Count + 1

            Dim gridIndex = task.gridMap.Get(Of Integer)(Math.Floor(lp.ptE1.Y), Math.Floor(lp.ptE1.X))
            edgeMap(task.gridNabeRects(gridIndex)).SetTo(lp.index)
            lpList.Add(lp)

            Line(dst1, lp.p1, lp.p2, lp.index, task.lineWidth, LineTypes.Link4)
            Dim tierIndex = tiers.dst2.Get(Of Byte)(lp.p1.Y, lp.p1.X)
            Line(dst2, lp.p1, lp.p2, task.scalarColors(tierIndex), task.lineWidth + 1, LineTypes.Link4)
        Next

        Threshold(dst1, dst3, 0, 255, ThresholdTypes.Binary)

        labels(3) = CStr(lpList.Count) + " lines found And " + CStr(edgeDuplicates.Count) + " edge duplicates."
    End Sub
    Protected Overrides Sub Finalize()
        ld.Dispose()
    End Sub
End Class





Public Class XR_Line_BasicsLSD : Inherits TaskParent
    Implements IDisposable
    Public lpList As New List(Of lpData)
    Dim lsd As LineSegmentDetector
    Dim edges As New Edge_Sobel
    Dim tiers As New Depth_Tiers
    Public Sub New()
        dst1 = New Mat(dst3.Size, MatType.CV_8U, 0)
        dst3 = New Mat(dst3.Size, MatType.CV_8U, 0)
        labels(2) = "Edges_Basics output"
        lsd = LineSegmentDetector.Create()
        desc = "Run FLD (Fast Line Detector) With sobel input."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.color.Clone
        If src.Channels <> 1 Or src.Type <> MatType.CV_8U Then src = task.gray.Clone

        edges.Run(src)
        labels(2) = edges.labels(2)

        tiers.Run(src)

        Dim vecMat As New Mat
        lsd.Detect(src, vecMat)
        Dim vecArray() As Vec4f = Nothing
        vecMat.GetArray(Of Vec4f)(vecArray)
        lpList = Line_Core.getRawSortedLines(vecArray)

        dst1.SetTo(0)
        Dim index As Integer
        For Each lp In lpList
            index += 1
            lp.index = index
            Line(dst1, lp.p1, lp.p2, lp.index, task.lineWidth, LineTypes.Link4)
            Dim tierIndex = tiers.dst2.Get(Of Byte)(lp.p1.Y, lp.p1.X)
            Line(dst2, lp.p1, lp.p2, task.scalarColors(tierIndex), task.lineWidth + 1, LineTypes.Link4)
        Next

        Threshold(dst1, dst3, 0, 255, ThresholdTypes.Binary)

        labels(3) = CStr(lpList.Count) + " lines found"
    End Sub
    Protected Overrides Sub Finalize()
        lsd.Dispose()
    End Sub
End Class





Public Class XR_Line_WithAging : Inherits TaskParent
    Implements IDisposable
    Public lpList As New List(Of lpData)
    Public motionMask As Mat = New Mat(dst2.Size, MatType.CV_8U, 255)
    Public ld As FastLineDetector
    Public removeOverlappingLines As Boolean = True
    Public overLappingCount As Integer
    Public Sub New()
        dst0 = New Mat(dst0.Size, MatType.CV_8U, 0)
        dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
        dst3 = New Mat(dst3.Size, MatType.CV_8U, 0)
        If standalone Then task.gOptions.showMotionMask.Checked = True
        ld = FastLineDetector.Create
        desc = "If line Is Not In motion mask, Then keep it.  If line Is In motion mask, add it."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then motionMask = task.motion.motionMask

        If src.Channels <> 1 Or src.Type <> MatType.CV_8U Then src = task.gray.Clone
        CvtColor(src, dst2, ColorConversionCodes.GRAY2BGR)
        If lpList.Count <= 1 Then
            motionMask.SetTo(255)
            lpList = Line_Core.getRawSortedLines(ld.Detect(src))
        End If

        Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
        For Each lp In lpList
            If Not (motionMask.Get(Of Byte)(lp.p1.Y, lp.p1.X) Or motionMask.Get(Of Byte)(lp.p2.Y, lp.p2.X)) Then
                lp.age += 1
                sortlines.Add(lp.length, lp)
            End If
        Next
        Dim count As Integer = sortlines.Count

        lpList = Line_Core.getRawSortedLines(ld.Detect(src))

        For Each lp In lpList
            If motionMask.Get(Of Byte)(lp.p1.Y, lp.p1.X) Or motionMask.Get(Of Byte)(lp.p2.Y, lp.p2.X) Then
                sortlines.Add(lp.length, lp)
            End If
        Next
        Dim newCount As Integer = sortlines.Count - count

        lpList.Clear()
        overLappingCount = 0
        dst0.SetTo(0)
        dst1.SetTo(0)
        For Each lp In sortlines.Values
            lp.index = lpList.Count
            If removeOverlappingLines Then
                If lp.rect.Width = 0 Then Continue For
                If lp.rect.Height = 0 Then Continue For
                If CountNonZero(dst1(lp.rect)) > 0 Then
                    overLappingCount += 1
                    Continue For
                End If
            End If
            Line(dst0, lp.ptE1, lp.ptE2, lp.index + 1, task.lineWidth + 1, LineTypes.Link4)
            Line(dst1, lp.p1, lp.p2, lp.index + 1, task.lineWidth, LineTypes.Link4)
            Line(dst2, lp.p1, lp.p2, lp.color, task.lineWidth + 1, task.lineType)
            lpList.Add(lp)
        Next

        Threshold(dst1, dst3, 0, 255, ThresholdTypes.Binary)

        If lpList.Count > 0 And task.lpD IsNot Nothing Then
            If task.lpD.rect.Width = 0 Then task.lpD = lpList(0)
        End If

        labels(2) = CStr(count) + " lines retained - " + CStr(newCount) + " were New"
        If removeOverlappingLines Then labels(2) += ". " + CStr(overLappingCount) + " overlap(s) removed."
    End Sub
    Protected Overrides Sub Finalize()
        ld.Dispose()
    End Sub
End Class







Public Class Line_Perpendicular : Inherits TaskParent
    Public input As lpData
    Public output As lpData
    Public Sub New()
        labels = {"", "", "White Is the line selected For display And yellow Is perpendicular line", ""}
        desc = "Find the line perpendicular To the line created by the points provided."
    End Sub
    Public Shared Function computePerp(lp As lpData) As lpData
        Dim midPoint = New Point2f((lp.p1.X + lp.p2.X) / 2, (lp.p1.Y + lp.p2.Y) / 2)
        Dim m = If(lp.slope = 0, maxSlope, -1 / lp.slope)
        Dim b = midPoint.Y - m * midPoint.X
        Dim p1 = New Point2f(-b / m, 0)
        Dim p2 = New Point2f((task.workRes.Height - b) / m, task.workRes.Height)

        Dim w = task.workRes.Width
        Dim h = task.workRes.Height

        If p1.X < 0 Then p1 = New Point2f(0, b)
        If p1.X > w Then p1 = New Point2f(w, m * w + b)
        If p1.Y < 0 Then p1 = New Point2f(-b / m, 0)
        If p1.Y > h Then p1 = New Point2f(w, m * w + b)

        If p2.X < 0 Then p2 = New Point2f(0, b)
        If p2.X > w Then p2 = New Point2f(w, m * w + b)
        If p2.Y < 0 Then p2 = New Point2f(-b / m, 0)
        If p2.Y > h Then p2 = New Point2f(w, m * w + b)

        Return New lpData(p1, p2)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then input = task.lpGravity
        dst2.SetTo(0)
        Line(dst2, input.p1, input.p2, white, task.lineWidth, task.lineType)

        output = computePerp(input)
        Circle(dst2, input.ptCenter, task.DotSize + 2, Scalar.Red, -1, task.lineType)
        Line(dst2, output.p1, output.p2, yellow, task.lineWidth, task.lineType)

        If standaloneTest() Then SetTrueText("The line displayed at left Is the gravity vector.", 3)
    End Sub
End Class






Public Class XR_Line_Parallel : Inherits TaskParent
    Public classes() As List(Of Integer) ' groups of lines that are parallel
    Public unParallel As New List(Of Integer) ' lines which are not parallel
    Public Sub New()
        labels(2) = "Text shows the parallel Class With 0 being unparallel."
        desc = "Identify lines that are parallel (Or nearly so), perpendicular, And Not parallel."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        Dim parallels As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
        For Each lp In task.lines.lpList
            parallels.Add(lp.angle, lp.index)
        Next

        If parallels.Count <= 1 Then Exit Sub ' no lines...

        ReDim classes(task.lines.lpList.Count - 1)
        Dim index As Integer, j As Integer
        unParallel.Clear()
        For i = 0 To parallels.Count - 1
            Dim lp1 = task.lines.lpList(parallels.ElementAt(i).Value - 1)
            For j = i + 1 To parallels.Count - 1
                Dim lp2 = task.lines.lpList(parallels.ElementAt(j).Value - 1)
                If Math.Abs(lp1.angle - lp2.angle) < AngleThreshold Then
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
                Dim lp = task.lines.lpList(classes(i).ElementAt(j) - 1)
                Line(dst2, lp.p1, lp.p2, lp.color, task.lineWidth * 2, task.lineType)
                SetTrueText(CStr(colorIndex), lp.ptCenter)
            Next
            colorIndex += 1
        Next

        For Each index In unParallel
            Dim lp = task.lines.lpList(index - 1)
            Line(dst2, lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
            SetTrueText("0", lp.ptCenter)
        Next

        dst3 = task.lines.dst2
        labels(3) = task.lines.labels(2)
    End Sub
End Class






' https://stackoverflow.com/questions/7446126/opencv-2d-line-intersection-helper-function
Public Class Line_Intersection : Inherits TaskParent
    Public lp1 As lpData, lp2 As lpData
    Public intersectionPoint As Point2f
    Public Sub New()
        desc = "Determine If 2 lines intersect, where the cv.Point Is, And If that cv.Point Is In the image."
    End Sub
    Public Shared Function IntersectTest(p1 As Point2f, p2 As Point2f, p3 As Point2f, p4 As Point2f) As Point2f
        Dim x = p3 - p1
        Dim d1 = p2 - p1
        Dim d2 = p4 - p3
        Dim cross = d1.X * d2.Y - d1.Y * d2.X
        If Math.Abs(cross) < 0.000001 Then Return New Point2f
        Dim t1 = (x.X * d2.Y - x.Y * d2.X) / cross
        Dim pt = p1 + d1 * t1
        Return pt
    End Function
    Public Shared Function IntersectTest(lp1 As lpData, lp2 As lpData) As Point2f
        Dim x = lp2.p1 - lp1.p1
        Dim d1 = lp1.p2 - lp1.p1
        Dim d2 = lp2.p2 - lp2.p1
        Dim cross = d1.X * d2.Y - d1.Y * d2.X
        If Math.Abs(cross) < 0.000001 Then Return New Point2f
        Dim t1 = (x.X * d2.Y - x.Y * d2.X) / cross
        Dim pt = lp1.p1 + d1 * t1
        Return pt
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            If task.heartBeat Then
                lp1 = New lpData(New Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)),
                                     New Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)))
                lp2 = New lpData(New Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)),
                                     New Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)))
            End If
        End If

        intersectionPoint = Line_Intersection.IntersectTest(lp1, lp2)

        If standaloneTest() Then
            dst2.SetTo(0)
            Line(dst2, lp1.p1, lp1.p2, Scalar.Yellow, task.lineWidth, task.lineType)
            Line(dst2, lp2.p1, lp2.p2, Scalar.Yellow, task.lineWidth, task.lineType)
            If intersectionPoint <> New Point2f Then
                Circle(dst2, intersectionPoint, task.DotSize + 4, white, -1, task.lineType)
                labels(2) = "Intersection cv.Point = " + CStr(CInt(intersectionPoint.X)) + " x " + CStr(CInt(intersectionPoint.Y))
            Else
                labels(2) = "Parallel!!!"
            End If
            If intersectionPoint.X < 0 Or intersectionPoint.X > dst2.Width Or intersectionPoint.Y < 0 Or intersectionPoint.Y > dst2.Height Then
                labels(2) += " (off screen)"
            End If
        End If
    End Sub
End Class





Public Class XR_Line_Select : Inherits TaskParent
    Public delaunay As New Delaunay_LineSelect
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Select a line With mouse movement And put the selection into task.lpD."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static lpList As New List(Of lpData)
        If task.heartBeatLT Then
            delaunay.Run(src)
            lpList = New List(Of lpData)(task.lines.lpList)
            labels(2) = delaunay.labels(2)
            dst2 = delaunay.dst2
        End If
        strOut = task.lpD.lpDisplay(dst3)
        SetTrueText(strOut, 1) ' the line info is already prepped in strout in delaunay.
    End Sub
End Class







Public Class XR_Line_DepthHistogram : Inherits TaskParent
    Dim lineVert As New XR_Line_Vertical
    Dim plot As New PlotMouse_Basics
    Public Sub New()
        plot.plotHist.createHistogram = True
        plot.plotHist.removeZeroEntry = True
        If standalone Then task.gOptions.DebugCheckBox.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Show the histogram Of the depth data For a line.  Use debug check box To study longest line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lineVert.Run(src)
        dst3 = lineVert.dst2
        For Each lp In lineVert.lpLeft
            Dim depth = task.pcSplit(2)(lp.rect)
            Dim depthMask As New Mat(lp.rect.Size, MatType.CV_8U, 0)
            Dim p1 = New Point2f(lp.p1.X - lp.rect.X, lp.p1.Y - lp.rect.Y)
            Dim p2 = New Point2f(lp.p2.X - lp.rect.BottomRight.X, lp.p2.Y - lp.rect.BottomRight.Y)
            Line(depthMask, p1, p2, 255, task.lineWidth, task.lineType)
            Dim mmDepth = GetMinMax(depth, depthMask)
            plot.plotHist.Run(depth)
            Dim hist = plot.plotHist.histArray.ToList
            Dim bestIndex = hist.IndexOf(hist.Max)
            Dim incr = (mmDepth.maxVal - mmDepth.minVal) / task.gOptions.HistBinBar.Value
            Dim depth1 = mmDepth.minVal + incr * bestIndex
            If task.gOptions.DebugCheckBox.Checked Then
                dst2 = plot.plotHist.dst2
                Rectangle(dst3, lp.rect, task.highlight, task.lineWidth)
                labels(3) = "The histogram at left indicates that the depth Is likely at " + depth1.ToString(fmt1) + "m" + vbCrLf
                labels(2) = plot.plotHist.labels(2)
                Exit For
            End If
        Next
        strOut = "To view any line, uncheck the debugCheckBox In the Global options." + vbCrLf
        strOut += "With debugCheckBox checked, only the longest line will be displayed." + vbCrLf
        strOut += "Hover With the mouse over the line whose depth will be plotted." + vbCrLf
        SetTrueText(strOut, 1)
    End Sub
End Class





Public Class XR_Line_LeftRightMotion : Inherits TaskParent
    Public linesRight As New Line_Basics
    Public Sub New()
        labels = {"", "", "Left image lines", "Right image lines"}
        desc = "Find the lines In the Left And Right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lines.dst2
        labels(2) = task.lines.labels(2)

        linesRight.Run(task.rightView)

        dst3 = linesRight.dst2
        labels(3) = linesRight.labels(2)
    End Sub
End Class





Public Class XR_Line_Vertical : Inherits TaskParent
    Dim lrLines As New XR_Line_LeftRightMotion
    Public lpLeft As New List(Of lpData)
    Public lpRight As New List(Of lpData)
    Public Sub New()
        desc = "Find just the vertical lines In the left And right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lrLines.Run(src)
        CvtColor(task.leftView, dst2, ColorConversionCodes.GRAY2BGR)
        lpLeft.Clear()
        For Each lp In task.lines.lpList
            If Math.Abs(lp.angle) > 87 Then
                lpLeft.Add(lp)
                Line(dst2, lp.p1, lp.p2, lp.color, task.lineWidth, task.lineType)
            End If
        Next

        CvtColor(task.rightView, dst3, ColorConversionCodes.GRAY2BGR)
        lpRight.Clear()
        For Each lp In lrLines.linesRight.lpList
            If Math.Abs(lp.angle) > 87 Then
                lpRight.Add(lp)
                SetTrueText(CStr(lp.age), New cv.Point(lp.ptCenter.X + 2, lp.ptCenter.Y + 2), 3)
                Line(dst3, lp.p1, lp.p2, lp.color, task.lineWidth, task.lineType)
            End If
        Next
    End Sub
End Class






''' <summary>Holds a line with a stable track ID and color for multi-frame tracking.</summary>
Friend Class TrackedLine
    Public trackId As Integer
    Public lp As lpData
    Public missedCount As Integer
End Class





''' <summary>Find all lines in the image, assign each an ID, and track them as the camera moves.</summary>
Public Class XR_Line_LeftTrack : Inherits TaskParent
    ''' <summary>Tracked lines: (trackId, lpData, color, missedCount).</summary>
    Dim tracked As New List(Of TrackedLine)
    Dim nextTrackId As Integer = 1
    Const maxMissed As Integer = 5
    Const maxTracked As Integer = 200
    Const angleThresh As Single = 8.0F
    Const distThresh As Single = 120.0F
    Const lenRatioThresh As Single = 0.45F

    Public lpList As New List(Of lpData)
    Dim lines As New XR_Line_BasicsOld
    Dim options As New Options_LeftRightCorrelation
    Dim motionLeft As New Motion_Basics_TA
    Public Sub New()
        If standalone Then task.gOptions.displayDst0.Checked = True
        labels = {"", "", "Left image: detected lines with stable track IDs", ""}
        desc = "Cursor.ai: Find all lines in the left image, identify each and track them."
    End Sub
    Private Function matchScore(r As lpData, t As TrackedLine) As Single
        Dim ad = Math.Abs(r.angle - t.lp.angle)
        If ad > 90 Then ad = 180 - ad
        If ad > angleThresh Then Return Single.MaxValue
        Dim dist = r.ptCenter.DistanceTo(t.lp.ptCenter)
        If dist > distThresh Then Return Single.MaxValue
        Dim mx = Math.Max(r.length, t.lp.length) + 1.0F
        Dim lr = Math.Abs(r.length - t.lp.length) / mx
        If lr > lenRatioThresh Then Return Single.MaxValue
        Return ad * 2.0F + dist / 20.0F + lr * 20.0F
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst0 = task.leftView
        Dim raw = task.lines.lpList

        Dim usedRaw As New HashSet(Of lpData)
        Dim usedTracked As New HashSet(Of TrackedLine)

        ' Greedy assignment: longest raw lines first to reduce conflicts
        Dim rawByLen = raw.OrderByDescending(Function(x) x.length).ToList()
        For Each r In rawByLen
            Dim bestT As TrackedLine = Nothing
            Dim bestSc As Single = Single.MaxValue
            For Each t In tracked
                If usedTracked.Contains(t) Then Continue For
                Dim sc = matchScore(r, t)
                If sc < bestSc Then bestSc = sc : bestT = t
            Next
            If bestT IsNot Nothing Then
                bestT.lp = r
                bestT.missedCount = 0
                usedRaw.Add(r)
                usedTracked.Add(bestT)
            End If
        Next

        ' Increment missed; remove if over threshold
        For Each t In tracked
            If usedTracked.Contains(t) Then Continue For
            t.missedCount += 1
        Next
        tracked.RemoveAll(Function(t) t.missedCount > maxMissed)

        ' Add new tracks for unmatched raw
        For Each r In raw
            If usedRaw.Contains(r) Then Continue For
            If tracked.Count >= maxTracked Then Exit For
            Dim t As New TrackedLine With {
                            .trackId = nextTrackId,
                            .lp = r,
                            .missedCount = 0
                        }
            r.color = t.lp.color
            r.index = t.trackId
            nextTrackId += 1
            tracked.Add(t)
        Next

        ' Build lpList and draw
        lpList.Clear()
        dst3.SetTo(0)
        For Each t In tracked
            t.lp.index = t.trackId
            If lpList.Count < 10 Then
                Line(dst2, t.lp.p1, t.lp.p2, t.lp.color, options.lineTrackerWidth, LineTypes.Link8)
            End If
            lpList.Add(t.lp)
        Next

        dst2 = task.leftView.Clone
        If dst2.Channels = 1 Then CvtColor(dst2, dst2, ColorConversionCodes.GRAY2BGR)
        dst1 = New Mat(dst2.Size, MatType.CV_8U, 0)

        For Each t In tracked
            Line(dst2, t.lp.p1, t.lp.p2, t.lp.color, task.lineWidth, task.lineType)
            Line(dst1, t.lp.p1, t.lp.p2, t.trackId Mod 255 + 1, 1, LineTypes.Link4)
            SetTrueText(CStr(t.trackId), New cv.Point(CInt(t.lp.ptCenter.X), CInt(t.lp.ptCenter.Y)), 2)
        Next

        labels(2) = "Tracked " + CStr(tracked.Count) + " lines, " + CStr(raw.Count) + " detected this frame"
    End Sub
End Class






Public Class XR_Line_Tracker : Inherits TaskParent
    Dim options As New Options_LeftRightCorrelation
    Dim lpList As New List(Of lpData)
    Public Sub New()
        dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
        desc = "Track lines in the left image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        labels(2) = task.lines.labels(2)

        dst2.SetTo(0)
        lpList.Clear()
        For Each lp In task.lines.lpList
            Line(dst2, lp.p1, lp.p2, lp.index + 1, options.lineTrackerWidth, LineTypes.Link8)
            lpList.Add(lp)
            If lpList.Count > 10 Then Exit For
        Next

        dst3 = Palettize(dst2, 0)
    End Sub
End Class






Public Class XR_Line_BrickList : Inherits TaskParent
    Public lp As lpData ' set this input
    Public lpOutput As lpData ' this is the result lp
    Public sobel As New Edge_Sobel
    Public ptList As New List(Of cv.Point)
    Dim options As New Options_LeftRightCorrelation
    Public Sub New()
        labels(3) = "Find the line's bricks containing the line."
        dst3 = New Mat(dst0.Size, MatType.CV_8U, 0)
        dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
        desc = "Add a bricklist to the requested lp"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If task.lines.lpList.Count = 0 Then Exit Sub

        If standalone Then
            lp = task.lines.lpList(0)
            If lp.length = 0 Then Exit Sub
        End If

        dst3.SetTo(0)
        Line(dst3, lp.p1, lp.p2, lp.index + 1, options.lineTrackerWidth, LineTypes.Link8)

        Dim r = lp.rect
        dst1.SetTo(0)
        sobel.Run(task.gray)
        sobel.dst2(r).CopyTo(dst1(r), dst3(r))
        DrawRect(dst1, r, black)

        Dim allPoints As New List(Of cv.Point)
        Dim brickList As New List(of cv.Rect)
        For Each rect In task.gridRects
            Dim brick = dst1(rect)
            If CountNonZero(brick) = 0 Then Continue For
            Dim mm = GetMinMax(brick)
            Dim pt = New cv.Point(mm.maxLoc.X + rect.X, mm.maxLoc.Y + rect.Y)
            allPoints.Add(pt)
            brickList.Add(rect)
        Next

        ptList.Clear()
        Dim angles As New List(Of Single)
        Dim epList As New List(Of Tuple(Of Single, Single, Single, Single))
        For i = 0 To allPoints.Count - 1
            Dim pt = allPoints(i)
            For j = i + 1 To allPoints.Count - 1
                Dim lpTest = New lpData(pt, allPoints(j))
                'If Math.Abs(lp.angle - lpTest.angle) < AngleThreshold Then
                angles.Add(lpTest.angle)
                ptList.Add(pt)
                ptList.Add(allPoints(j))
                epList.Add(New Tuple(Of Single, Single, Single, Single)(lpTest.ptE1.X,
                           lpTest.ptE1.Y, lpTest.ptE2.X, lpTest.ptE2.Y))
                'End If
            Next
        Next

        If ptList.Count < 2 Then
            SetTrueText("No edges were found in the area.", 3)
            lp = Nothing
            Exit Sub
        End If
        dst2 = src
        For Each pt In ptList
            Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
        Next

        Dim x1 = epList.Average(Function(x) x.Item1)
        Dim y1 = epList.Average(Function(x) x.Item2)
        Dim x2 = epList.Average(Function(x) x.Item3)
        Dim y2 = epList.Average(Function(x) x.Item4)
        lpOutput = New lpData(New Point2f(x1, y1), New Point2f(x2, y2))
        Line(dst2, lpOutput.p1, lpOutput.p2, task.highlight, task.lineWidth, task.lineType)

        If standalone Then lp = lpOutput

        For Each r In brickList
            DrawRect(dst3, r, white)
        Next
    End Sub
End Class



Public Class XR_Line_BrickListTest : Inherits TaskParent
    Dim brickLines As New XR_Line_BrickList
    Public Sub New()
        desc = "Find the brick list for each line in the lines.lplist"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        brickLines.lp = task.lines.lpList(0)
        brickLines.Run(task.gray)
        'For Each r In brickLines.brickList
        '    DrawRect(dst3, r, white)
        'Next
    End Sub
End Class





Public Class XR_Line_MapRects : Inherits TaskParent
    Public lpList As New List(Of lpData) ' the list of non-overlapping lines.
    Public pointCloud As New Mat
    Dim depthToWorld As New Cloud_DepthToWorld
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(1) = "Move mouse over any image to see line."
        labels(3) = "Each rectangle is divided into 2 regions defined by the line."
        dst0 = New Mat(dst0.Size, MatType.CV_8U, 0)
        dst3 = New Mat(dst3.Size, MatType.CV_8U, 0)
        desc = "Create a map with the lp.rect field."
    End Sub
    Private Function fillTriangle(lp As lpData, p1 As cv.Point) As Boolean
        Dim val = dst3.Get(Of Byte)(p1.Y, p1.X)
        If val > 0 Then
            FloodFill(dst3, p1, 255)
            Return True
        End If
        Return False
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lines.lpList.Count = 0 Then Exit Sub ' nothing to work on.

        Dim mmList As New List(Of mmData)
        Dim pad = 5
        dst3.SetTo(0)
        dst0.SetTo(0)
        lpList.Clear()
        For Each lp In task.lines.lpList
            Dim val = dst3.Get(Of Byte)(lp.ptCenter.Y, lp.ptCenter.X)
            If val = 0 Then
                Rectangle(dst0, lp.rect, Scalar.All(lp.index + 1), -1)
                Rectangle(dst3, lp.rect, Scalar.All(lp.index + 1), -1)
                Line(dst3, lp.p1, lp.p2, 0, task.lineWidth, LineTypes.Link8)
                lpList.Add(lp)
            End If
        Next
        labels(2) = CStr(lpList.Count) + " non-overlapping lines were found."

        For Each lp In task.lines.lpList
            If fillTriangle(lp, lp.rect.TopLeft) Then Continue For
            If fillTriangle(lp, lp.rect.BottomRight) Then Continue For

            Dim topRight As New cv.Point(lp.rect.X + lp.rect.Width, lp.rect.Top)
            If fillTriangle(lp, topRight) Then Continue For

            Dim botleft As New cv.Point(lp.rect.X, lp.rect.Top + lp.rect.Height)
            If fillTriangle(lp, botleft) Then Continue For
        Next

        dst2 = Palettize(dst3, 0)
        Dim pcZ = task.pcSplit(2).Clone
        For Each lp In task.lines.lpList
            Dim mask1 = dst3(lp.rect).Clone
            InRange(mask1, 255, 255, mask1)
            Dim mask2 = Not mask1

            Dim depth1 = Mean(pcZ(lp.rect), mask1)(0)
            Dim depth2 = Mean(pcZ(lp.rect), mask2)(0)

            ' if the depth change at the line is less than 5 cm's, ignore it.
            If Math.Abs(depth1 - depth2) > 0.05 Then
                depth2 = depth1
                pcZ(lp.rect).SetTo(depth1, mask1)
                pcZ(lp.rect).SetTo(depth2, mask2)

                depthToWorld.Run(pcZ(lp.rect))
                depthToWorld.dst2.CopyTo(pcZ(lp.rect))
            End If
        Next

        Merge({task.pcSplit(0), task.pcSplit(1), pcZ}, pointCloud)

        Dim index = dst0.Get(Of Byte)(task.mouseMovePoint.Y, task.mouseMovePoint.X) - 1
        If index >= 0 And index < task.lines.lpList.Count Then
            task.lpD = task.lines.lpList(index)
        Else
            If task.lpD Is Nothing Then task.lpD = task.lines.lpList(0)
        End If
        SetTrueText(task.lpD.lpDisplay(dst1), 1)
    End Sub
End Class





Public Class XR_Line_Map : Inherits TaskParent
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(1) = "Move mouse over any image to see line."
        labels(3) = "Each rectangle is divided into 2 regions defined by the line."
        dst3 = New Mat(dst3.Size, MatType.CV_8U, 0)
        desc = "Create a map with the lp.rect field."
    End Sub
    Private Function fillTriangle(lp As lpData, p1 As cv.Point) As Boolean
        Dim val = dst3.Get(Of Byte)(p1.Y, p1.X)
        If val > 0 Then
            FloodFill(dst3, p1, 255)
            Return True
        End If
        Return False
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim mmList As New List(Of mmData)
        Dim pad = 5
        dst3.SetTo(0)
        For Each lp In task.lines.lpList
            Line(dst3, lp.p1, lp.p2, lp.index + 1, task.lineWidth * 3, LineTypes.Link8)
        Next
        labels(2) = CStr(task.lines.lpList.Count) + " non-overlapping lines were found."

        dst2 = Palettize(dst3, 0)

        Dim index = dst3.Get(Of Byte)(task.mouseMovePoint.Y, task.mouseMovePoint.X) - 1
        If task.lines.lpList.Count > 0 And index < task.lines.lpList.Count Then
            If index <= 0 Then
                If task.lpD Is Nothing Then task.lpD = task.lines.lpList(0)
            Else
                task.lpD = task.lines.lpList(index)
            End If
            task.lpD.lpDisplay(dst1)
        End If
    End Sub
End Class






Public Class XR_Line_BasicsOldNoMotion : Inherits TaskParent
    Dim lines As New XR_Line_BasicsOld
    Public Sub New()
        desc = "Ignore motion when finding the lines."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.gray.Clone

        lines.motionMask.SetTo(255) ' every pixel has motion...
        lines.Run(src)
        dst2 = lines.dst2
        labels(2) = lines.labels(2)
    End Sub
End Class





Public Class XR_Line_TranslatedRightView : Inherits TaskParent
    Dim lines As New Line_Basics
    Public lpListRight As New List(Of lpData)
    Public Sub New()
        desc = "Translate lines from the color (left for ZED) image to the right image.."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.gray.Clone
        dst2 = task.lines.dst2
        labels(2) = task.lines.labels(2)

        CvtColor(task.rightView, dst3, ColorConversionCodes.GRAY2BGR)

        lines.Run(src) ' we could use to validate the lines that are translated from the left view.

        lpListRight.Clear()
        Dim pt1 As cv.Point, pt2 As cv.Point
        For Each lp In task.lines.lpList
            Dim depth1 = task.pcSplit(2).Get(Of Single)(lp.p1.Y, lp.p1.X)
            If depth1 = 0 Then
                Dim p1GridIndex = task.gridMap.Get(Of Integer)(lp.p1.Y, lp.p1.X)
                Dim r = task.gridRects(p1GridIndex)
                depth1 = Mean(task.pcSplit(2)(r), task.depthmask(r))
            End If
            Dim depth2 = task.pcSplit(2).Get(Of Single)(lp.p2.Y, lp.p2.X)
            If depth2 = 0 Then
                Dim p2GridIndex = task.gridMap.Get(Of Integer)(lp.p2.Y, lp.p2.X)
                Dim r = task.gridRects(p2GridIndex)
                depth2 = Mean(task.pcSplit(2)(r), task.depthmask(r))
            End If
            If depth1 = 0 Or depth2 = 0 Then Continue For

            pt1 = lp.p1
            pt1.X -= task.calibData.baseline * task.calibData.leftIntrinsics.fx / depth1
            If pt1.X < 0 Or pt1.X >= dst2.Width Then Continue For

            pt2 = lp.p2
            pt2.X -= task.calibData.baseline * task.calibData.leftIntrinsics.fx / depth2
            If pt2.X < 0 Or pt2.X >= dst2.Width Then Continue For

            Dim lpR As New lpData(pt1, pt2)
            Line(dst3, lpR.p1, lpR.p2, lp.color, task.lineWidth + 1, task.lineType)
            lpListRight.Add(lpR)
        Next
        labels(3) = CStr(lpListRight.Count) + " lines were translated from the left image to the right image."
    End Sub
End Class





Public Class XR_Line_EdgeLineCompare : Inherits TaskParent
    Dim edgeLine As New EdgeLine_Basics
    Public Sub New()
        dst3 = New Mat(dst2.Size, MatType.CV_8U, 0)
        labels(3) = "Lines where EdgeLine_Basics and Line_Basics agree."
        desc = "Compare the output of EdgeLine_Basics and Line_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edgeLine.Run(src)
        dst2 = edgeLine.dst2
        labels(2) = edgeLine.labels(2)

        dst3 = task.lines.dst3
    End Sub
End Class





Public Class XR_Line_Longest : Inherits TaskParent
    Public Sub New()
        desc = "Compare the longest lines of the current and previous image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static lpLast As lpData = task.lines.lpList(0)
        dst2 = task.color.Clone

        Dim lp = task.lines.lpList(0)
        Line(dst2, lp.ptE1, lp.ptE2, task.highlight, task.lineWidth + 3)

        Dim distance1 As Single, distance2 As Single
        If lp.ptE1.Y = 0 Then
            distance1 = lp.ptE1.X - lpLast.ptE1.X
            distance2 = lp.ptE2.X - lpLast.ptE2.X
        End If
        If lp.ptE1.Y = 0 Then
            distance1 = lp.ptE1.X - lpLast.ptE1.X
            distance2 = lp.ptE2.X - lpLast.ptE2.X
        End If
        ' Debug.WriteLine("distance1 = " + distance1.ToString(fmt2) + " distance2 = " + distance2.ToString(fmt2))
        lpLast = task.lines.lpList(0)
    End Sub
End Class






Public Class XR_Line_EdgeLine : Inherits TaskParent
    Dim edgeLine As New EdgeLine_Basics
    Dim lines As New Line_Basics
    Public Sub New()
        dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
        desc = "Search for lines in the EdgeLine output."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edgeLine.Run(task.gray)

        Static matList As New List(Of Mat)
        Dim _thrEdge As New Mat
        Threshold(edgeLine.dst2, _thrEdge, 0, 255, ThresholdTypes.Binary)
        matList.Add(_thrEdge)
        If matList.Count > 5 Then matList.RemoveAt(0)

        dst2.SetTo(0)
        For Each mat In matList
            dst2 = dst2 Or mat
        Next

        lines.Run(dst2)
        dst3 = lines.dst2
        labels(3) = lines.labels(2)
    End Sub
End Class






Public Class XR_Line_FindSimple : Inherits TaskParent
    Dim edges As New Edge_Basics_TA
    Dim side As Integer
    Dim pixels(side * side) As cv.Point
    Public Sub New()
        side = task.gOptions.GridSlider.Value
        ReDim pixels(side * side)
        desc = "Find lines within each brick."
    End Sub
    Public Shared Function testPixels(pixels() As cv.Point) As lpData
        Dim testX As Boolean = True
        Dim testY As Boolean = True

        For j = 1 To pixels.Count - 1
            If Math.Abs(pixels(j - 1).X - pixels(j).X) > 1 Then
                testX = False
                Exit For
            End If
        Next

        For j = 1 To pixels.Count - 1
            If Math.Abs(pixels(j - 1).Y - pixels(j).Y) > 1 Then
                testX = False
                Exit For
            End If
        Next
        If testX Or testY Then Return New lpData(pixels(0), pixels(pixels.Count - 1))
        Return Nothing
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.edges.dst2
        labels(2) = task.edges.labels(2)

        dst3.SetTo(0)
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            Dim pixelCount = CountNonZero(dst2(r))
            If pixelCount = 0 Or pixelCount > 20 Then Continue For
            Dim pixelMat As New Mat
            FindNonZero(dst2(r), pixelMat)

            pixelMat.GetArray(Of cv.Point)(pixels)
            Dim lp = testPixels(pixels)
            If lp IsNot Nothing Then
                Line(dst3(r), lp.p1, lp.p2, task.highlight, task.lineWidth)
            End If
        Next
        Threshold(dst2, dst2, 0, 255, ThresholdTypes.Binary)
        Threshold(dst3, dst3, 0, 255, ThresholdTypes.Binary)
    End Sub
End Class





Public Class XR_Line_RedFlood : Inherits TaskParent
    Dim flood As New Flood_BasicsMask
    Public Sub New()
        flood.showSelected = False
        desc = "Use the edges as input to flood."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = task.edges.dst2
        labels(3) = task.edges.labels(2)

        flood.inputRemoved = Not dst3
        flood.Run(dst3)
        dst2 = flood.dst2
        labels(2) = flood.labels(2)
    End Sub
End Class






Public Class XR_Line_Brick : Inherits TaskParent
    Dim lpList As New List(Of lpData)
    Public Sub New()
        desc = "Find the bricks that clearly have lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src

        If lpList.Count < 2 Then
            For i = 0 To Math.Min(10, task.lines.lpList.Count) - 1
                lpList.Add(task.lines.lpList(i))
            Next
        End If

        For Each lp In lpList
            Line(dst2, lp.p1, lp.p2, task.highlight, task.lineWidth + 2)
        Next
    End Sub
End Class






Public Class XR_Line_Finder : Inherits TaskParent
    Dim side As Integer
    Dim pixels(side * side) As cv.Point
    Public Sub New()
        side = task.gOptions.GridSlider.Value
        ReDim pixels(side * side)
        desc = "Find lines within each brick."
    End Sub
    Public Function findLines(pixels() As cv.Point) As List(Of lpData)
        Dim lpList As New List(Of lpData)
        Dim ptList As New List(Of cv.Point)
        For i = 1 To pixels.Count - 1
            If pixels(i - 1).X > 0 Or pixels(i).X > 0 Then
                If ptList.Count = 0 Then
                    ptList.Add(pixels(i - 1))
                Else
                    If Math.Abs(pixels(i).X - ptList.Last.X) <= 1 Then
                        ptList.Add(pixels(i))
                    ElseIf Math.Abs(pixels(i - 1).X - ptList.Last.X) <= 1 Then
                        ptList.Add(pixels(i - 1))
                    Else
                        lpList.Add(New lpData(ptList(0), ptList.Last))
                        ptList.Clear()
                    End If
                End If
            End If
        Next
        If ptList.Count Then lpList.Add(New lpData(ptList.First, ptList.Last))

        Return lpList
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.edges.dst2
        labels(2) = task.edges.labels(2)

        dst3.SetTo(0)
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)

            Dim pixelCount = CountNonZero(dst2(r))
            If pixelCount = 0 Or pixelCount > 20 Then Continue For
            If pixelCount < side Then Continue For

            Dim pixelMat As New Mat
            FindNonZero(dst2(r), pixelMat)

            pixelMat.GetArray(Of cv.Point)(pixels)

            ' use this line for debugging...
            If task.drawRect.Contains(r.TopLeft) Then Dim k = 0

            Dim lpList = findLines(pixels)
            For Each lp In lpList
                Line(dst3(r), lp.p1, lp.p2, task.highlight, task.lineWidth)
            Next
        Next
    End Sub
End Class







Public Class XR_Line_TrackV : Inherits TaskParent
    Public lastV As New List(Of lpData)
    Public matchList As New List(Of lpData)
    Dim knn As New KNN_Minimal
    Dim match As New Match_Basics
    Dim lastImage As Mat
    Dim verticalLast As New List(Of lpData)
    Public trainInput As New List(Of Vec4f)
    Public queries As New List(Of Vec4f)
    Public Sub New()
        labels(3) = "The vertical lines found in the previous heartbeat image."
        desc = "Track the vertical lines on the heartbeat."
    End Sub
    Private Function getVerticals(lpList As List(Of lpData)) As List(Of lpData)
        Dim verticals As New List(Of lpData)
        For Each lp In lpList
            If lp.ptE1.Y <> 0 And lp.ptE2.Y <> 0 Then Continue For
            If lp.ptE1.Y <> 0 Then lp = New lpData(lp.p2, lp.p1)
            lp.index = verticals.Count + 1
            verticals.Add(lp)
        Next
        Return verticals
    End Function

    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.firstPass Then
            lastImage = src.Clone
            verticalLast = getVerticals(task.lines.lpList)
        End If
        If task.heartBeatLT Then
            dst3.SetTo(0)
            For Each lp In verticalLast
                Line(dst3, lp.ptE1, lp.ptE2, lp.color, task.lineWidth, LineTypes.Link4)
            Next

            trainInput.Clear()
            For Each lp In verticalLast
                trainInput.Add(New Vec4f(lp.ptE1.X, lp.ptE1.Y, lp.ptE2.X, lp.ptE2.Y))
            Next

            Dim verticalsCurr = getVerticals(task.lines.lpList)
            queries.Clear()
            For Each lp In verticalsCurr
                queries.Add(New Vec4f(lp.ptE1.X, lp.ptE1.Y, lp.ptE2.X, lp.ptE2.Y))
            Next

            Dim dimension = 4
            knn.queryMat = Mat.FromPixelData(queries.Count, dimension, MatType.CV_32F, queries.ToArray)
            knn.trainMat = Mat.FromPixelData(trainInput.Count, dimension, MatType.CV_32F, trainInput.ToArray)
            knn.Run(emptyMat)

            matchList.Clear()
            dst2.SetTo(0)
            Dim correlationCount As Integer
            Dim angleCount As Integer
            Dim intersectCount As Integer
            For i = 0 To verticalsCurr.Count - 1
                Dim lp = verticalsCurr(i)
                Dim vec = trainInput(knn.result(i, 0))
                Dim lpPrev = New lpData(New Point2f(vec(0), vec(1)), New Point2f(vec(2), vec(3)))

                If lp.rect.IntersectsWith(lpPrev.rect) Then
                    match.template = lastImage(lpPrev.rect)
                    match.Run(src(lp.rect))
                    If match.correlation > task.fCorrThreshold Then
                        Line(dst2, lp.ptE1, lp.ptE2, task.scalarColors(i), task.lineWidth, LineTypes.Link4)
                        Line(dst2, lpPrev.ptE1, lpPrev.ptE2, task.scalarColors(i), task.lineWidth, LineTypes.Link4)

                        matchList.Add(lp)
                        matchList.Add(lpPrev)
                    Else
                        correlationCount += 1
                        Continue For
                    End If
                Else
                    intersectCount += 1
                    Continue For
                End If
                Exit For
            Next

            labels(2) = CStr(matchList.Count / 2) + " matches found" + ".  Match failures: Correlation = " +
                        CStr(correlationCount) + " Angle = " + CStr(angleCount) + " intersection = " +
                        CStr(intersectCount)
            lastImage = src.Clone
            verticalLast = New List(Of lpData)(verticalsCurr)
        End If
    End Sub
End Class




Public Class XR_Line_BasicsOldEmboss : Inherits TaskParent
    Implements IDisposable
    Public lpList As New List(Of lpData)
    Dim ld As FastLineDetector
    Dim emboss As New PhotoShop_Emboss
    Public Sub New()
        dst1 = New Mat(dst3.Size, MatType.CV_8U, 0)
        dst3 = New Mat(dst3.Size, MatType.CV_8U, 0)
        ld = FastLineDetector.Create
        desc = "Run FLD with emboss input."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Or src.Type <> MatType.CV_8U Then src = task.gray.Clone

        emboss.Run(src)
        dst2 = emboss.dst3

        lpList = Line_Core.getRawSortedLines(ld.Detect(dst2))

        dst1.SetTo(0)
        For Each lp In lpList
            lp.index = lpList.Count
            Line(dst1, lp.p1, lp.p2, lp.index + 1, task.lineWidth, LineTypes.Link4)
        Next

        Threshold(dst1, dst3, 0, 255, ThresholdTypes.Binary)

        labels(2) = CStr(lpList.Count) + " lines found"
    End Sub
    Protected Overrides Sub Finalize()
        ld.Dispose()
    End Sub
End Class



Public Class XR_Line_Sobel : Inherits TaskParent
    Dim edges As New Edge_Sobel
    Dim lines As New Line_Basics
    Public Sub New()
        desc = "Find lines in the Sobel output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(task.gray)
        dst2 = edges.dst2

        lines.Run(dst2)

        dst3.SetTo(0)
        For Each lp In lines.lpList
            Line(dst3, lp.p1, lp.p2, task.highlight, task.lineWidth)
        Next
    End Sub
End Class




Public Class XR_Line_LongestTest : Inherits TaskParent
    Dim lpLast As New lpData
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Check to see that the longest line is always present."
    End Sub
    Public Shared Function compareLines(lpCurr As lpData, lpLast As lpData) As Boolean
        Dim distThreshold = task.gridWH
        If (lpCurr.ptE1.DistanceTo(lpLast.ptE1) < distThreshold And
           lpCurr.ptE2.DistanceTo(lpLast.ptE2) < distThreshold) Or
           (lpCurr.ptE2.DistanceTo(lpLast.ptE1) < distThreshold And
           lpCurr.ptE1.DistanceTo(lpLast.ptE2) < distThreshold) Then
            Return True
        End If
        Return False
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static presentCount As Integer
        Static lostLongest As Integer
        If task.lines.lpList.Count = 0 Then
            dst2.SetTo(0)
        Else
            Dim lpCurr = task.lines.lpList(0)
            dst1 = task.color.Clone
            Line(dst1, lpCurr.ptE1, lpCurr.ptE2, task.highlight, task.lineWidth)
            If compareLines(lpCurr, lpLast) Then
                Line(dst2, lpCurr.ptE1, lpCurr.ptE2, task.highlight, task.lineWidth)
                presentCount += 1
                If presentCount > 1000 Then presentCount = 100
            Else
                dst2.SetTo(0)
                lostLongest = 15
                presentCount = 0
            End If
            lpLast = lpCurr
        End If

        If lostLongest > 0 Then
            SetTrueText("The longest line was lost! ", 2)
            lostLongest -= 1
        Else
            labels(2) = "The longest line has been present " + CStr(presentCount) + " times."
        End If

        SetTrueText("If the camera is moved, the longest line (task.lines.lpList(0) should produce a solid." + vbCrLf +
                    "If that line disappears or its center moves a log, dst2 is set to 0 and it starts over." + vbCrLf +
                    "It should not disappear unless the movement makes another line the lpList(0)", 3)
    End Sub
End Class







Public Class XO_Line_EdgePoints : Inherits TaskParent
    Dim knn As New KNN_Basics
    Public xMatches As New List(Of Single)
    Public yMatches As New List(Of Single)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(2) = "The accumulated image after WarpAffine."
        labels(3) = "The lines below had non-zero X or Y displacement"
        desc = "Use KNN to match edge points of the current and previous frames."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lines.lpList.Count = 0 Then Exit Sub
        Dim lpList As New List(Of lpData)(task.lines.lpList)
        knn.queries.Clear()
        For Each lp In lpList
            knn.queries.Add(lp.ptE1)
            knn.queries.Add(lp.ptE2)
        Next

        For Each lp In task.lines.lpList
            knn.queries.Add(lp.ptE1)
            knn.queries.Add(lp.ptE2)
            lpList.Add(lp)
        Next

        If task.firstPass Then knn.trainInput = New List(Of Point2f)(knn.queries)

        knn.Run(emptyMat)

        If task.heartBeat Then dst3 = src.Clone

        xMatches.Clear()
        yMatches.Clear()
        Dim vectors As New List(Of lpData)
        Dim outLiers As New List(Of lpData)
        Dim zeroVectors As New List(Of lpData)
        Dim xPerp As New List(Of lpData)
        Dim yPerp As New List(Of lpData)
        For i = 0 To knn.queries.Count - 1
            Dim p1 = knn.queries(i)
            Dim index = knn.result(i, 0)
            Dim p2 = knn.trainInput(index)
            Dim distance = p1.DistanceTo(p2)
            Dim lp = lpList(Math.Floor(i / 2))

            If distance > task.gridWH Then
                outLiers.Add(lp)
                Continue For
            End If

            If distance = 0 Then
                zeroVectors.Add(lp)
                Continue For
            End If

            If i Mod 2 = 0 Then
                If Math.Abs(lp.angle) > 85 Then xPerp.Add(lp)
                If Math.Abs(lp.angle) < 5 Then yPerp.Add(lp)
            End If

            If distance < 0.5 Then vectors.Add(lp)
            xMatches.Add(p1.X - p2.X)
            yMatches.Add(p1.Y - p2.Y)
        Next

        knn.trainInput = New List(Of Point2f)(knn.queries)

        If xMatches.Count > 0 Then
            strOut = "There were " + CStr(xMatches.Count) + " useful edge points after filtering." + vbCrLf
            strOut += "Average X offset = " + xMatches.Average.ToString(fmt2) + vbCrLf
            strOut += "Average Y offset = " + yMatches.Average.ToString(fmt2) + vbCrLf
            strOut += "There were " + CStr(lpList.Count) + " lines in the current image" + vbCrLf
            strOut += "There were " + CStr(lpList.Count * 2) + " edge points in the current image" + vbCrLf
            strOut += "There were " + CStr(vectors.Count)
            strOut += " with delta < 0.5 indicating direction of motion." + vbCrLf
            strOut += "There were " + CStr(outLiers.Count) + " outliers implying new or lost lines " + vbCrLf
            strOut += "There were " + CStr(zeroVectors.Count) + " implying identical lines" + vbCrLf

            ' if we have enough lines show the vectors that are closest.
            If xMatches.Count >= knn.queries.Count / 2 Then
                For Each lp In vectors
                    Line(dst3, lp.p1, lp.p2, task.highlight, task.lineWidth)
                Next
            End If
        End If
        SetTrueText(strOut, 1)
    End Sub
End Class






Public Class XR_Line_FinderPlus : Inherits TaskParent
    Dim find As New Line_Finder
    Dim lines As New Line_Basics
    Public Sub New()
        desc = "Find lines in the brickline_find output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        find.Run(task.gray)
        dst2 = find.dst3

        lines.Run(dst2)
        dst3 = lines.dst2
        labels(3) = lines.labels(2)
    End Sub
End Class





Public Class Line_Finder : Inherits TaskParent
    Public ptList() As cv.Point
    Dim side As Integer
    Dim pixels(side * side) As cv.Point
    Dim sortX As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalInteger)
    Public Sub New()
        dst0 = New Mat(dst0.Size, MatType.CV_8U, 0)
        side = task.gOptions.GridSlider.Value
        ReDim pixels(side * side)
        desc = "Find only the bricks containing what are clearly lines."
    End Sub
    Public Function findLines(pixels() As cv.Point) As lpData
        Dim ordered As Boolean = True
        Dim minX As Integer, maxX As Integer, minY = pixels(0).Y, maxY = pixels.Last.Y
        For i = 1 To pixels.Count - 1
            If Math.Abs(pixels(i).Y - pixels(i - 1).Y) > 1 Then
                ordered = False
                Exit For
            End If
        Next

        If ordered Then
            sortX.Clear()
            For Each pt In pixels
                sortX.Add(pt.X, pt)
            Next
            minX = sortX.Values(0).X
            maxX = sortX.Values.Last.X

            For i = 1 To sortX.Values.Count - 1
                If Math.Abs(sortX.Values(i).X - sortX.Values(i - 1).X) > 1 Then
                    ordered = False
                    Exit For
                End If
            Next
        End If

        If ordered = False Then Return Nothing
        Dim lp = New lpData(New cv.Point(minX, minY), New cv.Point(maxX, maxY))
        If Not pixels.Contains(lp.p1) Or Not pixels.Contains(lp.p2) Then Return Nothing
        Return lp
    End Function

    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        dst1 = task.edges.dst2
        labels(2) = task.edges.labels(2)

        Dim maxPixels = side * 1.5
        dst3.SetTo(0)
        dst0.SetTo(0)
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)

            Dim pixelCount = CountNonZero(dst1(r))
            If pixelCount = 0 Or pixelCount > maxPixels Then Continue For
            If pixelCount < side Then Continue For

            Dim pixelMat As New Mat
            FindNonZero(dst1(r), pixelMat)

            pixelMat.GetArray(Of cv.Point)(pixels)

            Dim lp = findLines(pixels)

            ' use this line for debugging...
            If task.drawRect.Contains(r.TopLeft) Then Dim k = 0

            If lp IsNot Nothing Then
                Line(dst2(r), lp.p1, lp.p2, task.highlight, task.lineWidth + 1)
                dst0(r).Set(Of Byte)(lp.p1.Y, lp.p1.X, 255)
                dst0(r).Set(Of Byte)(lp.p2.Y, lp.p2.X, 255)
            End If
        Next

        Dim pointMat As New Mat
        FindNonZero(dst0, pointMat)
        If pointMat.Rows > 0 Then
            ReDim ptList(pointMat.Rows)
            pointMat.GetArray(Of cv.Point)(ptList)
        End If
        dst3.SetTo(0)
        dst3.SetTo(task.highlight, dst0)
    End Sub
End Class





Public Class Line_FindClosest : Inherits TaskParent
    Public inputLine As lpData
    Public closestLine As lpData
    Public lpList As New List(Of lpData)
    Public LastList As New List(Of lpData)
    Public Sub New()
        labels(3) = "The lines found in the current image - task.lines.dst3"
        desc = "Find the line in provided lpList closest to the requested line"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then inputLine = task.longestLine

        If standaloneTest() Then
            dst3 = task.lines.dst3
            dst2 = task.color.Clone
            Line(dst2, inputLine.p1, inputLine.p2, white, task.lineWidth + 1)
        End If

        Dim candidates As New List(Of lpData)
        For Each lp In LastList
            If Math.Abs(lp.angle - inputLine.angle) < 2 Then candidates.Add(lp)
        Next

        labels(2) = "There were " + CStr(candidates.Count) + " with an angle within 2 degrees of the input line."
        closestLine = Nothing
        If candidates.Count = 0 Then Exit Sub ' no lines were found.

        Dim distances As New List(Of Single)
        For Each lp In candidates
            Dim distance = inputLine.ptE1.DistanceTo(lp.ptE1) + inputLine.ptE2.DistanceTo(lp.ptE2)
            If distance >= dst2.Height Then
                distance = inputLine.ptE1.DistanceTo(lp.ptE2) + inputLine.ptE2.DistanceTo(lp.ptE1)
            End If
            distances.Add(distance)
        Next

        closestLine = candidates(distances.IndexOf(distances.Min))
        Line(dst2, closestLine.ptE1, closestLine.ptE2, task.highlight, task.lineWidth + 2)
    End Sub
End Class





Public Class Line_RightOnly : Inherits TaskParent
    Public linesRight As New Line_Core
    Dim stableR As New StableGray_Right
    Public lpList As New List(Of lpData)
    Public Sub New()
        dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
        desc = "Find the lines in the right image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        stableR.Run(emptyMat)

        Dim lastList = New List(Of lpData)(linesRight.lpList)
        linesRight.Run(stableR.dst3)
        lpList = Line_Basics_TA.removeDuplicates(linesRight.lpList)
        Dim averageAge = Line_Basics_TA.updateAgesAndLongest(linesRight.lpList, lastList)

        dst2.SetTo(0)
        For Each lp In linesRight.lpList
            Line(dst2, lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
            SetTrueText(CStr(lp.age), New cv.Point(lp.ptCenter.X + 2, lp.ptCenter.Y + 2), 2)
        Next
        labels(2) = CStr(lpList.Count) + " lines in the right image with average age = " + averageAge.ToString(fmt1)
    End Sub
End Class




Public Class Line_LeftRightZED : Inherits TaskParent
    Public rightOnly As New Line_RightOnly
    Public Sub New()
        dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
        dst3 = New Mat(dst2.Size, MatType.CV_8U, 0)
        desc = "Find the lines in the left and right images.  Left image is already found for StereoLabs..."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2.SetTo(0)
        For Each lp In task.lines.lpList
            Line(dst2, lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
            SetTrueText(CStr(lp.age), New cv.Point(lp.ptCenter.X + 2, lp.ptCenter.Y + 2), 2)
        Next
        labels(2) = CStr(task.lines.lpList.Count) + " lines in the left image."

        rightOnly.Run(emptyMat)
        labels(3) = rightOnly.labels(2)

        dst3.SetTo(0)
        For Each lp In rightOnly.lpList
            Line(dst3, lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
            SetTrueText(CStr(lp.age), New cv.Point(lp.ptCenter.X + 2, lp.ptCenter.Y + 2), 3)
        Next
    End Sub
End Class




Public Class Line_DepthSimple : Inherits TaskParent
    Dim lineLR As New Line_LeftRight
    Public Sub New()
        desc = "How many lines have both endpoints with depth?"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lineLR.Run(emptyMat)
        CvtColor(lineLR.dst2, dst2, ColorConversionCodes.GRAY2BGR)
        labels(2) = lineLR.labels(2)

        Dim leftLines = lineLR.leftList
        Dim rightLines = lineLR.rightList

        Dim count As Integer
        For Each lp In leftLines
            Dim depth1 = task.pcSplit(2).Get(Of Single)(lp.p1.Y, lp.p1.X)
            If depth1 > 0 Then Circle(dst2, lp.p1, task.DotSize + 2, task.highlight, -1)
            Dim depth2 = task.pcSplit(2).Get(Of Single)(lp.p2.Y, lp.p2.X)
            If depth2 > 0 Then Circle(dst2, lp.p2, task.DotSize + 2, task.highlight, -1)

            If depth1 > 0 And depth2 > 0 Then count += 1
        Next

        SetTrueText(CStr(count) + " lines had depth at both endpoints", 3)
    End Sub
End Class



Public Class Line_Depth : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public Sub New()
        dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
        desc = "Get the best measure of depth for the end points of each line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lpList.Clear()
        For Each lp In task.lines.lpList
            dst1(lp.rect).SetTo(0)
            Line(dst1, lp.p1, lp.p2, 255, task.lineWidth)

            If task.drawRect.Width > 0 Then
                If lp.rect.IntersectsWith(task.drawRect) Then
                    Dim k = 0
                End If
            End If

            Dim ptArray() As cv.Point = Nothing
            Dim _fnz As New Mat
            FindNonZero(dst1(lp.rect), _fnz)
            _fnz.GetArray(Of cv.Point)(ptArray)

            Dim depth1 As Single = 0, depth2 As Single = 0
            Dim p1 As cv.Point = Nothing
            Dim p2 As cv.Point = Nothing
            For Each p1 In ptArray
                p1 = New cv.Point(lp.rect.X + p1.X, lp.rect.Y + p1.Y)
                depth1 = task.pcSplit(2).Get(Of Single)(p1.Y, p1.X)
                If depth1 > 0 Then Exit For
            Next

            If depth1 > 0 Then
                For i = ptArray.Count - 1 To 0 Step -1
                    p2 = New cv.Point(lp.rect.X + ptArray(i).X, lp.rect.Y + ptArray(i).Y)
                    depth2 = task.pcSplit(2).Get(Of Single)(p2.Y, p2.X)
                    If depth2 > 0 Then
                        Dim lpNew = New lpData(p1, p2)
                        If lpNew.length > task.gridWH * 2 And lpNew.rect.Width > 0 And lpNew.rect.Height > 0 Then
                            lpNew.age = lp.age
                            lpNew.index = lpList.Count + 1
                            lpNew.pVec1 = task.pointCloud.Get(Of Vec3f)(p1.Y, p1.X)
                            lpNew.pVec2 = task.pointCloud.Get(Of Vec3f)(p2.Y, p2.X)
                            lpList.Add(lpNew)
                            Exit For
                        End If
                    End If
                Next
            End If
        Next

        dst3 = task.pointCloud.Clone
        dst2 = task.color.Clone
        For Each lp In lpList
            Line(dst3, lp.p1, lp.p2, black, task.lineWidth + 4)
            Line(dst2, lp.p1, lp.p2, task.highlight, task.lineWidth + 1)
            SetTrueText(CStr(lp.age), New cv.Point(lp.ptCenter.X + 2, lp.ptCenter.Y + 2), 2)
        Next
        labels(2) = CStr(lpList.Count) + " lines had depth at or near both endpoints.  Value is line age."
    End Sub
End Class






Public Class Line_DepthUpdate : Inherits TaskParent
    Dim lineDepth As New Line_Depth
    Public Sub New()
        dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
        desc = "For each line in the lpList output of Line_Depth, update the pointcloud with linear data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lineDepth.Run(emptyMat)
        dst2 = lineDepth.dst2
        labels(2) = lineDepth.labels(2)

        dst3 = task.pointCloud.Clone
        For Each lp In lineDepth.lpList
            Dim vec1 = task.pointCloud.Get(Of Vec3f)(lp.p1.Y, lp.p1.X)
            Dim vec2 = task.pointCloud.Get(Of Vec3f)(lp.p2.Y, lp.p2.X)

            dst1(lp.rect).SetTo(0)
            Line(dst1, lp.p1, lp.p2, 255, 1)

            Dim ptArray() As cv.Point = Nothing
            Dim _fnz As New Mat
            FindNonZero(dst1(lp.rect), _fnz)
            _fnz.GetArray(Of cv.Point)(ptArray)

            Dim xIncr = (vec1.Item0 - vec2.Item0) / ptArray.Count
            Dim yIncr = (vec1.Item1 - vec2.Item1) / ptArray.Count
            Dim zIncr = (vec1.Item2 - vec2.Item2) / ptArray.Count

            Line(dst3, lp.p1, lp.p2, black, task.lineWidth + 4)
            Line(dst2, lp.p1, lp.p2, black, task.lineWidth + 2)
            Line(dst2, lp.p1, lp.p2, task.highlight, 1)
            For i = 0 To ptArray.Count - 1
                Dim pt = ptArray(i)
                Dim vec = New Vec3f(vec1.Item0 + i * xIncr, vec1.Item1 + i * yIncr, vec1.Item2 + i * zIncr)
                dst3.Set(Of Vec3f)(pt.Y, pt.X, vec)
            Next
            SetTrueText(CStr(lp.age), New cv.Point(lp.ptCenter.X + 2, lp.ptCenter.Y + 2), 2)
        Next
    End Sub
End Class




Public Class XR_Line_LeftRight : Inherits TaskParent
    Public leftList As New List(Of lpData)
    Public rightList As New List(Of lpData)
    Public Sub New()
        dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
        dst3 = New Mat(dst2.Size, MatType.CV_8U, 0)
        desc = "Find the lines in the left and right images - use StableGray_LeftRight for left/right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.Settings.cameraName.StartsWith("StereoLabs") Then
            Static lrZED As New Line_LeftRightZED
            lrZED.Run(emptyMat)
            dst2 = lrZED.dst2
            dst3 = lrZED.dst3
            labels = lrZED.labels

            leftList = New List(Of lpData)(task.lines.lpList)
            rightList = New List(Of lpData)(lrZED.rightOnly.lpList)

            For Each lp In leftList
                SetTrueText(CStr(lp.age), New cv.Point(lp.ptCenter.X + 2, lp.ptCenter.Y + 2), 2)
            Next

            For Each lp In rightList
                SetTrueText(CStr(lp.age), New cv.Point(lp.ptCenter.X + 2, lp.ptCenter.Y + 2), 3)
            Next
        Else
            Static stableLR As New StableGray_LeftRight
            Static linesLeft As New Line_Core
            Static linesRight As New Line_Core
            stableLR.Run(emptyMat)

            Dim lastList = New List(Of lpData)(linesLeft.lpList)
            linesLeft.Run(stableLR.dst2)
            linesLeft.lpList = Line_Basics_TA.removeDuplicates(linesLeft.lpList)
            Dim averageAgeLeft = Line_Basics_TA.updateAgesAndLongest(linesLeft.lpList, lastList)

            dst2.SetTo(0)
            For Each lp In linesLeft.lpList
                Line(dst2, lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
                SetTrueText(CStr(lp.age), New cv.Point(lp.ptCenter.X + 2, lp.ptCenter.Y + 2), 2)
            Next
            labels(2) = CStr(linesLeft.lpList.Count) + " lines in the left image.  Highlighted line is the current longest line."

            lastList = New List(Of lpData)(linesRight.lpList)
            linesRight.Run(stableLR.dst3)
            linesRight.lpList = Line_Basics_TA.removeDuplicates(linesRight.lpList)
            Dim averageAgeRight = Line_Basics_TA.updateAgesAndLongest(linesRight.lpList, lastList)

            dst3.SetTo(0)
            For Each lp In linesRight.lpList
                Line(dst3, lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
                SetTrueText(CStr(lp.age), New cv.Point(lp.ptCenter.X + 2, lp.ptCenter.Y + 2), 3)
            Next
            labels(3) = CStr(linesRight.lpList.Count) + " lines in the right image."
        End If
    End Sub
End Class





Public Class Line_LeftRight : Inherits TaskParent
    Public leftList As New List(Of lpData)
    Public rightList As New List(Of lpData)
    Dim linesRight As New Line_Core
    Public Sub New()
        dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
        dst3 = New Mat(dst2.Size, MatType.CV_8U, 0)
        desc = "Find the lines in the left and right images - use StableGray_LeftRight for left/right images."
    End Sub
    Private Sub showLines(dst As Mat, lpList As List(Of lpData), pictag As Integer)
        dst.SetTo(0)
        For Each lp In lpList
            Line(dst, lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
            SetTrueText(CStr(lp.age), New cv.Point(lp.ptCenter.X + 2, lp.ptCenter.Y + 2), pictag)
        Next
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lastList = New List(Of lpData)(leftList)
        leftList = New List(Of lpData)(task.lines.lpList)

        Dim averageAgeLeft = Line_Basics_TA.updateAgesAndLongest(leftList, lastList)
        showLines(dst2, leftList, 2)
        labels(2) = CStr(leftList.Count) + " lines were found in the left image shown in white "

        Static stableLR As New StableGray_LeftRight
        stableLR.Run(emptyMat)

        lastList = New List(Of lpData)(rightList)
        linesRight.Run(stableLR.dst3)
        rightList = Line_Basics_TA.removeDuplicates(linesRight.lpList)

        Dim averageAgeRight = Line_Basics_TA.updateAgesAndLongest(rightList, lastList)
        showLines(dst3, rightList, 3)
        labels(2) += " and " + CStr(rightList.Count) + " lines in the right image shown in color."
    End Sub
End Class
