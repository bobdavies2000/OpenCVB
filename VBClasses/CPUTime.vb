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

            algorithmNames.Add("Wait For Input")
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
        Public Function PrepareReport(treeData As List(Of String)) As String
            Dim algorithm_ms = New List(Of Single)(task.cpu.algorithm_ms)
            Dim sumTime As Single
            For i = 0 To algorithm_ms.Count - 1
                sumTime += algorithm_ms(i)
                task.cpu.algorithm_ms(i) = 0
            Next

            For Each percent In algorithm_ms
                percent /= sumTime
            Next

            Dim saveWaitTime As String = ""
            Dim PercentTimes As New SortedList(Of Single, String)(New compareAllowIdenticalSingleInverted)
            Dim percentStr As String = ""
            For i = 0 To algorithm_ms.Count - 1
                algorithm_ms(i) /= sumTime
                If algorithm_ms(i) < 0 Then algorithm_ms(i) = 0
                If i >= task.cpu.algorithmNames.Count Then Exit For
                Dim str = Format(algorithm_ms(i), "00.0%") + " " + task.cpu.algorithmNames(i)
                If task.cpu.displayObjectName IsNot Nothing Then
                    If task.cpu.displayObjectName.Length > 0 Then
                        If str.Contains(task.cpu.displayObjectName) Then percentStr = str
                    End If
                End If
                If task.cpu.algorithmNames(i).Contains("Wait For Input") Then
                    saveWaitTime = str + "<<<<<< " + vbCrLf
                Else
                    PercentTimes.Add(algorithm_ms(i), str)
                End If
            Next

            Dim otherTimes As New List(Of Single)
            For Each percent In PercentTimes.Keys
                If percent < 0.01 Then otherTimes.Add(percent)
            Next

            Dim percentTime As String = ""
            percentTime = "Click on an algorithm to see more info. " + vbCrLf + vbCrLf
            percentTime += "Algorithm FPS = " + Format(task.fpsAlgorithm, "0") + vbCrLf
            percentTime += "Camera FPS = " + Format(task.fpsCamera, "0") + vbCrLf

            Dim fps As Single = task.Settings.FPSdisplay
            fps = If(fps >= 1, fps, If(fps = 0, 1, 1 / Math.Abs(fps)))
            Dim fmt As String = If(fps >= 1, "0", "0.0")
            percentTime += "Display FPS ~ " + Format(fps, fmt) + vbCrLf + vbCrLf

            'Static boldFont = New Font(PercentTime.Font, FontStyle.Bold)
            'Static regularFont = New Font(PercentTime.Font, FontStyle.Regular)

            Dim timeDataTree As New List(Of String)(treeData)
            For i = 0 To PercentTimes.Count - 1
                If PercentTimes.ElementAt(i).Key > 0.01 Then
                    Dim str = PercentTimes.ElementAt(i).Value
                    Dim index = treeData.IndexOf(str.Substring(6))
                    percentTime += str + vbCrLf
                    If index >= 0 Then timeDataTree(index) = str.Substring(0, 5) + " " + timeDataTree(index)
                End If
            Next

            percentTime += saveWaitTime + vbCrLf

            percentTime += Format(otherTimes.Sum, "00.0%") + " " + CStr(otherTimes.Count) + " algorithms each < 1.0%" +
                                vbCrLf + vbCrLf + "Click an algorithm at left to see it below:" + vbCrLf + vbCrLf

            percentTime += If(percentStr Is Nothing, "Inactive algorithm selected", percentStr)
            Return percentTime
        End Function
    End Class
End Namespace
