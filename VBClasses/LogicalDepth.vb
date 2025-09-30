Imports cv = OpenCvSharp
Public Class LogicalDepth_Bricks : Inherits TaskParent
    Dim contours As New Contour_Basics_List
    Public Sub New()
        desc = "Identify the bricks with the highest correlation of left and right images in each contour."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        contours.Run(src)

    End Sub
End Class
