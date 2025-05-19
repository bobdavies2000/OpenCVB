Imports cv = OpenCvSharp
Public Class Intrinsics_Basics : Inherits TaskParent
    Public Sub New()
        desc = "Some cameras don't provide aligned color and left images.  This algorithm tries to align the left and color image."
    End Sub
    Public Shared Function translatePixel(pt As cv.Point3f) As cv.Point2f
        Dim ptTranslated As cv.Point2f, ptTranslated3D As cv.Point3f
        If task.calibData.translation Is Nothing Then
            ptTranslated3D = pt ' no translation or rotation - they are likely the same camera...
        Else
            ptTranslated3D.X = task.calibData.rotation(0) * pt.X +
                               task.calibData.rotation(1) * pt.Y +
                               task.calibData.rotation(2) * pt.Z + task.calibData.translation(0)
            ptTranslated3D.Y = task.calibData.rotation(3) * pt.X +
                               task.calibData.rotation(4) * pt.Y +
                               task.calibData.rotation(5) * pt.Z + task.calibData.translation(1)
            ptTranslated3D.Z = task.calibData.rotation(6) * pt.X +
                               task.calibData.rotation(7) * pt.Y +
                               task.calibData.rotation(8) * pt.Z + task.calibData.translation(2)
            ptTranslated.X = task.calibData.rgbIntrinsics.fx * ptTranslated3D.X / ptTranslated3D.Z + task.calibData.rgbIntrinsics.ppx
            ptTranslated.Y = task.calibData.rgbIntrinsics.fy * ptTranslated3D.Y / ptTranslated3D.Z + task.calibData.rgbIntrinsics.ppy
        End If

        Return ptTranslated
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            Dim vec = New cv.Vec3b(0, 255, 255) ' yellow
            If task.rgbLeftAligned Then
                For Each brick In task.brickList
                    dst2.Circle(brick.rect.TopLeft, task.DotSize, task.highlight, -1)
                Next
            Else
                For Each brick In task.brickList
                    Dim pt = translatePixel(task.pointCloud.Get(Of cv.Point3f)(brick.rect.Y, brick.rect.X))
                    If Single.IsNaN(pt.X) Or Single.IsNaN(pt.Y) Then Continue For
                    If Single.IsInfinity(pt.X) Or Single.IsInfinity(pt.Y) Then Continue For
                    pt = validatePoint(pt)
                    dst2.Circle(pt, task.DotSize, task.highlight, -1)
                Next
            End If
        End If
    End Sub
End Class







Public Class Intrinsics_PixelByPixel : Inherits TaskParent
    Public rect As cv.Rect
    Public Sub New()
        desc = "Translate each pixel location from the color image to the left image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView.Clone
        If task.frameCount > 10 Then
            dst2.Rectangle(rect, 0, task.lineWidth, task.lineType)
            Exit Sub
        End If

        Dim minX As Single = Single.MaxValue, maxX As Single = Single.MinValue, minY As Single = Single.MaxValue, maxY As Single = Single.MinValue
        Dim cMinX As Integer, cMaxX As Integer, cMinY As Integer, cMaxY As Integer

        For x = 0 To dst2.Width - 1
            For y = 0 To dst2.Height - 1
                Dim pc = task.pointCloud.Col(0).Get(Of cv.Point3f)(y, x)
                If pc.Z > 0 Then
                    Dim pt As cv.Point2f = Intrinsics_Basics.translatePixel(pc)
                    If minX > pt.X Then
                        minX = pt.X
                        cMinX = x
                    End If
                    If maxX < pt.X Then
                        maxX = pt.X
                        cMaxX = x
                    End If

                    If minY > pt.Y Then
                        minY = pt.Y
                        cMinY = y
                    End If
                    If maxY < pt.Y Then
                        maxY = pt.Y
                        cMaxY = y
                    End If
                End If
            Next
        Next

        strOut = "Min max values..." + vbCrLf + vbCrLf
        strOut += "Min X Color = " + CStr(cMinX) + " with Min X Left = " + Format(minX, fmt1) + vbCrLf
        strOut += "Min Y Color = " + CStr(cMinY) + " with Min Y Left = " + Format(minY, fmt1) + vbCrLf
        strOut += "Max X Color = " + CStr(cMaxX) + " with Max X Left = " + Format(maxX, fmt1) + vbCrLf
        strOut += "Max Y Color = " + CStr(cMaxY) + " with Max Y Left = " + Format(maxY, fmt1) + vbCrLf

        Dim delta = (maxX - minX) / (dst2.Width) ' pixel dimensions are the same in x and y for both cameras.

        rect = New cv.Rect(CInt(minX), CInt(minY), delta * dst2.Width, delta * dst2.Height)
        dst2.Rectangle(rect, 0, task.lineWidth, task.lineType)
        SetTrueText(strOut, 3)
    End Sub
End Class
