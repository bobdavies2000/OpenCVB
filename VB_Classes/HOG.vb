Imports cv = OpenCvSharp
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/HOGSample.vb
Public Class HOG_Basics : Inherits VB_Parent
    Dim staticImage As cv.Mat
    Dim staticImageProcessed As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("HOG Threshold", 0, 100, 0)
            sliders.setupTrackBar("HOG Stride", 1, 100, 1)
            sliders.setupTrackBar("HOG Scale", 0, 2000, 300)
        End If
        desc = "Find people with Histogram of Gradients (HOG) 2D feature"
        staticImage = cv.Cv2.ImRead(task.homeDir + "Data/Asahiyama.jpg", cv.ImreadModes.Color)
        dst3 = staticImage.Resize(dst3.Size)
    End Sub
    Private Sub drawFoundRectangles(dst2 As cv.Mat, found() As cv.Rect)
        For Each rect As cv.Rect In found
            ' the HOG detector returns slightly larger rectangles than the real objects.
            ' so we slightly shrink the rectangles to get a nicer output.
            Dim r As cv.Rect = New cv.Rect With
            {
                .X = rect.X + CInt(Math.Truncate(Math.Round(rect.Width * 0.1))),
                .Y = rect.Y + CInt(Math.Truncate(Math.Round(rect.Height * 0.1))),
                .Width = CInt(Math.Truncate(Math.Round(rect.Width * 0.8))),
                .Height = CInt(Math.Truncate(Math.Round(rect.Height * 0.8)))
            }
            dst2.Rectangle(r.TopLeft, r.BottomRight, cv.Scalar.Red, 3, cv.LineTypes.Link8, 0)
        Next rect
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = FindSlider("HOG Threshold")
        Static strideSlider = FindSlider("HOG Stride")
        Static scaleSlider = FindSlider("HOG Scale")

        Dim hog As New cv.HOGDescriptor()
        hog.SetSVMDetector(cv.HOGDescriptor.GetDefaultPeopleDetector())

        Dim b As Boolean = hog.CheckDetectorSize()
        b.ToString()

        ' run the detector with default parameters. to get a higher hit-rate
        ' (and more false alarms, respectively), decrease the hitThreshold and
        ' groupThreshold (set groupThreshold to 0 to turn off the grouping completely).
        Dim threshold = thresholdSlider.Value
        Dim stride = CInt(strideSlider.Value)
        Dim scale = scaleSlider.Value / 1000
        If src.Height = 94 Then src = src.Resize(New cv.Size(src.Width * 2, src.Height * 2))
        Dim found() As cv.Rect = hog.DetectMultiScale(src, threshold, New cv.Size(stride, stride), New cv.Size(24, 16), scale, 2)
        labels(2) = String.Format("{0} region(s) found", found.Length)
        If dst2.Height = 94 Then dst2 = src.Resize(dst2.Size) Else src.CopyTo(dst2)
        drawFoundRectangles(dst2, found)

        If staticImageProcessed = False Then
            If dst3.Height = 94 Then dst3 = dst3.Resize(New cv.Size(dst3.Width * 2, dst3.Height * 2))
            found = hog.DetectMultiScale(dst3, threshold, New cv.Size(stride, stride), New cv.Size(24, 16), scale, 2)
            drawFoundRectangles(dst3, found)
            If found.Length > 0 Then
                staticImageProcessed = True
                labels(3) = String.Format("{0} region(s) found", found.Length)
            Else
                labels(3) = "Try adjusting slider bars."
            End If
        End If
    End Sub
End Class




