Imports System.Windows.Forms.Design.AxImporter
Imports OpenCvSharp
Imports SharpGL
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
    Dim centerX As Integer
    Dim centerY As Integer
    Dim centerZ As Integer
    Dim upX As Integer
    Dim upY As Integer = 1
    Dim upZ As Integer
    Public options As Options_SharpGL
    Public options2 As Options_SharpGL2
    Private Sub GLForm_Load(sender As Object, e As EventArgs) Handles Me.Load
        options = New Options_SharpGL
        options2 = New Options_SharpGL2
        Me.Left = GetSetting("Opencv", "sglLeft", "sglLeft", task.mainFormLocation.X + task.mainFormLocation.Width)
        Me.Top = GetSetting("Opencv", "sglTop", "sglTop", task.mainFormLocation.Y)
        Me.Width = GetSetting("Opencv", "sglWidth", "sglWidth", task.mainFormLocation.Width)
        Me.Height = GetSetting("Opencv", "sglHeight", "sglHeight", task.mainFormLocation.Height)
        gl = GLControl.OpenGL
    End Sub
    Public Sub saveLocation()
        SaveSetting("Opencv", "sglLeft", "sglLeft", Math.Abs(Me.Left))
        SaveSetting("Opencv", "sglTop", "sglTop", Me.Top)
        SaveSetting("Opencv", "sglWidth", "sglWidth", Me.Width)
        SaveSetting("Opencv", "sglHeight", "sglHeight", Me.Height)
    End Sub
    Private Sub OpenGLControl_MouseDown(sender As Object, e As MouseEventArgs) Handles GLControl.MouseDown
        If e.Button = MouseButtons.Right Then
            isPanning = True
            lastMousePos = New cv.Point(e.Location.X, e.Location.Y)
        End If
        If e.Button = MouseButtons.Left Then
            isDragging = True
            lastMousePos = New cv.Point(e.Location.X, e.Location.Y)
        End If
    End Sub
    Private Sub OpenGLControl_MouseMove(sender As Object, e As MouseEventArgs) Handles GLControl.MouseMove
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
    Private Sub OpenGLControl_MouseUp(sender As Object, e As MouseEventArgs) Handles GLControl.MouseUp
        If e.Button = MouseButtons.Left Then isDragging = False
        If e.Button = MouseButtons.Right Then isPanning = False
    End Sub
    Public Sub resetView()
        Me.Text = "SharpGL - " + If(task.gOptions.GL_LinearMode.Checked, "", "Non") + "Linear mode (see Global Options)"
        rotationX = 0.0
        rotationY = 0.0
        zoomZ = -5.0F
    End Sub
    Private Sub OpenGLControl_MouseWheel(sender As Object, e As MouseEventArgs) Handles GLControl.MouseWheel
        Dim delta As Integer = e.Delta
        zoomZ += If(delta > 0, 0.5F, -0.5F)
        GLControl.Invalidate() ' Force redraw
    End Sub
    Public Function getWorldCoordinates(p As cv.Point, depth As Single) As cv.Point3f
        Dim x = (p.X - task.calibData.rgbIntrinsics.ppx) / task.calibData.rgbIntrinsics.fx
        Dim y = (p.Y - task.calibData.rgbIntrinsics.ppy) / task.calibData.rgbIntrinsics.fy
        Return New cv.Point3f(-x * depth, y * depth, depth)
    End Function
    Private Sub sgl_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        task.closeRequest = True
    End Sub
    Private Function GetMinMax(mat As cv.Mat, Optional mask As cv.Mat = Nothing) As mmData
        Dim mm As mmData
        If mask Is Nothing Then
            mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc)
        Else
            mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc, mask)
        End If

        If Double.IsInfinity(mm.maxVal) Then
            Console.WriteLine("IsInfinity encountered in getMinMax.")
            mm.maxVal = 0 ' skip ...
        End If
        mm.range = mm.maxVal - mm.minVal
        Return mm
    End Function
    Private Function runFunction(func As Integer, pointCloud As cv.Mat, RGB As cv.Mat) As String
        Dim label = ""
        Select Case func
            Case Comm.oCase.drawPointCloudRGB
                If RGB Is Nothing Then RGB = task.color
                gl.Begin(OpenGL.GL_POINTS)

                For y = 0 To task.pointCloud.Height - 1
                    For x = 0 To task.pointCloud.Width - 1
                        Dim vec As cv.Vec3f = task.pointCloud.At(Of cv.Vec3f)(y, x)
                        If vec(2) <> 0 Then
                            Dim vec3b = RGB.Get(Of cv.Vec3b)(y, x)
                            gl.Color(vec3b(2) / 255, vec3b(1) / 255, vec3b(0) / 255)
                            gl.Vertex(vec.Item0, -vec.Item1, -vec.Item2)
                        End If
                    Next
                Next
                gl.End()
                label = CStr(task.pointCloud.Total) + " points were rendered."


            Case Comm.oCase.pcLines
                gl.Begin(OpenGL.GL_POINTS)
                Dim count As Integer, all255 As Boolean
                If RGB Is Nothing Then all255 = True
                For y = 0 To pointCloud.Height - 1
                    For x = 0 To pointCloud.Width - 1
                        Dim vec = pointCloud.Get(Of cv.Vec3f)(y, x)
                        If vec(2) <> 0 Then
                            If all255 Then
                                gl.Color(1.0, 1.0, 1.0)
                            Else
                                Dim vec3b = RGB.Get(Of cv.Vec3b)(y, x)
                                gl.Color(vec3b(2) / 255, vec3b(1) / 255, vec3b(0) / 255)
                            End If
                            gl.Vertex(vec.Item0, -vec.Item1, -vec.Item2)
                            count += 1
                        End If
                    Next
                Next
                gl.End()
                label = CStr(count) + " points were rendered for the selected line(s)."


            Case Comm.oCase.quadBasics, Comm.oCase.readPointCloud
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
                If func = Comm.oCase.readPointCloud Then
                    task.sharpDepth = New cv.Mat(Height, Width, cv.MatType.CV_32F)

                    gl.ReadPixels(0, 0, Width, Height, OpenGL.GL_DEPTH_COMPONENT, OpenGL.GL_FLOAT,
                                  task.sharpDepth.Data)

                    Dim mm1 = GetMinMax(task.sharpDepth)

                    Dim near = options.zNear
                    Dim far = options.zFar
                    Dim a As Single = 2.0F * near * far
                    Dim b As Single = far + near
                    Dim c As Single = far - near

                    ' convert from (0 to 1) to (-1 to 1)
                    task.sharpDepth = task.sharpDepth * 2.0F
                    task.sharpDepth -= 1.0F
                    Dim mm2 = GetMinMax(task.sharpDepth)

                    Dim denom As New Mat()
                    Cv2.Multiply(task.sharpDepth, c, denom)         ' denom = task.sharpDepth * c
                    Cv2.Subtract(b, denom, denom)         ' denom = b - task.sharpDepth * c
                    Cv2.Divide(a, denom, task.sharpDepth)

                    gl.Flush()
                    gl.Finish()
                End If


            Case Comm.oCase.draw3DLines
                label = draw3DLines()


            Case Comm.oCase.draw3DLinesAndCloud
                gl.Begin(OpenGL.GL_POINTS)
                For y = 0 To pointCloud.Height - 1
                    For x = 0 To pointCloud.Width - 1
                        Dim vec = pointCloud.Get(Of cv.Vec3f)(y, x)
                        If vec(2) <> 0 Then
                            gl.Color(1.0, 1.0, 1.0)
                            gl.Vertex(vec.Item0, -vec.Item1, -vec.Item2)
                        End If
                    Next
                Next
                gl.End()

                label = draw3DLines()
        End Select

        gl.Flush()
        Return label
    End Function
    Private Function draw3DLines()
        gl.LineWidth(options.pointSize)

        For Each lp In task.lines.lpList
            Dim color = task.scalarColors(lp.index)
            gl.Color(color(2) / 255, color(1) / 255, color(0) / 255)
            gl.Begin(OpenGL.GL_LINES)
            gl.Vertex(lp.pVec1(0), -lp.pVec1(1), -lp.pVec1(2))
            gl.Vertex(lp.pVec2(0), -lp.pVec2(1), -lp.pVec2(2))
            gl.End()
        Next
        Return task.lines.labels(2)
    End Function
    Public Function RunSharp(func As Integer, Optional pointcloud As cv.Mat = Nothing, Optional RGB As cv.Mat = Nothing) As String
        options.Run()
        options2.Run()

        If task.gOptions.DebugCheckBox.Checked Then
            task.gOptions.DebugCheckBox.Checked = False
            task.sharpGL.resetView()
        End If
        If task.firstPass Or task.optionsChanged Then task.sharpGL.resetView()

        gl.MatrixMode(OpenGL.GL_PROJECTION)
        gl.LoadIdentity()


        If task.gOptions.GL_LinearMode.Checked Then
            gl.Ortho(-task.xRange, task.xRange, -task.yRange, task.yRange, options.zNear, options.zFar)
        Else
            ' gl.Perspective(options.perspective, GLControl.Width / GLControl.Height, options.zNear, options.zFar)
        End If

        gl.MatrixMode(OpenGL.GL_MODELVIEW)
        gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT Or OpenGL.GL_DEPTH_BUFFER_BIT)
        gl.LoadIdentity()

        gl.LookAt(options2.eye(0), options2.eye(1), options2.eye(2), 0, 0, -5, upX, upY, upZ)
        gl.Translate(panX, panY, zoomZ)
        gl.Rotate(rotationX, 1.0F, 0.0F, 0.0F)
        gl.Rotate(rotationY, 0.0F, 1.0F, 0.0F)
        gl.PointSize(options.pointSize)

        Return runFunction(func, pointcloud, RGB)
    End Function
End Class
