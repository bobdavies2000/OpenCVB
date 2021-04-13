Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Transform_Resize
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Resize Percent", 50, 1000, 50)
        End If
        task.desc = "Resize an image based on the slider value."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim resizeFactor = sliders.trackbar(0).Value / 100
        Dim w = CInt(resizeFactor * src.Width)
        Dim h = CInt(resizeFactor * src.Height)
        If resizeFactor > 1 Then
            Dim tmp As New cv.Mat
            tmp = src.Resize(New cv.Size(w, h), 0)
            Dim roi = New cv.Rect((w - src.Width) / 2, (h - src.Height) / 2, src.Width, src.Height)
            tmp(roi).CopyTo(dst1)
        Else
            Dim roi = New cv.Rect((src.Width - w) / 2, (src.Height - h) / 2, w, h)
            dst1(roi) = src.Resize(New cv.Size(w, h), 0)
        End If
    End Sub
End Class




Public Class Transform_Rotate
    Inherits VBparent
    Public imageCenter As cv.Point2f
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Angle", -180, 180, 30)
            sliders.setupTrackBar(1, "Scale Factor% (100% means no scaling)", 1, 100, 100)
            sliders.setupTrackBar(2, "Rotation center X", 1, dst1.Width, dst1.Width / 2)
            sliders.setupTrackBar(3, "Rotation center Y", 1, dst1.Height, dst1.Height / 2)
        End If
        task.desc = "Rotate and scale and image based on the slider values."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        imageCenter = New cv.Point2f(sliders.trackbar(2).Value, sliders.trackbar(3).Value)
        Dim rotationMat = cv.Cv2.GetRotationMatrix2D(imageCenter, sliders.trackbar(0).Value, sliders.trackbar(1).Value / 100)
        cv.Cv2.WarpAffine(src, dst1, rotationMat, New cv.Size())
        dst1.Circle(imageCenter, task.dotSize * 2, cv.Scalar.Yellow, -1, task.lineType)
        dst1.Circle(imageCenter, task.dotSize, cv.Scalar.Blue, -1, task.lineType)
    End Sub
End Class



Public Class Transform_Sort
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 4)
            radio.check(0).Text = "Ascending"
            radio.check(0).Checked = True
            radio.check(1).Text = "Descending"
            radio.check(2).Text = "EveryColumn"
            radio.check(3).Text = "EveryRow"
        End If
        task.desc = "Sort the pixels of a grayscale image."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim sortOption = cv.SortFlags.Ascending
        If radio.check(1).Checked Then sortOption = cv.SortFlags.Descending
        If radio.check(2).Checked Then sortOption = cv.SortFlags.EveryColumn
        If radio.check(3).Checked Then sortOption = cv.SortFlags.EveryRow
        dst1 = src.Sort(sortOption + cv.SortFlags.EveryColumn)
    End Sub
End Class






Public Class Transform_SortReshape
    Inherits VBparent
    Public sortVector As cv.Mat
    Public Sub New()
        initParent()
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 2)
            radio.check(0).Text = "Ascending"
            radio.check(0).Checked = True
            radio.check(1).Text = "Descending"
        End If
        task.desc = "Sort the pixels of a grayscale image."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim sortOption = cv.SortFlags.Ascending
        If radio.check(1).Checked Then sortOption = cv.SortFlags.Descending
        Dim tmp = src.Reshape(1, src.Rows * src.Cols)
        sortVector = tmp.Sort(sortOption + cv.SortFlags.EveryColumn)
        dst1 = sortVector.Reshape(1, src.Rows)
    End Sub
End Class





Public Class Transform_Affine3D
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 2)
            check.Box(0).Text = "Check to snap the first point cloud"
            check.Box(1).Text = "Check to snap the second point cloud"
        End If
        task.desc = "Using 2 point clouds compute the 3D affine transform between them"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim output = "Use the check boxes to snapshot the different point clouds" + vbCrLf
        Static pc1 As cv.Mat
        Static pc2 As cv.Mat
        Static affineTransform As cv.Mat

        If task.parms.testAllRunning Then
            If task.frameCount = 30 Then check.Box(0).Checked = True
            If task.frameCount = 60 Then check.Box(1).Checked = True
        End If

        If check.Box(0).Checked Then
            pc1 = task.pointCloud.Clone()
            check.Box(0).Checked = False
            output += "First point cloud captured" + vbCrLf
            affineTransform = Nothing
        End If

        If check.Box(1).Checked Then
            pc2 = task.pointCloud.Clone()
            check.Box(1).Checked = False
            output += "Second point cloud captured" + vbCrLf
            affineTransform = Nothing
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
                    output += Format(affineTransform.Get(Of Double)(i, j), "0.000") + vbTab
                Next
                output += vbCrLf
            Next
            output += "0" + vbTab + "0" + vbTab + "0" + vbTab + "1" + vbCrLf
        End If
        task.trueText(output)
    End Sub
End Class




