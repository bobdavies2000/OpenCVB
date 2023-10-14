Imports cv = OpenCvSharp
Imports  System.IO
Imports System.Runtime.InteropServices
Imports  System.IO.MemoryMappedFiles
Imports  System.IO.Pipes

' https://docs.opencv.org/3.4.1/d2/dc1/camshiftdemo_8cpp-example.html
' https://docs.opencv.org/3.4/d7/d00/tutorial_meanshift.html
Public Class CamShift_Basics : Inherits VB_Algorithm
    Public trackBox As New cv.RotatedRect
    Dim options As New Options_CamShift
    Dim redHue As New CamShift_RedHue
    Public Sub New()
        labels(2) = "Draw anywhere to create histogram and start camshift"
        labels(3) = "Histogram of targeted region (hue only)"
        desc = "CamShift Demo - draw on the images to define the object to track. "
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()
        Static roi As New cv.Rect
        Static histogram As New cv.Mat

        redHue.Run(src)
        dst2 = redHue.dst2
        Dim hue = redHue.dst1
        Dim mask = redHue.dst3

        Dim ranges() = {New cv.Rangef(0, 180)}
        Dim hsize() As Integer = {task.histogramBins}
        task.drawRect = validateRect(task.drawRect)
        cv.Cv2.CalcHist({hue(task.drawRect)}, {0}, mask(task.drawRect), histogram, 1, hsize, ranges)
        histogram = histogram.Normalize(0, 255, cv.NormTypes.MinMax)
        roi = task.drawRect

        If histogram.Rows <> 0 Then
            cv.Cv2.CalcBackProject({hue}, {0}, histogram, dst1, ranges)
            trackBox = cv.Cv2.CamShift(dst1 And mask, roi, cv.TermCriteria.Both(10, 1))
            dst3 = Show_HSV_Hist(histogram)
            If dst3.Channels = 1 Then dst3 = src
            dst3 = dst3.CvtColor(cv.ColorConversionCodes.HSV2BGR)
        End If
        If trackBox.Size.Width > 0 Then
            dst2.Ellipse(trackBox, cv.Scalar.White, task.lineWidth + 1, task.lineType)
        End If
    End Sub
End Class






' https://docs.opencv.org/3.4/d7/d00/tutorial_meanshift.html
Public Class CamShift_Foreground : Inherits VB_Algorithm
    Dim camshift As New CamShift_Basics
    Dim fore As New Depth_Foreground
    Dim flood As New Flood_PointList
    Public Sub New()
        labels(2) = "Draw anywhere to start Camshift"
        labels(3) = "The foreground BGR from depth data"
        desc = "Use depth to isolate foreground for use with camshift demo."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        dst0.SetTo(cv.Scalar.All(1))
        fore.Run(src)
        src.CopyTo(dst0, fore.dst2)
        flood.Run(dst0)
        If flood.redCells.Count > 1 Then
            Dim rc = flood.redCells(1)
            If camshift.trackBox.Size.Width / dst2.Width < 0.5 Then task.drawRect = rc.rect
            camshift.Run(src)
            dst2 = camshift.dst2
            dst3 = camshift.dst3
        End If
    End Sub
End Class







Public Class CamShift_RedHue : Inherits VB_Algorithm
    Dim options As New Options_CamShift
    Public Sub New()
        labels = {"", "Hue", "Image regions with red hue", "Mask for hue regions"}
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Find that portion of the image where red dominates"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()

        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)

        cv.Cv2.MixChannels({hsv}, {dst1}, {0, 0})
        dst3 = hsv.InRange(options.camSBins, New cv.Scalar(180, 255, options.camMax))

        dst2.SetTo(0)
        src.CopyTo(dst2, dst3)
    End Sub
End Class