Imports cv = OpenCvSharp
Imports System.Windows.Forms
Imports System.IO.Pipes
Module Algorithm_Module
    Public task As ActiveTask
    Public allOptions As OptionsContainer
    Public Const RESULT_DST0 = 0 ' 0=rgb 1=depth 2=dst1 3=dst2
    Public Const RESULT_DST1 = 1 ' 0=rgb 1=depth 2=dst1 3=dst2
    Public Const RESULT_DST2 = 2 ' 0=rgb 1=depth 2=dst1 3=dst2
    Public Const RESULT_DST3 = 3 ' 0=rgb 1=depth 2=dst1 3=dst2
    Public Const screenDWidth As Integer = 18
    Public Const screenDHeight As Integer = 20
    Public term As New cv.TermCriteria(cv.CriteriaTypes.Eps + cv.CriteriaTypes.Count, 10, 1.0)
    Public recordedData As Replay_Play

    Public algorithmTimes As New List(Of DateTime)
    Public algorithmStack As New Stack()

    <System.Runtime.CompilerServices.Extension()>
    Public Sub SwapWith(Of T)(ByRef thisObj As T, ByRef withThisObj As T)
        Dim tempObj = thisObj
        thisObj = withThisObj
        withThisObj = tempObj
    End Sub
    Public Sub Swap(Of T)(ByRef a As T, ByRef b As T)
        Dim temp = b
        b = a
        a = temp
    End Sub
    Public Function findfrm(title As String) As Windows.Forms.Form
        For Each frm In Application.OpenForms
            If frm.text = title Then Return frm
        Next
        Return Nothing
    End Function
    Public Function findCheckBox(opt As String) As CheckBox
        While 1
            Try
                For Each frm In Application.OpenForms
                    If frm.text.endswith(" CheckBox Options") Then
                        For j = 0 To frm.Box.length - 1
                            If frm.Box(j).text.contains(opt) Then Return frm.Box(j)
                        Next
                    End If
                Next
            Catch ex As Exception
                Console.WriteLine("findCheckBox failed.  The application list of forms changed while iterating.  Not critical.")
            End Try
            Application.DoEvents()
            Static retryCount As Integer
            retryCount += 1
            If retryCount >= 5 Then
                Console.WriteLine("A checkbox was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")
                Exit While
            End If
        End While
        Return Nothing
    End Function
    Public Function findRadio(opt As String) As RadioButton
        While 1
            Try
                For Each frm In Application.OpenForms
                    If frm.text.endswith(" Radio Options") Then
                        For j = 0 To frm.check.length - 1
                            If frm.check(j).text.contains(opt) Then Return frm.check(j)
                        Next
                    End If
                Next
            Catch ex As Exception
                Console.WriteLine("findRadio failed.  The application list of forms changed while iterating.  Not critical.")
            End Try
            Application.DoEvents()
            Static retryCount As Integer
            retryCount += 1
            If retryCount >= 5 Then
                Console.WriteLine("A findRadio was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")
                Exit While
            End If
        End While
        Return Nothing
    End Function
    Public Function findSlider(opt As String) As TrackBar
        Try
            For Each frm In Application.OpenForms
                If frm.text.endswith(" Slider Options") Then
                    For j = 0 To frm.trackbar.length - 1
                        If frm.sLabels(j).text.contains(opt) Then Return frm.trackbar(j)
                    Next
                End If
            Next
        Catch ex As Exception
            Console.WriteLine("findSlider failed.  The application list of forms changed while iterating.  Not critical.")
        End Try
        Console.WriteLine("A slider was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")

        Return Nothing
    End Function
    Public Sub startRun(name As String)
        SyncLock algorithmStack
            If task.algorithmNames.Contains(name) = False Then
                task.algorithmNames.Add(name)
                task.algorithm_ms.Add(0)
                algorithmTimes.Add(Now)
            End If
            Dim nextTime = Now

            Dim index = algorithmStack.Peek
            Dim elapsedTicks = nextTime.Ticks - algorithmTimes(index).Ticks
            Dim span = New TimeSpan(elapsedTicks)
            task.algorithm_ms(index) += span.Ticks / TimeSpan.TicksPerMillisecond

            index = task.algorithmNames.IndexOf(name)
            algorithmTimes(index) = nextTime
            algorithmStack.Push(index)
        End SyncLock
    End Sub
    Public Sub endRun(name As String)
        SyncLock algorithmStack
            Dim nextTime = Now
            Dim index = algorithmStack.Peek
            Dim elapsedTicks = nextTime.Ticks - algorithmTimes(index).Ticks
            Dim span = New TimeSpan(elapsedTicks)
            task.algorithm_ms(index) += span.Ticks / TimeSpan.TicksPerMillisecond
            algorithmStack.Pop()
            algorithmTimes(algorithmStack.Peek) = nextTime
        End SyncLock
    End Sub
End Module






Public Class ActiveTask : Implements IDisposable
#Region "VBTask variables"
    Dim TaskTimer As New System.Timers.Timer(1000)
    Public WarningCount As Integer
    Dim algoList As New algorithmList
    Public algorithmObject As Object
    Public frameCount As Integer = 0
    Public paused As Boolean

    Public color As cv.Mat
    Public RGBDepth As cv.Mat
    Public imgResult As New cv.Mat
    Public pointCloud As cv.Mat
    Public leftView As cv.Mat
    Public rightView As cv.Mat
    Public viewOptions As Object
    Public PixelViewer As Object
    Public IMUStable As Object
    Public IMULevel As Object

    ' add any global option algorithms and structures here
    Public depthOptions As Object
    Public noDepthMask As New cv.Mat
    Public depthMask As New cv.Mat
    Public depth32f As New cv.Mat
    Public depthOptionsChanged As Boolean

    Public hist3DThreshold As Integer
    Public histogramBins As Integer

    Public useKalman As Boolean
    Public useKalmanWhenStable As Boolean

    Public cameraStable As Boolean
    Public cameraLevel As Boolean
    Public cameraMotionLimit As Single
    Public cameraLevelLimit As Single

    Public palette As Palette_Basics
    Public paletteGradient As cv.Mat
    Public paletteScheme As cv.ColormapTypes
    Public paletteSchemeName As String

    Public minDepth As Integer
    Public maxDepth As Integer

    Public mouseClickFlag As Boolean
    Public mouseClickPoint As cv.Point
    Public mousePicTag As Integer ' which image was the mouse in?
    Public mousePoint As cv.Point ' trace any mouse movements using this.
    Public mousePointUpdated As Boolean
    Public parms As ActiveTask.algParms
    Public defaultRect As cv.Rect
    Public dst0Updated As Boolean
    Public dst1Updated As Boolean

    Public font As cv.HersheyFonts
    Public fontSize As Single
    Public dotSize As Integer
    Public lineWidth As Integer
    Public lineType As cv.LineTypes
    Public resolutionIndex As Integer
    Public AddWeighted As Single

    Public IMU_Barometer As Single
    Public IMU_Magnetometer As cv.Point3f
    Public IMU_Temperature As Single
    Public IMU_TimeStamp As Double
    Public IMU_Rotation As System.Numerics.Quaternion
    Public IMU_Translation As cv.Point3f
    Public IMU_Acceleration As cv.Point3f
    Public IMU_Velocity As cv.Point3f
    Public IMU_AngularAcceleration As cv.Point3f
    Public IMU_AngularVelocity As cv.Point3f
    Public IMU_FrameTime As Double
    Public CPU_TimeStamp As Double
    Public CPU_FrameTime As Double

    Public drawRect As cv.Rect ' filled in if the user draws on any of the images.
    Public drawRectClear As Boolean ' used to remove the drawing rectangle when it has been used to initialize a camshift or mean shift.
    Public drawRectUpdated As Boolean

    Public pixelViewerRect As cv.Rect
    Public pixelViewTag As Integer

    Public pipeIn As NamedPipeServerStream
    Public pipeOut As NamedPipeServerStream
    Public pipeName As String
    Public pipeIndex As Integer ' back-to-back pipe usage can sometimes have 2 active pipes.  This index avoids conflict...
    Public pipe As NamedPipeServerStream
    Public pythonTaskName As String

    Public labels(4 - 1) As String
    Public desc As String
    Public intermediateName As String
    Public intermediateObject As VBparent
    Public intermediateActive As Boolean
    Public activeObjects As New List(Of Object)
    Public ratioImageToCampic As Single
    Public pixelViewerOn As Boolean

    Public transformationMatrix() As Single

    Public scalarColors(255) As cv.Scalar
    Public vecColors(255) As cv.Vec3b

    Public topCameraPoint As cv.Point
    Public sideCameraPoint As cv.Point
    Public maxX As Single
    Public maxY As Single
    Public maxZ As Single

    Public hFov As Single
    Public vFov As Single
    Public angleX As Single  ' rotation angle in radians around x-axis to align with gravity
    Public angleY As Single  ' this angle is only used manually - no IMU connection.
    Public angleZ As Single  ' rotation angle in radians around z-axis to align with gravity

    Public algName As String

    Public ttTextData As New List(Of TTtext)
    Public callTrace As New List(Of String)

    Public algorithm_ms As New List(Of Single)
    Public algorithmNames As New List(Of String)
    Public algorithmFrameCount As Integer
    Public algorithmAccumulate As Boolean ' accumulate times or use latest interval times.

    Public Structure Extrinsics_VB
        Public rotation As Single()
        Public translation As Single()
    End Structure
    Public Structure intrinsics_VB
        Public ppx As Single
        Public ppy As Single
        Public fx As Single
        Public fy As Single
        Public coeffs As Single()
        Public FOV As Single()
    End Structure
    Public Structure algParms
        Public cameraName As camNames
        Enum camNames
            Kinect4AzureCam
            StereoLabsZED2
            MyntD1000
            D435i
            D455
            OakDCamera
        End Enum

        Public PythonExe As String
        Public homeDir As String
        Public useRecordedData As Boolean
        Public externalPythonInvocation As Boolean ' OpenCVB was initialized remotely...
        Public ShowConsoleLog As Boolean
        Public testAllRunning As Boolean
        Public RotationMatrix() As Single
        Public RotationVector As cv.Point3f
        Public VTK_Present As Boolean
        Public IMU_Present As Boolean
        Public pixelViewerOn As Boolean

        Public intrinsicsLeft As intrinsics_VB
        Public intrinsicsRight As intrinsics_VB
        Public extrinsics As Extrinsics_VB
    End Structure
    Private Sub buildColors()
        Dim vec As cv.Scalar, r As Integer = 120, b As Integer = 255, g As Integer = 0
        Dim scalarList As New List(Of cv.Scalar)
        For i = 0 To 255
            Select Case i Mod 3
                Case 0
                    vec = New cv.Scalar(b, g, r)
                    r = (r + 50) Mod 255
                Case 1
                    vec = New cv.Scalar(b, g, r)
                    g = (g + 75) Mod 255
                Case 2
                    vec = New cv.Scalar(b, g, r)
                    b = (b + 150) Mod 255
            End Select
            If scalarList.Contains(New cv.Scalar(b, g, r)) Then b = (b + 100) Mod 255 ' try not to have duplicates.
            If r + g + b < 180 Then r = 120 ' need bright colors.

            task.scalarColors(i) = New cv.Scalar(b, g, r)
            scalarList.Add(task.scalarColors(i))
        Next
        Dim msrng As New System.Random
        For i = 0 To task.vecColors.Length - 1
            task.vecColors(i) = New cv.Vec3b(msrng.Next(100, 255), msrng.Next(100, 255), msrng.Next(100, 255)) ' note: cannot generate black!
            task.scalarColors(i) = New cv.Scalar(task.vecColors(i).Item0, task.vecColors(i).Item1, task.vecColors(i).Item2)
        Next
    End Sub
#End Region
    Private Sub VBTaskTimerPop(sender As Object, e As EventArgs)
        Static saveFrameCount = -1
        If saveFrameCount = frameCount And frameCount > 0 Then
            Console.WriteLine("Warning: " + task.algName + " has not completed work on a frame in a second. Warning " + CStr(WarningCount))
            WarningCount += 1
        Else
            WarningCount = 0
            saveFrameCount = frameCount
        End If
        saveFrameCount = frameCount
    End Sub
    Public Sub trueText(text As String, Optional x As Integer = 10, Optional y As Integer = 40, Optional picTag As Integer = 2)
        Dim str As New TTtext(text, x, y, picTag)
        task.ttTextData.Add(str)
    End Sub
    Public Sub New(parms As algParms, resolution As cv.Size, _algName As String, camWidth As Integer, camHeight As Integer, _defaultRect As cv.Rect)
        AddHandler TaskTimer.Elapsed, New Timers.ElapsedEventHandler(AddressOf VBTaskTimerPop)
        TaskTimer.AutoReset = True
        TaskTimer.Enabled = True
        Randomize() ' just in case anyone uses VB.Net's Rnd
        color = New cv.Mat(resolution.Height, resolution.Width, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        RGBDepth = New cv.Mat(color.Size(), cv.MatType.CV_8UC3, cv.Scalar.All(0))
        pointCloud = New cv.Mat(camHeight, camWidth, cv.MatType.CV_32FC3, cv.Scalar.All(0))
        imgResult = New cv.Mat(color.Height, color.Width * 2, cv.MatType.CV_8UC3, cv.Scalar.All(0))

        task = Me
        task.algName = _algName
        task.parms = parms
        task.defaultRect = _defaultRect
        font = cv.HersheyFonts.HersheyComplex
        resolutionIndex = If(task.color.Width = 640, 2, 3)

        buildColors()
        task.pythonTaskName = task.parms.homeDir + "VB_Classes\" + task.algName

        allOptions = New OptionsContainer

        If task.algName.EndsWith(".py") = False Then allOptions.Show()

        If task.paused Then PixelViewer = algoList.createAlgorithm("Pixel_Viewer")

        task.callTrace.Add("OptionsCommon_Histogram") ' so calltrace is not nothing on initial call...
        viewOptions = algoList.createAlgorithm("OptionsCommon_Histogram")
        depthOptions = algoList.createAlgorithm("OptionsCommon_Depth")
        IMUStable = algoList.createAlgorithm("IMU_IscameraStable")
        IMULevel = algoList.createAlgorithm("IMU_IsCameraLevel")
        PixelViewer = algoList.createAlgorithm("Pixel_Viewer")

        task.callTrace.Clear()
        task.callTrace.Add(task.algName + "\")
        task.activeObjects.Clear()
        algorithmObject = algoList.createAlgorithm(task.algName)

        If algorithmObject Is Nothing Then
            task.desc = "The algorithm: " + task.algName + " was not found"
            task.trueText("The algorithm: " + task.algName + " was not found in the algorithmList.vb code." + vbCrLf +
                          "If using on the <Rank x> groups, rerun the Survey (takes a while) and then the UIRanking project." + vbCrLf +
                          "Otherwise, there must be a problem in the UIindexer", 10, 200, 3)

            depthOptions.standalone = True
            algorithmObject = depthOptions
        End If
        If parms.useRecordedData Then recordedData = New Replay_Play()

        ' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
        ' https://support.stereolabs.com/hc/en-us/articles/360007395634-What-is-the-camera-focal-length-and-field-of-view-
        ' https://www.mynteye.com/pages/mynt-eye-d   
        ' https://www.intelrealsense.com/depth-camera-d435i/
        ' https://www.intelrealsense.com/depth-camera-d455/
        ' https://towardsdatascience.com/opinion-26190c7fed1b
        ' order of cameras is the same as the order above...
        ' Microsoft Kinect4Azure, StereoLabs Zed 2, Mynt EyeD 1000, RealSense D435i, RealSense D455, Oak-D
        Dim hFOVangles() As Single = {90, 104, 105, 69.4, 86, 72} ' all values from the specification - this is usually overridden by calibration data.
        Dim vFOVangles() As Single = {59, 72, 58, 42.5, 57, 81} ' all values from the specification - this is usually overridden by calibration data.
        task.hFov = hFOVangles(parms.cameraName)
        task.vFov = vFOVangles(parms.cameraName) ' these are default values in case the calibration data is unavailable

        If allOptions IsNot Nothing Then allOptions.layoutOptions()
        Application.DoEvents()
    End Sub
    Public Sub RunAlgorithm()
        Try
            If algorithmAccumulate = False Then
                If task.frameCount Mod 30 = 0 Then
                    For i = 0 To algorithm_ms.Count - 1
                        algorithm_ms(i) = 0
                    Next
                    algorithmFrameCount = 1
                Else
                    algorithmFrameCount += 1
                End If
            Else
                algorithmFrameCount += 1
            End If
            If task.parms.useRecordedData Then recordedData.RunClass(task.color.Clone)

            Static lastTime = Now
            Dim nextTime = Now
            Dim elapsedTicks = nextTime.Ticks - lastTime.Ticks
            Dim span = New TimeSpan(elapsedTicks)
            algorithm_ms(0) += span.Ticks / TimeSpan.TicksPerMillisecond
            algorithmTimes(1) = nextTime ' starting the main algorithm

            ' run any global options algorithms here.
            viewOptions.RunClass(Nothing) ' updates any new input from the sliders (when setting up a new camera.)
            depthOptions.RunClass(Nothing) ' updates all the depth info.
            IMUStable.RunClass(Nothing) ' updates the flag that indicates stability according to the IMU.
            IMULevel.RunClass(Nothing)  ' updates the flag that indicate the camera is level according to the IMU

            TaskTimer.Enabled = True
            algorithmObject.NextFrame(task.color.Clone)
            TaskTimer.Enabled = False

            lastTime = nextTime

        Catch ex As Exception
            Console.WriteLine("Active Algorithm exception occurred: " + ex.Message)
        End Try
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        TaskTimer.Enabled = False
        If task.pipeOut IsNot Nothing Then pipeOut.Close()
        If task.pipeIn IsNot Nothing Then pipeIn.Close()
        If task.pipe IsNot Nothing Then pipe.Close()
        If recordedData IsNot Nothing Then recordedData.Dispose()
        If algorithmObject IsNot Nothing Then algorithmObject.Dispose()
    End Sub
    Public Sub trueText(text As String, pt As cv.Point, Optional picTag As Integer = 2)
        Dim str As New TTtext(text, pt.X, pt.Y, picTag)
        task.ttTextData.Add(str)
    End Sub
End Class