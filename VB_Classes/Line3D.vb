Imports cv = OpenCvSharp
Public Class Line3D_Basics : Inherits TaskParent
    Dim sLines As New Structured_Lines
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find all the lines in 3D using the structured slices through the pointcloud."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        sLines.Run(src)

        dst2 = src
        dst3.SetTo(0)
        For Each lp In sLines.lineX.lpList
            dst2.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth, task.lineType)
            dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next

        For Each lp In sLines.lineY.lpList
            dst2.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth, task.lineType)
            dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next

        If task.heartBeat Then
            labels(2) = CStr(sLines.lineX.lpList.Count) + " X-direction lines and " +
                        CStr(sLines.lineY.lpList.Count) + " Y-direction lines were identified in 3D."
            labels(3) = labels(2)
        End If
    End Sub
End Class






Public Class Line3D_Correlation : Inherits TaskParent
    Dim gpoints As New Feature_GridPoints
    Public Sub New()
        task.gOptions.GridSlider.Minimum = 2 ' smaller will hang
        desc = "Find the correlation of image coordinates to pointcloud coordinates"
    End Sub
    Private Function getCorrelation(A As cv.Mat, B As cv.Mat) As Single
        Dim correlation As New cv.Mat
        cv.Cv2.MatchTemplate(A, B, correlation, cv.TemplateMatchModes.CCoeffNormed)
        Return correlation.Get(Of Single)(0, 0)
    End Function
    Public Overrides sub runAlg(src As cv.Mat)
        If standalone Then task.redC.Run(src)
        dst2 = task.redC.dst2
        labels(2) = task.redC.labels(2)

        gpoints.Run(src)

        Dim xList As New List(Of Single), yList As New List(Of Single), zList As New List(Of Single)
        For Each pt In task.rc.ptList
            Dim vec = task.pointCloud.Get(Of cv.Point3f)(pt.Y, pt.X)
            xList.Add(vec.X)
            yList.Add(vec.Y)
            zList.Add(vec.Z)
        Next

        If xList.Count > 0 Then
            Dim xMat As cv.Mat = cv.Mat.FromPixelData(xList.Count, 1, cv.MatType.CV_32F, xList.ToArray)
            Dim yMat As cv.Mat = cv.Mat.FromPixelData(xList.Count, 1, cv.MatType.CV_32F, yList.ToArray)
            Dim zMat As cv.Mat = cv.Mat.FromPixelData(xList.Count, 1, cv.MatType.CV_32F, zList.ToArray)

            Dim correlationXZ As Single = getCorrelation(xMat, zMat)
            Dim correlationYZ As Single = getCorrelation(yMat, zMat)

            strOut = "X to Z correlation = " + Format(correlationXZ, fmt3) + vbCrLf +
                     "Y to Z correlation = " + Format(correlationYZ, fmt3) + vbCrLf
        End If
        If task.heartBeat Then SetTrueText(strOut, 3)
        For Each pt In task.rc.ptList
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
        Next
    End Sub
End Class






Public Class Line3D_Draw : Inherits TaskParent
    Public p1 As cv.Point, p2 As cv.Point
    Dim plot As New Plot_OverTimeScalar
    Dim toggleFirstSecond As Boolean
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        plot.plotCount = 2

        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))

        p1 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        p2 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        labels(2) = "Click twice in the image below to draw a line and that line's depth is correlated in X to Z and Y to Z in the plot at right"
        desc = "Determine where a 3D line is close to the real depth data"
    End Sub
    Private Function findCorrelation(pts1 As cv.Mat, pts2 As cv.Mat) As Single
        Dim correlationMat As New cv.Mat
        cv.Cv2.MatchTemplate(pts1, pts2, correlationMat, cv.TemplateMatchModes.CCoeffNormed)
        Return correlationMat.Get(Of Single)(0, 0)
    End Function
    Public Overrides sub runAlg(src As cv.Mat)
        If standaloneTest() Then
            If task.mouseClickFlag Then
                If toggleFirstSecond = False Then
                    p1 = task.ClickPoint
                Else
                    p2 = task.ClickPoint
                    toggleFirstSecond = False
                End If
            End If
        End If

        If toggleFirstSecond Then Exit Sub ' wait until the second point is selected...

        dst1 = src
        DrawLine(dst1, p1, p2, task.HighlightColor)
        dst0.SetTo(0)
        DrawLine(dst0, p1, p2, 255)
        dst1.SetTo(0)
        task.pcSplit(0).CopyTo(dst1, dst0)
        Dim points = dst1.FindNonZero()

        Dim nextList As New List(Of cv.Point3f)
        For i = 0 To points.Rows - 1
            Dim pt = points.Get(Of cv.Point)(i, 0)
            nextList.Add(task.pointCloud.Get(Of cv.Point3f)(pt.Y, pt.X))
        Next
        If nextList.Count = 0 Then Exit Sub ' line is completely in area with no depth.

        Dim pts As cv.Mat = cv.Mat.FromPixelData(nextList.Count, 1, cv.MatType.CV_32FC3, nextList.ToArray)
        Dim zSplit = pts.Split()
        Dim c1 = findCorrelation(zSplit(0), zSplit(2))
        Dim c2 = findCorrelation(zSplit(1), zSplit(2))

        plot.plotData = New cv.Scalar(c1, c2, 0)

        plot.Run(empty)
        dst2 = plot.dst2
        dst3 = plot.dst3
        labels(3) = "using " + CStr(nextList.Count) + " points, the correlation of X to Z = " + Format(c1, fmt3) + " (blue), correlation of Y to Z = " + Format(c2, fmt3) + " (green)"
    End Sub
End Class







Public Class Line3D_CandidatesFirstLast : Inherits TaskParent
    Dim pts As New PointCloud_Basics
    Public pcLines As New List(Of cv.Point3f)
    Public pcLinesMat As cv.Mat
    Public actualCount As Integer
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Get a list of points from PointCloud_Basics.  Identify first and last as the line " +
               "in the sequence"
    End Sub
    Private Sub addLines(nextList As List(Of List(Of cv.Point3f)), xyList As List(Of List(Of cv.Point)))
        Dim white32 As New cv.Point3f(1, 1, 1)
        For i = 0 To nextList.Count - 1
            pcLines.Add(white32)
            pcLines.Add(nextList(i)(0))
            pcLines.Add(nextList(i)(nextList(i).Count - 1))
        Next

        For Each ptlist In xyList
            Dim p1 = ptlist(0)
            Dim p2 = ptlist(ptlist.Count - 1)
            DrawLine(dst2, p1, p2, white)
        Next
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        pts.Run(src)
        dst2 = pts.dst2

        pcLines.Clear()
        addLines(pts.hList, pts.xyHList)
        addLines(pts.vList, pts.xyVList)

        pcLinesMat = cv.Mat.FromPixelData(pcLines.Count, 1, cv.MatType.CV_32FC3, pcLines.ToArray)
        labels(2) = "Point series found = " + CStr(pts.hList.Count + pts.vList.Count)
    End Sub
End Class






Public Class Line3D_Constructed : Inherits TaskParent
    Dim lines As New Line3D_Basics
    Public Sub New()
        desc = "Build the 3D lines found in Line3D_Basics"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2
        dst3 = lines.dst3
        labels(2) = lines.labels(2)


    End Sub
End Class
