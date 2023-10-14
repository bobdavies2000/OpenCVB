Imports cv = OpenCvSharp
Imports System.Windows.Forms
Imports System.Drawing
Imports System.ComponentModel
Public Class PixelViewerForm
    Public mousePoint As cv.Point
    Public saveText As String
    Dim secondCount As Integer
    Private Sub PixelShow_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Left = GetSetting("OpenCVB1", "PixelViewerLeft", "PixelViewerLeft", Me.Left)
        Me.Top = GetSetting("OpenCVB1", "PixelViewerTop", "PixelViewerTop", Me.Top)

        Me.Width = GetSetting("OpenCVB1", "PixelViewerWidth", "PixelViewerWidth", 1280)
        Me.Height = GetSetting("OpenCVB1", "PixelViewerHeight", "PixelViewerHeight", 720)
        PixelViewerForm_ResizeEnd(sender, e)
        UpdateFrequency.Items.Add("As often as possible")
        UpdateFrequency.Items.Add("Once a second")

        UpdateFrequency.SelectedIndex = 1
        Timer1.Enabled = True
    End Sub
    Private Sub PixelViewerForm_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        rtb.Width = Me.Width - 40
        rtb.Height = Me.Height - 80
        SaveSetting("OpenCVB1", "PixelViewerLeft", "PixelViewerLeft", Me.Left)
        SaveSetting("OpenCVB1", "PixelViewerTop", "PixelViewerTop", Me.Top)
        SaveSetting("OpenCVB1", "PixelViewerWidth", "PixelViewerWidth", Me.Width)
        SaveSetting("OpenCVB1", "PixelViewerHeight", "PixelViewerHeight", Me.Height)
    End Sub
    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        mousePoint.X -= 1
    End Sub
    Private Sub ToolStripButton2_Click(sender As Object, e As EventArgs) Handles ToolStripButton2.Click
        mousePoint.Y -= 1
    End Sub
    Private Sub ToolStripButton3_Click(sender As Object, e As EventArgs) Handles ToolStripButton3.Click
        mousePoint.Y += 1
    End Sub
    Private Sub ToolStripButton4_Click(sender As Object, e As EventArgs) Handles ToolStripButton4.Click
        mousePoint.X += 1
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
        If UpdateFrequency.SelectedIndex = 1 Then rtb.Text = saveText
    End Sub
    Private Sub ToolStripButton5_Click_1(sender As Object, e As EventArgs) Handles ToolStripButton5.Click
        secondCount = 0
        rtb.Text = saveText
    End Sub
    Private Sub PixelViewerForm_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        Timer1.Enabled = False
    End Sub
End Class