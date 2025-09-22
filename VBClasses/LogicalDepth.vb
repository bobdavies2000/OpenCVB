Imports cv = OpenCvSharp
Public Class LogicalDepth_Bricks : Inherits TaskParent
    Public Sub New()
        If task.contours Is Nothing Then task.contours = New Contour_Basics_List
        desc = "Identify the bricks with the highest correlation of left and right images in each contour."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)

    End Sub
End Class
