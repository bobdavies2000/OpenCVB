Imports cv = OpenCvSharp
Imports System.Drawing.Imaging
Namespace VBClasses
    Public Class TrueText
        Declare Sub CopyClassToManagedCpp Lib "ManagedCppLibrary.dll" (dataPtr As IntPtr)
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
    Public Class TaskParent : Implements IDisposable
        ' these variables are specific to each algorithm
        Public check As New OptionsCheckbox
        Public combo As New OptionsCombo
        Public radio As New OptionsRadioButtons
        Public sliders As New OptionsSliders
        Public standalone As Boolean
        Public dst0 As cv.Mat, dst1 As cv.Mat, dst2 As cv.Mat, dst3 As cv.Mat
        Public labels() As String = {"", "", "", ""}
        Public traceName As String
        Public desc As String
        Public cPtr As IntPtr
        Public trueData As New List(Of TrueText)
        Public strOut As String
        Public emptyRect As New cv.Rect
        Public Sub New()
            traceName = Me.GetType.Name

            If task.cpu.callTrace.Count = 0 Then
                task.cpu.callTrace.Add(task.Settings.algorithm + "\")
            End If

            labels = {"", "", traceName, ""}
            Dim stackTrace = Environment.StackTrace
            Dim lines() = stackTrace.Split(vbCrLf)
            Dim callStack As String = ""
            For i = 0 To lines.Count - 1
                If lines(i).Contains("System.Environment") Then Continue For
                If lines(i).Contains("TaskParent") Then Continue For
                lines(i) = Trim(lines(i))
                lines(i) = lines(i).Replace("at VBClasses.", "")
                lines(i) = lines(i).Replace(" at VBClasses.", "")
                lines(i) = lines(i).Substring(0, InStr(lines(i), ".") - 1)
                If lines(i).StartsWith("AlgorithmTask") Then Exit For
                If lines(i).StartsWith("at Microsoft") Then Continue For
                If lines(i).StartsWith("at System") Then Continue For
                If lines(i).StartsWith("at Main") Then Continue For
                callStack = lines(i) + "\" + callStack
            Next

            dst0 = New cv.Mat(task.workRes, cv.MatType.CV_8UC3, 0)
            dst1 = New cv.Mat(task.workRes, cv.MatType.CV_8UC3, 0)
            dst2 = New cv.Mat(task.workRes, cv.MatType.CV_8UC3, 0)
            dst3 = New cv.Mat(task.workRes, cv.MatType.CV_8UC3, 0)

            standalone = traceName = task.Settings.algorithm
            callStack = callStack.Replace("at Startup\", "")
            callStack = callStack.Replace("at Windows\", "")

            'If callStack.StartsWith(task.Settings.algorithm) = False Then
            '    callStack = task.Settings.algorithm + "\" + task.Settings.algorithm + "\" + callStack
            'End If

            task.cpu.callTrace.Add(callStack)

            Dim newCallTrace As New List(Of String)({task.cpu.callTrace(0)})
            For i = 1 To task.cpu.callTrace.Count - 1
                If task.cpu.callTrace(i) = task.cpu.callTrace(0) Then Continue For
                newCallTrace.Add(task.cpu.callTrace(i))
            Next

            task.cpu.callTrace = newCallTrace
            task.cpu.activeObjects.Add(Me)

            If standalone Then task.cpu.initialize(traceName)
        End Sub
        Public Sub SetTrueText(text As String, pt As cv.Point, Optional picTag As Integer = 2)
            SetTrueTextBase(text, pt, picTag)
        End Sub
        Public Sub SetTrueText(text As String, Optional picTag As Integer = 2)
            SetTrueTextBase(text, New cv.Point(0, 0), picTag)
        End Sub
        Public Sub SetTrueTextBase(text As String, pt As cv.Point, picTag As Integer)
            If text Is Nothing Then Return
            Dim strnext As New TrueText(text, pt, picTag)
            trueData.Add(strnext)
        End Sub
        Public Function standaloneTest() As Boolean
            If standalone Or task.cpu.displayObjectName = traceName Then Return True
            Return False
        End Function
        Public Sub DrawRect(dst As cv.Mat, rect As cv.Rect, color As cv.Scalar)
            dst.Rectangle(rect, color, task.lineWidth, task.lineType)
        End Sub
        Public Sub DrawRect(dst As cv.Mat, rect As cv.Rect)
            dst.Rectangle(rect, task.highlight, task.lineWidth, task.lineType)
        End Sub
        Public Sub DrawCircle(dst As cv.Mat, pt As cv.Point2f, radius As Integer, color As cv.Scalar,
                          Optional fillFlag As Integer = -1)
            dst.Circle(pt, radius, color, fillFlag, task.lineType)
        End Sub
        Public Sub DrawCircle(dst As cv.Mat, pt As cv.Point2f)
            dst.Circle(pt, task.DotSize, task.highlight, -1, task.lineType)
        End Sub
        Public Sub DrawCircle(dst As cv.Mat, pt As cv.Point2f, color As cv.Scalar)
            dst.Circle(pt, task.DotSize, color, -1, task.lineType)
        End Sub
        Public Shared Function PaletteFull(input As cv.Mat) As cv.Mat
            Dim output As New cv.Mat
            If input.Type <> cv.MatType.CV_8U Then
                Dim input8u As New cv.Mat
                input.ConvertTo(input8u, cv.MatType.CV_8U)
                cv.Cv2.ApplyColorMap(input8u, output, task.colorMap)
            Else
                cv.Cv2.ApplyColorMap(input, output, task.colorMap)
            End If

            Return output
        End Function
        Public Shared Function PaletteBlackZero(input As cv.Mat) As cv.Mat
            Dim output As New cv.Mat
            If input.Type <> cv.MatType.CV_8U Then
                Dim input8u As New cv.Mat
                input.ConvertTo(input8u, cv.MatType.CV_8U)
                cv.Cv2.ApplyColorMap(input8u, output, task.colorMapZeroIsBlack)
            Else
                cv.Cv2.ApplyColorMap(input, output, task.colorMapZeroIsBlack)
            End If

            Return output
        End Function
        Public Function ShowAddweighted(src1 As cv.Mat, src2 As cv.Mat, ByRef label As String) As cv.Mat
            Static addw As New AddWeighted_Basics

            addw.src2 = src2
            addw.Run(src1)
            Dim wt = addw.options.addWeighted
            label = "AddWeighted: src1 = " + Format(wt, "0%") + " vs. src2 = " + Format(1 - wt, "0%")
            Return addw.dst2
        End Function
        Public Function runRedList(src As cv.Mat, ByRef label As String, removeMask As cv.Mat) As cv.Mat
            If task.redList Is Nothing Then task.redList = New XO_RedList_Basics
            task.redList.inputRemoved = removeMask
            task.redList.Run(src)
            label = task.redList.labels(2)
            Return task.redList.dst2
        End Function
        Public Function runRedList(src As cv.Mat, ByRef label As String) As cv.Mat
            If task.redList Is Nothing Then task.redList = New XO_RedList_Basics
            task.redList.Run(src)
            label = task.redList.labels(2)
            Return task.redList.dst2
        End Function
        Public Function runRedCloud(src As cv.Mat, ByRef label As String) As cv.Mat
            If task.redCloud Is Nothing Then task.redCloud = New RedCloud_PrepEdges
            task.redCloud.Run(src)
            label = task.redCloud.labels(2)
            Return task.redCloud.dst2
        End Function
        Public Shared Sub DrawTour(dst As cv.Mat, contour As List(Of cv.Point), color As cv.Scalar, Optional lineWidth As Integer = -1,
                        Optional lineType As cv.LineTypes = cv.LineTypes.Link8)
            If contour Is Nothing Then Exit Sub
            If contour.Count < 3 Then Exit Sub ' this is not enough to draw.
            Dim listOfPoints = New List(Of List(Of cv.Point))({contour})
            cv.Cv2.DrawContours(dst, listOfPoints, 0, color, lineWidth, lineType)
        End Sub
        Public Sub Run(src As cv.Mat)
            task.cpu.measureStartRun(traceName)

            trueData.Clear()
            Try
                RunAlg(src)
            Catch ex As Exception
                Debug.WriteLine($"Exception in {traceName}: {ex.Message}")
                Debug.WriteLine($"Stack trace: {ex.StackTrace}")
            End Try

            task.cpu.measureEndRun()
        End Sub
        Public Overridable Sub RunAlg(src As cv.Mat)
            ' every algorithm overrides this Sub 
        End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace