Imports cv = OpenCvSharp
Public Class Cloud_Basics : Inherits TaskParent
    Public Shared ppx = task.calibData.rgbIntrinsics.ppx
    Public Shared ppy = task.calibData.rgbIntrinsics.ppy
    Public Shared fx = task.calibData.rgbIntrinsics.fx
    Public Shared fy = task.calibData.rgbIntrinsics.fy
    Public Sub New()
        desc = "Convert depth values to a point cloud."
    End Sub
    Public Shared Function WorldCoordinates(p As cv.Point3f) As cv.Point3f
        Dim x = (p.X - ppx) / fx
        Dim y = (p.Y - ppy) / fy
        Return New cv.Point3f(x * p.Z, y * p.Z, p.Z)
    End Function
    Public Shared Function WorldCoordinates(p As cv.Point, depth As Single) As cv.Point3f
        Dim x = (p.X - ppx) / fx
        Dim y = (p.Y - ppy) / fy
        Return New cv.Point3f(x * depth, y * depth, depth)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
    End Sub
End Class

