﻿Imports System.Windows.Forms
Public Class OptionsCheckbox
    Public Box As New List(Of CheckBox)
    Public Function Setup(traceName As String) As Boolean
        If findfrm(traceName + " CheckBoxes") IsNot Nothing Then Return False
        Me.MdiParent = allOptions
        Me.Text = traceName + " CheckBoxes"
        allOptions.addTitle(Me)
        Me.Show()
        Return True
    End Function
    Public Sub addCheckBox(labelStr As String)
        Dim index = Box.Count
        Box.Add(New CheckBox)
        AddHandler Box(index).CheckedChanged, AddressOf Box_CheckChanged
        Box(index).AutoSize = True
        Box(index).Text = labelStr
        FlowLayoutPanel1.Controls.Add(Box(index))
    End Sub
    Private Sub Box_CheckChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
    End Sub
    Private Sub OptionsCheckbox_Click(sender As Object, e As EventArgs) Handles Me.Click
        Me.BringToFront()
    End Sub
    Private Sub FlowLayoutPanel1_Click(sender As Object, e As EventArgs) Handles FlowLayoutPanel1.Click
        Me.BringToFront()
    End Sub
End Class