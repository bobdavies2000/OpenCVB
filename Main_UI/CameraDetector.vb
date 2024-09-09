Imports System.Management
Imports System.Runtime.InteropServices
Imports System.Threading

Public Class CameraDetector
    Private Const WM_DEVICECHANGE As Integer = &H219
    Private Const DBT_DEVICEARRIVAL As Integer = &H8000
    Private Const DBT_DEVICEREMOVECOMPLETE As Integer = &H8004
    Private Const DBT_DEVTYP_DEVICEINTERFACE As Integer = &H5
    Private Const deviceChanging As Integer = &H7 ' what is the official name for this...

    <StructLayout(LayoutKind.Sequential)>
    Private Structure DEV_BROADCAST_DEVICEINTERFACE
        Public dbcc_size As Integer
        Public dbcc_devicetype As Integer
        Public dbcc_reserved As Integer
        Public dbcc_classguid As Guid
        Public dbcc_name As Char
    End Structure

    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Private Shared Function RegisterDeviceNotification(hRecipient As IntPtr, NotificationFilter As IntPtr, Flags As UInteger) As IntPtr
    End Function

    Private Class MessageWindow
        Inherits NativeWindow

        Private stopRequested As Boolean = False

        Public Sub New()
            Dim cp As New CreateParams()
            Me.CreateHandle(cp)
        End Sub
        Private Sub CheckForCamera()
            Dim searcher As New ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'Image'")
            For Each device As ManagementObject In searcher.Get()
                Console.WriteLine("Camera detected: " & device("Name"))
            Next
        End Sub

        Protected Overrides Sub WndProc(ByRef m As Message)
            If m.Msg = WM_DEVICECHANGE Then
                Select Case CInt(m.WParam)
                    Case DBT_DEVICEARRIVAL
                        Debug.WriteLine("Device attached.")
                        Main_UI.DevicesChanged = True
                        'CheckForCamera()
                    Case DBT_DEVICEREMOVECOMPLETE
                        Debug.WriteLine("Device removed.")
                    Case deviceChanging
                End Select
            End If
            MyBase.WndProc(m)
        End Sub

        Public Sub RequestStop()
            stopRequested = True
        End Sub

        Public Sub RunMessageLoop()
            While Not stopRequested
                Application.DoEvents()
                Thread.Sleep(100) ' Adjust the sleep time as needed
            End While
        End Sub
    End Class

    Dim messageWindow1 As MessageWindow
    Dim messageThread As Thread

    Public Sub New()
        messageWindow1 = New MessageWindow()

        Dim dbi As New DEV_BROADCAST_DEVICEINTERFACE()
        dbi.dbcc_size = Marshal.SizeOf(dbi)
        dbi.dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE
        dbi.dbcc_reserved = 0
        dbi.dbcc_classguid = New Guid("E5323777-F976-4f5b-9B55-B94699C46E44") ' GUID_DEVINTERFACE_IMAGE

        Dim buffer As IntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(dbi))
        Marshal.StructureToPtr(dbi, buffer, True)

        RegisterDeviceNotification(messageWindow1.Handle, buffer, 0)
    End Sub

    <STAThread>
    Public Sub StartDetector()
        messageThread = New Thread(AddressOf messageWindow1.RunMessageLoop)
        messageThread.SetApartmentState(ApartmentState.STA)
        messageThread.Name = "Camera Detector"
        messageThread.Start()
    End Sub

    Public Sub StopDetector()
        messageWindow1.RequestStop()
        messageThread.Join()
    End Sub
End Class