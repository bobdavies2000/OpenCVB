Imports cv = OpenCvSharp
Public Class Extrinsics_Basics : Inherits TaskParent
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        If standalone Then task.gOptions.DotSizeSlider.Value = 5
        desc = "MatchShapes: Show the alignment of the BGR image to the left and right camera images."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        dst2 = task.leftView
        dst3 = task.rightView

        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2Gray)

        If task.drawRect.Width > 0 Then
            dst2.Rectangle(task.drawRect, white, task.lineWidth, task.lineType)
            addw.src2 = dst2(task.drawRect).Resize(dst2.Size)
            addw.Run(gray)
            dst1 = addw.dst2
        End If

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
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        labels = {"", "", "Left Image", "Right Image"}
        desc = "MatchShapes: Build overlays for the left and right images on the BGR image"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        options.RunOpt()
        optTrans.RunOpt()

        Dim rectLeft = New cv.Rect(options.leftCorner - optTrans.leftTrans, options.topCorner, dst2.Width - 2 * options.leftCorner, dst2.Height - 2 * options.topCorner)
        Dim rectRight = New cv.Rect(options.rightCorner - optTrans.rightTrans, options.topCorner, dst2.Width - 2 * options.rightCorner, dst2.Height - 2 * options.topCorner)
        addw.src2 = task.leftView(rectLeft).Resize(dst2.Size)
        addw.Run(src)
        dst2 = addw.dst2.Clone

        addw.src2 = task.rightView(rectRight).Resize(dst2.Size)
        addw.Run(src)
        dst3 = addw.dst2.Clone
    End Sub
End Class