Imports System.Drawing
Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Imports gdip = OpenCvSharp.GdipExtensions
' https://www.csharpcodi.com/csharp-examples/OpenCvSharp.Extensions.BitmapConverter.ToBitmap(OpenCvSharp.Mat)/
Public Class Bitmap_ToMat : Inherits TaskParent
    Public Sub New()
        labels(2) = "Convert color bitmap to Mat"
        labels(3) = "Convert Mat to bitmap and then back to Mat"
        desc = "Convert a color and grayscale bitmap to a cv.Mat"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim filePath As String = task.homeDir + "opencv/Samples/Data/lena.jpg"
        Dim bitmap = New System.Drawing.Bitmap(filePath)
        Resize(gdip.BitmapConverter.ToMat(bitmap), dst2, src.Size)

        bitmap = gdip.BitmapConverter.ToBitmap(src)
        dst3 = gdip.BitmapConverter.ToMat(bitmap)
    End Sub
End Class
