Imports System.Runtime.InteropServices
Imports System.Windows.Shapes
Imports OpenCvSharp
Imports OpenCvSharp.Cv2
Imports cv = OpenCvSharp
' https://docs.opencvb.org/3.4.2/de/dc7/fitellipse_8cpp-example.html
Public Class FitEllipse_Basics : Inherits TaskParent
    Dim options As New Options_MinArea
    Public inputPoints As New List(Of cv.Point2f)
    Public box As cv.RotatedRect
    Public vertices() As cv.Point2f
    Public Sub New()
        desc = "Use FitEllipse OpenCV API to draw around a set of points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If Not task.heartBeat Then Exit Sub
        If standaloneTest() Then
            options.Run()
            inputPoints = options.srcPoints
        End If

        dst2.SetTo(0)
        For Each pt In inputPoints
            Circle(dst2, pt, task.DotSize, white, -1, task.lineType)
        Next

        If inputPoints.Count > 4 Then
            box = FitEllipse(inputPoints)
            vertices = box.Points()
            If standaloneTest() Then
                For i = 0 To vertices.Count - 1
                    If Single.IsNaN(vertices(i).X) Or Single.IsNaN(vertices(i).Y) Then Exit Sub ' can't draw the result...
                    cv.Cv2.Line(dst2, vertices(i), vertices((i + 1) Mod 4), cv.Scalar.Green, task.lineWidth, task.lineType)
                Next
                cv.Cv2.Ellipse(dst2, box, cv.Scalar.Green, task.lineWidth, task.lineType)
            End If
        End If
    End Sub
End Class





' https://docs.opencvb.org/3.4.2/de/dc7/fitellipse_8cpp-example.html
Public Class XR_FitEllipse_AMS_CPP : Inherits TaskParent
    Dim options As New Options_MinArea
    Public inputPoints As New List(Of cv.Point2f)
    Public Sub New()
        labels(2) = "NR_FitEllipse_AMS_CPP C++ "
        desc = "Use FitEllipse_AMS to draw around a set of points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If Not task.heartBeat Then Exit Sub
        If standaloneTest() Then
            options.Run()
            inputPoints = options.srcPoints
        End If
        dst2.SetTo(0)
        For Each pt In inputPoints
            Circle(dst2, pt, task.DotSize, white, -1, task.lineType)
        Next

        Dim input As cv.Mat = cv.Mat.FromPixelData(inputPoints.Count, 1, cv.MatType.CV_32FC2, inputPoints.ToArray)
        Dim dataSrc(inputPoints.Count * 2 - 1) As Single
        Marshal.Copy(input.Data, dataSrc, 0, dataSrc.Length)

        Dim srcHandle = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim boxPtr = FitEllipse_AMS(srcHandle.AddrOfPinnedObject(), inputPoints.Count)
        srcHandle.Free()

        Dim ellipses(5 - 1) As Single
        Marshal.Copy(boxPtr, ellipses, 0, ellipses.Length)

        Dim angle = ellipses(0)
        Dim center = lpData.validatePoint(New cv.Point2f(ellipses(1), ellipses(2)))
        Dim size As New cv.Size2f(ellipses(3), ellipses(4))
        If Single.IsNaN(ellipses(3)) Or Single.IsNaN(ellipses(4)) Then Exit Sub ' one of the random points is the same
        If size.Width < task.lineWidth + 1 Or size.Height < task.lineWidth + 1 Then Exit Sub

        Dim box = New cv.RotatedRect(center, size, angle)
        cv.Cv2.Ellipse(dst2, box, cv.Scalar.Yellow, task.lineWidth, task.lineType)
    End Sub
End Class






' https://docs.opencvb.org/3.4.2/de/dc7/fitellipse_8cpp-example.html
Public Class XR_FitEllipse_Direct_CPP : Inherits TaskParent
    Dim options As New Options_MinArea
    Public Sub New()
        labels(2) = "The FitEllipse_Direct C++ "
        desc = "Use FitEllipse to draw around a set of points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If Not task.heartBeat Then Exit Sub
        options.Run()
        Dim dataSrc(options.srcPoints.Count * 2 - 1) As Single

        dst2.SetTo(0)
        For Each pt In options.srcPoints
            Circle(dst2, pt, task.DotSize, white, -1, task.lineType)
        Next

        Dim input As cv.Mat = cv.Mat.FromPixelData(options.srcPoints.Count, 1, cv.MatType.CV_32FC2, options.srcPoints.ToArray)
        Marshal.Copy(input.Data, dataSrc, 0, dataSrc.Length)

        Dim srcHandle = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim boxPtr = FitEllipse_Direct(srcHandle.AddrOfPinnedObject(), options.srcPoints.Count)
        srcHandle.Free()

        Dim ellipses(5 - 1) As Single
        Marshal.Copy(boxPtr, ellipses, 0, ellipses.Length)

        Dim angle = ellipses(0)
        Dim center As New cv.Point2f(ellipses(1), ellipses(2))
        Dim size As New cv.Size2f(ellipses(3), ellipses(4))
        If size.Width < task.lineWidth + 1 Or size.Height < task.lineWidth + 1 Then Exit Sub

        Dim box = New cv.RotatedRect(center, size, angle)
        cv.Cv2.Ellipse(dst2, box, cv.Scalar.Yellow, task.lineWidth, task.lineType)
    End Sub
End Class







Public Class XR_FitEllipse_RedCloud : Inherits TaskParent
    Dim fitE As New FitEllipse_Basics
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Create an ellipse from a contour"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        SetTrueText(strOut, 3)

        If Not task.heartBeat Then Exit Sub

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        SetTrueText(redC.strOut, 3)

        If task.rcD.contour Is Nothing Then task.rcD = redC.rcList(0)
        fitE.inputPoints.Clear()
        For Each pt In task.rcD.contour
            fitE.inputPoints.Add(New cv.Point2f(pt.X, pt.Y))
        Next
        fitE.Run(src)
        dst3.SetTo(0)
        dst3(task.rcD.rect).SetTo(white, task.rcD.mask)
        DrawRect(dst3, task.rcD.rect, white)
        cv.Cv2.Ellipse(dst3(task.rcD.rect), fitE.box, cv.Scalar.Yellow, task.lineWidth, task.lineType)
    End Sub
End Class






Public Class XR_FitEllipse_Rectangle : Inherits TaskParent
    Public noisyLine As New Eigen_Input
    Public rect As cv.RotatedRect
    Public vertices() As cv.Point2f
    Public ptList As New List(Of cv.Point2f)
    Public Sub New()
        desc = "Get a rectangle to fit a set of points by first fitting an ellipse."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            If task.heartBeatLT = False Then
                Exit Sub
            Else
                noisyLine.Run(src)
                ptList = New List(Of cv.Point2f)(noisyLine.PointList)
                dst2 = noisyLine.dst2
            End If
        End If

        rect = FitEllipse(ptList)
        vertices = rect.Points()
        For i = 0 To vertices.Count - 1
            cv.Cv2.Line(dst2, vertices(i), vertices((i + 1) Mod 4), 255, task.lineWidth, task.lineType)
        Next
    End Sub
End Class

