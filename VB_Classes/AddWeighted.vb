Imports cv = OpenCvSharp
Public Class AddWeighted_Basics : Inherits VBparent
    Public src2 As New cv.Mat
    Public Sub New()
        src2 = New cv.Mat(dst2.Size, cv.MatType.CV_8UC3, 0)
        task.desc = "Add 2 images with specified weights."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 2
        If standalone Then src2 = task.RGBDepth ' external use must provide src2!
        If src.Channels = src2.Channels And src.Type = src2.Type Then
            cv.Cv2.AddWeighted(src, task.AddWeighted, src2, 1.0 - task.AddWeighted, 0, dst2)
        Else
            task.trueText("Unable to mix src and src2 - not the same number of channels or type...")
        End If
        label1 = "depth " + Format(1 - task.AddWeighted, "#0%") + " RGB " + Format(task.AddWeighted, "#0%")
    End Sub
End Class






Public Class AddWeighted_Edges : Inherits VBparent
    Dim edges As New Edges_BinarizedSobel
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        task.desc = "Add in the edges separating light and dark to the color image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        edges.Run(src)
        dst2 = edges.dst3

        addw.src2 = edges.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        addw.Run(src)
        dst3 = addw.dst2
    End Sub
End Class







Public Class AddWeighted_ImageAccumulate : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Accumulation weight of each image X100", 1, 100, 10)
        End If

        task.desc = "Update a running average of the image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static weightSlider = findSlider("Accumulation weight of each image X100")
        dst2 = New cv.Mat(task.depth32f.Size, cv.MatType.CV_32F)
        cv.Cv2.AccumulateWeighted(task.depth32f, dst2, weightSlider.value / 100, New cv.Mat)
    End Sub
End Class







Public Class AddWeighted_InfraRed : Inherits VBparent
    Dim infra As New LeftRight_Basics
    Dim addw As New AddWeighted_Basics
    Dim src2 As New cv.Mat
    Public Sub New()
        If radio.Setup(caller, 2) Then
            radio.check(0).Text = "Use LeftView"
            radio.check(1).Text = "Use RightView"
            radio.check(1).Checked = True
        End If
        task.desc = "Align the depth data with the left or right view.  Oak-D is aligned with the right image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static rightRadio = findRadio("Use RightView")
        infra.Run(src)

        Dim leftOrRight As String = "Right"
        If rightRadio.checked Then
            addw.src2 = infra.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Else
            addw.src2 = infra.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            leftOrRight = "Left"
        End If
        addw.Run(task.RGBDepth)
        dst2 = addw.dst2.Clone

        addw.Run(src)
        dst3 = addw.dst2
        label1 = "InfraRed " + leftOrRight + " " + Format(1 - task.AddWeighted, "#0%") + " Depth " + Format(task.AddWeighted, "#0%")
        label2 = "InfraRed " + leftOrRight + " " + Format(1 - task.AddWeighted, "#0%") + " RGB " + Format(task.AddWeighted, "#0%")
    End Sub
End Class

