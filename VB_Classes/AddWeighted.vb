Imports cv = OpenCvSharp
Public Class AddWeighted_Basics : Inherits VBparent
    Public src2 As New cv.Mat
    Public weightSlider As System.Windows.Forms.TrackBar
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Weight", 0, 100, 50)
        End If
        weightSlider = findSlider("Weight")
        task.desc = "Add 2 images with specified weights."
    End Sub
    Public Sub Run(src As cv.Mat)
        If standalone Or task.intermediateReview = caller Then src2 = task.RGBDepth ' external use must provide src2!
        Dim alpha = weightSlider.Value / 100
        cv.Cv2.AddWeighted(src, alpha, src2, 1.0 - alpha, 0, dst1)
        label1 = "depth " + Format(1 - weightSlider.Value / 100, "#0%") + " RGB " + Format(weightSlider.Value / 100, "#0%")
    End Sub
End Class






Public Class AddWeighted_Edges : Inherits VBparent
    Dim edges As Edges_BinarizedSobel
    Dim addw As AddWeighted_Basics
    Public Sub New()
        edges = New Edges_BinarizedSobel
        addw = New AddWeighted_Basics
        findSlider("Weight").Value = 75
        task.desc = "Add in the edges separating light and dark to the color image"
    End Sub
    Public Sub Run(src As cv.Mat)
        edges.Run(src)
        dst1 = edges.dst2

        addw.src2 = edges.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        addw.Run(src)
        dst2 = addw.dst1
    End Sub
End Class







Public Class AddWeighted_ImageAccumulate : Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Accumulation weight of each image X100", 1, 100, 10)
        End If

        task.desc = "Update a running average of the image"
    End Sub
    Public Sub Run(src As cv.Mat)

        dst1 = New cv.Mat(task.depth32f.Size, cv.MatType.CV_32F)
        Static weightSlider = findSlider("Accumulation weight of each image X100")
        cv.Cv2.AccumulateWeighted(task.depth32f, dst1, weightSlider.value / 100, New cv.Mat)
    End Sub
End Class







Public Class AddWeighted_InfraRed : Inherits VBparent
    Dim addw As AddWeighted_Basics
    Dim infra As LeftRightView_BrightnessContrast
    Dim src2 As New cv.Mat
    Public Sub New()
        infra = New LeftRightView_BrightnessContrast
        addw = New AddWeighted_Basics

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 2)
            radio.check(0).Text = "Use LeftView"
            radio.check(1).Text = "Use RightView"
            radio.check(1).Checked = True
        End If

        task.desc = "Align the depth data with the left or right view.  Oak-D is aligned with the right image."
    End Sub
    Public Sub Run(src As cv.Mat)
        infra.Run(src)

        Static rightRadio = findRadio("Use RightView")
        Dim leftOrRight As String = "Right"
        If rightRadio.checked Then
            addw.src2 = infra.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Else
            addw.src2 = infra.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            leftOrRight = "Left"
        End If
        addw.Run(task.RGBDepth)
        dst1 = addw.dst1.Clone

        addw.Run(src)
        dst2 = addw.dst1
        label1 = "InfraRed " + leftOrRight + " " + Format(1 - addw.weightSlider.Value / 100, "#0%") + " Depth " + Format(addw.weightSlider.Value / 100, "#0%")
        label2 = "InfraRed " + leftOrRight + " " + Format(1 - addw.weightSlider.Value / 100, "#0%") + " RGB " + Format(addw.weightSlider.Value / 100, "#0%")
    End Sub
End Class

