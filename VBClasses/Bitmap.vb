Imports System.Drawing
Imports cv = OpenCvSharp
' https://www.csharpcodi.com/csharp-examples/OpenCvSharp.Extensions.BitmapConverter.ToBitmap(OpenCvSharp.Mat)/
Public Class Bitmap_ToMat : Inherits TaskParent
    Public Sub New()
        labels(2) = "Convert color bitmap to Mat"
        labels(3) = "Convert Mat to bitmap and then back to Mat"
        desc = "Convert a color and grayscale bitmap to a cv.Mat"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim filePath As String = task.settings.HomeDir + "opencv/Samples/Data/lena.jpg"
        Dim bitmap = New System.Drawing.Bitmap(filePath)
        dst2 = cv.Extensions.BitmapConverter.ToMat(bitmap).Resize(src.Size)

        bitmap = cv.Extensions.BitmapConverter.ToBitmap(src)
        dst3 = cv.Extensions.BitmapConverter.ToMat(bitmap)
    End Sub
End Class