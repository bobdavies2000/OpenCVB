Imports System.Windows.Controls
Imports cv = OpenCvSharp
Public Class OptionsRedCloud
    Public radioText As String = "Reduction_Basics"
    Public channels() As Integer = {0, 1}
    Public reduction As Integer ' 0 = simple, 1 = bitwise, 2 = none
    Public Const simpleReduce As Integer = 0
    Public Const bitwiseReduce As Integer = 1
    Public Const noReduce As Integer = 2
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

        Channels01.Checked = True
        RadioButton2.Checked = True ' Reduction_basics is the default.
        SimpleReduction.Checked = True

        Me.Left = 0
        Me.Top = 0
    End Sub
    Private Sub HistBinSlider_ValueChanged(sender As Object, e As EventArgs) Handles HistBinSlider.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        histBins.Text = CStr(HistBinSlider.Value)
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
    Private Sub RadioButton1_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton1.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        radioText = RadioButton1.Text
    End Sub
    Private Sub RadioButton4_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton4.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        radioText = RadioButton4.Text
    End Sub
    Private Sub RadioButton3_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton3.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        radioText = RadioButton3.Text
    End Sub
    Private Sub RadioButton2_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton2.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        radioText = RadioButton2.Text
    End Sub
    Private Sub Channels01_CheckedChanged(sender As Object, e As EventArgs) Handles Channels01.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        channels = {0, 1}
    End Sub
    Private Sub Channels02_CheckedChanged(sender As Object, e As EventArgs) Handles Channels02.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        channels = {0, 2}
    End Sub
    Private Sub Channels12_CheckedChanged(sender As Object, e As EventArgs) Handles Channels12.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        channels = {1, 2}
    End Sub
    Private Sub UpperSlider_ValueChanged(sender As Object, e As EventArgs) Handles UpperSlider.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        UpperLabel.Text = CStr(UpperSlider.Value)
    End Sub
    Private Sub LowerSliderr_ValueChanged(sender As Object, e As EventArgs) Handles LowerSlider.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        LowerLabel.Text = CStr(LowerSlider.Value)
    End Sub
    Private Sub SimpleReduction_CheckedChanged(sender As Object, e As EventArgs) Handles SimpleReduction.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        reduction = simpleReduce
    End Sub
    Private Sub BitwiseReduction_CheckedChanged(sender As Object, e As EventArgs) Handles BitwiseReduction.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        reduction = bitwiseReduce
    End Sub
    Private Sub NoReduction_CheckedChanged(sender As Object, e As EventArgs) Handles NoReduction.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        reduction = noReduce
    End Sub
    Private Sub ColorReductionSlider_ValueChanged(sender As Object, e As EventArgs) Handles ColorReductionSlider.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        ColorLabel.Text = CStr(ColorReductionSlider.Value)
    End Sub
    Private Sub BitwiseReductionSlider_ValueChanged(sender As Object, e As EventArgs) Handles BitwiseReductionSlider.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        bitwiseLabel.Text = CStr(BitwiseReductionSlider.Value)
    End Sub
End Class