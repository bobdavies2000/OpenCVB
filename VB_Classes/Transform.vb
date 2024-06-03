Imports cv = OpenCvSharp
Public Class Transform_Resize : Inherits VB_Parent
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Resize Percent", 50, 1000, 50)
        desc = "Resize an image based on the slider value."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static percentSlider = findSlider("Resize Percent")
        Dim resizeFactor = percentSlider.Value / 100
        Dim w = CInt(resizeFactor * src.Width)
        Dim h = CInt(resizeFactor * src.Height)
        If resizeFactor > 1 Then
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





Public Class Transform_Affine3D : Inherits VB_Parent
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Check to snap the first point cloud")
            check.addCheckBox("Check to snap the second point cloud")
        End If
        desc = "Using 2 point clouds compute the 3D affine transform between them"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static firstCheck = findCheckBox("Check to snap the first point cloud")
        Static secondCheck = findCheckBox("Check to snap the second point cloud")
        Dim output = "Use the check boxes to snapshot the different point clouds" + vbCrLf
        Static pc1 As cv.Mat
        Static pc2 As cv.Mat
        Static affineTransform As cv.Mat

        If task.testAllRunning Then
            If task.frameCount = 30 Then firstCheck.Checked = True
            If task.frameCount = 60 Then secondCheck.Checked = True
        End If

        If firstCheck.Checked Then
            pc1 = task.pointCloud.Clone()
            firstCheck.Checked = False
            output += "First point cloud captured" + vbCrLf
        End If

        If secondCheck.Checked Then
            pc2 = task.pointCloud.Clone()
            secondCheck.Checked = False
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
        setTrueText(output)
    End Sub
End Class









Public Class Transform_Rotate : Inherits VB_Parent
    Public imageCenter As cv.Point2f
    Public angleSlider As Windows.Forms.TrackBar
    Public scaleSlider As Windows.Forms.TrackBar
    Public centerXSlider As Windows.Forms.TrackBar
    Public centerYSlider As Windows.Forms.TrackBar
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Angle", -180, 180, 30)
            sliders.setupTrackBar("Scale Factor% (100% means no scaling)", 1, 100, 100)
            sliders.setupTrackBar("Rotation center X", 1, dst2.Width, dst2.Width / 2)
            sliders.setupTrackBar("Rotation center Y", 1, dst2.Height, dst2.Height / 2)
        End If
        angleSlider = findSlider("Angle")
        scaleSlider = findSlider("Scale Factor% (100% means no scaling)")
        centerXSlider = findSlider("Rotation center X")
        centerYSlider = findSlider("Rotation center Y")
        desc = "Rotate and scale and image based on the slider values."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        imageCenter = New cv.Point2f(centerXSlider.Value, centerYSlider.Value)
        Dim rotationMat = cv.Cv2.GetRotationMatrix2D(imageCenter, angleSlider.Value, scaleSlider.Value / 100)
        cv.Cv2.WarpAffine(src, dst2, rotationMat, New cv.Size())
        dst2.Circle(imageCenter, task.dotSize * 2, cv.Scalar.Yellow, -1, task.lineType)
        dst2.Circle(imageCenter, task.dotSize, cv.Scalar.Blue, -1, task.lineType)
    End Sub
End Class