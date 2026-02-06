Imports System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar
Imports OpenCvSharp
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Intrinsics_Basics : Inherits TaskParent
        Public Sub New()
            If standalone Then If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            If standalone Then atask.gOptions.gravityPointCloud.Checked = False
            desc = "Some cameras don't provide aligned color and left images.  This algorithm tries to align the left and color image."
        End Sub
        Public Shared Function translate_LeftToRight(pt As cv.Point3f) As cv.Point2f
            Dim ptTranslated As cv.Point2f, ptTranslated3D As cv.Point3f
            ptTranslated3D.X = atask.calibData.LtoR_rotation(0) * pt.X +
                               atask.calibData.LtoR_rotation(1) * pt.Y +
                               atask.calibData.LtoR_rotation(2) * pt.Z + atask.calibData.LtoR_translation(0)
            ptTranslated3D.Y = atask.calibData.LtoR_rotation(3) * pt.X +
                               atask.calibData.LtoR_rotation(4) * pt.Y +
                               atask.calibData.LtoR_rotation(5) * pt.Z + atask.calibData.LtoR_translation(1)
            ptTranslated3D.Z = atask.calibData.LtoR_rotation(6) * pt.X +
                               atask.calibData.LtoR_rotation(7) * pt.Y +
                               atask.calibData.LtoR_rotation(8) * pt.Z + atask.calibData.LtoR_translation(2)
            ptTranslated.X = atask.calibData.leftIntrinsics.fx * ptTranslated3D.X / ptTranslated3D.Z + atask.calibData.leftIntrinsics.ppx
            ptTranslated.Y = atask.calibData.leftIntrinsics.fy * ptTranslated3D.Y / ptTranslated3D.Z + atask.calibData.leftIntrinsics.ppy

            Return ptTranslated
        End Function
        Public Shared Function translate_ColorToLeft(pt As cv.Point3f) As cv.Point2f
            Dim ptTranslated As cv.Point2f, ptTranslated3D As cv.Point3f
            ptTranslated3D.X = atask.calibData.ColorToLeft_rotation(0) * pt.X +
                               atask.calibData.ColorToLeft_rotation(1) * pt.Y +
                               atask.calibData.ColorToLeft_rotation(2) * pt.Z + atask.calibData.ColorToLeft_translation(0)
            ptTranslated3D.Y = atask.calibData.ColorToLeft_rotation(3) * pt.X +
                               atask.calibData.ColorToLeft_rotation(4) * pt.Y +
                               atask.calibData.ColorToLeft_rotation(5) * pt.Z + atask.calibData.ColorToLeft_translation(1)
            ptTranslated3D.Z = atask.calibData.ColorToLeft_rotation(6) * pt.X +
                               atask.calibData.ColorToLeft_rotation(7) * pt.Y +
                               atask.calibData.ColorToLeft_rotation(8) * pt.Z + atask.calibData.ColorToLeft_translation(2)
            ptTranslated.X = atask.calibData.leftIntrinsics.fx * ptTranslated3D.X / ptTranslated3D.Z + atask.calibData.leftIntrinsics.ppx
            ptTranslated.Y = atask.calibData.leftIntrinsics.fy * ptTranslated3D.Y / ptTranslated3D.Z + atask.calibData.leftIntrinsics.ppy

            Return ptTranslated
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                dst2 = atask.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                Dim vec = New cv.Vec3b(0, 255, 255) ' yellow
                For Each gr In atask.bricks.brickList
                    If gr.depth > 0 Then DrawCircle(dst2, gr.rect.TopLeft)
                Next
            End If
        End Sub
    End Class




    Public Class Intrinsics_TranslateRGBtoLeft : Inherits TaskParent
        Public Sub New()
            atask.gOptions.highlight.SelectedItem = "Red"
            atask.gOptions.LineWidth.Value += 1
            labels(3) = "The left image with the lines that can be translated on both ends."
            desc = "Translate a point from the RGB image to the left image.  Test with the longest line as input."
        End Sub
        '---------------------------------------------
        ' Map a pixel from RGB image into Left IR image
        '---------------------------------------------
        Public Shared Function MapRgbToLeftIr(uRgb As Single, vRgb As Single, depth As Single) As cv.Point2f
            Dim rgbintr = atask.calibData.rgbIntrinsics
            Dim leftintr = atask.calibData.leftIntrinsics
            Dim rotation = atask.calibData.ColorToLeft_Rotation
            Dim translation = atask.calibData.ColorToLeft_Translation

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
            dst3 = atask.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR) ' so we can show the red line...
            Dim count As Integer
            If atask.Settings.cameraName.startswith("StereoLabs") Then
                For Each lp In atask.lines.lpList
                    dst2.Line(lp.p1, lp.p2, atask.highlight, atask.lineWidth, atask.lineType)
                    dst3.Line(lp.p1, lp.p2, atask.highlight, atask.lineWidth, atask.lineType)
                Next
            Else
                For Each lp In atask.lines.lpList
                    dst2.Line(lp.p1, lp.p2, atask.highlight, atask.lineWidth, atask.lineType)
                    Dim depth1 = atask.pointCloud.Get(Of cv.Point3f)(CInt(lp.p1.Y), CInt(lp.p1.X)).Z
                    Dim depth2 = atask.pointCloud.Get(Of cv.Point3f)(CInt(lp.p2.Y), CInt(lp.p2.X)).Z

                    If depth1 > 0 And depth2 > 0 Then
                        Dim p1 = MapRgbToLeftIr(lp.p1.X, lp.p1.Y, depth1)
                        Dim p2 = MapRgbToLeftIr(lp.p2.X, lp.p2.Y, depth2)
                        dst3.Line(p1, p2, atask.highlight, atask.lineWidth, atask.lineType)
                        count += 1
                    End If
                Next
            End If
            labels(2) = CStr(count) + " lines were found in the BGR image."
        End Sub
    End Class





    Public Class Intrinsics_TranslateLeftToRGB : Inherits TaskParent
        Public Sub New()
            desc = "Translate a point from the left image to the RGB image"
        End Sub

        '---------------------------------------------------------
        ' Convert a pixel from Left IR image to RGB image (VB.NET)
        '---------------------------------------------------------
        'Public Function MapLeftToRgb(uLeft As Single, vLeft As Single, depth As Single) As cv.Point2f

        '    Dim rgbintr = atask.calibData.rgbIntrinsics
        '    Dim leftintr = atask.calibData.leftIntrinsics
        '    Dim rotation = atask.calibData.ColorToLeft_Rotation
        '    Dim translation = atask.calibData.ColorToLeft_Translation

        '    '-----------------------------------------
        '    ' 1. Unproject Left IR pixel into 3D point
        '    '-----------------------------------------
        '    Dim Xleft As Single = (uLeft - leftintr.ppx) * depth / leftintr.fx
        '    Dim Yleft As Single = (vLeft - leftintr.ppy) * depth / leftintr.fy
        '    Dim Zleft As Single = depth

        '    Dim Pleft() As Single = {Xleft, Yleft, Zleft}

        '    '-----------------------------------------------------
        '    ' 2. Transform Left IR 3D point → RGB camera 3D point
        '    '-----------------------------------------------------
        '    Dim Prgb(2) As Single

        '    For i = 0 To 2
        '        Prgb(i) =
        '    extrLeftToRgb.rotation(i * 3 + 0) * Pleft(0) +
        '    extrLeftToRgb.rotation(i * 3 + 1) * Pleft(1) +
        '    extrLeftToRgb.rotation(i * 3 + 2) * Pleft(2) +
        '    extrLeftToRgb.translation(i)
        '    Next

        '    Dim Xrgb As Single = Prgb(0)
        '    Dim Yrgb As Single = Prgb(1)
        '    Dim Zrgb As Single = Prgb(2)

        '    '-----------------------------------------
        '    ' 3. Project RGB 3D point → RGB pixel
        '    '-----------------------------------------
        '    Dim uRgb As Single = rgbintr.fx * (Xrgb / Zrgb) + rgbintr.ppx
        '    Dim vRgb As Single = rgbintr.fy * (Yrgb / Zrgb) + rgbintr.ppy

        '    Return New cv.Point2f(uRgb, vRgb)
        'End Function

        'Public Function InvertExtrinsicsOpenCV(src As Single())
        ''-----------------------------------------
        '' Convert rotation[] → 3×3 Mat
        ''-----------------------------------------
        'Dim R = cv.Mat.FromPixelData(3, 3, MatType.CV_32F, atask.calibData.ColorToLeft_Rotation)

        '' Convert translation[] → 3×1 Mat
        'Dim T = cv.Mat.FromPixelData(3, 1, MatType.CV_32F, atask.calibData.ColorToLeft_Translation)

        ''-----------------------------------------
        '' 1. Invert rotation: R_inv = R^T
        ''-----------------------------------------
        'Dim Rinv As New Mat()
        'Cv2.Transpose(R, Rinv)

        ''-----------------------------------------
        '' 2. Invert translation: T_inv = -R^T * T
        ''-----------------------------------------
        'Dim Tinv As Mat = -(Rinv * T)

        ''-----------------------------------------
        '' Pack back into rs2_extrinsics
        ''-----------------------------------------
        'Dim inv As New rs2_extrinsics()
        'inv.rotation = New Single(8) {}
        'inv.translation = New Single(2) {}

        '' Copy rotation
        'For R = 0 To 2
        '    For c = 0 To 2
        '        inv.rotation(R * 3 + c) = Rinv.Get(Of Single)(R, c)
        '    Next
        'Next

        '' Copy translation
        'For i = 0 To 2
        '    inv.translation(i) = Tinv.Get(Of Single)(i, 0)
        'Next

        'Return inv
        'End Function


        Public Overrides Sub RunAlg(src As cv.Mat)
            'dst2 = src
            'dst3 = atask.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR) ' so we can show the red line...
            'Dim count As Integer
            'For Each lp In atask.lines.lpList
            '    dst2.Line(lp.p1, lp.p2, atask.highlight, atask.lineWidth, atask.lineType)
            '    Dim depth1 = atask.pointCloud.Get(Of cv.Point3f)(CInt(lp.p1.Y), CInt(lp.p1.X)).Z
            '    Dim depth2 = atask.pointCloud.Get(Of cv.Point3f)(CInt(lp.p2.Y), CInt(lp.p2.X)).Z

            '    If depth1 > 0 And depth2 > 0 Then
            '        Dim p1 = MapRgbToLeftIr(lp.p1.X, lp.p1.Y, depth1)
            '        Dim p2 = MapRgbToLeftIr(lp.p2.X, lp.p2.Y, depth2)
            '        dst3.Line(p1, p2, atask.highlight, atask.lineWidth, atask.lineType)
            '        count += 1
            '    End If
            'Next
            'labels(2) = CStr(count) + " lines had depth for both ends and could be translated from BGR to left."
        End Sub
    End Class





    Public Class Intrinsics_MapLeftToRight : Inherits TaskParent
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            desc = "Map a point from the left image to the right image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src
            dst3 = atask.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR) ' so we can show the red line...
            Dim count As Integer
            If atask.Settings.cameraName.StartsWith("StereoLabs") Then
                For Each lp In atask.lines.lpList
                    Dim brick1 = atask.bricks.brickList(lp.p1GridIndex)
                    Dim brick2 = atask.bricks.brickList(lp.p2GridIndex)
                    Dim p1 = lp.p1 ' avoid updating list of lines.
                    Dim p2 = lp.p2
                    If brick1.depth > 0 And brick2.depth > 0 Then
                        p1.X -= atask.calibData.baseline * atask.calibData.leftIntrinsics.fx / brick1.mmDepth.minVal
                        p2.X -= atask.calibData.baseline * atask.calibData.leftIntrinsics.fx / brick2.mmDepth.minVal
                        dst2.Line(lp.p1, lp.p2, lp.color, atask.lineWidth + 1, atask.lineType)
                        dst3.Line(p1, p2, lp.color, atask.lineWidth + 1, atask.lineType)
                    Else
                        count += 1
                    End If
                Next
            Else
                'For Each lp In atask.lines.lpList
                '    dst2.Line(lp.p1, lp.p2, atask.highlight, atask.lineWidth, atask.lineType)
                '    Dim depth1 = atask.pointCloud.Get(Of cv.Point3f)(CInt(lp.p1.Y), CInt(lp.p1.X)).Z
                '    Dim depth2 = atask.pointCloud.Get(Of cv.Point3f)(CInt(lp.p2.Y), CInt(lp.p2.X)).Z

                '    If depth1 > 0 And depth2 > 0 Then
                '        Dim p1 = MapRgbToLeftIr(lp.p1.X, lp.p1.Y, depth1)
                '        Dim p2 = MapRgbToLeftIr(lp.p2.X, lp.p2.Y, depth2)
                '        dst3.Line(p1, p2, atask.highlight, atask.lineWidth, atask.lineType)
                '        count += 1
                '    End If
                'Next
            End If
            labels(3) = CStr(count) + " were missing depth and could not be mapped into the right image."
        End Sub
    End Class

End Namespace