Public Class OptionsFeatures
    Public grayCheckbox() As RadioButton
    Public colorCheckbox() As RadioButton
    Public colorMethods() As String = {"BackProject_Full", "Bin4Way_Regions", "Hist3DColor_Basics",
                                       "KMeans_Basics", "LUT_Basics", "Reduction_Basics", "PCA_NColor_CPP",
                                       "MeanSubtraction_Gray"}
    Private Sub OptionsFeatures_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = task.allOptions
        Me.Left = 0
        Me.Top = 0

        FeatureMethod.Items.Add("AGAST")
        FeatureMethod.Items.Add("BrickPoint")
        FeatureMethod.Items.Add("BRISK")
        FeatureMethod.Items.Add("FAST")
        FeatureMethod.Items.Add("GoodFeatures")
        FeatureMethod.Items.Add("Harris")
        FeatureMethod.Items.Add("LineInput")
        FeatureMethod.SelectedItem() = "GoodFeatures"

        EdgeMethods.Items.Add("Binarized Reduction")
        EdgeMethods.Items.Add("Binarized Sobel")
        EdgeMethods.Items.Add("Canny")
        EdgeMethods.Items.Add("Color Gap")
        EdgeMethods.Items.Add("Laplacian")
        EdgeMethods.Items.Add("Sobel")
        EdgeMethods.SelectedItem() = "Sobel"

        MatchCorrSlider.Value = 95

        ReDim grayCheckbox(task.filterBasics.grayFilter.filterList.Count - 1)
        For i = 0 To task.filterBasics.grayFilter.filterList.Count - 1
            Dim cb As New RadioButton
            cb.Text = task.filterBasics.grayFilter.filterList(i)
            cb.Location = New Point(20, 20 + i * 20)
            cb.AutoSize = True
            cb.Tag = i
            AddHandler cb.CheckedChanged, AddressOf CheckBox_CheckedChanged
            GrayGroup.Controls.Add(cb)
            grayCheckbox(i) = cb
        Next
        grayCheckbox(0).Checked = True

        ReDim colorCheckbox(task.filterBasics.filterList.Count - 1)
        For i = 0 To task.filterBasics.filterList.Count - 1
            Dim cb As New RadioButton
            cb.Text = task.filterBasics.filterList(i)
            cb.Location = New Point(20, 20 + i * 20)
            cb.AutoSize = True
            cb.Tag = i
            AddHandler cb.CheckedChanged, AddressOf CheckBox_CheckedChanged
            ColorGroup.Controls.Add(cb)
            colorCheckbox(i) = cb
        Next
        colorCheckbox(0).Checked = True

        For i = 0 To colorMethods.Count - 1
            Dim method = colorMethods(i)
            Color8USource.Items.Add(method)
        Next
        Color8USource.SelectedItem = "KMeans_Basics"
        ReductionColor.Value = 32
        ReductionDepth.Value = 200

        ColorDiffSlider.Value = 10
        MotionPixelSlider.Maximum = 10
        MotionPixelSlider.Value = 10
        Select Case task.workRes.Width
            Case 1920
                ColorDiffSlider.Value = 25
                MotionPixelSlider.Maximum = 100
                MotionPixelSlider.Value = 50
            Case 1280
                ColorDiffSlider.Value = 20
            Case 960
                ColorDiffSlider.Value = 18
            Case 672
                ColorDiffSlider.Value = 15
            Case 640, 480 '
                ColorDiffSlider.Value = 12
            Case 240, 320, 160
                ColorDiffSlider.Value = 10
            Case 336, 168 '
                ColorDiffSlider.Value = 10
        End Select
        FrameHistoryCount.Value = 3
        LineCombo.Items.Add("Fast Line Detection")
        LineCombo.Items.Add("Line Segment Detection")
        LineCombo.SelectedItem = "Line Segment Detection"
    End Sub



    Private Sub CheckBox_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
    End Sub
    Private Sub FeatureMethod_SelectedIndexChanged(sender As Object, e As EventArgs) Handles FeatureMethod.SelectedIndexChanged
        task.optionsChanged = True
    End Sub
    Private Sub verticalRadio_CheckedChanged(sender As Object, e As EventArgs)
        task.verticalLines = True
    End Sub
    Private Sub HorizRadio_CheckedChanged(sender As Object, e As EventArgs)
        task.verticalLines = False
    End Sub
    Private Sub EdgeMethods_SelectedIndexChanged(sender As Object, e As EventArgs) Handles EdgeMethods.SelectedIndexChanged
        task.optionsChanged = True
    End Sub




    Private Sub ReductionColor_ValueChanged(sender As Object, e As EventArgs) Handles ReductionColor.ValueChanged
        Lab1.Text = ReductionColor.Value.ToString(fmt0)
        task.optionsChanged = True
    End Sub
    Private Sub ReductionDepth_ValueChanged(sender As Object, e As EventArgs) Handles ReductionDepth.ValueChanged
        Lab9.Text = ReductionDepth.Value.ToString(fmt0)
        task.optionsChanged = True
    End Sub
    Private Sub FCorrSlider_ValueChanged(sender As Object, e As EventArgs) Handles MatchCorrSlider.ValueChanged
        task.fCorrThreshold = MatchCorrSlider.Value / 100
        task.optionsChanged = True
        FeatureCorrelationLabel.Text = task.fCorrThreshold.ToString(fmt2)
    End Sub
    Private Sub FeatureSampleSize_ValueChanged(sender As Object, e As EventArgs) Handles FrameHistoryCount.ValueChanged
        task.FeatureSampleSize = FrameHistoryCount.Value
        task.optionsChanged = True
        FeatureSampleSizeLabel.Text = CStr(task.FeatureSampleSize)
    End Sub
    Private Sub ColorDiffSlider_ValueChanged(sender As Object, e As EventArgs) Handles ColorDiffSlider.ValueChanged
        task.colorDiffThreshold = ColorDiffSlider.Value
        task.optionsChanged = True
        ColorDiffLabel.Text = CStr(task.colorDiffThreshold)
    End Sub
    Private Sub MotionPixelSlider_ValueChanged(sender As Object, e As EventArgs) Handles MotionPixelSlider.ValueChanged
        task.motionThreshold = MotionPixelSlider.Value
        task.optionsChanged = True
        MotionPixelLabel1.Text = CStr(task.motionThreshold)
    End Sub



    Private Sub ColorSource_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Color8USource.SelectedIndexChanged
        task.optionsChanged = True
    End Sub

    Private Sub LineCombo_SelectedIndexChanged(sender As Object, e As EventArgs) Handles LineCombo.SelectedIndexChanged
        task.optionsChanged = True
    End Sub
End Class
