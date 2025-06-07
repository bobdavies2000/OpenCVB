Imports cv = OpenCvSharp
Public Class Hull_Basics : Inherits TaskParent
    Dim random As New Random_Basics
    Public inputPoints As New List(Of cv.Point2f)
    Public hull As New List(Of cv.Point)
    Public useRandomPoints As Boolean
    Public Sub New()
        labels = {"", "", "Input Points - draw a rectangle anywhere.  Enclosing rectangle in yellow.", ""}
        If standalone Then random.range = New cv.Rect(100, 100, 50, 50)
        desc = "Given a list of points, create a hull that encloses them."
    End Sub
    Private Function vbFloat2Int(ptList2f As List(Of cv.Point2f)) As List(Of cv.Point)
        Dim ptList As New List(Of cv.Point)
        For Each pt In ptList2f
            ptList.Add(New cv.Point(CInt(pt.X), CInt(pt.Y)))
        Next
        Return ptList
    End Function
    Public Overrides sub RunAlg(src As cv.Mat)
        If (standaloneTest() And task.heartBeat) Or (useRandomPoints And task.heartBeat) Then
            random.Run(src)
            dst2.SetTo(0)
            For Each pt In random.PointList
                DrawCircle(dst2, pt, task.DotSize, white)
            Next
            inputPoints = New List(Of cv.Point2f)(random.PointList)
        End If
        Dim hull2f = cv.Cv2.ConvexHull(inputPoints, True)
        hull = vbFloat2Int(hull2f.ToList)
        DrawContour(dst2, hull, cv.Scalar.Yellow)
    End Sub
End Class








Public Class Hull_Contour : Inherits TaskParent
    Dim contours As New Contour_General
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Compare the hull to the contour of a contour cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Contour_Info.setContourSelection(task.contours.contourList, task.contours.contourMap)

        dst2.SetTo(0)
        dst2(task.contourD.rect).SetTo(255, task.contourD.mask)
        contours.Run(dst2)
        'dst3.SetTo(0)
        'Dim contour = task.contourD

        'Dim jumpList As New List(Of cv.Point)
        'For i = 1 To rc.contour.Count - 1
        '    Dim p1 = rc.contour(i - 1)
        '    Dim p2 = rc.contour(i)
        '    If p1.DistanceTo(p2) > 1 Then
        '        If jumpList.Contains(p2) = False Then jumpList.Add(p2)
        '    End If
        'Next

        dst3.SetTo(0)
        Dim hull = cv.Cv2.ConvexHull(contours.allContours(0).ToArray, True).ToList
        DrawContour(dst3, contours.allContours(0).ToList, white, -1)
        ' If hull.Count > 0 Then hull.RemoveAt(hull.Count - 1)
        DrawContour(dst3, hull, white, task.lineWidth)
    End Sub
End Class
