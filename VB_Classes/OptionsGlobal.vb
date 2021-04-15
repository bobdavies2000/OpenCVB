Imports System.Windows.Forms
Imports cv = OpenCvSharp
Public Class OptionsGlobal
    Dim check() As RadioButton
    Public scheme As cv.ColormapTypes = 0
    Public schemeName As String
    Dim mapNames() As String = {"Autumn", "Bone", "Cividis", "Cool", "Hot", "Hsv", "Inferno", "Jet", "Magma", "Ocean", "Parula", "Pink",
                                   "Plasma", "Rainbow", "Spring", "Summer", "Twilight", "TwilightShifted", "Viridis", "Winter"}
    Private Sub MaxRange_Scroll(sender As Object, e As EventArgs) Handles MaxRange.Scroll
        maxCount.Text = CStr(MaxRange.Value)
    End Sub
    Private Sub MinRange_Scroll(sender As Object, e As EventArgs) Handles MinRange.Scroll
        minCount.Text = CStr(MinRange.Value)
    End Sub
    Private Sub OptionsGlobal_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = aOptions
        MinRange.Value = GetSetting("OpenCVB", "MinRangeDepth", "MinRangeDepth", 200)
        MaxRange.Value = GetSetting("OpenCVB", "MaxRangeDepth", "MaxRangeDepth", 4000)
        maxCount.Text = CStr(MaxRange.Value)
        minCount.Text = CStr(MinRange.Value)

        HistBinSlider.Value = GetSetting("OpenCVB", "HistogramBins", "HistogramBins", 40)
        HistBinsCount.Text = CStr(HistBinSlider.Value)

        ProjectionSlider.Value = 2 '  GetSetting("OpenCVB", "ProjectionThreshold", "ProjectionThreshold", 2)
        ProjectionThreshold.Text = CStr(ProjectionSlider.Value)

        IMUmotionSlider.Value = GetSetting("OpenCVB", "IMUmotionSlider", "IMUmotionSlider", 1)
        IMUmotion.Text = CStr(IMUmotionSlider.Value)

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
        SaveSetting("OpenCVB", "MinRangeDepth", "MinRangeDepth", MinRange.Value)
        SaveSetting("OpenCVB", "MaxRangeDepth", "MaxRangeDepth", MaxRange.Value)
        SaveSetting("OpenCVB", "HistogramBins", "HistogramBins", HistBinSlider.Value)
        SaveSetting("OpenCVB", "ProjectionThreshold", "ProjectionThreshold", ProjectionSlider.Value)
        SaveSetting("OpenCVB", "IMUmotionSlider", "IMUmotionSlider", IMUmotionSlider.Value)

        SaveSetting("OpenCVB", "useKalman", "useKalman", UseKalman.Checked)
        SaveSetting("OpenCVB", "UseKalmanWhenStable", "UseKalmanWhenStable", UseKalmanWhenStable.Checked)
        SaveSetting("OpenCVB", "DefaultPalette", "DefaultPalette", schemeName)
    End Sub
    Private Sub thresholdSlider_Scroll(sender As Object, e As EventArgs) Handles HistBinSlider.Scroll
        HistBinsCount.Text = CStr(HistBinSlider.Value)
    End Sub
    Private Sub IMUmotionSlider_Scroll(sender As Object, e As EventArgs) Handles IMUmotionSlider.Scroll
        IMUmotion.Text = CStr(IMUmotionSlider.Value)
    End Sub
    Private Sub palette_CheckedChanged(sender As Object, e As EventArgs)
        checkRadios()
    End Sub
    Private Sub resetToDefaults_CheckedChanged(sender As Object, e As EventArgs) Handles resetToDefaults.CheckedChanged
        SaveSetting("OpenCVB", "MinRangeDepth", "MinRangeDepth", 200)
        SaveSetting("OpenCVB", "MaxRangeDepth", "MaxRangeDepth", 4000)
        SaveSetting("OpenCVB", "HistogramBins", "HistogramBins", 40)
        SaveSetting("OpenCVB", "ProjectionThreshold", "ProjectionThreshold", 2)
        SaveSetting("OpenCVB", "IMUmotionSlider", "IMUmotionSlider", 1)
        SaveSetting("OpenCVB", "useKalman", "useKalman", True)
        SaveSetting("OpenCVB", "UseKalmanWhenStable", "UseKalmanWhenStable", False)
        SaveSetting("OpenCVB", "DefaultPalette", "DefaultPalette", "Jet")
        OptionsGlobal_Load(sender, e)
        resetToDefaults.Checked = False
    End Sub
End Class