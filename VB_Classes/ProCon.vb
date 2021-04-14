Imports cv = OpenCvSharp
Imports System.Threading
' https://www.codeproject.com/Articles/5280034/Generation-of-Infinite-Sequences-in-Csharp-and-Unm
Public Class ProCon_Basics
    Inherits VBparent
    Public buffer(10 - 1) As Integer
    Public mutex = New Mutex(True, "BufferMutex")
    Public p As Thread
    Public c As Thread
    Public head = -1
    Public tail = -1
    Public frameCount = 1
    Public flow As Font_FlowText
    Public terminateConsumer As Boolean
    Public terminateProducer As Boolean
    Public pduration As Integer
    Public cduration As Integer
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Buffer Size", 1, 100, buffer.Length)
            sliders.setupTrackBar(1, "Producer Workload Duration (ms)", 1, 1000, 100)
            sliders.setupTrackBar(2, "Consumer Workload Duration (ms)", 1, 1000, 10)
        End If
        pduration = sliders.trackbar(1).Value
        cduration = sliders.trackbar(2).Value

        flow = New Font_FlowText()

        buffer = Enumerable.Repeat(-1, buffer.Length).ToArray

        p = New Thread(AddressOf Producer)
        p.Name = "Producer"
        p.Start()
        c = New Thread(AddressOf Consumer)
        c.Name = "Consumer"
        c.Start()

        task.desc = "DijKstra's Producer/Consumer 'Cooperating Sequential Process'.  Consumer must see every item produced."
		' task.rank = 1
    End Sub
    Public Function success(index As Integer) As Integer
        Return (index + 1) Mod buffer.Length
    End Function
    Public Sub Consumer()
        While 1
            SyncLock mutex
                head = success(head)
                Dim item = buffer(head)
                If item <> -1 Then
                    flow.msgs.Add("Consumer: = " + CStr(item))
                    buffer(head) = -1
                End If
            End SyncLock
            If terminateConsumer Then Exit While Else Thread.Sleep(cduration)
        End While
    End Sub
    Private Sub Producer()
        While 1
            SyncLock mutex
                tail = success(tail)
                If buffer(tail) = -1 Then
                    buffer(tail) = frameCount
                    'flow.msgs.Add("Producer=: " + CStr(tail) + " = " + CStr(frameCount))
                    frameCount += 1
                End If
            End SyncLock
            If terminateProducer Then Exit While Else Thread.Sleep(pduration)
        End While
    End Sub
    Public Sub Run(src as cv.Mat)
        If sliders.trackbar(0).Value <> buffer.Length Then
            SyncLock mutex
                ReDim buffer(sliders.trackbar(0).Value - 1)
                buffer = Enumerable.Repeat(-1, buffer.Length).ToArray
                frameCount = 0
                head = -1
                tail = -1
            End SyncLock
        End If

        pduration = sliders.trackbar(1).Value
        cduration = sliders.trackbar(2).Value
        SyncLock mutex
            flow.Run(src)
        End SyncLock
    End Sub
    Public Sub Close()
        terminateProducer = True
        terminateConsumer = True
    End Sub
End Class





' https://www.codeproject.com/Articles/5280034/Generation-of-Infinite-Sequences-in-Csharp-and-Unm
Public Class ProCon_Variation
    Inherits VBparent
    Dim procon As ProCon_Basics
    Dim frameCount As Integer
    Public Sub New()
        initParent()
        procon = New ProCon_Basics()
        procon.sliders.trackbar(1).Enabled = False ' no duration for the producer because algorithm task is the producer.
        procon.terminateProducer = True ' we don't want 2 producer tasks...
        task.desc = "DijKstra's Producer/Consumer - similar to Basics above but producer is the algorithm thread."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        SyncLock procon.mutex
            procon.tail = procon.success(procon.tail)
            If procon.buffer(procon.tail) = -1 Then
                procon.buffer(procon.tail) = frameCount
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




