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
                taskAlg.drawRect.Width = 0
                taskAlg.drawRect.Height = 0
                mouseDownPoint.X = x
                mouseDownPoint.Y = y
            End If
        End Sub
        Private Sub CamPic_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            If taskAlg Is Nothing Then Exit Sub
            taskAlg.mouseDisplayPoint = New cv.Point(e.X, e.Y)
            Dim x As Integer = e.X * settings.workRes.Width / pics(0).Width
            Dim y As Integer = e.Y * settings.workRes.Height / pics(0).Height
            Dim pic = DirectCast(sender, PictureBox)
            taskAlg.mousePicTag = pic.Tag
            If activeMouseDown Then Exit Sub
            If DrawingRectangle Then
                If x < 0 Then x = 0
                If x >= taskAlg.workRes.Width Then x = taskAlg.workRes.Width - 1
                If y < 0 Then y = 0
                If y >= taskAlg.workRes.Height Then y = taskAlg.workRes.Height - 1
                mouseMovePoint.X = x
                mouseMovePoint.Y = y
                taskAlg.drawRect.X = Math.Min(mouseDownPoint.X, mouseMovePoint.X)
                taskAlg.drawRect.Y = Math.Min(mouseDownPoint.Y, mouseMovePoint.Y)
                taskAlg.drawRect.Width = Math.Abs(mouseDownPoint.X - mouseMovePoint.X)
                taskAlg.drawRect.Height = Math.Abs(mouseDownPoint.Y - mouseMovePoint.Y)
                If taskAlg.drawRect.X + taskAlg.drawRect.Width > pics(0).Width Then
                    taskAlg.drawRect.Width = pics(0).Width - taskAlg.drawRect.X
                End If
                If taskAlg.drawRect.Y + taskAlg.drawRect.Height > pics(0).Height Then
                    taskAlg.drawRect.Height = pics(0).Height - taskAlg.drawRect.Y
                End If
                BothFirstAndLastReady = True
            End If

            StatusLabel.Text = String.Format("X: {0}, Y: {1}    ", x, y)
            taskAlg.mouseMovePoint = New cv.Point(x, y)
            If taskAlg IsNot Nothing Then
                StatusLabel.Text += String.Format("Last click: {0}, {1}    ", taskAlg.clickPoint.X, taskAlg.clickPoint.Y)
            End If

            If taskAlg.drawRect.Width > 0 And taskAlg.drawRect.Height > 0 Then
                StatusLabel.Text += "DrawRect = " + String.Format("x: {0}, y: {1}, w: {2}, h: {3}", taskAlg.drawRect.X, taskAlg.drawRect.Y,
                                    taskAlg.drawRect.Width, taskAlg.drawRect.Height)
            End If
        End Sub
        Private Sub campic_DoubleClick(sender As Object, e As EventArgs)
            DrawingRectangle = False
        End Sub
        Private Sub clickPic(sender As Object, e As EventArgs)
            If taskAlg Is Nothing Then Exit Sub
            'If taskAlg IsNot Nothing Then  if taskAlg.sharpgl IsNot Nothing Then sharpGL.Activate()
            If taskAlg IsNot Nothing Then If taskAlg.treeView IsNot Nothing Then taskAlg.treeView.Activate()
            If taskAlg IsNot Nothing Then If taskAlg.allOptions IsNot Nothing Then taskAlg.allOptions.Activate()
            taskAlg.clickPoint = taskAlg.mouseMovePoint
        End Sub
    End Class
End Namespace
