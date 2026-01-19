Imports cv = OpenCvSharp
Public Class Options
    Public cameraRadioButton(cameraNames.Count - 1) As RadioButton
    Public workResRadio() As RadioButton
    '     "1344x752 - Full resolution", "672x376 - Quarter resolution", "336x188 - Small resolution  ",
    Public resolutionList As New List(Of String)(
        {"1920x1080 - Full resolution", "960x540 - Quarter resolution", "480x270 - Small resolution",
         "1280x720 - Full resolution", "640x360 - Quarter resolution", "320x180 - Small resolution",
         "640x480 - Full resolution", "320x240 - Quarter resolution", "160x120 - Small resolution",
         "960x600 - Full resolution", "480x300 - Quarter resolution", "240x150 - Small resolution  ",
         "672x376 - Full resolution", "336x188 - Quarter resolution", "168x94 - Small resolution    "})
    Dim formLoadComplete As Boolean
    Private Sub OKButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OKButton.Click
        With settings
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
    End Sub
    Public Sub defineCameraResolutions()
        ' see resolutionList - helps to see how code maps to layout of the resolutions.
        Select Case settings.cameraName
            Case "StereoLabs ZED 2/2i"
                settings.resolutionsSupported = New List(Of Boolean)({True, True, True,
                                                                      True, True, True,
                                                                      False, False, False,
                                                                      True, True, True,
                                                                      True, True, True})
            Case "Intel(R) RealSense(TM) Depth Camera 435i", "Intel(R) RealSense(TM) Depth Camera 455"
                settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                                      True, True, True,
                                                                      False, False, False,
                                                                      False, False, False,
                                                                      False, False, False})
            Case "Oak-D camera"
                settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                                      True, True, True,
                                                                      True, True, True,
                                                                      False, False, False,
                                                                      False, False, False})
            Case "Orbbec Gemini 335L", "Orbbec Gemini 336L", "Orbbec Gemini 335"
                settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                                      True, True, True,
                                                                      True, True, True,
                                                                      False, False, False,
                                                                      False, False, False})
        End Select
    End Sub
    Private Sub cameraRadioButton_CheckChanged(sender As Object, e As EventArgs)
        If formLoadComplete = False Then Exit Sub

        settings.cameraName = sender.text

        defineCameraResolutions()

        For i = 0 To workResRadio.Count - 1
            workResRadio(i).Enabled = settings.resolutionsSupported(i)
        Next

        If settings.cameraName.StartsWith("StereoLabs") Then
            workResRadio(resolutionList.IndexOf("336x188 - Quarter resolution")).Checked = True
        Else
            workResRadio(resolutionList.IndexOf("320x180 - Small resolution")).Checked = True
        End If
    End Sub
    Public Sub MainOptions_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim resStr = CStr(settings.workRes.Width) + "x" + CStr(settings.workRes.Height)

        If formLoadComplete = False Then
            For i = 0 To cameraRadioButton.Count - 1
                cameraRadioButton(i) = New RadioButton With {.Visible = True, .AutoSize = True,
                                       .Enabled = settings.cameraPresent(i), .Text = cameraNames(i)}
                CameraGroup.Controls.Add(cameraRadioButton(i))
                AddHandler cameraRadioButton(i).CheckedChanged, AddressOf cameraRadioButton_CheckChanged
            Next

            defineCameraResolutions()

            ReDim workResRadio(resolutionList.Count - 1)
            For i = 0 To workResRadio.Count - 1
                workResRadio(i) = New RadioButton With {.Text = resolutionList(i), .Tag = i,
                                         .AutoSize = True, .Visible = True}
                workResRadio(i).Enabled = settings.resolutionsSupported(i)
                Resolutions.Controls.Add(workResRadio(i))
            Next
        End If

        For i = 0 To cameraRadioButton.Count - 1
            If settings.cameraName = cameraNames(i) Then cameraRadioButton(i).Checked = True
        Next

        For i = 0 To workResRadio.Count - 1
            If resolutionList(i).StartsWith(resStr) Then workResRadio(i).Checked = True
        Next
        formLoadComplete = True
    End Sub
    Private Sub MainOptions_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        If e.KeyCode = Keys.Escape Then Cancel_Button_Click(sender, e)
    End Sub
    Private Sub Cancel_Button_Click(sender As Object, e As EventArgs) Handles Cancel_Button.Click
        Me.DialogResult = DialogResult.Cancel
    End Sub
End Class