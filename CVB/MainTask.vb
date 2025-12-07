Imports System.IO
Imports VBClasses
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
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