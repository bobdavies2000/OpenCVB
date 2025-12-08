Imports System.IO
Imports VBClasses
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Module GlobalVariables
    Public settings As CVB.Json
    Public homeDirPath As String
    Public myTask As cvbTask

    Public Const fmt0 = "0"
    Public Const fmt1 = "0.0"
    Public Const fmt2 = "0.00"
    Public Const fmt3 = "0.000"
    Public Const fmt4 = "0.0000"

    Public cameraNames As New List(Of String)({"Intel(R) RealSense(TM) Depth Camera 435i",
                                               "Intel(R) RealSense(TM) Depth Camera 455",
                                               "Oak-D camera",
                                               "Orbbec Gemini 335",
                                               "Orbbec Gemini 335L",
                                               "Orbbec Gemini 336L",
                                               "StereoLabs ZED 2/2i"
                                               })
End Module
Namespace CVB
    Partial Public Class MainForm
        Private Sub processImages(camImages As CameraImages.images)
            If myTask Is Nothing Then myTask = New cvbTask(camImages, settings)
            For i = 0 To 3
                myTask.dstList(i) = camImages.images(i)
            Next
            AlgDescription.Text = myTask.desc
        End Sub
    End Class
End Namespace