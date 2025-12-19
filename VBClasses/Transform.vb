Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Transform_Resize : Inherits TaskParent
        Dim options As New Options_Transform
        Public Sub New()
            desc = "Resize an image based on the slider value."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim w = CInt(options.resizeFactor * src.Width)
            Dim h = CInt(options.resizeFactor * src.Height)
            If options.resizeFactor > 1 Then
                Dim tmp As New cv.Mat
                tmp = src.Resize(New cv.Size(w, h), 0)
                Dim roi = New cv.Rect((w - src.Width) / 2, (h - src.Height) / 2, src.Width, src.Height)
                tmp(roi).CopyTo(dst2)
            Else
                dst2.SetTo(0)
                Dim roi = New cv.Rect((src.Width - w) / 2, (src.Height - h) / 2, w, h)
                dst2(roi) = src.Resize(New cv.Size(w, h), 0)
            End If
        End Sub
    End Class





    Public Class Transform_Affine3D : Inherits TaskParent
        Dim pc1 As cv.Mat
        Dim pc2 As cv.Mat
        Dim affineTransform As cv.Mat
        Dim options As New Options_Transform
        Public Sub New()
            desc = "Using 2 point clouds compute the 3D affine transform between them"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim output = "Use the check boxes to snapshot the different point clouds" + vbCrLf

            If taskAlg.testAllRunning Then
                If taskAlg.frameCount = 30 Then options.firstCheck = True
                If taskAlg.frameCount = 60 Then options.secondCheck = True
            End If

            If options.firstCheck Then
                pc1 = taskAlg.pointCloud.Clone()
                options.firstCheck = False
                output += "First point cloud captured" + vbCrLf
            End If

            If options.secondCheck Then
                pc2 = taskAlg.pointCloud.Clone()
                options.secondCheck = False
                output += "Second point cloud captured" + vbCrLf
            End If

            If pc1 IsNot Nothing Then
                If pc2 IsNot Nothing Then
                    Dim inliers = New cv.Mat
                    affineTransform = New cv.Mat(3, 4, cv.MatType.CV_64F)
                    pc1 = pc1.Reshape(3, pc1.Rows * pc1.Cols)
                    pc2 = pc2.Reshape(3, pc2.Rows * pc2.Cols)
                    cv.Cv2.EstimateAffine3D(pc1, pc2, affineTransform, inliers)
                    pc1 = Nothing
                    pc2 = Nothing
                End If
            End If

            If affineTransform IsNot Nothing Then
                output += "Affine Transform 3D results:" + vbCrLf
                For i = 0 To 3 - 1
                    For j = 0 To 4 - 1
                        output += Format(affineTransform.Get(Of Double)(i, j), fmt3) + vbTab
                    Next
                    output += vbCrLf
                Next
                output += "0" + vbTab + "0" + vbTab + "0" + vbTab + "1" + vbCrLf
            End If
            SetTrueText(output)
        End Sub
    End Class









    Public Class Transform_Rotate : Inherits TaskParent
        Public imageCenter As cv.Point2f
        Dim options As New Options_Transform
        Public Sub New()
            desc = "Rotate and scale and image based on the slider values."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            imageCenter = New cv.Point2f(options.centerX, options.centerY)
            Dim rotationMat = cv.Cv2.GetRotationMatrix2D(imageCenter, options.angle, options.scale)
            cv.Cv2.WarpAffine(src, dst2, rotationMat, New cv.Size())
            DrawCircle(dst2, imageCenter, taskAlg.DotSize * 2, cv.Scalar.Yellow)
            DrawCircle(dst2, imageCenter, taskAlg.DotSize, cv.Scalar.Blue)
        End Sub
    End Class
End Namespace