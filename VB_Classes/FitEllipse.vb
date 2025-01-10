Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://docs.opencvb.org/3.4.2/de/dc7/fitellipse_8cpp-example.html
Public Class FitEllipse_Basics : Inherits TaskParent
    Dim options As New Options_MinArea
    Public inputPoints As New List(Of cv.Point2f)
    Public box As cv.RotatedRect
    Public vertices() As cv.Point2f
    Public Sub New()
        desc = "Use FitEllipse OpenCV API to draw around a set of points"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        If not task.heartBeat Then Exit Sub
        If standaloneTest() Then
            options.RunOpt()
            inputPoints = options.srcPoints
        End If

        dst2.SetTo(0)
        For Each pt In inputPoints
            DrawCircle(dst2,pt, task.DotSize, white)
        Next

        If inputPoints.Count > 4 Then
            box = cv.Cv2.FitEllipse(inputPoints)
            vertices = box.Points()
            If standaloneTest() Then
                For j = 0 To vertices.Count - 1
                    DrawLine(dst2, vertices(j), vertices((j + 1) Mod 4), cv.Scalar.Green)
                Next
                dst2.Ellipse(box, cv.Scalar.Green, task.lineWidth, task.lineType)
            End If
        End If
    End Sub
End Class





' https://docs.opencvb.org/3.4.2/de/dc7/fitellipse_8cpp-example.html
Public Class FitEllipse_AMS_CPP : Inherits TaskParent
    Dim options As New Options_MinArea
    Public inputPoints As New List(Of cv.Point2f)
    Public Sub New()
        labels(2) = "FitEllipse_AMS_CPP C++ "
        desc = "Use FitEllipse_AMS to draw around a set of points"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        If not task.heartBeat Then Exit Sub
        If standaloneTest() Then
            options.RunOpt()
            inputPoints = options.srcPoints
        End If
        dst2.SetTo(0)
        For Each pt In inputPoints
            DrawCircle(dst2, pt, task.DotSize, white)
        Next

        Dim input As cv.Mat = cv.Mat.FromPixelData(inputPoints.Count, 1, cv.MatType.CV_32FC2, inputPoints.ToArray)
        Dim dataSrc(inputPoints.Count * 2 - 1) As Single
        Marshal.Copy(input.Data, dataSrc, 0, dataSrc.Length)

        Dim srcHandle = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim boxPtr = FitEllipse_AMS(srcHandle.AddrOfPinnedObject(), inputPoints.Count)
        srcHandle.Free()

        Dim ellipse(5 - 1) As Single
        Marshal.Copy(boxPtr, ellipse, 0, ellipse.Length)

        Dim angle = ellipse(0)
        Dim center As New cv.Point2f(ellipse(1), ellipse(2))
        Dim size As New cv.Size2f(ellipse(3), ellipse(4))
        If size.Width < task.lineWidth + 1 Or size.Height < task.lineWidth + 1 Then Exit Sub

        Dim box = New cv.RotatedRect(center, size, angle)
        dst2.Ellipse(box, cv.Scalar.Yellow, task.lineWidth, task.lineType)
    End Sub
End Class






' https://docs.opencvb.org/3.4.2/de/dc7/fitellipse_8cpp-example.html
Public Class FitEllipse_Direct_CPP : Inherits TaskParent
    Dim options As New Options_MinArea
    Public Sub New()
        labels(2) = "The FitEllipse_Direct C++ "
        desc = "Use FitEllipse to draw around a set of points"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        If not task.heartBeat Then Exit Sub
        options.RunOpt()
        Dim dataSrc(options.srcPoints.Count * 2 - 1) As Single

        dst2.SetTo(0)
        For Each pt In options.srcPoints
            DrawCircle(dst2,pt, task.DotSize, white)
        Next

        Dim input As cv.Mat = cv.Mat.FromPixelData(options.srcPoints.Count, 1, cv.MatType.CV_32FC2, options.srcPoints.ToArray)
        Marshal.Copy(input.Data, dataSrc, 0, dataSrc.Length)

        Dim srcHandle = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim boxPtr = FitEllipse_Direct(srcHandle.AddrOfPinnedObject(), options.srcPoints.Count)
        srcHandle.Free()

        Dim ellipse(5 - 1) As Single
        Marshal.Copy(boxPtr, ellipse, 0, ellipse.Length)

        Dim angle = ellipse(0)
        Dim center As New cv.Point2f(ellipse(1), ellipse(2))
        Dim size As New cv.Size2f(ellipse(3), ellipse(4))
        If size.Width < task.lineWidth + 1 Or size.Height < task.lineWidth + 1 Then Exit Sub

        Dim box = New cv.RotatedRect(center, size, angle)
        dst2.Ellipse(box, cv.Scalar.Yellow, task.lineWidth, task.lineType)
    End Sub
End Class







Public Class FitEllipse_RedCloud : Inherits TaskParent
    Dim fitE As New FitEllipse_Basics
    Public Sub New()
        desc = "Create an ellipse from a contour"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        If not task.heartBeat Then Exit Sub
        dst2 = getRedColor(src, labels(2))

        If task.rc.contour Is Nothing Then Exit Sub
        fitE.inputPoints.Clear()
        For Each pt In task.rc.contour
            fitE.inputPoints.Add(New cv.Point2f(pt.X, pt.Y))
        Next
        fitE.Run(src)
        dst3.SetTo(0)
        dst3(task.rc.rect).SetTo(white, task.rc.mask)
        dst3.Rectangle(task.rc.rect, white, task.lineWidth, task.lineType)
        dst3(task.rc.rect).Ellipse(fitE.box, cv.Scalar.Yellow, task.lineWidth, task.lineType)
    End Sub
End Class