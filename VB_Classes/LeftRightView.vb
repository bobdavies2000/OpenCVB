Imports cv = OpenCvSharp
Public Class LeftRightView_Basics
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Infrared Brightness", 0, 255, 100)
        End If
        task.desc = "Show the left and right views from the 3D Camera"
        Select Case task.parms.cameraName
            Case VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam
                label1 = "Infrared Image"
                label2 = "There is only one infrared image"
                sliders.trackbar(0).Value = 0
            Case Else
                label1 = "Left Image"
                label2 = "Right Image"
        End Select
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        dst1 = task.leftView
        dst2 = task.rightView

        dst1 += sliders.trackbar(0).Value
        dst2 += sliders.trackbar(0).Value
    End Sub
End Class






Public Class LeftRightView_CompareRaw
    Inherits VBparent
    Dim lrView As LeftRightView_Basics
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Infrared Brightness", 0, 255, 100)
            sliders.setupTrackBar(1, "Slice Starting Y", 0, 300, 100)
            sliders.setupTrackBar(2, "Slice Height", 1, (src.Rows - 100) / 2, 30)
        End If

        Select Case task.parms.cameraName
            Case VB_Classes.ActiveTask.algParms.camNames.D435i, VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2,
                label1 = "Left Image"
                label2 = "Right Image"
            Case VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam
                label1 = "Infrared Image"
                label2 = "There is only one infrared image"
                sliders.trackbar(0).Value = 0
        End Select
        lrView = New LeftRightView_Basics()
        lrView.sliders.Hide()
        task.desc = "Show slices of the left and right view next to each other for visual comparison"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        lrView.Run()

        dst1 = New cv.Mat(dst1.Rows, dst1.Cols, cv.MatType.CV_8U, 0)

        Dim sliceY = sliders.trackbar(1).Value
        Dim slideHeight = sliders.trackbar(2).Value
        Dim r1 = New cv.Rect(0, sliceY, lrView.dst1.Width, slideHeight)
        Dim r2 = New cv.Rect(0, 100, lrView.dst1.Width, slideHeight)
        lrView.dst1(r1).CopyTo(dst1(r2))

        r2.Y += slideHeight
        lrView.dst2(r1).CopyTo(dst1(r2))
        dst2 = lrView.dst2
    End Sub
End Class





Public Class LeftRightView_Features
    Inherits VBparent
    Dim lrView As LeftRightView_Basics
    Dim features As Features_GoodFeatures
    Public Sub New()
        initParent()
        features = New Features_GoodFeatures()

        lrView = New LeftRightView_Basics()

        task.desc = "Find GoodFeatures in the left and right depalettized infrared images"
        label1 = "Left Image"
        label2 = "Right Image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        lrView.Run()

        features.src = lrView.dst2
        features.Run()
        lrView.dst2.CopyTo(dst2)
        For i = 0 To features.goodFeatures.Count - 1
            cv.Cv2.Circle(dst2, features.goodFeatures(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next

        features.src = lrView.dst1
        features.Run()
        lrView.dst1.CopyTo(dst1)
        For i = 0 To features.goodFeatures.Count - 1
            cv.Cv2.Circle(dst1, features.goodFeatures(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class




Public Class LeftRightView_Palettized
    Inherits VBparent
    Dim lrView As LeftRightView_Basics
    Dim palette As Palette_Basics
    Public Sub New()
        initParent()
        lrView = New LeftRightView_Basics()
        palette = New Palette_Basics()

        task.desc = "Add color to the 8-bit infrared images."
        label1 = "Left Image"
        label2 = "Right Image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        lrView.Run()

        palette.src = lrView.dst1
        palette.Run()
        dst1 = palette.dst1

        palette.src = lrView.dst2
        palette.Run()
        dst2 = palette.dst1
    End Sub
End Class




Public Class LeftRightView_BRISK
    Inherits VBparent
    Dim lrView As LeftRightView_Basics
    Dim brisk As BRISK_Basics
    Public Sub New()
        initParent()
        task.desc = "Add color to the 8-bit infrared images."
        label1 = "Infrared Left Image"
        label2 = "Infrared Right Image"

        brisk = New BRISK_Basics()
        brisk.sliders.trackbar(0).Value = 20

        lrView = New LeftRightView_Basics()
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        lrView.Run()
        brisk.src = lrView.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        brisk.Run()
        dst2 = brisk.dst1

        brisk.src = lrView.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        brisk.Run()
        dst1 = brisk.dst1
    End Sub
End Class






Public Class LeftRightView_BrightnessContrast
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            If task.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.OakDCamera Then
                sliders.setupTrackBar(0, "Brightness Alpha (contrast)", 0, 10000, 1200)
                sliders.setupTrackBar(1, "Brightness Beta (brightness)", -255, 255, -50)
            Else
                sliders.setupTrackBar(0, "Brightness Alpha (contrast)", 0, 10000, 5000)
                sliders.setupTrackBar(1, "Brightness Beta (brightness)", -255, 255, -100)
            End If
        End If

        label1 = "Left View"
        label2 = "Right View"
        task.desc = "Enhance the left/right views with brightness and contrast."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        dst1 = task.leftView.ConvertScaleAbs(sliders.trackbar(0).Value / 500, sliders.trackbar(1).Value)
        dst2 = task.rightView.ConvertScaleAbs(sliders.trackbar(0).Value / 500, sliders.trackbar(1).Value)
    End Sub
End Class