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
Public Class DFT_Basics : Inherits VBparent
    Dim mats As New Mat_4to1
    Public magnitude As New cv.Mat
    Public spectrum As New cv.Mat
    Public complexImage As New cv.Mat
    Public gray As cv.Mat
    Public rows As integer
    Public cols As integer
    Public Sub New()
        mats.lineSeparators = False

        task.desc = "Explore the Discrete Fourier Transform."
        labels(2) = "Image after inverse DFT"
        labels(3) = "DFT_Basics Spectrum Magnitude"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
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
        mats.RunClass(src)
        dst3 = mats.dst2

        dst2 = inverseDFT(complexImage)
    End Sub
End Class





' http://opencvexamples.blogspot.com/
Public Class DFT_Inverse : Inherits VBparent
    Dim mats As New Mat_2to1
    Public Sub New()
        labels(2) = "Image after Inverse DFT"
        task.desc = "Take the inverse of the Discrete Fourier Transform."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
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
        mats.RunClass(src)
        If mats.mat(0).countnonzero() > 0 Then
            dst3 = mats.dst2
            labels(3) = "Mask of difference (top) and relative diff (bot)"
        Else
            labels(3) = "InverseDFT reproduced original"
            dst3.SetTo(0)
        End If
    End Sub
End Class





' http://breckon.eu/toby/teaching/dip/opencv/lecture_demos/c++/butterworth_lowpass.cpp
' https://github.com/ruohoruotsi/Butterworth-Filter-Design
Public Class DFT_ButterworthFilter_MT : Inherits VBparent
    Public dft As New DFT_Basics
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "DFT B Filter - Radius", 1, dst2.Rows, dst2.Rows)
            sliders.setupTrackBar(1, "DFT B Filter - Order", 1, dst2.Rows, 2)
        End If
        If radio.Setup(caller, 6) Then
            radio.check(0).Text = "DFT Flags ComplexOutput"
            radio.check(1).Text = "DFT Flags Inverse"
            radio.check(2).Text = "DFT Flags None"
            radio.check(3).Text = "DFT Flags RealOutput"
            radio.check(4).Text = "DFT Flags Rows"
            radio.check(5).Text = "DFT Flags Scale"
            radio.check(0).Checked = True
        End If

        task.desc = "Use the Butterworth filter on a DFT image - color image input."
        labels(2) = "Image with Butterworth Low Pass Filter Applied"
        labels(3) = "Same filter with radius / 2"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dft.RunClass(src)

        Static radius As Integer
        Static order As Integer
        Static butterworthFilter(1) As cv.Mat
        ' only create the filter if radius or order has changed.
        If radius <> sliders.trackbar(0).Value Or order <> sliders.trackbar(1).Value Then
            radius = sliders.trackbar(0).Value
            order = sliders.trackbar(1).Value

            Parallel.For(0, 2,
            Sub(k)
                Dim r = radius / (k + 1), rNext As Double
                butterworthFilter(k) = New cv.Mat(dft.complexImage.Size, cv.MatType.CV_32FC2)
                Dim tmp As New cv.Mat(butterworthFilter(k).Size(), cv.MatType.CV_32F, 0)
                Dim center As New cv.Point(butterworthFilter(k).Rows / 2, butterworthFilter(k).Cols / 2)
                For i = 0 To butterworthFilter(k).Rows - 1
                    For j = 0 To butterworthFilter(k).Cols - 1
                        rNext = Math.Sqrt(Math.Pow(i - center.X, 2) + Math.Pow(j - center.Y, 2))
                        tmp.Set(Of Single)(i, j, 1 / (1 + Math.Pow(rNext / r, 2 * order)))
                    Next
                Next
                Dim tmpMerge() = {tmp, tmp}
                cv.Cv2.Merge(tmpMerge, butterworthFilter(k))
            End Sub)
        End If

        Dim dftFlag As cv.DctFlags
        Static frm = findfrm(caller + " Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                dftFlag = Choose(i + 1, cv.DftFlags.ComplexOutput, cv.DftFlags.Inverse, cv.DftFlags.None,
                                        cv.DftFlags.RealOutput, cv.DftFlags.Rows, cv.DftFlags.Scale)
            End If
        Next

        Parallel.For(0, 2,
       Sub(k)
           Dim complex As New cv.Mat
           cv.Cv2.MulSpectrums(butterworthFilter(k), dft.complexImage, complex, dftFlag)
           If k = 0 Then dst2 = inverseDFT(complex) Else dst3 = inverseDFT(complex)
       End Sub)
    End Sub
End Class






' http://breckon.eu/toby/teaching/dip/opencv/lecture_demos/c++/butterworth_lowpass.cpp
' https://github.com/ruohoruotsi/Butterworth-Filter-Design
Public Class DFT_ButterworthDepth : Inherits VBparent
    Dim bfilter As New DFT_ButterworthFilter_MT
    Public Sub New()
        task.desc = "Use the Butterworth filter on a DFT image - RGBDepth as input."
        labels(2) = "Image with Butterworth Low Pass Filter Applied"
        labels(3) = "Same filter with radius / 2"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        bfilter.RunClass(task.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst2 = bfilter.dst2
        dst3 = bfilter.dst3
    End Sub
End Class












Public Class DFT_Shapes : Inherits VBparent
    Dim dft As New DFT_Basics
    Dim circle As New Draw_Circles
    Dim ellipse As New Draw_Ellipses
    Dim polygon As New Draw_Polygon
    Dim rectangle As New Rectangle_Basics
    Dim lines As New Draw_Line
    Dim symShapes As New Draw_SymmetricalShapes
    Dim optDraw As New Draw_Options
    Public Sub New()
        findSlider("DrawCount").Value = 1

        If radio.Setup(caller, 7) Then
            radio.check(0).Text = "Draw Circle"
            radio.check(1).Text = "Draw Ellipse"
            radio.check(2).Text = "Draw Rectangle"
            radio.check(3).Text = "Draw Polygon"
            radio.check(4).Text = "Draw Line"
            radio.check(5).Text = "Draw Symmetrical Shapes"
            radio.check(6).Text = "Draw Point"
            radio.check(0).Checked = True
        End If

        labels(2) = "Input to the DFT"
        labels(3) = "Discrete Fourier Transform Output"
        task.desc = "Show the spectrum magnitude for some standard shapes. Painterly"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static circleRadio = findRadio("Draw Circle")
        Static ellipseRadio = findRadio("Draw Ellipse")
        Static lineRadio = findRadio("Draw Line")
        Static rectangleRadio = findRadio("Draw Rectangle")
        Static polygonRadio = findRadio("Draw Polygon")
        Static symShapeRadio = findRadio("Draw Symmetrical Shapes")
        Static pointRadio = findRadio("Draw Point")

        optDraw.RunClass(Nothing)

        If circleRadio.checked Then
            circle.RunClass(src)
            dst2 = circle.dst2
        ElseIf ellipseRadio.checked Then
            ellipse.RunClass(src)
            dst2 = ellipse.dst2
        ElseIf rectangleRadio.checked Then
            rectangle.RunClass(src)
            dst2 = rectangle.dst2
        ElseIf polygonRadio.checked Then
            polygon.RunClass(src)
            dst2 = polygon.dst2
        ElseIf symShapeRadio.checked Then
            symShapes.RunClass(src)
            dst2 = symShapes.dst2
        ElseIf lineRadio.checked Then
            lines.RunClass(src)
            dst2 = lines.dst2
        ElseIf pointRadio.checked Then
            If task.frameCount Mod optDraw.updateFrequency = 0 Then
                dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
                Dim pt1 = New cv.Point(msRNG.Next(0, dst2.Width / 10), msRNG.Next(0, dst2.Height / 10))
                Dim pt2 = New cv.Point(msRNG.Next(0, dst2.Width / 10), msRNG.Next(0, dst2.Height / 10))
                dst2.Set(Of Byte)(pt1.Y, pt1.X, 255)
                dst2.Set(Of Byte)(pt2.Y, pt2.X, 255)
                labels(2) = "pt1 = (" + CStr(pt1.X) + "," + CStr(pt1.Y) + ")  pt2 = (" + CStr(pt2.X) + "," + CStr(pt2.Y) + ")"
            End If
        End If

        dft.RunClass(dst2)
        dst3 = dft.dst3

        ' uncomment the following line to view the inverse of the DFT transform.  It is the grayscale image of the input - no surprise.  It works!
        ' dst2 = inverseDFT(dft.complexImage)
    End Sub
End Class
