Imports cvb = OpenCvSharp
Public Class Hull_Basics : Inherits VB_Parent
    Dim random As New Random_Basics
    Public inputPoints As New List(Of cvb.Point2f)
    Public hull As New List(Of cvb.Point)
    Public useRandomPoints As Boolean
    Public Sub New()
        labels = {"", "", "Input Points - draw a rectangle anywhere.  Enclosing rectangle in yellow.", ""}
        If standaloneTest() Then random.range = New cvb.Rect(100, 100, 50, 50)
        desc = "Given a list of points, create a hull that encloses them."
    End Sub
    Private Function vbFloat2Int(ptList2f As List(Of cvb.Point2f)) As List(Of cvb.Point)
        Dim ptList As New List(Of cvb.Point)
        For Each pt In ptList2f
            ptList.Add(New cvb.Point(CInt(pt.X), CInt(pt.Y)))
        Next
        Return ptList
    End Function
    Public Sub RunVB(src As cvb.Mat)
        If (standaloneTest() And task.heartBeat) Or (useRandomPoints And task.heartBeat) Then
            random.Run(empty)
            dst2.SetTo(0)
            For Each pt In random.PointList
                DrawCircle(dst2, pt, task.DotSize, cvb.Scalar.White)
            Next
            inputPoints = New List(Of cvb.Point2f)(random.PointList)
        End If
        Dim hull2f = cvb.Cv2.ConvexHull(inputPoints, True)
        hull = vbFloat2Int(hull2f.ToList)
        DrawContour(dst2, hull, cvb.Scalar.Yellow)
    End Sub
End Class








Public Class Hull_Contour : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Compare the hull to the contour of a RedCloud cell"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        Dim rc = task.rc

        Dim jumpList As New List(Of cvb.Point)
        For i = 1 To rc.contour.Count - 1
            Dim p1 = rc.contour(i - 1)
            Dim p2 = rc.contour(i)
            If p1.DistanceTo(p2) > 1 Then
                If jumpList.Contains(p2) = False Then jumpList.Add(p2)
            End If
        Next
        rc.hull = cvb.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
        DrawContour(dst3, rc.contour, cvb.Scalar.LightBlue, task.lineWidth)
        If rc.hull.Count > 0 Then rc.hull.RemoveAt(rc.hull.Count - 1)
        DrawContour(dst3, rc.hull, cvb.Scalar.White, task.lineWidth)
    End Sub
End Class
