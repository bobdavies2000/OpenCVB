Imports VBClasses
Public Class OptionsFeatures
    Public grayCheckbox() As RadioButton
    Public colorCheckbox() As RadioButton
    Public colorMethods() As String = {"BackProject_Full", "Bin4Way_Regions",
                                       "Binarize_DepthTiers", "EdgeLine_Basics", "Hist3DColor_Basics",
                                       "KMeans_Basics", "LUT_Basics", "Reduction_Basics",
                                       "PCA_NColor_CPP", "MeanSubtraction_Gray"}
    Private Sub OptionsFeatures_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = taskA.allOptions
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
        EdgeMethods.Items.Add("Deriche")
        EdgeMethods.Items.Add("Laplacian")
        EdgeMethods.Items.Add("Resize and Add")
        EdgeMethods.Items.Add("Scharr")
        EdgeMethods.Items.Add("Sobel")
        EdgeMethods.SelectedItem() = "Canny"
        taskA.edgeMethod = "Canny"

        MatchCorrSlider.Value = 95

        ReDim grayCheckbox(taskA.filterBasics.grayFilter.filterList.Count - 1)
        For i = 0 To taskA.filterBasics.grayFilter.filterList.Count - 1
            Dim cb As New RadioButton
            cb.Text = taskA.filterBasics.grayFilter.filterList(i)
            cb.Location = New Point(20, 20 + i * 20)
            cb.AutoSize = True
            cb.Tag = i
            AddHandler cb.CheckedChanged, AddressOf CheckBox_CheckedChanged
            GrayGroup.Controls.Add(cb)
            grayCheckbox(i) = cb
        Next
        grayCheckbox(0).Checked = True

        ReDim colorCheckbox(taskA.filterBasics.filterList.Count - 1)
        For i = 0 To taskA.filterBasics.filterList.Count - 1
            Dim cb As New RadioButton
            cb.Text = taskA.filterBasics.filterList(i)
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
        Color8USource.SelectedItem = "Reduction_Basics"

        Select Case taskA.workRes.Width
            Case 1920
                MotionPixelSlider.Value = 400
                taskA.colorDiffThreshold = 50
            Case 1280
                ColorDiffSlider.Value = 40
                MotionPixelSlider.Value = 100
            Case 960
                ColorDiffSlider.Value = 30
                MotionPixelSlider.Value = 100
            Case 672
                ColorDiffSlider.Value = 20
                MotionPixelSlider.Value = 100
            Case 640
                ColorDiffSlider.Value = 20
                MotionPixelSlider.Value = 20
            Case 240, 320, 160
                MotionPixelSlider.Value = 5
                ColorDiffSlider.Value = 26
            Case 336, 168
                MotionPixelSlider.Value = 5
                ColorDiffSlider.Value = 5
        End Select
    End Sub



    Private Sub CheckBox_CheckedChanged(sender As Object, e As EventArgs)
        taskA.optionsChanged = True
    End Sub
    Private Sub FeatureMethod_SelectedIndexChanged(sender As Object, e As EventArgs) Handles FeatureMethod.SelectedIndexChanged
        taskA.optionsChanged = True
    End Sub
    Private Sub verticalRadio_CheckedChanged(sender As Object, e As EventArgs)
        taskA.verticalLines = True
    End Sub
    Private Sub HorizRadio_CheckedChanged(sender As Object, e As EventArgs)
        taskA.verticalLines = False
    End Sub
    Private Sub EdgeMethods_SelectedIndexChanged(sender As Object, e As EventArgs) Handles EdgeMethods.SelectedIndexChanged
        taskA.edgeMethod = EdgeMethods.Text
        taskA.optionsChanged = True
    End Sub



    Private Sub FCorrSlider_ValueChanged(sender As Object, e As EventArgs) Handles MatchCorrSlider.ValueChanged
        taskA.fCorrThreshold = MatchCorrSlider.Value / 100
        taskA.optionsChanged = True
        FeatureCorrelationLabel.Text = Format(taskA.fCorrThreshold, fmt2)
    End Sub
    Private Sub FeatureSampleSize_ValueChanged(sender As Object, e As EventArgs) Handles FeatureSampleSize.ValueChanged
        taskA.FeatureSampleSize = FeatureSampleSize.Value
        taskA.optionsChanged = True
        FeatureSampleSizeLabel.Text = CStr(taskA.FeatureSampleSize)
    End Sub
    Private Sub ColorDiffSlider_ValueChanged(sender As Object, e As EventArgs) Handles ColorDiffSlider.ValueChanged
        taskA.colorDiffThreshold = ColorDiffSlider.Value
        taskA.optionsChanged = True
        ColorDiffLabel.Text = CStr(taskA.colorDiffThreshold)
    End Sub
    Private Sub MotionPixelSlider_ValueChanged(sender As Object, e As EventArgs) Handles MotionPixelSlider.ValueChanged
        taskA.motionThreshold = MotionPixelSlider.Value
        taskA.optionsChanged = True
        MotionPixelLabel1.Text = CStr(taskA.motionThreshold)
    End Sub
    Private Sub ColorSource_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Color8USource.SelectedIndexChanged
        taskA.optionsChanged = True
    End Sub
End Class
