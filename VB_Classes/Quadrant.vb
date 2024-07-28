﻿Imports cv = OpenCvSharp
Public Class Quadrant_Basics : Inherits VB_Parent
    Dim p1 As New cv.Point, p2 As New cv.Point(dst2.Width - 1, 0), p3 As New cv.Point(0, dst2.Height - 1)
    Dim p4 As New cv.Point(dst2.Width - 1, dst2.Height - 1), rect As New cv.Rect, mask As New cv.Mat
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, 0)
        labels(2) = "dst1 contains a map defining the quadrant value for each pixel"
        desc = "Divide the color and depth images into 4 quadrants based on the horizon and gravity vectors"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst1.SetTo(0)
        dst1.Line(task.gravityVec.p1, task.gravityVec.p2, 255, 1, cv.LineTypes.Link8)
        dst1.Line(task.horizonVec.p1, task.horizonVec.p2, 255, 1, cv.LineTypes.Link8)

        Dim flags = cv.FloodFillFlags.FixedRange Or (255 << 8)
        If dst1.Get(Of Byte)(p1.Y, p1.X) = 0 Then cv.Cv2.FloodFill(dst1, New cv.Mat, p1, 1 * 255 / 4, rect, 0, 0, flags)
        If dst1.Get(Of Byte)(p2.Y, p2.X) = 0 Then cv.Cv2.FloodFill(dst1, New cv.Mat, p2, 2 * 255 / 4, rect, 0, 0, flags)
        If dst1.Get(Of Byte)(p3.Y, p3.X) = 0 Then cv.Cv2.FloodFill(dst1, New cv.Mat, p3, 3 * 255 / 4, rect, 0, 0, flags)
        If dst1.Get(Of Byte)(p4.Y, p4.X) = 0 Then cv.Cv2.FloodFill(dst1, New cv.Mat, p4, 4 * 255 / 4, rect, 0, 0, flags)

        dst2 = ShowPalette(dst1)
    End Sub
End Class
