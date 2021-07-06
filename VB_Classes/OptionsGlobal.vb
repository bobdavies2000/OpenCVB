Imports System.Windows.Forms
Imports cv = OpenCvSharp
Public Class OptionsGlobal
    Dim check() As RadioButton
    Public scheme As cv.ColormapTypes = 0
    Public schemeName As String
    Dim mapNames() As String = {"Autumn", "Bone", "Cividis", "Cool", "Hot", "Hsv", "Inferno", "Jet", "Magma", "Ocean", "Parula", "Pink",
                                "Plasma", "Rainbow", "Spring", "Summer", "Twilight", "Twilight_Shifted", "Viridis", "Winter"}
    Private Sub MaxRange_Scroll(sender As Object, e As EventArgs) Handles MaxRange.Scroll
        maxCount.Text = CStr(MaxRange.Value)
    End Sub
    Private Sub OptionsGlobal_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = allOptions
        MaxRange.Value = GetSetting("OpenCVB", "MaxRangeDepth", "MaxRangeDepth", 4000)
        HistBinSlider.Value = GetSetting("OpenCVB", "HistogramBins", "HistogramBins", 40)
        ProjectionSlider.Value = GetSetting("OpenCVB", "ProjectionThreshold", "ProjectionThreshold", 2)
        IMUmotionSlider.Value = GetSetting("OpenCVB", "IMUmotionSlider", "IMUmotionSlider", 1)
        IMUlevelSlider.Value = GetSetting("OpenCVB", "IMUlevelSlider", "IMUlevelSlider", 20)
        If task.color.Width = 640 Then
            LineThickness.Value = GetSetting("OpenCVB", "LineThickness640", "LineThickness640", 1)
            dotSizeSlider.Value = GetSetting("OpenCVB", "dotSizeSlider640", "dotSizeSlider640", 2)
            fontSizeSlider.Value = GetSetting("OpenCVB", "fontSizeSlider640", "fontSizeSlider640", 6)
        Else
            LineThickness.Value = GetSetting("OpenCVB", "LineThickness", "LineThickness", 2)
            dotSizeSlider.Value = GetSetting("OpenCVB", "dotSizeSlider", "dotSizeSlider", 5)
            fontSizeSlider.Value = GetSetting("OpenCVB", "fontSizeSlider", "fontSizeSlider", 12)
        End If
        AddWeightedSlider.Value = GetSetting("OpenCVB", "addweight", "addweight", 50)

        maxCount.Text = CStr(MaxRange.Value)
        HistBinsCount.Text = CStr(HistBinSlider.Value)
        ProjectionThreshold.Text = CStr(ProjectionSlider.Value)
        MotionThresholdValue.Text = CStr(IMUmotionSlider.Value)
        LevelThresholdValue.Text = CStr(IMUlevelSlider.Value)
        LineThicknessAmount.Text = CStr(LineThickness.Value)
        DotSizeLabel.Text = CStr(dotSizeSlider.Value)
        FontSizeLabel.Text = CStr(fontSizeSlider.Value)
        AddWeighted.Text = CStr(AddWeightedSlider.Value)

        UseKalman.Checked = GetSetting("OpenCVB", "useKalman", "useKalman", True)
        UseKalmanWhenStable.Checked = GetSetting("OpenCVB", "UseKalmanWhenStable", "UseKalmanWhenStable", False)

        schemeName = GetSetting("OpenCVB", "DefaultPalette", "DefaultPalette", "Jet")
        If check Is Nothing Then
            ReDim check(mapNames.Count - 1)
            For i = 0 To mapNames.Count - 1
                check(i) = New RadioButton
                check(i).AutoSize = True
                AddHandler check(i).CheckedChanged, AddressOf palette_CheckedChanged
                FlowLayoutPanel1.Controls.Add(check(i))
                check(i).Text = mapNames(i)
            Next
        End If
        For i = 0 To mapNames.Count - 1
            If mapNames(i) = schemeName Then check(i).Checked = True
        Next
        checkRadios()
    End Sub
    Private Sub checkRadios()
        For i = 0 To mapNames.Count - 1
            If check(i).Checked Then
                schemeName = mapNames(i)
                scheme = Choose(i + 1, cv.ColormapTypes.Autumn, cv.ColormapTypes.Bone, cv.ColormapTypes.Cividis, cv.ColormapTypes.Cool,
                                       cv.ColormapTypes.Hot, cv.ColormapTypes.Hsv, cv.ColormapTypes.Inferno, cv.ColormapTypes.Jet,
                                       cv.ColormapTypes.Magma, cv.ColormapTypes.Ocean, cv.ColormapTypes.Parula, cv.ColormapTypes.Pink,
                                       cv.ColormapTypes.Plasma, cv.ColormapTypes.Rainbow, cv.ColormapTypes.Spring, cv.ColormapTypes.Summer,
                                       cv.ColormapTypes.Twilight, cv.ColormapTypes.TwilightShifted, cv.ColormapTypes.Viridis,
                                       cv.ColormapTypes.Winter)
                Exit For
            End If
        Next
    End Sub
    Private Sub OptionsGlobal_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        SaveSetting("OpenCVB", "MaxRangeDepth", "MaxRangeDepth", MaxRange.Value)
        SaveSetting("OpenCVB", "HistogramBins", "HistogramBins", HistBinSlider.Value)
        SaveSetting("OpenCVB", "ProjectionThreshold", "ProjectionThreshold", ProjectionSlider.Value)

        SaveSetting("OpenCVB", "IMUmotionSlider", "IMUmotionSlider", IMUmotionSlider.Value)
        SaveSetting("OpenCVB", "IMUlevelSlider", "IMUlevelSlider", IMUlevelSlider.Value)

        SaveSetting("OpenCVB", "useKalman", "useKalman", UseKalman.Checked)
        SaveSetting("OpenCVB", "UseKalmanWhenStable", "UseKalmanWhenStable", UseKalmanWhenStable.Checked)
        SaveSetting("OpenCVB", "DefaultPalette", "DefaultPalette", schemeName)
        If task.color.Width = 640 Then
            SaveSetting("OpenCVB", "LineThickness640", "LineThickness640", LineThickness.Value)
            SaveSetting("OpenCVB", "dotSizeSlider640", "dotSizeSlider640", dotSizeSlider.Value)
            SaveSetting("OpenCVB", "fontSizeSlider640", "fontSizeSlider640", fontSizeSlider.Value)
        Else
            SaveSetting("OpenCVB", "LineThickness", "LineThickness", LineThickness.Value)
            SaveSetting("OpenCVB", "dotSizeSlider", "dotSizeSlider", dotSizeSlider.Value)
            SaveSetting("OpenCVB", "fontSizeSlider", "fontSizeSlider", fontSizeSlider.Value)
        End If
        SaveSetting("OpenCVB", "addweight", "addweight", CInt(AddWeighted.Text))
    End Sub
    Private Sub thresholdSlider_Scroll(sender As Object, e As EventArgs) Handles HistBinSlider.Scroll
        HistBinsCount.Text = CStr(HistBinSlider.Value)
    End Sub
    Private Sub palette_CheckedChanged(sender As Object, e As EventArgs)
        checkRadios()
    End Sub
    Private Sub resetToDefaults_CheckedChanged(sender As Object, e As EventArgs) Handles resetToDefaults.CheckedChanged
        SaveSetting("OpenCVB", "MaxRangeDepth", "MaxRangeDepth", 4000)
        SaveSetting("OpenCVB", "HistogramBins", "HistogramBins", 40)
        SaveSetting("OpenCVB", "ProjectionThreshold", "ProjectionThreshold", 2)

        SaveSetting("OpenCVB", "IMUmotionSlider", "IMUmotionSlider", 1)
        SaveSetting("OpenCVB", "IMUlevelSlider", "IMUlevelSlider", 20)

        SaveSetting("OpenCVB", "useKalman", "useKalman", True)
        SaveSetting("OpenCVB", "UseKalmanWhenStable", "UseKalmanWhenStable", False)
        SaveSetting("OpenCVB", "DefaultPalette", "DefaultPalette", "Jet")
        If task.color.Width = 640 Then
            SaveSetting("OpenCVB", "LineThickness640", "LineThickness640", 1)
            SaveSetting("OpenCVB", "dotSizeSlider640", "dotSizeSlider640", 2)
            SaveSetting("OpenCVB", "fontSizeSlider640", "fontSizeSlider640", 6)
        Else
            SaveSetting("OpenCVB", "LineThickness", "LineThickness", 2)
            SaveSetting("OpenCVB", "dotSizeSlider", "dotSizeSlider", 5)
            SaveSetting("OpenCVB", "fontSizeSlider", "fontSizeSlider", 12)
        End If
        SaveSetting("OpenCVB", "addweight", "addweight", 50)
        OptionsGlobal_Load(sender, e)
        resetToDefaults.Checked = False
    End Sub
    Private Sub TrackBar1_Scroll(sender As Object, e As EventArgs) Handles IMUlevelSlider.Scroll
        LevelThresholdValue.Text = CStr(IMUlevelSlider.Value)
    End Sub
    Private Sub IMUmotionSlider_Scroll(sender As Object, e As EventArgs) Handles IMUmotionSlider.Scroll
        MotionThresholdValue.Text = CStr(IMUmotionSlider.Value)
    End Sub
    Private Sub LineThickness_Scroll(sender As Object, e As EventArgs) Handles LineThickness.Scroll
        LineThicknessAmount.Text = CStr(LineThickness.Value)
    End Sub
    Private Sub dotSizeSlider_Scroll(sender As Object, e As EventArgs) Handles dotSizeSlider.Scroll
        DotSizeLabel.Text = CStr(dotSizeSlider.Value)
    End Sub
    Private Sub fontSizeSlider_Scroll(sender As Object, e As EventArgs) Handles fontSizeSlider.Scroll
        FontSizeLabel.Text = CStr(fontSizeSlider.Value)
    End Sub
    Private Sub AddWeightedSlider_Scroll(sender As Object, e As EventArgs) Handles AddWeightedSlider.Scroll
        AddWeighted.Text = CStr(AddWeightedSlider.Value)
    End Sub
End Class