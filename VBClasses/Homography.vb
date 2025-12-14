Imports cv = OpenCvSharp
Public Class Homography_Basics : Inherits TaskParent
    Public corners1 As New List(Of cv.Point2d)
    Public corners2 As New List(Of cv.Point2d)
    Dim random As New Random_Point2d
    Dim options As New Options_Homography
    Public Sub New()
        desc = "Build the homography matrix from 2 lists of corners and use it in a WarpPerspective call."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Options.Run()

        If standaloneTest() And algTask.heartBeat And options.hMethod = cv.HomographyMethods.None Then
            random.Run(src)
            corners1 = New List(Of cv.Point2d)(random.PointList)
            random.Run(src)
            corners2 = New List(Of cv.Point2d)(random.PointList)
        End If

        ' cannot find a homography when less than 4...
        If corners1.Count >= 4 Or corners2.Count >= 4 Then
            Dim H = cv.Cv2.FindHomography(corners1, corners2, options.hMethod)
            If H.Width > 0 Then dst2 = src.WarpPerspective(H, src.Size)
        End If
    End Sub
End Class

