Imports cv = OpenCvSharp
Imports VBClasses.VBClasses.vbc
Namespace MainUI
    Partial Public Class MainUI
        Dim DrawingRectangle As Boolean
        Dim LastX As Integer
        Dim LastY As Integer
        Dim mouseDownPoint As cv.Point
        Dim mouseMovePoint As cv.Point ' last place the mouse was located in any of the OpenCVB images.
        Dim activeMouseDown As Boolean
        Dim BothFirstAndLastReady As Boolean
        Private Sub CamPic_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            If DrawingRectangle Then DrawingRectangle = False
            activeMouseDown = False
        End Sub
        Private Sub CamPic_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            Dim x As Integer = e.X * settings.workRes.Width / pics(0).Width
            Dim y As Integer = e.Y * settings.workRes.Height / pics(0).Height
            Dim pic = DirectCast(sender, PictureBox)
            If e.Button = System.Windows.Forms.MouseButtons.Right Then
                activeMouseDown = True
            End If
            If e.Button = System.Windows.Forms.MouseButtons.Left Then
                DrawingRectangle = True
                BothFirstAndLastReady = False ' we have to see some movement after mousedown.
                algTask.drawRect.Width = 0
                algTask.drawRect.Height = 0
                mouseDownPoint.X = x
                mouseDownPoint.Y = y
            End If
        End Sub
        Private Sub CamPic_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            If algTask Is Nothing Then Exit Sub
            Dim x As Integer = e.X * settings.workRes.Width / pics(0).Width
            Dim y As Integer = e.Y * settings.workRes.Height / pics(0).Height
            Dim pic = DirectCast(sender, PictureBox)
            algTask.mousePicTag = pic.Tag
            If activeMouseDown Then Exit Sub
            If DrawingRectangle Then
                If x < 0 Then x = 0
                If x >= algTask.workRes.Width Then x = algTask.workRes.Width - 1
                If y < 0 Then y = 0
                If y >= algTask.workRes.Height Then y = algTask.workRes.Height - 1
                mouseMovePoint.X = x
                mouseMovePoint.Y = y
                algTask.drawRect.X = Math.Min(mouseDownPoint.X, mouseMovePoint.X)
                algTask.drawRect.Y = Math.Min(mouseDownPoint.Y, mouseMovePoint.Y)
                algTask.drawRect.Width = Math.Abs(mouseDownPoint.X - mouseMovePoint.X)
                algTask.drawRect.Height = Math.Abs(mouseDownPoint.Y - mouseMovePoint.Y)
                If algTask.drawRect.X + algTask.drawRect.Width > pics(0).Width Then
                    algTask.drawRect.Width = pics(0).Width - algTask.drawRect.X
                End If
                If algTask.drawRect.Y + algTask.drawRect.Height > pics(0).Height Then
                    algTask.drawRect.Height = pics(0).Height - algTask.drawRect.Y
                End If
                BothFirstAndLastReady = True
            End If

            StatusLabel.Text = String.Format("X: {0}, Y: {1}    ", x, y)
            algTask.mouseMovePoint = New cv.Point(x, y)
            If algTask IsNot Nothing Then
                StatusLabel.Text += String.Format("Last click: {0}, {1}    ", algTask.clickPoint.X, algTask.clickPoint.Y)
            End If

            If algTask.drawRect.Width > 0 And algTask.drawRect.Height > 0 Then
                StatusLabel.Text += "DrawRect = " + String.Format("x: {0}, y: {1}, w: {2}, h: {3}", algTask.drawRect.X, algTask.drawRect.Y,
                                    algTask.drawRect.Width, algTask.drawRect.Height)
            End If
        End Sub
        Private Sub campic_DoubleClick(sender As Object, e As EventArgs)
            DrawingRectangle = False
        End Sub
    End Class
End Namespace
