Imports cv = OpenCvSharp
Imports System.Threading

Namespace CVB
    Public Class CameraZED2
        Private zed As CamZed
        Private workRes As cv.Size
        Private captureRes As cv.Size
        Private ratio As Integer

        Public Property Color As cv.Mat
        Public Property LeftView As cv.Mat
        Public Property RightView As cv.Mat
        Public Property PointCloud As cv.Mat
        Public Property Depth As cv.Mat

        Public Sub New(_workRes As cv.Size, _captureRes As cv.Size, deviceName As String)
            workRes = _workRes
            captureRes = _captureRes
            ratio = CInt(captureRes.Width / workRes.Width)
            zed = New CamZed(workRes, captureRes, deviceName)
        End Sub

        Public Sub GetNextFrame()
            zed.GetNextFrame()

            If workRes <> captureRes Then
                Color = zed.color.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                LeftView = zed.leftView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                RightView = zed.rightView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                PointCloud = zed.pointCloud.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
            Else
                Color = zed.color
                LeftView = zed.leftView
                RightView = zed.rightView
                PointCloud = zed.pointCloud
            End If
        End Sub

        Public Sub StopCamera()
            If zed IsNot Nothing AndAlso PointCloud IsNot Nothing AndAlso PointCloud.Width > 0 Then
                zed.StopCamera()
            End If
        End Sub
    End Class
End Namespace

