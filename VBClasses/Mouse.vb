Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Mouse_Basics : Inherits TaskParent
        Dim lastPoint = New cv.Point
        Dim colorIndex As Integer
        Public Sub New()
            labels(2) = "Move the mouse below to show mouse tracking."
            desc = "Test the mousePoint interface"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            ' only display mouse movement in the lower left image (pic.tag = 2)
            If lastPoint = taskAlg.mouseMovePoint Or taskAlg.mousePicTag <> 2 Then Exit Sub
            lastPoint = taskAlg.mouseMovePoint
            Dim nextColor = taskAlg.scalarColors(colorIndex)
            Dim nextPt = taskAlg.mouseMovePoint
            DrawCircle(dst2, nextPt, taskAlg.DotSize + 3, nextColor)
            colorIndex += 1
            If colorIndex >= taskAlg.scalarColors.Count Then colorIndex = 0
        End Sub
    End Class



    Public Class Mouse_LeftClickZoom : Inherits TaskParent
        Public Sub New()
            labels(2) = "Left click and drag to draw a rectangle"
            desc = "Demonstrate what the left-click enables"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            SetTrueText("Left-click and drag to select a region in any of the images." + vbCrLf +
                    "The selected area is a rectangle that is saved in taskAlg.drawRect." + vbCrLf +
                    "In this example, the selected region from the BGR image will be resized to fit in the Result2 image to the right." + vbCrLf +
                    "Double-click an image to remove the selected region.")

            If taskAlg.drawRect.Width <> 0 And taskAlg.drawRect.Height <> 0 Then dst3 = src(taskAlg.drawRect).Resize(dst3.Size())
        End Sub
    End Class








    Public Class Mouse_ClickPointUsage : Inherits TaskParent
        Public Sub New()
            desc = "This algorithm shows how to use taskAlg.ClickPoint to dynamically identify what to break on."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            SetTrueText("Click on one of the feature points (carefully) to hit the breakpoint below.")

            For Each pt In taskAlg.features
                If pt = taskAlg.ClickPoint Then
                    debug.writeline("Hit the point you selected.")
                End If
            Next
        End Sub
    End Class





    Public Class Mouse_ValidateLocation : Inherits TaskParent
        Public Sub New()
            desc = "With custom display resolutions, it is necessary to validate the location in terms of the workres."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            SetTrueText("Move the mouse over the RGB image above." + vbCrLf +
                        "The display resolution will appear in dst3 " + vbCrLf +
                        "while the workRes location will appear in the lower left corner.")

            Dim ratioX As Single = taskAlg.workRes.Width / taskAlg.Settings.displayRes.Width
            Dim ratioY As Single = taskAlg.workRes.Height / taskAlg.Settings.displayRes.Height
            SetTrueText("Mouse location in display resolution (X, Y): " +
                        CStr(taskAlg.mouseDisplayPoint.X) + ", " +
                        CStr(taskAlg.mouseDisplayPoint.Y) + vbCrLf +
                        "Mouse location in workRes resolution (X, Y): " +
                        CStr(CInt(taskAlg.mouseDisplayPoint.X * ratioX)) + ", " +
                        CStr(CInt(taskAlg.mouseDisplayPoint.Y * ratioY)), 3)
        End Sub
    End Class

End Namespace