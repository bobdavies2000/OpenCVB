Imports cv = OpenCvSharp
Public Class LeftRight_Basics : Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Brightness Alpha (contrast)", 0, 10000, 5000)
            sliders.setupTrackBar(1, "Brightness Beta (brightness)", -255, 255, -100)
        End If
        If task.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.D435i Then findSlider("Brightness Alpha (contrast)").Value = 1500
        label2 = If(task.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam, "No right image", "Right Image")
        task.desc = "Enhance the left/right views with brightness and contrast."
    End Sub
    Public Sub Run(src As cv.Mat)
        Static betaSlider = findSlider("Brightness Beta (brightness)")
        Static alphaSlider = findSlider("Brightness Alpha (contrast)")
        dst1 = (task.leftView * cv.Scalar.All(alphaSlider.Value / 500) + betaSlider.Value).ToMat
        dst2 = (task.rightView * cv.Scalar.All(alphaSlider.Value / 500) + betaSlider.Value).ToMat
    End Sub
End Class







Public Class LeftRight_CompareRaw : Inherits VBparent
    Dim lrView As New LeftRight_Basics
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Slice Starting Y", 0, 300, 100)
            sliders.setupTrackBar(1, "Slice Height", 1, (dst1.Rows - 100) / 2, 30)
        End If

        label1 = lrView.label1
        label2 = lrView.label2
        task.desc = "Show slices of the left and right view next to each other for visual comparison"
    End Sub
    Public Sub Run(src As cv.Mat)
        Static startYSlider = findSlider("Slice Starting Y")
        Static hSlider = findSlider("Slice Height")
        lrView.Run(src)

        dst1 = New cv.Mat(dst1.Rows, dst1.Cols, cv.MatType.CV_8U, 0)

        Dim sliceY = startYSlider.Value
        Dim slideHeight = hSlider.Value
        Dim r1 = New cv.Rect(0, sliceY, lrView.dst1.Width, slideHeight)
        Dim r2 = New cv.Rect(0, 100, lrView.dst1.Width, slideHeight)
        lrView.dst1(r1).CopyTo(dst1(r2))

        r2.Y += slideHeight
        lrView.dst2(r1).CopyTo(dst1(r2))
        dst2 = lrView.dst2
    End Sub
End Class





Public Class LeftRight_Features : Inherits VBparent
    Dim lrView As New LeftRight_Basics
    Dim features As New Features_GoodFeatures
    Public Sub New()
        task.desc = "Find GoodFeatures in the left and right depalettized infrared images"
        label1 = "Left Image"
        label2 = "Right Image"
    End Sub
    Public Sub Run(src As cv.Mat)
        lrView.Run(src)

        features.Run(lrView.dst2)
        lrView.dst2.CopyTo(dst2)
        For i = 0 To features.goodFeatures.Count - 1
            cv.Cv2.Circle(dst2, features.goodFeatures(i), 3, cv.Scalar.White, -1, task.lineType)
        Next

        features.Run(lrView.dst1)
        lrView.dst1.CopyTo(dst1)
        For i = 0 To features.goodFeatures.Count - 1
            cv.Cv2.Circle(dst1, features.goodFeatures(i), 3, cv.Scalar.White, -1, task.lineType)
        Next
    End Sub
End Class




Public Class LeftRight_Palettized : Inherits VBparent
    Dim lrView As New LeftRight_Basics
    Public Sub New()
        task.desc = "Add color to the 8-bit infrared images."
        label1 = "Left Image"
        label2 = "Right Image"
    End Sub
    Public Sub Run(src As cv.Mat)
        lrView.Run(src)

        task.palette.Run(lrView.dst1)
        dst1 = task.palette.dst1

        task.palette.Run(lrView.dst2)
        dst2 = task.palette.dst1
    End Sub
End Class




Public Class LeftRight_BRISK : Inherits VBparent
    Dim lrView As New LeftRight_Basics
    Dim brisk As New BRISK_Basics
    Public Sub New()
        brisk.sliders.trackbar(0).Value = 20
        label1 = "Infrared Left Image"
        label2 = "Infrared Right Image"
        task.desc = "Add color to the 8-bit infrared images."
    End Sub
    Public Sub Run(src as cv.Mat)
        lrView.Run(src)
        brisk.Run(lrView.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
        dst2 = brisk.dst1

        brisk.Run(lrView.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
        dst1 = brisk.dst1
    End Sub
End Class