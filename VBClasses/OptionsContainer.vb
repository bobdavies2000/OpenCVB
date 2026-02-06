Imports cv = OpenCvSharp
Imports VBClasses
Public Class OptionsContainer
    Dim optionsTitle As New List(Of String)
    Public hiddenOptions As New List(Of String)
    Public titlesAdded As Boolean
    Public offset = 30
    Public positionedFromSettings As Boolean
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
        Dim w = taskA.Settings.allOptionsWidth
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

        Dim indexS = 1
        Dim indexO = 1
        Dim indexHide As Integer
        For Each title In optionsTitle
            Dim frm = OptionParent.FindFrm(title)
            If frm IsNot Nothing Then
                frm.BringToFront()
                Dim sidelineOptions As Boolean = True
                Dim displayTheseOptions As New List(Of String)({"Image_Basics OpenFile Options"})
                If displayTheseOptions.Contains(frm.Text) Then sidelineOptions = False
                If normalRequest And sidelineOptions And taskA.Settings.ShowAllOptions = False Then
                    If frm Is Nothing Then Continue For
                    frm.SetDesktopLocation(Me.Width - 2 * offset, sliderOffset.Y + indexHide * offset)
                    indexHide += 1
                Else
                    If title.Contains("OpenFile") Then
                        frm.SetDesktopLocation(0, taskA.gOptions.Top + 350)
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
        hiddenOptions.Clear()
    End Sub
    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        layoutOptions(normalRequest:=True)
        taskA.featureOptions.BringToFront()
        taskA.gOptions.BringToFront()
        taskA.gOptions.BringToFront()
    End Sub
    Private Sub ToolStripButton2_Click(sender As Object, e As EventArgs) Handles ToolStripButton2.Click
        layoutOptions(normalRequest:=False)
    End Sub
    Private Sub OptionsContainer_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
    End Sub
    Private Sub OptionsContainer_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        If positionedFromSettings Then
            taskA.Settings.allOptionsLeft = Me.Left
            taskA.Settings.allOptionsTop = Me.Top
            taskA.Settings.allOptionsWidth = Me.Width
            taskA.Settings.allOptionsHeight = Me.Height
        End If
    End Sub
    Private Sub OptionsContainer_Move(sender As Object, e As EventArgs) Handles Me.Move
        OptionsContainer_ResizeEnd(sender, e)
    End Sub
    Private Sub OptionsContainer_Disposed(sender As Object, e As EventArgs) Handles Me.Disposed
        For Each title In hiddenOptions
            Dim hideList As New List(Of Form)
            For Each frm In Application.OpenForms
                If frm.text = title Then frm.close()
            Next
        Next
        taskA.gOptions.Close()
    End Sub
End Class
