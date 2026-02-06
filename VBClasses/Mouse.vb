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
            If lastPoint = taskA.mouseMovePoint Or taskA.mousePicTag <> 2 Then Exit Sub
            lastPoint = taskA.mouseMovePoint
            Dim nextColor = taskA.scalarColors(colorIndex)
            Dim nextPt = taskA.mouseMovePoint
            DrawCircle(dst2, nextPt, taskA.DotSize + 3, nextColor)
            colorIndex += 1
            If colorIndex >= taskA.scalarColors.Count Then colorIndex = 0
        End Sub
    End Class



    Public Class NR_Mouse_LeftClickZoom : Inherits TaskParent
        Public Sub New()
            labels(2) = "Left click and drag to draw a rectangle"
            desc = "Demonstrate what the left-click enables"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            SetTrueText("Left-click and drag to select a region in any of the images." + vbCrLf +
                    "The selected area is a rectangle that is saved in taskA.drawRect." + vbCrLf +
                    "In this example, the selected region from the BGR image will be resized to fit in the Result2 image to the right." + vbCrLf +
                    "Double-click an image to remove the selected region.")

            If taskA.drawRect.Width <> 0 And taskA.drawRect.Height <> 0 Then dst3 = src(taskA.drawRect).Resize(dst3.Size())
        End Sub
    End Class








    Public Class NR_Mouse_ClickPointUsage : Inherits TaskParent
        Public Sub New()
            desc = "This algorithm shows how to use taskA.ClickPoint to dynamically identify what to break on."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            SetTrueText("Click on one of the feature points (carefully) to hit the breakpoint below.")

            For Each pt In taskA.features
                If pt = taskA.ClickPoint Then
                    debug.writeline("Hit the point you selected.")
                End If
            Next
        End Sub
    End Class





    Public Class NR_Mouse_ValidateLocation : Inherits TaskParent
        Public Sub New()
            desc = "With custom display resolutions, it is necessary to validate the location in terms of the workres."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            SetTrueText("Move the mouse over the RGB image above." + vbCrLf +
                        "The display resolution will appear in dst3 " + vbCrLf +
                        "while the workRes location will appear in the lower left corner.")

            Dim ratioX As Single = taskA.workRes.Width / taskA.Settings.displayRes.Width
            Dim ratioY As Single = taskA.workRes.Height / taskA.Settings.displayRes.Height
            SetTrueText("Mouse location in display resolution (X, Y): " +
                        CStr(taskA.mouseDisplayPoint.X) + ", " +
                        CStr(taskA.mouseDisplayPoint.Y) + vbCrLf +
                        "Mouse location in workRes resolution (X, Y): " +
                        CStr(CInt(taskA.mouseDisplayPoint.X * ratioX)) + ", " +
                        CStr(CInt(taskA.mouseDisplayPoint.Y * ratioY)), 3)
        End Sub
    End Class

End Namespace