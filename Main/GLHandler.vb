Imports SharpGL
Imports cv = OpenCvSharp
Namespace OpenCVB
    Partial Class Main : Inherits Form
        Dim gl As OpenGL
        Dim isDragging As Boolean = False
        Dim lastMousePos As cv.Point
        Dim rotationX As Single = 0.0F
        Dim rotationY As Single = 0.0F
        Dim zoomZ As Single = -5.0F
        Dim isPanning As Boolean = False
        Dim panX As Single = 0.0F
        Dim panY As Single = 0.0F
        Public Sub OpenGLControl_MouseDown(sender As Object, e As MouseEventArgs) Handles GLControl.MouseDown
            If e.Button = MouseButtons.Right Then
                isPanning = True
                lastMousePos = New cv.Point(e.Location.X, e.Location.Y)
            End If
            If e.Button = MouseButtons.Left Then
                isDragging = True
                lastMousePos = New cv.Point(e.Location.X, e.Location.Y)
            End If
        End Sub
        Public Sub OpenGLControl_MouseMove(sender As Object, e As MouseEventArgs) Handles GLControl.MouseMove
            If isDragging Then
                Dim dx = e.X - lastMousePos.X
                Dim dy = e.Y - lastMousePos.Y

                rotationY += dx * 0.5F
                rotationX += dy * 0.5F

                lastMousePos = New cv.Point(e.Location.X, e.Location.Y)
            End If
            If isPanning Then
                Dim dx = e.X - lastMousePos.X
                Dim dy = e.Y - lastMousePos.Y

                panX += dx * 0.01F ' Adjust sensitivity as needed
                panY -= dy * 0.01F

                lastMousePos = New cv.Point(e.Location.X, e.Location.Y)
                GLControl.Invalidate()
            End If
        End Sub
        Public Sub OpenGLControl_MouseUp(sender As Object, e As MouseEventArgs) Handles GLControl.MouseUp
            If e.Button = MouseButtons.Left Then isDragging = False
            If e.Button = MouseButtons.Right Then isPanning = False
        End Sub
        Public Sub resetView()
            rotationX = 0.0
            rotationY = 0.0
            zoomZ = -5.0F
        End Sub
        Public Sub OpenGLControl_MouseWheel(sender As Object, e As MouseEventArgs) Handles GLControl.MouseWheel
            Dim delta As Integer = e.Delta
            zoomZ += If(delta > 0, 0.5F, -0.5F)
            GLControl.Invalidate() ' Force redraw
        End Sub
        Public Sub updateSharpGL()
            If gl Is Nothing Then gl = GLControl.OpenGL

            gl.MatrixMode(OpenGL.GL_PROJECTION)
            gl.LoadIdentity()
            gl.Perspective(45, GLControl.Width / GLControl.Height, 0.1, 100)
            gl.MatrixMode(OpenGL.GL_MODELVIEW)

            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT Or OpenGL.GL_DEPTH_BUFFER_BIT)
            gl.LoadIdentity()

            gl.Translate(0.0F, 0.0F, -5.0F)
            gl.Rotate(0.0F, 1.0F, 0.0F, 0.0F)
            gl.Rotate(0.0F, 0.0F, 1.0F, 0.0F)
            gl.PointSize(1.0F)


            gl.MatrixMode(OpenGL.GL_PROJECTION)
            gl.LoadIdentity()
            'gl.Perspective(Options.perspective, GLControl.Width / GLControl.Height,
            '               Options.zNear, Options.zFar)
            gl.Perspective(45, GLControl.Width / GLControl.Height, 0.1, 100)

            gl.MatrixMode(OpenGL.GL_MODELVIEW)

            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT Or OpenGL.GL_DEPTH_BUFFER_BIT)
            gl.LoadIdentity()

            gl.Translate(panX, panY, zoomZ)
            gl.Rotate(rotationX, 1.0F, 0.0F, 0.0F)
            gl.Rotate(rotationY, 0.0F, 1.0F, 0.0F)
            gl.PointSize(1.0F)

            Select Case results.GLRequest
                Case Comm.oCase.drawPointCloudRGB
                    If results.GLcloud Is Nothing Then Exit Sub
                    If results.GLcloud.Size <> OpenCVB.Main.settings.workRes Then
                        results.GLcloud = New cv.Mat(OpenCVB.Main.settings.workRes, cv.MatType.CV_32FC3, 0)
                    End If
                    gl.Begin(OpenGL.GL_POINTS)

                    For y = 0 To settings.workRes.Height - 1
                        For x = 0 To settings.workRes.Width - 1
                            Dim vec As cv.Vec3f = results.GLcloud.At(Of cv.Vec3f)(y, x)
                            If vec(2) <> 0 Then
                                Dim vec3b = results.GLrgb.Get(Of cv.Vec3b)(y, x)
                                gl.Color(vec3b(2) / 255, vec3b(1) / 255, vec3b(0) / 255)
                                gl.Vertex(vec.Item0, -vec.Item1, -vec.Item2)
                            End If
                        Next
                    Next
                    gl.End()
            End Select

            gl.Flush()
        End Sub
    End Class
End Namespace
