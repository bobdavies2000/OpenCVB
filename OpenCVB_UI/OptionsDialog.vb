Imports cv = OpenCvSharp
Public Class OptionsDialog
    Public cameraRadioButton(Main_UI.cameraNames.Count - 1) As RadioButton
    Public WorkingResRadio(resolutionList.Count - 1) As RadioButton
    Public cameraWorkingRes As cv.Size
    Public cameraDisplayRes As cv.Size
    Public cameraName As String
    Public cameraIndex As Integer
    Public testDuration As Integer
    Public Shared resolutionList As New List(Of String)(
        {"1920x1080 - Full resolution", "960x540 - Quarter resolution", "480x270 - Small resolution",
         "1280x720 - Full resolution", "640x360 - Quarter resolution", "320x180 - Small resolution",
         "640x480 - Full resolution", "320x240 - Quarter resolution", "160x120 - Small resolution",
         "672x376 - Full resolution", "336x188 - Quarter resolution", "168x94 - Small resolution    "})

    Private Sub OKButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OKButton.Click
        Main_UI.settings.showConsoleLog = showConsoleLog.Checked
        Main_UI.settings.snap640 = Snap640.Checked
        Main_UI.settings.snap320 = Snap320.Checked
        Main_UI.settings.snapCustom = SnapCustom.Checked

        For Each radio In Resolutions.Controls
            If radio.Checked Then
                Main_UI.settings.WorkingResIndex = radio.Tag
                Dim strRes = radio.text.split(" ")
                Dim resText = strRes(0)
                Dim strVals = resText.split("x")
                cameraWorkingRes = New cv.Size(CInt(strVals(0)), CInt(strVals(1)))
                Exit For
            End If
        Next

        Main_UI.settings.WorkingRes = cameraWorkingRes
        Main_UI.settings.displayRes = cameraDisplayRes

        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub
    Public Sub defineCameraResolutions(index As Integer)
        Select Case Main_UI.cameraNames(index)
            Case "Azure Kinect 4K"
                Main_UI.settings.resolutionsSupported = New List(Of Boolean)({True, True, True, True,
                                                        True, True, False, False, False, False, False, False})
            Case "Intel(R) RealSense(TM) Depth Camera 435i"
                Main_UI.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                        True, True, True, True, True, True, False, False, False})
            Case "Intel(R) RealSense(TM) Depth Camera 455"
                Main_UI.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                        True, True, True, True, True, True, False, False, False})
            Case "Oak-D camera"
                Main_UI.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                        True, True, True, False, False, False, False, False, False})
            Case "StereoLabs ZED 2/2i"
                Main_UI.settings.resolutionsSupported = New List(Of Boolean)({True, True, True,
                                                        True, True, True, False, False, False, True, True, True})
            Case "MYNT-EYE-D1000"
                Main_UI.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                        True, True, True, False, False, False, False, False, False})
            Case "Orbbec Gemini 335L"
                Main_UI.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                        True, True, True, True, True, True, False, False, False})
        End Select
    End Sub
    Private Sub cameraRadioButton_CheckChanged(sender As Object, e As EventArgs)
        Dim index = Main_UI.cameraNames.IndexOf(sender.text)
        If sender.checked = False Then Exit Sub
        cameraName = Main_UI.cameraNames(index)
        cameraIndex = index

        defineCameraResolutions(cameraIndex)

        For i = 0 To WorkingResRadio.Count - 1
            WorkingResRadio(i).Enabled = Main_UI.settings.resolutionsSupported(i)
        Next

        If cameraName.StartsWith("Intel") Then
            WorkingResRadio(resolutionList.IndexOf("320x240 - Quarter resolution")).Checked = True
        Else
            WorkingResRadio(resolutionList.IndexOf("320x180 - Small resolution")).Checked = True
        End If
        If cameraName.StartsWith("StereoLabs") Then
            WorkingResRadio(resolutionList.IndexOf("336x188 - Quarter resolution")).Checked = True
        End If
        If cameraName.StartsWith("Azure Kinect 4K") Then
            WorkingResRadio(resolutionList.IndexOf("320x180 - Small resolution")).Checked = True
        End If
        If cameraName.StartsWith("Orbbec Gemini 335L") Then
            WorkingResRadio(resolutionList.IndexOf("320x180 - Small resolution")).Checked = True
        End If
    End Sub
    Public Sub OptionsDialog_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Static radioButtonsPresent = False
        defineCameraResolutions(cameraIndex)

        If radioButtonsPresent = False Then
            radioButtonsPresent = True
            For i = 0 To cameraRadioButton.Count - 1
                cameraRadioButton(i) = New RadioButton With {.Visible = True, .AutoSize = True,
                                       .Enabled = Main_UI.settings.cameraPresent(i), .Text = Main_UI.cameraNames(i)}
                CameraGroup.Controls.Add(cameraRadioButton(i))
                AddHandler cameraRadioButton(i).CheckedChanged, AddressOf cameraRadioButton_CheckChanged
            Next

            For i = 0 To WorkingResRadio.Count - 1
                WorkingResRadio(i) = New RadioButton With {.Text = resolutionList(i), .Tag = i,
                                     .AutoSize = True, .Visible = True}
                WorkingResRadio(i).Enabled = Main_UI.settings.resolutionsSupported(i)
                Resolutions.Controls.Add(WorkingResRadio(i))
            Next
        End If

        cameraRadioButton(Main_UI.settings.cameraIndex).Checked = True

        Snap640.Checked = Main_UI.settings.snap640
        Snap320.Checked = Main_UI.settings.snap320
        SnapCustom.Checked = Main_UI.settings.snapCustom

        TestAllDuration.Value = Main_UI.settings.testAllDuration
        cameraDisplayRes = Main_UI.settings.displayRes
        showConsoleLog.Checked = Main_UI.settings.showConsoleLog
    End Sub
    Private Sub OptionsDialog_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        If e.KeyCode = Keys.Escape Then Cancel_Button_Click(sender, e)
    End Sub
    Private Sub Cancel_Button_Click(sender As Object, e As EventArgs) Handles Cancel_Button.Click
        OptionsDialog_Load(sender, e) ' restore the settings to what they were on entry...
        Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Hide()
    End Sub
    Private Sub TestAllDuration_ValueChanged(sender As Object, e As EventArgs) Handles TestAllDuration.ValueChanged
        If TestAllDuration.Value < 5 Then TestAllDuration.Value = 5
        testDuration = TestAllDuration.Value
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        FontDialog1.Font = Main_UI.settings.fontInfo
        If FontDialog1.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            Main_UI.settings.fontInfo = FontDialog1.Font
        End If
    End Sub
    Public Sub Snap320_CheckedChanged(sender As Object, e As EventArgs) Handles Snap320.CheckedChanged
        Dim height = 180
        If cameraWorkingRes.Height = 120 Or cameraWorkingRes.Height = 240 Or cameraWorkingRes.Height = 480 Then height = 240
        cameraDisplayRes = New cv.Size(320, height)
    End Sub
    Public Sub Snap640_CheckedChanged(sender As Object, e As EventArgs) Handles Snap640.CheckedChanged
        Dim height = 360
        If cameraWorkingRes.Height = 120 Or cameraWorkingRes.Height = 240 Or cameraWorkingRes.Height = 480 Then height = 480
        cameraDisplayRes = New cv.Size(640, height)
    End Sub
    Public Sub SnapCustom_CheckedChanged(sender As Object, e As EventArgs) Handles SnapCustom.CheckedChanged
        cameraDisplayRes = New cv.Size(0, 0) ' figure it out in OpenCVB_UI.vb resizing...
    End Sub
End Class
