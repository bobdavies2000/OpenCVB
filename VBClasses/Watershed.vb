Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Watershed_Basics : Inherits TaskParent
        Dim rects As New List(Of cv.Rect)
        Public UseCorners As Boolean
        Public Sub New()
            labels(2) = "Draw rectangle to add another marker"
            labels(3) = "Mask for watershed (selected regions)."
            desc = "Watershed API experiment.  Draw on the image to test."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.drawRect.Width > 0 And taskAlg.drawRect.Height > 0 Then rects.Add(taskAlg.drawRect)

            If (standaloneTest() Or UseCorners) And taskAlg.optionsChanged Then
                For i = 0 To 4 - 1
                    Dim r As New cv.Rect(0, 0, src.Width / 10, src.Height / 10)
                    Select Case i
                        Case 1
                            r.X = src.Width - src.Width / 10
                        Case 2
                            r.X = src.Width - src.Width / 10
                            r.Y = src.Height - src.Height / 10
                        Case 3
                            r.Y = src.Height - src.Height / 10
                    End Select
                    rects.Add(r)
                Next
            End If

            If rects.Count > 0 Then
                Dim markers = New cv.Mat(src.Size(), cv.MatType.CV_32S, 0)
                For i = 0 To rects.Count - 1
                    markers.Rectangle(rects.ElementAt(i), cv.Scalar.All(i + 1), -1)
                Next

                cv.Cv2.Watershed(src, markers)

                markers *= Math.Truncate(255 / rects.Count)
                Dim tmp As New cv.Mat
                markers.ConvertTo(tmp, cv.MatType.CV_8U)
                dst3 = PaletteFull(tmp)

                dst2 = ShowAddweighted(dst3, src, labels(2))
            Else
                dst2 = src
            End If
            taskAlg.drawRect = New cv.Rect
            labels(2) = "There were " + CStr(rects.Count) + " regions defined as input"
        End Sub
    End Class







    Public Class Watershed_DepthReduction : Inherits TaskParent
        Dim watershed As New Watershed_Basics
        Dim reduction As New Reduction_Basics
        Public Sub New()
            watershed.UseCorners = True
            labels(3) = "Reduction input to WaterShed"
            desc = "Watershed the depth image using shadow, close, and far points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            reduction.Run(taskAlg.depthRGB)
            dst3 = reduction.dst3

            watershed.Run(dst3)
            dst2 = watershed.dst2
            labels(2) = watershed.labels(2)
            SetTrueText("Draw anywhere in dst2 to add regions.", 3)
        End Sub
    End Class








    Public Class Watershed_DepthAuto : Inherits TaskParent
        Dim watershed As New Watershed_Basics
        Public Sub New()
            watershed.UseCorners = True
            desc = "Watershed the four corners of the depth image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            watershed.Run(taskAlg.depthRGB)
            dst2 = watershed.dst2
            labels(2) = watershed.labels(2)
        End Sub
    End Class
End Namespace