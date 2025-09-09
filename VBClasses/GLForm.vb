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
    Public ppx = task.calibData.rgbIntrinsics.ppx
    Public ppy = task.calibData.rgbIntrinsics.ppy
    Public fx = task.calibData.rgbIntrinsics.fx
    Public fy = task.calibData.rgbIntrinsics.fy
    Dim mmZ As mmData
    Dim zNear As Single
    Dim zFar As Single
    Private Sub GLForm_Load(sender As Object, e As EventArgs) Handles Me.Load
        options = New Options_SharpGL
        options2 = New Options_SharpGL2
        Me.Left = GetSetting("Opencv", "sglLeft", "sglLeft", task.mainFormLocation.X + task.mainFormLocation.Width)
        Me.Top = GetSetting("Opencv", "sglTop", "sglTop", task.mainFormLocation.Y)
        Me.Width = task.workRes.Width + 50
        Me.Height = task.workRes.Height + 64
        'Me.Width = GetSetting("Opencv", "sglWidth", "sglWidth", task.mainFormLocation.Width)
        'Me.Height = GetSetting("Opencv", "sglHeight", "sglHeight", task.mainFormLocation.Height)
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
    Private Function drawQuads() As String
        gl.Begin(OpenGL.GL_QUADS)

        Dim count As Integer
        For i = 0 To task.gridRects.Count - 1
            Dim rect = task.gridRects(i)
            Dim depth = -task.pcSplit(2)(rect).Mean(task.depthMask(rect))(0)
            If depth = 0 Then Continue For
            count += 1
            Dim color = task.color(rect).Mean()

            gl.Color(CSng(color(2) / 255), CSng(color(1) / 255), CSng(color(0) / 255))
            Dim p0 = Cloud_Basics.worldCoordinates(rect.TopLeft, depth)
            Dim p1 = Cloud_Basics.worldCoordinates(rect.BottomRight, depth)
            gl.Vertex(p0.X, p0.Y, depth)
            gl.Vertex(p1.X, p0.Y, depth)
            gl.Vertex(p1.X, p1.Y, depth)
            gl.Vertex(p0.X, p1.Y, depth)
        Next

        gl.End()
        Return CStr(count) + " grid rects had depth."
    End Function
    Private Function drawCloud(pc As cv.Mat, rgb As cv.Mat) As String
        gl.Begin(OpenGL.GL_POINTS)
        Dim count As Integer
        'gl.StencilFunc(CType(OpenGL.GL_ALWAYS, Enumerations.StencilFunction), CInt(1), &HFF) ' Always pass stencil test
        'gl.StencilOp(OpenGL.GL_KEEP, OpenGL.GL_KEEP, OpenGL.GL_REPLACE) ' Replace stencil with 1 on depth pass
        For y = 0 To pc.Height - 1
            For x = 0 To pc.Width - 1
                If task.depthMask.Get(Of Byte)(y, x) <> 0 Then
                    Dim vec As cv.Vec3f = pc.At(Of cv.Vec3f)(y, x)
                    Dim vec3b = rgb.Get(Of cv.Vec3b)(y, x)
                    gl.Color(vec3b(2) / 255, vec3b(1) / 255, vec3b(0) / 255)
                    gl.Vertex(vec.Item0, -vec.Item1, -vec.Item2)
                    count += 1
                End If
            Next
        Next
        gl.End()
        Return CStr(count) + " of " + CStr(pc.Total) + " points were rendered."
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
    'Public Function worldCoordinateInverse(mask As cv.Mat, depth As cv.Mat) As cv.Mat
    '    Dim dst As New cv.Mat(depth.Size, cv.MatType.CV_32F, 0)
    '    Dim znear = options.zNear
    '    Dim zfar = options.zFar
    '    Dim mm = GetMinMax(task.pcSplit(0))
    '    Dim worldWidth = mm.maxVal - mm.minVal
    '    mm = GetMinMax(task.pcSplit(1))
    '    Dim worldHeight = mm.maxVal - mm.minVal
    '    For y = 0 To depth.Height - 1
    '        For x = 0 To depth.Width - 1
    '            If mask.Get(Of Byte)(y, x) = 0 Then Continue For
    '            Dim d = znear + depth.Get(Of Single)(y, x) * (zfar - znear)
    '            If d > 1 Then Dim k = 0
    '            Dim u = CInt(((x * worldWidth) * fx / d) + ppx)
    '            Dim v = CInt(((y * worldHeight) * fy / d) + ppy)
    '            If u >= 0 And u < task.workRes.Width And v >= 0 And v < task.workRes.Height Then
    '                dst.Set(Of Single)(v, u, d)
    '            End If
    '        Next
    '    Next
    '    Return dst
    'End Function
    Private Sub readPointCloud()

        'Dim near = options.zNear
        'Dim far = options.zFar
        'Dim a As Single = 2.0F * near * far
        'Dim b As Single = far + near
        'Dim c As Single = far - near

        '' convert from (0 to 1) to (-1 to 1)
        'task.sharpDepth = task.sharpDepth * 2.0F
        'task.sharpDepth -= 1.0F

        'Dim denom As New Mat()
        'Cv2.Multiply(task.sharpDepth, c, denom)         ' denom = task.sharpDepth * c
        'Cv2.Subtract(b, denom, denom)         ' denom = b - task.sharpDepth * c
        'Cv2.Divide(a, denom, task.sharpDepth)
    End Sub
    Private Function ReadFilteredDepth() As cv.Mat
        Dim sharpDepth As New cv.Mat(task.workRes, cv.MatType.CV_32F, 0)
        gl.ReadPixels(0, 0, sharpDepth.Width, sharpDepth.Height, OpenGL.GL_DEPTH_COMPONENT, OpenGL.GL_FLOAT, sharpDepth.Data)
        Dim dst As New cv.Mat(task.workRes, cv.MatType.CV_32F, 0)
        Dim h = sharpDepth.Height
        For y = 0 To sharpDepth.Height - 1
            For x = 0 To sharpDepth.Width - 1
                Dim depthValue = sharpDepth.Get(Of Single)(y, x)

                ' Filter out untouched pixels (depth = 1.0 means zFar)
                If depthValue < 1.0F Then
                    ' Convert normalized depth to world-space Z (linear in ortho)
                    Dim Z = znear + depthValue * (zfar - znear)
                    dst.Set(Of Single)(h - 1 - y, x, Z)
                End If
            Next
        Next

        Return sharpDepth
    End Function
    Public Function worldCoordinateInverse(depth As cv.Mat) As cv.Mat
        Dim dst As New cv.Mat(depth.Size, cv.MatType.CV_32F, 0)
        Dim count As Integer
        For yy = 0 To depth.Height - 1
            For xx = 0 To depth.Width - 1
                Dim d = depth.Get(Of Single)(yy, xx)
                If d >= 1.0F Or d < 0.0001F Then Continue For
                count += 1
                'Dim x = mmX.minVal + xx * (mmX.maxVal - mmX.minVal)
                'Dim y = mmY.minVal + yy * (mmY.maxVal - mmY.minVal)
                d = mmZ.minVal + d * (mmZ.maxVal - mmZ.minVal)

                Dim u = CInt((xx * fx / d) + ppx)
                Dim v = CInt((yy * fy / d) + ppy)

                If u >= 0 And u < dst.Width And v >= 0 And v < dst.Height Then
                    dst.Set(Of Single)(dst.Height - 1 - v, u, d)
                End If
            Next
        Next
        ' Debug.WriteLine("count = " + CStr(count))
        Return dst
    End Function
    Public Function RunSharp(func As Integer, Optional pointcloud As cv.Mat = Nothing, Optional RGB As cv.Mat = Nothing) As String
        options.Run()
        options2.Run()
        zNear = options.zNear
        zFar = options.zFar

        If task.gOptions.DebugCheckBox.Checked Then
            task.gOptions.DebugCheckBox.Checked = False
            task.sharpGL.resetView()
        End If
        If task.firstPass Or task.optionsChanged Then task.sharpGL.resetView()
        gl.Viewport(0, 0, GLControl.Width, GLControl.Height)
        gl.MatrixMode(OpenGL.GL_PROJECTION)
        gl.LoadIdentity()

        If task.gOptions.GL_LinearMode.Checked Then
            mmZ = GetMinMax(task.pcSplit(2), task.depthMask)
            Dim zoomFactor As Single = 1.0F + zoomZ
            gl.Ortho(-task.xRange, task.xRange, -task.yRange, task.yRange, zNear * zoomFactor, zFar * zoomFactor)
        Else
            ' gl.Perspective(options.perspective, GLControl.Width / GLControl.Height, znear, zFar)
        End If

        gl.MatrixMode(OpenGL.GL_MODELVIEW)

        gl.Enable(OpenGL.GL_DEPTH_TEST)
        gl.DepthFunc(OpenGL.GL_LESS)

        gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT Or OpenGL.GL_DEPTH_BUFFER_BIT)
        gl.LoadIdentity()

        ' gl.LookAt(options2.eye(0), options2.eye(1), options2.eye(2), 0, 0, zoomZ, upX, upY, upZ)

        gl.Translate(panX, panY, zoomZ)
        If task.heartBeat Then Debug.WriteLine("ZoomZ = " + Format(zoomZ, fmt3))
        gl.Rotate(rotationX, 1.0F, 0.0F, 0.0F)
        gl.Rotate(rotationY, 0.0F, 1.0F, 0.0F)
        gl.PointSize(options.pointSize)

        Dim label = ""
        If pointcloud Is Nothing Then pointcloud = task.pointCloud
        If RGB Is Nothing Then RGB = task.color
        Select Case func
            Case Comm.oCase.readPC
                label = drawCloud(pointcloud, RGB)
                gl.Flush()
                gl.ReadPixels(0, 0, task.workRes.Width, task.workRes.Height, OpenGL.GL_DEPTH_COMPONENT,
                              OpenGL.GL_FLOAT, task.sharpDepth.Data)

            Case Comm.oCase.drawPointCloudRGB
                'gl.Enable(OpenGL.GL_DEPTH_TEST)
                'gl.Enable(OpenGL.GL_STENCIL_TEST)

                'gl.ClearStencil(0)
                'gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT Or OpenGL.GL_DEPTH_BUFFER_BIT Or OpenGL.GL_STENCIL_BUFFER_BIT)

                label = drawCloud(pointcloud, RGB)


            Case Comm.oCase.line3D
                gl.Begin(OpenGL.GL_POINTS)
                Dim count As Integer, all255 As Boolean
                If RGB Is Nothing Then all255 = True
                For y = 0 To pointcloud.Height - 1
                    For x = 0 To pointcloud.Width - 1
                        Dim vec = pointcloud.Get(Of cv.Vec3f)(y, x)
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


            Case Comm.oCase.quadBasics
                drawQuads()

            Case Comm.oCase.draw3DLines
                label = draw3DLines()

            Case Comm.oCase.draw3DLinesAndCloud
                label = drawCloud(pointcloud, RGB)

                label += " " + draw3DLines()
        End Select

        gl.Flush()
        Return label
    End Function
End Class
