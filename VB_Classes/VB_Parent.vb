Imports cv = OpenCvSharp
Public Class trueText
    Public text As String
    Public picTag = 2
    Public pt As cv.Point
    Private Sub setup(_text As String, _pt As cv.Point, camPicIndex As Integer)
        text = _text
        pt = _pt
        picTag = camPicIndex
    End Sub
    Public Sub New(_text As String, _pt As cv.Point, camPicIndex As Integer)
        setup(_text, _pt, camPicIndex)
    End Sub
    Public Sub New(_text As String, _pt As cv.Point)
        setup(_text, _pt, 2)
    End Sub
End Class
Public Class VB_Parent : Implements IDisposable
    Public check As New OptionsCheckbox
    Public combo As New OptionsCombo
    Public radio As New OptionsRadioButtons
    Public sliders As New OptionsSliders
    Public standalone As Boolean
    Public firstPass As Boolean
    Public dst0 As cv.Mat, dst1 As cv.Mat, dst2 As cv.Mat, dst3 As cv.Mat, empty As cv.Mat
    Public labels(4 - 1) As String
    Public msRNG As New System.Random
    Public algorithm As Object
    Public traceName As String
    Public desc As String
    Dim callStack = ""
    Public nearColor = cv.Scalar.Yellow
    Public farColor = cv.Scalar.Blue
    Public black As New cv.Vec3b, white As New cv.Vec3b(255, 255, 255), gray As New cv.Vec3b(127, 127, 127)
    Public yellow = New cv.Vec3b(0, 255, 255), purple = New cv.Vec3b(255, 0, 255), teal = New cv.Vec3b(255, 255, 0)
    Public red = New cv.Vec3b(0, 0, 255), green = New cv.Vec3b(0, 255, 0), blue = New cv.Vec3b(255, 0, 0)
    Public zero3f As New cv.Point3f(0, 0, 0)
    Public newVec4f As New cv.Vec4f
    Public cPtr As IntPtr
    Public trueData As New List(Of trueText)
    Public strOut As String
    Public Sub initParent()
        If task.algName.StartsWith("CPP_") Then
            task.callTrace.Clear()
            task.callTrace.Add("CPP_Basics\")
        End If

        standalone = task.callTrace(0) = traceName + "\" ' only the first is standaloneTest() (the primary algorithm.)
        If traceName = "Python_Run" Then standalone = True
        If standalone = False And task.callTrace.Contains(callStack) = False Then
            task.callTrace.Add(callStack)
        End If

        dst0 = New cv.Mat(task.workingRes, cv.MatType.CV_8UC3, 0)
        dst1 = New cv.Mat(task.workingRes, cv.MatType.CV_8UC3, 0)
        dst2 = New cv.Mat(task.workingRes, cv.MatType.CV_8UC3, 0)
        dst3 = New cv.Mat(task.workingRes, cv.MatType.CV_8UC3, 0)
        task.activeObjects.Add(Me)

        If task.recordTimings Then
            If standalone And task.testAllRunning = False Then
                task.algorithmNames.Add("waitingForInput")
                algorithmTimes.Add(Now)
                task.algorithm_ms.Add(0)

                task.algorithmNames.Add("inputBufferCopy")
                algorithmTimes.Add(Now)
                task.algorithm_ms.Add(0)

                task.algorithmNames.Add("ReturnCopyTime")
                algorithmTimes.Add(Now)
                task.algorithm_ms.Add(0)

                task.algorithmNames.Add(traceName)
                algorithmTimes.Add(Now)
                task.algorithm_ms.Add(0)

                algorithmStack = New Stack()
                algorithmStack.Push(0)
                algorithmStack.Push(1)
                algorithmStack.Push(2)
                algorithmStack.Push(3)
            End If
        End If
        firstPass = True
    End Sub
    Public Sub Run(src As cv.Mat)
        If task.testAllRunning = False Then measureStartRun(traceName)

        trueData.Clear()
        If task.paused = False Then
            If task.algName.StartsWith("Options_") = False Then algorithm.RunVB(src)
        End If
        firstPass = False
        If task.testAllRunning = False Then measureEndRun(traceName)
    End Sub
    Public Sub setTrueText(text As String, pt As cv.Point, Optional picTag As Integer = 2)
        Dim str As New trueText(text, pt, picTag)
        trueData.Add(str)
    End Sub
    Public Sub setTrueText(text As String)
        Dim pt = New cv.Point(0, 0)
        Dim picTag = 2
        Dim str As New trueText(text, pt, picTag)
        trueData.Add(str)
    End Sub
    Public Sub setTrueText(text As String, picTag As Integer)
        Dim pt = New cv.Point(0, 0)
        Dim str As New trueText(text, pt, picTag)
        trueData.Add(str)
    End Sub
    Public Sub setFlowText(text As String, picTag As Integer)
        Dim pt = New cv.Point(0, 0)
        Dim str As New trueText(text, pt, picTag)
        task.flowData.Add(str)
    End Sub
    Public Function standaloneTest() As Boolean
        If standalone Or showIntermediate() Then Return True
        Return False
    End Function
    Public Sub measureStartRun(name As String)
        If task.recordTimings = False Then Exit Sub
        Dim nextTime = Now
        If task.algorithmNames.Contains(name) = False Then
            task.algorithmNames.Add(name)
            task.algorithm_ms.Add(0)
            algorithmTimes.Add(nextTime)
        End If

        Dim index = algorithmStack.Peek
        Dim elapsedTicks = nextTime.Ticks - algorithmTimes(index).Ticks
        Dim span = New TimeSpan(elapsedTicks)
        task.algorithm_ms(index) += span.Ticks / TimeSpan.TicksPerMillisecond

        index = task.algorithmNames.IndexOf(name)
        algorithmTimes(index) = nextTime
        algorithmStack.Push(index)
    End Sub
    Public Sub measureEndRun(name As String)
        If task.recordTimings = False Then Exit Sub
        Dim nextTime = Now
        Dim index = algorithmStack.Peek
        Dim elapsedTicks = nextTime.Ticks - algorithmTimes(index).Ticks
        Dim span = New TimeSpan(elapsedTicks)
        task.algorithm_ms(index) += span.Ticks / TimeSpan.TicksPerMillisecond
        algorithmStack.Pop()
        algorithmTimes(algorithmStack.Peek) = nextTime
    End Sub
    Public Function showIntermediate() As Boolean
        If task.intermediateObject Is Nothing Then Return False
        If task.intermediateObject.traceName = traceName Then Return True
        Return False
    End Function
    Public Function initRandomRect(margin As Integer) As cv.Rect
        Return New cv.Rect(msRNG.Next(margin, dst2.Width - 2 * margin), msRNG.Next(margin, dst2.Height - 2 * margin),
                           msRNG.Next(margin, dst2.Width - 2 * margin), msRNG.Next(margin, dst2.Height - 2 * margin))
    End Function
    Public Function quickRandomPoints(howMany As Integer) As List(Of cv.Point2f)
        Dim srcPoints As New List(Of cv.Point2f)
        Dim w = task.workingRes.Width
        Dim h = task.workingRes.Height
        For i = 0 To howMany - 1
            Dim pt = New cv.Point2f(msRNG.Next(0, w), msRNG.Next(0, h))
            srcPoints.Add(pt)
        Next
        Return srcPoints
    End Function
    Public Sub New()
        algorithm = Me
        traceName = Me.GetType.Name
        labels = {"", "", traceName, ""}
        Dim stackTrace = Environment.StackTrace
        Dim lines() = stackTrace.Split(vbCrLf)
        For i = 0 To lines.Count - 1
            lines(i) = Trim(lines(i))
            Dim offset = InStr(lines(i), "VB_Classes.")
            If offset > 0 Then
                Dim partLine = Mid(lines(i), offset + 11)
                If partLine.StartsWith("AlgorithmList.createVBAlgorithm") Then Exit For
                Dim split() = partLine.Split("\")
                partLine = Mid(partLine, 1, InStr(partLine, ".") - 1)
                If Not (partLine.StartsWith("VB_Parent") Or partLine.StartsWith("VBtask")) Then
                    callStack = partLine + "\" + callStack
                End If
            End If
        Next
        initParent()
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If allOptions IsNot Nothing Then allOptions.Close()
        If task.pythonTaskName.EndsWith(".py") Then
            Dim proc = Process.GetProcesses()
            For i = 0 To proc.Count - 1
                If proc(i).ProcessName.ToLower.Contains("pythonw") Then Continue For
                If proc(i).ProcessName.ToLower.Contains("python") Then
                    If proc(i).HasExited = False Then proc(i).Kill()
                End If
            Next
        End If
        For Each algorithm In task.activeObjects
            Dim type As Type = algorithm.GetType()
            If type.GetMethod("Close") IsNot Nothing Then algorithm.Close()  ' Close any unmanaged classes...
        Next
        sliders.Dispose()
        check.Dispose()
        radio.Dispose()
        combo.Dispose()
    End Sub
    Public Sub NextFrame(src As cv.Mat)
        algorithm.Run(src)

        task.labels = labels

        ' make sure that any outputs from the algorithm are the right size.nearest
        If dst0.Size <> task.workingRes And dst0.Width > 0 Then dst0 = dst0.Resize(task.workingRes, cv.InterpolationFlags.Nearest)
        If dst1.Size <> task.workingRes And dst1.Width > 0 Then dst1 = dst1.Resize(task.workingRes, cv.InterpolationFlags.Nearest)
        If dst2.Size <> task.workingRes And dst2.Width > 0 Then dst2 = dst2.Resize(task.workingRes, cv.InterpolationFlags.Nearest)
        If dst3.Size <> task.workingRes And dst3.Width > 0 Then dst3 = dst3.Resize(task.workingRes, cv.InterpolationFlags.Nearest)

        If task.pixelViewerOn Then
            If task.intermediateObject IsNot Nothing Then
                task.dst0 = task.intermediateObject.dst0
                task.dst1 = task.intermediateObject.dst1
                task.dst2 = task.intermediateObject.dst2
                task.dst3 = task.intermediateObject.dst3
            Else
                task.dst0 = If(gOptions.displayDst0.Checked, dst0, task.color)
                task.dst1 = If(gOptions.displayDst1.Checked, dst1, task.depthRGB)
                task.dst2 = dst2
                task.dst3 = dst3
            End If
            task.PixelViewer.viewerForm.Show()
            task.PixelViewer.Run(src)
        Else
            If task.PixelViewer IsNot Nothing Then If task.PixelViewer.viewerForm.Visible Then task.PixelViewer.viewerForm.Hide()
        End If

        Dim obj = checkIntermediateResults()
        task.intermediateObject = obj
        task.trueData = New List(Of trueText)(trueData)
        If obj IsNot Nothing Then
            If gOptions.displayDst0.Checked Then task.dst0 = MakeSureImage8uC3(obj.dst0) Else task.dst0 = task.color
            If gOptions.displayDst1.Checked Then task.dst1 = MakeSureImage8uC3(obj.dst1) Else task.dst1 = task.depthRGB
            task.dst2 = If(obj.dst2.Type = cv.MatType.CV_8UC3, obj.dst2, MakeSureImage8uC3(obj.dst2))
            task.dst3 = If(obj.dst3.Type = cv.MatType.CV_8UC3, obj.dst3, MakeSureImage8uC3(obj.dst3))
            task.labels = obj.labels
            task.trueData = New List(Of trueText)(obj.trueData)
        Else
            If gOptions.displayDst0.Checked Then task.dst0 = MakeSureImage8uC3(dst0) Else task.dst0 = task.color
            If gOptions.displayDst1.Checked Then task.dst1 = MakeSureImage8uC3(dst1) Else task.dst1 = task.depthRGB
            task.dst2 = MakeSureImage8uC3(dst2)
            task.dst3 = MakeSureImage8uC3(dst3)
        End If

        If task.gifCreator IsNot Nothing Then task.gifCreator.createNextGifImage()

        If task.dst2.Width = task.workingRes.Width And task.dst2.Height = task.workingRes.Height Then
            If gOptions.ShowGrid.Checked Then task.dst2.SetTo(cv.Scalar.White, task.gridMask)
            If task.dst2.Width <> task.workingRes.Width Or task.dst2.Height <> task.workingRes.Height Then
                task.dst2 = task.dst2.Resize(task.workingRes, cv.InterpolationFlags.Nearest)
            End If
            If task.dst3.Width <> task.workingRes.Width Or task.dst3.Height <> task.workingRes.Height Then
                task.dst3 = task.dst3.Resize(task.workingRes, cv.InterpolationFlags.Nearest)
            End If
        End If
    End Sub
End Class
