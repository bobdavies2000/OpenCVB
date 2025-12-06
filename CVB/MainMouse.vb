Imports cv = OpenCvSharp
Namespace CVB
    Partial Public Class MainForm : Inherits Form
        Dim DrawingRectangle As Boolean
        Dim drawRect As New cv.Rect
        Dim drawRectPic As Integer
        Dim frameCount As Integer = -1
        Dim GrabRectangleData As Boolean
        Dim LastX As Integer
        Dim LastY As Integer
        Dim mouseClickFlag As Boolean
        Dim activateTaskForms As Boolean
        Dim ClickPoint As New cv.Point ' last place where mouse was clicked.
        Dim mousePicTag As Integer
        Dim mouseDownPoint As cv.Point
        Dim mouseMovePoint As cv.Point ' last place the mouse was located in any of the OpenCVB images.
        Dim mousePointCamPic As New cv.Point ' mouse location in campics
        Dim activeMouseDown As Boolean
        Dim BothFirstAndLastReady As Boolean

        Private Sub CamPic_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles campicRGB.MouseUp, campicPointCloud.MouseUp, campicLeft.MouseUp, campicRight.MouseUp
            Try
                If DrawingRectangle Then
                    DrawingRectangle = False
                    GrabRectangleData = True
                End If
                activeMouseDown = False
            Catch ex As Exception
                Debug.WriteLine("Error in camPic_MouseUp: " + ex.Message)
            End Try
        End Sub
        Private Sub CamPic_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles campicRGB.MouseDown, campicPointCloud.MouseDown, campicLeft.MouseDown, campicRight.MouseDown
            Dim x As Integer = e.X * settings.workRes.Width / campicRGB.Width
            Dim y As Integer = e.Y * settings.workRes.Height / campicRGB.Height
            Try
                Dim pic = DirectCast(sender, PictureBox)
                If e.Button = System.Windows.Forms.MouseButtons.Right Then
                    activeMouseDown = True
                End If
                If e.Button = System.Windows.Forms.MouseButtons.Left Then
                    DrawingRectangle = True
                    BothFirstAndLastReady = False ' we have to see some movement after mousedown.
                    drawRect.Width = 0
                    drawRect.Height = 0
                    mouseDownPoint.X = x
                    mouseDownPoint.Y = y
                End If
            Catch ex As Exception
                Debug.WriteLine("Error in camPic_MouseDown: " + ex.Message)
            End Try
        End Sub
        Private Sub CamPic_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles campicRGB.MouseMove, campicPointCloud.MouseMove, campicLeft.MouseMove, campicRight.MouseMove
            Dim x As Integer = e.X * settings.workRes.Width / campicRGB.Width
            Dim y As Integer = e.Y * settings.workRes.Height / campicRGB.Height
            Try
                Dim pic = DirectCast(sender, PictureBox)
                If activeMouseDown Then Exit Sub
                If DrawingRectangle Then
                    mouseMovePoint.X = x
                    mouseMovePoint.Y = y
                    If mouseMovePoint.X < 0 Then mouseMovePoint.X = 0
                    If mouseMovePoint.Y < 0 Then mouseMovePoint.Y = 0
                    drawRectPic = pic.Tag
                    If x < campicRGB.Width Then
                        drawRect.X = Math.Min(mouseDownPoint.X, mouseMovePoint.X)
                    Else
                        drawRect.X = Math.Min(mouseDownPoint.X - campicRGB.Width, mouseMovePoint.X - campicRGB.Width)
                        drawRectPic = 3 ' When wider than campicRGB, it can only be dst3 which has no pic.tag (because campic(2) is double-wide for timing reasons.
                    End If
                    drawRect.Y = Math.Min(mouseDownPoint.Y, mouseMovePoint.Y)
                    drawRect.Width = Math.Abs(mouseDownPoint.X - mouseMovePoint.X)
                    drawRect.Height = Math.Abs(mouseDownPoint.Y - mouseMovePoint.Y)
                    If drawRect.X + drawRect.Width > campicRGB.Width Then drawRect.Width = campicRGB.Width - drawRect.X
                    If drawRect.Y + drawRect.Height > campicRGB.
                        Height Then drawRect.Height = campicRGB.Height - drawRect.Y
                    BothFirstAndLastReady = True
                End If

                mousePicTag = pic.Tag
                mousePointCamPic.X = x
                mousePointCamPic.Y = y
                mousePointCamPic *= settings.workRes.Width / campicRGB.Width

            Catch ex As Exception
                Debug.WriteLine("Error in camPic_MouseMove: " + ex.Message)
            End Try

            If lastClickPoint <> Point.Empty Then
                StatusLabel.Text = String.Format("X: {0}, Y: {1}", x, y)
                StatusLabel.Text += String.Format(", Last click: {0}, {1}", lastClickPoint.X, lastClickPoint.Y)
            Else
                StatusLabel.Text = String.Format("X: {0}, Y: {1}", x, y)
            End If

            If drawRect.Width > 0 And drawRect.Height > 0 Then
                StatusLabel.Text += " DrawRect = " + String.Format("x: {0}, y: {1}, w: {2}, h: {3}", drawRect.X, drawRect.Y, drawRect.Width, drawRect.Height)
            End If
        End Sub
        Private Sub PictureBox_MouseClick(sender As Object, e As MouseEventArgs) Handles campicRGB.MouseClick, campicPointCloud.MouseClick, campicLeft.MouseClick, campicRight.MouseClick
            Dim picBox = TryCast(sender, PictureBox)
            Dim x As Integer = e.X * settings.workRes.Width / campicRGB.Width
            Dim y As Integer = e.Y * settings.workRes.Height / campicRGB.Height
            lastClickPoint = New Point(x, y)
        End Sub
        Private Sub campic_DoubleClick(sender As Object, e As EventArgs) Handles campicRGB.DoubleClick, campicPointCloud.DoubleClick, campicLeft.DoubleClick, campicRight.DoubleClick
            DrawingRectangle = False
        End Sub
    End Class
End Namespace
