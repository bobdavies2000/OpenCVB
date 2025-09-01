Imports System.IO
Imports cv = OpenCvSharp
Public Class Options
    Public cameraRadioButton(Comm.cameraNames.Count - 1) As RadioButton
    Public workResRadio(Comm.resolutionList.Count - 1) As RadioButton
    Public cameraName As String
    Public cameraIndex As Integer
    Public testDuration As Integer
    Private Sub OKButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OKButton.Click
        OpenCVB.Main.settings.showConsoleLog = showConsoleLog.Checked
        OpenCVB.Main.settings.snap640 = Snap640.Checked
        OpenCVB.Main.settings.snap320 = Snap320.Checked
        OpenCVB.Main.settings.snapCustom = SnapCustom.Checked

        With OpenCVB.Main.settings
            For Each radio In Resolutions.Controls
                If radio.Checked Then
                    Dim strRes = radio.text.split(" ")
                    Dim resText = strRes(0)
                    Dim strVals = resText.split("x")
                    .workRes = New cv.Size(CInt(strVals(0)), CInt(strVals(1)))
                    Exit For
                End If
            Next

            Select Case .workRes.Height
                Case 270, 540, 1080
                    .captureRes = New cv.Size(1920, 1080)
                    If .camera1920x1080Support(.cameraIndex) = False Then
                        .captureRes = New cv.Size(1280, 720)
                        .workRes = New cv.Size(320, 180)
                    End If
                Case 180, 360, 720
                    .captureRes = New cv.Size(1280, 720)
                Case 376, 188, 94
                    If .cameraName <> "StereoLabs ZED 2/2i" Then
                        MessageBox.Show("The json settings don't appear to be correct!" + vbCrLf +
                                    "The 'settings.json' file will be removed" + vbCrLf +
                                    "and rebuilt with default settings upon restart.")
                        Dim fileinfo As New FileInfo(OpenCVB.Main.jsonfs.jsonFileName)
                        fileinfo.Delete()
                        End
                    End If
                    .captureRes = New cv.Size(672, 376)
                Case 120, 240, 480
                    .captureRes = New cv.Size(640, 480)
                    If .camera640x480Support(.cameraIndex) = False Then
                        .captureRes = New cv.Size(1280, 720)
                        .workRes = New cv.Size(320, 180)
                    End If
            End Select
        End With
        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub
    Public Sub defineCameraResolutions(index As Integer)
        ' see resolutionList above - helps to see how code maps to layout of the resolutions.
        Select Case Comm.cameraNames(index)
            Case "StereoLabs ZED 2/2i"
                OpenCVB.Main.settings.resolutionsSupported = New List(Of Boolean)({True, True, True,
                                                                           True, True, True,
                                                                           False, False, False,
                                                                           True, True, True,
                                                                           True, True, True})
            Case "Intel(R) RealSense(TM) Depth Camera 435i", "Intel(R) RealSense(TM) Depth Camera 455"
                OpenCVB.Main.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                                           True, True, True,
                                                                           True, True, True,
                                                                           False, False, False,
                                                                           False, False, False})
            Case "Oak-D camera"
                OpenCVB.Main.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                                           True, True, True,
                                                                           False, False, False,
                                                                           False, False, False,
                                                                           False, False, False})
            Case "Orbbec Gemini 335L", "Orbbec Gemini 336L", "Orbbec Gemini 335"
                OpenCVB.Main.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                                           True, True, True,
                                                                           True, True, True,
                                                                           False, False, False,
                                                                           False, False, False})
        End Select
    End Sub
    Private Sub cameraRadioButton_CheckChanged(sender As Object, e As EventArgs)
        Dim index = Comm.cameraNames.IndexOf(sender.text)
        cameraName = Comm.cameraNames(index)
        cameraIndex = index

        defineCameraResolutions(cameraIndex)

        For i = 0 To workResRadio.Count - 1
            workResRadio(i).Enabled = OpenCVB.Main.settings.resolutionsSupported(i)
        Next

        If cameraName.StartsWith("StereoLabs") Then
            workResRadio(Comm.resolutionList.IndexOf("336x188 - Quarter resolution")).Checked = True
        Else
            workResRadio(Comm.resolutionList.IndexOf("320x180 - Small resolution")).Checked = True
        End If
    End Sub
    Public Sub MainOptions_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Static radioButtonsPresent = False
        defineCameraResolutions(cameraIndex)

        If radioButtonsPresent = False Then
            radioButtonsPresent = True
            For i = 0 To cameraRadioButton.Count - 1
                cameraRadioButton(i) = New RadioButton With {.Visible = True, .AutoSize = True,
                                       .Enabled = OpenCVB.Main.settings.cameraPresent(i), .Text = Comm.cameraNames(i)}
                CameraGroup.Controls.Add(cameraRadioButton(i))
                AddHandler cameraRadioButton(i).CheckedChanged, AddressOf cameraRadioButton_CheckChanged
            Next

            For i = 0 To workResRadio.Count - 1
                workResRadio(i) = New RadioButton With {.Text = Comm.resolutionList(i), .Tag = i,
                                     .AutoSize = True, .Visible = True}
                workResRadio(i).Enabled = OpenCVB.Main.settings.resolutionsSupported(i)
                Resolutions.Controls.Add(workResRadio(i))
            Next
        End If

        cameraRadioButton(OpenCVB.Main.settings.cameraIndex).Checked = True

        Snap640.Checked = OpenCVB.Main.settings.snap640
        Snap320.Checked = OpenCVB.Main.settings.snap320
        SnapCustom.Checked = OpenCVB.Main.settings.snapCustom

        TestAllDuration.Value = OpenCVB.Main.settings.testAllDuration
        If TestAllDuration.Value < 5 Then TestAllDuration.Value = 5
        showConsoleLog.Checked = OpenCVB.Main.settings.showConsoleLog
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
        FontDialog1.Font = OpenCVB.Main.settings.fontInfo
        If FontDialog1.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            OpenCVB.Main.settings.fontInfo = FontDialog1.Font
        End If
    End Sub
    Public Sub Snap320_CheckedChanged(sender As Object, e As EventArgs) Handles Snap320.CheckedChanged
        Dim height = 180
        Dim h = OpenCVB.Main.settings.workRes.Height
        If h = 120 Or h = 240 Or h = 480 Then OpenCVB.Main.settings.displayRes = New cv.Size(320, 240)
    End Sub
    Public Sub Snap640_CheckedChanged(sender As Object, e As EventArgs) Handles Snap640.CheckedChanged
        Dim height = 360
        Dim h = OpenCVB.Main.settings.workRes.Height
        If h = 120 Or h = 240 Or h = 480 Then OpenCVB.Main.settings.displayRes = New cv.Size(640, 480)
    End Sub
    Public Sub UpdateXRef_Click(sender As Object, e As EventArgs) Handles UpdateXRef.Click
        Dim UIProcess As New Process
        UIProcess.StartInfo.FileName = OpenCVB.Main.HomeDir.FullName + "UI_Generator\bin\x64\Release\net8.0\UI_Generator.exe"
        UIProcess.StartInfo.WorkingDirectory = OpenCVB.Main.HomeDir.FullName + "UI_Generator\bin\x64\Release\net8.0\"
        UIProcess.StartInfo.Arguments = "All"
        UIProcess.Start()
    End Sub
End Class
