Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Remap_Basics : Inherits TaskParent
        Public direction As Integer = 3 ' default to remap horizontally and vertically
        Dim mapx1 As cv.Mat, mapx2 As cv.Mat, mapx3 As cv.Mat
        Dim mapy1 As cv.Mat, mapy2 As cv.Mat, mapy3 As cv.Mat
        Public Sub New()
            mapx1 = New cv.Mat(dst2.Size(), cv.MatType.CV_32F)
            mapy1 = New cv.Mat(dst2.Size(), cv.MatType.CV_32F)
            mapx2 = New cv.Mat(dst2.Size(), cv.MatType.CV_32F)
            mapy2 = New cv.Mat(dst2.Size(), cv.MatType.CV_32F)
            mapx3 = New cv.Mat(dst2.Size(), cv.MatType.CV_32F)
            mapy3 = New cv.Mat(dst2.Size(), cv.MatType.CV_32F)

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
                    cv.Cv2.Remap(src, dst2, mapx1, mapy1, cv.InterpolationFlags.Nearest)
                Case 2
                    cv.Cv2.Remap(src, dst2, mapx2, mapy2, cv.InterpolationFlags.Nearest)
                Case 3
                    cv.Cv2.Remap(src, dst2, mapx3, mapy3, cv.InterpolationFlags.Nearest)
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
                    cv.Cv2.Flip(src, dst2, cv.FlipMode.Y)
                Case 2 ' flip horizontally
                    cv.Cv2.Flip(src, dst2, cv.FlipMode.X)
                Case 3 ' flip horizontally and vertically
                    cv.Cv2.Flip(src, dst2, cv.FlipMode.XY)
            End Select
            If task.heartBeat Then
                direction += 1
                direction = direction Mod 4
            End If
        End Sub
    End Class







    Public Class Flip_Basics : Inherits TaskParent
        Dim flip As New Remap_Flip
        Public Sub New()
            desc = "Placeholder to make it easy to remember 'Remap'."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            flip.Run(src)
            dst2 = flip.dst2
            labels = flip.labels
        End Sub
    End Class
End Namespace