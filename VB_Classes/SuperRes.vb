Imports cv = OpenCvSharp
' https://github.com/opencv/opencv/blob/3.2.0/samples/gpu/super_resolution.cpp
Public Class SuperRes_Basics : Inherits VBparent
    Dim video As New SuperRes_Input
    Dim options As New SuperRes_Options
    Public Sub New()
        labels(2) = "Original Input video"
        labels(3) = "SuperRes output"
        task.desc = "Create superres version of the video input"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static optFlow As cv.DenseOpticalFlowExt
        Static superres As cv.SuperResolution
        Static warningMessage As Integer = 10
        If warningMessage > 0 Then
            setTrueText("The first frame takes a while - adjust the 180 iterations in the code to speed things up...")
            warningMessage -= 1
            Exit Sub
        End If

        options.RunClass(Nothing)
        If options.restartWithNewOptions Then
            warningMessage = 10
            optFlow = Nothing ' start over...
            'findfrm("SuperRes_Options Radio Options").Visible = False
            'findfrm("Video_Basics OpenFile Options").Visible = False
            video = New SuperRes_Input
            allOptions.layoutOptions()
            Exit Sub
        End If

        video.Run(Nothing)
        dst2 = video.dst2

        If optFlow Is Nothing Then
            Select Case options.method ' only one method available with OpenCVSharp...
                Case "farneback"
                    optFlow = cv.FarnebackOpticalFlow.CreateFarneback
                Case "brox"
                    optFlow = cv.BroxOpticalFlow.CreateFarneback
                Case "tvl1"
                    optFlow = cv.DualTVL1OpticalFlow.CreateDualTVL1
                Case "pyrlk"
                    optFlow = cv.PyrLKOpticalFlow.CreateFarneback
            End Select
            If optFlow Is Nothing Then Exit Sub
            superres = cv.SuperResolution.CreateBTVL1()
            superres.Iterations = options.iterations
            superres.Scale = 4
            superres.TemporalAreaRadius = 4
            superres.SetInput(cv.FrameSource.CreateFrameSource_Video(video.inputFileName))
        End If

        superres.NextFrame(dst3)
        If dst3.Width = 0 Then
            dst3 = dst2.Clone
            optFlow = Nothing ' start over...
        End If
    End Sub
End Class






'https://github.com/opencv/opencv/blob/3.2.0/samples/gpu/super_resolution.cpp
Public Class SuperRes_Input : Inherits VBparent
    Public video As New Video_Basics
    Public inputFileName As String
    Public Sub New()
        video.fileNameForm.filename.Text = task.parms.homeDir + "Data/testdata_superres_car.avi"
        inputfilename = video.fileNameForm.filename.Text
        task.desc = "Input data for the superres testing"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        video.RunClass(Nothing)
        If video.dst2.Empty() = False And video.image.Empty() = False Then dst2 = video.image
    End Sub
End Class










Public Class SuperRes_Options : Inherits VBparent
    Public method As String = "farneback"
    Public iterations As Integer = 10
    Public restartWithNewOptions As Boolean
    Public Sub New()
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 4)
            radio.check(0).Text = "farneback"
            radio.check(1).Text = "tvl1"
            radio.check(2).Text = "brox"
            radio.check(3).Text = "pyrlk"
            radio.check(0).Checked = True
        End If

        If sliders.Setup(caller) Then sliders.setupTrackBar(0, "SuperRes Iterations", 10, 200, 10)
        task.desc = "Options for OpenCV's SuperRes"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static iterSlider = findSlider("SuperRes Iterations")
        If standalone Then setTrueText("SuperRes_Options just handles all the options for SuperRes_Basics")
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                method = Choose(i + 1, "farneback", "tvl1", "brox", "pyrlk")
                Exit For
            End If
        Next
        Static lastMethod = method
        restartWithNewOptions = False
        If lastMethod <> method Or iterSlider.value <> iterations Then restartWithNewOptions = True
        lastMethod = method
        iterations = iterSlider.value
    End Sub
End Class