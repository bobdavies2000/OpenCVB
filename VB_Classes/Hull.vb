Imports System.Windows.Media.Media3D
Imports cv = OpenCvSharp

Public Class Hull_Basics : Inherits VB_Algorithm
    Dim random As New Random_Basics
    Public inputPoints As New List(Of cv.Point2f)
    Public hull As New List(Of cv.Point)
    Public useRandomPoints As Boolean
    Public Sub New()
        labels = {"", "", "Input Points - draw a rectangle anywhere.  Enclosing rectangle in yellow.", ""}
        If standaloneTest() Then random.range = New cv.Rect(100, 100, 50, 50)
        desc = "Given a list of points, create a hull that encloses them."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If (standaloneTest() And task.heartBeat) Or (useRandomPoints And task.heartBeat) Then
            random.Run(empty)
            dst2.SetTo(0)
            For Each pt In random.pointList
                dst2.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType, 0)
            Next
            inputPoints = New List(Of cv.Point2f)(random.pointList)
        End If
        Dim hull2f = cv.Cv2.ConvexHull(inputPoints, True)
        hull = vbFloat2Int(hull2f.ToList)
        vbDrawContour(dst2, hull, cv.Scalar.Yellow)
    End Sub
End Class








Public Class Hull_Contour : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Compare the hull to the contour of a RedCloud cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        Dim rc = task.rc

        Dim jumpList As New List(Of cv.Point)
        For i = 1 To rc.contour.Count - 1
            Dim p1 = rc.contour(i - 1)
            Dim p2 = rc.contour(i)
            If p1.DistanceTo(p2) > 1 Then
                If jumpList.Contains(p2) = False Then jumpList.Add(p2)
            End If
        Next
        rc.hull = cv.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
        vbDrawContour(dst3, rc.contour, cv.Scalar.LightBlue, task.lineWidth)
        If rc.hull.Count > 0 Then rc.hull.RemoveAt(rc.hull.Count - 1)
        vbDrawContour(dst3, rc.hull, cv.Scalar.White, task.lineWidth)
    End Sub
End Class
