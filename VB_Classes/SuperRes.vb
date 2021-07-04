Imports cv = OpenCvSharp
' https://github.com/opencv/opencv/blob/3.2.0/samples/gpu/super_resolution.cpp
Public Class SuperRes_Basics : Inherits VBparent
    Dim video As New SuperRes_Input
    Public Sub New()
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 4)
            radio.check(0).Text = "farneback"
            radio.check(1).Text = "tvl1"
            radio.check(2).Text = "brox"
            radio.check(3).Text = "pyrlk"
            radio.check(0).Checked = True
        End If

        labels(2) = "Original Input video"
        labels(3) = "SuperRes output"
        task.desc = "Create superres version of the video input"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.frameCount = 0 Then
            setTrueText("The first frame takes a while - adjust the 180 iterations below to speed things up...")
            Exit Sub
        End If
        Dim method As String = ""
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                method = Choose(i + 1, "farneback", "simple", "tvl1", "brox", "pyrlk")
                Exit For
            End If
        Next

        video.Run(Nothing)
        dst2 = video.dst2

        Static optFlow As cv.DenseOpticalFlowExt
        Static superres As cv.SuperResolution
        If optFlow Is Nothing Then
            Select Case method ' only one method available with OpenCVSharp...
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
            superres.Iterations = 180
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
        If video.dst2.Empty() = False And video.image.Empty() = False Then
            dst2 = video.image
        End If
    End Sub
End Class