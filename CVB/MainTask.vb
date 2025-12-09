Imports cv = OpenCvSharp
Module GlobalVariables
    Public settings As CVB.Json
    Public homeDirPath As String
    Public myTask As cvbTask

    Public emptyRect As New cv.Rect

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
            ' process the images and put the results in dst().
            myTask.color = camImages.images(0)
            myTask.pointCloud = camImages.images(1)
            myTask.leftView = camImages.images(2)
            myTask.rightView = camImages.images(3)

            myTask.pcSplit = myTask.pointCloud.Split()
            myTask.colorizer.Run(myTask.pcSplit(2))

            myTask.dst = {myTask.color, myTask.depthRGB, myTask.leftView, myTask.rightView}

            AlgDescription.Text = myTask.desc
        End Sub
    End Class
End Namespace