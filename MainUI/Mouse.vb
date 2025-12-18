Imports cv = OpenCvSharp
Imports VBClasses
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
                Task.drawRect.Width = 0
                Task.drawRect.Height = 0
                mouseDownPoint.X = x
                mouseDownPoint.Y = y
            End If
        End Sub
        Private Sub CamPic_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            If task Is Nothing Then Exit Sub
            task.mouseDisplayPoint = New cv.Point(e.X, e.Y)
            Dim x As Integer = e.X * settings.workRes.Width / pics(0).Width
            Dim y As Integer = e.Y * settings.workRes.Height / pics(0).Height
            Dim pic = DirectCast(sender, PictureBox)
            Task.mousePicTag = pic.Tag
            If activeMouseDown Then Exit Sub
            If DrawingRectangle Then
                If x < 0 Then x = 0
                If x >= Task.workRes.Width Then x = Task.workRes.Width - 1
                If y < 0 Then y = 0
                If y >= Task.workRes.Height Then y = Task.workRes.Height - 1
                mouseMovePoint.X = x
                mouseMovePoint.Y = y
                Task.drawRect.X = Math.Min(mouseDownPoint.X, mouseMovePoint.X)
                Task.drawRect.Y = Math.Min(mouseDownPoint.Y, mouseMovePoint.Y)
                Task.drawRect.Width = Math.Abs(mouseDownPoint.X - mouseMovePoint.X)
                Task.drawRect.Height = Math.Abs(mouseDownPoint.Y - mouseMovePoint.Y)
                If Task.drawRect.X + Task.drawRect.Width > pics(0).Width Then
                    Task.drawRect.Width = pics(0).Width - Task.drawRect.X
                End If
                If Task.drawRect.Y + Task.drawRect.Height > pics(0).Height Then
                    Task.drawRect.Height = pics(0).Height - Task.drawRect.Y
                End If
                BothFirstAndLastReady = True
            End If

            StatusLabel.Text = String.Format("X: {0}, Y: {1}    ", x, y)
            Task.mouseMovePoint = New cv.Point(x, y)
            If task IsNot Nothing Then
                StatusLabel.Text += String.Format("Last click: {0}, {1}    ", task.clickPoint.X, task.clickPoint.Y)
            End If

            If Task.drawRect.Width > 0 And Task.drawRect.Height > 0 Then
                StatusLabel.Text += "DrawRect = " + String.Format("x: {0}, y: {1}, w: {2}, h: {3}", Task.drawRect.X, Task.drawRect.Y,
                                    Task.drawRect.Width, Task.drawRect.Height)
            End If
        End Sub
        Private Sub campic_DoubleClick(sender As Object, e As EventArgs)
            DrawingRectangle = False
        End Sub
        Private Sub clickPic(sender As Object, e As EventArgs)
            If task Is Nothing Then Exit Sub
            'If task IsNot Nothing Then  if task.sharpgl IsNot Nothing Then sharpGL.Activate()
            If task IsNot Nothing Then If task.treeView IsNot Nothing Then task.treeView.Activate()
            If task IsNot Nothing Then If task.allOptions IsNot Nothing Then task.allOptions.Activate()
            task.clickPoint = Task.mouseMovePoint
        End Sub
    End Class
End Namespace
