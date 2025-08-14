Imports System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar
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
    Dim zoomZ As Single = -5.0F
    Dim isPanning As Boolean = False
    Dim panX As Single = 0.0F
    Dim panY As Single = 0.0F
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
        If e.Button = MouseButtons.Right Then
            isPanning = True
            lastMousePos = New cv.Point(e.Location.X, e.Location.Y)
        End If
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
        If isPanning Then
            Dim dx = e.X - lastMousePos.X
            Dim dy = e.Y - lastMousePos.Y

            panX += dx * 0.01F ' Adjust sensitivity as needed
            panY -= dy * 0.01F

            lastMousePos = New cv.Point(e.Location.X, e.Location.Y)
            OpenglControl1.Invalidate()
        End If
    End Sub
    Private Sub OpenGLControl_MouseUp(sender As Object, e As MouseEventArgs) Handles OpenglControl1.MouseUp
        If e.Button = MouseButtons.Left Then isDragging = False
        If e.Button = MouseButtons.Right Then isPanning = False
    End Sub
    Public Sub resetView()
        rotationX = 0.0
        rotationY = 0.0
        zoomZ = -5.0F
    End Sub
    Private Sub OpenGLControl_MouseWheel(sender As Object, e As MouseEventArgs) Handles OpenglControl1.MouseWheel
        Dim delta As Integer = e.Delta
        zoomZ += If(delta > 0, 0.5F, -0.5F)
        OpenglControl1.Invalidate() ' Force redraw
    End Sub
    Public Function getWorldCoordinates(p As cv.Point, depth As Single) As cv.Point3f
        Dim x = (p.X - task.calibData.rgbIntrinsics.ppx) / task.calibData.rgbIntrinsics.fx
        Dim y = (p.Y - task.calibData.rgbIntrinsics.ppy) / task.calibData.rgbIntrinsics.fy
        Return New cv.Point3f(-x * depth, y * depth, depth)
    End Function
    Public Function showSharpGL(func As Integer) As String
        gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT Or OpenGL.GL_DEPTH_BUFFER_BIT)
        gl.LoadIdentity()

        gl.Translate(panX, panY, zoomZ)
        gl.Rotate(rotationX, 1.0F, 0.0F, 0.0F)
        gl.Rotate(rotationY, 0.0F, 1.0F, 0.0F)
        gl.PointSize(2.0F)

        Dim label = ""
        Select Case func
            Case oCase.drawPointCloudRGB
                gl.Begin(OpenGL.GL_POINTS)

                For y = 0 To task.pointCloud.Height - 1
                    For x = 0 To task.pointCloud.Width - 1
                        Dim vec3b = task.color.Get(Of cv.Vec3b)(y, x)
                        gl.Color(vec3b(2) / 255, vec3b(1) / 255, vec3b(0) / 255)
                        Dim vec As Vec3f = task.pointCloud.At(Of Vec3f)(y, x)
                        gl.Vertex(vec.Item0, -vec.Item1, -vec.Item2)
                    Next
                Next
                gl.End()
                label = CStr(task.pointCloud.Total) + " points were rendered."
            Case oCase.quadBasics
                gl.Begin(OpenGL.GL_QUADS)

                Dim count As Integer
                For i = 0 To task.gridRects.Count - 1
                    Dim rect = task.gridRects(i)
                    Dim depth = -task.pcSplit(2)(rect).Mean(task.depthMask(rect))(0)
                    If depth = 0 Then Continue For
                    count += 1
                    Dim color = task.color(rect).Mean()

                    gl.Color(CSng(color(2) / 255), CSng(color(1) / 255), CSng(color(0) / 255))
                    Dim p0 = getWorldCoordinates(rect.TopLeft, depth)
                    Dim p1 = getWorldCoordinates(rect.BottomRight, depth)
                    gl.Vertex(p0.X, p0.Y, depth)
                    gl.Vertex(p1.X, p0.Y, depth)
                    gl.Vertex(p1.X, p1.Y, depth)
                    gl.Vertex(p0.X, p1.Y, depth)
                Next

                gl.End()
                label = CStr(count) + " grid rects had depth."
        End Select

        gl.Flush()
        Return label
    End Function
    Private Sub sgl_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        task.closeRequest = True
    End Sub
End Class