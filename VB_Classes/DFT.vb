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
    Dim mats As Mat_4to1
    Public magnitude As New cv.Mat
    Public spectrum As New cv.Mat
    Public complexImage As New cv.Mat
    Public gray As cv.Mat
    Public rows As integer
    Public cols As integer
    Public Sub New()
        mats = New Mat_4to1()
        mats.noLines = True

        task.desc = "Explore the Discrete Fourier Transform."
		' task.rank = 1
        label1 = "Image after inverse DFT"
        label2 = "DFT_Basics Spectrum Magnitude"
    End Sub
    Public Sub Run(src as cv.Mat)
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
        mats.Run(Nothing)
        dst2 = mats.dst1

        dst1 = inverseDFT(complexImage)
    End Sub
End Class





' http://opencvexamples.blogspot.com/
Public Class DFT_Inverse : Inherits VBparent
    Dim mats As Mat_2to1
    Public Sub New()
        mats = New Mat_2to1()
        task.desc = "Take the inverse of the Discrete Fourier Transform."
        ' task.rank = 1
        label1 = "Image after Inverse DFT"
    End Sub
    Public Sub Run(src As cv.Mat)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim gray32f As New cv.Mat
        src.ConvertTo(gray32f, cv.MatType.CV_32F)
        Dim planes() = {gray32f, New cv.Mat(gray32f.Size(), cv.MatType.CV_32F, 0)}
        Dim complex As New cv.Mat, complexImage As New cv.Mat
        cv.Cv2.Merge(planes, complex)
        cv.Cv2.Dft(complex, complexImage)

        dst1 = inverseDFT(complexImage)

        Dim diff As New cv.Mat
        cv.Cv2.Absdiff(src, dst1, diff)
        mats.mat(0) = diff.Threshold(0, 255, cv.ThresholdTypes.Binary)
        mats.mat(1) = (diff * 50).ToMat
        mats.Run(src)
        If mats.mat(0).countnonzero() > 0 Then
            dst2 = mats.dst1
            label2 = "Mask of difference (top) and relative diff (bot)"
        Else
            label2 = "InverseDFT reproduced original"
            dst2.SetTo(0)
        End If
    End Sub
End Class





' http://breckon.eu/toby/teaching/dip/opencv/lecture_demos/c++/butterworth_lowpass.cpp
' https://github.com/ruohoruotsi/Butterworth-Filter-Design
Public Class DFT_ButterworthFilter_MT : Inherits VBparent
    Public dft As DFT_Basics
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "DFT B Filter - Radius", 1, dst1.Rows, dst1.Rows)
            sliders.setupTrackBar(1, "DFT B Filter - Order", 1, dst1.Rows, 2)
        End If
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 6)
            radio.check(0).Text = "DFT Flags ComplexOutput"
            radio.check(1).Text = "DFT Flags Inverse"
            radio.check(2).Text = "DFT Flags None"
            radio.check(3).Text = "DFT Flags RealOutput"
            radio.check(4).Text = "DFT Flags Rows"
            radio.check(5).Text = "DFT Flags Scale"
            radio.check(0).Checked = True
        End If

        dft = New DFT_Basics()
        task.desc = "Use the Butterworth filter on a DFT image - color image input."
		' task.rank = 1
        label1 = "Image with Butterworth Low Pass Filter Applied"
        label2 = "Same filter with radius / 2"
    End Sub
    Public Sub Run(src as cv.Mat)
        dft.Run(src)

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
        Static frm = findfrm("DFT_ButterworthFilter_MT Radio Options")
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
           If k = 0 Then dst1 = inverseDFT(complex) Else dst2 = inverseDFT(complex)
       End Sub)
    End Sub
End Class






' http://breckon.eu/toby/teaching/dip/opencv/lecture_demos/c++/butterworth_lowpass.cpp
' https://github.com/ruohoruotsi/Butterworth-Filter-Design
Public Class DFT_ButterworthDepth : Inherits VBparent
    Dim bfilter As DFT_ButterworthFilter_MT
    Public Sub New()
        bfilter = New DFT_ButterworthFilter_MT()

        task.desc = "Use the Butterworth filter on a DFT image - RGBDepth as input."
		' task.rank = 1
        label1 = "Image with Butterworth Low Pass Filter Applied"
        label2 = "Same filter with radius / 2"
    End Sub
    Public Sub Run(src as cv.Mat)
        bfilter.Run(task.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst1 = bfilter.dst1
        dst2 = bfilter.dst2
    End Sub
End Class












Public Class DFT_Shapes : Inherits VBparent
    Dim dft As DFT_Basics
    Dim circle As Draw_Circles
    Dim ellipse As Draw_Ellipses
    Dim polygon As Draw_Polygon
    Dim rectangle As Rectangle_Basics
    Dim lines As Draw_Line
    Dim symShapes As Draw_SymmetricalShapes
    Dim optDraw As Draw_Options
    Public Sub New()
        dft = New DFT_Basics

        optDraw = New Draw_Options

        circle = New Draw_Circles
        ellipse = New Draw_Ellipses
        polygon = New Draw_Polygon
        rectangle = New Rectangle_Basics
        lines = New Draw_Line
        symShapes = New Draw_SymmetricalShapes

        findSlider("DrawCount").Value = 1

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 7)
            radio.check(0).Text = "Draw Circle"
            radio.check(1).Text = "Draw Ellipse"
            radio.check(2).Text = "Draw Rectangle"
            radio.check(3).Text = "Draw Polygon"
            radio.check(4).Text = "Draw Line"
            radio.check(5).Text = "Draw Symmetrical Shapes"
            radio.check(6).Text = "Draw Point"
            radio.check(0).Checked = True
        End If

        label1 = "Input to the DFT"
        label2 = "Discrete Fourier Transform Output"
        task.desc = "Show the spectrum magnitude for some standard shapes. Painterly"
		' task.rank = 1
    End Sub
    Public Sub Run(src As cv.Mat)
        optDraw.Run(Nothing)

        Static circleRadio = findRadio("Draw Circle")
        Static ellipseRadio = findRadio("Draw Ellipse")
        Static lineRadio = findRadio("Draw Line")
        Static rectangleRadio = findRadio("Draw Rectangle")
        Static polygonRadio = findRadio("Draw Polygon")
        Static symShapeRadio = findRadio("Draw Symmetrical Shapes")
        Static pointRadio = findRadio("Draw Point")

        If circleRadio.checked Then
            circle.Run(src)
            dst1 = circle.dst1
        ElseIf ellipseRadio.checked Then
            ellipse.Run(src)
            dst1 = ellipse.dst1
        ElseIf rectangleRadio.checked Then
            rectangle.Run(src)
            dst1 = rectangle.dst1
        ElseIf polygonRadio.checked Then
            polygon.Run(src)
            dst1 = polygon.dst1
        ElseIf symShapeRadio.checked Then
            symShapes.Run(src)
            dst1 = symShapes.dst1
        ElseIf lineRadio.checked Then
            lines.Run(src)
            dst1 = lines.dst1
        ElseIf pointRadio.checked Then
            If task.frameCount Mod optDraw.updateFrequency = 0 Then
                dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
                Dim pt1 = New cv.Point(msRNG.Next(0, dst1.Width / 10), msRNG.Next(0, dst1.Height / 10))
                Dim pt2 = New cv.Point(msRNG.Next(0, dst1.Width / 10), msRNG.Next(0, dst1.Height / 10))
                dst1.Set(Of Byte)(pt1.Y, pt1.X, 255)
                dst1.Set(Of Byte)(pt2.Y, pt2.X, 255)
                label1 = "pt1 = (" + CStr(pt1.X) + "," + CStr(pt1.Y) + ")  pt2 = (" + CStr(pt2.X) + "," + CStr(pt2.Y) + ")"
            End If
        End If

        dft.Run(dst1)
        dst2 = dft.dst2

        ' uncomment the following line to view the inverse of the DFT transform.  It is the grayscale image of the input - no surprise.  It works!
        ' dst1 = inverseDFT(dft.complexImage)
    End Sub
End Class
