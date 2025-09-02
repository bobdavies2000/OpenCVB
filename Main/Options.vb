Imports System.IO
Imports cv = OpenCvSharp
Public Class Options
    Public cameraRadioButton(Comm.cameraNames.Count - 1) As RadioButton
    Public workResRadio(Comm.resolutionList.Count - 1) As RadioButton
    Public cameraName As String
    Public cameraIndex As Integer
    Public testDuration As Integer
    Private Sub OKButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OKButton.Click
        OpenCVB.MainForm.settings.showConsoleLog = showConsoleLog.Checked
        OpenCVB.MainForm.settings.snap640 = Snap640.Checked
        OpenCVB.MainForm.settings.snap320 = Snap320.Checked
        OpenCVB.MainForm.settings.snapCustom = SnapCustom.Checked

        With OpenCVB.MainForm.settings
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
        ' see resolutionList above - helps to see how code maps to layout of the resolutions.
        Select Case Comm.cameraNames(index)
            Case "StereoLabs ZED 2/2i"
                OpenCVB.MainForm.settings.resolutionsSupported = New List(Of Boolean)({True, True, True,
                                                                           True, True, True,
                                                                           False, False, False,
                                                                           True, True, True,
                                                                           True, True, True})
            Case "Intel(R) RealSense(TM) Depth Camera 435i", "Intel(R) RealSense(TM) Depth Camera 455"
                OpenCVB.MainForm.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                                           True, True, True,
                                                                           True, True, True,
                                                                           False, False, False,
                                                                           False, False, False})
            Case "Oak-D camera"
                OpenCVB.MainForm.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
                                                                           True, True, True,
                                                                           False, False, False,
                                                                           False, False, False,
                                                                           False, False, False})
            Case "Orbbec Gemini 335L", "Orbbec Gemini 336L", "Orbbec Gemini 335"
                OpenCVB.MainForm.settings.resolutionsSupported = New List(Of Boolean)({False, False, False,
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
            workResRadio(i).Enabled = OpenCVB.MainForm.settings.resolutionsSupported(i)
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
                                       .Enabled = OpenCVB.MainForm.settings.cameraPresent(i), .Text = Comm.cameraNames(i)}
                CameraGroup.Controls.Add(cameraRadioButton(i))
                AddHandler cameraRadioButton(i).CheckedChanged, AddressOf cameraRadioButton_CheckChanged
            Next

            For i = 0 To workResRadio.Count - 1
                workResRadio(i) = New RadioButton With {.Text = Comm.resolutionList(i), .Tag = i,
                                     .AutoSize = True, .Visible = True}
                workResRadio(i).Enabled = OpenCVB.MainForm.settings.resolutionsSupported(i)
                Resolutions.Controls.Add(workResRadio(i))
            Next
        End If

        cameraRadioButton(OpenCVB.MainForm.settings.cameraIndex).Checked = True

        Snap640.Checked = OpenCVB.MainForm.settings.snap640
        Snap320.Checked = OpenCVB.MainForm.settings.snap320
        SnapCustom.Checked = OpenCVB.MainForm.settings.snapCustom

        TestAllDuration.Value = OpenCVB.MainForm.settings.testAllDuration
        If TestAllDuration.Value < 5 Then TestAllDuration.Value = 5
        showConsoleLog.Checked = OpenCVB.MainForm.settings.showConsoleLog
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
        FontDialog1.Font = OpenCVB.MainForm.settings.fontInfo
        If FontDialog1.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            OpenCVB.MainForm.settings.fontInfo = FontDialog1.Font
        End If
    End Sub
    Public Sub Snap320_CheckedChanged(sender As Object, e As EventArgs) Handles Snap320.CheckedChanged
        Dim h = OpenCVB.MainForm.settings.workRes.Height
        If h = 120 Or h = 240 Or h = 480 Then
            OpenCVB.MainForm.settings.displayRes = New cv.Size(320, 240)
        End If
    End Sub
    Public Sub Snap640_CheckedChanged(sender As Object, e As EventArgs) Handles Snap640.CheckedChanged
        Dim h = OpenCVB.MainForm.settings.workRes.Height
        If h = 120 Or h = 240 Or h = 480 Then
            OpenCVB.MainForm.settings.displayRes = New cv.Size(640, 480)
        End If
    End Sub
    Public Sub UpdateXRef_Click(sender As Object, e As EventArgs) Handles UpdateXRef.Click
        Dim UIProcess As New Process
        UIProcess.StartInfo.FileName = OpenCVB.MainForm.HomeDir.FullName + "UI_Generator\bin\x64\Release\net8.0\UI_Generator.exe"
        UIProcess.StartInfo.WorkingDirectory = OpenCVB.MainForm.HomeDir.FullName + "UI_Generator\bin\x64\Release\net8.0\"
        UIProcess.StartInfo.Arguments = "All"
        UIProcess.Start()
    End Sub
End Class
