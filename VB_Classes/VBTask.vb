Imports cv = OpenCvSharp
Imports System.IO
Imports System.Windows.Forms
Module Algorithm_Module
    Public task As ActiveTask
    Public aOptions As OptionsContainer
    Public Const RESULT1 = 2 ' 0=rgb 1=depth 2=result1 3=Result2
    Public Const RESULT2 = 3 ' 0=rgb 1=depth 2=result1 3=Result2
    Public PipeTaskIndex As Integer
    Public vtkTaskIndex As Integer
    Public term As New cv.TermCriteria(cv.CriteriaTypes.Eps + cv.CriteriaTypes.Count, 10, 1.0)
    Public recordedData As Replay_Play
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
End Module






Public Class ActiveTask : Implements IDisposable
#Region "VBTask variables"
    Dim TaskTimer As New System.Timers.Timer(1000)
    Public WarningCount As Integer
    Dim algoList As New algorithmList
    Public algorithmObject As Object
    Public frameCount As Integer = 0

    Public color As cv.Mat
    Public RGBDepth As cv.Mat
    Public imgResult As New cv.Mat
    Public pointCloud As cv.Mat
    Public leftView As cv.Mat
    Public rightView As cv.Mat
    Public viewOptions As Object
    Public PixelViewer As Object
    Public IMUStable As Object

    ' add any global option algorithms and structures here
    Public inrange As Object
    Public noDepthMask As New cv.Mat
    Public depthMask As New cv.Mat
    Public depth32f As New cv.Mat
    Public depthOptionsChanged As Boolean

    Public hist3DThreshold As Integer
    Public useKalman As Boolean
    Public useKalmanWhenStable As Boolean
    Public palette As Palette_Basics
    Public paletteScheme As cv.ColormapTypes
    Public paletteSchemeName As String

    Public minDepth As Integer
    Public maxDepth As Integer
    Public cameraStableSlider As Windows.Forms.TrackBar

    Public mouseClickFlag As Boolean
    Public mouseClickPoint As cv.Point
    Public mousePicTag As Integer ' which image was the mouse in?
    Public mousePoint As cv.Point ' trace any mouse movements using this.
    Public mousePointUpdated As Boolean
    Public parms As ActiveTask.algParms
    Public defaultRect As cv.Rect

    Public font As cv.HersheyFonts
    Public fontSize As Single
    Public dotSize As Integer
    Public lineSize As Integer
    Public resolutionIndex As Integer

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

    Public label1 As String
    Public label2 As String
    Public desc As String
    Public rank As Integer
    Public intermediateReview As String
    Public activeObjects As New List(Of Object)
    Public ratioImageToCampic As Single
    Public pixelViewerOn As Boolean

    Public transformationMatrix() As Single

    Public scalarColors(255) As cv.Scalar
    Public vecColors(255) As cv.Vec3b

    Public topCameraPoint As cv.Point
    Public sideCameraPoint As cv.Point
    Public topFrustrumAdjust As Single
    Public sideFrustrumAdjust As Single

    Public Const MAXZ_DEFAULT = 4
    Public maxZ As Single = MAXZ_DEFAULT

    Public pixelsPerMeter As Single
    Public hFov As Single
    Public vFov As Single
    Public angleX As Single  ' rotation angle in radians around x-axis to align with gravity
    Public angleY As Single  ' this angle is only used manually - no IMU connection.
    Public angleZ As Single  ' rotation angle in radians around z-axis to align with gravity

    Public intermediateObject As VBparent

    Public pythonTaskName As String
    Public algName As String
    Public cameraStable As Boolean
    Public lineType As cv.LineTypes

    Public ttTextData As New List(Of TTtext)
    Public callTrace As New List(Of String)

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
            PythonRS2
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
        Select Case task.color.Width
            Case 320
                fontSize = task.color.Width / task.pointCloud.Width
                dotSize = 3
                lineSize = 1
                resolutionIndex = 1
            Case 640
                fontSize = task.color.Width / task.pointCloud.Width
                dotSize = 7
                lineSize = 2
                resolutionIndex = 2
            Case 1280
                fontSize = 1
                dotSize = 15
                lineSize = 4
                resolutionIndex = 3
        End Select

        buildColors()
        task.pythonTaskName = task.parms.homeDir + "VB_Classes\" + task.algName

        aOptions = New OptionsContainer
        If task.algName.EndsWith(".py") = False Then
            aOptions.Show()
            inrange = algoList.createAlgorithm("OptionsCommon_Depth")
            viewOptions = algoList.createAlgorithm("OptionsCommon_Histogram")
            IMUStable = algoList.createAlgorithm("IMU_IscameraStable")
            PixelViewer = algoList.createAlgorithm("Pixel_Viewer")
        End If

        algorithmObject = algoList.createAlgorithm(task.algName)

        If algorithmObject Is Nothing Then
            MsgBox("The algorithm: " + task.algName + " was not found in the algorithmList.vb code." + vbCrLf +
                   "Problem likely originated with the UIindexer.")
        End If
        If parms.useRecordedData Then recordedData = New Replay_Play()

        ' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
        ' https://support.stereolabs.com/hc/en-us/articles/360007395634-What-is-the-camera-focal-length-and-field-of-view-
        ' https://www.mynteye.com/pages/mynt-eye-d
        ' https://www.intelrealsense.com/depth-camera-d435i/
        ' https://www.intelrealsense.com/depth-camera-d455/
        ' https://towardsdatascience.com/opinion-26190c7fed1b
        ' order of cameras is the same as the order above...
        ' Microsoft Kinect4Azure, StereoLabs Zed 2, Mynt EyeD 1000, RealSense D435i, RealSense D455, Python RS2, Oak-D
        Dim hFOVangles() As Single = {90, 104, 105, 69.4, 86, 86, 72} ' all values from the specification.
        Dim vFOVangles() As Single = {59, 72, 58, 42.5, 57, 57, 81} ' all values from the specification.
        task.hFov = hFOVangles(parms.cameraName)
        task.vFov = vFOVangles(parms.cameraName)

        If aOptions IsNot Nothing Then aOptions.layoutOptions()
        Application.DoEvents()
    End Sub
    Public Sub RunAlgorithm()
        Try
            If task.parms.useRecordedData Then recordedData.Run(task.color.Clone)

            ' run any global options algorithms here.
            If task.pythonTaskName.EndsWith(".py") = False Then
                inrange.Run(task.color.Clone) ' updates all the depth info.
                IMUStable.run(task.color.Clone) ' updates the flag that indicates stability according to the IMU.
            End If

            algorithmObject.NextFrame(task.color.Clone)

            label1 = task.label1
            label2 = task.label2
            intermediateReview = task.intermediateReview
        Catch ex As Exception
            Console.WriteLine("Active Algorithm exception occurred: " + ex.Message)
        End Try
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        TaskTimer.Enabled = False
        If recordedData IsNot Nothing Then recordedData.Dispose()
        If algorithmObject IsNot Nothing Then algorithmObject.Dispose()
    End Sub
    Public Sub trueText(text As String, Optional x As Integer = 10, Optional y As Integer = 40, Optional picTag As Integer = 2)
        Dim str As New TTtext(text, x, y, picTag)
        task.ttTextData.Add(str)
    End Sub
    Public Sub trueText(text As String, pt As cv.Point, Optional picTag As Integer = 2)
        Dim str As New TTtext(text, pt.X, pt.Y, picTag)
        task.ttTextData.Add(str)
    End Sub
End Class