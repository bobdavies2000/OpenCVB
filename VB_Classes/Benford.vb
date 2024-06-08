Imports cv = OpenCvSharp
Imports System.Text.RegularExpressions
' Benford's Law is pretty cool but I don't think it is a phenomenon of nature.  It is produced from bringing real world measurements to a human scale.
' Reducing an image with compression works because human understanding maps the data within reach of the understanding embedded in our number system.
' (Further investigation: would a base other than 10 provide the same results?)
' If real world measurements do not conform to Benford's Law, it is likely because the measurement is not a good one or has been manipulated.
' Benford's law is a good indicator that the scale for the measurement is appropriate.
' Below are 2 types of examples - one just takes the grayscale image and applies Benford's analysis, the other uses jpeg/PNG before applying Benford.
' Only the JPEG/PNG examples match Benford while the grayscale image does not.
' Note that with the 10-99 Benford JPEG example, the results match Benford and then stop matching and abruptly fall off in the middle of the plot.
' This impact is likely the result of how JPEG compression truncates values as insignificant - a definite manipulation of the data.

' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
Public Class Benford_Basics : Inherits VB_Parent
    Public expectedDistribution(10 - 1) As Single
    Public counts(expectedDistribution.Count - 1) As Single
    Dim plot As New Plot_Histogram
    Dim addW As New AddWeighted_Basics
    Dim use99 As Boolean
    Public Sub New()
        For i = 1 To expectedDistribution.Count - 1
            expectedDistribution(i) = Math.Log10(1 + 1 / i) ' get the precise expected values.
        Next

        labels(3) = "Actual distribution of input"
        desc = "Build the capability to perform a Benford analysis."
    End Sub
    Public Sub setup99()
        ReDim expectedDistribution(100 - 1)
        For i = 1 To expectedDistribution.Count - 1
            expectedDistribution(i) = Math.Log10(1 + 1 / i)
        Next
        ReDim counts(expectedDistribution.Count - 1)
        use99 = True
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If standaloneTest() Then
            dst2 = If(src.Channels = 1, src, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            src = New cv.Mat(dst2.Size, cv.MatType.CV_32F)
            dst2.ConvertTo(src, cv.MatType.CV_32F)
        End If

        src = src.Reshape(1, src.Width * src.Height)
        Dim indexer = src.GetGenericIndexer(Of Single)()
        ReDim counts(expectedDistribution.Count - 1)
        If use99 = False Then
            For i = 0 To src.Rows - 1
                Dim val = indexer(i).ToString
                If val <> 0 And Single.IsNaN(val) = False Then
                    Dim firstInt = Regex.Match(val, "[1-9]{1}")
                    If firstInt.Length > 0 Then counts(firstInt.Value) += 1
                End If
            Next
        Else
            ' this is for the distribution 10-99
            For i = 0 To src.Rows - 1
                Dim val = indexer(i).ToString
                If val <> 0 And Single.IsNaN(val) = False Then
                    Dim firstInt = Regex.Match(val, "[1-9]{1}").ToString
                    Dim index = val.IndexOf(firstInt)
                    If index < Len(val - 2) And index > 0 Then
                        Dim val99 = Mid(val, index + 1, 2)
                        If IsNumeric(val99) Then counts(val99) += 1
                    End If
                End If
            Next
        End If

        Dim hist = New cv.Mat(counts.Length, 1, cv.MatType.CV_32F, counts)
        plot.backColor = cv.Scalar.Blue
        plot.Run(hist)
        dst3 = plot.dst2.Clone
        For i = 0 To counts.Count - 1
            counts(i) = src.Rows * expectedDistribution(i)
        Next

        hist = New cv.Mat(counts.Length, 1, cv.MatType.CV_32F, counts)
        plot.backColor = cv.Scalar.Gray
        plot.Run(hist)

        addW.src2 = Not plot.dst2
        addW.Run(dst3)
        dst2 = addW.dst2

        Static weightSlider = FindSlider("Add Weighted %")
        Dim wt = weightSlider.value
        labels(2) = "AddWeighted: " + Format(wt, "%0.0") + " actual vs. " + Format(1 - wt, "%0.0") + " Benford distribution"
    End Sub
End Class






' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
Public Class Benford_NormalizedImage : Inherits VB_Parent
    Public benford As New Benford_Basics
    Public Sub New()
        desc = "Perform a Benford analysis of an image normalized to between 0 and 1"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        dst3 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim gray32f As New cv.Mat
        dst3.ConvertTo(gray32f, cv.MatType.CV_32F)

        benford.Run(gray32f.Normalize(1))
        dst2 = benford.dst2
        labels(2) = benford.labels(3)
        labels(3) = "Input image"
    End Sub
End Class






' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
Public Class Benford_NormalizedImage99 : Inherits VB_Parent
    Public benford As New Benford_Basics
    Public Sub New()
        benford.setup99()

        desc = "Perform a Benford analysis for 10-99, not 1-9, of an image normalized to between 0 and 1"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        dst3 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim gray32f As New cv.Mat
        dst3.ConvertTo(gray32f, cv.MatType.CV_32F)

        benford.Run(gray32f.Normalize(1))
        dst2 = benford.dst2
        labels(2) = benford.labels(3)
        labels(3) = "Input image"
    End Sub
End Class






' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
Public Class Benford_JPEG : Inherits VB_Parent
    Public benford As New Benford_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("JPEG Quality", 1, 100, 90)
        desc = "Perform a Benford analysis for 1-9 of a JPEG compressed image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static qualitySlider = FindSlider("JPEG Quality")
        Dim jpeg() = src.ImEncode(".jpg", New Integer() {cv.ImwriteFlags.JpegQuality, qualitySlider.Value})
        Dim tmp = New cv.Mat(jpeg.Count, 1, cv.MatType.CV_8U, jpeg)
        dst3 = cv.Cv2.ImDecode(tmp, cv.ImreadModes.Color)
        benford.Run(tmp)
        dst2 = benford.dst2
        labels(2) = benford.labels(3)
        labels(3) = "Input image"
    End Sub
End Class






' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
Public Class Benford_JPEG99 : Inherits VB_Parent
    Public benford As New Benford_Basics
    Public Sub New()
        benford.setup99()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("JPEG Quality", 1, 100, 90)
        desc = "Perform a Benford analysis for 10-99, not 1-9, of a JPEG compressed image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static qualitySlider = FindSlider("JPEG Quality")
        Dim jpeg() = src.ImEncode(".jpg", New Integer() {cv.ImwriteFlags.JpegQuality, qualitySlider.Value})
        Dim tmp = New cv.Mat(jpeg.Count, 1, cv.MatType.CV_8U, jpeg)
        dst3 = cv.Cv2.ImDecode(tmp, cv.ImreadModes.Color)
        benford.Run(tmp)
        dst2 = benford.dst2
        labels(2) = benford.labels(3)
        labels(3) = "Input image"
    End Sub
End Class







' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
Public Class Benford_PNG : Inherits VB_Parent
    Public benford As New Benford_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("PNG Compression", 1, 100, 90)
        desc = "Perform a Benford analysis for 1-9 of a JPEG compressed image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static compressionSlider = FindSlider("PNG Compression")
        Dim png = src.ImEncode(".png", New Integer() {cv.ImwriteFlags.PngCompression, compressionSlider.Value})
        Dim tmp = New cv.Mat(png.Count, 1, cv.MatType.CV_8U, png)
        dst3 = cv.Cv2.ImDecode(tmp, cv.ImreadModes.Color)
        benford.Run(tmp)
        dst2 = benford.dst2
        labels(2) = benford.labels(3)
        labels(3) = "Input image"
    End Sub
End Class






Public Class Benford_Depth : Inherits VB_Parent
    Public benford As New Benford_Basics
    Public Sub New()
        desc = "Apply Benford to the depth data"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        benford.Run(task.pcSplit(2))
        dst2 = benford.dst2
        labels(2) = benford.labels(3)
    End Sub
End Class









Public Class Benford_Primes : Inherits VB_Parent
    Dim sieve As New Sieve_BasicsVB
    Dim benford As New Benford_Basics
    Public Sub New()
        Static countSlider = FindSlider("Count of desired primes")
        countSlider.Value = countSlider.Maximum
        labels = {"", "", "Actual Distribution of input", ""}
        desc = "Apply Benford to a list of primes"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If task.optionsChanged Then sieve.Run(src) ' only need to compute this once...
        setTrueText($"Primes found: {sieve.primes.Count}", 3)

        Dim tmp = New cv.Mat(sieve.primes.Count, 1, cv.MatType.CV_32S, sieve.primes.ToArray())
        tmp.ConvertTo(tmp, cv.MatType.CV_32F)
        benford.Run(tmp)
        dst2 = benford.dst2
    End Sub
End Class
