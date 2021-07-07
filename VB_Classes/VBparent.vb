Imports System.Windows.Forms
Imports cv = OpenCvSharp
Imports System.IO
Public Class TTtext
    Public text As String
    Public picTag = 2
    Public x As Integer
    Public y As Integer
    Private Sub setup(_text As String, _x As Integer, _y As Integer, camPicIndex As Integer)
        text = _text
        x = _x
        y = _y
        picTag = camPicIndex
    End Sub
    Public Sub New(_text As String, _x As Integer, _y As Integer, camPicIndex As Integer)
        setup(_text, _x, _y, camPicIndex)
    End Sub
    Public Sub New(_text As String, _x As Integer, _y As Integer)
        setup(_text, _x, _y, 2)
    End Sub
End Class
Public Class VBparent : Implements IDisposable
    Public check As New OptionsCheckbox
    Public combo As New OptionsCombo
    Public radio As New OptionsRadioButtons
    Public radio1 As New OptionsRadioButtons
    Public sliders As New OptionsSliders
    Public pyStream As Object
    Public standalone As Boolean
    Public dst0 As cv.Mat
    Public dst1 As cv.Mat
    Public dst2 As cv.Mat
    Public dst3 As cv.Mat
    Public labels(4 - 1) As String
    Public msRNG As New System.Random
    Public algorithm As Object
    Public caller As String
    Public desc As String
    Public quadrantIndex As Integer = QUAD3
    Public minVal As Double, maxVal As Double
    Public minLoc As cv.Point, maxLoc As cv.Point
    Public usingdst0 As Boolean
    Public usingdst1 As Boolean
    Dim callStack = ""
    Public Sub initParent()
        standalone = task.callTrace(0) = caller + "\" ' only the first is standalone (the primary algorithm.)
        If caller = "Python_Run" Then standalone = True
        If standalone = False And task.callTrace.Contains(callStack) = False Then
            task.callTrace.Add(callStack)
        End If
        dst0 = New cv.Mat(task.color.Size, cv.MatType.CV_8UC3, 0)
        dst1 = New cv.Mat(task.color.Size, cv.MatType.CV_8UC3, 0)
        dst2 = New cv.Mat(task.color.Size, cv.MatType.CV_8UC3, 0)
        dst3 = New cv.Mat(task.color.Size, cv.MatType.CV_8UC3, 0)
        task.activeObjects.Add(Me)
        If standalone Then
            task.algorithmNames.Add("Non-Algorithm Time")
            algorithmTimes.Add(Now)
            task.algorithm_ms.Add(0)

            task.algorithmNames.Add(caller)
            algorithmTimes.Add(Now)
            task.algorithm_ms.Add(0)

            algorithmStack = New Stack()
            algorithmStack.Push(0)
            algorithmStack.Push(1)
        End If
    End Sub
    Public Sub checkIntermediateResults()
        task.intermediateObject = Nothing ' return nothing if there is nothing in dst2
        For Each obj In task.activeObjects
            If obj.caller = task.intermediateName Then
                Dim tmp = obj.dst2
                ' there may be several instances of an algorithmobject.  This will find one that is actually active.
                If obj.dst2.channels = 3 Then tmp = obj.dst2.cvtcolor(cv.ColorConversionCodes.BGR2GRAY)
                If tmp.countnonzero() > 0 Then
                    task.intermediateObject = obj
                    Exit Sub
                End If
            End If
        Next
    End Sub
    Public Sub NextFrame(src As cv.Mat)
        If task.drawRect.Width <> 0 Then task.drawRect = validateRect(task.drawRect)

        algorithm.RunClass(src)

        If standalone Or caller = "Python_Run" Then
            task.dst0Updated = usingdst0
            task.dst1Updated = usingdst1
            task.labels(0) = labels(0)
            task.labels(1) = labels(1)
            task.labels(2) = labels(2)
            task.labels(3) = labels(3)

            If dst0.Width <> task.color.Width Then dst0 = dst0.Resize(task.color.Size)
            If dst1.Width <> task.color.Width Then dst1 = dst1.Resize(task.color.Size)
            If dst2.Width <> task.color.Width Then dst2 = dst2.Resize(task.color.Size)
            If dst3.Width <> task.color.Width Then dst3 = dst3.Resize(task.color.Size)

            Dim tmpDst2 = dst2.Clone
            Dim tmpDst3 = dst3.Clone
            If task.intermediateName <> "" Then
                If task.intermediateActive = False And task.ttTextData.Count = 0 Then
                    Dim str As New TTtext("The " + task.intermediateName + " algorithm is not active in this configuration" + vbCrLf +
                                          "or the dst2 output was empty.", 10, 100, 2)
                    task.ttTextData.Add(str)
                    tmpDst2.SetTo(0)
                    tmpDst3.SetTo(0)
                    task.labels(2) = ""
                    task.labels(3) = ""
                Else
                    checkIntermediateResults()
                    If task.intermediateObject Is Nothing Then
                        setTrueText("The selected algorithm does not appear to be active.")
                    Else
                        tmpDst2 = task.intermediateObject.dst2.Clone
                        tmpDst3 = task.intermediateObject.dst3.Clone
                        task.labels(2) = task.intermediateObject.labels(2)
                        task.labels(3) = task.intermediateObject.labels(3)
                    End If
                End If
            End If
            If task.imgResult.Width <> dst2.Width * 2 Or task.imgResult.Height <> dst2.Height Then
                task.imgResult = New cv.Mat(New cv.Size(dst2.Width * 2, dst2.Height), cv.MatType.CV_8UC3)
            End If

            If usingdst0 Then task.color = dst0
            If usingdst1 Then task.RGBDepth = dst1

            If task.pixelViewerOn Then
                task.PixelViewer.viewerForm.Show()
                task.PixelViewer.RunClass(src)
            Else
                If task.PixelViewer.viewerForm.Visible Then task.PixelViewer.viewerForm.Hide()
            End If

            If usingdst0 Then task.color = MakeSureImage8uC3(dst0)
            If usingdst1 Then task.RGBDepth = MakeSureImage8uC3(dst1)
            task.imgResult(New cv.Rect(0, 0, task.color.Width, task.color.Height)) = MakeSureImage8uC3(tmpDst2)
            task.imgResult(New cv.Rect(task.color.Width, 0, task.color.Width, task.color.Height)) = MakeSureImage8uC3(tmpDst3)
            task.frameCount += 1
        End If
    End Sub
    Public Sub RunClass(src As cv.Mat)
        startRun(caller)
        algorithm.Run(src)
        endRun(caller)
    End Sub
    Public Sub setTrueText(text As String, Optional x As Integer = 10, Optional y As Integer = 40, Optional picTag As Integer = 2)
        Dim str As New TTtext(text, x, y, picTag)
        If task.intermediateActive Or task.intermediateName = "" Then task.ttTextData.Add(str)
    End Sub
    Public Function validateRect(r As cv.Rect) As cv.Rect
        If r.Width <= 0 Then r.Width = 1
        If r.Height <= 0 Then r.Height = 1
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.X > dst2.Width Then r.X = dst2.Width
        If r.Y > dst2.Height Then r.Y = dst2.Height
        If r.X + r.Width > dst2.Width Then r.Width = dst2.Width - r.X
        If r.Y + r.Height > dst2.Height Then r.Height = dst2.Height - r.Y
        Return r
    End Function
    Public Function validatePoint2f(p As cv.Point2f) As cv.Point2f
        If p.X < 0 Then p.X = 0
        If p.Y < 0 Then p.Y = 0
        If p.X >= dst2.Width Then p.X = dst2.Width - 1
        If p.Y >= dst2.Height Then p.Y = dst2.Height - 1
        Return p
    End Function
    Public Sub New()
        algorithm = Me
        caller = Me.GetType.Name
        labels(2) = caller
        Dim stackTrace = Environment.StackTrace
        Dim lines() = stackTrace.Split(vbCrLf)
        For i = 0 To lines.Count - 1
            lines(i) = Trim(lines(i))
            Dim offset = InStr(lines(i), "VB_Classes.")
            If offset > 0 Then
                Dim partLine = Mid(lines(i), offset + 11)
                If partLine.StartsWith("algorithmList.createAlgorithm") Then Exit For
                Dim split() = partLine.Split("\")
                partLine = Mid(partLine, 1, InStr(partLine, ".") - 1)
                If Not (partLine.StartsWith("VBparent") Or partLine.StartsWith("ActiveTask")) Then
                    callStack = partLine + "\" + callStack
                End If
            End If
        Next
        initParent()
    End Sub
    Public Function normalize32f(Input As cv.Mat) As cv.Mat
        Dim outMat = Input.Normalize(0, 255, cv.NormTypes.MinMax)
        outMat.ConvertTo(outMat, cv.MatType.CV_8UC1)
        Return outMat.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Function
    Private Function MakeSureImage8uC3(ByVal input As cv.Mat) As cv.Mat
        Dim outMat = input
        If input.Type = cv.MatType.CV_32F Then
            outMat = normalize32f(input)
        ElseIf input.Type = cv.MatType.CV_32SC1 Then
            Dim tmp As New cv.Mat
            input.ConvertTo(tmp, cv.MatType.CV_32F)
            outMat = normalize32f(tmp)
        ElseIf input.Type = cv.MatType.CV_32FC3 Then
            Dim split = input.Split()
            split(0) = split(0).ConvertScaleAbs(255)
            split(1) = split(1).ConvertScaleAbs(255)
            split(2) = split(2).ConvertScaleAbs(255)
            cv.Cv2.Merge(split, outMat)
        End If
        If input.Channels = 1 And input.Type = cv.MatType.CV_8UC1 Then outMat = input.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Return outMat
    End Function
    Public Sub Dispose() Implements IDisposable.Dispose
        If allOptions IsNot Nothing Then allOptions.Close()
        If task.pythonTaskName.EndsWith(".py") Then
            Dim proc = Process.GetProcessesByName("python")
            For i = 0 To proc.Count - 1
                ' the Oak-D interface runs as a Python script - so don't kill that one.  No one else can run with high priority.
                If proc(i).HasExited = False Then
                    If proc(i).PriorityClass <> ProcessPriorityClass.High Then proc(i).Kill()
                End If
            Next
            If pyStream IsNot Nothing Then pyStream.Dispose()
        End If
        Dim type As Type = algorithm.GetType()
        If type.GetMethod("Close") IsNot Nothing Then algorithm.Close()  ' Close any unmanaged classes...
        sliders.Dispose()
        check.Dispose()
        radio.Dispose()
        radio1.Dispose()
        combo.Dispose()
    End Sub

    Public Const QUAD0 = 0 ' there are 4 images to the user interface when using Mat_4to1.
    Public Const QUAD1 = 1
    Public Const QUAD2 = 2
    Public Const QUAD3 = 3
    Public Const verticalSlope As Single = 1000000

    Public Sub setMyActiveMat()
        If task.mouseClickFlag Then
            Dim pt = task.mouseClickPoint
            If pt.Y < task.color.Height / 2 Then
                If pt.X < task.color.Width / 2 Then quadrantIndex = QUAD0 Else quadrantIndex = QUAD1
            Else
                If pt.X < task.color.Width / 2 Then quadrantIndex = QUAD2 Else quadrantIndex = QUAD3
            End If
            task.mouseClickFlag = False
        End If
    End Sub
End Class
