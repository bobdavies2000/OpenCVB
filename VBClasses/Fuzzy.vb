Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    ' https://docs.opencv.org/4.x/df/d5b/group__fuzzy.html
    Public Class Fuzzy_Basics : Inherits TaskParent
        Public Sub New()
            If sliders.Setup(traceName) Then
                sliders.setupTrackBar("F-transform radius", 1, 10, 3)
            End If
            labels(2) = "F0-transform (FT02D_process)"
            labels(3) = "Fast linear F0-transform (FT02D_FL_process)"
            desc = "Smooth an image with OpenCV's fuzzy F0-transform."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static radiusSlider = OptionParent.FindSlider("F-transform radius")
            Dim radius As Integer = radiusSlider.Value

            ' Both transforms below expect an 8-bit BGR image. The fast path reads each channel as bytes.
            Dim input As Mat = src
            If input.Type <> MatType.CV_8UC3 Then
                input = New Mat()
                If src.Channels() = 1 Then
                    CvtColor(src, input, ColorConversionCodes.GRAY2BGR)
                Else
                    src.ConvertTo(input, MatType.CV_8UC3)
                End If
            End If

            ' Triangular membership function. The kernel's channel count has to match the image.
            Dim kernel As New Mat
            Fuzzy.CreateKernel(FuzzyFunction.Linear, radius, kernel, input.Channels())

            ' Direct F0-transform plus its inverse. An empty mask keeps every pixel.
            ' The result is 32-bit and stays near the original 0..255 range.
            Dim ft As New Mat
            Fuzzy.Ft02DProcess(input, kernel, ft, emptyMat)
            ft.ConvertTo(dst2, MatType.CV_8UC3)

            ' Same linear F0-transform with the kernel built inside the call. Output is 8UC3.
            Fuzzy.Ft02DFastLinearProcess(input, radius, dst3)

            labels(2) = "FT02D_process  radius=" + CStr(radius)
            labels(3) = "FT02D_FL_process  radius=" + CStr(radius)
        End Sub
    End Class
End Namespace
