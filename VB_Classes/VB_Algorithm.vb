Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Imports System.Drawing
Public Class trueText
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
Public Class VB_Algorithm : Implements IDisposable
    Public check As New OptionsCheckbox
    Public combo As New OptionsCombo
    Public radio As New OptionsRadioButtons
    Public sliders As New OptionsSliders
    Public standalone As Boolean
    Public firstPass As Boolean
    Public myHighLightColor = cv.Scalar.Yellow
    Public dst0 As cv.Mat, dst1 As cv.Mat, dst2 As cv.Mat, dst3 As cv.Mat, empty As cv.Mat
    Public labels(4 - 1) As String
    Public msRNG As New System.Random
    Public algorithm As Object
    Public traceName As String
    Public desc As String
    Public advice As String
    Dim callStack = ""
    Public nearColor = cv.Scalar.Yellow
    Public farColor = cv.Scalar.Blue
    Public black As New cv.Vec3b
    Public white As New cv.Vec3b(255, 255, 255)
    Public zero3f As New cv.Point3f(0, 0, 0)
    Public cPtr As IntPtr
    Public trueData As New List(Of trueText)
    Public strOut As String
    Public Sub initParent()
        If task.algName.StartsWith("CPP_") Then
            task.callTrace.Clear()
            task.callTrace.Add("CPP_Basics\")
        End If

        standalone = task.callTrace(0) = traceName + "\" ' only the first is standalone (the primary algorithm.)
        If traceName = "Python_Run" Then standalone = True
        If standalone = False And task.callTrace.Contains(callStack) = False Then
            task.callTrace.Add(callStack)
        End If

        dst0 = New cv.Mat(task.workingRes, cv.MatType.CV_8UC3, 0)
        dst1 = New cv.Mat(task.workingRes, cv.MatType.CV_8UC3, 0)
        dst2 = New cv.Mat(task.workingRes, cv.MatType.CV_8UC3, 0)
        dst3 = New cv.Mat(task.workingRes, cv.MatType.CV_8UC3, 0)
        task.activeObjects.Add(Me)

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
        firstPass = True
    End Sub
    Public Function checkIntermediateResults() As VB_Algorithm
        For Each obj In task.activeObjects
            If obj.traceName = task.intermediateName And obj.firstPass = False Then Return obj
        Next
        Return Nothing
    End Function
    Public Sub measureStartRun(name As String)
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
        Dim nextTime = Now
        Dim index = algorithmStack.Peek
        Dim elapsedTicks = nextTime.Ticks - algorithmTimes(index).Ticks
        Dim span = New TimeSpan(elapsedTicks)
        task.algorithm_ms(index) += span.Ticks / TimeSpan.TicksPerMillisecond
        algorithmStack.Pop()
        algorithmTimes(algorithmStack.Peek) = nextTime
    End Sub
    Public Sub Run(src As cv.Mat)
        If task.testAllRunning = False Then measureStartRun(traceName)

        trueData.Clear()
        If task.paused = False Then algorithm.RunVB(src)
        firstPass = False
        If task.testAllRunning = False Then measureEndRun(traceName)
    End Sub
    Public Sub setTrueText(text As String, pt As cv.Point, Optional picTag As Integer = 2)
        Dim str As New trueText(text, pt.X, pt.Y, picTag)
        trueData.Add(str)
    End Sub
    Public Sub setTrueText(text As String)
        Dim pt = New cv.Point(0, 0)
        Dim picTag = 2
        Dim str As New trueText(text, pt.X, pt.Y, picTag)
        trueData.Add(str)
    End Sub
    Public Sub setTrueText(text As String, picTag As Integer)
        Dim pt = New cv.Point(0, 0)
        Dim str As New trueText(text, pt.X, pt.Y, picTag)
        trueData.Add(str)
    End Sub
    Public Function showIntermediate() As Boolean
        If task.intermediateObject Is Nothing Then Return False
        If task.intermediateObject.traceName = traceName Then Return True
        Return False
    End Function
    Public Sub setFlowText(text As String, picTag As Integer)
        Dim pt = New cv.Point(0, 0)
        Dim str As New trueText(text, pt.X, pt.Y, picTag)
        task.flowData.Add(str)
    End Sub
    Public Function initRandomRect(margin As Integer) As cv.Rect
        Return New cv.Rect(msRNG.Next(margin, dst2.Width - 2 * margin), msRNG.Next(margin, dst2.Height - 2 * margin),
                           msRNG.Next(margin, dst2.Width - 2 * margin), msRNG.Next(margin, dst2.Height - 2 * margin))
    End Function
    Public Function vecToScalar(v As cv.Vec3b) As cv.Scalar
        Return New cv.Scalar(v(0), v(1), v(2))
    End Function
    Public Sub drawRotatedRectangle(rotatedRect As cv.RotatedRect, dst2 As cv.Mat, color As cv.Scalar)
        Dim vertices2f = rotatedRect.Points()
        Dim vertices(vertices2f.Length - 1) As cv.Point
        For j = 0 To vertices2f.Length - 1
            vertices(j) = New cv.Point(CInt(vertices2f(j).X), CInt(vertices2f(j).Y))
        Next
        dst2.FillConvexPoly(vertices, color, task.lineType)
    End Sub
    Public Sub drawRotatedOutline(rotatedRect As cv.RotatedRect, dst2 As cv.Mat, color As cv.Scalar)
        Dim pts = rotatedRect.Points()
        Dim lastPt = pts(0)
        For i = 1 To pts.Length
            Dim index = i Mod pts.Length
            Dim pt = New cv.Point(CInt(pts(index).X), CInt(pts(index).Y))
            dst2.Line(pt, lastPt, task.highlightColor, task.lineWidth, task.lineType)
            lastPt = pt
        Next
    End Sub
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
    Public Function findCorrelation(pts1 As cv.Mat, pts2 As cv.Mat) As Single
        Dim correlationMat As New cv.Mat
        cv.Cv2.MatchTemplate(pts1, pts2, correlationMat, cv.TemplateMatchModes.CCoeffNormed)
        Return correlationMat.Get(Of Single)(0, 0)
    End Function
    Public Sub AddPlotScale(dst As cv.Mat, minVal As Double, maxVal As Double, Optional lineCount As Integer = 3)
        ' draw a scale along the side
        Dim spacer = CInt(dst.Height / (lineCount + 1))
        Dim spaceVal = CInt((maxVal - minVal) / (lineCount + 1))
        If lineCount > 1 Then If spaceVal < 1 Then spaceVal = 1
        If spaceVal > 10 Then spaceVal += spaceVal Mod 10
        For i = 0 To lineCount
            Dim p1 = New cv.Point(0, spacer * i)
            Dim p2 = New cv.Point(dst.Width, spacer * i)
            dst.Line(p1, p2, cv.Scalar.White, task.cvFontThickness)
            Dim nextVal = (maxVal - spaceVal * i)
            Dim nextText = If(maxVal > 1000, Format(nextVal / 1000, "###,##0.0") + "k", Format(nextVal, fmt2))
            cv.Cv2.PutText(dst, nextText, p1, cv.HersheyFonts.HersheyPlain, task.cvFontSize, cv.Scalar.White,
                           task.cvFontThickness, task.lineType)
        Next
    End Sub
    Public Sub AddPlotScaleNew(dst As cv.Mat, minVal As Single, maxVal As Single, average As Single)
        Dim diff = maxVal - minVal
        Dim fmt = If(diff > 10, fmt0, If(diff > 2, fmt1, If(diff > 0.5, fmt2, fmt3)))
        For i = 0 To 2
            Dim nextVal = Choose(i + 1, maxVal, average, minVal)
            Dim nextText = If(maxVal > 1000, Format(nextVal / 1000, "###,##0.0") + "k", Format(nextVal, fmt))
            Dim pt = Choose(i + 1, New cv.Point(0, 15), New cv.Point(0, dst2.Height / 2), New cv.Point(0, dst2.Height - 10))
            cv.Cv2.PutText(dst, nextText, pt, cv.HersheyFonts.HersheyPlain, 1.0, cv.Scalar.White, 1, task.lineType)
        Next
    End Sub
    Public Function rectContainsPt(r As cv.Rect, pt As cv.Point) As Boolean
        If r.X <= pt.X And r.X + r.Width > pt.X And r.Y <= pt.Y And r.Y + r.Height > pt.Y Then Return True
        Return False
    End Function
    Public Function validateRect(ByVal r As cv.Rect) As cv.Rect
        If r.Width <= 0 Then r.Width = 1
        If r.Height <= 0 Then r.Height = 1
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.X > dst2.Width Then r.X = dst2.Width - 1
        If r.Y > dst2.Height Then r.Y = dst2.Height - 1
        If r.X + r.Width >= dst2.Width Then r.Width = dst2.Width - r.X - 1
        If r.Y + r.Height >= dst2.Height Then r.Height = dst2.Height - r.Y - 1
        If r.Width <= 0 Then r.Width = 1 ' check again (it might have changed.)
        If r.Height <= 0 Then r.Height = 1
        If r.X = dst2.Width Then r.X = r.X - 1
        If r.Y = dst2.Height Then r.Y = r.Y - 1
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
        traceName = Me.GetType.Name
        labels = {"", "", traceName, ""}
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
                If Not (partLine.StartsWith("VB_Algorithm") Or partLine.StartsWith("VBtask")) Then
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

    Public Sub setMyActiveMat()
        If task.mouseClickFlag Then
            Dim pt = task.clickPoint
            If pt.Y < task.workingRes.Height / 2 Then
                If pt.X < task.workingRes.Width / 2 Then task.quadrantIndex = QUAD0 Else task.quadrantIndex = QUAD1
            Else
                If pt.X < task.workingRes.Width / 2 Then task.quadrantIndex = QUAD2 Else task.quadrantIndex = QUAD3
            End If
            task.mouseClickFlag = False
        End If
    End Sub
    Public Sub NextFrame(src As cv.Mat)
        If task.drawRect.Width <> 0 Then task.drawRect = validateRect(task.drawRect)
        task.pcSplit(2).SetTo(task.maxZmeters, task.maxDepthMask)
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

        task.intermediateObject = checkIntermediateResults()
        task.trueData = New List(Of trueText)(trueData)
        If task.intermediateObject IsNot Nothing Then
            If gOptions.displayDst0.Checked Then task.dst0 = MakeSureImage8uC3(task.intermediateObject.dst0) Else task.dst0 = task.color
            If gOptions.displayDst1.Checked Then task.dst1 = MakeSureImage8uC3(task.intermediateObject.dst1) Else task.dst1 = task.depthRGB
            task.dst2 = MakeSureImage8uC3(task.intermediateObject.dst2)
            task.dst3 = MakeSureImage8uC3(task.intermediateObject.dst3)
            task.labels = task.intermediateObject.labels
            task.trueData = New List(Of trueText)(task.intermediateObject.trueData)
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
            task.frameCount += 1
        End If
    End Sub
End Class

