Imports System.Windows.Documents
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
    Public Overrides Sub RunAlg(src As cv.Mat)
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
    Public contours As New Contour_General
    Public hull As New List(Of cv.Point)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Compare the hull to the contour of a contour cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Contour_Info.setContourSelection(task.contours.contourList, task.contours.contourMap)

        dst2.SetTo(0)
        dst2(task.contourD.rect).SetTo(255, task.contourD.mask)
        contours.Run(dst2)

        dst3.SetTo(0)
        hull = cv.Cv2.ConvexHull(contours.allContours(0).ToArray, True).ToList
        DrawContour(dst3, contours.allContours(0).ToList, white, -1)
        DrawContour(dst3, hull, white, task.lineWidth)
    End Sub
End Class






Public Class Hull_Defect : Inherits TaskParent
    Public hull As New List(Of cv.Point)
    Public contour As cv.Point()
    Public Sub New()
        desc = "Find defects in the hull provided."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            Static hContour = New Hull_Contour
            hContour.run(src)
            dst2 = hContour.dst2
            hull = hContour.hull
            contour = hContour.contours.allContours(0)
        End If

        Dim hullIndices = cv.Cv2.ConvexHullIndices(contour, False)
        Dim defects = cv.Cv2.ConvexityDefects(contour, hullIndices.ToList)

        Dim lastV As Integer = -1
        Dim newC As New List(Of cv.Point)
        For Each v In defects
            If v(0) <> lastV And lastV >= 0 Then
                For i = lastV To v(0) - 1
                    newC.Add(contour(i))
                Next
            End If
            newC.Add(contour(v(0)))
            newC.Add(contour(v(2)))
            newC.Add(contour(v(1)))
            lastV = v(1)
        Next
        dst3.SetTo(0)
        DrawContour(dst3, newC, white)
    End Sub
End Class

