Imports cv = OpenCvSharp
Imports System.Windows.Forms
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
Namespace VBClasses
    Public Class Benford_Basics : Inherits TaskParent
        Public expectedDistribution(10 - 1) As Single
        Public counts(expectedDistribution.Count - 1) As Single
        Dim plotHist As New Plot_Histogram
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
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim gray32f As New cv.Mat
            If standalone Then
                tsk.gray.ConvertTo(gray32f, cv.MatType.CV_32F)
            Else
                gray32f = src
            End If

            gray32f = gray32f.Reshape(1, gray32f.Width * gray32f.Height)
            Dim indexer = gray32f.GetGenericIndexer(Of Single)()
            ReDim counts(expectedDistribution.Count - 1)
            If use99 = False Then
                For i = 0 To gray32f.Rows - 1
                    Dim val = indexer(i)
                    Dim valstr = val.ToString
                    If val <> 0 And Single.IsNaN(val) = False Then
                        Dim firstInt = Regex.Match(valstr, "[1-9]{1}")
                        If firstInt.Length > 0 Then counts(firstInt.Value) += 1
                    End If
                Next
            Else
                ' this is for the distribution 10-99
                For i = 0 To gray32f.Rows - 1
                    Dim val = indexer(i)
                    If val <> 0 And Single.IsNaN(val) = False Then
                        Dim valstr = val.ToString
                        Dim firstInt = Regex.Match(valstr, "[1-9]{1}").ToString
                        Dim index = valstr.IndexOf(firstInt)
                        If index < Len(valstr - 2) And index > 0 Then
                            Dim val99 = Mid(valstr, index + 1, 2)
                            If IsNumeric(val99) Then counts(val99) += 1
                        End If
                    End If
                Next
            End If

            Dim hist = cv.Mat.FromPixelData(counts.Length, 1, cv.MatType.CV_32F, counts)
            plotHist.backColor = cv.Scalar.Blue
            plotHist.Run(hist)
            dst3 = plotHist.dst2.Clone
            For i = 0 To counts.Count - 1
                counts(i) = gray32f.Rows * expectedDistribution(i)
            Next

            hist = cv.Mat.FromPixelData(counts.Length, 1, cv.MatType.CV_32F, counts)
            plotHist.backColor = cv.Scalar.Gray
            plotHist.Run(hist)

            dst2 = ShowAddweighted(Not plotHist.dst2, dst3, labels(2))
        End Sub
    End Class






    ' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
    Public Class NR_Benford_NormalizedImage : Inherits TaskParent
        Public benford As New Benford_Basics
        Public Sub New()
            desc = "Perform a Benford analysis of an image normalized to between 0 and 1"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim gray32f As New cv.Mat
            tsk.gray.ConvertTo(gray32f, cv.MatType.CV_32F)

            benford.Run(gray32f.Normalize(1))
            dst2 = benford.dst2
            labels(2) = benford.labels(3)
            labels(3) = "Input image"
        End Sub
    End Class






    ' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
    Public Class NR_Benford_NormalizedImage99 : Inherits TaskParent
        Public benford As New Benford_Basics
        Public Sub New()
            benford.setup99()

            desc = "Perform a Benford analysis for 10-99, not 1-9, of an image normalized to between 0 and 1"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim gray32f As New cv.Mat
            tsk.gray.ConvertTo(gray32f, cv.MatType.CV_32F)

            benford.Run(gray32f.Normalize(1))
            dst2 = benford.dst2
            labels(2) = benford.labels(3)
            labels(3) = "Input image"
        End Sub
    End Class






    ' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
    Public Class NR_Benford_JPEG : Inherits TaskParent
        Public benford As New Benford_Basics
        Dim options As New Options_JpegQuality
        Public Sub New()
            desc = "Perform a Benford analysis for 1-9 of a JPEG compressed image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim jpeg() = src.ImEncode(".jpg", New Integer() {cv.ImwriteFlags.JpegQuality, options.quality})
            Dim tmp = cv.Mat.FromPixelData(jpeg.Count, 1, cv.MatType.CV_8U, jpeg)
            dst3 = cv.Cv2.ImDecode(tmp, cv.ImreadModes.Color)
            benford.Run(tmp)
            dst2 = benford.dst2
            labels(2) = benford.labels(3)
            labels(3) = "Input image"
        End Sub
    End Class






    ' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
    Public Class NR_Benford_JPEG99 : Inherits TaskParent
        Public benford As New Benford_Basics
        Public options As New Options_JpegQuality
        Public Sub New()
            benford.setup99()
            desc = "Perform a Benford analysis for 10-99, not 1-9, of a JPEG compressed image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim jpeg() = src.ImEncode(".jpg", New Integer() {cv.ImwriteFlags.JpegQuality, options.quality})
            Dim tmp = cv.Mat.FromPixelData(jpeg.Count, 1, cv.MatType.CV_8U, jpeg)
            dst3 = cv.Cv2.ImDecode(tmp, cv.ImreadModes.Color)
            benford.Run(tmp)
            dst2 = benford.dst2
            labels(2) = benford.labels(3)
            labels(3) = "Input image"
        End Sub
    End Class







    ' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
    Public Class NR_Benford_PNG : Inherits TaskParent
        Dim options As New Options_PNGCompression
        Public benford As New Benford_Basics
        Public Sub New()
            desc = "Perform a Benford analysis for 1-9 of a JPEG compressed image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim png = src.ImEncode(".png", New Integer() {cv.ImwriteFlags.PngCompression, options.compression})
            Dim tmp = cv.Mat.FromPixelData(png.Count, 1, cv.MatType.CV_8U, png)
            dst3 = cv.Cv2.ImDecode(tmp, cv.ImreadModes.Color)
            benford.Run(tmp)
            dst2 = benford.dst2
            labels(2) = benford.labels(3)
            labels(3) = "Input image"
        End Sub
    End Class






    Public Class NR_Benford_Depth : Inherits TaskParent
        Public benford As New Benford_Basics
        Public Sub New()
            desc = "Apply Benford to the depth data"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            benford.Run(tsk.pcSplit(2))
            dst2 = benford.dst2
            labels(2) = benford.labels(3)
        End Sub
    End Class









    Public Class NR_Benford_Primes : Inherits TaskParent
        Dim sieve As New Sieve_BasicsVB
        Dim benford As New Benford_Basics
        Public Sub New()
            sieve.setMaxPrimes()
            labels = {"", "", "Actual Distribution of input", ""}
            desc = "Apply Benford to a list of primes"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If tsk.optionsChanged Then sieve.Run(src) ' only need to compute this once...
            SetTrueText($"Primes found: {sieve.primes.Count}", 3)

            Dim tmp = cv.Mat.FromPixelData(sieve.primes.Count, 1, cv.MatType.CV_32S, sieve.primes.ToArray())
            tmp.ConvertTo(tmp, cv.MatType.CV_32F)
            benford.Run(tmp)
            dst2 = benford.dst2
        End Sub
    End Class
End Namespace