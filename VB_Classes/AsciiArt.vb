Imports cvb = OpenCvSharp
' https://github.com/ncosentino/DevLeader/tree/master/AsciiArtGenerator
Public Class AsciiArt_Basics : Inherits TaskParent
    Dim asciiChars As String() = {"@", "%", "#", "*", "+", "=", "-", ":", ",", ".", " "}
    Dim options As New Options_AsciiArt
    Public Sub New()
        UpdateAdvice(traceName + ": use the local options for height and width.")
        labels = {"", "", "Ascii version", "Grayscale input to ascii art"}
        desc = "Build an ascii art representation of the input stream."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst3 = src.CvtColor(cvb.ColorConversionCodes.BGR2Gray).Resize(options.size, 0, 0, cvb.InterpolationFlags.Nearest)
        For y = 0 To dst3.Height - 1
            For x = 0 To dst3.Width - 1
                Dim grayValue = dst3.Get(Of Byte)(y, x)
                Dim asciiChar = asciiChars(grayValue * (asciiChars.Length - 1) / 255)
                SetTrueText(asciiChar, New cvb.Point(x * options.wStep, y * options.hStep), 2)
            Next
        Next
        labels(2) = "Ascii version using " + Format(dst3.Height * dst3.Width, fmt0) + " characters"
    End Sub
End Class







Public Class AsciiArt_Color : Inherits TaskParent
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "A palette'd version of the ascii art data"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim hStep = CInt(src.Height / 31) - 1
        Dim wStep = CInt(src.Width / 55) - 1
        Dim size = New cvb.Size(55, 31)
        dst1 = src.CvtColor(cvb.ColorConversionCodes.BGR2Gray).Resize(size, 0, 0, cvb.InterpolationFlags.Nearest)
        Dim grayRatio = 12 / 255
        For y = 0 To dst1.Height - 1
            For x = 0 To dst1.Width - 1
                Dim r = New cvb.Rect(x * wStep, y * hStep, wStep - 1, hStep - 1)
                Dim asciiChar = CInt(dst1.Get(Of Byte)(y, x) * grayRatio)
                dst3(r).SetTo(asciiChar)
            Next
        Next
        dst2 = ShowPalette(dst3 / grayRatio)
    End Sub
End Class







Public Class AsciiArt_Diff : Inherits TaskParent
    Dim grayAA As New AsciiArt_Color
    Dim diff As New Diff_Basics
    Public Sub New()
        desc = "Display the instability in image pixels."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        grayAA.Run(src)
        dst2 = grayAA.dst2

        diff.Run(dst2.CvtColor(cvb.ColorConversionCodes.BGR2Gray))
        dst3 = diff.dst2
    End Sub
End Class
