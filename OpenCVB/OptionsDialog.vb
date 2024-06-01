Imports cv = OpenCvSharp
Public Class OptionsDialog
    Public cameraRadioButton(OpenCVB.cameraNames.Count - 1) As RadioButton
    Public workingResRadio(resolutionList.Count - 1) As RadioButton
    Public cameraWorkingRes As cv.Size
    Public cameraDisplayRes As cv.Size
    Public cameraName As String
    Public cameraIndex As Integer
    Public testDuration As Integer
    Public Shared resolutionList As New List(Of String)(
        {"1920x1080 - Full resolution", "960x540 - Quarter resolution", "480x270 - Small resolution",
         "1280x720 - Full resolution  ", "640x360 - Quarter resolution", "320x180 - Small resolution",
         "640x480 - Full resolution    ", "320x240 - Quarter resolution", "160x120 - Small resolution",
         "672x376 - Full resolution    ", "336x188 - Quarter resolution", "168x94 - Small resolution    "})

    Private Sub OKButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OKButton.Click
        OpenCVB.settings.showConsoleLog = showConsoleLog.Checked
        OpenCVB.settings.snap640 = Snap640.Checked
        OpenCVB.settings.snap320 = Snap320.Checked
        OpenCVB.settings.snapCustom = SnapCustom.Checked

        For Each radio In Resolutions.Controls
            If radio.Checked Then
                OpenCVB.settings.workingResIndex = radio.Tag
                Dim strRes = radio.text.split(" ")
                Dim resText = strRes(0)
                Dim strVals = resText.split("x")
                cameraWorkingRes = New cv.Size(CInt(strVals(0)), CInt(strVals(1)))
                Exit For
            End If
        Next

        OpenCVB.settings.workingRes = cameraWorkingRes
        OpenCVB.settings.displayRes = cameraDisplayRes

        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub
    Public Sub defineCameraResolutions(index As Integer)
        Select Case OpenCVB.cameraNames(index)
            Case "Azure Kinect 4K"
                OpenCVB.settings.resolutionsSupported = New List(Of Boolean)({True, True, True, True,
                                                        True, True, False, False, False, False, False, False})
            Case "Intel(R) RealSense(TM) Depth Camera 435i"
                OpenCVB.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                        True, True, True, True, True, True, False, False, False})
            Case "Intel(R) RealSense(TM) Depth Camera 455"
                OpenCVB.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                        True, True, True, True, True, True, False, False, False})
            Case "Oak-D camera"
                OpenCVB.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                        True, True, True, False, False, False, False, False, False})
            Case "StereoLabs ZED 2/2i"
                OpenCVB.settings.resolutionsSupported = New List(Of Boolean)({True, True, True,
                                                        True, True, True, False, False, False, True, True, True})
            Case "MYNT-EYE-D1000"
                OpenCVB.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                        True, True, True, False, False, False, False, False, False})
            Case "Orbbec Gemini 335L"
                OpenCVB.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                        False, False, False, True, False, False, False, False, False})
        End Select
    End Sub
    Private Sub cameraRadioButton_CheckChanged(sender As Object, e As EventArgs)
        Dim index = OpenCVB.cameraNames.IndexOf(sender.text)
        If sender.checked = False Then Exit Sub
        cameraName = OpenCVB.cameraNames(index)
        cameraIndex = index

        defineCameraResolutions(cameraIndex)

        For i = 0 To workingResRadio.Count - 1
            workingResRadio(i).Enabled = OpenCVB.settings.resolutionsSupported(i)
        Next

        If cameraName.StartsWith("Intel") Then
            workingResRadio(resolutionList.IndexOf("320x240 - Quarter resolution")).Checked = True
        Else
            workingResRadio(resolutionList.IndexOf("320x180 - Small resolution")).Checked = True
        End If
        If cameraName.StartsWith("StereoLabs") Then
            workingResRadio(resolutionList.IndexOf("336x188 - Quarter resolution")).Checked = True
        End If
        If cameraName.StartsWith("Azure Kinect 4K") Then
            workingResRadio(resolutionList.IndexOf("320x180 - Small resolution")).Checked = True
        End If
        If cameraName.StartsWith("Orbbec Gemini 335L") Then
            workingResRadio(resolutionList.IndexOf("640x480 - Full resolution    ")).Checked = True
        End If
    End Sub
    Public Sub OptionsDialog_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Static radioButtonsPresent = False
        defineCameraResolutions(cameraIndex)

        If radioButtonsPresent = False Then
            radioButtonsPresent = True
            For i = 0 To cameraRadioButton.Count - 1
                cameraRadioButton(i) = New RadioButton With {.Visible = True, .AutoSize = True,
                                       .Enabled = OpenCVB.settings.cameraPresent(i), .Text = OpenCVB.cameraNames(i)}
                CameraGroup.Controls.Add(cameraRadioButton(i))
                AddHandler cameraRadioButton(i).CheckedChanged, AddressOf cameraRadioButton_CheckChanged
            Next

            For i = 0 To workingResRadio.Count - 1
                workingResRadio(i) = New RadioButton With {.Text = resolutionList(i), .Tag = i,
                                     .AutoSize = True, .Visible = True}
                workingResRadio(i).Enabled = OpenCVB.settings.resolutionsSupported(i)
                Resolutions.Controls.Add(workingResRadio(i))
            Next
        End If

        cameraRadioButton(OpenCVB.settings.cameraIndex).Checked = True

        Snap640.Checked = OpenCVB.settings.snap640
        Snap320.Checked = OpenCVB.settings.snap320
        SnapCustom.Checked = OpenCVB.settings.snapCustom

        TestAllDuration.Value = OpenCVB.settings.testAllDuration
        cameraDisplayRes = OpenCVB.settings.displayRes
        showConsoleLog.Checked = OpenCVB.settings.showConsoleLog
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
    Private Sub Button1_Click(sender As Object, e As EventArgs)
        FontDialog1.Font = OpenCVB.settings.fontInfo
        If FontDialog1.ShowDialog() = Windows.Forms.DialogResult.OK Then OpenCVB.settings.fontInfo = FontDialog1.Font
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
        cameraDisplayRes = New cv.Size(0, 0) ' figure it out in OpenCVB.vb resizing...
    End Sub
End Class
