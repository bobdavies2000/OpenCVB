Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Intrinsics_Basics : Inherits TaskParent
        Public Sub New()
            If standalone Then If algTask.bricks Is Nothing Then algTask.bricks = New Brick_Basics
            If standalone Then algTask.gOptions.gravityPointCloud.Checked = False
            desc = "Some cameras don't provide aligned color and left images.  This algorithm tries to align the left and color image."
        End Sub
        Public Shared Function translate_LeftToRight(pt As cv.Point3f) As cv.Point2f
            Dim ptTranslated As cv.Point2f, ptTranslated3D As cv.Point3f
            ptTranslated3D.X = algTask.calibData.LtoR_rotation(0) * pt.X +
                               algTask.calibData.LtoR_rotation(1) * pt.Y +
                               algTask.calibData.LtoR_rotation(2) * pt.Z + algTask.calibData.LtoR_translation(0)
            ptTranslated3D.Y = algTask.calibData.LtoR_rotation(3) * pt.X +
                               algTask.calibData.LtoR_rotation(4) * pt.Y +
                               algTask.calibData.LtoR_rotation(5) * pt.Z + algTask.calibData.LtoR_translation(1)
            ptTranslated3D.Z = algTask.calibData.LtoR_rotation(6) * pt.X +
                               algTask.calibData.LtoR_rotation(7) * pt.Y +
                               algTask.calibData.LtoR_rotation(8) * pt.Z + algTask.calibData.LtoR_translation(2)
            ptTranslated.X = algTask.calibData.rgbIntrinsics.fx * ptTranslated3D.X / ptTranslated3D.Z + algTask.calibData.rgbIntrinsics.ppx
            ptTranslated.Y = algTask.calibData.rgbIntrinsics.fy * ptTranslated3D.Y / ptTranslated3D.Z + algTask.calibData.rgbIntrinsics.ppy

            Return ptTranslated
        End Function
        Public Shared Function translate_ColorToLeft(pt As cv.Point3f) As cv.Point2f
            Dim ptTranslated As cv.Point2f, ptTranslated3D As cv.Point3f
            ptTranslated3D.X = algTask.calibData.ColorToLeft_rotation(0) * pt.X +
                               algTask.calibData.ColorToLeft_rotation(1) * pt.Y +
                               algTask.calibData.ColorToLeft_rotation(2) * pt.Z + algTask.calibData.ColorToLeft_translation(0)
            ptTranslated3D.Y = algTask.calibData.ColorToLeft_rotation(3) * pt.X +
                               algTask.calibData.ColorToLeft_rotation(4) * pt.Y +
                               algTask.calibData.ColorToLeft_rotation(5) * pt.Z + algTask.calibData.ColorToLeft_translation(1)
            ptTranslated3D.Z = algTask.calibData.ColorToLeft_rotation(6) * pt.X +
                               algTask.calibData.ColorToLeft_rotation(7) * pt.Y +
                               algTask.calibData.ColorToLeft_rotation(8) * pt.Z + algTask.calibData.ColorToLeft_translation(2)
            ptTranslated.X = algTask.calibData.leftIntrinsics.fx * ptTranslated3D.X / ptTranslated3D.Z + algTask.calibData.leftIntrinsics.ppx
            ptTranslated.Y = algTask.calibData.leftIntrinsics.fy * ptTranslated3D.Y / ptTranslated3D.Z + algTask.calibData.leftIntrinsics.ppy

            Return ptTranslated
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                dst2 = algTask.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                Dim vec = New cv.Vec3b(0, 255, 255) ' yellow
                If algTask.rgbLeftAligned Then
                    For Each brick In algTask.bricks.brickList
                        If brick.depth > 0 Then DrawCircle(dst2, brick.rect.TopLeft)
                    Next
                Else
                    For Each brick In algTask.bricks.brickList
                        Dim pt = translate_ColorToLeft(algTask.pointCloud.Get(Of cv.Point3f)(brick.rect.Y, brick.rect.X))
                        If Single.IsNaN(pt.X) Or Single.IsNaN(pt.Y) Then Continue For
                        If Single.IsInfinity(pt.X) Or Single.IsInfinity(pt.Y) Then Continue For
                        pt = lpData.validatePoint(pt)
                        DrawCircle(dst2, pt)
                    Next
                End If
            End If
        End Sub
    End Class
End Namespace