Imports cv = OpenCvSharp
Namespace CVB
    Partial Public Class MainForm
        Dim DrawingRectangle As Boolean
        Dim drawRect As New cv.Rect
        Dim LastX As Integer
        Dim LastY As Integer
        Dim mouseClickFlag As Boolean
        Dim mousePicTag As Integer
        Dim mouseDownPoint As cv.Point
        Dim mouseMovePoint As cv.Point ' last place the mouse was located in any of the OpenCVB images.
        Dim activeMouseDown As Boolean
        Dim BothFirstAndLastReady As Boolean
        Private Sub CamPic_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles campicRGB.MouseUp, campicPointCloud.MouseUp, campicLeft.MouseUp, campicRight.MouseUp
            Try
                If DrawingRectangle Then DrawingRectangle = False
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
                mousePicTag = pic.Tag
                If activeMouseDown Then Exit Sub
                If DrawingRectangle Then
                    mouseMovePoint.X = x
                    mouseMovePoint.Y = y
                    If mouseMovePoint.X < 0 Then mouseMovePoint.X = 0
                    If mouseMovePoint.Y < 0 Then mouseMovePoint.Y = 0
                    drawRect.X = Math.Min(mouseDownPoint.X, mouseMovePoint.X)
                    drawRect.Y = Math.Min(mouseDownPoint.Y, mouseMovePoint.Y)
                    drawRect.Width = Math.Abs(mouseDownPoint.X - mouseMovePoint.X)
                    drawRect.Height = Math.Abs(mouseDownPoint.Y - mouseMovePoint.Y)
                    If drawRect.X + drawRect.Width > campicRGB.Width Then drawRect.Width = campicRGB.Width - drawRect.X
                    If drawRect.Y + drawRect.Height > campicRGB.
                        Height Then drawRect.Height = campicRGB.Height - drawRect.Y
                    BothFirstAndLastReady = True
                End If

            Catch ex As Exception
                Debug.WriteLine("Error in camPic_MouseMove: " + ex.Message)
            End Try

            StatusLabel.Text = String.Format("X: {0}, Y: {1}    ", x, y)
            StatusLabel.Text += String.Format("Last click: {0}, {1}    ", myTask.clickPoint.X, myTask.clickPoint.Y)

            If drawRect.Width > 0 And drawRect.Height > 0 Then
                StatusLabel.Text += "DrawRect = " + String.Format("x: {0}, y: {1}, w: {2}, h: {3}", drawRect.X, drawRect.Y, drawRect.Width, drawRect.Height)
            End If
        End Sub
        Private Sub PictureBox_MouseClick(sender As Object, e As MouseEventArgs) Handles campicRGB.MouseClick, campicPointCloud.MouseClick, campicLeft.MouseClick, campicRight.MouseClick
            Dim picBox = TryCast(sender, PictureBox)
            Dim x As Integer = e.X * settings.workRes.Width / campicRGB.Width
            Dim y As Integer = e.Y * settings.workRes.Height / campicRGB.Height
            myTask.clickPoint = New cv.Point(x, y)
        End Sub
        Private Sub campic_DoubleClick(sender As Object, e As EventArgs) Handles campicRGB.DoubleClick, campicPointCloud.DoubleClick, campicLeft.DoubleClick, campicRight.DoubleClick
            DrawingRectangle = False
        End Sub
    End Class
End Namespace
