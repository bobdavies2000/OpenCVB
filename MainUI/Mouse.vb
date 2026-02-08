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
            tsk.mouseMagnifyEndPoint = New cv.Point(e.X, e.Y)
        End Sub
        Private Sub CamPic_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            If tsk Is Nothing Then Exit Sub
            Dim x As Integer = e.X * settings.workRes.Width / pics(0).Width
            Dim y As Integer = e.Y * settings.workRes.Height / pics(0).Height
            Dim pic = DirectCast(sender, PictureBox)
            If e.Button = System.Windows.Forms.MouseButtons.Right Then
                activeMouseDown = True
            End If
            If e.Button = System.Windows.Forms.MouseButtons.Left Then
                DrawingRectangle = True
                BothFirstAndLastReady = False ' we have to see some movement after mousedown.
                tsk.drawRect.Width = 0
                tsk.drawRect.Height = 0
                mouseDownPoint.X = x
                mouseDownPoint.Y = y
            End If

            tsk.mouseMagnifyStartPoint = New cv.Point(e.X, e.Y)
            tsk.mouseMagnifyPicTag = pic.Tag
        End Sub
        Private Sub CamPic_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            If tsk Is Nothing Then Exit Sub
            tsk.mouseDisplayPoint = New cv.Point(e.X, e.Y)
            Dim x As Integer = e.X * settings.workRes.Width / pics(0).Width
            Dim y As Integer = e.Y * settings.workRes.Height / pics(0).Height
            Dim pic = DirectCast(sender, PictureBox)
            tsk.mousePicTag = pic.Tag
            If activeMouseDown Then Exit Sub
            If DrawingRectangle Then
                If x < 0 Then x = 0
                If x >= tsk.workRes.Width Then x = tsk.workRes.Width - 1
                If y < 0 Then y = 0
                If y >= tsk.workRes.Height Then y = tsk.workRes.Height - 1
                mouseMovePoint.X = x
                mouseMovePoint.Y = y
                tsk.drawRect.X = Math.Min(mouseDownPoint.X, mouseMovePoint.X)
                tsk.drawRect.Y = Math.Min(mouseDownPoint.Y, mouseMovePoint.Y)
                tsk.drawRect.Width = Math.Abs(mouseDownPoint.X - mouseMovePoint.X)
                tsk.drawRect.Height = Math.Abs(mouseDownPoint.Y - mouseMovePoint.Y)
                If tsk.drawRect.X + tsk.drawRect.Width > tsk.workRes.Width Then
                    tsk.drawRect.Width = tsk.workRes.Width - tsk.drawRect.X
                End If
                If tsk.drawRect.Y + tsk.drawRect.Height > tsk.workRes.Height Then
                    tsk.drawRect.Height = tsk.workRes.Height - tsk.drawRect.Y
                End If
                BothFirstAndLastReady = True
            End If

            StatusLabel.Text = String.Format("X: {0}, Y: {1}    ", x, y)
            tsk.mouseMovePoint = New cv.Point(x, y)
            If tsk IsNot Nothing Then
                StatusLabel.Text += String.Format("Last click: {0}, {1}    ", tsk.clickPoint.X, tsk.clickPoint.Y)
            End If

            If tsk.drawRect.Width > 0 And tsk.drawRect.Height > 0 Then
                StatusLabel.Text += "DrawRect = " + String.Format("x: {0}, y: {1}, w: {2}, h: {3}", tsk.drawRect.X, tsk.drawRect.Y,
                                    tsk.drawRect.Width, tsk.drawRect.Height)
            End If
        End Sub
        Private Sub campic_DoubleClick(sender As Object, e As EventArgs)
            DrawingRectangle = False
        End Sub
        Private Sub clickPic(sender As Object, e As EventArgs)
            If tsk Is Nothing Then Exit Sub
            'If tsk IsNot Nothing Then  if tsk.sharpgl IsNot Nothing Then sharpGL.Activate()
            If tsk IsNot Nothing Then If tsk.treeView IsNot Nothing Then tsk.treeView.Activate()
            If tsk IsNot Nothing Then If tsk.allOptions IsNot Nothing Then tsk.allOptions.Activate()
            tsk.clickPoint = tsk.mouseMovePoint
            tsk.mouseClickFlag = True
        End Sub
    End Class
End Namespace
