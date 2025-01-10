Imports cv = OpenCvSharp
Imports System.Threading
Public Class Texture_Basics : Inherits TaskParent
    Dim ellipse As New Draw_Ellipses
    Public texture As New cv.Mat
    Public tRect As cv.Rect
    Dim texturePop As Integer
    Public tChange As Boolean ' if the texture hasn't changed this will be false.
    Public Sub New()
        task.gOptions.setGridSize(CInt(dst2.Width / 8))

        desc = "find the best sample 256x256 texture of a mask"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If standaloneTest() Or src.Channels() <> 1 Then
            ellipse.Run(src)
            dst2 = ellipse.dst2.CvtColor(cv.ColorConversionCodes.BGR2Gray)
            dst2 = dst2.ConvertScaleAbs(255)
            dst3 = ellipse.dst2.Clone
            dst3.SetTo(cv.Scalar.Yellow, task.gridMask)
        Else
            dst2 = src
        End If

        tChange = True
        If texturePop > 0 Then
            Dim nextCount = dst2(tRect).CountNonZero
            If nextCount >= texturePop * 0.95 Then tChange = False
        End If
        If tChange Then
            Dim sortcounts As New SortedList(Of Integer, cv.Rect)(New compareAllowIdenticalIntegerInverted)
            For Each roi In task.gridRects
                sortcounts.Add(dst2(roi).CountNonZero, roi)
            Next
            If standaloneTest() Then dst3.Rectangle(sortcounts.ElementAt(0).Value, white, 2)
            tRect = sortcounts.ElementAt(0).Value
            texture = task.color(tRect)
            texturePop = dst2(tRect).CountNonZero
        End If
        If standaloneTest() Then dst3.Rectangle(tRect, white, 2)
    End Sub
End Class






Public Class OpenGL_TextureShuffle : Inherits TaskParent
    Dim shuffle As New Random_Shuffle
    Dim floor As New OpenGL_FlatStudy2
    Dim texture As Texture_Basics
    Public tRect As cv.Rect
    Public rgbaTexture As New cv.Mat
    Public Sub New()
        texture = New Texture_Basics()
        desc = "Use random shuffling to homogenize a texture sample of what the floor looks like."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            If dst2.Width = 320 Then
                SetTrueText("Texture_Shuffle is not supported at the 320x240 resolution.  It needs at least 256 rows in the output.")
                Exit Sub
            End If
            floor.plane.Run(src)
            dst3.SetTo(0)
            src.CopyTo(dst3, floor.plane.sliceMask)
            dst2 = floor.plane.dst2
            src = floor.plane.sliceMask
        End If

        texture.Run(src)
        dst2 = texture.dst3
        dst3.Rectangle(texture.tRect, white, task.lineWidth)
        shuffle.Run(texture.texture)
        tRect = New cv.Rect(0, 0, texture.tRect.Width * 4, texture.tRect.Height * 4)
        dst2(tRect) = shuffle.dst2.Repeat(4, 4)
        Dim split = dst2(tRect).Split()
        Dim alpha As New cv.Mat(split(0).Size(), cv.MatType.CV_8U, 1)
        Dim merged() As cv.Mat = {split(2), split(1), split(0), alpha}
        cv.Cv2.Merge(merged, rgbaTexture)
        SetTrueText("Use mouse movement over the image to display results.", 3)
    End Sub
End Class