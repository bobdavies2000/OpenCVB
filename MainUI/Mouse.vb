Imports cv = OpenCvSharp
Imports VBClasses
Namespace MainApp
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
            Dim x As Integer = e.X * settings.workRes.Width / pics(0).Width
            Dim y As Integer = e.Y * settings.workRes.Height / pics(0).Height
            vbc.task.mouseMagnifyEndPoint = New cv.Point(e.X, e.Y)
        End Sub
        Private Sub CamPic_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            If vbc.task Is Nothing Then Exit Sub
            Dim x As Integer = e.X * settings.workRes.Width / pics(0).Width
            Dim y As Integer = e.Y * settings.workRes.Height / pics(0).Height
            Dim pic = DirectCast(sender, PictureBox)
            If e.Button = System.Windows.Forms.MouseButtons.Right Then
                activeMouseDown = True
            End If
            If e.Button = System.Windows.Forms.MouseButtons.Left Then
                DrawingRectangle = True
                BothFirstAndLastReady = False ' we have to see some movement after mousedown.
                vbc.task.drawRect.Width = 0
                vbc.task.drawRect.Height = 0
                mouseDownPoint.X = x
                mouseDownPoint.Y = y
            End If

            vbc.task.mouseMagnifyStartPoint = New cv.Point(e.X, e.Y)
            vbc.task.mouseMagnifyPicTag = pic.Tag
        End Sub
        Private Sub CamPic_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            If vbc.task Is Nothing Then Exit Sub
            vbc.task.mouseDisplayPoint = New cv.Point(e.X, e.Y)
            Dim x As Integer = e.X * settings.workRes.Width / pics(0).Width
            Dim y As Integer = e.Y * settings.workRes.Height / pics(0).Height
            Dim pic = DirectCast(sender, PictureBox)
            vbc.task.mousePicTag = pic.Tag
            If activeMouseDown Then Exit Sub
            If DrawingRectangle Then
                If x < 0 Then x = 0
                If x >= vbc.task.workRes.Width Then x = vbc.task.workRes.Width - 1
                If y < 0 Then y = 0
                If y >= vbc.task.workRes.Height Then y = vbc.task.workRes.Height - 1
                mouseMovePoint.X = x
                mouseMovePoint.Y = y
                vbc.task.drawRect.X = Math.Min(mouseDownPoint.X, mouseMovePoint.X)
                vbc.task.drawRect.Y = Math.Min(mouseDownPoint.Y, mouseMovePoint.Y)
                vbc.task.drawRect.Width = Math.Abs(mouseDownPoint.X - mouseMovePoint.X)
                vbc.task.drawRect.Height = Math.Abs(mouseDownPoint.Y - mouseMovePoint.Y)
                If vbc.task.drawRect.X + vbc.task.drawRect.Width > vbc.task.workRes.Width Then
                    vbc.task.drawRect.Width = vbc.task.workRes.Width - vbc.task.drawRect.X
                End If
                If vbc.task.drawRect.Y + vbc.task.drawRect.Height > vbc.task.workRes.Height Then
                    vbc.task.drawRect.Height = vbc.task.workRes.Height - vbc.task.drawRect.Y
                End If
                BothFirstAndLastReady = True
            End If

            StatusLabel.Text = String.Format("X: {0}, Y: {1}    ", x, y)
            vbc.task.mouseMovePoint = New cv.Point(x, y)
            If vbc.task IsNot Nothing Then
                StatusLabel.Text += String.Format("Last click: {0}, {1}    ", vbc.task.clickPoint.X, vbc.task.clickPoint.Y)
            End If

            If vbc.task.drawRect.Width > 0 And vbc.task.drawRect.Height > 0 Then
                StatusLabel.Text += "DrawRect = " + String.Format("x: {0}, y: {1}, w: {2}, h: {3}", vbc.task.drawRect.X, vbc.task.drawRect.Y,
                                    vbc.task.drawRect.Width, vbc.task.drawRect.Height)
            End If
        End Sub
        Private Sub campic_DoubleClick(sender As Object, e As EventArgs)
            DrawingRectangle = False
        End Sub
        Private Sub clickPic(sender As Object, e As EventArgs)
            If vbc.task Is Nothing Then Exit Sub
            'If vbc.task IsNot Nothing Then  if vbc.task.sharpgl IsNot Nothing Then sharpGL.Activate()
            If vbc.task IsNot Nothing Then If vbc.task.treeView IsNot Nothing Then vbc.task.treeView.Activate()
            If vbc.task IsNot Nothing Then If vbc.task.allOptions IsNot Nothing Then vbc.task.allOptions.Activate()
            vbc.task.clickPoint = vbc.task.mouseMovePoint
            vbc.task.mouseClickFlag = True
        End Sub
    End Class
End Namespace
