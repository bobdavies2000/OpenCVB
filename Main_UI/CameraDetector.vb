Imports System.Runtime.InteropServices
Imports Intel.RealSense
Public Class CameraDetector
    Private Const WM_DEVICECHANGE As Integer = &H219
    Private Const DBT_DEVICEARRIVAL As Integer = &H8000
    Private Const DBT_DEVICEREMOVECOMPLETE As Integer = &H8004
    Private Const DBT_DEVTYP_DEVICEINTERFACE As Integer = &H5
    Private Const DBT_DEVNODES_CHANGED As Integer = 7

    <StructLayout(LayoutKind.Sequential)>
    Private Structure DEV_BROADCAST_DEVICEINTERFACE
        Public dbcc_size As Integer
        Public dbcc_devicetype As Integer
        Public dbcc_reserved As Integer
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=16)>
        Public dbcc_classguid As Byte()
        Public dbcc_name As Char
    End Structure

    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Private Shared Function RegisterDeviceNotification(hRecipient As IntPtr, NotificationFilter As IntPtr, Flags As UInteger) As IntPtr
    End Function

    Private Class MessageWindow
        Inherits NativeWindow

        Public Sub New()
            Dim cp As New CreateParams()
            Me.CreateHandle(cp)
        End Sub

        Protected Overrides Sub WndProc(ByRef m As Message)
            'If m.Msg = 799 Then Main_UI.DevicesStart = True
            'If m.Msg = WM_DEVICECHANGE Then
            '    Main_UI.DevicesChanged = True
            '    'Select Case CInt(m.WParam)
            '    '    Case DBT_DEVNODES_CHANGED
            '    'End Select
            'End If
            MyBase.WndProc(m)
        End Sub
    End Class

    Private messageWindow1 As MessageWindow

    Public Sub New()
        messageWindow1 = New MessageWindow()

        Dim dbi As New DEV_BROADCAST_DEVICEINTERFACE()
        dbi.dbcc_size = Marshal.SizeOf(dbi)
        dbi.dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE
        dbi.dbcc_reserved = 0
        dbi.dbcc_classguid = Guid.NewGuid().ToByteArray()
        Dim buffer As IntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(dbi))
        Marshal.StructureToPtr(dbi, buffer, True)

        RegisterDeviceNotification(messageWindow1.Handle, buffer, 0)
    End Sub

    Public Sub Start()
        Application.Run()
    End Sub
End Class