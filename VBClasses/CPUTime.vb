Imports jsonShared

Namespace VBClasses
    Public Class CPUTime
        ' TreeView and trace Data.
        Public callTrace As List(Of String)
        Public algorithm_msMain As New List(Of Single)
        Public algorithmNamesMain As New List(Of String)
        Public algorithm_ms As New List(Of Single)
        Public algorithmNames As New List(Of String)
        Public algorithmTimes As New List(Of DateTime)
        Public algorithmStack As New Stack()
        Public displayObjectName As String
        Public activeObjects As New List(Of Object)

        Public Sub initialize(algName As String)
            algorithm_ms.Clear()
            algorithmNames.Clear()

            algorithmNames.Add("waitingForInput")
            algorithm_ms.Add(0)
            algorithmTimes.Add(Now)

            algorithmNames.Add(algName)
            algorithmTimes.Add(Now)
            algorithm_ms.Add(0)

            algorithmStack = New Stack()
            algorithmStack.Push(0)
            algorithmStack.Push(1)
        End Sub
        Public Sub measureStartRun(algName As String)
            Dim nextTime = Now
            If algorithmNames.Contains(algName) = False Then
                algorithmNames.Add(algName)
                algorithm_ms.Add(0)
                algorithmTimes.Add(nextTime)
            End If

            If algorithmStack.Count > 0 Then
                Dim index = algorithmStack.Peek
                Dim elapsedTicks = nextTime.Ticks - algorithmTimes(index).Ticks
                Dim span = New TimeSpan(elapsedTicks)
                algorithm_ms(index) += span.Ticks / TimeSpan.TicksPerMillisecond

                index = algorithmNames.IndexOf(algName)
                algorithmTimes(index) = nextTime
                algorithmStack.Push(index)
            End If
        End Sub
        Public Sub measureEndRun()
            Dim nextTime = Now
            Dim index = algorithmStack.Peek
            Dim elapsedTicks = nextTime.Ticks - algorithmTimes(index).Ticks
            Dim span = New TimeSpan(elapsedTicks)
            algorithm_ms(index) += span.Ticks / TimeSpan.TicksPerMillisecond
            algorithmStack.Pop()
            algorithmTimes(algorithmStack.Peek) = nextTime
        End Sub
        Public Sub startRun(algName As String)
            algorithmNames.Add(algName)
            algorithmTimes.Add(Now)
            algorithm_ms.Add(0)

            algorithmStack = New Stack()
            algorithmStack.Push(0)
        End Sub
    End Class
End Namespace
