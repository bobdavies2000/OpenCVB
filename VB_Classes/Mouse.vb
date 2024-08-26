Imports cvb = OpenCvSharp
Public Class Mouse_Basics : Inherits VB_Parent
    Dim lastPoint = New cvb.Point
    Dim colorIndex As Integer
    Public Sub New()
        labels(2) = "Move the mouse below to show mouse tracking."
        desc = "Test the mousePoint interface"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        ' only display mouse movement in the lower left image (pic.tag = 2)
        If lastPoint = task.mouseMovePoint Or task.mousePicTag <> 2 Then Exit Sub
        lastPoint = task.mouseMovePoint
        Dim nextColor = task.scalarColors(colorIndex)
        Dim nextPt = task.mouseMovePoint
        DrawCircle(dst2,nextPt, task.DotSize + 3, nextColor)
        colorIndex += 1
        If colorIndex >= task.scalarColors.Count Then colorIndex = 0
    End Sub
End Class



Public Class Mouse_LeftClickZoom : Inherits VB_Parent
    Public Sub New()
        labels(2) = "Left click and drag to draw a rectangle"
        desc = "Demonstrate what the left-click enables"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        SetTrueText("Left-click and drag to select a region in any of the images." + vbCrLf +
                    "The selected area is a rectangle that is saved in task.drawRect." + vbCrLf +
                    "In this example, the selected region from the BGR image will be resized to fit in the Result2 image to the right." + vbCrLf +
                    "Double-click an image to remove the selected region.")

        If task.drawRect.Width <> 0 And task.drawRect.Height <> 0 Then dst3 = src(task.drawRect).Resize(dst3.Size())
    End Sub
End Class








Public Class Mouse_ClickPointUsage : Inherits VB_Parent
    Dim feat As New Feature_Basics
    Public Sub New()
        desc = "This algorithm shows how to use task.ClickPoint to dynamically identify what to break on."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        SetTrueText("Click on one of the feature points (carefully) to hit the breakpoint below.")
        feat.Run(src)
        dst2 = feat.dst2

        For Each pt In task.features
            If pt = task.ClickPoint Then
                Console.WriteLine("Hit the point you selected.")
            End If
        Next
    End Sub
End Class
