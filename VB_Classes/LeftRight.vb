Imports cv = OpenCvSharp
Public Class LeftRight_Basics : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            Dim kinect = task.parms.cameraName = ActiveTask.algParms.camNames.Kinect4AzureCam
            sliders.setupTrackBar(0, "Brightness Alpha (contrast)", 0, 10000, If(kinect, 600, 2000))
            sliders.setupTrackBar(1, "Brightness Beta (brightness)", -255, 255, If(kinect, 0, -100))
        End If
        If task.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.D435i Then findSlider("Brightness Alpha (contrast)").Value = 1500
        labels(3) = If(task.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam, "No right image", "Right Image")
        task.desc = "Enhance the left/right views with brightness and contrast."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static betaSlider = findSlider("Brightness Beta (brightness)")
        Static alphaSlider = findSlider("Brightness Alpha (contrast)")
        dst2 = (task.leftView * cv.Scalar.All(alphaSlider.Value / 500) + betaSlider.Value).ToMat
        dst3 = (task.rightView * cv.Scalar.All(alphaSlider.Value / 500) + betaSlider.Value).ToMat
    End Sub
End Class







Public Class LeftRight_CompareRaw : Inherits VBparent
    Dim lrView As New LeftRight_Basics
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Slice Starting Y", 0, 300, 100)
            sliders.setupTrackBar(1, "Slice Height", 1, (dst2.Rows - 100) / 2, 30)
        End If

        labels(2) = lrView.labels(2)
        labels(3) = lrView.labels(3)
        task.desc = "Show slices of the left and right view next to each other for visual comparison"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static startYSlider = findSlider("Slice Starting Y")
        Static hSlider = findSlider("Slice Height")
        lrView.Run(src)

        dst2 = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_8U, 0)

        Dim sliceY = startYSlider.Value
        Dim slideHeight = hSlider.Value
        Dim r1 = New cv.Rect(0, sliceY, lrView.dst2.Width, slideHeight)
        Dim r2 = New cv.Rect(0, 100, lrView.dst2.Width, slideHeight)
        lrView.dst2(r1).CopyTo(dst2(r2))

        r2.Y += slideHeight
        lrView.dst3(r1).CopyTo(dst2(r2))
        dst3 = lrView.dst3
    End Sub
End Class





Public Class LeftRight_Features : Inherits VBparent
    Dim lrView As New LeftRight_Basics
    Dim features As New Features_GoodFeatures
    Public Sub New()
        task.desc = "Find GoodFeatures in the left and right depalettized infrared images"
        labels(2) = "Left Image"
        labels(3) = "Right Image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        lrView.Run(src)

        features.Run(lrView.dst3)
        lrView.dst3.CopyTo(dst3)
        For i = 0 To features.goodFeatures.Count - 1
            dst3.Circle(features.goodFeatures(i), task.dotSize + 1, cv.Scalar.White, -1, task.lineType)
        Next

        features.Run(lrView.dst2)
        lrView.dst2.CopyTo(dst2)
        For i = 0 To features.goodFeatures.Count - 1
            dst2.Circle(features.goodFeatures(i), task.dotSize + 1, cv.Scalar.White, -1, task.lineType)
        Next
    End Sub
End Class




Public Class LeftRight_Palettized : Inherits VBparent
    Dim lrView As New LeftRight_Basics
    Public Sub New()
        task.desc = "Add color to the 8-bit infrared images."
        labels(2) = "Left Image"
        labels(3) = "Right Image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        lrView.Run(src)

        task.palette.Run(lrView.dst2)
        dst2 = task.palette.dst2

        task.palette.Run(lrView.dst3)
        dst3 = task.palette.dst2
    End Sub
End Class




Public Class LeftRight_BRISK : Inherits VBparent
    Dim lrView As New LeftRight_Basics
    Dim brisk As New BRISK_Basics
    Public Sub New()
        brisk.sliders.trackbar(0).Value = 20
        labels(2) = "Infrared Left Image"
        labels(3) = "Infrared Right Image"
        task.desc = "Add color to the 8-bit infrared images."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        lrView.Run(src)
        brisk.Run(lrView.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
        dst3 = brisk.dst2

        brisk.Run(lrView.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
        dst2 = brisk.dst2
    End Sub
End Class