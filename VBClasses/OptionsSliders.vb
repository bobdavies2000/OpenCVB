Imports VBClasses
Public Class OptionsSliders
    Public mytrackbars As New List(Of TrackBar)
    Public myLabels As New List(Of Label)
    Dim defaultHeight = 400
    Dim groups As New List(Of FlowLayoutPanel)
    Dim defaultWidth = 600
    Dim algoIndex As Integer
    Public Function Setup(traceName As String) As Boolean
        If OptionParent.FindFrm(traceName + " Sliders") IsNot Nothing Then Return False
        If tsk.allOptions.Text <> "" Then Me.MdiParent = tsk.allOptions
        Me.Text = traceName + " Sliders"
        tsk.allOptions.addTitle(Me)

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

        Dim index = myLabels.Count
        myLabels.Add(New Label)
        myLabels(index).AutoSize = False
        myLabels(index).Width = 100
        myLabels(index).Height = 50
        grp.Controls.Add(myLabels(index))

        mytrackbars.Add(New TrackBar)
        mytrackbars(index).Width = 350
        mytrackbars(index).Tag = index
        mytrackbars(index).Visible = False
        mytrackbars(index).Minimum = min
        mytrackbars(index).Maximum = max
        mytrackbars(index).Value = If(value >= min And value <= max, value, (max - min) / 2)
        mytrackbars(index).Visible = True
        AddHandler mytrackbars(index).ValueChanged, AddressOf TrackBar_ValueChanged
        grp.Controls.Add(mytrackbars(index))

        myLabels(index).Text = labelStr + " = " + CStr(mytrackbars(index).Value)
        myLabels(index).Visible = True
        FlowLayoutPanel1.AutoScroll = If(index > 3, True, False)
        FlowLayoutPanel1.Controls.Add(grp)
        groups.Add(grp)
    End Sub
    Private Sub TrackBar_ValueChanged(sender As Object, e As EventArgs)
        Dim outStr = myLabels(sender.tag).Text
        Dim split = outStr.Split("=")
        myLabels(sender.tag).Text = split(0) + "= " + CStr(mytrackbars(sender.tag).Value)
        tsk.optionsChanged = True
    End Sub
    Private Sub OptionsSliders_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Width = defaultWidth
        Me.Height = defaultHeight
    End Sub
    Private Sub OptionsSliders_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        FlowLayoutPanel1.Height = Me.Height - 65
        FlowLayoutPanel1.Width = Me.Width - 40
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            ' Remove event handlers
            For Each trackbar In mytrackbars
                If trackbar IsNot Nothing Then
                    RemoveHandler trackbar.ValueChanged, AddressOf TrackBar_ValueChanged
                End If
            Next

            ' Dispose all dynamically created controls
            For Each group In groups
                If group IsNot Nothing Then
                    group.Dispose()
                End If
            Next

            For Each trackbar In mytrackbars
                If trackbar IsNot Nothing Then
                    trackbar.Dispose()
                End If
            Next

            For Each label In myLabels
                If label IsNot Nothing Then
                    label.Dispose()
                End If
            Next

            ' Clear the lists
            groups.Clear()
            mytrackbars.Clear()
            myLabels.Clear()
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class
