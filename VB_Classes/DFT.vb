Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module dft_Module
    Public Function inverseDFT(complexImage As cv.Mat) As cv.Mat
        Dim invDFT As New cv.Mat
        cv.Cv2.Dft(complexImage, invDFT, cv.DftFlags.Inverse Or cv.DftFlags.RealOutput)
        invDFT = invDFT.Normalize(0, 255, cv.NormTypes.MinMax)
        Dim inverse8u As New cv.Mat
        invDFT.ConvertTo(inverse8u, cv.MatType.CV_8U)
        Return inverse8u
    End Function
End Module




' http://stackoverflow.com/questions/19761526/how-to-do-inverse-dft-in-opencv
Public Class DFT_Basics : Inherits VB_Algorithm
    Dim mats As New Mat_4to1
    Public magnitude As New cv.Mat
    Public spectrum As New cv.Mat
    Public complexImage As New cv.Mat
    Public gray As cv.Mat
    Public rows As integer
    Public cols As integer
    Public Sub New()
        mats.lineSeparators = False

        desc = "Explore the Discrete Fourier Transform."
        labels(2) = "Image after inverse DFT"
        labels(3) = "DFT_Basics Spectrum Magnitude"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        gray = src
        If src.Channels = 3 Then gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        rows = cv.Cv2.GetOptimalDFTSize(gray.Rows)
        cols = cv.Cv2.GetOptimalDFTSize(gray.Cols)
        Dim padded = New cv.Mat(gray.Width, gray.Height, cv.MatType.CV_8UC3)
        cv.Cv2.CopyMakeBorder(gray, padded, 0, rows - gray.Rows, 0, cols - gray.Cols, cv.BorderTypes.Constant, cv.Scalar.All(0))
        Dim padded32 As New cv.Mat
        padded.ConvertTo(padded32, cv.MatType.CV_32F)
        Dim planes() = {padded32, New cv.Mat(padded.Size(), cv.MatType.CV_32F, 0)}
        cv.Cv2.Merge(planes, complexImage)
        cv.Cv2.Dft(complexImage, complexImage)

        ' compute the magnitude And switch to logarithmic scale => log(1 + sqrt(Re(DFT(I))^2 + Im(DFT(I))^2))
        planes = complexImage.Split

        cv.Cv2.Magnitude(planes(0), planes(1), magnitude)
        magnitude += cv.Scalar.All(1) ' switch To logarithmic scale
        cv.Cv2.Log(magnitude, magnitude)

        ' crop the spectrum, if it has an odd number of rows Or columns
        spectrum = magnitude(New cv.Rect(0, 0, magnitude.Cols And -2, magnitude.Rows And -2))
        ' Transform the matrix with float values into range 0-255
        spectrum = spectrum.Normalize(0, 255, cv.NormTypes.MinMax)
        spectrum.ConvertTo(padded, cv.MatType.CV_8U)

        ' rearrange the quadrants of Fourier image  so that the origin is at the image center
        Dim cx = CInt(padded.Cols / 2)
        Dim cy = CInt(padded.Rows / 2)

        mats.mat(3) = padded(New cv.Rect(0, 0, cx, cy)).Clone()
        mats.mat(2) = padded(New cv.Rect(cx, 0, cx, cy)).Clone()
        mats.mat(1) = padded(New cv.Rect(0, cy, cx, cy)).Clone()
        mats.mat(0) = padded(New cv.Rect(cx, cy, cx, cy)).Clone()
        mats.Run(empty)
        dst3 = mats.dst2

        dst2 = inverseDFT(complexImage)
    End Sub
End Class





' http://opencvexamples.blogspot.com/
Public Class DFT_Inverse : Inherits VB_Algorithm
    Dim mats As New Mat_2to1
    Public Sub New()
        labels(2) = "Image after Inverse DFT"
        desc = "Take the inverse of the Discrete Fourier Transform."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim gray32f As New cv.Mat
        src.ConvertTo(gray32f, cv.MatType.CV_32F)
        Dim planes() = {gray32f, New cv.Mat(gray32f.Size(), cv.MatType.CV_32F, 0)}
        Dim complex As New cv.Mat, complexImage As New cv.Mat
        cv.Cv2.Merge(planes, complex)
        cv.Cv2.Dft(complex, complexImage)

        dst2 = inverseDFT(complexImage)

        Dim diff As New cv.Mat
        cv.Cv2.Absdiff(src, dst2, diff)
        mats.mat(0) = diff.Threshold(0, 255, cv.ThresholdTypes.Binary)
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
Public Class DFT_ButterworthFilter_MT : Inherits VB_Algorithm
    Public dft As New DFT_Basics
    Dim options As New Options_DFT
    Public Sub New()
        desc = "Use the Butterworth filter on a DFT image - color image input."
        labels(2) = "Image with Butterworth Low Pass Filter Applied"
        labels(3) = "Same filter with radius / 2"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()
        dft.Run(src)

        If task.optionsChanged Then
            Parallel.For(0, 2,
            Sub(k)
                Dim r = options.radius / (k + 1), rNext As Double
                options.butterworthFilter(k) = New cv.Mat(dft.complexImage.Size, cv.MatType.CV_32FC2)
                Dim tmp As New cv.Mat(options.butterworthFilter(k).Size(), cv.MatType.CV_32F, 0)
                Dim center As New cv.Point(options.butterworthFilter(k).Rows / 2, options.butterworthFilter(k).Cols / 2)
                For i = 0 To options.butterworthFilter(k).Rows - 1
                    For j = 0 To options.butterworthFilter(k).Cols - 1
                        rNext = Math.Sqrt(Math.Pow(i - center.X, 2) + Math.Pow(j - center.Y, 2))
                        tmp.Set(Of Single)(i, j, 1 / (1 + Math.Pow(rNext / r, 2 * options.order)))
                    Next
                Next
                Dim tmpMerge() = {tmp, tmp}
                cv.Cv2.Merge(tmpMerge, options.butterworthFilter(k))
            End Sub)
        End If
        Parallel.For(0, 2,
       Sub(k)
           Dim complex As New cv.Mat
           cv.Cv2.MulSpectrums(options.butterworthFilter(k), dft.complexImage, complex, options.dftFlag)
           If k = 0 Then dst2 = inverseDFT(complex) Else dst3 = inverseDFT(complex)
       End Sub)
    End Sub
End Class







' https://www.codeproject.com/Articles/5313198/Customizable-Butterworth-Digital-Filter
' http://breckon.eu/toby/teaching/dip/opencv/lecture_demos/c++/butterworth_lowpass.cpp
' https://github.com/ruohoruotsi/Butterworth-Filter-Design
Public Class DFT_ButterworthDepth : Inherits VB_Algorithm
    Dim bfilter As New DFT_ButterworthFilter_MT
    Public Sub New()
        desc = "Use the Butterworth filter on a DFT image - RGBDepth as input."
        labels(2) = "Image with Butterworth Low Pass Filter Applied"
        labels(3) = "Same filter with radius / 2"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        bfilter.Run(task.depthRGB.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst2 = bfilter.dst2
        dst3 = bfilter.dst3
    End Sub
End Class












Public Class DFT_Shapes : Inherits VB_Algorithm
    Dim dft As New DFT_Basics
    Dim circle As New Draw_Circles
    Dim ellipse As New Draw_Ellipses
    Dim polygon As New Draw_Polygon
    Dim rectangle As New Rectangle_Basics
    Dim lines As New Draw_Lines
    Dim symShapes As New Draw_SymmetricalShapes
    Dim options As New Options_Draw
    Public Sub New()
        findSlider("DrawCount").Value = 1
        If standaloneTest() Then gOptions.displayDst0.Checked = True
        If radio.Setup(traceName) Then
            radio.addRadio("Draw Circle")
            radio.addRadio("Draw Ellipse")
            radio.addRadio("Draw Rectangle")
            radio.addRadio("Draw Polygon")
            radio.addRadio("Draw Line")
            radio.addRadio("Draw Symmetrical Shapes")
            radio.addRadio("Draw Point")
            radio.check(0).Checked = True
        End If
        labels = {"Inverse of the DFT - the same grayscale input.", "", "Input to the DFT", "Discrete Fourier Transform Output"}
        desc = "Show the spectrum magnitude for some standard shapes"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()

        Static frm = findfrm(traceName + " Radio Buttons")
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
                    dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
                    Dim pt1 = New cv.Point(msRNG.Next(0, dst2.Width / 10), msRNG.Next(0, dst2.Height / 10))
                    Dim pt2 = New cv.Point(msRNG.Next(0, dst2.Width / 10), msRNG.Next(0, dst2.Height / 10))
                    dst2.Set(Of Byte)(pt1.Y, pt1.X, 255)
                    dst2.Set(Of Byte)(pt2.Y, pt2.X, 255)
                    labels(2) = "pt1 = (" + CStr(pt1.X) + "," + CStr(pt1.Y) + ")  pt2 = (" + CStr(pt2.X) + "," + CStr(pt2.Y) + ")"
                End If
        End Select

        dft.Run(dst2)
        dst3 = dft.dst3

        ' the following line to view the inverse of the DFT transform.  It is the grayscale image of the input - no surprise.  It works!
        dst0 = inverseDFT(dft.complexImage)
    End Sub
End Class
