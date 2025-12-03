Imports System.IO
Imports System.Management
Imports Newtonsoft.Json
Imports cv = OpenCvSharp

Namespace jsonClass
    Public Class ApplicationStorage
        Public algorithm As String

        Public cameraIndex As Integer
        Public cameraName As String
        Public cameraPresent As List(Of Boolean)
        Public cameraFound As Boolean
        Public resolutionsSupported As List(Of Boolean)
        Public cameraSupported As List(Of Boolean)
        Public camera640x480Support As List(Of Boolean)
        Public camera1920x1080Support As List(Of Boolean)

        Public locationPixelViewer As cv.Vec4f
        Public locationOpenGL As cv.Vec4f
        Public locationOptions As cv.Vec4f

        Public zedSDKready As Boolean
        Public oakDSDKready As Boolean

        Public snap640 As Boolean
        Public snap320 As Boolean
        Public snapCustom As Boolean

        Public workRes As cv.Size
        Public captureRes As cv.Size
        Public displayRes As New cv.Size(640, 360)

        Public showBatchConsole As Boolean

        Public treeButton As Boolean
        Public treeLocation As cv.Vec4f

        Public fontInfo As Font
        Public desiredFPS As Integer
    End Class
    Public Class jsonIO
        Public jsonFileName As String
        Public Sub Save(storageList As List(Of ApplicationStorage))
            Using streamWriter = File.CreateText(jsonFileName)
                Dim serializer = New JsonSerializer With {.Formatting = Formatting.Indented}
                serializer.Serialize(streamWriter, storageList)
            End Using
        End Sub
        Public Function Init() As List(Of ApplicationStorage)
            Dim fileInfo As New FileInfo(jsonFileName)
            If fileInfo.Exists Then
                Using streamReader = New StreamReader(jsonFileName)
                    Dim json = streamReader.ReadToEnd()
                    If json <> "" Then Return JsonConvert.DeserializeObject(Of List(Of ApplicationStorage))(json)
                End Using
            End If
            Dim empty As New List(Of ApplicationStorage)

            Dim emptyApp As New ApplicationStorage
            emptyApp.cameraName = ""
            emptyApp.cameraIndex = 0
            emptyApp.workRes = New cv.Size(320, 180)
            emptyApp.snap640 = True
            emptyApp.showBatchConsole = False
            emptyApp.treeButton = True
            emptyApp.treeLocation = New cv.Vec4f(20, 20, 500, 600)

            SaveSetting("OpenCVB", "gOptionsLeft", "gOptionsLeft", 10)
            SaveSetting("OpenCVB", "gOptionsTop", "gOptionsTop", 10)

            empty.Add(emptyApp)
            Return empty
        End Function
        Public Function read() As jsonClass.ApplicationStorage
            Dim settings = Init()(0)
            ' The camera names are defined in VBtask.algParms.cameraNames
            ' the 3 lists below must have an entry for each camera - supported/640x480/1920...
            '  cameraNames = New List(Of String)(VB_Classes.VBtask.algParms.cameraNames)  ' <<<<<<<<<<<< here is the list of supported cameras.
            With settings
                .cameraSupported = New List(Of Boolean)({True, True, True, True, True, False, True, True})
                .camera640x480Support = New List(Of Boolean)({False, True, True, False, False, False, True, True})
                .camera1920x1080Support = New List(Of Boolean)({True, False, False, False, True, False, False, False})
                Dim defines = New FileInfo(OpenCVB.MainUI.HomeDir.FullName + "Cameras\CameraDefines.hpp")
                Dim stereoLabsDefineIsOff As Boolean
                Dim sr = New StreamReader(defines.FullName)
                Dim zedIndex = Common.cameraNames.IndexOf("StereoLabs ZED 2/2i")
                While sr.EndOfStream = False
                    Dim nextLine = sr.ReadLine
                    If nextLine.Contains("STEREOLAB") Then
                        If nextLine.StartsWith("//") Then
                            .cameraSupported(zedIndex) = False
                            stereoLabsDefineIsOff = True
                        End If
                    End If
                End While

                sr.Close()

                ' checking the list for specific missing device here...
                Dim usbList = USBenumeration()
                Dim testlist As New List(Of String)
                For Each usbDevice In usbList
                    If LCase(usbDevice).Contains("orb") Then testlist.Add(usbDevice) ' debugging assistance...
                Next

                .cameraPresent = New List(Of Boolean)
                For i = 0 To Common.cameraNames.Count - 1
                    Dim searchname = Common.cameraNames(i)
                    Dim present As Boolean = False
                    If searchname.Contains("Oak-D") Then searchname = "Movidius MyriadX"
                    If stereoLabsDefineIsOff = False Then
                        If searchname.StartsWith("StereoLabs ZED 2/2i") Then searchname = "ZED 2"
                    End If

                    Dim subsetList As New List(Of String)
                    For Each usbDevice In usbList
                        If usbDevice.Contains("Orb") Then subsetList.Add(usbDevice)
                        If usbDevice.Contains(searchname) Then present = True
                    Next
                    .cameraPresent.Add(present <> False)
                Next

                For i = 0 To Common.cameraNames.Count - 1
                    If Common.cameraNames(i).StartsWith("Orbbec") Then
                        If Common.cameraNames(i) = .cameraName Then
                            .cameraIndex = i
                            Exit For
                        End If
                    Else
                        If Common.cameraNames(i).Contains(.cameraName) And .cameraName <> "" Then
                            .cameraIndex = i
                            Exit For
                        End If
                    End If
                Next

                If .cameraName = "" Or .cameraPresent(.cameraIndex) = False Then
                    For i = 0 To Common.cameraNames.Count - 1
                        If .cameraPresent(i) Then
                            .cameraIndex = i
                            .cameraName = Common.cameraNames(i)
                            Exit For
                        End If
                    Next
                Else
                    For i = 0 To Common.cameraNames.Count - 1
                        If Common.cameraNames(i) = .cameraName Then .cameraIndex = i
                    Next
                End If

                If .cameraPresent(zedIndex) And .cameraSupported(zedIndex) = False And stereoLabsDefineIsOff = False Then
                    MessageBox.Show("A StereoLabls ZED 2 camera is present but OpenCVB's" + vbCrLf +
                       "Cam_Zed2.dll has not been built with the SDK." + vbCrLf + vbCrLf +
                       "Edit " + OpenCVB.MainUI.HomeDir.FullName + "CameraDefines.hpp to add support" + vbCrLf +
                       "and rerun Update_All.bat to get the StereoLabs SDK.")
                End If

                settings.cameraFound = False
                For i = 0 To settings.cameraPresent.Count - 1
                    If settings.cameraPresent(i) Then
                        settings.cameraFound = True
                        Exit For
                    End If
                Next
                If settings.cameraFound = False Then
                    settings.cameraName = ""
                    MessageBox.Show("There are no supported cameras present!" + vbCrLf + vbCrLf)
                End If

                If settings.fontInfo Is Nothing Then settings.fontInfo = New Font("Tahoma", 9)

                Select Case .workRes.Height
                    Case 270, 540, 1080
                        .captureRes = New cv.Size(1920, 1080)
                        If .camera1920x1080Support(.cameraIndex) = False Then
                            .captureRes = New cv.Size(1280, 720)
                            .workRes = New cv.Size(320, 180)
                        End If
                    Case 180, 360, 720
                        .captureRes = New cv.Size(1280, 720)
                    Case 376, 188, 94
                        If .cameraName <> "StereoLabs ZED 2/2i" Then
                            MessageBox.Show("The json settings don't appear to be correct!" + vbCrLf +
                                    "The 'settings.json' file will be removed" + vbCrLf +
                                    "and rebuilt with default settings upon restart.")
                            Dim fileinfo As New FileInfo(OpenCVB.MainUI.jsonfs.jsonFileName)
                            fileinfo.Delete()
                            End
                        End If
                        .captureRes = New cv.Size(672, 376)
                    Case 120, 240, 480
                        .captureRes = New cv.Size(640, 480)
                        If .camera640x480Support(.cameraIndex) = False Then
                            .captureRes = New cv.Size(1280, 720)
                            .workRes = New cv.Size(320, 180)
                        End If
                End Select

                If .fontInfo Is Nothing Then .fontInfo = New Font("Tahoma", 9)
                .desiredFPS = 60
            End With

            Return settings
        End Function
        Public Sub write()
            If OpenCVB.MainUI.TestAllButton.Text <> "Stop Test" Then ' don't save the algorithm name and group if "Test All" is running.
                OpenCVB.MainUI.settings.algorithm = OpenCVB.MainUI.AvailableAlgorithms.Text
            End If

            If OpenCVB.MainUI.AvailableAlgorithms IsNot Nothing Then
                If OpenCVB.MainUI.AvailableAlgorithms.Items.Count > 0 And OpenCVB.MainUI.settings.algorithm = "" Then
                    OpenCVB.MainUI.settings.algorithm = OpenCVB.MainUI.AvailableAlgorithms.Items(0)
                End If
            End If
            If OpenCVB.MainUI.camPic(0) IsNot Nothing Then
                ' used only when .snapCustom is true
                OpenCVB.MainUI.settings.displayRes = New cv.Size(OpenCVB.MainUI.camPic(0).Width, OpenCVB.MainUI.camPic(0).Height)
            End If

            Dim setlist = New List(Of jsonClass.ApplicationStorage)
            setlist.Add(OpenCVB.MainUI.settings)
            Save(setlist)
        End Sub
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
    End Class
End Namespace