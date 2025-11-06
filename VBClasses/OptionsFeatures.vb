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
        Me.MdiParent = allOptions
        Me.Left = 0
        Me.Top = 0

        FeatureMethod.Items.Add("GoodFeatures Full Image")
        FeatureMethod.Items.Add("GoodFeatures using Grid")
        FeatureMethod.Items.Add("AGAST")
        FeatureMethod.Items.Add("BRISK")
        FeatureMethod.Items.Add("Harris")
        FeatureMethod.Items.Add("FAST")
        FeatureMethod.Items.Add("LineInput")
        FeatureMethod.Items.Add("BrickPoint")
        FeatureMethod.SelectedItem() = "BrickPoint"

        EdgeMethods.Items.Add("Canny")
        EdgeMethods.Items.Add("Scharr")
        EdgeMethods.Items.Add("Sobel")
        EdgeMethods.Items.Add("Resize and Add")
        EdgeMethods.Items.Add("Binarized Reduction")
        EdgeMethods.Items.Add("Binarized Sobel")
        EdgeMethods.Items.Add("Color Gap")
        EdgeMethods.Items.Add("Deriche")
        EdgeMethods.Items.Add("Laplacian")
        EdgeMethods.SelectedItem() = "Canny"
        task.edgeMethod = "Canny"

        MatchCorrSlider.Value = 95

        ReDim grayCheckbox(task.rgbFilter.grayFilter.filterList.Count - 1)
        For i = 0 To task.rgbFilter.grayFilter.filterList.Count - 1
            Dim cb As New RadioButton
            cb.Text = task.rgbFilter.grayFilter.filterList(i)
            cb.Location = New Point(20, 20 + i * 20)
            cb.AutoSize = True
            cb.Tag = i
            AddHandler cb.CheckedChanged, AddressOf CheckBox_CheckedChanged
            GrayGroup.Controls.Add(cb)
            grayCheckbox(i) = cb
        Next
        grayCheckbox(0).Checked = True

        ReDim colorCheckbox(task.rgbFilter.filterList.Count - 1)
        For i = 0 To task.rgbFilter.filterList.Count - 1
            Dim cb As New RadioButton
            cb.Text = task.rgbFilter.filterList(i)
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
            ColorSource.Items.Add(method)
        Next
        ColorSource.SelectedItem = "Reduction_Basics"
    End Sub



    Private Sub CheckBox_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
    End Sub
    Private Sub FeatureMethod_SelectedIndexChanged(sender As Object, e As EventArgs) Handles FeatureMethod.SelectedIndexChanged
        task.featureSource = FeatureMethod.SelectedIndex
        task.optionsChanged = True
    End Sub
    Private Sub verticalRadio_CheckedChanged(sender As Object, e As EventArgs)
        task.verticalLines = True
    End Sub
    Private Sub HorizRadio_CheckedChanged(sender As Object, e As EventArgs)
        task.verticalLines = False
    End Sub
    Private Sub EdgeMethods_SelectedIndexChanged(sender As Object, e As EventArgs) Handles EdgeMethods.SelectedIndexChanged
        task.edgeMethod = EdgeMethods.Text
        task.optionsChanged = True
    End Sub




    Private Sub DistanceSlider_ValueChanged(sender As Object, e As EventArgs) Handles DistanceSlider.ValueChanged
        DistanceLabel.Text = CStr(DistanceSlider.Value)
        task.minDistance = DistanceSlider.Value
        task.optionsChanged = True
    End Sub
    Private Sub FCorrSlider_ValueChanged(sender As Object, e As EventArgs) Handles MatchCorrSlider.ValueChanged
        task.fCorrThreshold = MatchCorrSlider.Value / 100
        task.optionsChanged = True
        FeatureCorrelationLabel.Text = Format(task.fCorrThreshold, fmt2)
    End Sub
    Private Sub FeatureSampleSize_ValueChanged(sender As Object, e As EventArgs) Handles FeatureSampleSize.ValueChanged
        task.FeatureSampleSize = FeatureSampleSize.Value
        task.optionsChanged = True
        FeatureSampleSizeLabel.Text = CStr(task.FeatureSampleSize)
    End Sub
    Private Sub ColorSource_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ColorSource.SelectedIndexChanged
        task.optionsChanged = True
    End Sub
End Class