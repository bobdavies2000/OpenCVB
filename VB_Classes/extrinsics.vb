Imports cv = OpenCvSharp
Public Class Extrinsics_Basics : Inherits VB_Algorithm
    Dim addw As New AddWeighted_Basics
    Dim match As New Match_Basics
    Public Sub New()
        If standaloneTest() Then gOptions.dotSizeSlider.Value = 5
        desc = "MatchShapes: Show the alignment of the BGR image to the left and right camera images."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        dst2 = task.leftview
        dst3 = task.rightview

        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        If task.drawRect.Width > 0 Then
            dst2.Rectangle(task.drawRect, cv.Scalar.White, task.lineWidth, task.lineType)
            addw.src2 = dst2(task.drawRect).Resize(dst2.Size)
            addw.Run(gray)
            dst1 = addw.dst2
        End If

        Dim pt = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        If standaloneTest() Then
            dst2.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
            dst3.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
            dst2.Circle(pt, task.dotSize - 2, cv.Scalar.Black, -1, task.lineType)
            dst3.Circle(pt, task.dotSize - 2, cv.Scalar.Black, -1, task.lineType)
            task.color.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
        End If
    End Sub
End Class







Public Class Extrinsics_Display : Inherits VB_Algorithm
    Dim options As New Options_Extrinsics
    Dim optTrans As New Options_Translation
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        labels = {"", "", "Left Image", "Right Image"}
        desc = "MatchShapes: Build overlays for the left and right images on the BGR image"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        options.RunVB()
        optTrans.RunVB()

        Dim rectLeft = New cv.Rect(options.leftCorner - optTrans.leftTrans, options.topCorner, dst2.Width - 2 * options.leftCorner, dst2.Height - 2 * options.topCorner)
        Dim rectRight = New cv.Rect(options.rightCorner - optTrans.rightTrans, options.topCorner, dst2.Width - 2 * options.rightCorner, dst2.Height - 2 * options.topCorner)
        addw.src2 = task.leftview(rectLeft).Resize(dst2.Size)
        addw.Run(src)
        dst2 = addw.dst2.Clone

        addw.src2 = task.rightview(rectRight).Resize(dst2.Size)
        addw.Run(src)
        dst3 = addw.dst2.Clone
    End Sub
End Class