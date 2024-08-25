Imports cvb = OpenCvSharp
Public Class Watershed_Basics : Inherits VB_Parent
    Dim addW As New AddWeighted_Basics
    Dim rects As New List(Of cvb.Rect)
    Public UseCorners As Boolean
    Public Sub New()
        labels(2) = "Draw rectangle to add another marker"
        labels(3) = "Mask for watershed (selected regions)."
        desc = "Watershed API experiment.  Draw on the image to test."
    End Sub
    Public Sub RunVB(src as cvb.Mat)
        If task.drawRect.Width > 0 And task.drawRect.Height > 0 Then rects.Add(task.drawRect)

        If (standaloneTest() Or UseCorners) And task.optionsChanged Then
            For i = 0 To 4 - 1
                Dim r As New cvb.Rect(0, 0, src.Width / 10, src.Height / 10)
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
            Dim markers = New cvb.Mat(src.Size(), cvb.MatType.CV_32S, 0)
            For i = 0 To rects.Count - 1
                markers.Rectangle(rects.ElementAt(i), cvb.Scalar.All(i + 1), -1)
            Next

            cvb.Cv2.Watershed(src, markers)

            markers *= Math.Truncate(255 / rects.Count)
            Dim tmp As New cvb.Mat
            markers.ConvertTo(tmp, cvb.MatType.CV_8U)
            dst3 = ShowPalette(tmp)

            addW.src2 = task.palette.dst2
            addW.Run(src)
            dst2 = addW.dst2
        Else
            dst2 = src
        End If
        task.drawRect = New cvb.Rect
        labels(2) = "There were " + CStr(rects.Count) + " regions defined as input"
    End Sub
End Class







Public Class Watershed_DepthReduction : Inherits VB_Parent
    Dim watershed As New Watershed_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        watershed.UseCorners = True
        labels(3) = "Reduction input to WaterShed"
        desc = "Watershed the depth image using shadow, close, and far points."
    End Sub
    Public Sub RunVB(src as cvb.Mat)
        reduction.Run(task.depthRGB)
        dst3 = reduction.dst3

        watershed.Run(dst3)
        dst2 = watershed.dst2
        labels(2) = watershed.labels(2)
        SetTrueText("Draw anywhere in dst2 to add regions.", 3)
    End Sub
End Class








Public Class Watershed_DepthAuto : Inherits VB_Parent
    Dim watershed As New Watershed_Basics
    Public Sub New()
        watershed.UseCorners = True
        desc = "Watershed the four corners of the depth image."
    End Sub
    Public Sub RunVB(src as cvb.Mat)
        watershed.Run(task.depthRGB)
        dst2 = watershed.dst2
        labels(2) = watershed.labels(2)
    End Sub
End Class


