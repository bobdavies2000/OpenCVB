Imports System.Windows.Forms
Imports System.Drawing
Imports System.ComponentModel
Public Class OptionsFeatures
    Public grayCheckbox() As RadioButton
    Public colorCheckbox() As RadioButton
    Public colorMethods() As String = {"BackProject_Full", "Bin4Way_Regions",
                                       "Binarize_DepthTiers", "EdgeLine_Basics", "Hist3DColor_Basics",
                                       "KMeans_Basics", "LUT_Basics", "Reduction_Basics",
                                       "PCA_NColor_CPP", "MeanSubtraction_Gray"}
    Private Sub OptionsFeatures_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = myTask.allOptions
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
        myTask.edgeMethod = "Canny"

        MatchCorrSlider.Value = 95

        'ReDim grayCheckbox(myTask.rgbFilter.grayFilter.filterList.Count - 1)
        'For i = 0 To myTask.rgbFilter.grayFilter.filterList.Count - 1
        '    Dim cb As New RadioButton
        '    cb.Text = myTask.rgbFilter.grayFilter.filterList(i)
        '    cb.Location = New Point(20, 20 + i * 20)
        '    cb.AutoSize = True
        '    cb.Tag = i
        '    AddHandler cb.CheckedChanged, AddressOf CheckBox_CheckedChanged
        '    GrayGroup.Controls.Add(cb)
        '    grayCheckbox(i) = cb
        'Next
        'grayCheckbox(0).Checked = True

        'ReDim colorCheckbox(myTask.rgbFilter.filterList.Count - 1)
        'For i = 0 To myTask.rgbFilter.filterList.Count - 1
        '    Dim cb As New RadioButton
        '    cb.Text = myTask.rgbFilter.filterList(i)
        '    cb.Location = New Point(20, 20 + i * 20)
        '    cb.AutoSize = True
        '    cb.Tag = i
        '    AddHandler cb.CheckedChanged, AddressOf CheckBox_CheckedChanged
        '    ColorGroup.Controls.Add(cb)
        '    colorCheckbox(i) = cb
        'Next
        'colorCheckbox(0).Checked = True

        For i = 0 To colorMethods.Count - 1
            Dim method = colorMethods(i)
            Color8USource.Items.Add(method)
        Next
        Color8USource.SelectedItem = "Reduction_Basics"
    End Sub



    Private Sub CheckBox_CheckedChanged(sender As Object, e As EventArgs)
        myTask.optionsChanged = True
    End Sub
    Private Sub FeatureMethod_SelectedIndexChanged(sender As Object, e As EventArgs) Handles FeatureMethod.SelectedIndexChanged
        myTask.optionsChanged = True
    End Sub
    Private Sub EdgeMethods_SelectedIndexChanged(sender As Object, e As EventArgs) Handles EdgeMethods.SelectedIndexChanged
        myTask.edgeMethod = EdgeMethods.Text
        myTask.optionsChanged = True
    End Sub



    Private Sub FCorrSlider_ValueChanged(sender As Object, e As EventArgs) Handles MatchCorrSlider.ValueChanged
        myTask.fCorrThreshold = MatchCorrSlider.Value / 100
        myTask.optionsChanged = True
        FeatureCorrelationLabel.Text = Format(myTask.fCorrThreshold, fmt2)
    End Sub
    Private Sub FeatureSampleSize_ValueChanged(sender As Object, e As EventArgs) Handles FeatureSampleSize.ValueChanged
        myTask.FeatureSampleSize = FeatureSampleSize.Value
        myTask.optionsChanged = True
        FeatureSampleSizeLabel.Text = CStr(myTask.FeatureSampleSize)
    End Sub
    Private Sub ColorSource_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Color8USource.SelectedIndexChanged
        myTask.optionsChanged = True
    End Sub
End Class