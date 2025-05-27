Imports cv = OpenCvSharp
Public Class ContourPlane_Basics : Inherits TaskParent
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
        desc = "Construct a simple plane from the interior bricks of the top contours"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = ShowPalette(task.contourMap)
        labels(2) = CStr(task.contourList.Count) + " largest contours of the " + CStr(task.contours.classCount) + " found."

        dst1.SetTo(0)
        Dim depth As Single
        For Each contour In task.contourList
            If contour.bricks.Count = 0 Then Continue For
            depth = 0
            For Each index In contour.bricks
                depth += task.brickList(index).depth
            Next
            depth /= contour.bricks.Count
            dst1(contour.rect).SetTo(depth, contour.mask)
        Next

        cv.Cv2.Merge({task.pcSplit(0), task.pcSplit(1), dst1}, dst3)
        dst3.SetTo(0, Not dst1.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs)
    End Sub
End Class





Public Class ContourPlane_Simple : Inherits TaskParent
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
        desc = "Construct a simple plane at the average depth for each of the top contours"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = ShowPalette(task.contourMap)
        labels(2) = CStr(task.contourList.Count) + " largest contours of the " + CStr(task.contours.classCount) +
                    " found."

        dst1.SetTo(0)
        For Each contour In task.contourList
            Dim depth = task.pcSplit(2)(contour.rect).Mean(contour.mask)
            dst1(contour.rect).SetTo(depth, contour.mask)
        Next

        cv.Cv2.Merge({task.pcSplit(0), task.pcSplit(1), dst1}, dst3)
    End Sub
End Class
