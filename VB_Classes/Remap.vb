Imports cv = OpenCvSharp
Public Class Remap_Basics : Inherits VB_Algorithm
    Public direction As Integer = 3 ' default to remap horizontally and vertically
    Public Sub New()
        desc = "Use remap to reflect an image in 4 directions."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim map_x = New cv.Mat(src.Size(), cv.MatType.CV_32F)
        Dim map_y = New cv.Mat(src.Size(), cv.MatType.CV_32F)

        labels(2) = Choose(direction + 1, "Remap_Basics - original", "Remap vertically", "Remap horizontally", "Remap horizontally and vertically")
        ' build a map for use with remap!
        For j = 0 To map_x.Rows - 1
            For i = 0 To map_x.Cols - 1
                Select Case direction
                    Case 0 ' leave the original unmoved!
                        dst2 = src
                    Case 1
                        map_x.Set(Of Single)(j, i, i)
                        map_y.Set(Of Single)(j, i, src.Rows - j)
                    Case 2
                        map_x.Set(Of Single)(j, i, src.Cols - i)
                        map_y.Set(Of Single)(j, i, j)
                    Case 3
                        map_x.Set(Of Single)(j, i, src.Cols - i)
                        map_y.Set(Of Single)(j, i, src.Rows - j)
                End Select
            Next
        Next

        If direction <> 0 Then cv.Cv2.Remap(src, dst2, map_x, map_y, cv.InterpolationFlags.Nearest)

        If heartBeat() Then
            direction += 1
            direction = direction Mod 4
        End If
    End Sub
End Class




Public Class Remap_Flip : Inherits VB_Algorithm
    Public direction = 0
    Public Sub New()
        desc = "Use flip to remap an image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
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
        If heartBeat() Then
            direction += 1
            direction = direction Mod 4
        End If
    End Sub
End Class




