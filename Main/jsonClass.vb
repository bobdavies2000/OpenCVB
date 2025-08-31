Imports System.IO
Imports System.Management
Imports System.Runtime
Imports Newtonsoft.Json
Imports VBClasses
Imports cv = OpenCvSharp

Namespace jsonClass

    Public Class ApplicationStorage
        Public MainUI_AlgName As String
        Public groupComboText As String

        Public cameraIndex As Integer
        Public cameraName As String
        Public cameraPresent As List(Of Boolean)
        Public cameraFound As Boolean
        Public resolutionsSupported As List(Of Boolean)
        Public cameraSupported As List(Of Boolean)
        Public camera640x480Support As List(Of Boolean)
        Public camera1920x1080Support As List(Of Boolean)

        Public locationMain As cv.Vec4f
        Public locationPixelViewer As cv.Vec4f
        Public locationOpenGL As cv.Vec4f
        Public locationOptions As cv.Vec4f

        Public myntSDKready As Boolean
        Public zedSDKready As Boolean
        Public oakDSDKready As Boolean

        Public snap640 As Boolean
        Public snap320 As Boolean
        Public snapCustom As Boolean

        Public workRes As cv.Size
        Public workResIndex As Integer
        Public captureRes As cv.Size
        Public displayRes As cv.Size

        Public testAllDuration As Integer
        Public showConsoleLog As Boolean

        Public treeButton As Boolean
        Public treeLocation As cv.Vec4f

        Public fontInfo As Font
        Public desiredFPS As Integer
        Public translatorMode As String
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
            emptyApp.testAllDuration = 5
            emptyApp.showConsoleLog = False
            emptyApp.treeButton = True
            emptyApp.treeLocation = New cv.Vec4f(20, 20, 500, 600)
            emptyApp.groupComboText = "< All >"
            emptyApp.translatorMode = "VB.Net to C#"

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
                Dim defines = New FileInfo(MyApp.UI.Main.HomeDir.FullName + "Cameras\CameraDefines.hpp")
                Dim stereoLabsDefineIsOff As Boolean
                Dim sr = New StreamReader(defines.FullName)
                Dim zedIndex = Comm.cameraNames.IndexOf("StereoLabs ZED 2/2i")
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
                For i = 0 To Comm.cameraNames.Count - 1
                    Dim searchname = Comm.cameraNames(i)
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

                For i = 0 To Comm.cameraNames.Count - 1
                    If Comm.cameraNames(i).StartsWith("Orbbec") Then
                        If Comm.cameraNames(i) = .cameraName Then
                            .cameraIndex = i
                            Exit For
                        End If
                    Else
                        If Comm.cameraNames(i).Contains(.cameraName) And .cameraName <> "" Then
                            .cameraIndex = i
                            Exit For
                        End If
                    End If
                Next

                If .cameraName = "" Or .cameraPresent(.cameraIndex) = False Then
                    For i = 0 To Comm.cameraNames.Count - 1
                        If .cameraPresent(i) Then
                            .cameraIndex = i
                            .cameraName = Comm.cameraNames(i)
                            Exit For
                        End If
                    Next
                Else
                    For i = 0 To Comm.cameraNames.Count - 1
                        If Comm.cameraNames(i) = .cameraName Then .cameraIndex = i
                    Next
                End If

                If .cameraPresent(zedIndex) And .cameraSupported(zedIndex) = False And stereoLabsDefineIsOff = False Then
                    MessageBox.Show("A StereoLabls ZED 2 camera is present but OpenCVB's" + vbCrLf +
                       "Cam_Zed2.dll has not been built with the SDK." + vbCrLf + vbCrLf +
                       "Edit " + MyApp.UI.Main.HomeDir.FullName + "CameraDefines.hpp to add support" + vbCrLf +
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

                If settings.testAllDuration < 5 Then settings.testAllDuration = 5
                If settings.fontInfo Is Nothing Then settings.fontInfo = New Font("Tahoma", 9)

                If .workRes = New cv.Size Then .workRes = New cv.Size(640, 480)
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
                        If settings.cameraName <> "StereoLabs ZED 2/2i" Then
                            MessageBox.Show("The json settings don't appear to be correct!" + vbCrLf +
                                "The 'settings.json' file will be removed" + vbCrLf +
                                "and rebuilt with default settings upon restart.")
                            Dim fileinfo As New FileInfo(MyApp.UI.Main.jsonfs.jsonFileName)
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

                Dim wh = .workRes.Height
                ' desktop style is the default
                If .snap320 = False And .snap640 = False And .snapCustom = False Then .snap640 = True
                If .snap640 Then
                    .locationMain.Item2 = 1321
                    .locationMain.Item3 = 845
                    If wh = 240 Or wh = 480 Or wh = 120 Then .locationMain.Item3 = 1096
                    If wh = 240 Or wh = 480 Or wh = 120 Then .displayRes = New cv.Size(640, 480) Else .displayRes = New cv.Size(640, 360)
                ElseIf .snap320 Then
                    .locationMain.Item2 = 683
                    .locationMain.Item3 = 510
                    If wh = 240 Or wh = 480 Or wh = 120 Then .locationMain.Item3 = 616
                    If wh = 240 Or wh = 480 Or wh = 120 Then .displayRes = New cv.Size(320, 240) Else .displayRes = New cv.Size(320, 180)
                End If

                Dim border As Integer = 6
                Dim defaultWidth = .workRes.Width * 2 + border * 7
                Dim defaultHeight = .workRes.Height * 2 + MyApp.UI.Main.ToolStrip1.Height + border * 12
                If MyApp.UI.Main.Height < 50 Then
                    MyApp.UI.Main.Width = defaultWidth
                    MyApp.UI.Main.Height = defaultHeight
                End If

                If .fontInfo Is Nothing Then .fontInfo = New Font("Tahoma", 9)
                If settings.groupComboText = "" Then settings.groupComboText = "< All >"

                If MyApp.UI.Main.testAllRunning = False Then
                    Dim resStr = CStr(.workRes.Width) + "x" + CStr(.workRes.Height)
                    For i = 0 To Comm.resolutionList.Count - 1
                        If Comm.resolutionList(i).StartsWith(resStr) Then
                            .workResIndex = i
                            Exit For
                        End If
                    Next
                End If

                .desiredFPS = 60
                MyApp.UI.Main.Left = .locationMain.Item0
                MyApp.UI.Main.Top = .locationMain.Item1
                MyApp.UI.Main.Width = .locationMain.Item2
                MyApp.UI.Main.Height = .locationMain.Item3
            End With
            Return settings
        End Function
        Public Sub write()
            If MyApp.UI.Main.TestAllButton.Text <> "Stop Test" Then ' don't save the algorithm name and group if "Test All" is running.
                MyApp.UI.Main.settings.MainUI_AlgName = MyApp.UI.Main.AvailableAlgorithms.Text
                MyApp.UI.Main.settings.groupComboText = MyApp.UI.Main.GroupComboBox.Text
            End If

            MyApp.UI.Main.settings.locationMain = New cv.Vec4f(MyApp.UI.Main.Left, MyApp.UI.Main.Top,
                                                               MyApp.UI.Main.Width, MyApp.UI.Main.Height)
            If MyApp.UI.Main.camPic(0) IsNot Nothing Then
                ' used only when .snapCustom is true
                MyApp.UI.Main.settings.displayRes = New cv.Size(MyApp.UI.Main.camPic(0).Width, MyApp.UI.Main.camPic(0).Height)
            End If
            If MyApp.UI.Main.settings.translatorMode = "" Then MyApp.UI.Main.settings.translatorMode = "VB.Net to C#"

            Dim setlist = New List(Of jsonClass.ApplicationStorage)
            setlist.Add(MyApp.UI.Main.settings)
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