Imports cv = OpenCvSharp
' https://docs.opencvb.org/3.4.1/d2/dc1/camshiftdemo_8cpp-example.html
' https://docs.opencvb.org/3.4/d7/d00/tutorial_meanshift.html
Namespace VBClasses
    Public Class CamShift_Basics : Inherits TaskParent
        Public trackBox As New cv.RotatedRect
        Dim redHue As New CamShift_RedHue
        Dim roi As New cv.Rect
        Dim histogram As New cv.Mat
        Public Sub New()
            labels(2) = "Draw anywhere to create histogram and start camshift"
            labels(3) = "Histogram of targeted region (hue only)"
            desc = "CamShift Demo - draw on the images to define the object to track. "
        End Sub
        Public Shared Function Show_HSV_Hist(hist As cv.Mat) As cv.Mat
            Dim img As New cv.Mat(New cv.Size(task.workRes.Width, task.workRes.Height), cv.MatType.CV_8UC3, cv.Scalar.All(0))
            Dim binCount = hist.Height
            Dim binWidth = img.Width / hist.Height
            Dim mm As mmData = GetMinMax(hist)
            img.SetTo(0)
            If mm.maxVal > 0 Then
                For i = 0 To binCount - 2
                    Dim h = img.Height * (hist.Get(Of Single)(i, 0)) / mm.maxVal
                    If h = 0 Then h = 5 ' show the color range in the plot
                    img.Rectangle(New cv.Rect(i * binWidth, img.Height - h, binWidth, h),
                              New cv.Scalar(180.0 * i \ binCount, 255, 255), -1)
                Next
            End If
            Return img
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            redHue.Run(src)
            dst2 = redHue.dst2
            Dim hue = redHue.dst1
            Dim mask = redHue.dst3

            Dim ranges() = {New cv.Rangef(0, 180)}
            Dim hsize() As Integer = {task.histogramBins}
            task.drawRect = ValidateRect(task.drawRect)
            cv.Cv2.CalcHist({hue(task.drawRect)}, {0}, mask(task.drawRect), histogram, 1, hsize, ranges)
            histogram = histogram.Normalize(0, 255, cv.NormTypes.MinMax)
            roi = task.drawRect

            If histogram.Rows <> 0 Then
                cv.Cv2.CalcBackProject({hue}, {0}, histogram, dst1, ranges)
                trackBox = cv.Cv2.CamShift(dst1 And mask, roi, cv.TermCriteria.Both(10, 1))
                dst3 = Show_HSV_Hist(histogram)
                If dst3.Channels() = 1 Then dst3 = src
                dst3 = dst3.CvtColor(cv.ColorConversionCodes.HSV2BGR)
            End If
            If trackBox.Size.Width > 0 Then
                dst2.Ellipse(trackBox, white, task.lineWidth + 1, task.lineType)
            End If
        End Sub
    End Class







    Public Class CamShift_RedHue : Inherits TaskParent
        Dim options As New Options_CamShift
        Public Sub New()
            labels = {"", "Hue", "Image regions with red hue", "Mask for hue regions"}
            desc = "Find that portion of the image where red dominates"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Options.Run()

            Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
            dst3 = hsv.InRange(options.camSBins, New cv.Scalar(180, 255, options.camMax))

            dst2.SetTo(0)
            src.CopyTo(dst2, dst3)
        End Sub
    End Class
End Namespace