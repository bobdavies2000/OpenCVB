Imports cv = OpenCvSharp
Imports System.ComponentModel
Imports System.Windows.Forms
Public Class OptionsContainer
    Public optionsTitle As New List(Of String)
    Public hiddenOptions As New List(Of String)
    Public offset = 30
    Private Sub allOptionsFrm_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.Left = GetSetting("OpenCVB", "aOptionsLeft", "aOptionsLeft", task.defaultRect.X - offset)
        Me.Top = GetSetting("OpenCVB", "aOptionsTop", "aOptionsTop", task.defaultRect.Y - offset)
        Me.Width = GetSetting("OpenCVB", "aOptionsWidth", "aOptionsWidth", task.defaultRect.Width)
        Me.Height = GetSetting("OpenCVB", "aOptionsHeight", "aOptionsHeight", task.defaultRect.Height)
    End Sub
    Private Sub Options_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        SaveSetting("OpenCVB", "aOptionsLeft", "aOptionsLeft", Me.Left)
        SaveSetting("OpenCVB", "aOptionsTop", "aOptionsTop", Me.Top)
        SaveSetting("OpenCVB", "aOptionsWidth", "aOptionsWidth", Me.Width)
        SaveSetting("OpenCVB", "aOptionsHeight", "aOptionsHeight", Me.Height)
    End Sub
    Public Sub addTitle(frm As Object)
        If optionsTitle.Contains(frm.Text) = False Then
            optionsTitle.Add(frm.Text)
        Else
            hiddenOptions.Add(frm.Text)
        End If
        frm.show
    End Sub
    Public Sub layoutOptions()
        Dim sliderOffset As New cv.Point(0, 0)
        Dim w = GetSetting("OpenCVB", "aOptionsWidth", "aOptionsWidth", task.defaultRect.Width)
        Dim otherOffset As New cv.Point(w / 2, 0)
        For Each title In hiddenOptions
            Dim hideList As New List(Of Form)
            For Each frm In Application.OpenForms
                If frm.text = title Then
                    frm.hide
                    Exit For
                End If
            Next
        Next

        Try
            Dim indexS As Integer = 0
            Dim indexO As Integer = 0
            indexS += 1 ' adding 1 for the Options_Common form which is always first and in the back.
            For Each title In optionsTitle
                Dim frm = findfrm(title)
                If frm IsNot Nothing And title <> "OptionsCommon Slider Options" And title <> "OptionsCommon_Histogram_ViewOptions" Then
                    frm.BringToFront()
                    If title.EndsWith(" Slider Options") Or title.EndsWith(" Keyboard Options") Or title.EndsWith("OptionsAlphaBlend") Then
                        If frm Is Nothing Then Continue For
                        frm.SetDesktopLocation(sliderOffset.X + indexS * offset, sliderOffset.Y + indexS * offset)
                        indexS += 1
                    End If
                    If title.EndsWith(" Radio Options") Or title.EndsWith(" CheckBox Options") Then
                        If frm Is Nothing Then Continue For
                        frm.SetDesktopLocation(otherOffset.X + indexO * offset, otherOffset.Y + indexO * offset)
                        indexO += 1
                    End If
                End If
            Next
        Catch ex As Exception
            Console.WriteLine("Error in layoutOptions: " + ex.Message)
        End Try
        hiddenOptions.Clear()
    End Sub
    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        layoutOptions()
    End Sub
End Class