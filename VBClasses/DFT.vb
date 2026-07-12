Imports OpenCvSharp.XFeatures2D
Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Module dft_Module
    Public Function inverseDFT(complexImage As Mat) As Mat
        Dim invDFT As New Mat
        Dft(complexImage, invDFT, DftFlags.Inverse Or DftFlags.RealOutput)
        Normalize(invDFT, invDFT, 0, 255, NormTypes.MinMax)
        Dim inverse8u As New Mat
        invDFT.ConvertTo(inverse8u, MatType.CV_8U)
        Return inverse8u
    End Function
End Module




' http://stackoverflow.com/questions/19761526/how-to-do-inverse-dft-in-opencv
Public Class DFT_Basics : Inherits TaskParent
    Dim mats As New Mat_4to1
    Public magnitude As New Mat
    Public spectrum As New Mat
    Public complexImage As New Mat
    Public grayMat As Mat
    Public rows As Integer
    Public cols As Integer
    Public Sub New()
        mats.lineSeparators = False

        desc = "Explore the Discrete Fourier Transform."
        labels(2) = "Image after inverse DFT"
        labels(3) = "DFT_Basics Spectrum Magnitude"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        rows = GetOptimalDFTSize(task.gray.Rows)
        cols = GetOptimalDFTSize(task.gray.Cols)
        Dim padded = New Mat(task.gray.Width, task.gray.Height, MatType.CV_8UC3)
        CopyMakeBorder(task.gray, padded, 0, rows - task.gray.Rows, 0, cols - task.gray.Cols, BorderTypes.Constant, Scalar.All(0))
        Dim padded32 As New Mat
        padded.ConvertTo(padded32, MatType.CV_32F)
        Dim planes() = {padded32, New Mat(padded.Size(), MatType.CV_32F, Scalar.All(0))}
        Merge(planes, complexImage)
        Dft(complexImage, complexImage)

        ' compute the magnitude And switch to logarithmic scale => log(1 + sqrt(Re(DFT(I))^2 + Im(DFT(I))^2))
        Split(complexImage, planes)

        Cv2.Magnitude(planes(0), planes(1), magnitude)
        magnitude += Scalar.All(1) ' switch To logarithmic scale
        Log(magnitude, magnitude)

        ' crop the spectrum, if it has an odd number of rows Or columns
        spectrum = magnitude(New cv.Rect(0, 0, magnitude.Cols And -2, magnitude.Rows And -2))
        ' Transform the matrix with float values into range 0-255
        Normalize(spectrum, spectrum, 0, 255, NormTypes.MinMax)
        spectrum.ConvertTo(padded, MatType.CV_8U)

        ' rearrange the quadrants of Fourier image  so that the origin is at the image center
        Dim cx = padded.Cols \ 2
        Dim cy = padded.Rows \ 2

        mats.mat(3) = padded(New cv.Rect(0, 0, cx, cy)).Clone()
        mats.mat(2) = padded(New cv.Rect(cx, 0, cx, cy)).Clone()
        mats.mat(1) = padded(New cv.Rect(0, cy, cx, cy)).Clone()
        mats.mat(0) = padded(New cv.Rect(cx, cy, cx, cy)).Clone()
        mats.Run(emptyMat)
        dst3 = mats.dst2

        dst2 = inverseDFT(complexImage)
    End Sub
End Class





' http://opencvexamples.blogspot.com/
Public Class XR_DFT_Inverse : Inherits TaskParent
    Dim mats As New Mat_2to1
    Public Sub New()
        labels(2) = "Image after Inverse DFT"
        desc = "Take the inverse of the Discrete Fourier Transform."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim gray32f As New Mat
        task.gray.ConvertTo(gray32f, MatType.CV_32F)
        Dim planes() = {gray32f, New Mat(gray32f.Size(), MatType.CV_32F, Scalar.All(0))}
        Dim complex As New Mat, complexImage As New Mat
        Merge(planes, complex)
        Dft(complex, complexImage)

        dst2 = inverseDFT(complexImage)

        Dim diff As New Mat
        Absdiff(task.gray, dst2, diff)
        Threshold(diff, mats.mat(0), 0, 255, ThresholdTypes.Binary)
        mats.mat(1) = (diff * 50).ToMat
        mats.Run(emptyMat)
        If CountNonZero(mats.mat(0)) > 0 Then
            dst3 = mats.dst2
            labels(3) = "Mask of difference (top) and relative diff (bot)"
        Else
            labels(3) = "InverseDFT reproduced original"
            dst3.SetTo(0)
        End If
    End Sub
End Class




' https://www.codeproject.com/Articles/5313198/Customizable-Butterworth-Digital-Filter
' http://breckon.eu/toby/teaching/dip/opencv/lecture_demos/c++/butterworth_lowpass.cpp
' https://github.com/ruohoruotsi/Butterworth-Filter-Design
Public Class DFT_ButterworthFilter_MT : Inherits TaskParent
    Public dft As New DFT_Basics
    Dim options As New Options_DFT
    Public Sub New()
        desc = "Use the Butterworth filter on a DFT image - color image input."
        labels(2) = "Image with Butterworth Low Pass Filter Applied"
        labels(3) = "Same filter with radius / 2"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        dft.Run(src)

        If task.optionsChanged Then
            Parallel.For(0, 2,
                Sub(k)
                    Dim r = options.radius / (k + 1), rNext As Double
                    options.butterworthFilter(k) = New Mat(dft.complexImage.Size(), MatType.CV_32FC2)
                    Dim tmp As New Mat(options.butterworthFilter(k).Size(), MatType.CV_32F, Scalar.All(0))
                    Dim center As New cv.Point(options.butterworthFilter(k).Rows / 2, options.butterworthFilter(k).Cols / 2)
                    For i = 0 To options.butterworthFilter(k).Rows - 1
                        For j = 0 To options.butterworthFilter(k).Cols - 1
                            rNext = Math.Sqrt(Math.Pow(i - center.X, 2) + Math.Pow(j - center.Y, 2))
                            tmp.Set(Of Single)(i, j, 1 / (1 + Math.Pow(rNext / r, 2 * options.order)))
                        Next
                    Next
                    Dim tmpMerge() = {tmp, tmp}
                    Merge(tmpMerge, options.butterworthFilter(k))
                End Sub)
        End If
        Parallel.For(0, 2,
           Sub(k)
               Dim complex As New Mat
               MulSpectrums(options.butterworthFilter(k), dft.complexImage, complex, options.dftFlag)
               If k = 0 Then dst2 = inverseDFT(complex) Else dst3 = inverseDFT(complex)
           End Sub)
    End Sub
End Class







' https://www.codeproject.com/Articles/5313198/Customizable-Butterworth-Digital-Filter
' http://breckon.eu/toby/teaching/dip/opencv/lecture_demos/c++/butterworth_lowpass.cpp
' https://github.com/ruohoruotsi/Butterworth-Filter-Design
Public Class XR_DFT_ButterworthDepth : Inherits TaskParent
    Dim bfilter As New DFT_ButterworthFilter_MT
    Public Sub New()
        desc = "Use the Butterworth filter on a DFT image - RGBDepth as input."
        labels(2) = "Image with Butterworth Low Pass Filter Applied"
        labels(3) = "Same filter with radius / 2"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim _bfilter_cvt As New Mat
        CvtColor(task.depthRGB, _bfilter_cvt, ColorConversionCodes.BGR2GRAY)
        bfilter.Run(_bfilter_cvt)
        dst2 = bfilter.dst2
        dst3 = bfilter.dst3
    End Sub
End Class












Public Class XR_DFT_Shapes : Inherits TaskParent
    Dim dft As New DFT_Basics
    Dim circle As New Draw_Circles
    Dim ellipse As New Draw_Ellipses
    Dim polygon As New Draw_Polygon
    Dim rectangle As New Rectangle_Basics
    Dim lines As New Draw_Lines
    Dim symShapes As New Draw_SymmetricalShapes
    Dim options As New Options_Draw
    Dim optionsDFT As New Options_DFTShape
    Public Sub New()
        OptionParent.FindSlider("DrawCount").Value = 1
        labels = {"Inverse of the DFT - the same grayscale input.", "", "Input to the DFT", "Discrete Fourier Transform Output"}
        desc = "Show the spectrum magnitude for some standard shapes"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Static frm = OptionParent.FindFrm("Options_DFTShape Radio Buttons")
        Select Case OptionParent.findRadioText(frm.check)
            Case "Draw Circle"
                circle.Run(src)
                dst2 = circle.dst2
            Case "Draw Ellipse"
                ellipse.Run(src)
                dst2 = ellipse.dst2
            Case "Draw Line"
                lines.Run(src)
                dst2 = lines.dst2
            Case "Draw Rectangle"
                rectangle.Run(src)
                dst2 = rectangle.dst2
            Case "Draw Polygon"
                polygon.Run(src)
                dst2 = polygon.dst2
            Case "Draw Symmetrical Shapes"
                symShapes.Run(src)
                dst2 = symShapes.dst2
            Case "Draw cv.Point"
                If task.heartBeat Then
                    dst2 = New Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0))
                    Dim pt1 = New cv.Point(msRNG.Next(0, dst2.Width / 10), msRNG.Next(0, dst2.Height / 10))
                    Dim pt2 = New cv.Point(msRNG.Next(0, dst2.Width / 10), msRNG.Next(0, dst2.Height / 10))
                    dst2.Set(Of Byte)(pt1.Y, pt1.X, 255)
                    dst2.Set(Of Byte)(pt2.Y, pt2.X, 255)
                    labels(2) = "pt1 = (" + CStr(pt1.X) + "," + CStr(pt1.Y) + ")  pt2 = (" + CStr(pt2.X) + "," + CStr(pt2.Y) + ")"
                End If
        End Select

        dft.Run(dst2)
        dst3 = dft.dst3

        ' the following line to view the inverse of the DFT transform.
        ' It is the grayscale image of the input - no surprise.  It works!
        dst0 = inverseDFT(dft.complexImage)
    End Sub
End Class
