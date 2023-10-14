Imports System.Threading
Imports cv = OpenCvSharp
Public Class Threading_Test : Inherits VB_Algorithm
    Dim addW As New AddWeighted_Basics
    Dim thread As System.Threading.Thread
    Public Sub New()
        desc = "Test using the threading in VB.Net"
    End Sub
    Private Sub runThread()
        While 1
            If task.srcThread IsNot Nothing Then
                addW.Run(task.srcThread)
                task.srcThread = Nothing
            Else
                Thread.Sleep(10)
            End If
        End While
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If thread Is Nothing Then
            thread = New System.Threading.Thread(AddressOf runThread)
            thread.Name = "Threading_Test"
            thread.Start()
        End If

        If task.srcThread Is Nothing Then
            dst2 = addW.dst2
            task.srcThread = src
        End If
    End Sub
    Public Sub Close()
        thread.Abort()
    End Sub
End Class

