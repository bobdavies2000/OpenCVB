Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Intrinsics_Basics : Inherits TaskParent
        Public Sub New()
            If standalone Then If task.bricks Is Nothing Then task.bricks = New Brick_Basics
            If standalone Then task.gOptions.gravityPointCloud.Checked = False
            desc = "Some cameras don't provide aligned color and left images.  This algorithm tries to align the left and color image."
        End Sub
        Public Shared Function translate_LeftToRight(pt As cv.Point3f) As cv.Point2f
            Dim ptTranslated As cv.Point2f, ptTranslated3D As cv.Point3f
            ptTranslated3D.X = task.calibData.LtoR_rotation(0) * pt.X +
                               task.calibData.LtoR_rotation(1) * pt.Y +
                               task.calibData.LtoR_rotation(2) * pt.Z + task.calibData.LtoR_translation(0)
            ptTranslated3D.Y = task.calibData.LtoR_rotation(3) * pt.X +
                               task.calibData.LtoR_rotation(4) * pt.Y +
                               task.calibData.LtoR_rotation(5) * pt.Z + task.calibData.LtoR_translation(1)
            ptTranslated3D.Z = task.calibData.LtoR_rotation(6) * pt.X +
                               task.calibData.LtoR_rotation(7) * pt.Y +
                               task.calibData.LtoR_rotation(8) * pt.Z + task.calibData.LtoR_translation(2)
            ptTranslated.X = task.calibData.leftIntrinsics.fx * ptTranslated3D.X / ptTranslated3D.Z + task.calibData.leftIntrinsics.ppx
            ptTranslated.Y = task.calibData.leftIntrinsics.fy * ptTranslated3D.Y / ptTranslated3D.Z + task.calibData.leftIntrinsics.ppy

            Return ptTranslated
        End Function
        Public Shared Function translate_ColorToLeft(pt As cv.Point3f) As cv.Point2f
            Dim ptTranslated As cv.Point2f, ptTranslated3D As cv.Point3f
            ptTranslated3D.X = task.calibData.ColorToLeft_rotation(0) * pt.X +
                               task.calibData.ColorToLeft_rotation(1) * pt.Y +
                               task.calibData.ColorToLeft_rotation(2) * pt.Z + task.calibData.ColorToLeft_translation(0)
            ptTranslated3D.Y = task.calibData.ColorToLeft_rotation(3) * pt.X +
                               task.calibData.ColorToLeft_rotation(4) * pt.Y +
                               task.calibData.ColorToLeft_rotation(5) * pt.Z + task.calibData.ColorToLeft_translation(1)
            ptTranslated3D.Z = task.calibData.ColorToLeft_rotation(6) * pt.X +
                               task.calibData.ColorToLeft_rotation(7) * pt.Y +
                               task.calibData.ColorToLeft_rotation(8) * pt.Z + task.calibData.ColorToLeft_translation(2)
            ptTranslated.X = task.calibData.leftIntrinsics.fx * ptTranslated3D.X / ptTranslated3D.Z + task.calibData.leftIntrinsics.ppx
            ptTranslated.Y = task.calibData.leftIntrinsics.fy * ptTranslated3D.Y / ptTranslated3D.Z + task.calibData.leftIntrinsics.ppy

            Return ptTranslated
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                Dim vec = New cv.Vec3b(0, 255, 255) ' yellow
                For Each brick In task.bricks.brickList
                    If brick.depth > 0 Then DrawCircle(dst2, brick.rect.TopLeft)
                Next
            End If
        End Sub
    End Class




    Public Class Intrinsics_TranslateRGBtoLeft : Inherits TaskParent
        Public Sub New()
            task.gOptions.highlight.SelectedItem = "Red"
            task.gOptions.LineWidth.Value += 1
            desc = "Translate a point from the RGB image to the left image.  Test with the longest line as input."
        End Sub
        '---------------------------------------------
        ' Map a pixel from RGB image into Left IR image
        '---------------------------------------------
        Public Shared Function MapRgbToLeftIr(uRgb As Single, vRgb As Single, depth As Single) As cv.Point2f
            Dim rgbintr = task.calibData.rgbIntrinsics
            Dim leftintr = task.calibData.leftIntrinsics
            Dim rotation = task.calibData.ColorToLeft_Rotation
            Dim translation = task.calibData.ColorToLeft_Translation

            '-------------------------------
            ' 1. Unproject RGB pixel to 3D
            '-------------------------------
            Dim Xrgb As Single = (uRgb - rgbIntr.ppx) * depth / rgbIntr.fx
            Dim Yrgb As Single = (vRgb - rgbIntr.ppy) * depth / rgbIntr.fy
            Dim Zrgb As Single = depth

            ' Pack into vector
            Dim Prgb() As Single = {Xrgb, Yrgb, Zrgb}

            '-----------------------------------------
            ' 2. Transform RGB 3D point → Left IR 3D
            '-----------------------------------------
            Dim Pleft(2) As Single

            ' Matrix multiply: R * P + T
            For i = 0 To 2
                Pleft(i) = rotation(i * 3 + 0) * Prgb(0) + rotation(i * 3 + 1) * Prgb(1) +
                           rotation(i * 3 + 2) * Prgb(2) + translation(i)
            Next

            Dim Xleft As Single = Pleft(0)
            Dim Yleft As Single = Pleft(1)
            Dim Zleft As Single = Pleft(2)

            '-----------------------------------------
            ' 3. Project Left IR 3D point → Left pixel
            '-----------------------------------------
            Dim uLeft As Single = leftIntr.fx * (Xleft / Zleft) + leftIntr.ppx
            Dim vLeft As Single = leftIntr.fy * (Yleft / Zleft) + leftIntr.ppy

            Return New cv.Point2f(uLeft, vLeft)
        End Function

        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src
            dst3 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR) ' so we can show the red line...
            Dim count As Integer
            For Each lp In task.lines.lpList
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
                Dim depth1 = task.pointCloud.Get(Of cv.Point3f)(CInt(lp.p1.Y), CInt(lp.p1.X)).Z
                Dim depth2 = task.pointCloud.Get(Of cv.Point3f)(CInt(lp.p2.Y), CInt(lp.p2.X)).Z

                If depth1 > 0 And depth2 > 0 Then
                    Dim p1 = MapRgbToLeftIr(lp.p1.X, lp.p1.Y, depth1)
                    Dim p2 = MapRgbToLeftIr(lp.p2.X, lp.p2.Y, depth2)
                    dst3.Line(p1, p2, task.highlight, task.lineWidth, task.lineType)
                    count += 1
                End If
            Next
            labels(2) = CStr(count) + " lines had depth for both ends and could be translated from BGR to left."
        End Sub
    End Class

End Namespace