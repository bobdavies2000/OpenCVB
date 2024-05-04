Imports cv = OpenCvSharp
' https://github.com/ncosentino/DevLeader/tree/master/AsciiArtGenerator
Public Class AsciiArt_Basics : Inherits VB_Algorithm
    Dim asciiChars As String() = {"@", "%", "#", "*", "+", "=", "-", ":", ",", ".", " "}
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Character height in pixels", 20, 100, 31)
            sliders.setupTrackBar("Character width in pixels", 20, 200, 55)
        End If

        vbAddAdvice(traceName + ": use the local options for height and width.")
        labels = {"", "", "Ascii version", "Grayscale input to ascii art"}
        desc = "Build an ascii art representation of the input stream."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static hSlider = findSlider("Character height in pixels")
        Static wSlider = findSlider("Character width in pixels")

        Dim hStep = CInt(src.Height / hSlider.value)
        Dim wStep = CInt(src.Width / wSlider.value)
        Dim size = New cv.Size(CInt(wSlider.value), CInt(hSlider.value))

        dst3 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Resize(size, cv.InterpolationFlags.Nearest)
        For y = 0 To dst3.Height - 1
            For x = 0 To dst3.Width - 1
                Dim grayValue = dst3.Get(Of Byte)(y, x)
                Dim asciiChar = asciiChars(grayValue * (asciiChars.Length - 1) / 255)
                setTrueText(asciiChar, New cv.Point(x * wStep, y * hStep), 2)
            Next
        Next
        labels(2) = "Ascii version using " + Format(dst3.Height * dst3.Width, fmt0) + " characters"
    End Sub
End Class







Public Class AsciiArt_Gray : Inherits VB_Algorithm
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "A palette'd version of the ascii art data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim hStep = CInt(src.Height / 31) - 1
        Dim wStep = CInt(src.Width / 55) - 1
        Dim size = New cv.Size(55, 31)
        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Resize(size, cv.InterpolationFlags.Nearest)
        Dim grayRatio = 12 / 255
        For y = 0 To dst1.Height - 1
            For x = 0 To dst1.Width - 1
                Dim r = New cv.Rect(x * wStep, y * hStep, wStep - 1, hStep - 1)
                Dim asciiChar = CInt(dst1.Get(Of Byte)(y, x) * grayRatio)
                dst3(r).SetTo(asciiChar)
            Next
        Next
        dst2 = vbPalette(dst3 / grayRatio)
    End Sub
End Class







Public Class AsciiArt_Diff : Inherits VB_Algorithm
    Dim grayAA As New AsciiArt_Gray
    Dim diff As New Diff_Basics
    Public Sub New()
        desc = "Display the instability in image pixels."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        grayAA.Run(src)
        dst2 = grayAA.dst2

        diff.Run(dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst3 = diff.dst2
    End Sub
End Class
