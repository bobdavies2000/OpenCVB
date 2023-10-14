Imports cv = OpenCvSharp
Imports OpenCvSharp.Extensions
' https://www.learnopencv.com/alpha-blending-using-opencv-cpp-python/
' https://docs.microsoft.com/en-us/dotnet/api/system.drawing.bitmap.maketransparent?view=dotnet-plat-ext-3.1
Public Class AlphaChannel_Basics : Inherits VB_Algorithm
    Dim alpha As New imageForm
    Public Sub New()
        alpha.Show()
        alpha.Size = New System.Drawing.Size(dst2.Width + 10, dst2.Height + 10)

        desc = "Use the the Windows 10 alpha channel to separate foreground and background"
    End Sub
    Public Sub RunVB(ByVal src As cv.Mat)
        src = src.CvtColor(cv.ColorConversionCodes.BGR2BGRA)
        Dim split = src.Split()
        split(3) = task.depthMask
        cv.Cv2.Merge(split, src)
        alpha.imagePic.Image = BitmapConverter.ToBitmap(src, System.Drawing.Imaging.PixelFormat.Format32bppArgb)
    End Sub
End Class