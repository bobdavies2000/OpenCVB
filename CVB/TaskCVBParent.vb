Imports cv = OpenCvSharp
Public Class TaskCVBParent : Implements IDisposable
    Public check As New OptCVBCheckbox
    Public combo As New OptCVBCombo
    Public radio As New OptCVBRadioButtons
    Public sliders As New OptCVBSliders
    Public standalone As Boolean
    Public dst0 As cv.Mat, dst1 As cv.Mat, dst2 As cv.Mat, dst3 As cv.Mat
    Public labels() As String = {"", "", "", ""}
    Public traceName As String
    Public desc As String
    Public cPtr As IntPtr
    ' Public trueData As New List(Of TrueText)
    Public strOut As String
    Public Sub New()
        traceName = Me.GetType.Name

        'If myTask.callTrace.Count = 0 Then myTask.callTrace.Add(myTask.algName + "\")
        'labels = {"", "", traceName, ""}
        'Dim stackTrace = Environment.StackTrace
        'Dim lines() = stackTrace.Split(vbCrLf)
        'Dim callStack As String = ""
        'For i = 0 To lines.Count - 1
        '    If lines(i).Contains("System.Environment") Then Continue For
        '    If lines(i).Contains("TaskParent") Then Continue For
        '    lines(i) = Trim(lines(i))
        '    lines(i) = lines(i).Replace("at VBClasses.", "")
        '    lines(i) = lines(i).Replace(" at VBClasses.", "")
        '    lines(i) = lines(i).Substring(0, InStr(lines(i), ".") - 1)
        '    If lines(i).StartsWith("VBtask") Then Exit For
        '    If lines(i).StartsWith("at Microsoft") Then Continue For
        '    If lines(i).StartsWith("at System") Then Continue For
        '    If lines(i).StartsWith("at Main") Then Continue For
        '    callStack = lines(i) + "\" + callStack
        'Next

        'dst0 = New cv.Mat(myTask.workRes, cv.MatType.CV_8UC3, 0)
        'dst1 = New cv.Mat(myTask.workRes, cv.MatType.CV_8UC3, 0)
        'dst2 = New cv.Mat(myTask.workRes, cv.MatType.CV_8UC3, 0)
        'dst3 = New cv.Mat(myTask.workRes, cv.MatType.CV_8UC3, 0)

        'standalone = traceName = myTask.algName
        'myTask.callTrace.Add(callStack)

        'myTask.activeObjects.Add(Me)

        'If standalone Then
        '    myTask.algorithm_ms.Clear()
        '    myTask.algorithmNames.Clear()
        '    myTask.algorithmNames.Add("waitingForInput")
        '    myTask.algorithmTimes.Add(Now)
        '    myTask.algorithm_ms.Add(0)

        '    myTask.algorithmNames.Add("inputBufferCopy")
        '    myTask.algorithmTimes.Add(Now)
        '    myTask.algorithm_ms.Add(0)

        '    myTask.algorithmNames.Add("ReturnCopyTime")
        '    myTask.algorithmTimes.Add(Now)
        '    myTask.algorithm_ms.Add(0)

        '    myTask.algorithmNames.Add(traceName)
        '    myTask.algorithmTimes.Add(Now)
        '    myTask.algorithm_ms.Add(0)

        '    myTask.algorithmStack = New Stack()
        '    myTask.algorithmStack.Push(0)
        '    myTask.algorithmStack.Push(1)
        '    myTask.algorithmStack.Push(2)
        '    myTask.algorithmStack.Push(3)
        'End If
    End Sub

    Public Sub measureStartRun(name As String)
        Dim nextTime = Now
        If myTask.algorithmNames.Contains(name) = False Then
            myTask.algorithmNames.Add(name)
            myTask.algorithm_ms.Add(0)
            myTask.algorithmTimes.Add(nextTime)
        End If

        If myTask.algorithmStack.Count > 0 Then
            Dim index = myTask.algorithmStack.Peek
            Dim elapsedTicks = nextTime.Ticks - myTask.algorithmTimes(index).Ticks
            Dim span = New TimeSpan(elapsedTicks)
            myTask.algorithm_ms(index) += span.Ticks / TimeSpan.TicksPerMillisecond

            index = myTask.algorithmNames.IndexOf(name)
            myTask.algorithmTimes(index) = nextTime
            myTask.algorithmStack.Push(index)
        End If
    End Sub
    Public Sub measureEndRun(name As String)
        Try
            Dim nextTime = Now
            Dim index = myTask.algorithmStack.Peek
            Dim elapsedTicks = nextTime.Ticks - myTask.algorithmTimes(index).Ticks
            Dim span = New TimeSpan(elapsedTicks)
            myTask.algorithm_ms(index) += span.Ticks / TimeSpan.TicksPerMillisecond
            myTask.algorithmStack.Pop()
            myTask.algorithmTimes(myTask.algorithmStack.Peek) = nextTime
        Catch ex As Exception
        End Try
    End Sub
    Public Sub Run(src As cv.Mat)
        measureStartRun(traceName)

        ' trueData.Clear()
        RunAlg(src)

        measureEndRun(traceName)
    End Sub
    Public Overridable Sub RunAlg(src As cv.Mat)
        ' every algorithm overrides this Sub 
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
