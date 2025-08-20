Imports cv = OpenCvSharp
Public Class Options
    Public cameraRadioButton(Main.cameraNames.Count - 1) As RadioButton
    Public workResRadio(resolutionList.Count - 1) As RadioButton
    Public cameraworkRes As cv.Size
    Public cameraDisplayRes As cv.Size
    Public cameraName As String
    Public cameraIndex As Integer
    Public testDuration As Integer
    '     "1344x752 - Full resolution", "672x376 - Quarter resolution", "336x188 - Small resolution  ",
    Public Shared resolutionList As New List(Of String)(
        {"1920x1080 - Full resolution", "960x540 - Quarter resolution", "480x270 - Small resolution",
         "1280x720 - Full resolution", "640x360 - Quarter resolution", "320x180 - Small resolution",
         "640x480 - Full resolution", "320x240 - Quarter resolution", "160x120 - Small resolution",
         "960x600 - Full resolution", "480x300 - Quarter resolution", "240x150 - Small resolution  ",
         "672x376 - Full resolution", "336x188 - Quarter resolution", "168x94 - Small resolution    "})

    Private Sub OKButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OKButton.Click
        Main.settings.showConsoleLog = showConsoleLog.Checked
        Main.settings.snap640 = Snap640.Checked
        Main.settings.snap320 = Snap320.Checked
        Main.settings.snapCustom = SnapCustom.Checked

        For Each radio In Resolutions.Controls
            If radio.Checked Then
                Main.settings.workResIndex = radio.Tag
                Dim strRes = radio.text.split(" ")
                Dim resText = strRes(0)
                Dim strVals = resText.split("x")
                cameraworkRes = New cv.Size(CInt(strVals(0)), CInt(strVals(1)))
                Exit For
            End If
        Next

        Main.settings.workRes = cameraworkRes
        Main.settings.displayRes = cameraDisplayRes

        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub
    Public Sub defineCameraResolutions(index As Integer)
        ' see resolutionList above - helps to see how code maps to layout of the resolutions.
        Select Case Main.cameraNames(index)
            Case "StereoLabs ZED 2/2i"
                Main.settings.resolutionsSupported = New List(Of Boolean)({True, True, True,
                                                                           True, True, True,
                                                                           False, False, False,
                                                                           True, True, True,
                                                                           True, True, True})
            Case "Intel(R) RealSense(TM) Depth Camera 435i", "Intel(R) RealSense(TM) Depth Camera 455"
                Main.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                                           True, True, True,
                                                                           True, True, True,
                                                                           False, False, False,
                                                                           False, False, False})
            Case "Oak-D camera"
                Main.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                                           True, True, True,
                                                                           False, False, False,
                                                                           False, False, False,
                                                                           False, False, False})
            Case "Orbbec Gemini 335L", "Orbbec Gemini 336L", "Orbbec Gemini 335"
                Main.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                                           True, True, True,
                                                                           True, True, True,
                                                                           False, False, False,
                                                                           False, False, False})
        End Select
    End Sub
    Private Sub cameraRadioButton_CheckChanged(sender As Object, e As EventArgs)
        Dim index = Main.cameraNames.IndexOf(sender.text)
        cameraName = Main.cameraNames(index)
        cameraIndex = index

        defineCameraResolutions(cameraIndex)

        For i = 0 To workResRadio.Count - 1
            workResRadio(i).Enabled = Main.settings.resolutionsSupported(i)
        Next

        If cameraName.StartsWith("StereoLabs") Then
            workResRadio(resolutionList.IndexOf("336x188 - Quarter resolution")).Checked = True
        Else
            workResRadio(resolutionList.IndexOf("320x180 - Small resolution")).Checked = True
        End If
    End Sub
    Public Sub MainOptions_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Static radioButtonsPresent = False
        defineCameraResolutions(cameraIndex)

        If radioButtonsPresent = False Then
            radioButtonsPresent = True
            For i = 0 To cameraRadioButton.Count - 1
                cameraRadioButton(i) = New RadioButton With {.Visible = True, .AutoSize = True,
                                       .Enabled = Main.settings.cameraPresent(i), .Text = Main.cameraNames(i)}
                CameraGroup.Controls.Add(cameraRadioButton(i))
                AddHandler cameraRadioButton(i).CheckedChanged, AddressOf cameraRadioButton_CheckChanged
            Next

            For i = 0 To workResRadio.Count - 1
                workResRadio(i) = New RadioButton With {.Text = resolutionList(i), .Tag = i,
                                     .AutoSize = True, .Visible = True}
                workResRadio(i).Enabled = Main.settings.resolutionsSupported(i)
                Resolutions.Controls.Add(workResRadio(i))
            Next
        End If

        cameraRadioButton(Main.settings.cameraIndex).Checked = True

        Snap640.Checked = Main.settings.snap640
        Snap320.Checked = Main.settings.snap320
        SnapCustom.Checked = Main.settings.snapCustom

        TestAllDuration.Value = Main.settings.testAllDuration
        If TestAllDuration.Value < 5 Then TestAllDuration.Value = 5
        cameraDisplayRes = Main.settings.displayRes
        showConsoleLog.Checked = Main.settings.showConsoleLog
    End Sub
    Private Sub MainOptions_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        If e.KeyCode = Keys.Escape Then Cancel_Button_Click(sender, e)
    End Sub
    Private Sub Cancel_Button_Click(sender As Object, e As EventArgs) Handles Cancel_Button.Click
        MainOptions_Load(sender, e) ' restore the settings to what they were on entry...
        Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Hide()
    End Sub
    Private Sub TestAllDuration_ValueChanged(sender As Object, e As EventArgs) Handles TestAllDuration.ValueChanged
        If TestAllDuration.Value < 5 Then TestAllDuration.Value = 5
        testDuration = TestAllDuration.Value
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        FontDialog1.Font = Main.settings.fontInfo
        If FontDialog1.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            Main.settings.fontInfo = FontDialog1.Font
        End If
    End Sub
    Public Sub Snap320_CheckedChanged(sender As Object, e As EventArgs) Handles Snap320.CheckedChanged
        Dim height = 180
        If cameraworkRes.Height = 120 Or cameraworkRes.Height = 240 Or cameraworkRes.Height = 480 Then height = 240
        cameraDisplayRes = New cv.Size(320, height)
    End Sub
    Public Sub Snap640_CheckedChanged(sender As Object, e As EventArgs) Handles Snap640.CheckedChanged
        Dim height = 360
        If cameraworkRes.Height = 120 Or cameraworkRes.Height = 240 Or cameraworkRes.Height = 480 Then height = 480
        cameraDisplayRes = New cv.Size(640, height)
    End Sub
    Public Sub SnapCustom_CheckedChanged(sender As Object, e As EventArgs) Handles SnapCustom.CheckedChanged
        ' cameraDisplayRes = New cv.Size(0, 0) ' figure it out in Main.vb resizing...
    End Sub

    Public Sub UpdateXRef_Click(sender As Object, e As EventArgs) Handles UpdateXRef.Click
        Dim UIProcess As New Process
        UIProcess.StartInfo.FileName = Main.HomeDir.FullName + "UI_Generator\bin\x64\Release\net8.0\UI_Generator.exe"
        UIProcess.StartInfo.WorkingDirectory = Main.HomeDir.FullName + "UI_Generator\bin\x64\Release\net8.0\"
        UIProcess.StartInfo.Arguments = "All"
        UIProcess.Start()
    End Sub
End Class
