Imports cv = OpenCvSharp
Public Class Mouse_Basics : Inherits TaskParent
    Dim lastPoint = New cv.Point
    Dim colorIndex As Integer
    Public Sub New()
        labels(2) = "Move the mouse below to show mouse tracking."
        desc = "Test the mousePoint interface"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        ' only display mouse movement in the lower left image (pic.tag = 2)
        If lastPoint = algTask.mouseMovePoint Or algTask.mousePicTag <> 2 Then Exit Sub
        lastPoint = algTask.mouseMovePoint
        Dim nextColor = algTask.scalarColors(colorIndex)
        Dim nextPt = algTask.mouseMovePoint
        DrawCircle(dst2,nextPt, algTask.DotSize + 3, nextColor)
        colorIndex += 1
        If colorIndex >= algTask.scalarColors.Count Then colorIndex = 0
    End Sub
End Class



Public Class Mouse_LeftClickZoom : Inherits TaskParent
    Public Sub New()
        labels(2) = "Left click and drag to draw a rectangle"
        desc = "Demonstrate what the left-click enables"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        SetTrueText("Left-click and drag to select a region in any of the images." + vbCrLf +
                    "The selected area is a rectangle that is saved in algTask.drawRect." + vbCrLf +
                    "In this example, the selected region from the BGR image will be resized to fit in the Result2 image to the right." + vbCrLf +
                    "Double-click an image to remove the selected region.")

        If algTask.drawRect.Width <> 0 And algTask.drawRect.Height <> 0 Then dst3 = src(algTask.drawRect).Resize(dst3.Size())
    End Sub
End Class








Public Class Mouse_ClickPointUsage : Inherits TaskParent
    Public Sub New()
        desc = "This algorithm shows how to use algTask.ClickPoint to dynamically identify what to break on."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        SetTrueText("Click on one of the feature points (carefully) to hit the breakpoint below.")

        For Each pt In algTask.features
            If pt = algTask.ClickPoint Then
                debug.writeline("Hit the point you selected.")
            End If
        Next
    End Sub
End Class
