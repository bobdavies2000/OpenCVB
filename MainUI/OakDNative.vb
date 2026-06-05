Imports System.IO
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Namespace MainApp
    ''' <summary>
    ''' Loads Cam_Oak-D.dll from the app directory and resolves exports via NativeLibrary
    ''' (avoids DllImport picking up a stale/broken copy from PATH).
    ''' </summary>
    Friend Module OakDNative
        Private Const DllName As String = "Cam_Oak-D.dll"

        Private _handle As IntPtr = IntPtr.Zero

        Private _open As OpenDelegate
        Private _waitForFrame As WaitForFrameDelegate
        Private _color As PtrDelegate
        Private _leftImage As PtrDelegate
        Private _rightImage As PtrDelegate
        Private _rawDepth3D As PtrDelegate
        Private _rawDepth4D As PtrDelegate
        Private _gyro As PtrDelegate
        Private _accel As PtrDelegate
        Private _imuTimeStamp As ImuTimeStampDelegate
        Private _intrinsics As IntrinsicsDelegate
        Private _extrinsicsRgbToLeft As PtrDelegate
        Private _extrinsicsLeftToRight As PtrDelegate
        Private _stop As StopDelegate
        Private _disparity As PtrDelegate
        Private _devices As DevicesDelegate
        Private _nextDevice As NextDeviceDelegate

        <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
        Private Delegate Function OpenDelegate(w As Integer, h As Integer, deviceIndex As Integer) As IntPtr

        <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
        Private Delegate Sub WaitForFrameDelegate(cPtr As IntPtr)

        <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
        Private Delegate Function PtrDelegate(cPtr As IntPtr) As IntPtr

        <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
        Private Delegate Function ImuTimeStampDelegate(cPtr As IntPtr) As Double

        <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
        Private Delegate Function IntrinsicsDelegate(cPtr As IntPtr, camera As Integer) As IntPtr

        <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
        Private Delegate Sub StopDelegate(cPtr As IntPtr)

        <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
        Private Delegate Function DevicesDelegate() As Integer

        <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
        Private Delegate Function NextDeviceDelegate() As IntPtr

        <ModuleInitializer>
        Public Sub RegisterDllImportResolver()
            NativeLibrary.SetDllImportResolver(
                Assembly.GetExecutingAssembly(),
                Function(libraryName, assembly, searchPath)
                    If String.Equals(libraryName, DllName, StringComparison.OrdinalIgnoreCase) Then
                        Return EnsureHandle()
                    End If
                    Return IntPtr.Zero
                End Function)
        End Sub

        Friend Sub EnsureLoaded()
            EnsureHandle()
        End Sub

        Friend Function LoadedDllPath() As String
            Return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, DllName))
        End Function

        Private Function EnsureHandle() As IntPtr
            If _handle <> IntPtr.Zero Then Return _handle
            SyncLock GetType(OakDNative)
                If _handle <> IntPtr.Zero Then Return _handle

                Dim dllPath = LoadedDllPath()
                If Not File.Exists(dllPath) Then
                    Throw New FileNotFoundException(
                        $"{DllName} not found at {dllPath}. Build Cam_Oak-D (Release|x64) then rebuild MainUI.",
                        dllPath)
                End If

                _handle = NativeLibrary.Load(dllPath)
                BindDelegates()
                Return _handle
            End SyncLock
        End Function

        Private Sub BindDelegates()
            _open = GetDelegate(Of OpenDelegate)("OakDOpen")
            _waitForFrame = GetDelegate(Of WaitForFrameDelegate)("OakDWaitForFrame")
            _color = GetDelegate(Of PtrDelegate)("OakDColor")
            _leftImage = GetDelegate(Of PtrDelegate)("OakDLeftImage")
            _rightImage = GetDelegate(Of PtrDelegate)("OakDRightImage")
            _rawDepth3D = GetDelegate(Of PtrDelegate)("OakDRawDepth3D")
            _rawDepth4D = GetDelegate(Of PtrDelegate)("OakDRawDepth4D")
            _gyro = GetDelegate(Of PtrDelegate)("OakDGyro")
            _accel = GetDelegate(Of PtrDelegate)("OakDAccel")
            _imuTimeStamp = GetDelegate(Of ImuTimeStampDelegate)("OakDIMUTimeStamp")
            _intrinsics = GetDelegate(Of IntrinsicsDelegate)("OakDintrinsics")
            _extrinsicsRgbToLeft = GetDelegate(Of PtrDelegate)("OakDExtrinsicsRGBtoLeft")
            _extrinsicsLeftToRight = GetDelegate(Of PtrDelegate)("OakDExtrinsicsLeftToRight")
            _stop = GetDelegate(Of StopDelegate)("OakDStop")
            _disparity = GetDelegate(Of PtrDelegate)("OakDDisparity")
            _devices = GetDelegate(Of DevicesDelegate)("OakDDevices")
            _nextDevice = GetDelegate(Of NextDeviceDelegate)("OakDNextDevice")
        End Sub

        Private Function GetDelegate(Of T As Class)(exportName As String) As T
            Dim exportPtr As IntPtr
            If Not NativeLibrary.TryGetExport(_handle, exportName, exportPtr) OrElse exportPtr = IntPtr.Zero Then
                Dim info = New FileInfo(LoadedDllPath())
                Throw New EntryPointNotFoundException(
                    $"Export '{exportName}' not found in {info.FullName} ({info.Length} bytes). " &
                    "Rebuild Cam_Oak-D (Release|x64) and rebuild MainUI.")
            End If
            Return Marshal.GetDelegateForFunctionPointer(Of T)(exportPtr)
        End Function

        Friend Function OakDOpen(w As Integer, h As Integer, deviceIndex As Integer) As IntPtr
            Return _open(w, h, deviceIndex)
        End Function

        Friend Sub OakDWaitForFrame(cPtr As IntPtr)
            _waitForFrame(cPtr)
        End Sub

        Friend Function OakDColor(cPtr As IntPtr) As IntPtr
            Return _color(cPtr)
        End Function

        Friend Function OakDLeftImage(cPtr As IntPtr) As IntPtr
            Return _leftImage(cPtr)
        End Function

        Friend Function OakDRightImage(cPtr As IntPtr) As IntPtr
            Return _rightImage(cPtr)
        End Function

        Friend Function OakDRawDepth3D(cPtr As IntPtr) As IntPtr
            Return _rawDepth3D(cPtr)
        End Function

        Friend Function OakDRawDepth4D(cPtr As IntPtr) As IntPtr
            Return _rawDepth4D(cPtr)
        End Function

        Friend Function OakDGyro(cPtr As IntPtr) As IntPtr
            Return _gyro(cPtr)
        End Function

        Friend Function OakDAccel(cPtr As IntPtr) As IntPtr
            Return _accel(cPtr)
        End Function

        Friend Function OakDIMUTimeStamp(cPtr As IntPtr) As Double
            Return _imuTimeStamp(cPtr)
        End Function

        Friend Function OakDintrinsics(cPtr As IntPtr, camera As Integer) As IntPtr
            Return _intrinsics(cPtr, camera)
        End Function

        Friend Function OakDExtrinsicsRGBtoLeft(cPtr As IntPtr) As IntPtr
            Return _extrinsicsRgbToLeft(cPtr)
        End Function

        Friend Function OakDExtrinsicsLeftToRight(cPtr As IntPtr) As IntPtr
            Return _extrinsicsLeftToRight(cPtr)
        End Function

        Friend Sub OakDStop(cPtr As IntPtr)
            _stop(cPtr)
        End Sub

        Friend Function OakDDisparity(cPtr As IntPtr) As IntPtr
            Return _disparity(cPtr)
        End Function

        Friend Function OakDDevices() As Integer
            Return _devices()
        End Function

        Friend Function OakDNextDevice() As IntPtr
            Return _nextDevice()
        End Function
    End Module
End Namespace
