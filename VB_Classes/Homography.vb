Imports cv = OpenCvSharp
Public Class Homography_Basics : Inherits TaskParent
    Public corners1 As New List(Of cv.Point2d)
    Public corners2 As New List(Of cv.Point2d)
    Dim random As New Random_Point2d
    Dim options As New Options_Homography
    Public Sub New()
        desc = "Build the homography matrix from 2 lists of corners and use it in a WarpPerspective call."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        Options.RunOpt()

        If standaloneTest() And task.heartBeat And options.hMethod = cv.HomographyMethods.None Then
            random.Run(empty)
            corners1 = New List(Of cv.Point2d)(random.PointList)
            random.Run(empty)
            corners2 = New List(Of cv.Point2d)(random.PointList)
        End If

        ' cannot find a homography when less than 4...
        If corners1.Count >= 4 Or corners2.Count >= 4 Then
            Dim H = cv.Cv2.FindHomography(corners1, corners2, options.hMethod)
            If H.Width > 0 Then dst2 = src.WarpPerspective(H, src.Size)
        End If
    End Sub
End Class









Public Class Homography_FPoly : Inherits TaskParent
    Dim fPoly As New FPoly_BasicsOriginal
    Dim hGraph As New Homography_Basics
    Public Sub New()
        desc = "Use the feature polygon to warp the current image to a previous image.  This is not useful but demonstrates how to use homography."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        fPoly.Run(src)
        dst2 = fPoly.dst1
        If fPoly.fPD.currPoly Is Nothing Or fPoly.fPD.prevPoly Is Nothing Then Exit Sub
        If fPoly.fPD.currPoly.Count = 0 Or fPoly.fPD.prevPoly.Count = 0 Then Exit Sub
        If fPoly.fPD.currPoly.Count <> fPoly.fPD.prevPoly.Count Then Exit Sub

        hGraph.corners1.Clear()
        hGraph.corners2.Clear()
        For i = 0 To fPoly.fPD.currPoly.Count - 1
            Dim p1 = fPoly.fPD.currPoly(i)
            Dim p2 = fPoly.fPD.prevPoly(i)
            hGraph.corners1.Add(New cv.Point2d(p1.X, p1.Y))
            hGraph.corners2.Add(New cv.Point2d(p2.X, p2.Y))
        Next

        hGraph.Run(src)
        dst3 = hGraph.dst2
    End Sub
End Class
