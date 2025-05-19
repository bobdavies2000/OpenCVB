Imports cv = OpenCvSharp
Imports  System.IO
Imports NAudio.Wave
Imports NAudio.Wave.SampleProviders.SignalGeneratorType
' https://archive.codeplex.com/?p=naudio
' http://ismir2002.ismir.net/proceedings/02-FP04-2.pdf
Public Class Sound_Basics : Inherits TaskParent
    Dim memData As WaveBuffer
    Dim pcmData8() As Short
    Dim pcmData16() As Short
    Dim currentTime As Date
    Dim startTime As Date
    Dim fileNameForm As OptionsFileName
    Public pcm32f As New cv.Mat
    Public stereo As Boolean
    Public bpp16 As Boolean
    Public pcmDuration As Double ' in seconds.
    Public player As IWavePlayer
    Public reader As MediaFoundationReader
    Private Sub LoadSoundData()
        Dim tmp(reader.Length - 1) As Byte
        Dim count = reader.Read(tmp, 0, tmp.Length)
        stereo = reader.WaveFormat.Channels() = 2
        bpp16 = reader.WaveFormat.BitsPerSample = 16
        memData = New WaveBuffer(tmp)
        If stereo Then
            ReDim pcmData16(count / reader.WaveFormat.Channels() - 1)
            For i = 0 To count / reader.WaveFormat.Channels() - 1
                pcmData16(i) = memData.ShortBuffer(i)
            Next
        Else
            ReDim pcmData8(count - 1)
            For i = 0 To count - 1
                pcmData8(i) = memData.ByteBuffer(i)
            Next
        End If
        pcmDuration = reader.TotalTime.TotalSeconds
    End Sub
    Public Sub New()

        fileNameForm = New OptionsFileName
        fileNameForm.OpenFileDialog1.InitialDirectory = task.HomeDir + "Data\"
        fileNameForm.OpenFileDialog1.FileName = "*.*"
        fileNameForm.OpenFileDialog1.CheckFileExists = False
        fileNameForm.OpenFileDialog1.Filter = "m4a (*.m4a)|*.m4a|mp3 (*.mp3)|*.mp3|mp4 (*.mp4)|*.mp4|wav (*.wav)|*.wav|aac (*.aac)|*.aac|All files (*.*)|*.*"
        fileNameForm.OpenFileDialog1.FilterIndex = 1
        fileNameForm.filename.Text = GetSetting("OpenCVB", "AudioFileName", "AudioFileName", task.HomeDir + "Data/01 I Let the Music Speak.m4a")
        fileNameForm.Text = "Select an audio file to analyze"
        fileNameForm.FileNameLabel.Text = "Select a file for use with the Sound_Basics algorithm."
        fileNameForm.PlayButton.Hide()
        fileNameForm.Setup(traceName)
        fileNameForm.Show()

        desc = "Load an audio file, play it, and convert to PCM"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If task.testAllRunning Then Exit Sub ' there have been some failures in player.Init below when running during a test all.  Skip so testing can proceed.
        Dim sender As New Object, e As New EventArgs
        Static fileinfo = New FileInfo(fileNameForm.filename.Text)
        If fileinfo.Exists And fileNameForm.PlayButton.Text = "Start" Then
            fileNameForm.PlayButton_Click(sender, e)

            reader = New MediaFoundationReader(fileinfo.FullName)
            LoadSoundData()
            reader = New MediaFoundationReader(fileinfo.FullName)
            SaveSetting("OpenCVB", "AudioFileName", "AudioFileName", fileinfo.FullName)

            player = New WaveOut
            player.Init(reader)
            player.Play()

            Dim channels = reader.WaveFormat.Channels
            Dim bpSample = reader.WaveFormat.BitsPerSample
            Dim mattype = cv.MatType.CV_16SC2
            If bpSample = 8 And channels = 1 Then mattype = cv.MatType.CV_8U
            If bpSample = 8 And channels = 2 Then mattype = cv.MatType.CV_8UC2
            If bpSample = 16 And channels = 1 Then mattype = cv.MatType.CV_16SC1
            Dim input As New cv.Mat
            If bpSample = 16 Then
                input = cv.Mat.FromPixelData(pcmData16.Length / channels, 1, mattype, pcmData16)
            Else
                input = cv.Mat.FromPixelData(pcmData8.Length, 1, mattype, pcmData8)
            End If
            input.ConvertTo(pcm32f, cv.MatType.CV_32F)
            startTime = Now
        End If
        If fileNameForm.PlayButton.Text = "Stop" And pcmDuration > 0 Then
            fileNameForm.TrackBar1.Value = (Now - startTime).TotalSeconds / pcmDuration * 10000
        Else
            fileNameForm.PlayButton_Click(sender, e)
            player?.Stop()
        End If
        SetTrueText("Requested sound data is in the pcm32f cv.Mat")
    End Sub
    Public Sub Close()
        player?.Stop()
        player?.Dispose()
        reader?.Dispose()
    End Sub
End Class





' https://github.com/naudio/sinegenerator-sample
Public Class Sound_SignalGenerator : Inherits TaskParent
    Dim player As NAudio.Wave.IWavePlayer
    Dim wGen As New NAudio.Wave.SampleProviders.SignalGenerator
    Public pcm32f As New cv.Mat
    Public stereo As Boolean = False ' only mono generated sound
    Public bpp16 As Boolean = False ' only 8 bit generated sound
    Public pcmDuration As Double ' in seconds.
    Dim pcmData() As Single
    Dim generatedSamplesPerSecond As Integer = 44100
    Dim startTime As Date
    Dim radioIndex As Integer
    Dim saveRadioIndex = -1
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Sine Wave Frequency", 10, 4000, 1000)
            sliders.setupTrackBar("Decibels", -100, 0, -20)
            sliders.setupTrackBar("Sweep Only - End Frequency", 20, 4000, 1000)
            sliders.setupTrackBar("Sweep Only - duration secs", 0, 10, 1)
            sliders.setupTrackBar("Retain Data for x seconds", 1, 100, 2)
        End If

        If radio.Setup(traceName) Then
            Dim radioChoices = {"Pink", "White", "Sweep", "Sin", "Square", "Triangle", "SawTooth"}
            For i = 0 To radioChoices.Count - 1
                radio.addRadio(radioChoices(i))
            Next
            radio.check(0).Checked = True
        End If

        If check.Setup(traceName) Then
            check.addCheckBox("PhaseReverse Left")
            check.addCheckBox("PhaseReverse Right")
        End If

        If task.testAllRunning = False Then
            ' there have been some failures in player.Init below when running during a test all.  Skip so testing can proceed.
            player = New WaveOut
            player.Init(wGen)
        End If

        desc = "Generate sound with a sine waveform."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If task.testAllRunning Then Exit Sub ' there have been some failures in player.Init below when running during a test all.  Skip so testing can proceed.
        Static wgenSlider =OptionParent.FindSlider("Sine Wave Frequency")
        Static DecibelSlider =OptionParent.FindSlider("Decibels")
        Static endSweepSlider =OptionParent.FindSlider("Sweep Only - End Frequency")
        Static sweepDurationSlider =OptionParent.FindSlider("Sweep Only - duration secs")
        Static retainSlider =OptionParent.FindSlider("Retain Data for x seconds")
        Static reverse0Check = OptionParent.findCheckBox("PhaseReverse Left")
        Static reverse1Check = OptionParent.findCheckBox("PhaseReverse Right")

        Static frm = OptionParent.findFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                wGen.Type = Choose(i + 1, Pink, white, Sweep, Sin, Square, Triangle, SawTooth)
                radioIndex = i
                Exit For
            End If
        Next

        If wgenSlider.Value <> wGen.Frequency Or retainSlider.Value <> pcmDuration Or saveRadioIndex = radioIndex Then
            If pcmDuration <> retainSlider.Value Then
                pcmDuration = retainSlider.Value
                ReDim pcmData(pcmDuration * generatedSamplesPerSecond - 1) ' enough for about 10 seconds of audio.
                startTime = Now
            End If

            wGen.PhaseReverse(0) = reverse0Check.Checked
            wGen.PhaseReverse(1) = reverse1Check.Checked

            wGen.Frequency = wgenSlider.Value
            wGen.Gain = NAudio.Utils.Decibels.DecibelsToLinear(DecibelSlider.Value)

            If wGen.Type = Sweep Then
                wGen.FrequencyEnd = endSweepSlider.Value
                wGen.SweepLengthSecs = sweepDurationSlider.Value
            End If

            Dim count = wGen.Read(pcmData, 0, pcmData.Length)
            pcm32f = cv.Mat.FromPixelData(pcmData.Length, 1, cv.MatType.CV_32F, pcmData)
            player.Play()
        End If
        SetTrueText("Requested sound data is in the pcm32f cv.Mat")
    End Sub
    Public Sub Close()
        player?.Stop()
        player?.Dispose()
    End Sub
End Class






Public Class Sound_Display : Inherits TaskParent
    Public soundSource As Object = New Sound_SignalGenerator
    Dim sliderPercent As Single
    Dim fileStarted As Boolean
    Dim formatIndex As Integer
    Dim samplesperLine As Single
    Dim starttime As Date
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Max Absolute Value")
            radio.addRadio("Max RMS Value")
            radio.addRadio("Sampled Peaks")
            radio.addRadio("Scaled Average")
            radio.check(0).Checked = True
        End If

        desc = "Display a sound buffer in several styles"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If task.testAllRunning Then Exit Sub ' there have been some failures in player.Init below when running during a test all.  Skip so testing can proceed.
        If standaloneTest() Then soundSource.Run(src)
        If fileStarted = False Then
            fileStarted = True
            soundSource.Run(src)
            sliderPercent = 0
            starttime = Now

            If soundSource.pcm32f.width = 0 Then Exit Sub ' sound hasn't loaded yet.
        End If

        Dim totalSamples = soundSource.pcm32f.Rows
        If totalSamples = 0 Then
            SetTrueText("No sound data was loaded.")
            Exit Sub
        End If
        dst2 = New cv.Mat(New cv.Size(src.Width * 2, src.Height), cv.MatType.CV_8UC3, cv.Scalar.Beige)
        samplesperLine = If(soundSource.stereo, totalSamples / 2 / dst2.Width, totalSamples / dst2.Width)
        Static frm = OptionParent.findFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then formatIndex = i
        Next

        Dim pcm = soundSource.pcm32f
        Dim absMinVal As Double, absMaxVal As Double
        pcm.MinMaxLoc(absMinVal, absMaxVal)
        If Double.IsNaN(absMaxVal) Or Double.IsNaN(absMinVal) Then Exit Sub ' bad input data...
        If Double.IsNegativeInfinity(absMinVal) Or Double.IsInfinity(absMaxVal) Then Exit Sub ' bad input data...
        Dim halfHeight As Integer = dst2.Height / 2
        Dim minVal As Double, maxVal As Double
        Select Case formatIndex
            Case 0
                For i = 0 To dst2.Width - 1
                    Dim rect = New cv.Rect(0, i * samplesperLine, 1, samplesperLine)
                    If rect.Y + rect.Height > pcm.height Then rect.Height = pcm.Height - rect.Y ' rounding possible when changing buffer size...
                    pcm(rect).MinMaxLoc(minVal, maxVal)
                    If minVal > 0 Then minVal = 0
                    If maxVal < 0 Then maxVal = 0

                    DrawLine(dst2, New cv.Point(i, halfHeight), New cv.Point(i, CInt(halfHeight - halfHeight * maxVal / absMaxVal)), cv.Scalar.Red)
                    DrawLine(dst2, New cv.Point(i, halfHeight), New cv.Point(i, CInt(halfHeight + Math.Abs(minVal) * halfHeight / -absMinVal)), cv.Scalar.Gray)
                Next
                labels(2) = CStr(CInt(soundSource.pcmDuration)) + " seconds displayed with Max Absolute Value"
            Case 1
                For i = 0 To dst2.Width - 1
                    Dim rect = New cv.Rect(0, i * samplesperLine, 1, samplesperLine)
                    If rect.Y + rect.Height > pcm.height Then rect.Height = pcm.Height - rect.Y ' rounding possible when changing buffer size...
                    Dim tmp = pcm(rect).mul(pcm(rect)).toMat()
                    Dim sum = tmp.sum()
                    Dim nextVal = Math.Sqrt(sum(0) / samplesperLine)

                    DrawLine(dst2, New cv.Point(i, halfHeight), New cv.Point(i, CInt(halfHeight - halfHeight * nextVal / absMaxVal)), cv.Scalar.Red)
                    DrawLine(dst2, New cv.Point(i, halfHeight), New cv.Point(i, CInt(halfHeight + halfHeight * nextVal / -absMinVal)), cv.Scalar.Gray)
                Next
                labels(2) = CStr(CInt(soundSource.pcmDuration)) + " seconds displayed with Max RMS Value"
            Case 2
                For i = 0 To dst2.Width - 1
                    Dim rect = New cv.Rect(0, i * samplesperLine, 1, samplesperLine)
                    If rect.Y + rect.Height > pcm.height Then rect.Height = pcm.Height - rect.Y ' rounding possible when changing buffer size...
                    pcm(rect).MinMaxLoc(minVal, maxVal)
                    If minVal > 0 Then minVal = 0
                    If maxVal < 0 Then maxVal = 0

                    DrawLine(dst2, New cv.Point(i, halfHeight), New cv.Point(i, CInt(halfHeight - halfHeight * maxVal / absMaxVal)), cv.Scalar.Red)
                    DrawLine(dst2, New cv.Point(i, halfHeight), New cv.Point(i, CInt(halfHeight + Math.Abs(minVal) * halfHeight / -absMinVal)), cv.Scalar.Gray)
                Next
            Case 3
                pcm = cv.Cv2.Abs(pcm).toMat
                For i = 0 To dst2.Width - 1
                    Dim rect = New cv.Rect(0, i * samplesperLine, 1, samplesperLine)
                    If rect.Y + rect.Height > pcm.height Then rect.Height = pcm.Height - rect.Y ' rounding possible when changing buffer size...
                    Dim sum = pcm(rect).sum
                    Dim nextVal = sum(0) / samplesperLine

                    DrawLine(dst2, New cv.Point(i, halfHeight), New cv.Point(i, CInt(halfHeight - halfHeight * nextVal / absMaxVal)), cv.Scalar.Red)
                    DrawLine(dst2, New cv.Point(i, halfHeight), New cv.Point(i, CInt(halfHeight + halfHeight * nextVal / -absMinVal)), cv.Scalar.Gray)
                Next
                labels(2) = CStr(CInt(soundSource.pcmDuration)) + " seconds displayed with Scaled Average"
        End Select
        sliderPercent = If(fileStarted, (Now - starttime).TotalSeconds / soundSource.pcmduration, 0)
        ' when playing back an audio file, restart at the beginning when it is over...
        If sliderPercent > 0.99 Or fileStarted = False Then
            sliderPercent = 0
            starttime = Now
        End If
        Dim x = dst2.Width * sliderPercent
        dst2.Line(New cv.Point(x, 0), New cv.Point(x, dst2.Height), cv.Scalar.Black, task.lineWidth + 1)
    End Sub
End Class








Public Class Sound_GenWaveDisplay : Inherits TaskParent
    Dim plotSound As New Sound_Display
    Public Sub New()
        desc = "Display the generated sound waves"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If task.testAllRunning Then Exit Sub ' there have been some failures in player.Init below when running during a test all.  Skip so testing can proceed.
        plotSound.soundSource.Run(src)
        plotSound.Run(src)
        Dim r1 = New cv.Rect(0, 0, src.Width, src.Height)
        Dim r2 = New cv.Rect(src.Width, 0, src.Width, src.Height)
        dst2 = plotSound.dst2(r1)
        dst3 = plotSound.dst2(r2)
    End Sub
End Class








Public Class Sound_WaveDisplay : Inherits TaskParent
    Dim plotSound As New Sound_Display
    Public Sub New()
        plotSound.soundSource = New Sound_Basics
        desc = "Display the generated sound waves"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If task.testAllRunning Then Exit Sub ' there have been some failures in player.Init below when running during a test all.  Skip so testing can proceed.
        plotSound.soundSource.Run(src)
        plotSound.Run(src)
        Dim r1 = New cv.Rect(0, 0, src.Width, src.Height)
        Dim r2 = New cv.Rect(src.Width, 0, src.Width, src.Height)
        dst2 = plotSound.dst2(r1)
        dst3 = plotSound.dst2(r2)
    End Sub
End Class
