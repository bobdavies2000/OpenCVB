Imports cvb = OpenCvSharp
Public Class Quadrant_Basics : Inherits VB_Parent
    Dim p1 As New cvb.Point, p2 As New cvb.Point(dst2.Width - 1, 0), p3 As New cvb.Point(0, dst2.Height - 1)
    Dim p4 As New cvb.Point(dst2.Width - 1, dst2.Height - 1), rect As New cvb.Rect, mask As New cvb.Mat
    Public Sub New()
        dst1 = New cvb.Mat(dst1.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        labels(2) = "dst1 contains a map defining the quadrant value for each pixel"
        desc = "Divide the color and depth images into 4 quadrants based on the horizon and gravity vectors"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst1.SetTo(0)
        dst1.Line(task.gravityVec.p1, task.gravityVec.p2, 255, 1, cvb.LineTypes.Link8)
        dst1.Line(task.horizonVec.p1, task.horizonVec.p2, 255, 1, cvb.LineTypes.Link8)

        Dim flags = cvb.FloodFillFlags.FixedRange Or (255 << 8)
        If dst1.Get(Of Byte)(p1.Y, p1.X) = 0 Then cvb.Cv2.FloodFill(dst1, New cvb.Mat, p1, 1 * 255 / 4, rect, 0, 0, flags)
        If dst1.Get(Of Byte)(p2.Y, p2.X) = 0 Then cvb.Cv2.FloodFill(dst1, New cvb.Mat, p2, 2 * 255 / 4, rect, 0, 0, flags)
        If dst1.Get(Of Byte)(p3.Y, p3.X) = 0 Then cvb.Cv2.FloodFill(dst1, New cvb.Mat, p3, 3 * 255 / 4, rect, 0, 0, flags)
        If dst1.Get(Of Byte)(p4.Y, p4.X) = 0 Then cvb.Cv2.FloodFill(dst1, New cvb.Mat, p4, 4 * 255 / 4, rect, 0, 0, flags)

        dst2 = ShowPalette(dst1)
    End Sub
End Class
