Imports cv = OpenCvSharp
Public Class MFI_Basics : Inherits VB_Algorithm
    Public motion As New Motion_Rect
    Public Sub New()
        labels(2) = "Motion-filtered image"
        desc = "Motion-Filtered Images (MFI) - update only the changed regions until the next heartbeat"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static color As cv.Mat
        Static pointCloud As cv.Mat
        Static pcSplit() As cv.Mat
        Dim refreshImages As Boolean
        If heartBeat() Then
            refreshImages = True
        Else
            motion.Run(src)
            If task.motionFlag Then ' motionFlag is set when the motion was too much.
                refreshImages = True
            Else
                dst3 = motion.dst0

                If task.motionRect.Width > 0 Then
                    src(task.motionRect).CopyTo(color(task.motionRect))
                    task.pointCloud(task.motionRect).CopyTo(pointCloud(task.motionRect))
                    task.pcSplit(0)(task.motionRect).CopyTo(pcSplit(0)(task.motionRect))
                    task.pcSplit(1)(task.motionRect).CopyTo(pcSplit(1)(task.motionRect))
                    task.pcSplit(2)(task.motionRect).CopyTo(pcSplit(2)(task.motionRect))
                End If
            End If
        End If

        If refreshImages Then
            color = task.color.Clone
            PointCloud = task.pointCloud.Clone
            pcSplit = PointCloud.Split()
        End If

        If gOptions.useMotion.Checked Then
            task.color = color
            task.pointCloud = pointCloud
            task.pcSplit = pcSplit
        End If
        If standalone Then dst2 = color
    End Sub
End Class







Public Class MFI_BasicsOld : Inherits VB_Algorithm
    Public motion As New Motion_Contours
    Public stableImg As cv.Mat
    Dim dMax As New Depth_StableMax
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Use motion-filtered pixel values")
            radio.addRadio("Use original (unchanged) pixels")
            radio.check(0).Checked = True
        End If
        labels(2) = "Motion-filtered image"
        desc = "Motion-Filtered Images (MFI) - update only the changed regions"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        dMax.Run(src)

        motion.Run(task.gray)
        labels(3) = motion.labels(3)
        dst3 = If(motion.dst3.Channels = 1, motion.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR), motion.dst3.Clone)

        Dim radioVal As Integer
        Static frm As OptionsRadioButtons = findfrm(traceName + " Radio Buttons")
        For radioVal = 0 To frm.check.Count - 1
            If frm.check(radioVal).Checked Then Exit For
        Next

        If task.motionReset Or firstPass Or radioVal = 1 Then
            stableImg = src.Clone
        End If

        dst2 = stableImg.Clone
    End Sub
End Class






Public Class MFI_Depth : Inherits VB_Algorithm
    Dim mfi As New MFI_BasicsOld
    Public Sub New()
        labels(2) = "Motion-filtered depth data"
        desc = "Motion-Filtered Images (MFI) - Stabilize the depth image but update any areas with motion"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        mfi.Run(task.pcSplit(2))
        dst2 = mfi.dst2
        dst3 = mfi.dst3
        labels(3) = mfi.labels(3)
    End Sub
End Class






Public Class MFI_PointCloud : Inherits VB_Algorithm
    Dim mfi As New MFI_BasicsOld
    Public Sub New()
        labels(2) = "Motion-filtered PointCloud"
        desc = "Motion-Filtered Images (MFI) - Stabilize the PointCloud but update any areas with motion"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        mfi.Run(task.pointCloud)
        dst2 = mfi.dst2
        dst3 = mfi.dst3
        labels(3) = mfi.labels(3)
    End Sub
End Class








Public Class MFI_Sobel : Inherits VB_Algorithm
    Dim mfi As New MFI_BasicsOld
    Dim sobel As New Edge_Sobel_Old
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Pixel threshold to zero", 0, 255, 100)
        End If

        labels(2) = "Sobel edges of Motion-Filtered RGB"
        desc = "Motion-Filtered Images (MFI) - Stabilize the Sobel output with MFD"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static thresholdSlider = findSlider("Pixel threshold to zero")
        mfi.Run(src)
        dst3 = mfi.dst3
        labels(3) = mfi.labels(3)

        sobel.Run(mfi.dst2)
        dst2 = sobel.dst2.Threshold(thresholdSlider.Value, 0, cv.ThresholdTypes.Tozero).Threshold(0, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class







Public Class MFI_BinarizedSobel : Inherits VB_Algorithm
    Public mfi As New MFI_BasicsOld
    Dim sobel As New Edge_BinarizedSobel
    Public Sub New()
        labels(2) = "Binarized Sobel edges of Motion-Filtered RGB"
        desc = "Motion-Filtered Images (MFI) - Stabilize the binarized Sobel output with MFD"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        mfi.Run(src)

        sobel.Run(mfi.dst2.Clone)

        dst2 = sobel.dst2
        dst3 = sobel.dst3
    End Sub
End Class






Public Class MFI_FloodFill : Inherits VB_Algorithm
    Public maskSizes As New SortedList(Of Integer, Integer)(New CompareMaskSize)
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Public floodPoints As New List(Of cv.Point)
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Dim initialMask As New cv.Mat
    Dim palette As New Palette_RandomColorMap
    Dim sobel As New MFI_BinarizedSobel
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("FloodFill Step Size", 1, dst2.Cols / 2, 15)
            sliders.setupTrackBar("FloodFill point distance from edge", 1, 25, 10)
        End If

        desc = "Motion-Filtered Images (MFI) - Floodfill the image of MFD edges (binarized Sobel output)"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static MFI_OnOffRadio = findRadio("Use motion-filtered pixel values")
        Static stepSlider = findSlider("FloodFill Step Size")
        Static fillSlider = findSlider("FloodFill point distance from edge")
        Dim fill = fillSlider.Value
        Dim stepSize = stepSlider.Value

        Dim input = src.Clone
        If input.Type <> cv.MatType.CV_8UC1 Then
            sobel.Run(src)
            input = sobel.dst3
        End If

        Static saveStepSize As Integer
        Static saveFillDistance As Integer
        Static saveMFI_OnOff = MFI_OnOffRadio.checked
        Dim resetColors As Boolean
        Static imageRects As New List(Of cv.Rect)
        If saveStepSize <> stepSize Or saveFillDistance <> fill Or MFI_OnOffRadio.checked <> saveMFI_OnOff Then
            resetColors = True
            saveStepSize = stepSize
            saveFillDistance = fill
            saveMFI_OnOff = MFI_OnOffRadio.checked
            Dim inputRect As New cv.Rect(0, 0, fill, fill)
            For y = fill To input.Height - fill - 1 Step stepSize
                For x = fill To input.Width - fill - 1 Step stepSize
                    inputRect.X = x
                    inputRect.Y = y
                    imageRects.Add(inputRect)
                Next
            Next
        End If

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
            Dim depthCount = task.pcSplit(2)(rect).CountNonZero
            If edgeCount = 0 And depthCount > depthThreshold Then
                Dim pt = New cv.Point(CInt(rect.X + fill / 2), CInt(rect.Y + fill / 2))
                Dim colorIndex = lastFrame.Get(Of Byte)(pt.Y, pt.X)
                If resetColors Or colorIndex = 0 Then colorIndex = (256 - masks.Count - 1) Mod 256

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
        palette.Run(dst2)
        dst2 = palette.dst2

        dst3 = input.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        For Each pt In floodPoints
            dst3.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        Next

        For Each rect In sobel.mfi.motion.intersect.enclosingRects
            task.color.Rectangle(rect, cv.Scalar.Yellow, 1)
        Next
        labels(3) = sobel.mfi.labels(3)
    End Sub
End Class







Public Class MFI_BasicsNew : Inherits VB_Algorithm
    Public motion As New Motion_MinRect
    Public Sub New()
        labels(2) = "Motion-filtered image"
        desc = "Motion-Filtered Images (MFI) - update only the changed regions color regions until the next heartbeat"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        motion.Run(src)
        task.motionMask = motion.dst3
        dst3 = motion.dst2

        If heartBeat() Then dst2 = src.Clone Else src.CopyTo(dst2, task.motionMask)

        'If task.motionRect.Width > 0 Then
        '    src(task.motionRect).CopyTo(color(task.motionRect))
        '    task.pointCloud(task.motionRect).CopyTo(PointCloud(task.motionRect))
        '    task.pcSplit(0)(task.motionRect).CopyTo(pcSplit(0)(task.motionRect))
        '    task.pcSplit(1)(task.motionRect).CopyTo(pcSplit(1)(task.motionRect))
        '    task.pcSplit(2)(task.motionRect).CopyTo(pcSplit(2)(task.motionRect))
        'End If

        'If refreshImages Then
        '    color = task.color.Clone
        '    pointCloud = task.pointCloud.Clone
        '    pcSplit = pointCloud.Split()
        'End If

        'If gOptions.useMotion.Checked Then
        '    task.color = color
        '    task.pointCloud = pointCloud
        '    task.pcSplit = pcSplit
        'End If
        'If standalone Then dst2 = color
    End Sub
End Class
