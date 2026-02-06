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
            taskA.mouseMagnifyEndPoint = New cv.Point(e.X, e.Y)
        End Sub
        Private Sub CamPic_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            If taskA Is Nothing Then Exit Sub
            Dim x As Integer = e.X * settings.workRes.Width / pics(0).Width
            Dim y As Integer = e.Y * settings.workRes.Height / pics(0).Height
            Dim pic = DirectCast(sender, PictureBox)
            If e.Button = System.Windows.Forms.MouseButtons.Right Then
                activeMouseDown = True
            End If
            If e.Button = System.Windows.Forms.MouseButtons.Left Then
                DrawingRectangle = True
                BothFirstAndLastReady = False ' we have to see some movement after mousedown.
                taskA.drawRect.Width = 0
                taskA.drawRect.Height = 0
                mouseDownPoint.X = x
                mouseDownPoint.Y = y
            End If

            taskA.mouseMagnifyStartPoint = New cv.Point(e.X, e.Y)
            taskA.mouseMagnifyPicTag = pic.Tag
        End Sub
        Private Sub CamPic_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            If taskA Is Nothing Then Exit Sub
            taskA.mouseDisplayPoint = New cv.Point(e.X, e.Y)
            Dim x As Integer = e.X * settings.workRes.Width / pics(0).Width
            Dim y As Integer = e.Y * settings.workRes.Height / pics(0).Height
            Dim pic = DirectCast(sender, PictureBox)
            taskA.mousePicTag = pic.Tag
            If activeMouseDown Then Exit Sub
            If DrawingRectangle Then
                If x < 0 Then x = 0
                If x >= taskA.workRes.Width Then x = taskA.workRes.Width - 1
                If y < 0 Then y = 0
                If y >= taskA.workRes.Height Then y = taskA.workRes.Height - 1
                mouseMovePoint.X = x
                mouseMovePoint.Y = y
                taskA.drawRect.X = Math.Min(mouseDownPoint.X, mouseMovePoint.X)
                taskA.drawRect.Y = Math.Min(mouseDownPoint.Y, mouseMovePoint.Y)
                taskA.drawRect.Width = Math.Abs(mouseDownPoint.X - mouseMovePoint.X)
                taskA.drawRect.Height = Math.Abs(mouseDownPoint.Y - mouseMovePoint.Y)
                If taskA.drawRect.X + taskA.drawRect.Width > taskA.workRes.Width Then
                    taskA.drawRect.Width = taskA.workRes.Width - taskA.drawRect.X
                End If
                If taskA.drawRect.Y + taskA.drawRect.Height > taskA.workRes.Height Then
                    taskA.drawRect.Height = taskA.workRes.Height - taskA.drawRect.Y
                End If
                BothFirstAndLastReady = True
            End If

            StatusLabel.Text = String.Format("X: {0}, Y: {1}    ", x, y)
            taskA.mouseMovePoint = New cv.Point(x, y)
            If taskA IsNot Nothing Then
                StatusLabel.Text += String.Format("Last click: {0}, {1}    ", taskA.clickPoint.X, taskA.clickPoint.Y)
            End If

            If taskA.drawRect.Width > 0 And taskA.drawRect.Height > 0 Then
                StatusLabel.Text += "DrawRect = " + String.Format("x: {0}, y: {1}, w: {2}, h: {3}", taskA.drawRect.X, taskA.drawRect.Y,
                                    taskA.drawRect.Width, taskA.drawRect.Height)
            End If
        End Sub
        Private Sub campic_DoubleClick(sender As Object, e As EventArgs)
            DrawingRectangle = False
        End Sub
        Private Sub clickPic(sender As Object, e As EventArgs)
            If taskA Is Nothing Then Exit Sub
            'If taskA IsNot Nothing Then  if taskA.sharpgl IsNot Nothing Then sharpGL.Activate()
            If taskA IsNot Nothing Then If taskA.treeView IsNot Nothing Then taskA.treeView.Activate()
            If taskA IsNot Nothing Then If taskA.allOptions IsNot Nothing Then taskA.allOptions.Activate()
            taskA.clickPoint = taskA.mouseMovePoint
            taskA.mouseClickFlag = True
        End Sub
    End Class
End Namespace
