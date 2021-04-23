Imports cv = OpenCvSharp
Public Class Watershed_Basics : Inherits VBparent
    Dim addW As AddWeighted_Basics
    Dim rects As New List(Of cv.Rect)
    Public UseCorners As Boolean
    Public Sub New()
        addW = New AddWeighted_Basics
        label1 = "Draw rectangle to add another marker"
        label2 = "Mask for watershed (selected regions)."
        task.desc = "Watershed API experiment.  Draw on the image to test."
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.drawRect.Width > 0 And task.drawRect.Height > 0 Then rects.Add(task.drawRect)

        If (standalone Or UseCorners) And task.frameCount = 0 Then
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
            task.palette.Run(tmp)
            dst2 = task.palette.dst1

            addW.src2 = task.palette.dst1
            addW.Run(src)
            dst1 = addW.dst1
        Else
            dst1 = src
        End If
        task.drawRect = New cv.Rect
        label1 = "There were " + CStr(rects.Count) + " regions defined as input"
    End Sub
End Class







Public Class Watershed_DepthReduction : Inherits VBparent
    Dim watershed As Watershed_Basics
    Dim reduction As Reduction_Basics
    Public Sub New()
        reduction = New Reduction_Basics()
        watershed = New Watershed_Basics()
        watershed.UseCorners = True
        label2 = "Reduction input to WaterShed"
        task.desc = "Watershed the depth image using shadow, close, and far points."
    End Sub
    Public Sub Run(src as cv.Mat)
        reduction.Run(task.RGBDepth)
        dst2 = reduction.dst2

        watershed.Run(dst2)
        dst1 = watershed.dst1
        label1 = watershed.label1
    End Sub
End Class








Public Class Watershed_DepthAuto : Inherits VBparent
    Dim watershed As Watershed_Basics
    Public Sub New()
        watershed = New Watershed_Basics()
        watershed.UseCorners = True
        task.desc = "Watershed the four corners of the depth image."
    End Sub
    Public Sub Run(src as cv.Mat)
        watershed.Run(task.RGBDepth)
        dst1 = watershed.dst1
        label1 = watershed.label1
    End Sub
End Class


