Imports cv = OpenCvSharp
Public Class Options
    Public cameraRadioButton(Common.cameraNames.Count - 1) As RadioButton
    Public workResRadio() As RadioButton
    Public cameraName As String
    Public cameraIndex As Integer
    '     "1344x752 - Full resolution", "672x376 - Quarter resolution", "336x188 - Small resolution  ",
    Public resolutionList As New List(Of String)(
        {"1920x1080 - Full resolution", "960x540 - Quarter resolution", "480x270 - Small resolution",
         "1280x720 - Full resolution", "640x360 - Quarter resolution", "320x180 - Small resolution",
         "640x480 - Full resolution", "320x240 - Quarter resolution", "160x120 - Small resolution",
         "960x600 - Full resolution", "480x300 - Quarter resolution", "240x150 - Small resolution  ",
         "672x376 - Full resolution", "336x188 - Quarter resolution", "168x94 - Small resolution    "})
    Private Sub OKButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OKButton.Click
        OpenCVB.MainUI.settings.showBatchConsole = showBatchConsole.Checked
        OpenCVB.MainUI.settings.snap640 = Snap640.Checked
        OpenCVB.MainUI.settings.snap320 = Snap320.Checked
        OpenCVB.MainUI.settings.snapCustom = SnapCustom.Checked

        With OpenCVB.MainUI.settings
            For Each radio In Resolutions.Controls
                If radio.Checked Then
                    Dim strRes = radio.text.split(" ")
                    Dim resText = strRes(0)
                    Dim strVals = resText.split("x")
                    .workRes = New cv.Size(CInt(strVals(0)), CInt(strVals(1)))
                    Exit For
                End If
            Next
        End With
        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub
    Public Sub defineCameraResolutions(index As Integer)
        ' see resolutionList - helps to see how code maps to layout of the resolutions.
        Select Case Common.cameraNames(index)
            Case "StereoLabs ZED 2/2i"
                OpenCVB.MainUI.settings.resolutionsSupported = New List(Of Boolean)({True, True, True,
                                                                           True, True, True,
                                                                           False, False, False,
                                                                           True, True, True,
                                                                           True, True, True})
            Case "Intel(R) RealSense(TM) Depth Camera 435i", "Intel(R) RealSense(TM) Depth Camera 455"
                OpenCVB.MainUI.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                                           True, True, True,
                                                                           True, True, True,
                                                                           False, False, False,
                                                                           False, False, False})
            Case "Oak-D camera"
                OpenCVB.MainUI.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                                           True, True, True,
                                                                           False, False, False,
                                                                           False, False, False,
                                                                           False, False, False})
            Case "Orbbec Gemini 335L", "Orbbec Gemini 336L", "Orbbec Gemini 335"
                OpenCVB.MainUI.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                                           True, True, True,
                                                                           True, True, True,
                                                                           False, False, False,
                                                                           False, False, False})
        End Select
    End Sub
    Private Sub cameraRadioButton_CheckChanged(sender As Object, e As EventArgs)
        Dim index = Common.cameraNames.IndexOf(sender.text)
        cameraName = Common.cameraNames(index)
        cameraIndex = index

        defineCameraResolutions(cameraIndex)

        For i = 0 To workResRadio.Count - 1
            workResRadio(i).Enabled = OpenCVB.MainUI.settings.resolutionsSupported(i)
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
                                       .Enabled = OpenCVB.MainUI.settings.cameraPresent(i), .Text = Common.cameraNames(i)}
                CameraGroup.Controls.Add(cameraRadioButton(i))
                AddHandler cameraRadioButton(i).CheckedChanged, AddressOf cameraRadioButton_CheckChanged
            Next

            ReDim workResRadio(resolutionList.Count - 1)
            For i = 0 To workResRadio.Count - 1
                workResRadio(i) = New RadioButton With {.Text = resolutionList(i), .Tag = i,
                                     .AutoSize = True, .Visible = True}
                workResRadio(i).Enabled = OpenCVB.MainUI.settings.resolutionsSupported(i)
                Resolutions.Controls.Add(workResRadio(i))
            Next
        End If

        cameraRadioButton(OpenCVB.MainUI.settings.cameraIndex).Checked = True

        Snap640.Checked = OpenCVB.MainUI.settings.snap640
        Snap320.Checked = OpenCVB.MainUI.settings.snap320
        SnapCustom.Checked = OpenCVB.MainUI.settings.snapCustom

        showBatchConsole.Checked = OpenCVB.MainUI.settings.showBatchConsole
    End Sub
    Public Function setDuration() As Integer
        Dim duration = 5
        Select Case OpenCVB.MainUI.settings.workRes.Width
            Case 1920
                duration = 40
            Case 1280
                duration = 35
            Case 960
                duration = 30
            Case 672
                duration = 15
            Case 640
                duration = 15
            Case 480
                duration = 10
            Case 240, 336, 320, 168, 160
                duration = 5
        End Select
        Return duration
    End Function
    Private Sub MainOptions_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        If e.KeyCode = Keys.Escape Then Cancel_Button_Click(sender, e)
    End Sub
    Private Sub Cancel_Button_Click(sender As Object, e As EventArgs) Handles Cancel_Button.Click
        MainOptions_Load(sender, e) ' restore the settings to what they were on entry...
        Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Hide()
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        FontDialog1.Font = OpenCVB.MainUI.settings.fontInfo
        If FontDialog1.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            OpenCVB.MainUI.settings.fontInfo = FontDialog1.Font
        End If
    End Sub
    Public Sub Snap320_CheckedChanged(sender As Object, e As EventArgs) Handles Snap320.CheckedChanged
        Dim h = OpenCVB.MainUI.settings.workRes.Height
        If h = 120 Or h = 240 Or h = 480 Then
            OpenCVB.MainUI.settings.displayRes = New cv.Size(320, 240)
        End If
    End Sub
    Public Sub Snap640_CheckedChanged(sender As Object, e As EventArgs) Handles Snap640.CheckedChanged
        Dim h = OpenCVB.MainUI.settings.workRes.Height
        If h = 120 Or h = 240 Or h = 480 Then
            OpenCVB.MainUI.settings.displayRes = New cv.Size(640, 480)
        End If
    End Sub
End Class
