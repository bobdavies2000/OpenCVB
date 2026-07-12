Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class Remap_Basics : Inherits TaskParent
    Public direction As Integer = 3 ' default to remap horizontally and vertically
    Dim mapx1 As Mat, mapx2 As Mat, mapx3 As Mat
    Dim mapy1 As Mat, mapy2 As Mat, mapy3 As Mat
    Public Sub New()
        mapx1 = New Mat(dst2.Size(), MatType.CV_32F)
        mapy1 = New Mat(dst2.Size(), MatType.CV_32F)
        mapx2 = New Mat(dst2.Size(), MatType.CV_32F)
        mapy2 = New Mat(dst2.Size(), MatType.CV_32F)
        mapx3 = New Mat(dst2.Size(), MatType.CV_32F)
        mapy3 = New Mat(dst2.Size(), MatType.CV_32F)

        For j = 0 To mapx1.Rows - 1
            For i = 0 To mapx1.Cols - 1
                mapx1.Set(Of Single)(j, i, i)
                mapy1.Set(Of Single)(j, i, dst2.Rows - j)
                mapx2.Set(Of Single)(j, i, dst2.Cols - i)
                mapy2.Set(Of Single)(j, i, j)
                mapx3.Set(Of Single)(j, i, dst2.Cols - i)
                mapy3.Set(Of Single)(j, i, dst2.Rows - j)
            Next
        Next

        desc = "Use remap to reflect an image in 4 directions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        labels(2) = Choose(direction + 1, "Remap_Basics - original", "Remap vertically", "Remap horizontally", "Remap horizontally and vertically")

        Select Case direction
            Case 0
                dst2 = src
            Case 1
                Remap(src, dst2, mapx1, mapy1, InterpolationFlags.Nearest)
            Case 2
                Remap(src, dst2, mapx2, mapy2, InterpolationFlags.Nearest)
            Case 3
                Remap(src, dst2, mapx3, mapy3, InterpolationFlags.Nearest)
        End Select

        If task.heartBeat Then
            direction += 1
            direction = direction Mod 4
        End If
    End Sub
End Class




Public Class Remap_Flip : Inherits TaskParent
    Public direction As Integer = 0
    Public Sub New()
        desc = "Use flip to remap an image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        labels(2) = Choose(direction + 1, "Remap_Flip - original", "Remap_Flip - flip horizontal", "Remap_Flip - flip veritical",
                                                "Remap_Flip - flip horizontal and vertical")
        Select Case direction
            Case 0 ' do nothing!
                src.CopyTo(dst2)
            Case 1 ' flip vertically
                Flip(src, dst2, FlipMode.Y)
            Case 2 ' flip horizontally
                Flip(src, dst2, FlipMode.X)
            Case 3 ' flip horizontally and vertically
                Flip(src, dst2, FlipMode.XY)
        End Select
        If task.heartBeat Then
            direction += 1
            direction = direction Mod 4
        End If
    End Sub
End Class
