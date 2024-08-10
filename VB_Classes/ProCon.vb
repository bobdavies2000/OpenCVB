Imports cv = OpenCvSharp
Imports System.Threading
' https://www.codeproject.com/Articles/5280034/Generation-of-Infinite-Sequences-in-Csharp-and-Unm
Public Class ProCon_Basics : Inherits VB_Parent
    Public mutex = New Mutex(True, "BufferMutex")
    Public p As Thread
    Public c As Thread
    Public head = -1
    Public tail = -1
    Public frameCount = 1
    Public flow As New Font_FlowText
    Public terminateConsumer As Boolean
    Public terminateProducer As Boolean
    Public options As New Options_ProCon
    Public Sub New()
        flow.parentData = Me
        p = New Thread(AddressOf Producer)
        p.Name = "Producer"
        p.Start()

        c = New Thread(AddressOf Consumer)
        c.Name = "Consumer"
        c.Start()

        desc = "DijKstra's Producer/Consumer 'Cooperating Sequential Process'.  Consumer must see every item produced."
    End Sub
    Public Function success(index As Integer) As Integer
        Return (index + 1) Mod options.buffer.Length
    End Function
    Public Sub Consumer()
        While 1
            If task.frameCount < 0 Then Exit While
            SyncLock mutex
                head = success(head)
                Dim item = options.buffer(head)
                If item <> -1 Then
                    flow.nextMsg = "Consumer: = " + CStr(item)
                    options.buffer(head) = -1
                End If
            End SyncLock
            If terminateConsumer Then Exit While
            Windows.Forms.Application.DoEvents()
        End While
    End Sub
    Private Sub Producer()
        While 1
            If task.frameCount < 0 Then Exit While
            SyncLock mutex
                tail = success(tail)
                If options.buffer(tail) = -1 Then
                    flow.nextMsg = "producer: = " + CStr(frameCount)
                    options.buffer(tail) = frameCount
                    frameCount += 1
                End If
            End SyncLock
            If terminateProducer Then Exit While
            Windows.Forms.Application.DoEvents()
        End While
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.testAllRunning Then
            SetTrueText("ProCon_Basics causes problems when running test all. " + vbCrLf + "Skipping for now...")
            Exit Sub
        End If
        options.RunVB()
        If options.buffer.Length <> options.bufferSize Then
            SyncLock mutex
                ReDim options.buffer(options.bufferSize - 1)
                options.buffer = Enumerable.Repeat(-1, options.buffer.Length).ToArray
                frameCount = 0
                head = -1
                tail = -1
            End SyncLock
        End If

        SyncLock mutex
            flow.Run(empty)
        End SyncLock
    End Sub
    Public Sub Close()
        terminateProducer = True
        terminateConsumer = True
    End Sub
End Class





' https://www.codeproject.com/Articles/5280034/Generation-of-Infinite-Sequences-in-Csharp-and-Unm
Public Class ProCon_Variation : Inherits VB_Parent
    Dim procon As ProCon_Basics
    Dim frameCount As Integer
    Public Sub New()
        procon = New ProCon_Basics()
        procon.terminateProducer = True ' we don't need a 2 producer task.  RunVB below provides the second thread.
        desc = "DijKstra's Producer/Consumer - similar to Basics above but producer is the algorithm thread."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        SyncLock procon.mutex
            procon.tail = procon.success(procon.tail)
            If procon.options.buffer(procon.tail) = -1 Then
                procon.flow.nextMsg = "producer: = " + CStr(frameCount)
                procon.options.buffer(procon.tail) = frameCount
                frameCount += 1
            End If
        End SyncLock
        procon.Run(src)
    End Sub
    Public Sub Close()
        procon.terminateConsumer = True
        procon.terminateProducer = True
    End Sub
End Class




