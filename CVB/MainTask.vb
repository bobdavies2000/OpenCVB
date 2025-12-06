Imports System.IO
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Namespace CVB
    Partial Public Class MainForm
        Private Sub processImages(camImages As CameraImages.images)
            dstImages = camImages
        End Sub
    End Class
End Namespace