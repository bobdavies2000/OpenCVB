Imports System.Windows.Forms
Imports System.Drawing
Public Class OptionsSliders
    Public trackbar As New List(Of TrackBar)
    Public sLabels As New List(Of Label)
    Dim defaultHeight = 400
    Dim groups As New List(Of FlowLayoutPanel)
    Dim defaultWidth = 600
    Dim algoIndex As Integer
    Public Function Setup(traceName As String) As Boolean
        If findfrm(traceName + " Sliders") IsNot Nothing Then Return False
        If allOptions.Text <> "" Then Me.MdiParent = allOptions
        Me.Text = traceName + " Sliders"
        allOptions.addTitle(Me)

        FlowLayoutPanel1.Width = Me.Width - 40
        FlowLayoutPanel1.Height = Me.Height - 60
        Return True
    End Function
    Public Sub setupTrackBar(labelStr As String, min As Integer, max As Integer, value As Integer)
        Dim grp = New FlowLayoutPanel
        grp.AutoSize = False
        grp.BorderStyle = BorderStyle.None
        grp.FlowDirection = FlowDirection.LeftToRight
        grp.WrapContents = False
        grp.Width = Me.Width + 10
        grp.Height = 50

        Dim index = sLabels.Count
        sLabels.Add(New Label)
        sLabels(index).AutoSize = False
        sLabels(index).Width = 100
        sLabels(index).Height = 50
        grp.Controls.Add(sLabels(index))

        trackbar.Add(New TrackBar)
        trackbar(index).Width = 350
        trackbar(index).Tag = index
        trackbar(index).Visible = False
        trackbar(index).Minimum = min
        trackbar(index).Maximum = max
        trackbar(index).Value = If(value >= min And value <= max, value, (max - min) / 2)
        trackbar(index).Visible = True
        AddHandler trackbar(index).ValueChanged, AddressOf TrackBar_ValueChanged
        grp.Controls.Add(trackbar(index))

        sLabels(index).Text = labelStr + " = " + CStr(trackbar(index).Value)
        sLabels(index).Visible = True
        FlowLayoutPanel1.AutoScroll = If(index > 3, True, False)
        FlowLayoutPanel1.Controls.Add(grp)
    End Sub
    Private Sub TrackBar_ValueChanged(sender As Object, e As EventArgs)
        Dim outStr = sLabels(sender.tag).Text
        Dim split = outStr.Split("=")
        sLabels(sender.tag).Text = split(0) + "= " + CStr(trackbar(sender.tag).Value)
        task.optionsChanged = True
    End Sub
    Private Sub OptionsSliders_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Width = defaultWidth
        Me.Height = defaultHeight
    End Sub
    Private Sub OptionsSliders_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        FlowLayoutPanel1.Height = Me.Height - 65
        FlowLayoutPanel1.Width = Me.Width - 40
    End Sub
End Class