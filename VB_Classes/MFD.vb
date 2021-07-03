Imports cv = OpenCvSharp
Public Class MFD_Basics : Inherits VBparent
    Public motion As New Motion_Basics
    Public stableImg As cv.Mat
    Dim dMax As New Depth_SmoothMax
    Public Sub New()
        If radio.Setup(caller, 2) Then
            radio.check(0).Text = "Use motion-filtered pixel values"
            radio.check(1).Text = "Use original (unchanged) pixels"
            radio.check(0).Checked = True
        End If
        labels(2) = "Motion-filtered image"
        task.desc = "Motion-Filtered Data (MFD) - update only the changed regions"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        dMax.RunClass(src)

        motion.RunClass(task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        labels(3) = motion.labels(3)
        dst3 = If(motion.dst3.Channels = 1, motion.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR), motion.dst3.Clone)

        Dim radioVal As Integer
        Static frm As OptionsRadioButtons = findfrm(caller + " Radio Options")
        For radioVal = 0 To frm.check.Length - 1
            If frm.check(radioVal).Checked Then Exit For
        Next

        If motion.resetAll Or stableImg Is Nothing Or radioVal = 1 Then
            stableImg = src.Clone
        Else
            For Each rect In motion.intersect.enclosingRects
                If rect.Width And rect.Height Then src(rect).CopyTo(stableImg(rect))
            Next
        End If

        dst2 = stableImg.Clone
    End Sub
End Class






Public Class MFD_Depth : Inherits VBparent
    Dim mfd As New MFD_Basics
    Public Sub New()
        labels(2) = "Motion-filtered depth data"
        task.desc = "Motion-Filtered Data (MFD) - Stabilize the depth image but update any areas with motion"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        mfd.RunClass(task.depth32f)
        dst2 = mfd.dst2
        dst3 = mfd.dst3
        labels(3) = mfd.labels(3)
    End Sub
End Class






Public Class MFD_PointCloud : Inherits VBparent
    Dim mfd As New MFD_Basics
    Public Sub New()
        labels(2) = "Motion-filtered PointCloud"
        task.desc = "Motion-Filtered Data (MFD) - Stabilize the PointCloud but update any areas with motion"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        mfd.RunClass(task.pointCloud)
        dst2 = mfd.dst2
        dst3 = mfd.dst3
        labels(3) = mfd.labels(3)
    End Sub
End Class








Public Class MFD_Sobel : Inherits VBparent
    Dim mfd As New MFD_Basics
    Dim sobel As New Edges_Sobel
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Pixel threshold to zero", 0, 255, 100)
        End If

        labels(2) = "Sobel edges of Motion-Filtered RGB"
        task.desc = "Motion-Filtered Data (MFD) - Stabilize the Sobel output with MFD"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static thresholdSlider = findSlider("Pixel threshold to zero")
        mfd.RunClass(src)
        dst3 = mfd.dst3
        labels(3) = mfd.labels(3)

        sobel.RunClass(mfd.dst2)
        dst2 = sobel.dst2.Threshold(thresholdSlider.value, 0, cv.ThresholdTypes.Tozero).Threshold(0, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class







Public Class MFD_BinarizedSobel : Inherits VBparent
    Public mfd As New MFD_Basics
    Dim sobel As New Edges_BinarizedSobel
    Public Sub New()
        labels(2) = "Binarized Sobel edges of Motion-Filtered RGB"
        task.desc = "Motion-Filtered Data (MFD) - Stabilize the binarized Sobel output with MFD"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        mfd.RunClass(src)

        sobel.RunClass(mfd.dst2.Clone)

        dst2 = sobel.dst2
        dst3 = sobel.dst3
    End Sub
End Class






Public Class MFD_FloodFill : Inherits VBparent
    Public maskSizes As New SortedList(Of Integer, Integer)(New CompareMaskSize)
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Public floodPoints As New List(Of cv.Point)
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Dim initialMask As New cv.Mat
    Dim palette As New Palette_RandomColorMap
    Dim sobel As New MFD_BinarizedSobel
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "FloodFill Step Size", 1, dst2.Cols / 2, 15)
            sliders.setupTrackBar(1, "FloodFill point distance from edge", 1, 25, 10)
        End If

        task.desc = "Motion-Filtered Data (MFD) - Floodfill the image of MFD edges (binarized Sobel output)"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static MFD_OnOffRadio = findRadio("Use motion-filtered pixel values")
        Static stepSlider = findSlider("FloodFill Step Size")
        Static fillSlider = findSlider("FloodFill point distance from edge")
        Dim fill = fillSlider.value
        Dim stepSize = stepSlider.Value

        Dim input = src.Clone
        If input.Type <> cv.MatType.CV_8UC1 Then
            sobel.RunClass(src)
            input = sobel.dst3.Clone
        End If

        Static saveStepSize As Integer
        Static saveFillDistance As Integer
        Static saveMFD_OnOff = MFD_OnOffRadio.checked
        Dim resetColors As Boolean
        Static imageRects As New List(Of cv.Rect)
        If saveStepSize <> stepSize Or saveFillDistance <> fill Or MFD_OnOffRadio.checked <> saveMFD_OnOff Then
            resetColors = True
            saveStepSize = stepSize
            saveFillDistance = fill
            saveMFD_OnOff = MFD_OnOffRadio.checked
            Dim inputRect As New cv.Rect(0, 0, fill, fill)
            For y = fill To input.Height - fill - 1 Step stepSize
                For x = fill To input.Width - fill - 1 Step stepSize
                    inputRect.X = x
                    inputRect.Y = y
                    imageRects.Add(inputRect)
                Next
            Next
        End If

        If task.mouseClickFlag And task.mousePicTag = RESULT_DST2 Then setMyActiveMat()

        masks.Clear()
        maskSizes.Clear()
        rects.Clear()
        centroids.Clear()

        Static zero As New cv.Scalar(0)
        Static maskRect = New cv.Rect(1, 1, input.Width, input.Height)

        Dim maskPlus = New cv.Mat(New cv.Size(input.Width + 2, input.Height + 2), cv.MatType.CV_8UC1, 0)
        floodPoints.Clear()

        Dim depthThreshold = fill * fill / 2
        Static lastFrame = input.Clone
        dst2 = input.Clone
        For Each rect In imageRects
            Dim edgeCount = input(rect).CountNonZero
            Dim depthCount = task.depth32f(rect).CountNonZero
            If edgeCount = 0 And depthCount > depthThreshold Then
                Dim pt = New cv.Point(CInt(rect.X + fill / 2), CInt(rect.Y + fill / 2))
                Dim colorIndex = lastFrame.Get(Of Byte)(pt.Y, pt.X)
                If resetColors Or colorIndex = 0 Then colorIndex = (255 - masks.Count - 1) Mod 255

                Dim floodRect As cv.Rect
                Dim pixelCount = cv.Cv2.FloodFill(dst2, maskPlus, pt, cv.Scalar.All(colorIndex), floodRect, zero, zero, floodFlag Or (255 << 8))
                If floodRect.Width And floodRect.Height Then
                    floodPoints.Add(pt)
                    Dim m = cv.Cv2.Moments(maskPlus(rect), True)
                    Dim centroid = New cv.Point2f(rect.X + m.M10 / m.M00, rect.Y + m.M01 / m.M00)
                    maskSizes.Add(pixelCount, masks.Count)
                    masks.Add(maskPlus(maskRect)(rect))
                    rects.Add(rect)
                    centroids.Add(centroid)
                End If
            End If
        Next

        lastFrame = dst2.Clone
        palette.RunClass(dst2)
        dst2 = palette.dst2

        dst3 = input.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        For Each pt In floodPoints
            dst3.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        Next

        For Each rect In sobel.mfd.motion.intersect.enclosingRects
            dst3.Rectangle(rect, cv.Scalar.Yellow, 1)
        Next
        labels(3) = sobel.mfd.labels(3)
    End Sub
End Class


