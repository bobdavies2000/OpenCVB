Imports cv = OpenCvSharp
Public Class ContourPlane_Basics : Inherits TaskParent
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
        desc = "Construct a simple plane at the average depth for each of the top contours"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.contours.dst2
        labels(2) = task.contours.labels(2)

        dst1.SetTo(0)
        For Each contour In task.contours.contourList
            Dim depth = task.pcSplit(2)(contour.rect).Mean(contour.mask)
            dst1(contour.rect).SetTo(depth, contour.mask)
        Next

        cv.Cv2.Merge({task.pcSplit(0), task.pcSplit(1), dst1}, dst3)
    End Sub
End Class






Public Class ContourPlane_MaxDist : Inherits TaskParent
    Public Sub New()
        desc = "Show the maxDist value in color (yellow) and in depth (blue)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.contours.dst2
        For Each contour In task.contours.contourList
            Dim maxDist = GetMaxDistDepth(contour.mask, contour.rect)
            dst2.Circle(maxDist, task.DotSize, task.highlight, -1, task.lineType)
            maxDist = GetMaxDist(contour.mask, contour.rect)
            dst2.Circle(maxDist, task.DotSize, blue, -1, task.lineType)
        Next
    End Sub
End Class






Public Class ContourPlane_RectX : Inherits TaskParent
    Public Sub New()
        desc = "Assume the plane in a contour in X"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.contours.dst2
        labels(2) = task.contours.labels(2)
        For Each contour In task.contours.contourList
            Dim maxDist = GetMaxDistDepth(contour.mask, contour.rect)

            Dim rleft = contour.rect, rRight = contour.rect
            rleft.Width = maxDist.X - contour.rect.X
            rRight.X = rleft.X + rleft.Width
            rRight.Width = contour.rect.Width - rleft.Width

            Dim index = task.contours.contourList.IndexOf(contour)
            If index = 1 Then
                dst3 = src
                If task.toggleOn Then
                    dst2.Rectangle(rleft, task.highlight, task.lineWidth)
                    Dim maskLeft = contour.mask(New cv.Rect(0, 0, rleft.Width, rleft.Height))
                    maskLeft = maskLeft And task.depthMask(rleft)
                    dst3(rleft).SetTo(white, maskLeft)
                    Dim depth = task.pcSplit(2)(rleft).Mean(maskLeft)
                    labels(3) = "Showing the left rectangle of the largest contour with depth = " + Format(depth(0), fmt3)
                Else
                    dst2.Rectangle(rRight, task.highlight, task.lineWidth)
                    Dim maskRight = contour.mask(New cv.Rect(maxDist.X - contour.rect.X, 0, rRight.Width, rRight.Height))
                    maskRight = maskRight And task.depthMask(rRight)
                    dst3(rRight).SetTo(white, maskRight)
                    Dim depth = task.pcSplit(2)(rRight).Mean(maskRight)
                    labels(3) = "Showing the right rectangle of the largest contour with depth = " + Format(depth(0), fmt3)
                End If
                dst2.Circle(maxDist, task.DotSize, task.highlight, -1, task.lineType)
            End If
        Next
    End Sub
End Class






Public Class ContourPlane_X : Inherits TaskParent
    Public Sub New()
        desc = "Assume the plane in a contour in X"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.contours.dst2
        For Each contour In task.contours.contourList
            Dim maxDist = GetMaxDistDepth(contour.mask, contour.rect)

            Dim rleft = contour.rect, rRight = contour.rect
            rleft.Width = maxDist.X - contour.rect.X
            rRight.X = rleft.X + rleft.Width
            rRight.Width = contour.rect.Width - rleft.Width


        Next
    End Sub
End Class
