Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Module dft_Module
    Public Function inverseDFT(complexImage As cvb.Mat) As cvb.Mat
        Dim invDFT As New cvb.Mat
        cvb.Cv2.Dft(complexImage, invDFT, cvb.DftFlags.Inverse Or cvb.DftFlags.RealOutput)
        invDFT = invDFT.Normalize(0, 255, cvb.NormTypes.MinMax)
        Dim inverse8u As New cvb.Mat
        invDFT.ConvertTo(inverse8u, cvb.MatType.CV_8U)
        Return inverse8u
    End Function
End Module




' http://stackoverflow.com/questions/19761526/how-to-do-inverse-dft-in-opencv
Public Class DFT_Basics : Inherits VB_Parent
    Dim mats As New Mat_4to1
    Public magnitude As New cvb.Mat
    Public spectrum As New cvb.Mat
    Public complexImage As New cvb.Mat
    Public grayMat As cvb.Mat
    Public rows As integer
    Public cols As integer
    Public Sub New()
        mats.lineSeparators = False

        desc = "Explore the Discrete Fourier Transform."
        labels(2) = "Image after inverse DFT"
        labels(3) = "DFT_Basics Spectrum Magnitude"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        grayMat = src
        If src.Channels() = 3 Then grayMat = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        rows = cvb.Cv2.GetOptimalDFTSize(grayMat.Rows)
        cols = cvb.Cv2.GetOptimalDFTSize(grayMat.Cols)
        Dim padded = New cvb.Mat(grayMat.Width, grayMat.Height, cvb.MatType.CV_8UC3)
        cvb.Cv2.CopyMakeBorder(grayMat, padded, 0, rows - grayMat.Rows, 0, cols - grayMat.Cols, cvb.BorderTypes.Constant, cvb.Scalar.All(0))
        Dim padded32 As New cvb.Mat
        padded.ConvertTo(padded32, cvb.MatType.CV_32F)
        Dim planes() = {padded32, New cvb.Mat(padded.Size(), cvb.MatType.CV_32F, cvb.Scalar.All(0))}
        cvb.Cv2.Merge(planes, complexImage)
        cvb.Cv2.Dft(complexImage, complexImage)

        ' compute the magnitude And switch to logarithmic scale => log(1 + sqrt(Re(DFT(I))^2 + Im(DFT(I))^2))
        planes = complexImage.Split

        cvb.Cv2.Magnitude(planes(0), planes(1), magnitude)
        magnitude += cvb.Scalar.All(1) ' switch To logarithmic scale
        cvb.Cv2.Log(magnitude, magnitude)

        ' crop the spectrum, if it has an odd number of rows Or columns
        spectrum = magnitude(New cvb.Rect(0, 0, magnitude.Cols And -2, magnitude.Rows And -2))
        ' Transform the matrix with float values into range 0-255
        spectrum = spectrum.Normalize(0, 255, cvb.NormTypes.MinMax)
        spectrum.ConvertTo(padded, cvb.MatType.CV_8U)

        ' rearrange the quadrants of Fourier image  so that the origin is at the image center
        Dim cx = CInt(padded.Cols / 2)
        Dim cy = CInt(padded.Rows / 2)

        mats.mat(3) = padded(New cvb.Rect(0, 0, cx, cy)).Clone()
        mats.mat(2) = padded(New cvb.Rect(cx, 0, cx, cy)).Clone()
        mats.mat(1) = padded(New cvb.Rect(0, cy, cx, cy)).Clone()
        mats.mat(0) = padded(New cvb.Rect(cx, cy, cx, cy)).Clone()
        mats.Run(empty)
        dst3 = mats.dst2

        dst2 = inverseDFT(complexImage)
    End Sub
End Class





' http://opencvexamples.blogspot.com/
Public Class DFT_Inverse : Inherits VB_Parent
    Dim mats As New Mat_2to1
    Public Sub New()
        labels(2) = "Image after Inverse DFT"
        desc = "Take the inverse of the Discrete Fourier Transform."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Dim gray32f As New cvb.Mat
        src.ConvertTo(gray32f, cvb.MatType.CV_32F)
        Dim planes() = {gray32f, New cvb.Mat(gray32f.Size(), cvb.MatType.CV_32F, cvb.Scalar.All(0))}
        Dim complex As New cvb.Mat, complexImage As New cvb.Mat
        cvb.Cv2.Merge(planes, complex)
        cvb.Cv2.Dft(complex, complexImage)

        dst2 = inverseDFT(complexImage)

        Dim diff As New cvb.Mat
        cvb.Cv2.Absdiff(src, dst2, diff)
        mats.mat(0) = diff.Threshold(0, 255, cvb.ThresholdTypes.Binary)
        mats.mat(1) = (diff * 50).ToMat
        mats.Run(empty)
        If mats.mat(0).CountNonZero > 0 Then
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
Public Class DFT_ButterworthFilter_MT : Inherits VB_Parent
    Public dft As New DFT_Basics
    Dim options As New Options_DFT
    Public Sub New()
        desc = "Use the Butterworth filter on a DFT image - color image input."
        labels(2) = "Image with Butterworth Low Pass Filter Applied"
        labels(3) = "Same filter with radius / 2"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        dft.Run(src)

        If tInfo.optionsChanged Then
            Parallel.For(0, 2,
            Sub(k)
                Dim r = options.radius / (k + 1), rNext As Double
                options.butterworthFilter(k) = New cvb.Mat(dft.complexImage.Size(), cvb.MatType.CV_32FC2)
                Dim tmp As New cvb.Mat(options.butterworthFilter(k).Size(), cvb.MatType.CV_32F, cvb.Scalar.All(0))
                Dim center As New cvb.Point(options.butterworthFilter(k).Rows / 2, options.butterworthFilter(k).Cols / 2)
                For i = 0 To options.butterworthFilter(k).Rows - 1
                    For j = 0 To options.butterworthFilter(k).Cols - 1
                        rNext = Math.Sqrt(Math.Pow(i - center.X, 2) + Math.Pow(j - center.Y, 2))
                        tmp.Set(Of Single)(i, j, 1 / (1 + Math.Pow(rNext / r, 2 * options.order)))
                    Next
                Next
                Dim tmpMerge() = {tmp, tmp}
                cvb.Cv2.Merge(tmpMerge, options.butterworthFilter(k))
            End Sub)
        End If
        Parallel.For(0, 2,
       Sub(k)
           Dim complex As New cvb.Mat
           cvb.Cv2.MulSpectrums(options.butterworthFilter(k), dft.complexImage, complex, options.dftFlag)
           If k = 0 Then dst2 = inverseDFT(complex) Else dst3 = inverseDFT(complex)
       End Sub)
    End Sub
End Class







' https://www.codeproject.com/Articles/5313198/Customizable-Butterworth-Digital-Filter
' http://breckon.eu/toby/teaching/dip/opencv/lecture_demos/c++/butterworth_lowpass.cpp
' https://github.com/ruohoruotsi/Butterworth-Filter-Design
Public Class DFT_ButterworthDepth : Inherits VB_Parent
    Dim bfilter As New DFT_ButterworthFilter_MT
    Public Sub New()
        desc = "Use the Butterworth filter on a DFT image - RGBDepth as input."
        labels(2) = "Image with Butterworth Low Pass Filter Applied"
        labels(3) = "Same filter with radius / 2"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        bfilter.Run(task.depthRGB.CvtColor(cvb.ColorConversionCodes.BGR2GRAY))
        dst2 = bfilter.dst2
        dst3 = bfilter.dst3
    End Sub
End Class












Public Class DFT_Shapes : Inherits VB_Parent
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
        FindSlider("DrawCount").Value = 1
        labels = {"Inverse of the DFT - the same grayscale input.", "", "Input to the DFT", "Discrete Fourier Transform Output"}
        desc = "Show the spectrum magnitude for some standard shapes"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Static frm = FindFrm("Options_DFTShape Radio Buttons")
        Select Case findRadioText(frm.check)
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
            Case "Draw Point"
                If task.heartBeat Then
                    dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
                    Dim pt1 = New cvb.Point(msRNG.Next(0, dst2.Width / 10), msRNG.Next(0, dst2.Height / 10))
                    Dim pt2 = New cvb.Point(msRNG.Next(0, dst2.Width / 10), msRNG.Next(0, dst2.Height / 10))
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
