Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class RedCloud_Basics : Inherits TaskParent
    Dim redCore As New RedCloud_Core
    Public pcList As New List(Of cloudData)
    Public Sub New()
        task.redCloud = Me
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Build contours for each cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redCore.Run(src)
        labels(2) = redCore.labels(2) + "  Age of each cell is displayed as well."

        Static pcListLast = New List(Of cloudData)(pcList)
        Static pcMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Static depthLast As cv.Mat = task.pcSplit(2)

        pcList.Clear()
        Dim r2 As cv.Rect
        For Each pc As cloudData In redCore.pcList
            Dim indexLast = pcMap.Get(Of Byte)(pc.maxDist.Y, pc.maxDist.X) - 1
            Dim r1 = pc.rect
            r2 = New cv.Rect(0, 0, 1, 1) ' fake rect to trigger conditional below...
            If indexLast >= 0 Then r2 = pcListLast(indexLast).rect
            If indexLast >= 0 And r1.IntersectsWith(r2) And task.heartBeatLT = False Then
                pc.age = pcListLast(indexLast).age + 1
                If pc.age > 1000 Then pc.age = 2
                pc.depthLast = depthLast(pc.rect).Mean(pc.mask)
                pc.maxDist = pcListLast(indexLast).maxdist
                pc.color = pcListLast(indexLast).color
            Else
                pc.color = task.vecColors(pc.index)
            End If
            pc.index = pcList.Count + 1
            pcList.Add(pc)
        Next

        dst1.SetTo(0)
        dst2.SetTo(0)
        For Each pc In pcList
            pc.contour = ContourBuild(pc.mask, cv.ContourApproximationModes.ApproxNone) ' ApproxTC89L1 or ApproxNone
            DrawTour(dst1(pc.rect), pc.contour, pc.index)
            pc.maskContour = dst1(pc.rect).InRange(pc.index, pc.index)
            dst2(pc.rect).SetTo(pc.color, pc.maskContour)
            dst2.Circle(pc.maxDist, task.DotSize, task.highlight, -1)
            SetTrueText(CStr(pc.age), pc.maxDist)
        Next

        task.pcD = RedCell_Basics.displayCell()
        If task.pcD IsNot Nothing Then
            If task.pcD.rect.Contains(task.ClickPoint) Then
                task.color(task.pcD.rect).SetTo(white, task.pcD.maskContour)
                SetTrueText(task.pcD.displayString, 3)
                SetTrueText(CStr(task.pcD.index), task.pcD.maxDist, 3)
            End If
        End If

        pcListLast = New List(Of cloudData)(task.redCloud.pcList)
        pcMap = dst1.Clone
        depthLast = task.pcSplit(2)
    End Sub
End Class






Public Class RedCloud_Core : Inherits TaskParent
    Public prepEdges As New RedPrep_Basics
    Public pcList As New List(Of cloudData)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Reduced point cloud - use 'Reduction Target' option to increase/decrease cell sizes."
        desc = "Find the biggest chunks of consistent depth data "
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prepEdges.Run(src)
        dst3 = Not prepEdges.dst2

        Dim index As Integer = 1
        Dim rect As New cv.Rect
        Dim maskRect = New cv.Rect(1, 1, dst3.Width, dst3.Height)
        Dim mask = New cv.Mat(New cv.Size(dst3.Width + 2, dst3.Height + 2), cv.MatType.CV_8U, 0)
        Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4 ' Or cv.FloodFillFlags.MaskOnly ' maskonly is expensive but why?
        Dim minCount = dst3.Total * 0.001, maxCount = dst3.Total * 3 / 4
        Dim newList As New SortedList(Of Integer, cloudData)(New compareAllowIdenticalInteger)
        For y = 0 To dst3.Height - 1
            For x = 0 To dst3.Width - 1
                Dim pt = New cv.Point(x, y)
                ' skip the regions with no depth or those that were already floodfilled.
                If dst3.Get(Of Byte)(pt.Y, pt.X) > index Then
                    Dim count = cv.Cv2.FloodFill(dst3, mask, pt, index, rect, 0, 0, flags)
                    If rect.Width > 0 And rect.Height > 0 Then
                        If count >= minCount And count < maxCount Then
                            Dim pc = New cloudData(dst3(rect).InRange(index, index), rect, count)
                            index += 1
                            pc.index = index
                            newList.Add(pc.maxDist.Y, pc)
                        End If
                    End If
                End If
            Next
        Next

        pcList.Clear()
        dst1.SetTo(0)
        For Each pc In newList.Values
            pc.index = pcList.Count + 1
            dst1(pc.rect).SetTo(pc.index Mod 255, pc.mask)
            SetTrueText(CStr(pc.index), New cv.Point(pc.rect.X, pc.rect.Y))
            pcList.Add(pc)
        Next

        If standaloneTest() Then
            dst2 = ShowPalette254(dst1)

            For Each pc In pcList
                dst2.Circle(pc.maxDist, task.DotSize, task.highlight, -1)
            Next
        End If
        labels(2) = CStr(newList.Count) + " regions were identified."
    End Sub
End Class






Public Class RedCloud_MotionSimple : Inherits TaskParent
    Dim redContours As New RedCloud_Basics
    Public Sub New()
        task.gOptions.HistBinBar.Maximum = 255
        task.gOptions.HistBinBar.Value = 255
        desc = "Use motion to identify which cells changed."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redContours.Run(src)
        dst1 = redContours.dst1
        dst2 = redContours.dst2
        labels(2) = redContours.labels(2)

        dst1.SetTo(0, Not task.motionMask)

        Dim histogram As New cv.Mat
        Dim ranges = {New cv.Rangef(1, 256)}
        cv.Cv2.CalcHist({dst1}, {0}, New cv.Mat, histogram, 1, {task.histogramBins}, ranges)

        Dim histArray(histogram.Rows - 1) As Single
        Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

        Dim pcUsed As New List(Of Integer)
        If task.heartBeatLT Then dst3 = dst2.Clone
        For i = 1 To histArray.Count - 1
            If histArray(i) > 0 And pcUsed.Contains(i) = False Then
                Dim pc = redContours.pcList(i)
                dst3(pc.rect).SetTo(task.scalarColors(pc.index), pc.mask)
                pcUsed.Add(i)
            End If
        Next
    End Sub
End Class






Public Class RedCloud_Motion : Inherits TaskParent
    Dim redContours As New RedCloud_Basics
    Public Sub New()
        task.gOptions.HistBinBar.Maximum = 255
        task.gOptions.HistBinBar.Value = 255
        desc = "Use motion to identify which cells changed."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redContours.Run(src)
        dst1 = redContours.dst1
        dst2 = redContours.dst2
        labels(2) = redContours.labels(2)

        dst3.SetTo(0)
        For Each pc In task.redCloud.pcList
            If pc.age > 10 Then DrawTour(dst3(pc.rect), pc.contour, pc.color)
        Next
    End Sub
End Class






Public Class RedCloud_Hulls : Inherits TaskParent
    Public Sub New()
        desc = "Create a hull for each RedCloud cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        dst3.SetTo(0)
        Dim hullCounts As New List(Of Integer)
        Dim contourCounts As New List(Of Integer)
        For Each pc In task.redCloud.pcList
            pc.hull = cv.Cv2.ConvexHull(pc.contour.ToArray, True).ToList
            DrawTour(dst3(pc.rect), pc.hull, pc.color, -1)
            hullCounts.Add(pc.hull.Count)
            contourCounts.Add(pc.contour.Count)
            SetTrueText(CStr(pc.age), pc.maxDist)
        Next
        labels(3) = "Average hull length = " + Format(hullCounts.Average, fmt1) + " points.  " +
                    "Average contour length = " + Format(contourCounts.Average, fmt1) + " points."
    End Sub
End Class







Public Class RedCloud_Defect : Inherits TaskParent
    Public hull As New List(Of cv.Point)
    Public Sub New()
        desc = "Find defects in the RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        dst3.SetTo(0)
        For Each pc In task.redCloud.pcList
            Dim hullIndices = cv.Cv2.ConvexHullIndices(pc.contour, False)
            For i = 0 To pc.contour.Count - 1
                Dim p1 = pc.contour(i)
                For j = i + 1 To pc.contour.Count - 1
                    Dim p2 = pc.contour(j)
                    If p1 = p2 Then Continue For
                Next
            Next

            Try
                Dim defects = cv.Cv2.ConvexityDefects(pc.contour, hullIndices.ToList)
                Dim lastV As Integer = -1
                Dim newC As New List(Of cv.Point)
                For Each v In defects
                    If v(0) <> lastV And lastV >= 0 Then
                        For i = lastV To v(0) - 1
                            newC.Add(pc.contour(i))
                        Next
                    End If
                    newC.Add(pc.contour(v(0)))
                    newC.Add(pc.contour(v(2)))
                    newC.Add(pc.contour(v(1)))
                    lastV = v(1)
                Next
                DrawTour(dst3(pc.rect), newC, pc.color)
            Catch ex As Exception
                Continue For
            End Try
        Next
    End Sub
End Class