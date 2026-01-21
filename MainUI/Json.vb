Imports System.IO
Imports System.Management
Imports System.Runtime.InteropServices
Imports jsonShared
Imports Newtonsoft.Json
Imports cv = OpenCvSharp
Namespace MainApp
    Public Class jsonIO
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
            Settings.cameraPresent = New List(Of Boolean)
            For i = 0 To cameraNames.Count - 1
                Dim searchname = cameraNames(i)
                Dim present As Boolean = False
                If searchname.StartsWith("Oak-3D") Then searchname = "Movidius"
                If searchname.StartsWith("Oak-4D") Then searchname = "OAK4-D"
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
                        Exit For
                    End If
                Next
            End If

            If Settings.cameraFound = False Then
                MessageBox.Show("There are no supported cameras present!" + vbCrLf + vbCrLf)
            End If

            Return Settings
        End Function
        Public Function USBenumeration() As List(Of String)
            Static usblist As New List(Of String)
            Dim info As ManagementObject
            Dim search As ManagementObjectSearcher
            search = New ManagementObjectSearcher("SELECT * From Win32_PnPEntity")
            If usblist.Count = 0 Then
                For Each info In search.Get()
                    Dim Name = CType(info("Caption"), String)
                    If Name IsNot Nothing Then
                        usblist.Add(Name)
                        ' This enumeration can tell us about the cameras present.  Built on first pass.
                        If InStr(Name, "Xeon") Or InStr(Name, "Chipset") Or InStr(Name, "Generic") Or InStr(Name, "Bluetooth") Or
                            InStr(Name, "Monitor") Or InStr(Name, "Mouse") Or InStr(Name, "NVIDIA") Or InStr(Name, "HID-compliant") Or
                            InStr(Name, " CPU ") Or InStr(Name, "PCI Express") Or Name.StartsWith("USB ") Or
                            Name.StartsWith("Microsoft") Or Name.StartsWith("Motherboard") Or InStr(Name, "SATA") Or
                            InStr(Name, "Volume") Or Name.StartsWith("WAN") Or InStr(Name, "ACPI") Or
                            Name.StartsWith("HID") Or InStr(Name, "OneNote") Or Name.StartsWith("Samsung") Or
                            Name.StartsWith("System ") Or Name.StartsWith("HP") Or InStr(Name, "Wireless") Or
                            Name.StartsWith("SanDisk") Or InStr(Name, "Wi-Fi") Or Name.StartsWith("Media ") Or
                            Name.StartsWith("High precision") Or Name.StartsWith("High Definition ") Or
                            InStr(Name, "Remote") Or InStr(Name, "Numeric") Or InStr(Name, "UMBus ") Or
                            Name.StartsWith("Plug or Play") Or InStr(Name, "Print") Or Name.StartsWith("Direct memory") Or
                            InStr(Name, "interrupt controller") Or Name.StartsWith("NVVHCI") Or Name.StartsWith("Plug and Play") Or
                            Name.StartsWith("ASMedia") Or Name = "Fax" Or Name.StartsWith("Speakers") Or
                            InStr(Name, "Host Controller") Or InStr(Name, "Management Engine") Or InStr(Name, "Legacy") Or
                            Name.StartsWith("NDIS") Or Name.StartsWith("Logitech USB Input Device") Or
                            Name.StartsWith("Simple Device") Or InStr(Name, "Ethernet") Or Name.StartsWith("WD ") Or
                            InStr(Name, "Composite Bus Enumerator") Or InStr(Name, "Turbo Boost") Or Name.StartsWith("Realtek") Or
                            Name.StartsWith("PCI-to-PCI") Or Name.StartsWith("Network Controller") Or Name.StartsWith("ATAPI ") Or
                            Name.Contains("Gen Intel(R) ") Then
                        Else
                            Debug.WriteLine(Name) ' looking for new cameras 
                        End If
                    End If
                Next
            End If
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

