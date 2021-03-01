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
    Public src As cv.Mat
    Public dst1 As cv.Mat
    Public dst2 As cv.Mat
    Public label1 As String
    Public label2 As String
    Public msRNG As New System.Random
    Public algorithm As Object
    Public caller As String
    Public desc As String
    Public quadrantIndex As Integer = QUAD3
    Dim callStack = ""
    Public Sub initParent()
        If task.callTrace.Count = 0 Then
            standalone = True
            task.callTrace.Clear()
            task.callTrace.Add(callStack)
        Else
            standalone = False
            If task.callTrace.Contains(callStack) = False Then task.callTrace.Add(callStack)
        End If

        src = New cv.Mat(task.color.Size, cv.MatType.CV_8UC3, 0)
        dst1 = New cv.Mat(task.color.Size, cv.MatType.CV_8UC3, 0)
        dst2 = New cv.Mat(task.color.Size, cv.MatType.CV_8UC3, 0)
    End Sub
    Public Sub NextFrame()
        If standalone Or task.intermediateReview = caller Then src = task.color
        If src.Width <> dst1.Width Or src.Width <> dst2.Width Then
            dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
            dst2 = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
        End If
        If task.drawRect.Width <> 0 Then task.drawRect = validateRect(task.drawRect)
        algorithm.Run()
        If standalone And src.Width > 0 Then
            ocvb.label1 = label1
            ocvb.label2 = label2
            If task.intermediateReview <> "" And task.intermediateReview <> caller Then
                If ocvb.intermediateObject Is Nothing Then
                    ocvb.trueText(task.intermediateReview + " is not active.", 10, 100)
                    dst1.SetTo(0)
                    dst2.SetTo(0)
                Else
                    dst1 = ocvb.intermediateObject.dst1
                    dst2 = ocvb.intermediateObject.dst2
                    ocvb.label1 = ocvb.intermediateObject.label1
                    ocvb.label2 = ocvb.intermediateObject.label2
                End If
            End If
            If dst1.Width <> task.color.Width Then dst1 = dst1.Resize(New cv.Size(task.color.Width, task.color.Height))
            If dst2.Width <> task.color.Width Then dst2 = dst2.Resize(New cv.Size(task.color.Width, task.color.Height))
            If task.result.Width <> dst1.Width * 2 Or task.result.Height <> dst1.Height Then
                task.result = New cv.Mat(New cv.Size(dst1.Width * 2, dst1.Height), cv.MatType.CV_8UC3)
            End If

            task.PixelViewer.Run()

            task.result(New cv.Rect(0, 0, task.color.Width, task.color.Height)) = MakeSureImage8uC3(dst1)
            task.result(New cv.Rect(task.color.Width, 0, task.color.Width, task.color.Height)) = MakeSureImage8uC3(dst2)
            ocvb.frameCount += 1
        End If
    End Sub
    Public Function validateRect(r As cv.Rect) As cv.Rect
        If r.Width < 0 Then r.Width = 1
        If r.Height < 0 Then r.Height = 1
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.X > src.Width Then r.X = src.Width
        If r.Y > src.Height Then r.Y = src.Height
        If r.X + r.Width > src.Width Then r.Width = src.Width - r.X
        If r.Y + r.Height > src.Height Then r.Height = src.Height - r.Y
        Return r
    End Function
    Public Function validatePoint2f(p As cv.Point2f) As cv.Point2f
        If p.X < 0 Then p.X = 0
        If p.Y < 0 Then p.Y = 0
        If p.X > dst1.Width Then p.X = dst1.Width - 1
        If p.Y > dst1.Height Then p.Y = dst1.Height - 1
        Return p
    End Function
    Public Sub New()
        algorithm = Me
        caller = Me.GetType.Name
        label1 = caller
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
    End Sub
    Private Function MakeSureImage8uC3(ByVal input As cv.Mat) As cv.Mat
        Dim outMat = input
        If input.Type = cv.MatType.CV_32F Then
            outMat = input.Normalize(0, 255, cv.NormTypes.MinMax)
            outMat.ConvertTo(outMat, cv.MatType.CV_8UC1)
            outMat = outMat.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
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
        On Error Resume Next
        Dim proc = Process.GetProcessesByName("python")
        For i = 0 To proc.Count - 1
            ' the Oak-D interface runs as a Python script - so don't kill that one.  No one else can run with high priority.
            If proc(i).HasExited = False Then
                If proc(i).PriorityClass <> ProcessPriorityClass.High Then proc(i).Kill()
            End If
        Next
        If pyStream IsNot Nothing Then pyStream.Dispose()
        Dim type As Type = algorithm.GetType()
        If type.GetMethod("Close") IsNot Nothing Then algorithm.Close()  ' Close any unmanaged classes...
        If aOptions IsNot Nothing Then aOptions.Close()
        sliders.Dispose()
        check.Dispose()
        radio.Dispose()
        radio1.Dispose()
        combo.Dispose()
        src.Dispose()
        dst1.Dispose()
        dst2.Dispose()
        If task.pixelViewerOn Then task.PixelViewer.closeViewer()
    End Sub

    Public Const QUAD0 = 0 ' there are 4 images to the user interface when using Mat_4to1.
    Public Const QUAD1 = 1
    Public Const QUAD2 = 2
    Public Const QUAD3 = 3

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
