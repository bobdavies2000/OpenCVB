Imports OpenCvSharp
Imports SharpGL
Imports SharpGL.WinForms
Imports cv = OpenCvSharp
Public Class sgl
    Dim gl As OpenGL
    Dim isDragging As Boolean = False
    Dim lastMousePos As cv.Point
    Dim rotationX As Single = 0.0F
    Dim rotationY As Single = 0.0F
    Private Sub GLForm_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.Left = GetSetting("Opencv", "sglLeft", "sglLeft", task.mainFormLocation.X + task.mainFormLocation.Width)
        Me.Top = GetSetting("Opencv", "sglTop", "sglTop", task.mainFormLocation.Y)
        Me.Width = GetSetting("Opencv", "sglWidth", "sglWidth", task.mainFormLocation.Width)
        Me.Height = GetSetting("Opencv", "sglHeight", "sglHeight", task.mainFormLocation.Height)
        gl = OpenglControl1.OpenGL

    End Sub
    Public Sub saveLocation()
        SaveSetting("Opencv", "sglLeft", "sglLeft", Math.Abs(Me.Left))
        SaveSetting("Opencv", "sglTop", "sglTop", Me.Top)
        SaveSetting("Opencv", "sglWidth", "sglWidth", Me.Width)
        SaveSetting("Opencv", "sglHeight", "sglHeight", Me.Height)
    End Sub
    Private Sub OpenGLControl_MouseDown(sender As Object, e As MouseEventArgs) Handles OpenglControl1.MouseDown
        If e.Button = MouseButtons.Left Then
            isDragging = True
            lastMousePos = New cv.Point(e.Location.X, e.Location.Y)
        End If
    End Sub
    Private Sub OpenGLControl_MouseMove(sender As Object, e As MouseEventArgs) Handles OpenglControl1.MouseMove
        If isDragging Then
            Dim dx = e.X - lastMousePos.X
            Dim dy = e.Y - lastMousePos.Y

            rotationY += dx * 0.5F
            rotationX += dy * 0.5F

            lastMousePos = New cv.Point(e.Location.X, e.Location.Y)
        End If
    End Sub
    Private Sub OpenGLControl_MouseUp(sender As Object, e As MouseEventArgs) Handles OpenglControl1.MouseUp
        If e.Button = MouseButtons.Left Then isDragging = False
    End Sub
    Public Sub showPointCloud()
        ' Clear screen and depth buffer
        gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT Or OpenGL.GL_DEPTH_BUFFER_BIT)
        gl.LoadIdentity()

        ' Move the camera back
        gl.Translate(0.0F, 0.0F, -5.0F)
        gl.Rotate(rotationX, 1.0F, 0.0F, 0.0F)
        gl.Rotate(rotationY, 0.0F, 1.0F, 0.0F)

        ' Set point size
        gl.PointSize(5.0F)

        ' Draw the point cloud
        gl.Begin(OpenGL.GL_POINTS)

        For y = 0 To task.pointCloud.Height - 1
            For x = 0 To task.pointCloud.Width - 1
                Dim vec3b = task.color.Get(Of cv.Vec3b)(y, x)
                gl.Color(vec3b(0) / 255, vec3b(1) / 255, vec3b(2) / 255)
                Dim vec As Vec3f = task.pointCloud.At(Of Vec3f)(y, x)
                gl.Vertex(vec.Item0, vec.Item1, vec.Item2)
            Next
        Next

        gl.End()
        gl.Flush()
    End Sub
End Class