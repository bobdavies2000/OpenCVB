Imports SharpGL
Imports cv = OpenCvSharp
Imports OpenCvSharp.Extensions
Imports VBClasses
Public Class SharpGLForm
    Dim gl As OpenGL
    Dim isDragging As Boolean = False
    Dim lastMousePos As cv.Point
    Dim rotationX As Single = 0.0F
    Dim rotationY As Single = 0.0F
    Const zoomZInit = -1.0F
    Dim zoomZ As Single = zoomZInit
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
    Public options1 As Options_GL
    Public options2 As Options_SharpGL2
    Public ppx = task.calibData.leftIntrinsics.ppx
    Public ppy = task.calibData.leftIntrinsics.ppy
    Public fx = task.calibData.leftIntrinsics.fx
    Public fy = task.calibData.leftIntrinsics.fy
    Public hulls As RedCloud_Basics
    Private Sub GLForm_Load(sender As Object, e As EventArgs) Handles Me.Load
        options = New Options_SharpGL
        options1 = New Options_GL
        options2 = New Options_SharpGL2

        Me.Location = New Point(task.settings.sharpGLLeft, task.settings.sharpGLTop)
        Me.Size = New Size(task.settings.sharpGLWidth, task.settings.sharpGLHeight)

        gl = GLControl.OpenGL
    End Sub
    Private Sub sgl_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        task.Settings.sharpGLLeft = Me.Left
        task.Settings.sharpGLTop = Me.Top
        task.Settings.sharpGLWidth = Me.Width
        task.Settings.sharpGLHeight = Me.Height
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
    Private Sub prepareSharpGL()
        If task.gOptions.DebugCheckBox.Checked Then
            task.gOptions.DebugCheckBox.Checked = False
            task.sharpGL.resetView()
        End If

        gl.Viewport(0, 0, GLControl.Width, GLControl.Height)
        'gl.MatrixMode(OpenGL.GL_PROJECTION)
        'gl.LoadIdentity()

        If options1.GL_LinearMode Then
            Dim mmZ = GetMinMax(task.pcSplit(2))
            Dim xRange = options.xRange
            Dim yRange = options.yRange
            gl.Ortho(-xRange, xRange, -yRange, yRange, mmZ.minVal, mmZ.maxVal)
        Else
            gl.Perspective(options.perspective, GLControl.Width / GLControl.Height, options.zNear, options.zFar)
        End If

        gl.MatrixMode(OpenGL.GL_MODELVIEW)

        gl.Enable(OpenGL.GL_DEPTH_TEST)
        gl.DepthFunc(OpenGL.GL_LESS)

        gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT Or OpenGL.GL_DEPTH_BUFFER_BIT)
        gl.LoadIdentity()

        ' gl.LookAt(options2.eye(0), options2.eye(1), options2.eye(2), 0, 0, zoomZ, upX, upY, upZ)

        gl.Translate(panX, panY, zoomZ)
        gl.Rotate(rotationX, 1.0F, 0.0F, 0.0F)
        gl.Rotate(rotationY, 0.0F, 1.0F, 0.0F)
        gl.PointSize(options.pointSize)
    End Sub
    Private Sub OpenGLControl_MouseUp(sender As Object, e As MouseEventArgs) Handles GLControl.MouseUp
        If e.Button = MouseButtons.Left Then isDragging = False
        If e.Button = MouseButtons.Right Then isPanning = False
    End Sub
    Public Sub resetView()
        Me.Text = "SharpGL - " + If(options1.GL_LinearMode, "", "Non") + "Linear mode"
        rotationX = 0.0
        rotationY = 0.0
        If options1.GL_LinearMode Then zoomZ = 0 Else zoomZ = zoomZInit
    End Sub
    Private Sub OpenGLControl_MouseWheel(sender As Object, e As MouseEventArgs) Handles GLControl.MouseWheel
        Dim delta As Integer = e.Delta
        zoomZ += If(delta > 0, 0.5F, -0.5F)
        GLControl.Invalidate() ' Force redraw
    End Sub
    Private Function GetMinMax(mat As cv.Mat, Optional mask As cv.Mat = Nothing) As mmData
        Dim mm As mmData = Nothing
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
            Dim depth = -task.pcSplit(2)(rect).Mean(task.depthmask(rect))(0)
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
                If task.depthmask.Get(Of Byte)(y, x) <> 0 Then
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
    Private Sub readPointCloud()
        task.sharpDepth = New cv.Mat(New cv.Size(GLControl.Width, GLControl.Height), cv.MatType.CV_32F, 0)
        gl.ReadPixels(0, 0, GLControl.Width, GLControl.Height, OpenGL.GL_DEPTH_COMPONENT,
                      OpenGL.GL_FLOAT, task.sharpDepth.Data)
        task.sharpDepth = task.sharpDepth.Resize(task.workRes)
        task.sharpDepth = task.sharpDepth.Flip(cv.FlipMode.X)
    End Sub
    Private Sub optionsSetup()
        options.Run()
        options1.Run()
        options2.Run()
        prepareSharpGL()
    End Sub
    Public Function RunSharp(func As Integer, Optional pointcloud As cv.Mat = Nothing, Optional RGB As cv.Mat = Nothing) As String
        optionsSetup()

        Dim label = ""
        If pointcloud Is Nothing Then pointcloud = task.pointCloud
        If RGB Is Nothing Then RGB = task.color
        Select Case func
            Case Common.oCase.readPC
                label = drawCloud(pointcloud, RGB)
                readPointCloud()

            Case Common.oCase.readLines
                label = drawCloud(pointcloud, RGB)
                label += draw3DLines(task.lines.lpList)
                readPointCloud()

            Case Common.oCase.readQuads
                label = drawQuads()
                readPointCloud()

            Case Common.oCase.drawPointCloudRGB
                'gl.ClearStencil(0)
                'gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT Or OpenGL.GL_DEPTH_BUFFER_BIT Or OpenGL.GL_STENCIL_BUFFER_BIT)

                label = drawCloud(pointcloud, RGB)


            Case Common.oCase.line3D
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


            Case Common.oCase.quadBasics
                drawQuads()

            Case Common.oCase.draw3DLines
                label = draw3DLines(task.lines.lpList)

            Case Common.oCase.draw3DLinesAndCloud
                label = drawCloud(pointcloud, RGB)

                label += " " + draw3DLines(task.lines.lpList)
        End Select

        gl.Flush()
        Return label
    End Function
    Private Function draw3DLines(lplist As List(Of lpData)) As String
        gl.LineWidth(options.pointSize)
        gl.Begin(OpenGL.GL_LINES)
        For Each lp In lplist
            If lp.pVec1(2) < 0.1 Or lp.pVec2(2) < 0.1 Then Continue For ' Lines that are impossibly close
            gl.Color(lp.color(2) / 255, lp.color(1) / 255, lp.color(0) / 255)
            gl.Vertex(lp.pVec1(0), -lp.pVec1(1), -lp.pVec1(2))
            gl.Vertex(lp.pVec2(0), -lp.pVec2(1), -lp.pVec2(2))
        Next
        gl.End()
        Return task.lines.labels(2)
    End Function
    Public Function RunLines(func As Integer, lpList As List(Of lpData)) As String
        optionsSetup()

        Dim label = ""
        Select Case func
            Case Common.oCase.draw3DLines
                label = draw3DLines(lpList)
            Case Common.oCase.draw3DLinesAndCloud
                label = drawCloud(task.pointCloud, task.color)
                label += " " + draw3DLines(lpList)
        End Select

        gl.Flush()
        Return label
    End Function
    Public Function RunTriangles(func As Integer, dataBuffer As List(Of cv.Vec3f)) As String
        optionsSetup()

        Dim label = ""
        Select Case func
            Case Common.oCase.colorTriangles
                Dim vec As cv.Vec3f
                gl.Begin(OpenGL.GL_TRIANGLES)

                For i = 0 To dataBuffer.Count - 1 Step 4
                    vec = dataBuffer(i)
                    gl.Color(CSng(vec(0) / 255), CSng(vec(1) / 255), CSng(vec(2) / 255))

                    vec = dataBuffer(i + 1)
                    gl.Vertex(vec(0), vec(1), -vec(2))

                    vec = dataBuffer(i + 2)
                    gl.Vertex(vec(0), vec(1), -vec(2))

                    vec = dataBuffer(i + 3)
                    gl.Vertex(vec(0), vec(1), -vec(2))
                Next

                gl.End()
                label = CStr(dataBuffer.Count) + " triangles were sent to OpenGL."

            Case Common.oCase.imageTriangles
                If hulls Is Nothing Then hulls = New RedCloud_Basics
                hulls.Run(task.color)

                Dim textureID As UInt32() = New UInt32(0) {} ' Array to hold the texture ID
                Dim rgba As cv.Mat = task.color.CvtColor(cv.ColorConversionCodes.BGR2RGBA)
                Dim bitmap As Bitmap = rgba.ToBitmap()

                gl.GenTextures(1, textureID)
                gl.BindTexture(OpenGL.GL_TEXTURE_2D, textureID(0))

                gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_LINEAR)
                gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_LINEAR)

                gl.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGBA,
                              bitmap.Width, bitmap.Height, 0,
                              OpenGL.GL_RGBA, OpenGL.GL_UNSIGNED_BYTE,
                              rgba.Data)

                gl.Enable(OpenGL.GL_TEXTURE_2D)
                gl.BindTexture(OpenGL.GL_TEXTURE_2D, textureID(0))

                gl.Begin(OpenGL.GL_TRIANGLES)

                Dim w = task.workRes.Width
                Dim h = task.workRes.Height
                Dim pt As cv.Point
                Dim vec(2) As cv.Vec3f
                Dim pts(2) As cv.Point
                Dim triangleCount As Integer
                For Each pc In hulls.rcList
                    Dim count As Single = pc.hull.Count
                    For i = 0 To pc.hull.Count - 1
                        Dim goodDepth As Boolean = True
                        For j = 0 To vec.Length - 1
                            Select Case j
                                Case 0
                                    pt = New cv.Point(CInt(pc.hull(i).X + pc.rect.X), CInt(pc.hull(i).Y + pc.rect.Y))
                                Case 1
                                    pt = pc.maxDist
                                Case 2
                                    pt = New cv.Point(CInt(pc.hull((i + 1) Mod count).X + pc.rect.X), CInt(pc.hull((i + 1) Mod count).Y + pc.rect.Y))
                            End Select

                            pts(j) = pt
                            vec(j) = task.pointCloud.Get(Of cv.Vec3f)(pt.Y, pt.X)
                            If vec(j)(0) = 0 Or vec(j)(1) = 0 Or vec(j)(2) = 0 Then goodDepth = False
                        Next

                        If goodDepth Then
                            triangleCount += 1
                            For j = 0 To vec.Length - 1
                                gl.TexCoord(pts(j).X / w, pts(j).Y / h)
                                gl.Vertex(vec(j)(0), -vec(j)(1), -vec(j)(2))
                            Next
                        End If
                    Next
                Next

                gl.End()
                gl.DeleteTextures(1, textureID)
                label = CStr(triangleCount) + " triangles were sent to OpenGL."
        End Select

        gl.Flush()
        Return label
    End Function
    Private Sub SharpGLForm_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        task.Settings.sharpGLLeft = Me.Left
        task.Settings.sharpGLTop = Me.Top
        task.Settings.sharpGLWidth = Me.Width
        task.Settings.sharpGLHeight = Me.Height
    End Sub
End Class

