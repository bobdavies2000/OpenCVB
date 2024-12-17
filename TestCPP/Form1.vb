Imports System.IO
Imports System.Runtime.InteropServices
Module Test
    <DllImport(("TestLib.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub TestLib_Simple(dataPtr As IntPtr, rows As Integer, cols As Integer)
    End Sub

    <DllImport(("TestLib.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub TestLib_LoadImage()
    End Sub


    <DllImport(("TestLib.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function TestLib_Open() As IntPtr
    End Function
    <DllImport(("TestLib.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function TestLib_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("TestLib.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function TestLib_Rects(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("TestLib.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function TestLib_Sizes(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("TestLib.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function TestLib_FloodPoints(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("TestLib.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function TestLib_Count(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("TestLib.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function TestLib_Run(cPtr As IntPtr, dataPtr As IntPtr, maskPtr As IntPtr,
                                 rows As Integer, cols As Integer) As IntPtr
    End Function
End Module
Public Class Form1
    Private Sub updatePath(neededDirectory As String, notFoundMessage As String)
        Dim systemPath = Environment.GetEnvironmentVariable("Path")
        Dim foundDirectory As Boolean
        If Directory.Exists(neededDirectory) Then
            foundDirectory = True
            systemPath = neededDirectory + ";" + systemPath
        End If

        If foundDirectory = False And notFoundMessage.Length > 0 Then
            MsgBox(neededDirectory + " was not found.  " + notFoundMessage)
        End If
        Environment.SetEnvironmentVariable("Path", systemPath)
    End Sub
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim homedir = New FileInfo("C:\_src\OpenCVB\")
        updatePath(homedir.FullName + "opencv\Build\install\x64\vc17\bin", "OpenCV and OpenCV Contrib are needed for C++ classes.")
        updatePath(homedir.FullName + "bin", "test library dll")
        TestLib_LoadImage()

    End Sub
End Class
