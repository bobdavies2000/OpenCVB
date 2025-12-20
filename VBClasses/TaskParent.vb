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

            If taskAlg.cpu.callTrace.Count = 0 Then taskAlg.cpu.callTrace.Add(taskAlg.Settings.algorithm + "\")
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

            dst0 = New cv.Mat(taskAlg.workRes, cv.MatType.CV_8UC3, 0)
            dst1 = New cv.Mat(taskAlg.workRes, cv.MatType.CV_8UC3, 0)
            dst2 = New cv.Mat(taskAlg.workRes, cv.MatType.CV_8UC3, 0)
            dst3 = New cv.Mat(taskAlg.workRes, cv.MatType.CV_8UC3, 0)

            standalone = traceName = taskAlg.Settings.algorithm
            taskAlg.cpu.callTrace.Add(callStack)

            taskAlg.cpu.activeObjects.Add(Me)

            If standalone Then taskAlg.cpu.initialize(traceName)
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
            If standalone Or taskAlg.cpu.displayObjectName = traceName Then Return True
            Return False
        End Function
        Public Sub DrawRect(dst As cv.Mat, rect As cv.Rect, color As cv.Scalar)
            dst.Rectangle(rect, color, taskAlg.lineWidth, taskAlg.lineType)
        End Sub
        Public Sub DrawRect(dst As cv.Mat, rect As cv.Rect)
            dst.Rectangle(rect, taskAlg.highlight, taskAlg.lineWidth, taskAlg.lineType)
        End Sub
        Public Sub DrawCircle(dst As cv.Mat, pt As cv.Point2f, radius As Integer, color As cv.Scalar,
                          Optional fillFlag As Integer = -1)
            dst.Circle(pt, radius, color, fillFlag, taskAlg.lineType)
        End Sub
        Public Sub DrawCircle(dst As cv.Mat, pt As cv.Point2f)
            dst.Circle(pt, taskAlg.DotSize, taskAlg.highlight, -1, taskAlg.lineType)
        End Sub
        Public Sub DrawCircle(dst As cv.Mat, pt As cv.Point2f, color As cv.Scalar)
            dst.Circle(pt, taskAlg.DotSize, color, -1, taskAlg.lineType)
        End Sub
        Public Sub DrawRotatedOutline(rotatedRect As cv.RotatedRect, dst2 As cv.Mat, color As cv.Scalar)
            Dim pts = rotatedRect.Points()
            Dim lastPt = pts(0)
            For i = 1 To pts.Length
                Dim index = i Mod pts.Length
                Dim pt = New cv.Point(CInt(pts(index).X), CInt(pts(index).Y))
                vbc.DrawLine(dst2, pt, lastPt, taskAlg.highlight)
                lastPt = pt
            Next
        End Sub
        Public Function ShowPaletteCorrelation(input As cv.Mat) As cv.Mat
            Dim output As New cv.Mat
            cv.Cv2.ApplyColorMap(input, output, taskAlg.correlationColorMap)
            Return output
        End Function
        Public Shared Function PaletteFull(input As cv.Mat) As cv.Mat
            Dim output As New cv.Mat
            If input.Type <> cv.MatType.CV_8U Then
                Dim input8u As New cv.Mat
                input.ConvertTo(input8u, cv.MatType.CV_8U)
                cv.Cv2.ApplyColorMap(input8u, output, taskAlg.colorMap)
            Else
                cv.Cv2.ApplyColorMap(input, output, taskAlg.colorMap)
            End If

            Return output
        End Function
        Public Shared Function PaletteBlackZero(input As cv.Mat) As cv.Mat
            Dim output As New cv.Mat
            If input.Type <> cv.MatType.CV_8U Then
                Dim input8u As New cv.Mat
                input.ConvertTo(input8u, cv.MatType.CV_8U)
                cv.Cv2.ApplyColorMap(input8u, output, taskAlg.colorMapZeroIsBlack)
            Else
                cv.Cv2.ApplyColorMap(input, output, taskAlg.colorMapZeroIsBlack)
            End If

            Return output
        End Function
        Public Shared Function ShowPaletteOriginal(input As cv.Mat) As cv.Mat
            If taskAlg.paletteRandom Is Nothing Then taskAlg.paletteRandom = New Palette_RandomColors
            If input.Type <> cv.MatType.CV_8U Then input.ConvertTo(input, cv.MatType.CV_8U)
            Return taskAlg.paletteRandom.useColorMapWithBlack(input).Clone
        End Function
        Public Function ShowPaletteFullColor(input As cv.Mat) As cv.Mat
            If taskAlg.paletteRandom Is Nothing Then taskAlg.paletteRandom = New Palette_RandomColors
            Return taskAlg.paletteRandom.useColorMapFull(input)
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
            If taskAlg.redList Is Nothing Then taskAlg.redList = New XO_RedList_Basics
            taskAlg.redList.inputRemoved = removeMask
            taskAlg.redList.Run(src)
            label = taskAlg.redList.labels(2)
            Return taskAlg.redList.dst2
        End Function
        Public Function runRedList(src As cv.Mat, ByRef label As String) As cv.Mat
            If taskAlg.redList Is Nothing Then taskAlg.redList = New XO_RedList_Basics
            taskAlg.redList.Run(src)
            label = taskAlg.redList.labels(2)
            Return taskAlg.redList.dst2
        End Function
        Public Function runRedCloud(src As cv.Mat, ByRef label As String) As cv.Mat
            If taskAlg.redCloud Is Nothing Then taskAlg.redCloud = New RedCloud_Basics
            taskAlg.redCloud.Run(src)
            label = taskAlg.redCloud.labels(2)
            Return taskAlg.redCloud.dst2
        End Function
        Public Function runRedColor(src As cv.Mat, ByRef label As String) As cv.Mat
            If taskAlg.redColor Is Nothing Then taskAlg.redColor = New RedColor_Basics
            taskAlg.redColor.Run(src)
            label = taskAlg.redColor.labels(2)
            Return taskAlg.redColor.dst2
        End Function
        Public Function InitRandomRect(margin As Integer) As cv.Rect
            Return New cv.Rect(msRNG.Next(margin, dst2.Width - 2 * margin), msRNG.Next(margin, dst2.Height - 2 * margin),
                           msRNG.Next(margin, dst2.Width - 2 * margin), msRNG.Next(margin, dst2.Height - 2 * margin))
        End Function
        Public Shared Sub DrawTour(dst As cv.Mat, contour As List(Of cv.Point), color As cv.Scalar, Optional lineWidth As Integer = -1,
                        Optional lineType As cv.LineTypes = cv.LineTypes.Link8)
            If contour Is Nothing Then Exit Sub
            If contour.Count < 3 Then Exit Sub ' this is not enough to draw.
            Dim listOfPoints = New List(Of List(Of cv.Point))({contour})
            cv.Cv2.DrawContours(dst, listOfPoints, 0, color, lineWidth, lineType)
        End Sub
        Public Sub DetectFace(ByRef src As cv.Mat, cascade As cv.CascadeClassifier)
            Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim faces() = cascade.DetectMultiScale(gray, 1.08, 3, cv.HaarDetectionTypes.ScaleImage, New cv.Size(30, 30))
            For Each fface In faces
                DrawRect(src, fface, cv.Scalar.Red)
            Next
        End Sub
        Public Sub houghShowLines(dst As cv.Mat, segments() As cv.LineSegmentPolar, desiredCount As Integer)
            For i = 0 To Math.Min(segments.Length, desiredCount) - 1
                Dim rho As Single = segments(i).Rho
                Dim theta As Single = segments(i).Theta

                Dim a As Double = Math.Cos(theta)
                Dim b As Double = Math.Sin(theta)
                Dim x As Double = a * rho
                Dim y As Double = b * rho

                Dim pt1 As cv.Point = New cv.Point(x + 1000 * -b, y + 1000 * a)
                Dim pt2 As cv.Point = New cv.Point(x - 1000 * -b, y - 1000 * a)
                dst.Line(pt1, pt2, cv.Scalar.Red, taskAlg.lineWidth + 1, taskAlg.lineType, 0)
            Next
        End Sub
        Public Sub Run(src As cv.Mat)
            taskAlg.cpu.measureStartRun(traceName)

            trueData.Clear()
            RunAlg(src)

            taskAlg.cpu.measureEndRun()
        End Sub
        Public Overridable Sub RunAlg(src As cv.Mat)
            ' every algorithm overrides this Sub 
        End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace