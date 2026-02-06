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
            atask.mouseMagnifyEndPoint = New cv.Point(e.X, e.Y)
        End Sub
        Private Sub CamPic_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            If atask Is Nothing Then Exit Sub
            Dim x As Integer = e.X * settings.workRes.Width / pics(0).Width
            Dim y As Integer = e.Y * settings.workRes.Height / pics(0).Height
            Dim pic = DirectCast(sender, PictureBox)
            If e.Button = System.Windows.Forms.MouseButtons.Right Then
                activeMouseDown = True
            End If
            If e.Button = System.Windows.Forms.MouseButtons.Left Then
                DrawingRectangle = True
                BothFirstAndLastReady = False ' we have to see some movement after mousedown.
                atask.drawRect.Width = 0
                atask.drawRect.Height = 0
                mouseDownPoint.X = x
                mouseDownPoint.Y = y
            End If

            atask.mouseMagnifyStartPoint = New cv.Point(e.X, e.Y)
            atask.mouseMagnifyPicTag = pic.Tag
        End Sub
        Private Sub CamPic_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            If atask Is Nothing Then Exit Sub
            atask.mouseDisplayPoint = New cv.Point(e.X, e.Y)
            Dim x As Integer = e.X * settings.workRes.Width / pics(0).Width
            Dim y As Integer = e.Y * settings.workRes.Height / pics(0).Height
            Dim pic = DirectCast(sender, PictureBox)
            atask.mousePicTag = pic.Tag
            If activeMouseDown Then Exit Sub
            If DrawingRectangle Then
                If x < 0 Then x = 0
                If x >= atask.workRes.Width Then x = atask.workRes.Width - 1
                If y < 0 Then y = 0
                If y >= atask.workRes.Height Then y = atask.workRes.Height - 1
                mouseMovePoint.X = x
                mouseMovePoint.Y = y
                atask.drawRect.X = Math.Min(mouseDownPoint.X, mouseMovePoint.X)
                atask.drawRect.Y = Math.Min(mouseDownPoint.Y, mouseMovePoint.Y)
                atask.drawRect.Width = Math.Abs(mouseDownPoint.X - mouseMovePoint.X)
                atask.drawRect.Height = Math.Abs(mouseDownPoint.Y - mouseMovePoint.Y)
                If atask.drawRect.X + atask.drawRect.Width > atask.workRes.Width Then
                    atask.drawRect.Width = atask.workRes.Width - atask.drawRect.X
                End If
                If atask.drawRect.Y + atask.drawRect.Height > atask.workRes.Height Then
                    atask.drawRect.Height = atask.workRes.Height - atask.drawRect.Y
                End If
                BothFirstAndLastReady = True
            End If

            StatusLabel.Text = String.Format("X: {0}, Y: {1}    ", x, y)
            atask.mouseMovePoint = New cv.Point(x, y)
            If atask IsNot Nothing Then
                StatusLabel.Text += String.Format("Last click: {0}, {1}    ", atask.clickPoint.X, atask.clickPoint.Y)
            End If

            If atask.drawRect.Width > 0 And atask.drawRect.Height > 0 Then
                StatusLabel.Text += "DrawRect = " + String.Format("x: {0}, y: {1}, w: {2}, h: {3}", atask.drawRect.X, atask.drawRect.Y,
                                    atask.drawRect.Width, atask.drawRect.Height)
            End If
        End Sub
        Private Sub campic_DoubleClick(sender As Object, e As EventArgs)
            DrawingRectangle = False
        End Sub
        Private Sub clickPic(sender As Object, e As EventArgs)
            If atask Is Nothing Then Exit Sub
            'If atask IsNot Nothing Then  if atask.sharpgl IsNot Nothing Then sharpGL.Activate()
            If atask IsNot Nothing Then If atask.treeView IsNot Nothing Then atask.treeView.Activate()
            If atask IsNot Nothing Then If atask.allOptions IsNot Nothing Then atask.allOptions.Activate()
            atask.clickPoint = atask.mouseMovePoint
            atask.mouseClickFlag = True
        End Sub
    End Class
End Namespace
