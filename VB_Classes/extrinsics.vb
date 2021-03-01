Imports cv = OpenCvSharp
Public Class Extrinsics_Basics
    Inherits VBparent
    Public Sub New()
        initParent()
        task.desc = "Show the depth camera extrinsics."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim nextline = "Rotation MatrixTranslation" + vbCrLf
        Dim fmt = "#0.0000"
        nextline += Format(ocvb.parms.extrinsics.rotation(0), fmt) + vbTab + Format(ocvb.parms.extrinsics.rotation(1), fmt) + vbTab +
                       Format(ocvb.parms.extrinsics.rotation(2), fmt) + vbTab + vbTab + vbTab + Format(ocvb.parms.extrinsics.translation(0), fmt) + vbCrLf

        nextline += Format(ocvb.parms.extrinsics.rotation(3), fmt) + vbTab + Format(ocvb.parms.extrinsics.rotation(4), fmt) + vbTab +
                       Format(ocvb.parms.extrinsics.rotation(5), fmt) + vbTab + vbTab + vbTab + Format(ocvb.parms.extrinsics.translation(1), fmt) + vbCrLf

        nextline += Format(ocvb.parms.extrinsics.rotation(6), fmt) + vbTab + Format(ocvb.parms.extrinsics.rotation(7), fmt) + vbTab +
                       Format(ocvb.parms.extrinsics.rotation(8), fmt) + vbTab + vbTab + vbTab + Format(ocvb.parms.extrinsics.translation(2), fmt) + vbCrLf
        ocvb.trueText(nextline)
    End Sub
End Class


