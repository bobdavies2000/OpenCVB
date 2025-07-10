Imports cv = OpenCvSharp

Public Class newClass_Basics : Inherits TaskParent
    Public Sub New()
        desc = "description"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
    End Sub
End Class


