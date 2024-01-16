Imports System.Runtime.InteropServices
Imports System.Windows.Controls
Imports NAudio.Gui
Imports cv = OpenCvSharp
Public Class OptionsRedCloud
    Public colorInput As String = "Reduction_Basics"
    Public reductionType As String = "Use Simple Reduction"
    Public depthInput As String = "RedCloud_Core"

    Public PCReduction As Integer
    Public channels() As Integer = {0, 1}
    Public channelIndex As Integer
    Public rangesBGR() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 256), New cv.Rangef(0, 256), New cv.Rangef(0, 256)}
    Public rangesHSV() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 180), New cv.Rangef(0, 256), New cv.Rangef(0, 256)}
    Dim rangesCloud() As cv.Rangef
    Public ranges() As cv.Rangef
    Public channelCount As Integer
    Public histBinList() As Integer
    Public bins3D As Integer
    Public imageThresholdPercent As Single = 0.95
    Private Sub OptionsRedCloud_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = allOptions
        Me.Text = "Options mostly for RedCloud_Basics but other related algorithms too."

        ' The following lines control the pointcloud histograms for X and Y, and the camera location.

        ' The specification for each camera spells out the FOV angle
        ' The sliders adjust the depth data histogram to fill the frustrum which is built from the specification FOV
        Select Case task.cameraName
            Case "Azure Kinect 4K"
                task.xRange = 4.4
                task.yRange = 1.5
            Case "Intel(R) RealSense(TM) Depth Camera 435i"
                If task.workingRes.Height = 480 Or task.workingRes.Height = 240 Or task.workingRes.Height = 120 Then
                    task.xRange = 1.38
                    task.yRange = 1.0
                Else
                    task.xRange = 2.5
                    task.yRange = 0.8
                End If
            Case "Intel(R) RealSense(TM) Depth Camera 455"
                If task.workingRes.Height = 480 Or task.workingRes.Height = 240 Or task.workingRes.Height = 120 Then
                    task.xRange = 2.04
                    task.yRange = 2.14
                Else
                    task.xRange = 3.22
                    task.yRange = 1.39
                End If
            Case "Oak-D camera"
                task.xRange = 4.07
                task.yRange = 1.32
            Case "StereoLabs ZED 2/2i"
                task.xRange = 4
                task.yRange = 1.5
            Case "MYNT-EYE-D1000"
                task.xRange = 3.5
                task.yRange = 1.5
        End Select

        XRangeSlider.Value = task.xRange * 100
        YRangeSlider.Value = task.yRange * 100

        task.xRangeDefault = task.xRange
        task.yRangeDefault = task.yRange

        task.sideCameraPoint = New cv.Point(0, CInt(task.workingRes.Height / 2))
        task.topCameraPoint = New cv.Point(CInt(task.workingRes.Width / 2), 0)

        task.channelsTop = {2, 0}
        task.channelsSide = {1, 2}

        Reduction_Basics.Checked = True ' Reduction_basics is the default.
        SimpleReduction.Checked = True
        PCReduction = 3
        XYReduction.Checked = True
        GuidedBP_Depth.Checked = True
        histBinList = {task.histogramBins, task.histogramBins}
        UseDepthAndColor.Checked = True

        redOptions.SimpleReductionSlider.Value = 40
        Select Case task.cameraName
            Case "Azure Kinect 4K"
            Case "Intel(R) RealSense(TM) Depth Camera 435i"
            Case "Intel(R) RealSense(TM) Depth Camera 455"
            Case "Oak-D camera"
                redOptions.SimpleReductionSlider.Value = 80
            Case "StereoLabs ZED 2/2i"
            Case "MYNT-EYE-D1000"
        End Select

        redOptions.BitwiseReductionSlider.Value = 5

        Me.Left = 0
        Me.Top = 0
    End Sub
    Public Sub Sync()
        task.maxZmeters = gOptions.MaxDepth.Value + 0.01 ' why add a cm?  Because histograms are exclusive on ranges.

        task.rangesTop = New cv.Rangef() {New cv.Rangef(0.1, task.maxZmeters), New cv.Rangef(-task.xRange, task.xRange)}
        task.rangesSide = New cv.Rangef() {New cv.Rangef(-task.yRange, task.yRange), New cv.Rangef(0.1, task.maxZmeters)}

        task.sideCameraPoint = New cv.Point(0, CInt(task.workingRes.Height / 2))
        task.topCameraPoint = New cv.Point(CInt(task.workingRes.Width / 2), 0)

        task.xRange = XRangeSlider.Value / 100 + 0.01 ' why add a cm?  Because histograms are exclusive on ranges.
        task.yRange = YRangeSlider.Value / 100 + 0.01 ' why add a cm?  Because histograms are exclusive on ranges.

        task.redThresholdSide = SideViewThreshold.Value
        task.redThresholdTop = TopViewThreshold.Value

        Dim rx = New cv.Vec2f(-task.xRangeDefault, task.xRangeDefault)
        Dim ry = New cv.Vec2f(-task.yRangeDefault, task.yRangeDefault)
        Dim rz = New cv.Vec2f(0, task.maxZmeters)
        channelCount = 1

        channelIndex = 0
        Select Case redOptions.PCReduction
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

        Dim testVal = colorInput = "Reduction_Basics"
        ReductionSliders.Enabled = testVal
        ReductionTypeGroup.Enabled = testVal
        If testVal Then
            SimpleReductionSlider.Enabled = reductionType = "Use Simple Reduction"
            BitwiseReductionSlider.Enabled = reductionType = "Use Bitwise Reduction"
        End If
    End Sub




    Private Sub XRangeSlider_ValueChanged(sender As Object, e As EventArgs) Handles XRangeSlider.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        XLabel.Text = CStr(XRangeSlider.Value)
    End Sub
    Private Sub YRangeSlider_ValueChanged(sender As Object, e As EventArgs) Handles YRangeSlider.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        YLabel.Text = CStr(YRangeSlider.Value)
    End Sub
    Private Sub SideViewThreshold_ValueChanged(sender As Object, e As EventArgs) Handles SideViewThreshold.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        SideLabel.Text = CStr(SideViewThreshold.Value)
    End Sub
    Private Sub TopViewThreshold_ValueChanged(sender As Object, e As EventArgs) Handles TopViewThreshold.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        TopLabel.Text = CStr(TopViewThreshold.Value)
    End Sub




    Private Sub RadioButton1_CheckedChanged(sender As Object, e As EventArgs) Handles BackProject_Full.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        colorInput = BackProject_Full.Text
    End Sub
    Private Sub RadioButton4_CheckedChanged(sender As Object, e As EventArgs) Handles KMeans_Basics.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        colorInput = KMeans_Basics.Text
    End Sub
    Private Sub RadioButton3_CheckedChanged(sender As Object, e As EventArgs) Handles LUT_Basics.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        colorInput = LUT_Basics.Text
    End Sub
    Private Sub RadioButton2_CheckedChanged(sender As Object, e As EventArgs) Handles Reduction_Basics.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        colorInput = Reduction_Basics.Text
    End Sub
    Private Sub BackProject3D_CheckedChanged(sender As Object, e As EventArgs) Handles BackProject3D.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        colorInput = BackProject3D.Text
    End Sub
    Private Sub FeatureLessRadio_CheckedChanged(sender As Object, e As EventArgs) Handles FeatureLessRadio.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        colorInput = FeatureLessRadio.Text
    End Sub
    Private Sub BinarizeRadio_CheckedChanged(sender As Object, e As EventArgs) Handles BinarizeRadio.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        colorInput = BinarizeRadio.Text
    End Sub



    Private Sub SimpleReduction_CheckedChanged(sender As Object, e As EventArgs) Handles SimpleReduction.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        reductionType = SimpleReduction.Text
    End Sub
    Private Sub BitwiseReduction_CheckedChanged(sender As Object, e As EventArgs) Handles BitwiseReduction.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        reductionType = BitwiseReduction.Text
    End Sub
    Private Sub NoReduction_CheckedChanged(sender As Object, e As EventArgs) Handles NoReduction.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        reductionType = NoReduction.Text
    End Sub




    Private Sub ColorReductionSlider_ValueChanged(sender As Object, e As EventArgs) Handles SimpleReductionSlider.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        ColorLabel.Text = CStr(SimpleReductionSlider.Value)
    End Sub
    Private Sub BitwiseReductionSlider_ValueChanged(sender As Object, e As EventArgs) Handles BitwiseReductionSlider.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        bitwiseLabel.Text = CStr(BitwiseReductionSlider.Value)
    End Sub



    Private Sub XReduction_CheckedChanged(sender As Object, e As EventArgs) Handles XReduction.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        PCReduction = XReduction.Tag
        gOptions.HistBinSlider.Value = 16
    End Sub
    Private Sub YReduction_CheckedChanged(sender As Object, e As EventArgs) Handles YReduction.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        PCReduction = YReduction.Tag
        gOptions.HistBinSlider.Value = 16
    End Sub
    Private Sub ZReduction_CheckedChanged(sender As Object, e As EventArgs) Handles ZReduction.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        PCReduction = ZReduction.Tag
        gOptions.HistBinSlider.Value = 16
    End Sub
    Private Sub ReductionXY_CheckedChanged(sender As Object, e As EventArgs) Handles XYReduction.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        PCReduction = XYReduction.Tag
        gOptions.HistBinSlider.Value = 16
    End Sub
    Private Sub XZReduction_CheckedChanged(sender As Object, e As EventArgs) Handles XZReduction.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        PCReduction = XZReduction.Tag
        gOptions.HistBinSlider.Value = 16
    End Sub
    Private Sub YZReduction_CheckedChanged(sender As Object, e As EventArgs) Handles YZReduction.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        PCReduction = YZReduction.Tag
        gOptions.HistBinSlider.Value = 16
    End Sub
    Public Sub XYZReduction_CheckedChanged(sender As Object, e As EventArgs) Handles XYZReduction.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        PCReduction = XYZReduction.Tag
        gOptions.HistBinSlider.Value = 6
    End Sub



    Private Sub GuidedBP_Depth_CheckedChanged(sender As Object, e As EventArgs) Handles GuidedBP_Depth.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        depthInput = GuidedBP_Depth.Text
    End Sub
    Private Sub RedCloud_Core_CheckedChanged(sender As Object, e As EventArgs) Handles RedCloud_Core.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        depthInput = RedCloud_Core.Text
    End Sub
    Private Sub desiredCellSlider_ValueChanged(sender As Object, e As EventArgs) Handles DesiredCellSlider.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        LabelDesiredCell.Text = CStr(DesiredCellSlider.Value)
    End Sub
    Private Sub HistBinSlider_ValueChanged(sender As Object, e As EventArgs) Handles HistBinSlider.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        LabelHistogramBins.Text = CStr(HistBinSlider.Value)
        bins3D = HistBinSlider.Value * HistBinSlider.Value * HistBinSlider.Value
    End Sub
End Class
