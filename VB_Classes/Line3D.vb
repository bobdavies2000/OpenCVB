Imports cv = OpenCvSharp
Public Class Line3D_Draw : Inherits VB_Parent
    Public p1 As cv.Point, p2 As cv.Point
    Dim plot As New Plot_OverTimeScalar
    Public Sub New()
        If standaloneTest() Then task.gOptions.displayDst1.Checked = True
        plot.plotCount = 2

        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)

        p1 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        p2 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        labels(2) = "Click twice in the image below to draw a line and that line's depth is correlated in X to Z and Y to Z in the plot at right"
        desc = "Determine where a 3D line is close to the real depth data"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static toggleFirstSecond As Boolean

        If standaloneTest() Then
            If task.mouseClickFlag Then
                If toggleFirstSecond = False Then
                    p1 = task.clickPoint
                Else
                    p2 = task.clickPoint
                    toggleFirstSecond = False
                End If
            End If
        End If

        If toggleFirstSecond Then Exit Sub ' wait until the second point is selected...

        dst1 = src
        drawLine(dst1, p1, p2, task.highlightColor)
        dst0.SetTo(0)
        drawLine(dst0, p1, p2, 255)
        dst1.SetTo(0)
        task.pcSplit(0).CopyTo(dst1, dst0)
        Dim points = dst1.FindNonZero()

        Dim nextList As New List(Of cv.Point3f)
        For i = 0 To points.Rows - 1
            Dim pt = points.Get(Of cv.Point)(i, 0)
            nextList.Add(task.pointCloud.Get(Of cv.Point3f)(pt.Y, pt.X))
        Next
        If nextList.Count = 0 Then Exit Sub ' line is completely in area with no depth.

        Dim pts As New cv.Mat(nextList.Count, 1, cv.MatType.CV_32FC3, nextList.ToArray)
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










Public Class Line3D_Checks : Inherits VB_Parent
    Dim pts As New PointCloud_Basics
    Public pcLines As New List(Of cv.Point3f)
    Public Sub New()
        desc = "Use the first and last points in the sequence to build a single line and then check it against the rest of the sequence."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        pts.Run(src)
        dst3 = pts.dst2

        pcLines.Clear()
        For y = 0 To task.gridRows - 1
            Dim vecList As New List(Of cv.Point3f)
            For x = 0 To task.gridCols - 1
                Dim vec = pts.dst3.Get(Of cv.Point3f)(y, x)
                If vec.Z > 0 Then
                    vecList.Add(vec)
                Else
                    If vecList.Count > 2 Then
                        pcLines.Add(New cv.Point3f(1, 1, 1))
                        pcLines.Add(vecList(0))
                        pcLines.Add(vecList(vecList.Count - 1))
                    End If
                    vecList.Clear()
                End If
            Next
        Next
    End Sub
End Class








Public Class Line3D_CandidatesFirstLast : Inherits VB_Parent
    Dim pts As New PointCloud_Basics
    Public pcLines As New List(Of cv.Point3f)
    Public pcLinesMat As cv.Mat
    Public actualCount As Integer
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Get a list of points from PointCloud_Basics.  Identify first and last as the line in the sequence"
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
            drawLine(dst2, p1, p2, cv.Scalar.White)
        Next
    End Sub
    Public Sub RunVB(src as cv.Mat)
        pts.Run(src)
        dst2 = pts.dst2

        pcLines.Clear()
        addLines(pts.hList, pts.xyHList)
        addLines(pts.vList, pts.xyVList)

        pcLinesMat = New cv.Mat(pcLines.Count, 1, cv.MatType.CV_32FC3, pcLines.ToArray)
        labels(2) = "Point series found = " + CStr(pts.hList.Count + pts.vList.Count)
    End Sub
End Class








Public Class Line3D_CandidatesAll : Inherits VB_Parent
    Dim pts As New PointCloud_Basics
    Public pcLines As New List(Of cv.Point3f)
    Public pcLinesMat As cv.Mat
    Public actualCount As Integer
    Dim white32 As New cv.Point3f(1, 1, 1)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Get a list of points from PointCloud_Basics.  Identify all the lines in the sequence"
    End Sub
    Private Sub addLines(nextList As List(Of List(Of cv.Point3f)), xyList As List(Of List(Of cv.Point)))
        For i = 0 To nextList.Count - 1
            For j = 0 To nextList(i).Count - 2
                pcLines.Add(white32)
                pcLines.Add(nextList(i)(j))
                pcLines.Add(nextList(i)(j + 1))
            Next
        Next

        For Each ptlist In xyList
            For i = 0 To ptlist.Count - 2
                Dim p1 = ptlist(i)
                Dim p2 = ptlist(i + 1)
                drawLine(dst2, p1, p2, cv.Scalar.White)
            Next
        Next
    End Sub
    Public Sub RunVB(src as cv.Mat)
        pts.Run(src)
        dst2 = pts.dst2

        pcLines.Clear()
        addLines(pts.hList, pts.xyHList)
        addLines(pts.vList, pts.xyVList)

        pcLinesMat = New cv.Mat(pcLines.Count, 1, cv.MatType.CV_32FC3, pcLines.ToArray)
        labels(2) = "Point series found = " + CStr(pts.hList.Count + pts.vList.Count)
    End Sub
End Class
