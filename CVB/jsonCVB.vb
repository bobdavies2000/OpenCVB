Imports System.IO
Imports System.Management
Imports Main
Imports Newtonsoft.Json
Imports cv = OpenCvSharp

Namespace CVB
    Public Class jsonCVB
        Public cameraIndex As Integer
        Public cameraName As String = "StereoLabs ZED 2/2I"
        Public cameraPresent As List(Of Boolean)
        Public cameraFound As Boolean
        Public resolutionsSupported As List(Of Boolean)
        Public cameraSupported As List(Of Boolean)
        Public camera640x480Support As List(Of Boolean)
        Public camera1920x1080Support As List(Of Boolean)

        Public FormLeft As Integer = 0
        Public FormTop As Integer = 0
        Public FormWidth As Integer = 1867
        Public FormHeight As Integer = 1134
        Public algorithm As String

        Public workRes As New cv.Size(336, 188)
        Public captureRes As New cv.Size(672, 376)
        Public displayRes As Size

        Public locationPixelViewer As cv.Vec4f
        Public locationOpenGL As cv.Vec4f
        Public locationOptions As cv.Vec4f

        Public zedSDKready As Boolean
        Public oakDSDKready As Boolean

        Public showBatchConsole As Boolean

        Public treeLocation As cv.Vec4f

        Public fontInfo As Font
        Public desiredFPS As Integer
        Public testAllDuration As Integer
    End Class

    Public Class jsonCVBIO
        Private jsonFileName As String
        Public Sub New(fileName As String)
            jsonFileName = fileName
        End Sub

        Public Function Load() As jsonCVB
            Dim settings As New jsonCVB()
            Dim fileInfo As New FileInfo(jsonFileName)
            Dim homeDir = fileInfo.DirectoryName + "/../"
            If fileInfo.Exists Then
                Try
                    Using streamReader As New StreamReader(jsonFileName)
                        Dim json = streamReader.ReadToEnd()
                        If json <> "" Then
                            settings = JsonConvert.DeserializeObject(Of jsonCVB)(json)
                            settings = initialize(settings, homeDir)
                        End If
                    End Using

                    Select Case settings.captureRes.Width
                        Case 640
                            settings.displayRes = New Size(320, 240)
                        Case Else
                            settings.displayRes = New Size(336, 188)
                    End Select

                Catch ex As Exception
                    ' If deserialization fails, return default settings
                End Try
            Else
                settings = initialize(settings, homeDir)
            End If

            Return settings
        End Function
        Public Function initialize(ByRef settings As jsonCVB, homeDir As String) As jsonCVB
            settings.cameraSupported = New List(Of Boolean)({True, True, True, True, True, False, True, True})
            settings.camera640x480Support = New List(Of Boolean)({False, True, True, False, False, False, True, True})
            settings.camera1920x1080Support = New List(Of Boolean)({True, False, False, False, True, False, False, False})
            Dim cameraNames = Common.cameraNames

            ' checking the list for specific missing device here...
            Dim usbList = USBenumeration()
            settings.cameraPresent = New List(Of Boolean)
            For i = 0 To cameraNames.Count - 1
                Dim searchname = cameraNames(i)
                Dim present As Boolean = False
                If searchname.Contains("Oak-D") Then searchname = "Movidius MyriadX"
                If searchname.StartsWith("StereoLabs ZED 2/2I") Then searchname = "ZED 2"

                Dim subsetList As New List(Of String)
                For Each usbDevice In usbList
                    If usbDevice.Contains("Orb") Then subsetList.Add(usbDevice)
                    If usbDevice.Contains(searchname) Then present = True
                Next
                settings.cameraPresent.Add(present <> False)
            Next

            If settings.cameraName = "" Or settings.cameraPresent(settings.cameraIndex) = False Then
                For i = 0 To cameraNames.Count - 1
                    If settings.cameraPresent(i) Then
                        settings.cameraIndex = i
                        settings.cameraName = cameraNames(i)
                        Exit For
                    End If
                Next
            Else
                For i = 0 To cameraNames.Count - 1
                    If cameraNames(i) = settings.cameraName Then settings.cameraIndex = i
                Next
            End If

            For i = 0 To Common.cameraNames.Count - 1
                If cameraNames(i).StartsWith("Orbbec") Then
                    If cameraNames(i) = settings.cameraName Then
                        settings.cameraIndex = i
                        Exit For
                    End If
                Else
                    If cameraNames(i).Contains(settings.cameraName) And settings.cameraName <> "" Then
                        settings.cameraIndex = i
                        Exit For
                    End If
                End If
            Next


            settings.cameraFound = False
            For i = 0 To settings.cameraPresent.Count - 1
                If settings.cameraPresent(i) Then
                    settings.cameraFound = True
                    If settings.cameraName = Nothing Then settings.cameraName = cameraNames(i)
                    Exit For
                End If
            Next
            If settings.cameraFound = False Then
                settings.cameraName = ""
                MessageBox.Show("There are no supported cameras present!" + vbCrLf + vbCrLf)
            End If

            If settings.fontInfo Is Nothing Then settings.fontInfo = New Font("Tahoma", 9)

            Select Case settings.workRes.Height
                Case 270, 540, 1080
                    settings.captureRes = New cv.Size(1920, 1080)
                    If settings.camera1920x1080Support(settings.cameraIndex) = False Then
                        settings.captureRes = New cv.Size(1280, 720)
                        settings.workRes = New cv.Size(320, 180)
                    End If
                Case 180, 360, 720
                    settings.captureRes = New cv.Size(1280, 720)
                Case 376, 188, 94
                    If settings.cameraName <> "StereoLabs ZED 2/2I" Then
                        MessageBox.Show("The json settings don't appear to be correct!" + vbCrLf +
                                        "The 'settings.json' file should be deleted" + vbCrLf +
                                        "and rebuilt with default settings upon restart.")
                    End If
                    settings.captureRes = New cv.Size(672, 376)
                Case 120, 240, 480
                    settings.captureRes = New cv.Size(640, 480)
                    If settings.camera640x480Support(settings.cameraIndex) = False Then
                        settings.captureRes = New cv.Size(1280, 720)
                        settings.workRes = New cv.Size(320, 180)
                    End If
        End Select

        If settings.fontInfo Is Nothing Then settings.fontInfo = New Font("Tahoma", 9)
            settings.desiredFPS = 60
            settings.testAllDuration = 5
            Select Case settings.workRes.Width
        Case 1920
                    settings.testAllDuration = 40
                Case 1280
                    settings.testAllDuration = 35
                Case 960
                    settings.testAllDuration = 30
                Case 672
                    settings.testAllDuration = 15
                Case 640
                    settings.testAllDuration = 15
                Case 480
                    settings.testAllDuration = 10
                Case 240, 336, 320, 168, 160
                    settings.testAllDuration = 5
            End Select

        Return settings
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

        Public Sub Save(settings As jsonCVB)
            Try
                Using streamWriter As New StreamWriter(jsonFileName)
                    Dim serializer As New JsonSerializer With {.Formatting = Formatting.Indented}
                    serializer.Serialize(streamWriter, settings)
                End Using
            Catch ex As Exception
                ' Log error if needed, but don't throw
            End Try
        End Sub
    End Class
End Namespace

