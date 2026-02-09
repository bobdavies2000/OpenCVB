Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class NR_ContourPlane_Basics : Inherits TaskParent
        Public Sub New()
            If tsk.contours Is Nothing Then tsk.contours = New Contour_Basics_List
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
            desc = "Construct a simple plane at the average depth for each of the top contours"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            tsk.contours.Run(src)
            dst2 = tsk.contours.dst2
            labels(2) = tsk.contours.labels(2)

            dst1.SetTo(0)
            For Each contour In tsk.contours.contourList
                Dim depth = tsk.pcSplit(2)(contour.rect).Mean(contour.mask)
                dst1(contour.rect).SetTo(depth, contour.mask)
            Next

            cv.Cv2.Merge({tsk.pcSplit(0), tsk.pcSplit(1), dst1}, dst3)
        End Sub
    End Class






    Public Class NR_ContourPlane_MaxDist : Inherits TaskParent
        Public Sub New()
            If tsk.contours Is Nothing Then tsk.contours = New Contour_Basics_List
            desc = "Show the maxDist value in color (yellow) and in depth (blue)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            tsk.contours.Run(src)
            dst2 = tsk.contours.dst2
            For Each contour In tsk.contours.contourList
                Dim maxDist = Distance_Basics.GetMaxDistDepth(contour.mask, contour.rect)
                DrawCircle(dst2, maxDist)
                maxDist = Distance_Basics.GetMaxDist(contour.mask, contour.rect)
                DrawCircle(dst2, maxDist, blue)
            Next
        End Sub
    End Class






    Public Class NR_ContourPlane_RectX : Inherits TaskParent
        Public Sub New()
            If tsk.contours Is Nothing Then tsk.contours = New Contour_Basics_List
            desc = "Assume the plane in a contour in X"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            tsk.contours.Run(src)
            dst2 = tsk.contours.dst2
            labels(2) = tsk.contours.labels(2)
            For Each contour In tsk.contours.contourList
                Dim maxDist = Distance_Basics.GetMaxDistDepth(contour.mask, contour.rect)

                Dim rleft = contour.rect, rRight = contour.rect
                rleft.Width = maxDist.X - contour.rect.X
                rRight.X = rleft.X + rleft.Width
                rRight.Width = contour.rect.Width - rleft.Width

                Dim index = tsk.contours.contourList.IndexOf(contour)
                If index = 1 Then
                    dst3 = src
                    If tsk.toggleOn Then
                        rleft = ValidateRect(rleft)
                        dst2.Rectangle(rleft, tsk.highlight, tsk.lineWidth)
                        Dim maskLeft = contour.mask(New cv.Rect(0, 0, rleft.Width, rleft.Height))
                        maskLeft = maskLeft And tsk.depthMask(rleft)
                        dst3(rleft).SetTo(white, maskLeft)
                        Dim depth = tsk.pcSplit(2)(rleft).Mean(maskLeft)
                        labels(3) = "Showing the left rectangle of the largest contour with depth = " + Format(depth(0), fmt3)
                    Else
                        dst2.Rectangle(rRight, tsk.highlight, tsk.lineWidth)
                        Dim maskRight = contour.mask(New cv.Rect(maxDist.X - contour.rect.X, 0, rRight.Width, rRight.Height))
                        maskRight = maskRight And tsk.depthMask(rRight)
                        dst3(rRight).SetTo(white, maskRight)
                        Dim depth = tsk.pcSplit(2)(rRight).Mean(maskRight)
                        labels(3) = "Showing the right rectangle of the largest contour with depth = " + Format(depth(0), fmt3)
                    End If
                    DrawCircle(dst2, maxDist)
                End If
            Next
        End Sub
    End Class






    Public Class NR_ContourPlane_X : Inherits TaskParent
        Public Sub New()
            If tsk.contours Is Nothing Then tsk.contours = New Contour_Basics_List
            desc = "Assume the plane in a contour in X"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            tsk.contours.Run(src)
            dst2 = tsk.contours.dst2
            For Each contour In tsk.contours.contourList
                Dim maxDist = Distance_Basics.GetMaxDistDepth(contour.mask, contour.rect)

                Dim rleft = contour.rect, rRight = contour.rect
                rleft.Width = maxDist.X - contour.rect.X
                rRight.X = rleft.X + rleft.Width
                rRight.Width = contour.rect.Width - rleft.Width
            Next
        End Sub
    End Class
End Namespace