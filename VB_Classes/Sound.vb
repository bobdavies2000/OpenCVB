Imports cv = OpenCvSharp
Imports System.IO
Imports NAudio.Wave
Imports NAudio.Wave.SampleProviders.SignalGeneratorType
' https://archive.codeplex.com/?p=naudio
' http://ismir2002.ismir.net/proceedings/02-FP04-2.pdf
Public Class Sound_ToPCM
    Inherits VBparent
    Public reader As MediaFoundationReader
    Dim memData As WaveBuffer
    Dim pcmData8() As Short
    Dim pcmData16() As Short
    Dim currentTime As Date
    Dim startTime As Date
    Dim inputFileName As String
    Public pcm32f As New cv.Mat
    Public player As IWavePlayer
    Public stereo As Boolean
    Public bpp16 As Boolean
    Public pcmDuration As Double ' in seconds.
    Private Sub LoadSoundData()
        Dim tmp(reader.Length - 1) As Byte
        Dim count = reader.Read(tmp, 0, tmp.Length)
        stereo = reader.WaveFormat.Channels = 2
        bpp16 = reader.WaveFormat.BitsPerSample = 16
        memData = New WaveBuffer(tmp)
        If stereo Then
            ReDim pcmData16(count / reader.WaveFormat.Channels - 1)
            For i = 0 To count / reader.WaveFormat.Channels - 1
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
        initParent()

        task.openFileDialogRequested = True
        task.openFileInitialDirectory = task.parms.homeDir + "Data\"
        task.openFileDialogName = GetSetting("OpenCVB", "AudioFileName", "AudioFileName", "")
        task.openFileFilter = "m4a (*.m4a)|*.m4a|mp3 (*.mp3)|*.mp3|mp4 (*.mp4)|*.mp4|wav (*.wav)|*.wav|aac (*.aac)|*.aac|All files (*.*)|*.*"
        task.openFileFilterIndex = 1
        task.openFileDialogTitle = "Select an audio file to analyze"
        task.initialStartSetting = True

        task.desc = "Load an audio file, play it, and convert to PCM"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        If inputFileName <> task.openFileDialogName Then
            inputFileName = task.openFileDialogName
            Dim fileinfo = New FileInfo(inputFileName)
            If fileinfo.Exists And task.fileStarted Then
                Close()

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
                    input = New cv.Mat(pcmData16.Length / channels, 1, mattype, pcmData16)
                Else
                    input = New cv.Mat(pcmData8.Length, 1, mattype, pcmData8)
                End If
                input.ConvertTo(pcm32f, cv.MatType.CV_32F)
                startTime = Now
            End If
        End If
        If task.fileStarted Then
            task.openFileSliderPercent = (Now - startTime).TotalSeconds / pcmDuration
        Else
            inputFileName = ""
            player?.Stop()
        End If
        If standalone Or task.intermediateReview = caller Then task.trueText("Requested sound data is in the pcm32f cv.Mat")
    End Sub
    Public Sub Close()
        player?.Stop()
        player?.Dispose()
        reader?.Dispose()
    End Sub
End Class





' https://github.com/naudio/sinegenerator-sample
Public Class Sound_SignalGenerator
    Inherits VBparent
    Dim player As NAudio.Wave.IWavePlayer
    Dim wGen As New NAudio.Wave.SampleProviders.SignalGenerator
    Public pcm32f As New cv.Mat
    Public stereo As Boolean = False ' only mono generated sound
    Public bpp16 As Boolean = False ' only 8 bit generated sound
    Public pcmDuration As Double ' in seconds.
    Dim pcmData() As Single
    Dim generatedSamplesPerSecond As Integer = 44100
    Dim startTime As Date
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller, 5)
            sliders.setupTrackBar(0, "Sine Wave Frequency", 10, 4000, 1000)
            sliders.setupTrackBar(1, "Decibels", -100, 0, -20)
            sliders.setupTrackBar(2, "Sweep Only - End Frequency", 20, 4000, 1000)
            sliders.setupTrackBar(3, "Sweep Only - duration secs", 0, 10, 1)
            sliders.setupTrackBar(4, "Retain Data for x seconds", 1, 100, 2)
        End If

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 7)
            Static frm = findfrm("Sound_SignalGenerator Radio Options")
            For i = 0 To frm.check.length - 1
                frm.check(i).Text = Choose(i + 1, "Pink", "White", "Sweep", "Sin", "Square", "Triangle", "SawTooth")
            Next
            radio.check(0).Checked = True
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 2)
            check.Box(0).Text = "PhaseReverse Left"
            check.Box(1).Text = "PhaseReverse Right"
        End If
        player = New WaveOut
        player.Init(wGen)

        task.desc = "Generate sound with a sine waveform."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static radioIndex As Integer
        If task.openFileSliderPercent = 0 Or sliders.trackbar(0).Value <> wGen.Frequency Or sliders.trackbar(4).Value <> pcmDuration Or
            radio.check(radioIndex).Checked = False Then
            If pcmDuration <> sliders.trackbar(4).Value Then
                pcmDuration = sliders.trackbar(4).Value
                ReDim pcmData(pcmDuration * generatedSamplesPerSecond - 1) ' enough for about 10 seconds of audio.
                startTime = Now
            End If

            Static frm = findfrm("Sound_SignalGenerator Radio Options")
            For i = 0 To frm.check.length - 1
                If frm.check(i).Checked Then
                    wGen.Type = Choose(i + 1, Pink, White, Sweep, Sin, Square, Triangle, SawTooth)
                    radioIndex = i
                    Exit For
                End If
            Next

            wGen.PhaseReverse(0) = check.Box(0).Checked
            wGen.PhaseReverse(1) = check.Box(1).Checked

            wGen.Frequency = sliders.trackbar(0).Value
            wGen.Gain = NAudio.Utils.Decibels.DecibelsToLinear(sliders.trackbar(1).Value)

            If wGen.Type = Sweep Then
                wGen.FrequencyEnd = sliders.trackbar(2).Value
                wGen.SweepLengthSecs = sliders.trackbar(3).Value
            End If

            Dim count = wGen.Read(pcmData, 0, pcmData.Length)
            pcm32f = New cv.Mat(pcmData.Length, 1, cv.MatType.CV_32F, pcmData)
            player.Play()
        End If
        If standalone Or task.intermediateReview = caller Then task.trueText("Requested sound data is in the pcm32f cv.Mat")

        task.openFileSliderPercent = ((Now - startTime).TotalSeconds Mod pcmDuration) / pcmDuration
        If task.openFileSliderPercent >= 0.99 Then task.openFileSliderPercent = 0
    End Sub
    Public Sub Close()
        player?.Stop()
        player?.Dispose()
    End Sub
End Class






Public Class Sound_Display
    Inherits VBparent
    Dim sound As Object
    Public pcm32f As cv.Mat
    Public starttime As Date
    Public Sub New()
        initParent()

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 4)
            radio.check(0).Text = "Max Absolute Value"
            radio.check(1).Text = "Max RMS Value"
            radio.check(2).Text = "Sampled Peaks"
            radio.check(3).Text = "Scaled Average"
            radio.check(0).Checked = True
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Use generated sound (unchecked will use latest audio file)"
            check.Box(0).Checked = True
        End If

        label2 = "Black shows approximately what is currently playing"
        task.desc = "Display a sound buffer in several styles"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static useGenerated As Boolean
        If useGenerated <> check.Box(0).Checked Or sound Is Nothing Then
            useGenerated = check.Box(0).Checked
            If sound IsNot Nothing Then sound.dispose()
            If check.Box(0).Checked Then
                sound = New Sound_SignalGenerator()
                sound.sliders.trackbar(0).value = 30
                sound.radio.check(3).Checked = True
            Else
                sound = New Sound_ToPCM()
            End If
            starttime = Now
        End If

        If check.Box(0).Checked Or task.fileStarted Then
            sound.Run()

            Dim halfHeight As Integer = dst1.Height / 2
            If sound.pcm32f.width = 0 Then Exit Sub ' sound hasn't loaded yet.

            dst1 = New cv.Mat(src.Height, src.Width * 2, cv.MatType.CV_8UC3, cv.Scalar.Beige)
            Dim totalSamples = sound.pcm32f.Rows
            Dim samplesPerLine = If(sound.stereo, totalSamples / 2 / dst1.Width, totalSamples / dst1.Width)
            Dim formatIndex As Integer
            Static frm = findfrm("Sound_Display Radio Options")
            For i = 0 To frm.check.length - 1
                If frm.check(i).Checked Then formatIndex = i
            Next

            Dim absMinVal As Double, absMaxVal As Double
            Dim pcm = sound.pcm32f
            pcm.MinMaxLoc(absMinVal, absMaxVal)
            If Double.IsNaN(absMaxVal) Or Double.IsNaN(absMinVal) Then Exit Sub ' bad input data...
            If Double.IsNegativeInfinity(absMinVal) Or Double.IsInfinity(absMaxVal) Then Exit Sub ' bad input data...
            Dim minVal As Double, maxVal As Double
            Select Case formatIndex
                Case 0
                    For i = 0 To dst1.Width - 1
                        Dim rect = New cv.Rect(0, i * samplesPerLine, 1, samplesPerLine)
                        If rect.Y + rect.Height > pcm.height Then rect.Height = pcm.Height - rect.Y ' rounding possible when changing buffer size...
                        pcm(rect).MinMaxLoc(minVal, maxVal)
                        If minVal > 0 Then minVal = 0
                        If maxVal < 0 Then maxVal = 0

                        dst1.Line(New cv.Point(i, halfHeight), New cv.Point(i, CInt(halfHeight - halfHeight * maxVal / absMaxVal)), cv.Scalar.Red, 1)
                        dst1.Line(New cv.Point(i, halfHeight), New cv.Point(i, CInt(halfHeight + Math.Abs(minVal) * halfHeight / -absMinVal)), cv.Scalar.Gray, 1)
                    Next
                    label1 = CStr(CInt(sound.pcmDuration)) + " seconds displayed with Max Absolute Value"
                Case 1
                    For i = 0 To dst1.Width - 1
                        Dim rect = New cv.Rect(0, i * samplesPerLine, 1, samplesPerLine)
                        If rect.Y + rect.Height > pcm.height Then rect.Height = pcm.Height - rect.Y ' rounding possible when changing buffer size...
                        Dim tmp = pcm(rect).mul(pcm(rect)).toMat()
                        Dim sum = tmp.sum()
                        Dim nextVal = Math.Sqrt(sum.Item(0) / samplesPerLine)

                        dst1.Line(New cv.Point(i, halfHeight), New cv.Point(i, CInt(halfHeight - halfHeight * nextVal / absMaxVal)), cv.Scalar.Red, 1)
                        dst1.Line(New cv.Point(i, halfHeight), New cv.Point(i, CInt(halfHeight + halfHeight * nextVal / -absMinVal)), cv.Scalar.Gray, 1)
                    Next
                    label1 = CStr(CInt(sound.pcmDuration)) + " seconds displayed with Max RMS Value"
                Case 2
                    For i = 0 To dst1.Width - 1
                        Dim rect = New cv.Rect(0, i * samplesPerLine, 1, samplesPerLine)
                        If rect.Y + rect.Height > pcm.height Then rect.Height = pcm.Height - rect.Y ' rounding possible when changing buffer size...
                        pcm(rect).MinMaxLoc(minVal, maxVal)
                        If minVal > 0 Then minVal = 0
                        If maxVal < 0 Then maxVal = 0

                        dst1.Line(New cv.Point(i, halfHeight), New cv.Point(i, CInt(halfHeight - halfHeight * maxVal / absMaxVal)), cv.Scalar.Red, 1)
                        dst1.Line(New cv.Point(i, halfHeight), New cv.Point(i, CInt(halfHeight + Math.Abs(minVal) * halfHeight / -absMinVal)), cv.Scalar.Gray, 1)
                    Next
                Case 3
                    pcm = cv.Cv2.Abs(pcm).toMat
                    For i = 0 To dst1.Width - 1
                        Dim rect = New cv.Rect(0, i * samplesPerLine, 1, samplesPerLine)
                        If rect.Y + rect.Height > pcm.height Then rect.Height = pcm.Height - rect.Y ' rounding possible when changing buffer size...
                        Dim sum = pcm(rect).sum
                        Dim nextVal = sum.Item(0) / samplesPerLine

                        dst1.Line(New cv.Point(i, halfHeight), New cv.Point(i, CInt(halfHeight - halfHeight * nextVal / absMaxVal)), cv.Scalar.Red, 1)
                        dst1.Line(New cv.Point(i, halfHeight), New cv.Point(i, CInt(halfHeight + halfHeight * nextVal / -absMinVal)), cv.Scalar.Gray, 1)
                    Next
                    label1 = CStr(CInt(sound.pcmDuration)) + " seconds displayed with Scaled Average"
            End Select
        End If
        If check.Box(0).Checked = False Then
            task.openFileSliderPercent = If(task.fileStarted, (Now - starttime).TotalSeconds / sound.pcmduration, 0)
            ' when playing back an audio file, restart at the beginning when it is over...
            If task.openFileSliderPercent > 0.99 Or task.fileStarted = False Then
                sound.close()
                sound = Nothing ' this will restart the audio
            End If
        End If
        Dim x = dst1.Width * task.openFileSliderPercent
        dst1.Line(New cv.Point(x, 0), New cv.Point(x, dst1.Height), cv.Scalar.Black, 2)
    End Sub
End Class
