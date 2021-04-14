Imports cv = OpenCvSharp
Public Class Extrinsics_Basics
    Inherits VBparent
    Public Sub New()
        initParent()
        task.desc = "Show the depth camera extrinsics."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        Dim nextline = "Rotation MatrixTranslation" + vbCrLf
        Dim fmt = "#0.0000"
        If task.parms.extrinsics.rotation Is Nothing Then
            nextline = "Rotation and translation matrices are missing!"
        Else
            nextline += Format(task.parms.extrinsics.rotation(0), fmt) + vbTab + Format(task.parms.extrinsics.rotation(1), fmt) + vbTab +
                           Format(task.parms.extrinsics.rotation(2), fmt) + vbTab + vbTab + vbTab + Format(task.parms.extrinsics.translation(0), fmt) + vbCrLf

            nextline += Format(task.parms.extrinsics.rotation(3), fmt) + vbTab + Format(task.parms.extrinsics.rotation(4), fmt) + vbTab +
                       Format(task.parms.extrinsics.rotation(5), fmt) + vbTab + vbTab + vbTab + Format(task.parms.extrinsics.translation(1), fmt) + vbCrLf

            nextline += Format(task.parms.extrinsics.rotation(6), fmt) + vbTab + Format(task.parms.extrinsics.rotation(7), fmt) + vbTab +
                           Format(task.parms.extrinsics.rotation(8), fmt) + vbTab + vbTab + vbTab + Format(task.parms.extrinsics.translation(2), fmt) + vbCrLf
        End If
        task.trueText(nextline)
    End Sub
End Class



