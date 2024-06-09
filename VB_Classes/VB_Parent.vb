Imports OpenCvSharp
Imports cv = OpenCvSharp
Imports System.Windows.Forms
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
    End Sub
    Public Function FindSlider(opt As String) As TrackBar
        Try
            For Each frm In Application.OpenForms
                If frm.text.endswith(" Sliders") Then
                    For j = 0 To frm.myTrackbars.Count - 1
                        If frm.myLabels(j).text.startswith(opt) Then Return frm.myTrackbars(j)
                    Next
                End If
            Next
        Catch ex As Exception
            Console.WriteLine("FindSlider failed.  The application list of forms changed while iterating.  Not critical." + ex.Message)
        End Try
        Console.WriteLine("A slider was Not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")

        Return Nothing
    End Function
    Public Function findCheckBox(opt As String) As CheckBox
        While 1
            Try
                For Each frm In Application.OpenForms
                    If frm.text.endswith(" CheckBoxes") Then
                        For j = 0 To frm.Box.Count - 1
                            If frm.Box(j).text = opt Then Return frm.Box(j)
                        Next
                    End If
                Next
            Catch ex As Exception
                Console.WriteLine("findCheckBox failed.  The application list of forms changed while iterating.  Not critical.")
            End Try
            Application.DoEvents()
            Static retryCount As Integer
            retryCount += 1
            If retryCount >= 5 Then
                Console.WriteLine("A checkbox was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")
                Exit While
            End If
        End While
        Return Nothing
    End Function
    Private Function searchForms(opt As String, ByRef index As Integer)
        While 1
            Try
                For Each frm In Application.OpenForms
                    If frm.text.endswith(" Radio Buttons") Then
                        For j = 0 To frm.check.count - 1
                            If frm.check(j).text = opt Then
                                index = j
                                Return frm.check
                            End If
                        Next
                    End If
                Next
            Catch ex As Exception
                Console.WriteLine("findRadioForm failed.  The application list of forms changed while iterating.  Not critical.")
            End Try
            Application.DoEvents()
            Static retryCount As Integer
            retryCount += 1
            If retryCount >= 5 Then
                Console.WriteLine("A Radio button was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")
                Exit While
            End If
        End While
        Return Nothing
    End Function
    Public Function findRadio(opt As String) As RadioButton
        Dim index As Integer
        Dim radio = searchForms(opt, index)
        If radio Is Nothing Then Return Nothing
        Return radio(index)
    End Function
    Public Function findRadioText(ByRef radioList As List(Of RadioButton)) As String
        For Each rad In radioList
            If rad.Checked Then Return rad.Text
        Next
        Return radioList(0).Text
    End Function
    Public Function findRadioIndex(ByRef radioList As List(Of RadioButton)) As String
        For i = 0 To radioList.Count - 1
            If radioList(i).Checked Then Return i
        Next
        Return 0
    End Function
    Public Function RebuildCells(sortedCells As SortedList(Of Integer, rcData)) As cv.Mat
        task.redCells.Clear()
        task.redCells.Add(New rcData)
        For Each rc In sortedCells.Values
            rc.index = task.redCells.Count
            task.redCells.Add(rc)
            If rc.index >= 255 Then Exit For
        Next

        Return DisplayCells()
    End Function
    Public Function DisplayCells() As cv.Mat
        Dim dst As New cv.Mat(task.workingRes, cv.MatType.CV_8UC3, 0)
        task.cellMap.SetTo(0)
        For Each rc In task.redCells
            dst(rc.rect).SetTo(If(task.redOptions.NaturalColor.Checked, rc.naturalColor, rc.color), rc.mask)
            task.cellMap(rc.rect).SetTo(rc.index, rc.mask)
        Next
        Return dst
    End Function
    Public Function Show_HSV_Hist(hist As cv.Mat) As cv.Mat
        Dim img As New cv.Mat(task.workingRes, cv.MatType.CV_8UC3, 0)
        Dim binCount = hist.Height
        Dim binWidth = img.Width / hist.Height
        Dim mm As mmData = GetMinMax(hist)
        img.SetTo(0)
        If mm.maxVal > 0 Then
            For i = 0 To binCount - 2
                Dim h = img.Height * (hist.Get(Of Single)(i, 0)) / mm.maxVal
                If h = 0 Then h = 5 ' show the color range in the plot
                cv.Cv2.Rectangle(img, New cv.Rect(i * binWidth, img.Height - h, binWidth, h),
                                 New cv.Scalar(CInt(180.0 * i / binCount), 255, 255), -1)
            Next
        End If
        Return img
    End Function
    Public Function GetHist2Dminmax(input As cv.Mat, chan1 As Integer, chan2 As Integer) As cv.Rangef()
        If input.Type = cv.MatType.CV_8UC3 Then
            ' ranges are exclusive in OpenCV 
            Return {New cv.Rangef(-histDelta, 256),
                    New cv.Rangef(-histDelta, 256)}
        End If

        Dim xInput = input.ExtractChannel(chan1)
        Dim yInput = input.ExtractChannel(chan2)

        Dim mmX = GetMinMax(xInput)
        Dim mmY = GetMinMax(yInput)

        ' ranges are exclusive in OpenCV 
        Return {New cv.Rangef(mmX.minVal - histDelta, mmX.maxVal + histDelta),
                New cv.Rangef(mmY.minVal - histDelta, mmY.maxVal + histDelta)}
    End Function
    Public Function GetMaxDist(ByRef rc As rcData) As cv.Point
        Dim mask = rc.mask.Clone
        mask.Rectangle(New cv.Rect(0, 0, mask.Width, mask.Height), 0, 1)
        Dim distance32f = mask.DistanceTransform(cv.DistanceTypes.L1, 0)
        Dim mm As mmData = GetMinMax(distance32f)
        mm.maxLoc.X += rc.rect.X
        mm.maxLoc.Y += rc.rect.Y

        Return mm.maxLoc
    End Function
    Public Function GetMinMax(mat As cv.Mat, Optional mask As cv.Mat = Nothing) As mmData
        Dim mm As mmData
        If mask Is Nothing Then
            mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc)
        Else
            mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc, mask)
        End If
        Return mm
    End Function
    Public Sub Run(src As cv.Mat)
        If task.testAllRunning = False Then measureStartRun(traceName)

        trueData.Clear()
        If task.paused = False Then
            If task.algName.StartsWith("Options_") = False Then algorithm.RunVB(src)
        End If
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
    Public Sub UpdateAdvice(advice As String)
        If task.advice.StartsWith("No advice for ") Then task.advice = ""
        Dim split = advice.Split(":")
        If task.advice.Contains(split(0) + ":") Then Return
        task.advice += advice + vbCrLf + vbCrLf
    End Sub
    Public Sub DrawContour(ByRef dst As cv.Mat, contour As List(Of cv.Point), color As cv.Scalar, Optional lineWidth As Integer = -10)
        If lineWidth = -10 Then lineWidth = task.lineWidth ' VB.Net only allows constants for optional parameter.
        If contour.Count < 3 Then Exit Sub ' this is not enough to draw.
        Dim listOfPoints = New List(Of List(Of cv.Point))
        listOfPoints.Add(contour)
        cv.Cv2.DrawContours(dst, listOfPoints, -1, color, lineWidth, task.lineType)
    End Sub
    Public Sub DrawLine(dst As Mat, p1 As Point2f, p2 As Point2f, color As Scalar)
        dst.Line(p1, p2, color, task.lineWidth, task.lineType)
    End Sub
    Public Sub DrawCircle(dst As Mat, pt As Point2f, radius As Integer, color As Scalar)
        dst.Circle(pt, radius, color, -1, task.lineType)
    End Sub
    Public Sub drawPolkaDot(pt As cv.Point2f, dst As cv.Mat)
        dst.Circle(pt, task.dotSize + 2, cv.Scalar.White, -1, task.lineType)
        DrawCircle(dst, pt, task.dotSize, cv.Scalar.Black)
    End Sub
    Public Sub drawRotatedOutline(rotatedRect As cv.RotatedRect, dst2 As cv.Mat, color As cv.Scalar)
        Dim pts = rotatedRect.Points()
        Dim lastPt = pts(0)
        For i = 1 To pts.Length
            Dim index = i Mod pts.Length
            Dim pt = New cv.Point(CInt(pts(index).X), CInt(pts(index).Y))
            DrawLine(dst2, pt, lastPt, task.highlightColor)
            lastPt = pt
        Next
    End Sub
    Public Function ShowPalette(input As cv.Mat) As cv.Mat
        If input.Type = cv.MatType.CV_32SC1 Then input.ConvertTo(input, cv.MatType.CV_8U)
        task.palette.Run(input)
        Return task.palette.dst2.Clone
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
    Public Sub processFrame(src As cv.Mat)
        algorithm.Run(src)
        task.labels = labels

        task.dst0 = task.color
        task.dst1 = task.depthRGB
        task.dst2 = dst2
        task.dst3 = dst3
    End Sub
End Class

