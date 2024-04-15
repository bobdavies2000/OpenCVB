Imports cv = OpenCvSharp
Imports System.ComponentModel
Imports System.Windows.Forms
Imports System.Drawing
Public Class OptionsContainer
    Dim optionsTitle As New List(Of String)
    Public hiddenOptions As New List(Of String)
    Public titlesAdded As Boolean
    Public offset = 30
    Private Sub allOptionsFrm_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.Left = GetSetting("OpenCVB", "gOptionsLeft", "gOptionsLeft", task.mainFormLocation.X - offset)
        Me.Top = GetSetting("OpenCVB", "gOptionsTop", "gOptionsTop", task.mainFormLocation.Y - offset)
        Me.Width = GetSetting("OpenCVB", "gOptionsWidth", "gOptionsWidth", task.mainFormLocation.Width)
        Me.Height = GetSetting("OpenCVB", "gOptionsHeight", "gOptionsHeight", task.mainFormLocation.Height)

        Dim goodPoint = Screen.GetWorkingArea(New Point(Me.Left, Me.Top)) ' when they change the main screen, old coordinates can go way off the screen.
        If goodPoint.X > Me.Left Then Me.Left = goodPoint.X
        If goodPoint.Y > Me.Top Then Me.Top = goodPoint.Y
    End Sub
    Private Sub Options_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        SaveSetting("OpenCVB", "gOptionsLeft", "gOptionsLeft", Me.Left)
        SaveSetting("OpenCVB", "gOptionsTop", "gOptionsTop", Me.Top)
        SaveSetting("OpenCVB", "gOptionsWidth", "gOptionsWidth", Me.Width)
        SaveSetting("OpenCVB", "gOptionsHeight", "gOptionsHeight", Me.Height)
    End Sub
    Public Sub addTitle(frm As Object)
        If optionsTitle.Contains(frm.Text) = False Then
            optionsTitle.Add(frm.Text)
        Else
            hiddenOptions.Add(frm.Text)
        End If
        frm.show
        titlesAdded = True
    End Sub
    Public Sub layoutOptions(normalRequest As Boolean)
        Dim w = GetSetting("OpenCVB", "gOptionsWidth", "gOptionsWidth", task.mainFormLocation.Width)
        Dim radioCheckOffset = New cv.Point(w / 2, 0)

        Dim sliderOffset As New cv.Point(0, 0)
        For Each title In hiddenOptions
            Dim hideList As New List(Of Form)
            For Each frm In Application.OpenForms
                If frm.text = title Then
                    frm.hide
                    Exit For
                End If
            Next
        Next

        Dim showAllOptions = GetSetting("OpenCVB", "ShowAllOptions", "ShowAllOptions", False)
        Try
            Dim indexS = 1
            Dim indexO = 1
            Dim indexHide As Integer
            For Each title In optionsTitle
                Dim frm = findfrm(title)
                If frm IsNot Nothing Then
                    Dim sidelineOptions As Boolean = True
                    Dim displayTheseOptions As New List(Of String)({"Image_Basics OpenFile Options"})
                    If displayTheseOptions.Contains(frm.Text) Then sidelineOptions = False
                    If (normalRequest And sidelineOptions) And showAllOptions = False Then
                        If frm Is Nothing Then Continue For
                        frm.SetDesktopLocation(Me.Width - 2 * offset, sliderOffset.Y + indexHide * offset)
                        indexHide += 1
                    Else
                        If title.Contains("OpenFile") Then
                            frm.SetDesktopLocation(0, gOptions.Top + 350)
                        End If
                        If title.EndsWith(" Sliders") Or title.EndsWith(" Keyboard Options") Or title.EndsWith("OptionsAlphaBlend") Then
                            If frm Is Nothing Then Continue For
                            frm.SetDesktopLocation(sliderOffset.X + indexS * offset, sliderOffset.Y + indexS * offset)
                            indexS += 1
                        End If
                        If title.EndsWith(" Radio Buttons") Or title.EndsWith(" CheckBoxes") Then
                            If frm Is Nothing Then Continue For
                            frm.SetDesktopLocation(radioCheckOffset.X + indexO * offset, radioCheckOffset.Y + indexO * offset)
                            indexO += 1
                        End If
                    End If
                End If
            Next
        Catch ex As Exception
            Console.WriteLine("Error in layoutOptions: " + ex.Message)
        End Try
        hiddenOptions.Clear()
    End Sub
    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        layoutOptions(normalRequest:=True)
    End Sub
    Private Sub ToolStripButton2_Click(sender As Object, e As EventArgs) Handles ToolStripButton2.Click
        layoutOptions(normalRequest:=False)
    End Sub
End Class