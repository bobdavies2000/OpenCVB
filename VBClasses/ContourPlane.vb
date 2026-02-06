Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class NR_ContourPlane_Basics : Inherits TaskParent
        Public Sub New()
            If atask.contours Is Nothing Then atask.contours = New Contour_Basics_List
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
            desc = "Construct a simple plane at the average depth for each of the top contours"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            atask.contours.Run(src)
            dst2 = atask.contours.dst2
            labels(2) = atask.contours.labels(2)

            dst1.SetTo(0)
            For Each contour In atask.contours.contourList
                Dim depth = atask.pcSplit(2)(contour.rect).Mean(contour.mask)
                dst1(contour.rect).SetTo(depth, contour.mask)
            Next

            cv.Cv2.Merge({atask.pcSplit(0), atask.pcSplit(1), dst1}, dst3)
        End Sub
    End Class






    Public Class NR_ContourPlane_MaxDist : Inherits TaskParent
        Public Sub New()
            If atask.contours Is Nothing Then atask.contours = New Contour_Basics_List
            desc = "Show the maxDist value in color (yellow) and in depth (blue)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            atask.contours.Run(src)
            dst2 = atask.contours.dst2
            For Each contour In atask.contours.contourList
                Dim maxDist = Distance_Basics.GetMaxDistDepth(contour.mask, contour.rect)
                DrawCircle(dst2, maxDist)
                maxDist = Distance_Basics.GetMaxDist(contour.mask, contour.rect)
                DrawCircle(dst2, maxDist, blue)
            Next
        End Sub
    End Class






    Public Class NR_ContourPlane_RectX : Inherits TaskParent
        Public Sub New()
            If atask.contours Is Nothing Then atask.contours = New Contour_Basics_List
            desc = "Assume the plane in a contour in X"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            atask.contours.Run(src)
            dst2 = atask.contours.dst2
            labels(2) = atask.contours.labels(2)
            For Each contour In atask.contours.contourList
                Dim maxDist = Distance_Basics.GetMaxDistDepth(contour.mask, contour.rect)

                Dim rleft = contour.rect, rRight = contour.rect
                rleft.Width = maxDist.X - contour.rect.X
                rRight.X = rleft.X + rleft.Width
                rRight.Width = contour.rect.Width - rleft.Width

                Dim index = atask.contours.contourList.IndexOf(contour)
                If index = 1 Then
                    dst3 = src
                    If atask.toggleOn Then
                        rleft = ValidateRect(rleft)
                        dst2.Rectangle(rleft, atask.highlight, atask.lineWidth)
                        Dim maskLeft = contour.mask(New cv.Rect(0, 0, rleft.Width, rleft.Height))
                        maskLeft = maskLeft And atask.depthMask(rleft)
                        dst3(rleft).SetTo(white, maskLeft)
                        Dim depth = atask.pcSplit(2)(rleft).Mean(maskLeft)
                        labels(3) = "Showing the left rectangle of the largest contour with depth = " + Format(depth(0), fmt3)
                    Else
                        dst2.Rectangle(rRight, atask.highlight, atask.lineWidth)
                        Dim maskRight = contour.mask(New cv.Rect(maxDist.X - contour.rect.X, 0, rRight.Width, rRight.Height))
                        maskRight = maskRight And atask.depthMask(rRight)
                        dst3(rRight).SetTo(white, maskRight)
                        Dim depth = atask.pcSplit(2)(rRight).Mean(maskRight)
                        labels(3) = "Showing the right rectangle of the largest contour with depth = " + Format(depth(0), fmt3)
                    End If
                    DrawCircle(dst2, maxDist)
                End If
            Next
        End Sub
    End Class






    Public Class NR_ContourPlane_X : Inherits TaskParent
        Public Sub New()
            If atask.contours Is Nothing Then atask.contours = New Contour_Basics_List
            desc = "Assume the plane in a contour in X"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            atask.contours.Run(src)
            dst2 = atask.contours.dst2
            For Each contour In atask.contours.contourList
                Dim maxDist = Distance_Basics.GetMaxDistDepth(contour.mask, contour.rect)

                Dim rleft = contour.rect, rRight = contour.rect
                rleft.Width = maxDist.X - contour.rect.X
                rRight.X = rleft.X + rleft.Width
                rRight.Width = contour.rect.Width - rleft.Width


            Next
        End Sub
    End Class
End Namespace