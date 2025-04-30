Public Class OptionsFeatures
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
        FeatureMethod.SelectedItem() = "GoodFeatures Full Image"

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

        ColorDiffSlider.Value = 10
        FCorrSlider.Value = 50
        SelectedFeature.Value = 0

        verticalRadio.Checked = True
    End Sub



    Private Sub FeatureMethod_SelectedIndexChanged(sender As Object, e As EventArgs) Handles FeatureMethod.SelectedIndexChanged
        task.featureSource = FeatureMethod.SelectedIndex
        task.optionsChanged = True
    End Sub
    Private Sub verticalRadio_CheckedChanged(sender As Object, e As EventArgs) Handles verticalRadio.CheckedChanged
        task.verticalLines = True
    End Sub
    Private Sub HorizRadio_CheckedChanged(sender As Object, e As EventArgs) Handles HorizRadio.CheckedChanged
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
    Private Sub FCorrSlider_ValueChanged(sender As Object, e As EventArgs) Handles FCorrSlider.ValueChanged
        task.fCorrThreshold = FCorrSlider.Value / 100
        task.optionsChanged = True
        FeatureCorrelationLabel.Text = Format(task.fCorrThreshold, fmt2)
    End Sub
    Private Sub ColorDiffSlider_ValueChanged(sender As Object, e As EventArgs) Handles ColorDiffSlider.ValueChanged
        task.colorDiffThreshold = ColorDiffSlider.Value
        task.optionsChanged = True
        ColorDiffLabel.Text = CStr(task.colorDiffThreshold)
    End Sub
    Private Sub SelectedFeature_ValueChanged(sender As Object, e As EventArgs) Handles SelectedFeature.ValueChanged
        task.selectedFeature = SelectedFeature.Value
        task.optionsChanged = True
        SelectedLabel.Text = CStr(task.selectedFeature)
    End Sub
    Private Sub FeatureSampleSize_ValueChanged(sender As Object, e As EventArgs) Handles FeatureSampleSize.ValueChanged
        task.FeatureSampleSize = FeatureSampleSize.Value
        task.optionsChanged = True
        FeatureSampleSizeLabel.Text = CStr(task.FeatureSampleSize)
    End Sub
End Class