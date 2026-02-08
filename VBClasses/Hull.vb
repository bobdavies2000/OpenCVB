Imports cv = OpenCvSharp
Namespace VBClasses
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
            If (standaloneTest() And tsk.heartBeat) Or (useRandomPoints And tsk.heartBeat) Then
                random.Run(src)
                dst2.SetTo(0)
                For Each pt In random.PointList
                    DrawCircle(dst2, pt, tsk.DotSize, white)
                Next
                inputPoints = New List(Of cv.Point2f)(random.PointList)
            End If
            Dim hull2f = cv.Cv2.ConvexHull(inputPoints, True)
            hull = vbFloat2Int(hull2f.ToList)
            DrawTour(dst2, hull, cv.Scalar.Yellow)
        End Sub
    End Class







    Public Class NR_Hull_Defect : Inherits TaskParent
        Public hull As New List(Of cv.Point)
        Public contour As cv.Point()
        Public Sub New()
            desc = "Find defects in the hull provided."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                Static hContour As New Hull_Contour
                hContour.Run(src)
                dst2 = hContour.dst2
                hull = hContour.hull
                If hContour.contours1.sortContours.allContours.Count = 0 Then Exit Sub ' nothing to work on yet...
                contour = hContour.contours1.sortContours.allContours(0)
            End If

            Dim hullIndices = cv.Cv2.ConvexHullIndices(contour, False)
            For i = 0 To contour.Count - 1
                Dim p1 = contour(i)
                For j = i + 1 To contour.Count - 1
                    Dim p2 = contour(j)
                    If p1 = p2 Then
                        SetTrueText("Contour is self-intersecting and convexityDefects will fail.")
                        Exit Sub
                    End If
                Next
            Next

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
            DrawTour(dst3, newC, white)
        End Sub
    End Class





    Public Class Hull_Contour : Inherits TaskParent
        Public hull As New List(Of cv.Point)
        Public contours1 As New Contour_Basics
        Public contours2 As New Contour_Basics
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "Compare the hull to the contour of a contour cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            contours1.Run(src)
            tsk.contourD = contours1.selectContour()

            dst2.SetTo(0)
            dst2(tsk.contourD.rect).SetTo(255, tsk.contourD.mask)
            contours2.Run(dst2)

            dst3.SetTo(0)
            If contours1.sortContours.allContours.Count > 0 Then
                If contours1.sortContours.allContours(0).Count > 0 Then
                    hull = cv.Cv2.ConvexHull(contours1.sortContours.allContours(0), True).ToList

                    DrawTour(dst3, contours2.sortContours.allContours(0).ToList, white, -1)
                    DrawTour(dst3, hull, white, tsk.lineWidth)
                End If
            End If
        End Sub
    End Class
End Namespace