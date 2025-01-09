Imports cv = OpenCvSharp
Imports System.IO
' https://github.com/opencv/opencv/blob/3.2.0/samples/gpu/super_resolution.cpp
Public Class SuperRes_Basics : Inherits TaskParent
    Dim video As New SuperRes_Input
    Dim options As New Options_SuperRes
    Dim optFlow As cv.DenseOpticalFlowExt
    Dim superres As cv.SuperResolution
    Dim warningMessage As Integer = 10
    Public Sub New()
        labels(2) = "Original Input video"
        labels(3) = "SuperRes output"
        desc = "Create superres version of the video input"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        If warningMessage > 0 Then
            SetTrueText("The first frame takes a while when iterations are over 50 or so")
            warningMessage -= 1
            Exit Sub
        End If

        options.RunOpt()
        If options.restartWithNewOptions Then
            warningMessage = 10
            optFlow = Nothing ' start over...
            video = New SuperRes_Input
            Exit Sub
        End If

        video.Run(src)
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
Public Class SuperRes_Input : Inherits TaskParent
    Public video As New Video_Basics
    Public inputFileName As String
    Public Sub New()
        video.options.fileInfo = New FileInfo(task.HomeDir + "Data/testdata_superres_car.avi")
        inputFileName = video.options.fileInfo.FullName
        desc = "Input data for the superres testing"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        video.Run(src)
        dst2 = video.dst2
    End Sub
End Class










Public Class SuperRes_SubPixelZoom : Inherits TaskParent
    Dim zoom As New Pixel_SubPixel
    Dim video As New SuperRes_Input
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Is SuperRes better than just zoom with sub-pixel accuracy?"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        task.mouseMovePoint = New cv.Point(45, 60)
        video.Run(src)
        If video.video.captureVideo.PosFrames > 30 Then Exit Sub
        dst1 = video.dst2
        zoom.Run(video.dst2)
        dst2 = zoom.dst2
        dst3 = zoom.dst3
        labels = zoom.labels
    End Sub
End Class