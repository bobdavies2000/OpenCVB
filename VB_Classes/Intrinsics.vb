Imports cv = OpenCvSharp
Public Class Intrinsics_Basics : Inherits TaskParent
    Public gc As gcData
    Public ptTranslated As cv.Point2f
    Public ptTranslated3D As cv.Point3f
    Public Sub New()
        desc = "Some cameras don't provide aligned color and left images.  This algorithm aligns the left and color image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim pcTop = task.pointCloud.Get(Of cv.Point3f)(gc.rect.TopLeft.Y, gc.rect.TopLeft.X)
        If pcTop.Z = 0 Then pcTop = getWorldCoordinates(gc.rect.TopLeft, gc.depth)
        If pcTop.Z > 0 Then
            ptTranslated3D.X = task.calibData.rotation(0) * pcTop.X +
                               task.calibData.rotation(1) * pcTop.Y +
                               task.calibData.rotation(2) * pcTop.Z + task.calibData.translation(0)
            ptTranslated3D.Y = task.calibData.rotation(3) * pcTop.X +
                               task.calibData.rotation(4) * pcTop.Y +
                               task.calibData.rotation(5) * pcTop.Z + task.calibData.translation(1)
            ptTranslated3D.Z = task.calibData.rotation(6) * pcTop.X +
                               task.calibData.rotation(7) * pcTop.Y +
                               task.calibData.rotation(8) * pcTop.Z + task.calibData.translation(2)
            ptTranslated.X = task.calibData.leftIntrinsics.fx * ptTranslated3D.X / ptTranslated3D.Z + task.calibData.leftIntrinsics.ppx
            ptTranslated.Y = task.calibData.leftIntrinsics.fy * ptTranslated3D.Y / ptTranslated3D.Z + task.calibData.leftIntrinsics.ppy
        End If
    End Sub
End Class
