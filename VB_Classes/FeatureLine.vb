Imports cv = OpenCvSharp
Public Class FeatureLine_Basics : Inherits TaskParent
    Dim options As New Options_Features
    Public Sub New()
        labels = {"", "", "Longest line present.", ""}
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find and track a line using the end points"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        options.RunOpt()

        task.lines.Run(src)

        dst3.SetTo(0)
        For Each lp In task.lines.lpSorted
            dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next

        dst2 = src
        Dim lpt = task.lines.lpSorted(0)
        dst2.Line(lpt.p1, lpt.p2, task.HighlightColor, task.lineWidth + 1, task.lineType)
    End Sub
End Class





Public Class FeatureLine_Detector : Inherits TaskParent
    Dim lines As New Line_Detector
    Dim lineDisp As New Line_DisplayInfoOld
    Dim options As New Options_Features
    Dim match As New Match_tCell
    Public tcells As List(Of tCell)
    Public Sub New()
        Dim tc As tCell
        tcells = New List(Of tCell)({tc, tc})
        labels = {"", "", "Longest line present.", ""}
        desc = "Find and track a line using the end points"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        options.RunOpt()
        Dim distanceThreshold = 50 ' pixels - arbitrary but realistically needs some value
        Dim linePercentThreshold = 0.7 ' if less than 70% of the pixels in the line are edges, then find a better line.  Again, arbitrary but realistic.

        Dim correlationMin = options.correlationMin
        Dim correlationTest = tcells(0).correlation <= correlationMin Or tcells(1).correlation <= correlationMin
        lineDisp.distance = tcells(0).center.DistanceTo(tcells(1).center)
        If task.optionsChanged Or correlationTest Or lineDisp.maskCount / lineDisp.distance < linePercentThreshold Or lineDisp.distance < distanceThreshold Then
            Dim templatePad = options.templatePad
            lines.subsetRect = New cv.Rect(templatePad * 3, templatePad * 3, src.Width - templatePad * 6, src.Height - templatePad * 6)
            lines.Run(src.Clone)

            If lines.lpList.Count = 0 Then
                SetTrueText("No lines found.", 3)
                Exit Sub
            End If
            Dim lp = lines.lpList(0)

            tcells(0) = match.createCell(src, 0, lp.p1)
            tcells(1) = match.createCell(src, 0, lp.p2)
        End If

        dst2 = src.Clone
        For i = 0 To tcells.Count - 1
            match.tCells(0) = tcells(i)
            match.Run(src)
            tcells(i) = match.tCells(0)
            SetTrueText(tcells(i).strOut, New cv.Point(tcells(i).rect.X, tcells(i).rect.Y))
            SetTrueText(tcells(i).strOut, New cv.Point(tcells(i).rect.X, tcells(i).rect.Y), 3)
        Next

        lineDisp.tcells = New List(Of tCell)(tcells)
        lineDisp.Run(src)
        dst2 = lineDisp.dst2
        SetTrueText(lineDisp.strOut, New cv.Point(10, 40), 3)
    End Sub
End Class






Public Class FeatureLine_VerticalVerify : Inherits TaskParent
    Dim linesVH As New FeatureLine_VH
    Public verify As New IMU_VerticalVerify
    Public Sub New()
        desc = "Select a line or group of lines and track the result"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        linesVH.Run(src)

        verify.gCells = New List(Of gravityLine)(linesVH.gCells)
        verify.Run(src)
        dst2 = verify.dst2
    End Sub
End Class








Public Class FeatureLine_VH : Inherits TaskParent
    Public gCells As New List(Of gravityLine)
    Dim match As New Match_tCell
    Dim gLines As New Line_GCloud
    Dim options As New Options_Features
    Public Sub New()
        labels(3) = "More readable than dst1 - index, correlation, length (meters), and ArcY"
        desc = "Find and track all the horizontal or vertical lines"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        options.RunOpt()

        Dim templatePad = options.templatePad
        ' gLines.lines.subsetRect = New cv.Rect(templatePad * 3, templatePad * 3, src.Width - templatePad * 6, src.Height - templatePad * 6)
        gLines.Run(src)

        Dim sortedLines = If(options.useVertical, gLines.sortedVerticals, gLines.sortedHorizontals)
        If sortedLines.Count = 0 Then
            SetTrueText("There were no vertical lines found.", 3)
            Exit Sub
        End If

        Dim gc As gravityLine
        gCells.Clear()
        match.tCells.Clear()
        For i = 0 To sortedLines.Count - 1
            gc = sortedLines.ElementAt(i).Value

            If i = 0 Then
                dst1.SetTo(0)
                gc.tc1.template.CopyTo(dst1(gc.tc1.rect))
                gc.tc2.template.CopyTo(dst1(gc.tc2.rect))
            End If

            match.tCells.Clear()
            match.tCells.Add(gc.tc1)
            match.tCells.Add(gc.tc2)

            match.Run(src)
            Dim correlationMin = options.correlationMin
            If match.tCells(0).correlation >= correlationMin And match.tCells(1).correlation >= correlationMin Then
                gc.tc1 = match.tCells(0)
                gc.tc2 = match.tCells(1)
                gc = gLines.updateGLine(src, gc, gc.tc1.center, gc.tc2.center)
                If gc.len3D > 0 Then gCells.Add(gc)
            End If
        Next

        dst2 = src
        dst3.SetTo(0)
        For i = 0 To gCells.Count - 1
            Dim tc As tCell
            gc = gCells(i)
            Dim p1 As cv.Point2f, p2 As cv.Point2f
            For j = 0 To 2 - 1
                tc = Choose(j + 1, gc.tc1, gc.tc2)
                If j = 0 Then p1 = tc.center Else p2 = tc.center
            Next
            SetTrueText(CStr(i) + vbCrLf + tc.strOut + vbCrLf + Format(gc.arcY, fmt1), gc.tc1.center, 2)
            SetTrueText(CStr(i) + vbCrLf + tc.strOut + vbCrLf + Format(gc.arcY, fmt1), gc.tc1.center, 3)

            DrawLine(dst2, p1, p2, task.HighlightColor)
            DrawLine(dst3, p1, p2, task.HighlightColor)
        Next
    End Sub
End Class










Public Class FeatureLine_Tutorial1 : Inherits TaskParent
    Dim lines As New Line_Basics
    Public Sub New()
        labels(3) = "The highlighted lines are also lines in 3D."
        desc = "Find all the lines in the image and determine which are in the depth data."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2

        Dim raw2D As New List(Of linePoints)
        Dim raw3D As New List(Of cv.Point3f)
        For Each lp In task.lpList
            If task.pcSplit(2).Get(Of Single)(lp.p1.Y, lp.p1.X) > 0 And task.pcSplit(2).Get(Of Single)(lp.p2.Y, lp.p2.X) > 0 Then
                raw2D.Add(lp)
                raw3D.Add(task.pointCloud.Get(Of cv.Point3f)(lp.p1.Y, lp.p1.X))
                raw3D.Add(task.pointCloud.Get(Of cv.Point3f)(lp.p2.Y, lp.p2.X))
            End If
        Next

        dst3 = src
        For i = 0 To raw2D.Count - 2 Step 2
            DrawLine(dst3, raw2D(i).p1, raw2D(i).p2, task.HighlightColor)
        Next
        If task.heartBeat Then labels(2) = "Starting with " + Format(task.lpList.Count, "000") +
                                           " lines, there are " + Format(raw3D.Count / 2, "000") +
                                           " with depth data."
    End Sub
End Class







Public Class FeatureLine_Tutorial2 : Inherits TaskParent
    Dim lines As New Line_Basics
    Dim gMat As New IMU_GMatrix
    Dim options As New Options_LineFinder()
    Public Sub New()
        desc = "Find all the lines in the image and determine which are vertical and horizontal"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        options.RunOpt()

        lines.Run(src)
        dst2 = lines.dst2

        Dim raw3D As New List(Of cv.Point3f)
        For Each lp In task.lpList
            Dim pt1 As cv.Point3f, pt2 As cv.Point3f
            For j = 0 To 1
                Dim pt = Choose(j + 1, lp.p1, lp.p2)
                Dim rect = ValidateRect(New cv.Rect(pt.x - options.kSize, pt.y - options.kSize, options.kernelSize, options.kernelSize))
                Dim val = task.pointCloud(rect).Mean(task.depthMask(rect))
                If j = 0 Then pt1 = New cv.Point3f(val(0), val(1), val(2)) Else pt2 = New cv.Point3f(val(0), val(1), val(2))
            Next
            If pt1.Z > 0 And pt2.Z > 0 Then
                raw3D.Add(task.pointCloud.Get(Of cv.Point3f)(lp.p1.Y, lp.p1.X))
                raw3D.Add(task.pointCloud.Get(Of cv.Point3f)(lp.p2.Y, lp.p2.X))
            End If
        Next

        If task.heartBeat Then labels(2) = "Starting with " + Format(task.lpList.Count, "000") +
                               " lines, there are " + Format(raw3D.Count, "000") + " with depth data."
        If raw3D.Count = 0 Then
            SetTrueText("No vertical or horizontal lines were found")
        Else
            gMat.Run(src)
            task.gMatrix = gMat.gMatrix
            Dim matLines3D As cv.Mat = (cv.Mat.FromPixelData(raw3D.Count, 3, cv.MatType.CV_32F,
                                                             raw3D.ToArray)) * task.gMatrix
        End If
    End Sub
End Class








Public Class FeatureLine_LongestVerticalKNN : Inherits TaskParent
    Dim gLines As New Line_GCloud
    Dim longest As New FeatureLine_Longest
    Public Sub New()
        labels(3) = "All vertical lines.  The numbers: index and Arc-Y for the longest X vertical lines."
        desc = "Find all the vertical lines and then track the longest one with a lightweight KNN."
    End Sub
    Private Function testLastPair(lastPair As linePoints, gc As gravityLine) As Boolean
        Dim distance1 = lastPair.p1.DistanceTo(lastPair.p2)
        Dim p1 = gc.tc1.center
        Dim p2 = gc.tc2.center
        If distance1 < 0.75 * p1.DistanceTo(p2) Then Return True ' it the longest vertical * 0.75 > current lastPair, then use the longest vertical...
        Return False
    End Function
    Public Overrides sub runAlg(src As cv.Mat)
        gLines.Run(src)
        If gLines.sortedVerticals.Count = 0 Then
            SetTrueText("No vertical lines were present", 3)
            Exit Sub
        End If

        dst3 = src.Clone
        Dim index As Integer

        If testLastPair(longest.knn.lastPair, gLines.sortedVerticals.ElementAt(0).Value) Then longest.knn.lastPair = New linePoints
        For Each gc In gLines.sortedVerticals.Values
            If index >= 10 Then Exit For

            Dim p1 = gc.tc1.center
            Dim p2 = gc.tc2.center
            If longest.knn.lastPair.compare(New linePoints) Then longest.knn.lastPair = New linePoints(p1, p2)
            Dim pt = New cv.Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2)
            SetTrueText(CStr(index) + vbCrLf + Format(gc.arcY, fmt1), pt, 3)
            index += 1

            DrawLine(dst3, p1, p2, task.HighlightColor)
            longest.knn.trainInput.Add(p1)
            longest.knn.trainInput.Add(p2)
        Next

        longest.Run(src)
        dst2 = longest.dst2
    End Sub
End Class







Public Class FeatureLine_LongestV_Tutorial1 : Inherits TaskParent
    Dim lines As New FeatureLine_Finder
    Public Sub New()
        desc = "Use FeatureLine_Finder to find all the vertical lines and show the longest."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        dst2 = src.Clone
        lines.Run(src)

        If lines.sortedVerticals.Count = 0 Then
            SetTrueText("No vertical lines were found", 3)
            Exit Sub
        End If

        Dim index = lines.sortedVerticals.ElementAt(0).Value
        Dim p1 = lines.lines2D(index)
        Dim p2 = lines.lines2D(index + 1)
        DrawLine(dst2, p1, p2, task.HighlightColor)
        dst3.SetTo(0)
        DrawLine(dst3, p1, p2, task.HighlightColor)
    End Sub
End Class






Public Class FeatureLine_LongestV_Tutorial2 : Inherits TaskParent
    Dim lines As New FeatureLine_Finder
    Dim knn As New KNN_N4Basics
    Public pt1 As New cv.Point3f
    Public pt2 As New cv.Point3f
    Dim lengthReject As Integer
    Public Sub New()
        desc = "Use FeatureLine_Finder to find all the vertical lines.  Use KNN_Basics4D to track each line."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        dst2 = src.Clone
        lines.Run(src)
        dst1 = lines.dst3

        If lines.sortedVerticals.Count = 0 Then
            SetTrueText("No vertical lines were found", 3)
            Exit Sub
        End If

        Dim match3D As New List(Of cv.Point3f)
        knn.trainInput.Clear()
        For i = 0 To lines.sortedVerticals.Count - 1
            Dim sIndex = lines.sortedVerticals.ElementAt(i).Value
            Dim x1 = lines.lines2D(sIndex)
            Dim x2 = lines.lines2D(sIndex + 1)
            Dim vec = If(x1.Y < x2.Y, New cv.Vec4f(x1.X, x1.Y, x2.X, x2.Y), New cv.Vec4f(x2.X, x2.Y, x1.X, x1.Y))
            If knn.queries.Count = 0 Then knn.queries.Add(vec)
            knn.trainInput.Add(vec)
            match3D.Add(lines.lines3D(sIndex))
            match3D.Add(lines.lines3D(sIndex + 1))
        Next

        Dim saveVec = knn.queries(0)
        knn.Run(src)

        Dim index = knn.result(0, 0)
        Dim p1 = New cv.Point2f(knn.trainInput(index)(0), knn.trainInput(index)(1))
        Dim p2 = New cv.Point2f(knn.trainInput(index)(2), knn.trainInput(index)(3))
        pt1 = match3D(index * 2)
        pt2 = match3D(index * 2 + 1)
        DrawLine(dst2, p1, p2, task.HighlightColor)
        dst3.SetTo(0)
        DrawLine(dst3, p1, p2, task.HighlightColor)

        Static lastLength = lines.sorted2DV.ElementAt(0).Key
        Dim bestLength = lines.sorted2DV.ElementAt(0).Key
        knn.queries.Clear()
        If lastLength > 0.5 * bestLength Then
            knn.queries.Add(New cv.Vec4f(p1.X, p1.Y, p2.X, p2.Y))
            lastLength = p1.DistanceTo(p2)
        Else
            lengthReject += 1
            lastLength = bestLength
        End If
        labels(3) = "Length rejects = " + Format(lengthReject / (task.frameCount + 1), "0%")
    End Sub
End Class






Public Class FeatureLine_VerticalLongLine : Inherits TaskParent
    Dim lines As New FeatureLine_Finder
    Public Sub New()
        desc = "Use FeatureLine_Finder data to identify the longest lines and show its angle."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        If task.heartBeat Then
            dst2 = src.Clone
            lines.Run(src)

            If lines.sortedVerticals.Count = 0 Then
                SetTrueText("No vertical lines were found", 3)
                Exit Sub
            End If
        End If

        If lines.sortedVerticals.Count = 0 Then Exit Sub ' nothing found...
        Dim index = lines.sortedVerticals.ElementAt(0).Value
        Dim p1 = lines.lines2D(index)
        Dim p2 = lines.lines2D(index + 1)
        DrawLine(dst2, p1, p2, task.HighlightColor)
        dst3.SetTo(0)
        DrawLine(dst3, p1, p2, task.HighlightColor)
        Dim pt1 = lines.lines3D(index)
        Dim pt2 = lines.lines3D(index + 1)
        Dim len3D = distance3D(pt1, pt2)
        Dim arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958)
        SetTrueText(Format(arcY, fmt3) + vbCrLf + Format(len3D, fmt3) + "m len" + vbCrLf + Format(pt1.Z, fmt1) + "m dist", p1)
        SetTrueText(Format(arcY, fmt3) + vbCrLf + Format(len3D, fmt3) + "m len" + vbCrLf + Format(pt1.Z, fmt1) + "m distant", p1, 3)
    End Sub
End Class






Public Class FeatureLine_DetailsAll : Inherits TaskParent
    Dim lines As New FeatureLine_Finder
    Dim flow As New Font_FlowText
    Dim arcList As New List(Of Single)
    Dim arcLongAverage As New List(Of Single)
    Dim firstAverage As New List(Of Single)
    Dim firstBest As Integer
    Dim title = "ID" + vbTab + "length" + vbTab + "distance "
    Public Sub New()
        flow.parentData = Me
        flow.dst = 3
        desc = "Use FeatureLine_Finder data to collect vertical lines and measure accuracy of each."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        If task.heartBeat Then
            dst2 = src.Clone
            lines.Run(src)

            If lines.sortedVerticals.Count = 0 Then
                SetTrueText("No vertical lines were found", 3)
                Exit Sub
            End If

            dst3.SetTo(0)
            arcList.Clear()
            flow.nextMsg = title
            For i = 0 To Math.Min(10, lines.sortedVerticals.Count) - 1
                Dim index = lines.sortedVerticals.ElementAt(i).Value
                Dim p1 = lines.lines2D(index)
                Dim p2 = lines.lines2D(index + 1)
                DrawLine(dst2, p1, p2, task.HighlightColor)
                SetTrueText(CStr(i), If(i Mod 2, p1, p2), 2)
                DrawLine(dst3, p1, p2, task.HighlightColor)

                Dim pt1 = lines.lines3D(index)
                Dim pt2 = lines.lines3D(index + 1)
                Dim len3D = distance3D(pt1, pt2)
                If len3D > 0 Then
                    Dim arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958)
                    arcList.Add(arcY)
                    flow.nextMsg += Format(arcY, fmt3) + " degrees" + vbTab + Format(len3D, fmt3) + "m " + vbTab + Format(pt1.Z, fmt1) + "m"
                End If
            Next
            If flow.nextMsg = title Then flow.nextMsg = "No feature line found..."
        End If
        flow.Run(src)
        If arcList.Count = 0 Then Exit Sub

        Dim mostAccurate = arcList(0)
        firstAverage.Add(mostAccurate)
        For Each arc In arcList
            If arc > mostAccurate Then
                mostAccurate = arc
                Exit For
            End If
        Next
        If mostAccurate = arcList(0) Then firstBest += 1

        Dim avg = arcList.Average()
        arcLongAverage.Add(avg)
        labels(3) = "arcY avg = " + Format(avg, fmt1) + ", long term average = " + Format(arcLongAverage.Average, fmt1) +
                    ", first was best " + Format(firstBest / task.frameCount, "0%") + " of the time, Avg of longest line " + Format(firstAverage.Average, fmt1)
        If arcLongAverage.Count > 1000 Then
            arcLongAverage.RemoveAt(0)
            firstAverage.RemoveAt(0)
        End If
    End Sub
End Class






Public Class FeatureLine_LongestKNN : Inherits TaskParent
    Dim glines As New Line_GCloud
    Public knn As New KNN_ClosestTracker
    Public options As New Options_Features
    Public gline As gravityLine
    Public match As New Match_Basics
    Dim p1 As cv.Point, p2 As cv.Point
    Public Sub New()
        desc = "Find and track the longest line in the BGR image with a lightweight KNN."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        options.RunOpt()
        dst2 = src

        knn.Run(src.Clone)
        p1 = knn.lastPair.p1
        p2 = knn.lastPair.p2
        gline = glines.updateGLine(src, gline, p1, p2)

        Dim rect = ValidateRect(New cv.Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X) + 2, Math.Abs(p1.Y - p2.Y)))
        match.template = src(rect)
        match.Run(src)
        If match.correlation >= options.correlationMin Then
            dst3 = match.dst0.Resize(dst3.Size)
            DrawLine(dst2, p1, p2, task.HighlightColor)
            DrawCircle(dst2, p1, task.DotSize, task.HighlightColor)
            DrawCircle(dst2, p2, task.DotSize, task.HighlightColor)
            rect = ValidateRect(New cv.Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X) + 2, Math.Abs(p1.Y - p2.Y)))
            match.template = src(rect).Clone
        Else
            task.HighlightColor = If(task.HighlightColor = cv.Scalar.Yellow, cv.Scalar.Blue, cv.Scalar.Yellow)
            knn.lastPair = New linePoints(New cv.Point2f, New cv.Point2f)
        End If
        labels(2) = "Longest line end points had correlation of " + Format(match.correlation, fmt3) + " with the original longest line."
    End Sub
End Class






Public Class FeatureLine_Longest : Inherits TaskParent
    Dim glines As New Line_GCloud
    Public knn As New KNN_ClosestTracker
    Public options As New Options_Features
    Public gline As gravityLine
    Public match1 As New Match_Basics
    Public match2 As New Match_Basics
    Public Sub New()
        labels(2) = "Longest line end points are highlighted "
        desc = "Find and track the longest line in the BGR image with a lightweight KNN."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        options.RunOpt()
        dst2 = src.Clone
        Dim correlationMin = match1.options.correlationMin
        Dim templatePad = match1.options.templatePad
        Dim templateSize = match1.options.templateSize

        Static p1 As cv.Point, p2 As cv.Point
        If task.heartBeat Or match1.correlation < correlationMin And match2.correlation < correlationMin Then
            knn.Run(src.Clone)

            p1 = knn.lastPair.p1
            Dim r1 = ValidateRect(New cv.Rect(p1.X - templatePad, p1.Y - templatePad, templateSize, templateSize))
            match1.template = src(r1).Clone

            p2 = knn.lastPair.p2
            Dim r2 = ValidateRect(New cv.Rect(p2.X - templatePad, p2.Y - templatePad, templateSize, templateSize))
            match2.template = src(r2).Clone
        End If

        match1.Run(src)
        p1 = match1.matchCenter

        match2.Run(src)
        p2 = match2.matchCenter

        gline = glines.updateGLine(src, gline, p1, p2)
        DrawLine(dst2, p1, p2, task.HighlightColor)
        DrawCircle(dst2, p1, task.DotSize, task.HighlightColor)
        DrawCircle(dst2, p2, task.DotSize, task.HighlightColor)
        SetTrueText(Format(match1.correlation, fmt3), p1)
        SetTrueText(Format(match2.correlation, fmt3), p2)
    End Sub
End Class






Public Class FeatureLine_Finder : Inherits TaskParent
    Dim lines As New Line_Basics
    Public lines2D As New List(Of cv.Point2f)
    Public lines3D As New List(Of cv.Point3f)
    Public sorted2DV As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
    Public sortedVerticals As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
    Public sortedHorizontals As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
    Dim options As New Options_LineFinder()
    Public Sub New()
        desc = "Find all the lines in the image and determine which are vertical and horizontal"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        options.RunOpt()

        dst3 = src.Clone

        lines2D.Clear()
        lines3D.Clear()
        sorted2DV.Clear()
        sortedVerticals.Clear()
        sortedHorizontals.Clear()

        lines.Run(src)
        dst2 = lines.dst2

        Dim raw2D As New List(Of linePoints)
        Dim raw3D As New List(Of cv.Point3f)
        For Each lp In task.lpList
            Dim pt1 As cv.Point3f, pt2 As cv.Point3f
            For j = 0 To 1
                Dim pt = Choose(j + 1, lp.p1, lp.p2)
                Dim rect = ValidateRect(New cv.Rect(pt.x - options.kSize, pt.y - options.kSize, options.kernelSize, options.kernelSize))
                Dim val = task.pointCloud(rect).Mean(task.depthMask(rect))
                If j = 0 Then pt1 = New cv.Point3f(val(0), val(1), val(2)) Else pt2 = New cv.Point3f(val(0), val(1), val(2))
            Next

            If pt1.Z > 0 And pt2.Z > 0 And pt1.Z < 4 And pt2.Z < 4 Then ' points more than X meters away are not accurate...
                raw2D.Add(lp)
                raw3D.Add(pt1)
                raw3D.Add(pt2)
            End If
        Next

        If raw3D.Count = 0 Then
            SetTrueText("No vertical or horizontal lines were found")
        Else
            Dim matLines3D As cv.Mat = (cv.Mat.FromPixelData(raw3D.Count, 3, cv.MatType.CV_32F, raw3D.ToArray)) * task.gMatrix

            For i = 0 To raw2D.Count - 2 Step 2
                Dim pt1 = matLines3D.Get(Of cv.Point3f)(i, 0)
                Dim pt2 = matLines3D.Get(Of cv.Point3f)(i + 1, 0)
                Dim len3D = distance3D(pt1, pt2)
                Dim arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958)
                If Math.Abs(arcY - 90) < options.tolerance Then
                    DrawLine(dst3, raw2D(i).p1, raw2D(i).p2, cv.Scalar.Blue)
                    sortedVerticals.Add(len3D, lines3D.Count)
                    sorted2DV.Add(raw2D(i).p1.DistanceTo(raw2D(i).p2), lines2D.Count)
                    If pt1.Y > pt2.Y Then
                        lines3D.Add(pt1)
                        lines3D.Add(pt2)
                        lines2D.Add(raw2D(i).p1)
                        lines2D.Add(raw2D(i).p2)
                    Else
                        lines3D.Add(pt2)
                        lines3D.Add(pt1)
                        lines2D.Add(raw2D(i).p2)
                        lines2D.Add(raw2D(i).p1)
                    End If
                End If
                If Math.Abs(arcY) < options.tolerance Then
                    DrawLine(dst3, raw2D(i).p1, raw2D(i).p2, cv.Scalar.Yellow)
                    sortedHorizontals.Add(len3D, lines3D.Count)
                    If pt1.X < pt2.X Then
                        lines3D.Add(pt1)
                        lines3D.Add(pt2)
                        lines2D.Add(raw2D(i).p1)
                        lines2D.Add(raw2D(i).p2)
                    Else
                        lines3D.Add(pt2)
                        lines3D.Add(pt1)
                        lines2D.Add(raw2D(i).p2)
                        lines2D.Add(raw2D(i).p1)
                    End If
                End If
            Next
        End If
        labels(2) = "Starting with " + Format(task.lpList.Count, "000") + " lines, there are " +
                                       Format(lines3D.Count / 2, "000") + " with depth data."
        labels(3) = "There were " + CStr(sortedVerticals.Count) + " vertical lines (blue) and " + CStr(sortedHorizontals.Count) + " horizontal lines (yellow)"
    End Sub
End Class