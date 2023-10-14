Imports cv = OpenCvSharp
Imports CS_Classes
Public Class Wavelet_Basics : Inherits VB_Algorithm
    Dim wave As New CS_Wavelet
    Dim options As New Options_Wavelet
    Public Sub New()
        labels = {"", "", "Input image after wavelet transform", "Input image restored from the wavelet transform"}
        desc = "Accord: perform the wavelet transform on the input image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()

        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Resize(New cv.Size(512, 512))
        Dim Bitmap = cv.Extensions.BitmapConverter.ToBitmap(src)
        wave.RunCS(Bitmap, options.iterations, options.useHaar)
        dst2 = cv.Extensions.BitmapConverter.ToMat(wave.forwardImage)
        dst2 = dst2.Resize(task.workingRes)
        dst3 = cv.Extensions.BitmapConverter.ToMat(wave.backwardImage)
        dst3 = dst3.Resize(task.workingRes)
    End Sub
End Class








Public Class Wavelet_Edges : Inherits VB_Algorithm
    Dim wave As New Wavelet_Basics
    Dim edges As New Edge_Canny
    Public Sub New()
        findSlider("Wavelet Iterations").Value = 1
        findSlider("Canny threshold1").Value = 30
        findSlider("Canny threshold2").Value = 30
        desc = "Use the wavelet transformed image to find edges"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        wave.Run(src)
        dst2 = wave.dst2(New cv.Rect(dst2.Width / 2, 0, dst2.Width / 2, dst2.Height / 2))
        edges.Run(dst2)
        dst3 = edges.dst2
    End Sub
End Class
