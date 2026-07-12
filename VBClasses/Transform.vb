Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class XR_Transform_Resize : Inherits TaskParent
    Dim options As New Options_Transform
    Public Sub New()
        desc = "Resize an image based on the slider value."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim w = CInt(options.resizeFactor * src.Width)
        Dim h = CInt(options.resizeFactor * src.Height)
        If options.resizeFactor > 1 Then
            Dim tmp As New Mat
            Resize(src, tmp, New Size(w, h), 0)
            Dim roi = New cv.Rect((w - src.Width) / 2, (h - src.Height) / 2, src.Width, src.Height)
            tmp(roi).CopyTo(dst2)
        Else
            dst2.SetTo(0)
            Dim roi = New cv.Rect((src.Width - w) / 2, (src.Height - h) / 2, w, h)
            Resize(src, dst2(roi), New Size(w, h), 0)
        End If
    End Sub
End Class





Public Class XR_Transform_Affine3D : Inherits TaskParent
    Dim pc1 As Mat
    Dim pc2 As Mat
    Dim affineTransform As Mat
    Dim options As New Options_Transform
    Public Sub New()
        desc = "Using 2 cv.Point clouds compute the 3D affine transform between them"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim output = "Use the check boxes to snapshot the different cv.Point clouds" + vbCrLf

        If task.testAllRunning Then
            If task.frameCount = 30 Then options.firstCheck = True
            If task.frameCount = 60 Then options.secondCheck = True
        End If

        If options.firstCheck Then
            pc1 = task.pointCloud.Clone()
            options.firstCheck = False
            output += "First cv.Point cloud captured" + vbCrLf
        End If

        If options.secondCheck Then
            pc2 = task.pointCloud.Clone()
            options.secondCheck = False
            output += "Second cv.Point cloud captured" + vbCrLf
        End If

        If pc1 IsNot Nothing Then
            If pc2 IsNot Nothing Then
                Dim inliers = New Mat
                affineTransform = New Mat(3, 4, MatType.CV_64F)
                pc1 = pc1.Reshape(3, pc1.Rows * pc1.Cols)
                pc2 = pc2.Reshape(3, pc2.Rows * pc2.Cols)
                EstimateAffine3D(pc1, pc2, affineTransform, inliers)
                pc1 = Nothing
                pc2 = Nothing
            End If
        End If

        If affineTransform IsNot Nothing Then
            output += "Affine Transform 3D results:" + vbCrLf
            For i = 0 To 3 - 1
                For j = 0 To 4 - 1
                    output += affineTransform.Get(Of Double)(i, j).ToString(fmt3) + vbTab
                Next
                output += vbCrLf
            Next
            output += "0" + vbTab + "0" + vbTab + "0" + vbTab + "1" + vbCrLf
        End If
        SetTrueText(output)
    End Sub
End Class









Public Class XR_Transform_Rotate : Inherits TaskParent
    Public imageCenter As Point2f
    Dim options As New Options_Transform
    Public Sub New()
        desc = "Rotate and scale and image based on the slider values."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        imageCenter = New Point2f(options.centerX, options.centerY)
        Dim rotationMat = GetRotationMatrix2D(imageCenter, options.angle, options.scale)
        WarpAffine(src, dst2, rotationMat, New Size())
        Circle(dst2, imageCenter, task.DotSize * 2, Scalar.Yellow, -1, task.lineType)
        Circle(dst2, imageCenter, task.DotSize, Scalar.Blue, -1, task.lineType)
    End Sub
End Class
