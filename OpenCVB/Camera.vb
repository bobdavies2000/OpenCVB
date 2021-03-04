Imports cv = OpenCvSharp
Imports System.Numerics
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices




Module Palette_Custom_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Palette_Custom(img As IntPtr, map As IntPtr, dst1 As IntPtr, rows As Integer, cols As Integer, channels As Integer)
    End Sub
    Public mapNames() As String = {"Autumn", "Bone", "Cividis", "Cool", "Hot", "Hsv", "Inferno", "Jet", "Magma", "Ocean", "Parula", "Pink",
                                   "Plasma", "Rainbow", "Spring", "Summer", "Twilight", "TwilightShifted", "Viridis", "Winter", "Random - use slider to adjust"}
    Public Function Palette_Custom_Apply(src As cv.Mat, customColorMap As cv.Mat) As cv.Mat
        ' the VB.Net interface to OpenCV doesn't support adding a random lookup table to ApplyColorMap API.  It is available in C++ though.
        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)

        Dim mapData(customColorMap.Total * customColorMap.ElemSize - 1) As Byte
        Dim handleMap = GCHandle.Alloc(mapData, GCHandleType.Pinned)
        Marshal.Copy(customColorMap.Data, mapData, 0, mapData.Length)

        Dim dstData(src.Total * 3 - 1) As Byte ' it always comes back in color...
        Dim handledst1 = GCHandle.Alloc(dstData, GCHandleType.Pinned)

        ' the custom colormap API is not implemented for custom color maps.  Only colormapTypes can be provided.
        Palette_Custom(handleSrc.AddrOfPinnedObject, handleMap.AddrOfPinnedObject, handledst1.AddrOfPinnedObject, src.Rows, src.Cols, src.Channels)

        Dim output = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
        Marshal.Copy(dstData, 0, output.Data, dstData.Length)
        handleSrc.Free()
        handleMap.Free()
        handledst1.Free()
        Return output
    End Function
    Public Function colorTransition(color1 As cv.Scalar, color2 As cv.Scalar, width As Integer) As cv.Mat
        Dim f As Double = 1.0
        Dim gradientColors As New cv.Mat(1, width, cv.MatType.CV_64FC3)
        For i = 0 To width - 1
            gradientColors.Set(Of cv.Scalar)(0, i, New cv.Scalar(f * color2(0) + (1 - f) * color1(0), f * color2(1) + (1 - f) * color1(1),
                                                                     f * color2(2) + (1 - f) * color1(2)))
            f -= 1 / width
        Next
        Dim result = New cv.Mat(1, width, cv.MatType.CV_8UC3)
        For i = 0 To width - 1
            result.Col(i).SetTo(gradientColors.Get(Of cv.Scalar)(0, i))
        Next
        Return result
    End Function
End Module




Public Class Camera
    Public transformationMatrix() As Single
    Public IMU_Barometer As Single
    Public IMU_Magnetometer As cv.Point3f
    Public IMU_Temperature As Single
    Public IMU_TimeStamp As Double
    Public IMU_Rotation As System.Numerics.Quaternion
    Public RotationMatrix(9 - 1) As Single
    Public RotationVector As cv.Point3f
    Public IMU_Translation As cv.Point3f
    Public IMU_Acceleration As cv.Point3f
    Public IMU_Velocity As cv.Point3f
    Public IMU_AngularAcceleration As cv.Point3f
    Public IMU_AngularVelocity As cv.Point3f
    Public IMU_FrameTime As Double
    Public CPU_TimeStamp As Double
    Public CPU_FrameTime As Double
    Public frameCount As Integer

    Public color As New cv.Mat
    Public RGBDepth As New cv.Mat
    Public leftView As New cv.Mat
    Public rightView As New cv.Mat
    Public pointCloud As New cv.Mat
    Public depth16 As New cv.Mat
    Public depth32f As New cv.Mat
    Public width As Integer, height As Integer

    Public deviceCount As Integer
    Public deviceName As String = ""
    Public Extrinsics_VB As VB_Classes.ActiveTask.Extrinsics_VB
    Public intrinsicsLeft_VB As VB_Classes.ActiveTask.intrinsics_VB
    Public intrinsicsRight_VB As VB_Classes.ActiveTask.intrinsics_VB
    Public colorBytes() As Byte
    Public vertices() As Byte
    Public depthBytes() As Byte
    Public RGBDepthBytes() As Byte
    Public leftViewBytes() As Byte
    Public rightViewBytes() As Byte
    Public pointCloudBytes() As Byte

    Public serialNumber As String
    Public deviceIndex As Integer
    Public failedImageCount As Integer
    Public modelInverse As Boolean
    Public cameraRGBDepth As Boolean = True
    Dim customColorMap As New cv.Mat
    Public Structure imuDataStruct
        Dim r00 As Single
        Dim r01 As Single
        Dim r02 As Single
        Dim tx As Single
        Dim r10 As Single
        Dim r11 As Single
        Dim r12 As Single
        Dim ty As Single
        Dim r20 As Single
        Dim r21 As Single
        Dim r22 As Single
        Dim tz As Single
        Dim m30 As Single
        Dim m31 As Single
        Dim m32 As Single
        Dim m33 As Single
    End Structure
    Structure PoseData
        Public translation As cv.Point3f
        Public velocity As cv.Point3f
        Public acceleration As cv.Point3f
        Public rotation As Quaternion
        Public angularVelocity As cv.Point3f
        Public angularAcceleration As cv.Point3f
        Public trackerConfidence As Integer
        Public mapperConfidence As Integer
    End Structure
    Public Function setintrinsics(intrinsicsHW As rs.Intrinsics) As VB_Classes.ActiveTask.intrinsics_VB
        Dim intrinsics As New VB_Classes.ActiveTask.intrinsics_VB
        intrinsics.ppx = intrinsicsHW.ppx
        intrinsics.ppy = intrinsicsHW.ppy
        intrinsics.fx = intrinsicsHW.fx
        intrinsics.fy = intrinsicsHW.fy
        intrinsics.FOV = intrinsicsHW.FOV
        intrinsics.coeffs = intrinsicsHW.coeffs
        Return intrinsics
    End Function
    Public Sub New()
        customColorMap = colorTransition(cv.Scalar.Blue, cv.Scalar.Yellow, 256)
    End Sub
    Public Sub GetNextFrameCounts(frameTime As Double)
        Static imageCounter As Integer
        Static totalMS = frameTime
        If totalMS > 1000 Then
            imageCounter = 0
            totalMS = totalMS - 1000
        End If
        imageCounter += 1
        totalMS += frameTime

        Static lastFrameTime = IMU_TimeStamp
        Static imuStartTime = IMU_TimeStamp
        IMU_FrameTime = IMU_TimeStamp - lastFrameTime - imuStartTime
        lastFrameTime = IMU_TimeStamp - imuStartTime

        Static myStopWatch As New System.Diagnostics.Stopwatch
        If frameCount = 0 Then myStopWatch.Start()
        CPU_TimeStamp = myStopWatch.ElapsedMilliseconds
        Static lastCPUTime = CPU_TimeStamp
        CPU_FrameTime = CPU_TimeStamp - lastCPUTime
        lastCPUTime = CPU_TimeStamp

        ' if the camera is not providing the depth then build it manually here.
        If cameraRGBDepth = False Then
            Dim minDepth = 0
            Dim maxDepth = 4000
            Dim depthNormalized = (depth16 * 255 / (maxDepth - minDepth)).ToMat
            depthNormalized.ConvertTo(depthNormalized, cv.MatType.CV_8U)
            Dim mask = depthNormalized.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
            RGBDepth = Palette_Custom_Apply(depthNormalized.CvtColor(cv.ColorConversionCodes.GRAY2BGR), customColorMap)
            RGBDepth.SetTo(0, mask)
        End If

        frameCount += 1
    End Sub
End Class
