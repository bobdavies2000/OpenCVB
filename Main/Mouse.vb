Imports cv = OpenCvSharp
Namespace MainUI
    Partial Public Class MainUI
        Dim DrawingRectangle As Boolean
        Dim LastX As Integer
        Dim LastY As Integer
        Dim mouseDownPoint As cv.Point
        Dim mouseMovePoint As cv.Point ' last place the mouse was located in any of the OpenCVB images.
        Dim activeMouseDown As Boolean
        Dim BothFirstAndLastReady As Boolean
        Private Sub CamPic_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles campicRGB.MouseUp, campicPointCloud.MouseUp, campicLeft.MouseUp, campicRight.MouseUp
            If DrawingRectangle Then DrawingRectangle = False
            activeMouseDown = False
        End Sub
        Private Sub CamPic_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles campicRGB.MouseDown, campicPointCloud.MouseDown, campicLeft.MouseDown, campicRight.MouseDown
            If task Is Nothing Then Exit Sub
            Dim x As Integer = e.X * settings.workRes.Width / campicRGB.Width
            Dim y As Integer = e.Y * settings.workRes.Height / campicRGB.Height
            Dim pic = DirectCast(sender, PictureBox)
            If e.Button = System.Windows.Forms.MouseButtons.Right Then
                activeMouseDown = True
            End If
            If e.Button = System.Windows.Forms.MouseButtons.Left Then
                DrawingRectangle = True
                BothFirstAndLastReady = False ' we have to see some movement after mousedown.
                task.drawRect.Width = 0
                task.drawRect.Height = 0
                mouseDownPoint.X = x
                mouseDownPoint.Y = y
            End If
        End Sub
        Private Sub CamPic_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles campicRGB.MouseMove, campicPointCloud.MouseMove, campicLeft.MouseMove, campicRight.MouseMove
            If task Is Nothing Then Exit Sub

            Dim x As Integer = e.X * settings.workRes.Width / campicRGB.Width
            Dim y As Integer = e.Y * settings.workRes.Height / campicRGB.Height
            Dim pic = DirectCast(sender, PictureBox)
            task.mousePicTag = pic.Tag
            If activeMouseDown Then Exit Sub
            If DrawingRectangle Then
                If x < 0 Then x = 0
                If x >= task.workRes.Width Then x = task.workRes.Width - 1
                If y < 0 Then y = 0
                If y >= task.workRes.Height Then y = task.workRes.Height - 1
                mouseMovePoint.X = x
                mouseMovePoint.Y = y
                task.drawRect.X = Math.Min(mouseDownPoint.X, mouseMovePoint.X)
                task.drawRect.Y = Math.Min(mouseDownPoint.Y, mouseMovePoint.Y)
                task.drawRect.Width = Math.Abs(mouseDownPoint.X - mouseMovePoint.X)
                task.drawRect.Height = Math.Abs(mouseDownPoint.Y - mouseMovePoint.Y)
                If task.drawRect.X + task.drawRect.Width > campicRGB.Width Then task.drawRect.Width = campicRGB.Width - task.drawRect.X
                If task.drawRect.Y + task.drawRect.Height > campicRGB.Height Then
                    task.drawRect.Height = campicRGB.Height - task.drawRect.Y
                End If
                BothFirstAndLastReady = True
            End If

            StatusLabel.Text = String.Format("X: {0}, Y: {1}    ", x, y)
            task.mouseMovePoint = New cv.Point(x, y)
            If task IsNot Nothing Then
                StatusLabel.Text += String.Format("Last click: {0}, {1}    ", task.clickPoint.X, task.clickPoint.Y)
            End If

            If task.drawRect.Width > 0 And task.drawRect.Height > 0 Then
                StatusLabel.Text += "DrawRect = " + String.Format("x: {0}, y: {1}, w: {2}, h: {3}", task.drawRect.X, task.drawRect.Y,
                                    task.drawRect.Width, task.drawRect.Height)
            End If
        End Sub
        Private Sub PictureBox_MouseClick(sender As Object, e As MouseEventArgs) Handles campicRGB.MouseClick, campicPointCloud.MouseClick, campicLeft.MouseClick, campicRight.MouseClick
            If task Is Nothing Then Exit Sub
            Dim picBox = TryCast(sender, PictureBox)
            Dim x As Integer = e.X * settings.workRes.Width / campicRGB.Width
            Dim y As Integer = e.Y * settings.workRes.Height / campicRGB.Height
            task.clickPoint = New cv.Point(x, y)
            task.mouseClickFlag = True
        End Sub
        Private Sub campic_DoubleClick(sender As Object, e As EventArgs) Handles campicRGB.DoubleClick, campicPointCloud.DoubleClick, campicLeft.DoubleClick, campicRight.DoubleClick
            DrawingRectangle = False
        End Sub
    End Class
End Namespace
