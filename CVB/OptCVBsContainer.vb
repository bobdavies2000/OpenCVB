Imports cv = OpenCvSharp
Public Class OptCVBsContainer
    Dim optionsTitle As New List(Of String)
    Public hiddenOptions As New List(Of String)
    Public titlesAdded As Boolean
    Public offset = 30
    Dim afterLoad As Boolean
    Private Sub allOptionsFrm_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.Left = myTask.settings.allOptionsLeft - offset
        Me.Top = myTask.settings.allOptionsTop - offset
        Me.Width = myTask.settings.allOptionsWidth
        Me.Height = myTask.settings.allOptionsHeight
        afterLoad = True
    End Sub
    Public Sub addTitle(frm As Object)
        If optionsTitle.Contains(frm.Text) = False Then
            optionsTitle.Add(frm.Text)
        Else
            hiddenOptions.Add(frm.Text)
        End If
        Try
            frm.show
            titlesAdded = True
        Catch ex As Exception
        End Try
    End Sub
    Public Sub layoutOptions(normalRequest As Boolean)
        Dim w = Me.Width
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

        Try
            Dim indexS = 1
            Dim indexO = 1
            Dim indexHide As Integer
            For Each title In optionsTitle
                Dim frm = OptCVBParent.FindFrm(title)
                If frm IsNot Nothing Then
                    frm.BringToFront()
                    Dim sidelineOptions As Boolean = True
                    Dim displayTheseOptions As New List(Of String)({"Image_Basics OpenFile Options"})
                    If displayTheseOptions.Contains(frm.Text) Then sidelineOptions = False
                    If normalRequest And sidelineOptions Then
                        If frm Is Nothing Then Continue For
                        frm.SetDesktopLocation(Me.Width - 2 * offset, sliderOffset.Y + indexHide * offset)
                        indexHide += 1
                    Else
                        If title.Contains("OpenFile") Then
                            frm.SetDesktopLocation(0, myTask.settings.allOptionsTop + 350)
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
            Debug.WriteLine("Error in layoutOptions: " + ex.Message)
        End Try
        hiddenOptions.Clear()
    End Sub
    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        layoutOptions(normalRequest:=True)
    End Sub
    Private Sub ToolStripButton2_Click(sender As Object, e As EventArgs) Handles ToolStripButton2.Click
        layoutOptions(normalRequest:=False)
    End Sub
    Private Sub CheckIfOffScreen()
        Dim formRect As Rectangle = Me.Bounds
        Dim screenBounds As Rectangle = Screen.PrimaryScreen.WorkingArea ' Use WorkingArea to exclude taskbar

        ' Check if any part of the form is visible on the screen
        If Not screenBounds.IntersectsWith(formRect) Then
            ' The entire form is off the screen
            MessageBox.Show("Form is completely offscreen!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)

            ' Optionally, you might want to move the form back onto the screen
            ' For example, move it to the center of the primary screen
            Me.StartPosition = FormStartPosition.Manual
            Me.Location = New Point(0, 0)
            myTask.settings.allOptionsLeft = 0
            myTask.settings.allOptionsTop = 0
        End If
    End Sub
    Private Sub OptionsContainer_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        CheckIfOffScreen()
        For Each title In hiddenOptions
            Dim hideList As New List(Of Form)
            For Each frm In Application.OpenForms
                If frm.text = title Then
                    frm.close()
                    Exit For
                End If
            Next
        Next
    End Sub
    Private Sub OptionsContainer_Move(sender As Object, e As EventArgs) Handles Me.Move
        If afterLoad = False Then Exit Sub
        myTask.settings.allOptionsLeft = Me.Left
        myTask.settings.allOptionsTop = Me.Top
        myTask.settings.allOptionsWidth = Me.Width
        myTask.settings.allOptionsHeight = Me.Height
    End Sub
    Private Sub OptionsContainer_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        If afterLoad = False Then Exit Sub
        myTask.settings.allOptionsLeft = Me.Left
        myTask.settings.allOptionsTop = Me.Top
        myTask.settings.allOptionsWidth = Me.Width
        myTask.settings.allOptionsHeight = Me.Height
    End Sub
End Class