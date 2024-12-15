Imports System.IO
Imports System.Runtime.InteropServices
Module Test
    <DllImport(("TestLibrary.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub TestLibrary_RunCPP()
    End Sub
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
        updatePath(homedir.FullName + "opencv\Build\bin\Release\", "OpenCV and OpenCV Contrib are needed for C++ classes.")
        updatePath(homedir.FullName + "opencv\Build\bin\Debug\", "OpenCV and OpenCV Contrib are needed for C++ classes.")
        TestLibrary_RunCPP()
    End Sub
End Class
