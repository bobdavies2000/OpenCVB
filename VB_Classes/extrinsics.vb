Imports cv = OpenCvSharp
Public Class Extrinsics_Basics : Inherits TaskParent
    Public Sub New()
        If standalone Then task.gOptions.DotSizeSlider.Value = 5
        If standalone Then task.gOptions.displayDst1.Checked = True
        task.drawRect = New cv.Rect(dst2.Width / 3, dst2.Height / 3, dst2.Width / 3, dst2.Height / 3)
        desc = "MatchShapes: Show the alignment of the BGR image to the left and right camera images."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        dst2 = task.leftView
        dst3 = task.rightView

        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2Gray)

        dst1 = ShowAddweighted(dst2(task.drawRect).Resize(dst2.Size), dst3(task.drawRect).Resize(dst3.Size), labels(3))
        labels(3) = "Image above (dst1) is a combination of drawrect zoomed into both the left and right images."

        Dim pt = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        If standaloneTest() Then
            DrawCircle(dst2, pt, task.DotSize + 2, white)
            DrawCircle(dst3, pt, task.DotSize + 2, white)
            DrawCircle(dst2, pt, task.DotSize, cv.Scalar.Black)
            DrawCircle(dst3, pt, task.DotSize, cv.Scalar.Black)
            DrawCircle(task.color, pt, task.DotSize + 2, white)
        End If
    End Sub
End Class







Public Class Extrinsics_Display : Inherits TaskParent
    Dim options As New Options_Extrinsics
    Dim optTrans As New Options_Translation
    Public Sub New()
        labels = {"", "", "Left Image", "Right Image"}
        desc = "MatchShapes: Build overlays for the left and right images on the BGR image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        optTrans.Run()

        Dim rectLeft = New cv.Rect(options.leftCorner - optTrans.leftTrans, options.topCorner, dst2.Width - 2 * options.leftCorner, dst2.Height - 2 * options.topCorner)
        Dim rectRight = New cv.Rect(options.rightCorner - optTrans.rightTrans, options.topCorner, dst2.Width - 2 * options.rightCorner, dst2.Height - 2 * options.topCorner)
        dst2 = ShowAddweighted(task.leftView(rectLeft).Resize(dst2.Size), src, labels(2))
        labels(2) += " left view"
        dst3 = ShowAddweighted(task.rightView(rectRight).Resize(dst2.Size), src, labels(3))
        labels(3) += " right view"
    End Sub
End Class