Imports cv = OpenCvSharp
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes

' https://docs.opencv.org/3.4.1/d2/dc1/camshiftdemo_8cpp-example.html
' https://docs.opencv.org/3.4/d7/d00/tutorial_meanshift.html
Public Class CamShift_Basics : Inherits VBparent
    Public trackBox As New cv.RotatedRect
    Public Sub New()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "CamShift vMin", 0, 255, 32)
            sliders.setupTrackBar(1, "CamShift vMax", 0, 255, 255)
            sliders.setupTrackBar(2, "CamShift Smin", 0, 255, 60)
        End If
        label1 = "Draw anywhere to create histogram and start camshift"
        label2 = "Histogram of targeted region (hue only)"
        task.desc = "CamShift Demo - draw on the images to define the object to track. Tracker Algorithm"
    End Sub
    Public Sub Run(src as cv.Mat)
        Static roi As New cv.Rect
        Static vMinLast As integer
        Static vMaxLast As integer
        Static sBinsLast As cv.Scalar
        Static roi_hist As New cv.Mat
        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim hue = hsv.EmptyClone()

        Static vMinSlider = findSlider("CamShift vMin")
        Static vMaxSlider = findSlider("CamShift vMax")
        Static sMinSlider = findSlider("CamShift Smin")

        Dim hsize() As Integer = {task.histogramBins, task.histogramBins, task.histogramBins}
        Dim ranges() = {New cv.Rangef(0, 180)}
        Dim min = Math.Min(vMinSlider.value, vMaxSlider.Value)
        Dim max = Math.Max(vMinSlider.value, vMaxSlider.Value)
        Dim sbins = New cv.Scalar(0, sMinSlider.Value, min)

        cv.Cv2.MixChannels({hsv}, {hue}, {0, 0})
        Dim mask = hsv.InRange(sbins, New cv.Scalar(180, 255, max))

        If task.drawRect.Width > 0 And task.drawRect.Height > 0 Then
            vMinLast = min
            vMaxLast = max
            sBinsLast = sbins
            If task.drawRect.X + task.drawRect.Width > src.Width Then task.drawRect.Width = src.Width - task.drawRect.X - 1
            If task.drawRect.Y + task.drawRect.Height > src.Height Then task.drawRect.Height = src.Height - task.drawRect.Y - 1
            cv.Cv2.CalcHist(New cv.Mat() {hue(task.drawRect)}, {0, 0}, mask(task.drawRect), roi_hist, 1, hsize, ranges)
            roi_hist = roi_hist.Normalize(0, 255, cv.NormTypes.MinMax)
            roi = task.drawRect
            task.drawRectClear = True
        End If
        If roi_hist.Rows <> 0 Then
            Dim backproj As New cv.Mat
            cv.Cv2.CalcBackProject({hue}, {0, 0}, roi_hist, backproj, ranges)
            cv.Cv2.BitwiseAnd(backproj, mask, backproj)
            trackBox = cv.Cv2.CamShift(backproj, roi, cv.TermCriteria.Both(10, 1))
            Show_HSV_Hist(dst2, roi_hist)
            If dst2.Channels = 1 Then dst2 = src
            dst2 = dst2.CvtColor(cv.ColorConversionCodes.HSV2BGR)
        End If
        dst1.SetTo(0)
        src.CopyTo(dst1, mask)
        If trackBox.Size.Width > 0 Then
            dst1.Ellipse(trackBox, cv.Scalar.White, 2, task.lineType)
        End If
    End Sub
End Class




' https://docs.opencv.org/3.4/d7/d00/tutorial_meanshift.html
Public Class CamShift_Foreground : Inherits VBparent
    Dim camshift As CamShift_Basics
    Dim fore As Depth_Foreground
    Public Sub New()
        camshift = New CamShift_Basics()
        fore = New Depth_Foreground()
        label1 = "Automatically finding the head - top of nearest object"
        task.desc = "Use depth to find the head and start the camshift demo.  Tracker Algorithm"
    End Sub
    Public Sub Run(src as cv.Mat)
        Dim restartRequested As Boolean
        Static depthMin As integer
        Static depthMax As integer
        If camshift.trackBox.Size.Width < 50 Then restartRequested = True
        If task.minDepth <> depthMin Then
            depthMin = task.minDepth
            restartRequested = True
        End If
        If task.maxDepth <> depthMax Then
            depthMax = task.maxDepth
            restartRequested = True
        End If
        If restartRequested Then fore.Run(src)
        camshift.Run(src)
        dst1 = camshift.dst1
    End Sub
End Class






' https://docs.opencv.org/3.4/d7/d00/tutorial_meanshift.html
Public Class Camshift_Object : Inherits VBparent
    Dim blob As Blob_DepthClusters
    Dim camshift As CamShift_Basics
    Public Sub New()
        blob = New Blob_DepthClusters

        camshift = New CamShift_Basics

        label1 = "Largest blob with hue tracked.  Draw enabled."
        label2 = "Backprojection of depth clusters masked with hue"
        task.desc = "Use the blob depth cluster as input to initialize a camshift algorithm.  Tracker Algorithm"
    End Sub
    Public Sub Run(src as cv.Mat)
        blob.Run(src)
        dst2 = blob.dst2.Clone()

        If blob.flood.masks.Count > 0 Then
            Dim largestMask = blob.flood.sortedSizes.ElementAt(0).Value
            If camshift.trackBox.Size.Width > src.Width Or camshift.trackBox.Size.Height > src.Height Then
                task.drawRect = blob.flood.rects(largestMask)
            End If
            If camshift.trackBox.Size.Width < 50 Then task.drawRect = blob.flood.rects(largestMask)
            camshift.Run(src)
            dst1 = camshift.dst1
            Dim mask = dst1.ConvertScaleAbs(255)
            cv.Cv2.BitwiseNot(mask.CvtColor(cv.ColorConversionCodes.BGR2GRAY), mask)
            dst2.SetTo(0, mask)
            If camshift.trackBox.Size.Width > 0 Then dst2.Ellipse(camshift.trackBox, cv.Scalar.White, 2, task.lineType)
        End If
    End Sub
End Class




' https://docs.opencv.org/3.4/d7/d00/tutorial_meanshift.html
Public Class Camshift_TopObjects : Inherits VBparent
    Dim blob As Blob_DepthClusters
    Dim cams(4 - 1) As CamShift_Basics
    Dim mats As Mat_4to1
    Public Sub New()
        mats = New Mat_4to1

        blob = New Blob_DepthClusters
        For i = 0 To cams.Length - 1
            cams(i) = New CamShift_Basics
        Next

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Reinitialize camshift after x frames", 1, 500, 100)
        End If
        task.desc = "Track - Tracker Algorithm"
    End Sub
    Public Sub Run(src as cv.Mat)
        blob.Run(src)
        dst1 = blob.dst2

        Static updateSlider = findSlider("Reinitialize camshift after x frames")
        Dim updateFrequency = updateSlider.Value
        Dim trackBoxes As New List(Of cv.RotatedRect)
        For i = 0 To Math.Min(cams.Length, blob.flood.sortedSizes.Count) - 1
            If blob.flood.maskSizes.Count > i Then
                Dim camIndex = blob.flood.sortedSizes.ElementAt(i).Value
                If task.frameCount Mod updateFrequency = 0 Or cams(i).trackBox.Size.Width = 0 Then
                    task.drawRect = blob.flood.rects(camIndex)
                End If

                cams(i).Run(src)
                mats.mat(i) = cams(i).dst1.Clone()
                trackBoxes.Add(cams(i).trackBox)
            End If
        Next
        For i = 0 To trackBoxes.Count - 1
            dst1.Ellipse(trackBoxes(i), cv.Scalar.White, 2, task.lineType)
        Next
        mats.Run(Nothing)
        dst2 = mats.dst1
    End Sub
End Class


