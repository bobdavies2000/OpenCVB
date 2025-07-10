Imports cv = OpenCvSharp
Public Class OptionsRedColor
    Public depthInputIndex As Integer = 0 ' guidedBP is the default.

    Public SimpleReduction As Integer
    Public SimpleReductionChecked As Boolean

    Public bitReduction As Integer
    Public bitReductionChecked As Boolean

    Public PointCloudReductionLabel As String
    Public trackingLabel As String
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

        task.xRangeDefault = task.xRange
        task.yRangeDefault = task.yRange

        task.sideCameraPoint = New cv.Point(0, CInt(task.dst2.Height / 2))
        task.topCameraPoint = New cv.Point(CInt(task.dst2.Width / 2), 0)

        task.channelsTop = {2, 0}
        task.channelsSide = {1, 2}

        PointCloudReductionLabel = "XY Reduction"

        Select Case task.cameraName
            Case "Oak-D camera"
                task.redOptions.setBitReductionBar(80)
            Case Else
                task.redOptions.setBitReductionBar(40)
        End Select

        task.redOptions.setBitReductionBar(5)

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
    End Sub





    Private Sub BitwiseReductionSlider_ValueChanged(sender As Object, e As EventArgs) Handles BitwiseReductionBar.ValueChanged
        task.optionsChanged = True
        bitReduction = BitwiseReductionBar.Value
        bitwiseLabel.Text = CStr(BitwiseReductionBar.Value)
    End Sub



    Private Sub XReduction_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
        PointCloudReductionLabel = "X Reduction"
    End Sub
    Private Sub YReduction_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
        PointCloudReductionLabel = "Y Reduction"
    End Sub
    Private Sub ZReduction_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
        PointCloudReductionLabel = "Z Reduction"
    End Sub
    Private Sub ReductionXY_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
        PointCloudReductionLabel = "XY Reduction"
    End Sub
    Private Sub XZReduction_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
        PointCloudReductionLabel = "XZ Reduction"
    End Sub
    Private Sub YZReduction_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
        PointCloudReductionLabel = "YZ Reduction"
    End Sub
    Public Sub XYZReduction_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
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



    Public Sub setBitReductionBar(newVal As Integer)
        If newVal > BitwiseReductionBar.Maximum Then BitwiseReductionBar.Maximum = newVal
        BitwiseReductionBar.Value = newVal
    End Sub
    Public Function getBitReductionBar() As Integer
        Return BitwiseReductionBar.Value
    End Function
End Class
