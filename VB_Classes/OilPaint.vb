Imports cv = OpenCvSharp
Imports System.Windows.Forms

' Source: https://hackernoon.com/https-medium-com-matteoronchetti-pointillism-with-python-and-opencv-f4274e6bbb7b
Public Class OilPaint_Pointilism : Inherits VBparent
    Dim randomMask As cv.Mat
    Dim myRNG As New cv.RNG
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Stroke Scale", 1, 5, 3)
            sliders.setupTrackBar(1, "Smoothing Radius", 0, 100, 32)
        End If
        If radio.Setup(caller, 2) Then
            radio.check(0).Text = "Use Elliptical stroke"
            radio.check(1).Text = "Use Circular stroke"
            radio.check(1).Checked = True
        End If

        task.drawRect = New cv.Rect(dst1.Cols * 3 / 8, dst1.Rows * 3 / 8, dst1.Cols * 2 / 8, dst1.Rows * 2 / 8)
        task.desc = "Alter the image to effect the pointilism style - Painterly Effect"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst1 = src
        Dim img = src(task.drawRect)
        Static saveDrawRect As New cv.Rect
        If saveDrawRect <> task.drawRect Then
            saveDrawRect = task.drawRect
            ' only need to create the mask to order the brush strokes once.
            randomMask = New cv.Mat(img.Size(), cv.MatType.CV_32SC2)
            Dim nPt As New cv.Point
            For y = 0 To randomMask.Height - 1
                For x = 0 To randomMask.Width - 1
                    nPt.X = (msRNG.Next(-1, 1) + x) Mod (randomMask.Width - 1)
                    nPt.Y = (msRNG.Next(-1, 1) + y) Mod (randomMask.Height - 1)
                    If nPt.X < 0 Then nPt.X = 0
                    If nPt.Y < 0 Then nPt.Y = 0
                    randomMask.Set(Of cv.Point)(y, x, nPt)
                Next
            Next
            cv.Cv2.RandShuffle(randomMask, 1.0, myRNG) ' the RNG is not optional.
        End If
        Dim rand = randomMask.Resize(img.Size())
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim fieldx As New cv.Mat, fieldy As New cv.Mat
        cv.Cv2.Scharr(gray, fieldx, cv.MatType.CV_32FC1, 1, 0, 1 / 15.36)
        cv.Cv2.Scharr(gray, fieldy, cv.MatType.CV_32FC1, 0, 1, 1 / 15.36)

        Dim smoothingRadius = sliders.trackbar(1).Value * 2 + 1
        cv.Cv2.GaussianBlur(fieldx, fieldx, New cv.Size(smoothingRadius, smoothingRadius), 0, 0)
        cv.Cv2.GaussianBlur(fieldy, fieldy, New cv.Size(smoothingRadius, smoothingRadius), 0, 0)

        Dim strokeSize = sliders.trackbar(0).Value
        For y = 0 To img.Height - 1
            For x = 0 To img.Width - 1
                Dim nPt = rand.Get(Of cv.Point)(y, x)
                Dim nextColor = src.Get(Of cv.Vec3b)(saveDrawRect.Y + nPt.Y, saveDrawRect.X + nPt.X)
                Dim fx = fieldx(saveDrawRect).Get(Of Single)(nPt.Y, nPt.X)
                Dim fy = fieldy(saveDrawRect).Get(Of Single)(nPt.Y, nPt.X)
                Dim nPoint = New cv.Point2f(nPt.X, nPt.Y)
                Dim gradient_magnitude = Math.Sqrt(fx * fx + fy * fy)
                Dim slen = Math.Round(strokeSize + strokeSize * Math.Sqrt(gradient_magnitude))
                Dim eSize = New cv.Size2f(slen, strokeSize)
                Dim direction = Math.Atan2(fx, fy)
                Dim angle = direction * 180.0 / Math.PI + 90

                Dim rotatedRect = New cv.RotatedRect(nPoint, eSize, angle)
                If radio.check(0).Checked Then
                    dst1(saveDrawRect).Ellipse(rotatedRect, nextColor, -1, task.lineType)
                Else
                    dst1(saveDrawRect).Circle(nPoint, slen / 4, nextColor, -1, task.lineType)
                End If
            Next
        Next
    End Sub
End Class





Public Class OilPaint_ColorProbability : Inherits VBparent
    Public color_probability() As Single
    Public km As New KMeans_BasicsFast
    Dim kSlider As Windows.Forms.TrackBar
    Public Sub New()
        kSlider = findSlider("kMeans k")
        kSlider.Value = 12 ' we would like a dozen colors or so in the color image.
        ReDim color_probability(kSlider.Value - 1)
        label2 = "Color probabilities"
        task.desc = "Determine color probabilities on the output of kMeans - Painterly Effect"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        km.Run(src)
        dst1 = km.dst1
        ReDim color_probability(kSlider.Value - 1)
        For i = 0 To km.km.masks.Count - 1
            color_probability(i) = km.km.masks(i).CountNonZero
        Next

        Dim str = ""
        Dim total = 0.0
        For i = 0 To color_probability.Length - 1
            color_probability(i) /= km.km.masks(0).Total
            str += Format(color_probability(i), "0.0%") + vbCrLf
            total += color_probability(i)
        Next
        str += "Total = " + Format(total, "#0.0%")
        setTrueText(str, 20, 40, 3)
    End Sub
End Class




' https://code.msdn.microsoft.com/Image-Oil-Painting-and-b0977ea9
Public Class OilPaint_ManualVB : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Filter Size", 3, 15, 3)
            sliders.setupTrackBar(1, "Intensity", 5, 150, 25)
        End If
        task.desc = "Alter an image so it appears more like an oil painting - Painterly Effect.  Select a region of interest."
        task.drawRect = New cv.Rect(dst1.Cols * 3 / 8, dst1.Rows * 3 / 8, dst1.Cols * 2 / 8, dst1.Rows * 2 / 8)
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim filtersize = sliders.trackbar(0).Value
        Dim levels = sliders.trackbar(1).Value

        If filtersize Mod 2 = 0 Then filtersize += 1 ' must be odd
        Dim roi = task.drawRect
        src.CopyTo(dst1)
        Dim color = src(roi)
        Dim result1 = color.Clone()
        For y = filtersize To roi.Height - filtersize - 1
            For x = filtersize To roi.Width - filtersize - 1
                Dim intensitybins(levels) As Integer
                Dim bluebin(levels) As Integer
                Dim greenbin(levels) As Integer
                Dim redbin(levels) As Integer
                Dim maxIntensity As Integer = 0
                Dim maxIndex As Integer = 0
                Dim vec As cv.Vec3b = Nothing
                For yy = y - filtersize To y + filtersize - 1
                    For xx = x - filtersize To x + filtersize - 1
                        vec = color.Get(Of cv.Vec3b)(yy, xx)
                        Dim currentIntensity = Math.Round((CSng(vec(0)) + CSng(vec(1)) + CSng(vec(2))) * levels / (255 * 3))
                        intensitybins(currentIntensity) += 1
                        bluebin(currentIntensity) += vec(0)
                        greenbin(currentIntensity) += vec(1)
                        redbin(currentIntensity) += vec(0)

                        If intensitybins(currentIntensity) > maxIntensity Then
                            maxIndex = currentIntensity
                            maxIntensity = intensitybins(currentIntensity)
                        End If
                    Next
                Next

                vec(0) = If((bluebin(maxIndex) / maxIntensity) > 255, 255, bluebin(maxIndex) / maxIntensity)
                vec(1) = If((greenbin(maxIndex) / maxIntensity) > 255, 255, greenbin(maxIndex) / maxIntensity)
                vec(2) = If((redbin(maxIndex) / maxIntensity) > 255, 255, redbin(maxIndex) / maxIntensity)
                result1.Set(Of cv.Vec3b)(y, x, vec)
            Next
        Next
        result1.CopyTo(dst1(roi))
    End Sub
End Class



' https://code.msdn.microsoft.com/Image-Oil-Painting-and-b0977ea9
Public Class OilPaint_Manual : Inherits VBparent
    Dim oilPaint As New CS_Classes.OilPaintManual
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Kernel Size", 2, 10, 4)
            sliders.setupTrackBar(1, "Intensity", 1, 250, 20)
            sliders.setupTrackBar(2, "Threshold", 0, 200, 25) ' add the third slider for the threshold.
        End If
        task.desc = "Alter an image so it appears painted by a pointilist - Painterly Effect.  Select a region of interest to paint."
        label2 = "Selected area only"

        task.drawRect = New cv.Rect(dst1.Cols * 3 / 8, dst1.Rows * 3 / 8, dst1.Cols * 2 / 8, dst1.Rows * 2 / 8)
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim kernelSize = sliders.trackbar(0).Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1
        Dim roi = task.drawRect
        src.CopyTo(dst1)
        oilPaint.Start(src(roi), dst1(roi), kernelSize, sliders.trackbar(1).Value)
        dst2 = src.EmptyClone.SetTo(0)
        Dim factor As Integer = Math.Min(Math.Floor(dst2.Width / roi.Width), Math.Floor(dst2.Height / roi.Height))
        Dim s = New cv.Size(roi.Width * factor, roi.Height * factor)
        cv.Cv2.Resize(dst1(roi), dst2(New cv.Rect(0, 0, s.Width, s.Height)), s)
    End Sub
End Class




' https://code.msdn.microsoft.com/Image-Oil-Painting-and-b0977ea9
Public Class OilPaint_Cartoon : Inherits VBparent
    Dim oil As New OilPaint_Manual
    Dim laplacian As New Edges_Laplacian
    Public Sub New()
        task.drawRect = New cv.Rect(dst1.Cols * 3 / 8, dst1.Rows * 3 / 8, dst1.Cols * 2 / 8, dst1.Rows * 2 / 8)
        task.desc = "Alter an image so it appears more like a cartoon - Painterly Effect"
        label1 = "OilPaint_Cartoon"
        label2 = "Laplacian Edges"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim roi = task.drawRect
        laplacian.Run(src)
        dst2 = laplacian.dst1

        oil.Run(src)
        dst1 = oil.dst1

        Dim threshold = oil.sliders.trackbar(2).Value
        Dim vec000 = New cv.Vec3b(0, 0, 0)
        For y = 0 To roi.Height - 1
            For x = 0 To roi.Width - 1
                If dst2(roi).Get(Of Byte)(y, x) >= threshold Then
                    dst1(roi).Set(Of cv.Vec3b)(y, x, vec000)
                End If
            Next
        Next
    End Sub
End Class


