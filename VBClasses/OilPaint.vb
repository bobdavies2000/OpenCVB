Imports cv = OpenCvSharp
Imports System.Windows.Forms

' Source: https://hackernoon.com/https-medium-com-matteoronchetti-pointillism-with-python-and-opencv-f4274e6bbb7b
Namespace VBClasses
    Public Class OilPaint_Pointilism : Inherits TaskParent
        Dim randomMask As cv.Mat
        Dim myRNG As New cv.RNG
        Dim options As New Options_Pointilism
        Dim saveDrawRect As New cv.Rect
        Public Sub New()
            taskAlg.drawRect = New cv.Rect(dst2.Cols * 3 / 8, dst2.Rows * 3 / 8, dst2.Cols * 2 / 8, dst2.Rows * 2 / 8)
            desc = "Alter the image to effect the pointilism style"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst2 = src
            Dim img = src(taskAlg.drawRect)
            If saveDrawRect <> taskAlg.drawRect Then
                saveDrawRect = taskAlg.drawRect
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

            cv.Cv2.GaussianBlur(fieldx, fieldx, New cv.Size(options.smoothingRadius, options.smoothingRadius), 0, 0)
            cv.Cv2.GaussianBlur(fieldy, fieldy, New cv.Size(options.smoothingRadius, options.smoothingRadius), 0, 0)

            For y = 0 To img.Height - 1
                For x = 0 To img.Width - 1
                    Dim nPt = rand.Get(Of cv.Point)(y, x)
                    Dim nextColor = src.Get(Of cv.Vec3b)(saveDrawRect.Y + nPt.Y, saveDrawRect.X + nPt.X)
                    Dim fx = fieldx(saveDrawRect).Get(Of Single)(nPt.Y, nPt.X)
                    Dim fy = fieldy(saveDrawRect).Get(Of Single)(nPt.Y, nPt.X)
                    Dim nPoint = New cv.Point2f(nPt.X, nPt.Y)
                    Dim gradient_magnitude = Math.Sqrt(fx * fx + fy * fy)
                    Dim slen = Math.Round(options.strokeSize + options.strokeSize * Math.Sqrt(gradient_magnitude))
                    Dim eSize = New cv.Size2f(slen, options.strokeSize)
                    Dim direction = Math.Atan2(fx, fy)
                    Dim angle = direction * 180.0 / Math.PI + 90

                    Dim rotatedRect = New cv.RotatedRect(nPoint, eSize, angle)
                    If options.useElliptical Then
                        dst2(saveDrawRect).Ellipse(rotatedRect, nextColor)
                    Else
                        DrawCircle(dst2(saveDrawRect), nPoint, slen / 4, nextColor)
                    End If
                Next
            Next
        End Sub
    End Class







    ' https://code.msdn.microsoft.com/Image-Oil-Painting-and-b0977ea9
    Public Class OilPaint_ManualVB : Inherits TaskParent
        Public options As New Options_OilPaint
        Public Sub New()
            taskAlg.drawRect = New cv.Rect(dst2.Cols * 3 / 8, dst2.Rows * 3 / 8, dst2.Cols * 2 / 8, dst2.Rows * 2 / 8)
            desc = "Alter an image so it appears more like an oil painting.  Select a region of interest."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Options.Run()
            Dim filterKern = options.filterSize Or 1

            Dim roi = taskAlg.drawRect
            src.CopyTo(dst2)
            Dim color = src(roi)
            Dim result1 = color.Clone()
            For y = filterKern To roi.Height - filterKern - 1
                For x = filterKern To roi.Width - filterKern - 1
                    Dim intensitybins(options.intensity) As Integer
                    Dim bluebin(options.intensity) As Integer
                    Dim greenbin(options.intensity) As Integer
                    Dim redbin(options.intensity) As Integer
                    Dim maxIntensity As Integer = 0
                    Dim maxIndex As Integer = 0
                    Dim vec As cv.Vec3b
                    For yy = y - filterKern To y + filterKern - 1
                        For xx = x - filterKern To x + filterKern - 1
                            vec = color.Get(Of cv.Vec3b)(yy, xx)
                            Dim currentIntensity = Math.Round((CSng(vec(0)) + CSng(vec(1)) + CSng(vec(2))) * options.intensity / (255 * 3))
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
            result1.CopyTo(dst2(roi))
        End Sub
    End Class
End Namespace