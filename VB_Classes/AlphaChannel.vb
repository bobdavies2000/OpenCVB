Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
' https://www.learnopencv.com/alpha-blending-using-opencv-cpp-python/
' https://docs.microsoft.com/en-us/dotnet/api/system.drawing.bitmap.maketransparent?view=dotnet-plat-ext-3.1
Public Class AlphaChannel_Basics : Inherits VBparent
    Dim alpha As New imageForm
    Public Sub New()
        alpha.Show()
        alpha.Size = New System.Drawing.Size(dst2.Width + 10, dst2.Height + 10)

        task.desc = "Use the the Windows 10 alpha channel to separate foreground and background"
    End Sub
    Public Sub Run(ByVal src As cv.Mat)
        src = src.CvtColor(cv.ColorConversionCodes.BGR2BGRA)
        Dim split = src.Split()
        split(3) = task.depthMask
        cv.Cv2.Merge(split, src)
        alpha.imagePic.Image = cvext.BitmapConverter.ToBitmap(src, System.Drawing.Imaging.PixelFormat.Format32bppArgb)
    End Sub
End Class





' https://www.learnopencv.com/alpha-blending-using-opencv-cpp-python/
Public Class AlphaChannel_Blend : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Transparency amount", 0, 255, 100)
        End If

        task.desc = "Use alpha blending to smoothly separate background from foreground"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst3.SetTo(0)
        src.CopyTo(dst3, task.noDepthMask)

        Static transparencySlider = findSlider("Transparency amount")
        Dim alpha = transparencySlider.Value / 255
        cv.Cv2.AddWeighted(src, alpha, dst3, 1.0 - alpha, 0, dst2)
    End Sub
End Class

