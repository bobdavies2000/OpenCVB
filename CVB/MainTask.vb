Imports System.IO
Imports VBClasses
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Namespace CVB
    Partial Public Class MainForm
        Public task As cvbTask
        Private Sub processImages(camImages As CameraImages.images)
            If task Is Nothing Then task = New cvbTask(camImages, settings)
            dstImages = camImages
        End Sub
    End Class
End Namespace