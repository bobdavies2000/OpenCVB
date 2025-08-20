Imports cv = OpenCvSharp
Public Class OptionsContainer
    Dim optionsTitle As New List(Of String)
    Public hiddenOptions As New List(Of String)
    Public titlesAdded As Boolean
    Public offset = 30
    Private Sub allOptionsFrm_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.Left = GetSetting("Opencv", "gOptionsLeft", "gOptionsLeft", task.mainFormLocation.X - offset)
        Me.Top = GetSetting("Opencv", "gOptionsTop", "gOptionsTop", task.mainFormLocation.Y - offset)
        Me.Width = GetSetting("Opencv", "gOptionsWidth", "gOptionsWidth", task.mainFormLocation.Width)
        Me.Height = GetSetting("Opencv", "gOptionsHeight", "gOptionsHeight", task.mainFormLocation.Height)
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
        Dim w = GetSetting("Opencv", "gOptionsWidth", "gOptionsWidth", task.mainFormLocation.Width)
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

        Dim showAllOptions = GetSetting("Opencv", "ShowAllOptions", "ShowAllOptions", False)
        Try
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
                    If normalRequest And sidelineOptions And showAllOptions = False Then
                        If frm Is Nothing Then Continue For
                        frm.SetDesktopLocation(Me.Width - 2 * offset, sliderOffset.Y + indexHide * offset)
                        indexHide += 1
                    Else
                        If title.Contains("OpenFile") Then
                            frm.SetDesktopLocation(0, task.gOptions.Top + 350)
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
        task.featureOptions.BringToFront()
        task.gOptions.BringToFront()
        task.gOptions.BringToFront()
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
            MessageBox.Show("Form is completely off-screen!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)

            ' Optionally, you might want to move the form back onto the screen
            ' For example, move it to the center of the primary screen
            Me.StartPosition = FormStartPosition.Manual
            Me.Location = New Point(0, 0)
            SaveSetting("Opencv", "gOptionsLeft", "gOptionsLeft", 0)
            SaveSetting("Opencv", "gOptionsTop", "gOptionsTop", 0)
        End If
    End Sub
    Private Sub OptionsContainer_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        SaveSetting("Opencv", "gOptionsLeft", "gOptionsLeft", Math.Abs(Me.Left))
        SaveSetting("Opencv", "gOptionsTop", "gOptionsTop", Me.Top)
        SaveSetting("Opencv", "gOptionsWidth", "gOptionsWidth", Me.Width)
        SaveSetting("Opencv", "gOptionsHeight", "gOptionsHeight", Me.Height)
        CheckIfOffScreen()
        For Each title In hiddenOptions
            Dim hideList As New List(Of Form)
            For Each frm In Application.OpenForms
                If frm.text = title Then
                    frm.Dispose()
                    Exit For
                End If
            Next
        Next
        task.gOptions.Dispose()
        task.featureOptions.Dispose()
        GC.Collect()
    End Sub
End Class