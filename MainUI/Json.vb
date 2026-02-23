Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports jsonShared
Imports Newtonsoft.Json
Imports cv = OpenCvSharp
Namespace MainApp
    Public Class jsonIO
        ' SetupAPI constants for device enumeration (no WMI)
        Private Const DIGCF_PRESENT As UInteger = 2
        Private Const DIGCF_ALLCLASSES As UInteger = 4
        Private Const SPDRP_DEVICEDESC As UInteger = 0
        Private Const SPDRP_FRIENDLYNAME As UInteger = 12

        <StructLayout(LayoutKind.Sequential)>
        Private Structure SP_DEVINFO_DATA
            Public cbSize As UInteger
            Public ClassGuid As Guid
            Public DevInst As UInteger
            Public Reserved As UIntPtr
        End Structure

        <DllImport("setupapi.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Shared Function SetupDiGetClassDevs(ByVal ClassGuid As IntPtr, ByVal Enumerator As String, ByVal hwndParent As IntPtr, ByVal Flags As UInteger) As IntPtr
        End Function

        <DllImport("setupapi.dll", SetLastError:=True)>
        Private Shared Function SetupDiEnumDeviceInfo(ByVal DeviceInfoSet As IntPtr, ByVal MemberIndex As UInteger, ByRef DeviceInfoData As SP_DEVINFO_DATA) As Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Shared Function SetupDiGetDeviceRegistryProperty(ByVal DeviceInfoSet As IntPtr, ByRef DeviceInfoData As SP_DEVINFO_DATA, ByVal [Property] As UInteger,
            ByRef PropertyRegDataType As UInteger, ByVal PropertyBuffer As Byte(), ByVal PropertyBufferSize As UInteger, ByRef RequiredSize As UInteger) As Boolean
        End Function

        <DllImport("setupapi.dll", SetLastError:=True)>
        Private Shared Function SetupDiDestroyDeviceInfoList(ByVal DeviceInfoSet As IntPtr) As Boolean
        End Function

        <DllImport("Cam_Oak-D.dll", CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function OakDDevices() As Integer
        End Function

        <DllImport("Cam_Oak-D.dll", CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function OakDNextDevice() As IntPtr
        End Function

        Private jsonFileName As String
        Public Sub New(fileName As String)
            jsonFileName = fileName
        End Sub
        Public Function Load() As jsonShared.Settings
            Dim settings = New jsonShared.Settings()
            Dim fileInfo As New FileInfo(jsonFileName)
            If fileInfo.Exists Then
                Using streamReader As New StreamReader(jsonFileName)
                    Dim jsonSettings = streamReader.ReadToEnd()
                    If jsonSettings <> "" Then
                        streamReader.Close()
                        settings = JsonConvert.DeserializeObject(Of jsonShared.Settings)(jsonSettings)
                        If settings.algorithm = "" Then settings.algorithm = "AddWeighted_Basics"
                        settings = initialize(settings)
                    End If
                End Using
            Else
                settings = initialize(settings)
            End If

            Return settings
        End Function
        Public Function initialize(Settings As jsonShared.Settings) As jsonShared.Settings
            Dim usbList = USBenumeration()

            'Dim countOak = OakDDevices()
            'For i = 0 To countOak - 1
            '    Dim strPtr = OakDNextDevice()
            '    Dim productName As String = Marshal.PtrToStringAnsi(strPtr)
            '    If productName.StartsWith("OAK-D") Then Settings.OakIndex3D = i
            '    If productName.StartsWith("OAK-4") Then Settings.OakIndex4D = i
            'Next

            Settings.cameraPresent = New List(Of Boolean)
            For i = 0 To cameraNames.Count - 1
                Dim searchname = cameraNames(i)
                Dim present As Boolean = False
                'If searchname.StartsWith("Oak-3D") And countOak > 0 Then present = Settings.OakIndex3D >= 0
                'If searchname.StartsWith("Oak-4D") And countOak > 0 Then present = Settings.OakIndex4D >= 0
                If searchname.StartsWith("StereoLabs ZED 2/2i") Then searchname = "ZED 2"

                Dim subsetList As New List(Of String)
                For Each usbDevice In usbList
                    If usbDevice.Contains("Orb") Then subsetList.Add(usbDevice)
                    If usbDevice.Contains(searchname) Then
                        present = True
                        Exit For
                    End If
                Next
                Settings.cameraPresent.Add(present)
            Next

            Dim index = cameraNames.IndexOf(Settings.cameraName)
            If index < 0 Then
                Settings.cameraFound = False
                For i = 0 To Settings.cameraPresent.Count - 1
                    If Settings.cameraPresent(i) Then
                        Settings.cameraFound = True
                        If Settings.cameraName = Nothing Then Settings.cameraName = cameraNames(i)
                        Settings.captureRes = New cv.Size(1280, 720)
                        Exit For
                    End If
                Next
            End If

            If Settings.cameraFound = False Then
                MessageBox.Show("There are no supported cameras present!" + vbCrLf + vbCrLf)
            End If

            Save(Settings)

            Return Settings
        End Function
        ''' <summary>Enumerate PnP device names using SetupAPI (no WMI). Returns same style list as before for camera detection.</summary>
        Public Function USBenumeration() As List(Of String)
            Static usblist As New List(Of String)
            If usblist.Count > 0 Then Return usblist

            Dim deviceInfoSet = SetupDiGetClassDevs(IntPtr.Zero, Nothing, IntPtr.Zero, DIGCF_PRESENT Or DIGCF_ALLCLASSES)
            If deviceInfoSet = IntPtr.Zero OrElse deviceInfoSet = New IntPtr(-1) Then Return usblist

            Try
                Dim devInfo As SP_DEVINFO_DATA
                devInfo.cbSize = CUInt(Marshal.SizeOf(GetType(SP_DEVINFO_DATA)))
                devInfo.ClassGuid = Guid.Empty
                devInfo.DevInst = 0
                devInfo.Reserved = UIntPtr.Zero

                Dim memberIndex As UInteger = 0
                Const bufSize As Integer = 1024
                Dim buf As Byte() = New Byte(bufSize - 1) {}
                Dim reqSize As UInteger = 0
                Dim regType As UInteger = 0

                While SetupDiEnumDeviceInfo(deviceInfoSet, memberIndex, devInfo)
                    Dim name As String = Nothing
                    ' Prefer friendly name, fall back to device description (same as WMI Caption). CharSet.Auto = Unicode on Windows.
                    If SetupDiGetDeviceRegistryProperty(deviceInfoSet, devInfo, SPDRP_FRIENDLYNAME, regType, buf, bufSize, reqSize) AndAlso reqSize > 0 Then
                        name = Encoding.Unicode.GetString(buf, 0, CInt(Math.Min(reqSize, bufSize))).TrimEnd(Chr(0))
                    End If
                    If String.IsNullOrEmpty(name) AndAlso SetupDiGetDeviceRegistryProperty(deviceInfoSet, devInfo, SPDRP_DEVICEDESC, regType, buf, bufSize, reqSize) AndAlso reqSize > 0 Then
                        name = Encoding.Unicode.GetString(buf, 0, CInt(Math.Min(reqSize, bufSize))).TrimEnd(Chr(0))
                    End If
                    If name IsNot Nothing AndAlso name.Length > 0 Then
                        usblist.Add(name)
                        ' Same debug filtering as before: log names that might be new cameras
                        If InStr(name, "Xeon") Or InStr(name, "Chipset") Or InStr(name, "Generic") Or InStr(name, "Bluetooth") Or
                            InStr(name, "Monitor") Or InStr(name, "Mouse") Or InStr(name, "NVIDIA") Or InStr(name, "HID-compliant") Or
                            InStr(name, " CPU ") Or InStr(name, "PCI Express") Or name.StartsWith("USB ") Or
                            name.StartsWith("Microsoft") Or name.StartsWith("Motherboard") Or InStr(name, "SATA") Or
                            InStr(name, "Volume") Or name.StartsWith("WAN") Or InStr(name, "ACPI") Or
                            name.StartsWith("HID") Or InStr(name, "OneNote") Or name.StartsWith("Samsung") Or
                            name.StartsWith("System ") Or name.StartsWith("HP") Or InStr(name, "Wireless") Or
                            name.StartsWith("SanDisk") Or InStr(name, "Wi-Fi") Or name.StartsWith("Media ") Or
                            name.StartsWith("High precision") Or name.StartsWith("High Definition ") Or
                            InStr(name, "Remote") Or InStr(name, "Numeric") Or InStr(name, "UMBus ") Or
                            name.StartsWith("Plug or Play") Or InStr(name, "Print") Or name.StartsWith("Direct memory") Or
                            InStr(name, "interrupt controller") Or name.StartsWith("NVVHCI") Or name.StartsWith("Plug and Play") Or
                            name.StartsWith("ASMedia") Or name = "Fax" Or name.StartsWith("Speakers") Or
                            InStr(name, "Host Controller") Or InStr(name, "Management Engine") Or InStr(name, "Legacy") Or
                            name.StartsWith("NDIS") Or name.StartsWith("Logitech USB Input Device") Or
                            name.StartsWith("Simple Device") Or InStr(name, "Ethernet") Or name.StartsWith("WD ") Or
                            InStr(name, "Composite Bus Enumerator") Or InStr(name, "Turbo Boost") Or name.StartsWith("Realtek") Or
                            name.StartsWith("PCI-to-PCI") Or name.StartsWith("Network Controller") Or name.StartsWith("ATAPI ") Or
                            name.Contains("Gen Intel(R) ") Then
                        Else
                            Debug.WriteLine(name) ' looking for new cameras
                        End If
                    End If
                    devInfo.cbSize = CUInt(Marshal.SizeOf(GetType(SP_DEVINFO_DATA)))
                    memberIndex += 1
                End While
            Finally
                SetupDiDestroyDeviceInfoList(deviceInfoSet)
            End Try
            Return usblist
        End Function
        Public Sub Save(settings As jsonShared.Settings)
            If settings.allOptionsWidth = 0 Then settings.allOptionsWidth = 1000
            If settings.allOptionsHeight = 0 Then settings.allOptionsHeight = 500

            If settings.MainFormWidth = 0 Then settings.MainFormWidth = 500
            If settings.MainFormHeight = 0 Then settings.MainFormHeight = 350

            Select Case settings.workRes.Height
                Case 270, 540, 1080
                    settings.captureRes = New cv.Size(1920, 1080)
                Case 180, 360, 720
                    settings.captureRes = New cv.Size(1280, 720)
                Case 600
                    settings.captureRes = New cv.Size(960, 600)
                Case 376, 188, 94
                    If settings.cameraName = "StereoLabs ZED 2/2i" Then
                        settings.captureRes = New cv.Size(672, 376)
                    Else
                        settings.workRes = New cv.Size(320, 180)
                        settings.captureRes = New cv.Size(672, 376)
                    End If
                Case 120, 240, 480
                    settings.captureRes = New cv.Size(640, 480)
            End Select

            Select Case settings.workRes.Width
                Case 1920
                    settings.testAllDuration = 30
                Case 1280
                    settings.testAllDuration = 25
                Case 960
                    settings.testAllDuration = 20
                Case 672
                    settings.testAllDuration = 15
                Case 640
                    settings.testAllDuration = 12
                Case 480
                    settings.testAllDuration = 10
                Case 240, 336, 320, 168, 160
                    settings.testAllDuration = 5
            End Select

            settings.desiredFPS = 30
            If settings.cameraName.Contains("Orbbec") Then
                settings.desiredFPS = 0 ' maximum fps available at this resolution
            End If

            Select Case settings.captureRes.Width
                Case 1920
                    settings.desiredFPS = 15
                Case 1280
                    settings.desiredFPS = 30
                Case 960
                    settings.desiredFPS = 45
                Case 672
                    settings.desiredFPS = 100
                Case 640
                    settings.desiredFPS = 60
            End Select

            Try
                Using streamWriter As New StreamWriter(jsonFileName)
                    Dim serializer As New JsonSerializer With {.Formatting = Formatting.Indented}
                    serializer.Serialize(streamWriter, settings)
                End Using
            Catch ex As Exception
                ' Log error if needed, but don't throw
            End Try

            SaveSetting("OpenCVB", "SplashMessage", "SplashMessage", "Camera: " + settings.cameraName + vbCrLf +
                        "Algorithm: " + settings.algorithm)
        End Sub
    End Class
End Namespace

