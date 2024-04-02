Imports cv = OpenCvSharp
Imports System.Windows.Forms
Imports System.Drawing
Imports System.ComponentModel
Public Class PixelViewerForm
    Public mousePoint As cv.Point
    Public saveText As String
    Dim secondCount As Integer
    Private Sub PixelShow_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Left = GetSetting("OpenCVB", "PixelViewerLeft", "PixelViewerLeft", Me.Left)
        Me.Top = GetSetting("OpenCVB", "PixelViewerTop", "PixelViewerTop", Me.Top)

        Me.Width = GetSetting("OpenCVB", "PixelViewerWidth", "PixelViewerWidth", 1280)
        Me.Height = GetSetting("OpenCVB", "PixelViewerHeight", "PixelViewerHeight", 720)
        PixelViewerForm_ResizeEnd(sender, e)
        Timer1.Enabled = True
    End Sub
    Private Sub PixelViewerForm_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        rtb.Width = Me.Width - 40
        rtb.Height = Me.Height - 80
        SaveSetting("OpenCVB", "PixelViewerLeft", "PixelViewerLeft", Me.Left)
        SaveSetting("OpenCVB", "PixelViewerTop", "PixelViewerTop", Me.Top)
        SaveSetting("OpenCVB", "PixelViewerWidth", "PixelViewerWidth", Me.Width)
        SaveSetting("OpenCVB", "PixelViewerHeight", "PixelViewerHeight", Me.Height)
    End Sub

    Private Sub PixelViewerForm_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Select Case e.KeyCode
            Case Keys.Left
                mousePoint.X -= 1
            Case Keys.Up
                mousePoint.Y -= 1
            Case Keys.Down
                mousePoint.Y += 1
            Case Keys.Right
                mousePoint.X += 1
        End Select
    End Sub
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        rtb.Text = saveText
    End Sub
    Private Sub PixelViewerForm_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        Timer1.Enabled = False
    End Sub
    Private Sub ToolStripButton1_Click_1(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        mousePoint.X -= 1
    End Sub
    'Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
    '    mousePoint.X -= 1
    'End Sub
    'Private Sub ToolStripButton2_Click(sender As Object, e As EventArgs) Handles ToolStripButton2.Click
    '    mousePoint.Y -= 1
    'End Sub
    'Private Sub ToolStripButton3_Click(sender As Object, e As EventArgs) Handles ToolStripButton3.Click
    '    mousePoint.Y += 1
    'End Sub
    'Private Sub ToolStripButton4_Click(sender As Object, e As EventArgs) Handles ToolStripButton4.Click
    '    mousePoint.X += 1
    'End Sub
End Class