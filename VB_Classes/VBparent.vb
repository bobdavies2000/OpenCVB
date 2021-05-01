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
    Public dst1 As cv.Mat
    Public dst2 As cv.Mat
    Public dst3 As cv.Mat
    Public label1 As String
    Public label2 As String
    Public msRNG As New System.Random
    Public algorithm As Object
    Public caller As String
    Public desc As String
    Public quadrantIndex As Integer = QUAD3
    Public minVal As Double, maxVal As Double
    Public minLoc As cv.Point, maxLoc As cv.Point
    Dim callStack = ""
    Public Sub initParent()
        standalone = task.callTrace(0) = caller + "\" ' only the first is standalone (the primary algorithm.)
        If standalone = False And task.callTrace.Contains(callStack) = False Then
            task.callTrace.Add(callStack)
        End If
        task.activeObjects.Add(Me)
        dst1 = New cv.Mat(task.color.Size, cv.MatType.CV_8UC3, 0)
        dst2 = New cv.Mat(task.color.Size, cv.MatType.CV_8UC3, 0)
        dst3 = New cv.Mat(task.color.Size, cv.MatType.CV_8UC3, 0)
    End Sub
    Public Sub NextFrame(src As cv.Mat)
        If task.intermediateReview <> "" Then
            For Each obj In task.activeObjects
                If obj.caller = task.intermediateReview Then
                    task.intermediateObject = obj
                    Exit For
                End If
            Next
        End If
        If task.drawRect.Width <> 0 Then task.drawRect = validateRect(task.drawRect)
        algorithm.Run(src)
        If standalone Or caller = "Python_Run" Then
            task.label1 = label1
            task.label2 = label2
            If task.intermediateReview <> "" And task.intermediateReview <> caller Then
                If task.intermediateObject Is Nothing Then
                    task.trueText(task.intermediateReview + " is not active.", 10, 100)
                    dst1.SetTo(0)
                    dst2.SetTo(0)
                Else
                    dst1 = task.intermediateObject.dst1
                    dst2 = task.intermediateObject.dst2
                    task.label1 = task.intermediateObject.label1
                    task.label2 = task.intermediateObject.label2
                End If
            End If
            If dst1.Width <> task.color.Width Then dst1 = dst1.Resize(task.color.Size)
            If dst2.Width <> task.color.Width Then dst2 = dst2.Resize(task.color.Size)
            If task.imgResult.Width <> dst1.Width * 2 Or task.imgResult.Height <> dst1.Height Then
                task.imgResult = New cv.Mat(New cv.Size(dst1.Width * 2, dst1.Height), cv.MatType.CV_8UC3)
            End If

            If task.pythonTaskName.EndsWith(".py") = False Then
                If task.pixelViewerOn Then
                    task.PixelViewer.viewerForm.Show()
                    task.PixelViewer.Run(src)
                Else
                    If task.PixelViewer.viewerForm.Visible Then task.PixelViewer.viewerForm.Hide()
                End If
            End If

            task.imgResult(New cv.Rect(0, 0, task.color.Width, task.color.Height)) = MakeSureImage8uC3(dst1)
            task.imgResult(New cv.Rect(task.color.Width, 0, task.color.Width, task.color.Height)) = MakeSureImage8uC3(dst2)
            task.frameCount += 1
        End If
    End Sub
    Public Function validateRect(r As cv.Rect) As cv.Rect
        If r.Width < 0 Then r.Width = 1
        If r.Height < 0 Then r.Height = 1
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.X > dst1.Width Then r.X = dst1.Width
        If r.Y > dst1.Height Then r.Y = dst1.Height
        If r.X + r.Width > dst1.Width Then r.Width = dst1.Width - r.X
        If r.Y + r.Height > dst1.Height Then r.Height = dst1.Height - r.Y
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
        If aOptions IsNot Nothing Then aOptions.Close()
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
