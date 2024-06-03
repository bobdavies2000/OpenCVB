Imports System.Threading
Imports cv = OpenCvSharp
Public Class Threading_Test : Inherits VB_Parent
    Dim thread1 As System.Threading.Thread
    Dim thread2 As System.Threading.Thread
    Dim horizon As New Horizon_Basics
    Dim gravity As New Gravity_Basics
    Public Sub New()
        task.recordTimings = False
        labels = {"", "", "Output of thread 1 - horizon thread", "Output of thread 2 - gravity thread"}
        desc = "Test using the threading in VB.Net"
    End Sub
    Private Sub runThread(id As Integer)
        While 1
            If task.srcThread IsNot Nothing Then
                If id = 1 Then
                    horizon.autoDisplay = True
                    horizon.Run(task.srcThread)
                    dst2 = horizon.dst2
                Else
                    gravity.autoDisplay = True
                    gravity.Run(task.srcThread)
                    dst3 = gravity.dst2
                End If
                task.srcThread = Nothing
            Else
                Thread.Sleep(10)
            End If
        End While
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If thread1 Is Nothing Then
            thread1 = New System.Threading.Thread(AddressOf runThread)
            thread1.Name = "Threading_Test1"
            thread1.Start(1)
        End If

        If thread2 Is Nothing Then
            thread2 = New System.Threading.Thread(AddressOf runThread)
            thread2.Name = "Threading_Test2"
            thread2.Start(2)
        End If

        If task.srcThread Is Nothing Then task.srcThread = src.Clone
    End Sub
    Public Sub Close()
        thread1.Abort()
        thread2.Abort()
    End Sub
End Class





Public Class Threading_Test1 : Inherits VB_Parent
    Dim gravity As New Gravity_Basics
    Dim thread As System.Threading.Thread
    Public Sub New()
        ' if there are options, this approach won't work - no cross-thread references are allowed.
        desc = "Test using the threading in VB.Net - this test works because there are no options with Gravity_Basics."
    End Sub
    Private Sub runThread()
        While 1
            If task.srcThread IsNot Nothing Then
                gravity.autoDisplay = True
                gravity.Run(task.srcThread)
                dst2 = gravity.dst2
                task.srcThread = Nothing
            Else
                Thread.Sleep(10)
            End If
        End While
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If thread Is Nothing Then
            thread = New System.Threading.Thread(AddressOf runThread)
            thread.Name = "Threading_Test"
            thread.Start()
        End If

        If task.srcThread Is Nothing Then task.srcThread = src.Clone
    End Sub
    Public Sub Close()
        thread.Abort()
    End Sub
End Class
