Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/HOGSample.vb
Public Class HOG_Basics : Inherits TaskParent
    Dim Image As cv.Mat
    Dim ImageProcessed As Boolean
    Dim options As New Options_HOG
    Public Sub New()
        desc = "Find people with Histogram of Gradients (HOG) 2D feature"
        If Image Is Nothing Then Image = ImRead(task.homeDir + "Data/Asahiyama.jpg", cv.ImreadModes.Color)
        Resize(Image, dst3, dst3.Size)
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
            Rectangle(dst2, r.TopLeft, r.BottomRight, cv.Scalar.Red, 3, cv.LineTypes.Link8, 0)
        Next rect
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim hog As New cv.HOGDescriptor()
        hog.SetSVMDetector(cv.HOGDescriptor.GetDefaultPeopleDetector())

        Dim b As Boolean = hog.CheckDetectorSize()
        b.ToString()

        ' run the detector with default parameters. to get a higher hit-rate
        ' (and more false alarms, respectively), decrease the hitThreshold and
        ' groupThreshold (set groupThreshold to 0 to turn off the grouping completely).
        If src.Height = 94 Then Resize(src, src, New cv.Size(src.Width * 2, src.Height * 2))
        Dim found() As cv.Rect = hog.DetectMultiScale(src, options.thresholdHOG, New cv.Size(options.strideHOG, options.strideHOG), New cv.Size(24, 16), options.scaleHOG, 2)
        labels(2) = String.Format("{0} region(s) found", found.Length)
        If dst2.Height = 94 Then Resize(src, dst2, dst2.Size) Else src.CopyTo(dst2)
        drawFoundRectangles(dst2, found)

        If ImageProcessed = False Then
            If dst3.Height = 94 Then Resize(dst3, dst3, New cv.Size(dst3.Width * 2, dst3.Height * 2))
            found = hog.DetectMultiScale(dst3, options.thresholdHOG, New cv.Size(options.strideHOG, options.strideHOG), New cv.Size(24, 16), options.scaleHOG, 2)
            drawFoundRectangles(dst3, found)
            If found.Length > 0 Then
                ImageProcessed = True
                labels(3) = String.Format("{0} region(s) found", found.Length)
            Else
                labels(3) = "Try adjusting slider bars."
            End If
        End If
    End Sub
End Class
