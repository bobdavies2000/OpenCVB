Imports cv = OpenCvSharp
Public Class OptionsRedColor
    Public colorInputName As String
    Public colorInputIndex As Integer
    Public reductionType As String = "Use Simple Reduction"
    Public depthInputIndex As Integer = 0 ' guidedBP is the default.

    Public SimpleReduction As Integer
    Public SimpleReductionChecked As Boolean

    Public bitReduction As Integer
    Public bitReductionChecked As Boolean

    Public PointCloudReduction As Integer
    Public PointCloudReductionLabel As String
    Public channels() As Integer = {0, 1}
    Public channelIndex As Integer
    Public rangesBGR() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 256), New cv.Rangef(0, 256), New cv.Rangef(0, 256)}
    Public rangesHSV() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 180), New cv.Rangef(0, 256), New cv.Rangef(0, 256)}
    Public rangesCloud() As cv.Rangef
    Public ranges() As cv.Rangef
    Public channelCount As Integer
    Public histBinList() As Integer
    Public histBins3D As Integer
    Public trackingLabel As String
    Public trackingIndex As Integer
    Public colorMethods() As String = {"BackProject_Full", "Bin4Way_Regions",
                                       "Binarize_DepthTiers", "EdgeLine_Basics", "Hist3DColor_Basics",
                                       "KMeans_Basics", "LUT_Basics", "Reduction_Basics",
                                       "PCA_NColor_CPP", "MeanSubtraction_Gray"}
    Private Sub OptionsRedColor_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = allOptions
        Me.Text = "Options RedColor, RedCloud, RedMask and related algorithms"

        ' The following lines control the pointcloud histograms for X and Y, and the camera location.

        ' The specification for each camera spells out the FOV angle
        ' The sliders adjust the depth data histogram to fill the frustrum which is built from the specification FOV
#If AZURE_SUPPORT Then
        If task.cameraName.StartsWith("Azure Kinect 4K") Then
            task.xRange = 4.4
            task.yRange = 1.5
        Else
#End If
        If task.cameraName.StartsWith("StereoLabs ZED 2/2i") Then
            task.xRange = 4
            task.yRange = 1.5
        Else
            Select Case task.cameraName
                Case "Intel(R) RealSense(TM) Depth Camera 435i"
                    If task.dst2.Height = 480 Or task.dst2.Height = 240 Or task.dst2.Height = 120 Then
                        task.xRange = 1.38
                        task.yRange = 1.0
                    Else
                        task.xRange = 2.5
                        task.yRange = 0.8
                    End If
                Case "Intel(R) RealSense(TM) Depth Camera 455", ""
                    If task.dst2.Height = 480 Or task.dst2.Height = 240 Or task.dst2.Height = 120 Then
                        task.xRange = 2.04
                        task.yRange = 2.14
                    Else
                        task.xRange = 3.22
                        task.yRange = 1.39
                    End If
                Case "Oak-D camera"
                    task.xRange = 4.07
                    task.yRange = 1.32
                Case "MYNT-EYE-D1000"
                    task.xRange = 3.5
                    task.yRange = 1.5
                Case "Orbbec Gemini 335L", "Orbbec Gemini 336L", "Orbbec Gemini 335"
                    task.xRange = 3.5
                    task.yRange = 1.5
            End Select
        End If

        XRangeBar.Value = task.xRange * 100
        YRangeSlider.Value = task.yRange * 100

        task.xRangeDefault = task.xRange
        task.yRangeDefault = task.yRange

        task.sideCameraPoint = New cv.Point(0, CInt(task.dst2.Height / 2))
        task.topCameraPoint = New cv.Point(CInt(task.dst2.Width / 2), 0)

        task.channelsTop = {2, 0}
        task.channelsSide = {1, 2}

        UseSimpleReduction.Checked = True
        PointCloudReduction = 3
        XYReduction.Checked = True
        histBinList = {task.histogramBins, task.histogramBins}

        For i = 0 To colorMethods.Count - 1
            Dim method = colorMethods(i)
            ColorSource.Items.Add(method)
        Next
        ColorSource.SelectedItem = "EdgeLine_Basics"

        Select Case task.cameraName
            Case "Oak-D camera"
                task.redOptions.setBitReductionBar(80)
            Case Else
                task.redOptions.setBitReductionBar(40)
        End Select

        task.redOptions.setBitReductionBar(5)
        TrackingColor.Checked = True

        Me.Left = 0
        Me.Top = 30
    End Sub
    Public Sub Sync()
        task.rangesTop = New cv.Rangef() {New cv.Rangef(0.1, task.MaxZmeters + 0.1),
                                          New cv.Rangef(-task.xRange, task.xRange)}
        task.rangesSide = New cv.Rangef() {New cv.Rangef(-task.yRange, task.yRange),
                                           New cv.Rangef(0.1, task.MaxZmeters + 0.1)}

        task.sideCameraPoint = New cv.Point(0, CInt(task.dst2.Height / 2))
        task.topCameraPoint = New cv.Point(CInt(task.dst2.Width / 2), 0)

        task.projectionThreshold = ProjectionThresholdBar.Value

        Dim rx = New cv.Vec2f(-task.xRangeDefault, task.xRangeDefault)
        Dim ry = New cv.Vec2f(-task.yRangeDefault, task.yRangeDefault)
        Dim rz = New cv.Vec2f(0, task.MaxZmeters)
        rangesCloud = New cv.Rangef() {New cv.Rangef(rx.Item0, rx.Item1), New cv.Rangef(ry.Item0, ry.Item1), New cv.Rangef(rz.Item0, rz.Item1)}

        channelCount = 1
        channelIndex = 0
        Select Case task.redOptions.PointCloudReduction
            Case 0 ' "X Reduction"
                ranges = New cv.Rangef() {New cv.Rangef(rx.Item0, rx.Item1)}
                channels = {0}
                histBinList = {task.histogramBins}
            Case 1 ' "Y Reduction"
                ranges = New cv.Rangef() {New cv.Rangef(ry.Item0, ry.Item1)}
                channels = {1}
                histBinList = {task.histogramBins}
            Case 2 ' "Z Reduction"
                ranges = New cv.Rangef() {New cv.Rangef(rz.Item0, rz.Item1)}
                channels = {2}
                histBinList = {task.histogramBins}
            Case 3 ' "XY Reduction"
                ranges = New cv.Rangef() {New cv.Rangef(rx.Item0, rx.Item1), New cv.Rangef(ry.Item0, ry.Item1)}
                channelCount = 2
                channels = {0, 1}
                histBinList = {task.histogramBins, task.histogramBins}
            Case 4 ' "XZ Reduction"
                ranges = New cv.Rangef() {New cv.Rangef(rx.Item0, rx.Item1), New cv.Rangef(rz.Item0, rz.Item1)}
                channelCount = 2
                channels = {0, 2}
                channelIndex = 1
                histBinList = {task.histogramBins, task.histogramBins}
            Case 5 ' "YZ Reduction"
                ranges = New cv.Rangef() {New cv.Rangef(ry.Item0, ry.Item1), New cv.Rangef(rz.Item0, rz.Item1)}
                channelCount = 2
                channels = {1, 2}
                channelIndex = 1
                histBinList = {task.histogramBins, task.histogramBins}
            Case 6 ' "XYZ Reduction"
                ranges = New cv.Rangef() {New cv.Rangef(rx.Item0, rx.Item1), New cv.Rangef(ry.Item0, ry.Item1), New cv.Rangef(rz.Item0, rz.Item1)}
                channelCount = 3
                channels = {0, 1, 2}
                channelIndex = 2
                histBinList = {task.histogramBins, task.histogramBins, task.histogramBins}
        End Select

        SimpleReductionBar.Enabled = Not BitwiseReduction.Checked
        BitwiseReductionBar.Enabled = BitwiseReduction.Checked
    End Sub




    Private Sub XRangeSlider_ValueChanged(sender As Object, e As EventArgs) Handles XRangeBar.ValueChanged
        task.optionsChanged = True
        task.xRange = XRangeBar.Value / 100
        XLabel.Text = CStr(XRangeBar.Value)
    End Sub
    Private Sub YRangeSlider_ValueChanged(sender As Object, e As EventArgs) Handles YRangeSlider.ValueChanged
        task.optionsChanged = True
        task.yRange = YRangeSlider.Value / 100
        YLabel.Text = CStr(YRangeSlider.Value)
    End Sub
    Private Sub ProjectionThreshold_ValueChanged(sender As Object, e As EventArgs) Handles ProjectionThresholdBar.ValueChanged
        task.optionsChanged = True
        SideLabel.Text = CStr(ProjectionThresholdBar.Value)
    End Sub




    Private Sub SimpleReduction_CheckedChanged(sender As Object, e As EventArgs) Handles UseSimpleReduction.CheckedChanged
        task.optionsChanged = True
        SimpleReductionChecked = UseSimpleReduction.Checked
        reductionType = UseSimpleReduction.Text
    End Sub
    Private Sub BitwiseReduction_CheckedChanged(sender As Object, e As EventArgs) Handles BitwiseReduction.CheckedChanged
        task.optionsChanged = True
        bitReductionChecked = BitwiseReduction.Checked
        reductionType = BitwiseReduction.Text
    End Sub
    Private Sub NoReduction_CheckedChanged(sender As Object, e As EventArgs) Handles NoReduction.CheckedChanged
        task.optionsChanged = True
        reductionType = NoReduction.Text
    End Sub




    Private Sub ColorReductionSlider_ValueChanged(sender As Object, e As EventArgs) Handles SimpleReductionBar.ValueChanged
        task.optionsChanged = True
        SimpleReduction = SimpleReductionBar.Value
        ColorLabel.Text = CStr(SimpleReductionBar.Value)
    End Sub
    Private Sub BitwiseReductionSlider_ValueChanged(sender As Object, e As EventArgs) Handles BitwiseReductionBar.ValueChanged
        task.optionsChanged = True
        bitReduction = BitwiseReductionBar.Value
        bitwiseLabel.Text = CStr(BitwiseReductionBar.Value)
    End Sub



    Private Sub XReduction_CheckedChanged(sender As Object, e As EventArgs) Handles XReduction.CheckedChanged
        task.optionsChanged = True
        PointCloudReduction = XReduction.Tag
        PointCloudReductionLabel = "X Reduction"
    End Sub
    Private Sub YReduction_CheckedChanged(sender As Object, e As EventArgs) Handles YReduction.CheckedChanged
        task.optionsChanged = True
        PointCloudReduction = YReduction.Tag
        PointCloudReductionLabel = "Y Reduction"
    End Sub
    Private Sub ZReduction_CheckedChanged(sender As Object, e As EventArgs) Handles ZReduction.CheckedChanged
        task.optionsChanged = True
        PointCloudReduction = ZReduction.Tag
        PointCloudReductionLabel = "Z Reduction"
    End Sub
    Private Sub ReductionXY_CheckedChanged(sender As Object, e As EventArgs) Handles XYReduction.CheckedChanged
        task.optionsChanged = True
        PointCloudReduction = XYReduction.Tag
        PointCloudReductionLabel = "XY Reduction"
    End Sub
    Private Sub XZReduction_CheckedChanged(sender As Object, e As EventArgs) Handles XZReduction.CheckedChanged
        task.optionsChanged = True
        PointCloudReduction = XZReduction.Tag
        PointCloudReductionLabel = "XZ Reduction"
    End Sub
    Private Sub YZReduction_CheckedChanged(sender As Object, e As EventArgs) Handles YZReduction.CheckedChanged
        task.optionsChanged = True
        PointCloudReduction = YZReduction.Tag
        PointCloudReductionLabel = "YZ Reduction"
    End Sub
    Public Sub XYZReduction_CheckedChanged(sender As Object, e As EventArgs) Handles XYZReduction.CheckedChanged
        task.optionsChanged = True
        PointCloudReduction = XYZReduction.Tag
        PointCloudReductionLabel = "XYZ Reduction"
    End Sub



    Private Sub GuidedBP_Depth_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
        depthInputIndex = 0
    End Sub
    Private Sub RedCloud_Basics_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
        depthInputIndex = 1
    End Sub
    Private Sub HistBinSlider_ValueChanged(sender As Object, e As EventArgs) Handles HistBinBar3D.ValueChanged
        task.optionsChanged = True
        LabelHistogramBins.Text = CStr(HistBinBar3D.Value)
        histBins3D = HistBinBar3D.Value * HistBinBar3D.Value * HistBinBar3D.Value
    End Sub

    Private Sub ColorSource_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ColorSource.SelectedIndexChanged
        task.optionsChanged = True
        colorInputName = ColorSource.Text
        colorInputIndex = ColorSource.SelectedIndex
        If colorInputName = "Reduction_Basics" Then
            SimpleReductionBar.Enabled = reductionType = "Use Simple Reduction"
            BitwiseReductionBar.Enabled = reductionType = "Use Bitwise Reduction"
        End If
    End Sub


    Public Sub checkSimpleReduction(newVal As Boolean)
        UseSimpleReduction.Checked = newVal
    End Sub
    Public Sub setBitReductionBar(newVal As Integer)
        If newVal > BitwiseReductionBar.Maximum Then BitwiseReductionBar.Maximum = newVal
        BitwiseReductionBar.Value = newVal
    End Sub
    Public Function getBitReductionBar() As Integer
        Return BitwiseReductionBar.Value
    End Function
    Public Sub checkBitReduction(newVal As Boolean)
        BitwiseReduction.Checked = newVal
    End Sub
    Public Sub setProjection(val As Integer)
        ProjectionThresholdBar.Value = val
    End Sub
    Public Sub setXRangeSlider(val As Integer)
        XRangeBar.Value = val
    End Sub
    Public Function getXRangeSlider() As Integer
        Return XRangeBar.Value
    End Function







    Private Sub TrackingMeanColor_CheckedChanged(sender As Object, e As EventArgs) Handles TrackingMeanColor.CheckedChanged
        trackingLabel = TrackingMeanColor.Text
        trackingIndex = TaskParent.trackColor.meanColor
    End Sub
    Private Sub TrackingColor_CheckedChanged(sender As Object, e As EventArgs) Handles TrackingColor.CheckedChanged
        trackingLabel = TrackingColor.Text
        trackingIndex = TaskParent.trackColor.tracking
    End Sub
    Private Sub TrackingDepthColor_CheckedChanged(sender As Object, e As EventArgs) Handles TrackingDepthColor.CheckedChanged
        trackingLabel = TrackingDepthColor.Text
        trackingIndex = TaskParent.trackColor.colorWithDepth
    End Sub
End Class
