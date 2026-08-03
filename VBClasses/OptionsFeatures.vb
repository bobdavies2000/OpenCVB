Public Class OptionsFeatures
    Public grayCheckbox() As RadioButton
    Public colorCheckbox() As RadioButton
    Public colorMethods() As String = {"Bin4Way_Basics", "BinNWay_Basics", "Hist3DColor_Basics",
                                   "KMeans_Basics", "LUT_Basics", "Reduction_Basics", "PCA_NColor_CPP"}
    Private Sub OptionsFeatures_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = vbc.task.allOptions
        Me.Left = 0
        Me.Top = 0

        FeatureMethod.Items.Add("AGAST")
        FeatureMethod.Items.Add("AKAZE")
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

        ReDim grayCheckbox(vbc.task.filterBasics.grayFilter.filterList.Length - 1)
        For i = 0 To vbc.task.filterBasics.grayFilter.filterList.Length - 1
            Dim cb As New RadioButton With {.Text = vbc.task.filterBasics.grayFilter.filterList(i),
                                            .Location = New Point(20, 20 + i * 20), .AutoSize = True, .Tag = i}
            AddHandler cb.CheckedChanged, AddressOf CheckBox_CheckedChanged
            GrayGroup.Controls.Add(cb)
            grayCheckbox(i) = cb
        Next
        grayCheckbox(0).Checked = True

        ReDim colorCheckbox(vbc.task.filterBasics.filterList.Length - 1)
        For i = 0 To vbc.task.filterBasics.filterList.Length - 1
            Dim cb As New RadioButton With {.Text = vbc.task.filterBasics.filterList(i),
                                            .Location = New Point(20, 20 + i * 20), .AutoSize = True, .Tag = i}
            AddHandler cb.CheckedChanged, AddressOf CheckBox_CheckedChanged
            ColorGroup.Controls.Add(cb)
            colorCheckbox(i) = cb
        Next
        colorCheckbox(0).Checked = True

        For i = 0 To colorMethods.Length - 1
            Dim method = colorMethods(i)
            Color8USource.Items.Add(method)
        Next
        Color8USource.SelectedItem = "LUT_Basics"
        ReductionColor.Value = 32
        ReductionDepth.Value = 200

        ColorDiffSlider.Value = 10
        MotionPixelSlider.Maximum = 10
        MotionPixelSlider.Value = 10
        Select Case vbc.task.workRes.Width
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
        vbc.task.optionsChanged = True
    End Sub
    Private Sub FeatureMethod_SelectedIndexChanged(sender As Object, e As EventArgs) Handles FeatureMethod.SelectedIndexChanged
        vbc.task.optionsChanged = True
    End Sub
    Private Sub verticalRadio_CheckedChanged(sender As Object, e As EventArgs)
        vbc.task.verticalLines = True
    End Sub
    Private Sub HorizRadio_CheckedChanged(sender As Object, e As EventArgs)
        vbc.task.verticalLines = False
    End Sub
    Private Sub EdgeMethods_SelectedIndexChanged(sender As Object, e As EventArgs) Handles EdgeMethods.SelectedIndexChanged
        vbc.task.optionsChanged = True
    End Sub




    Private Sub ReductionColor_ValueChanged(sender As Object, e As EventArgs) Handles ReductionColor.ValueChanged
        Lab1.Text = ReductionColor.Value.ToString(fmt0)
        vbc.task.optionsChanged = True
    End Sub
    Private Sub ReductionDepth_ValueChanged(sender As Object, e As EventArgs) Handles ReductionDepth.ValueChanged
        Lab9.Text = ReductionDepth.Value.ToString(fmt0)
        vbc.task.optionsChanged = True
    End Sub
    Private Sub FCorrSlider_ValueChanged(sender As Object, e As EventArgs) Handles MatchCorrSlider.ValueChanged
        vbc.task.fCorrThreshold = MatchCorrSlider.Value / 100
        vbc.task.optionsChanged = True
        FeatureCorrelationLabel.Text = vbc.task.fCorrThreshold.ToString(fmt2)
    End Sub
    Private Sub FrameHistoryCount_ValueChanged(sender As Object, e As EventArgs) Handles FrameHistoryCount.ValueChanged
        vbc.task.optionsChanged = True
        FrameHistoryLabel.Text = CStr(FrameHistoryCount.Value)
    End Sub
    Private Sub FeatureSampleSize_ValueChanged(sender As Object, e As EventArgs) Handles FeatureSizeSlider.ValueChanged
        vbc.task.optionsChanged = True
        FeatureSamplesLabel.Text = CStr(FeatureSizeSlider.Value)
    End Sub
    Private Sub ColorDiffSlider_ValueChanged(sender As Object, e As EventArgs) Handles ColorDiffSlider.ValueChanged
        vbc.task.colorDiffThreshold = ColorDiffSlider.Value
        vbc.task.optionsChanged = True
        ColorDiffLabel.Text = CStr(vbc.task.colorDiffThreshold)
    End Sub
    Private Sub MotionPixelSlider_ValueChanged(sender As Object, e As EventArgs) Handles MotionPixelSlider.ValueChanged
        vbc.task.motionThreshold = MotionPixelSlider.Value
        vbc.task.optionsChanged = True
        MotionPixelLabel1.Text = CStr(vbc.task.motionThreshold)
    End Sub



    Private Sub ColorSource_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Color8USource.SelectedIndexChanged
        vbc.task.optionsChanged = True
    End Sub

    Private Sub LineCombo_SelectedIndexChanged(sender As Object, e As EventArgs) Handles LineCombo.SelectedIndexChanged
        vbc.task.optionsChanged = True
    End Sub
End Class
