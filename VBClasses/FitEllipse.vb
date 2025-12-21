Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Runtime.Remoting
' https://docs.opencvb.org/3.4.2/de/dc7/fitellipse_8cpp-example.html
Namespace VBClasses
    Public Class FitEllipse_Basics : Inherits TaskParent
        Dim options As New Options_MinArea
        Public inputPoints As New List(Of cv.Point2f)
        Public box As cv.RotatedRect
        Public vertices() As cv.Point2f
        Public Sub New()
            desc = "Use FitEllipse OpenCV API to draw around a set of points"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If Not taskAlg.heartBeat Then Exit Sub
            If standaloneTest() Then
                options.Run()
                inputPoints = options.srcPoints
            End If

            dst2.SetTo(0)
            For Each pt In inputPoints
                DrawCircle(dst2, pt, taskAlg.DotSize, white)
            Next

            If inputPoints.Count > 4 Then
                box = cv.Cv2.FitEllipse(inputPoints)
                vertices = box.Points()
                If standaloneTest() Then
                    For i = 0 To vertices.Count - 1
                        If Single.IsNaN(vertices(i).X) Or Single.IsNaN(vertices(i).Y) Then Exit Sub ' can't draw the result...
                        vbc.DrawLine(dst2, vertices(i), vertices((i + 1) Mod 4), cv.Scalar.Green)
                    Next
                    dst2.Ellipse(box, cv.Scalar.Green, taskAlg.lineWidth, taskAlg.lineType)
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
        Public Overrides Sub RunAlg(src As cv.Mat)
            If Not taskAlg.heartBeat Then Exit Sub
            If standaloneTest() Then
                options.Run()
                inputPoints = options.srcPoints
            End If
            dst2.SetTo(0)
            For Each pt In inputPoints
                DrawCircle(dst2, pt, taskAlg.DotSize, white)
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
            Dim center = lpData.validatePoint(New cv.Point2f(ellipse(1), ellipse(2)))
            Dim size As New cv.Size2f(ellipse(3), ellipse(4))
            If Single.IsNaN(ellipse(3)) Or Single.IsNaN(ellipse(4)) Then Exit Sub ' one of the random points is the same
            If size.Width < taskAlg.lineWidth + 1 Or size.Height < taskAlg.lineWidth + 1 Then Exit Sub

            Dim box = New cv.RotatedRect(center, size, angle)
            dst2.Ellipse(box, cv.Scalar.Yellow, taskAlg.lineWidth, taskAlg.lineType)
        End Sub
    End Class






    ' https://docs.opencvb.org/3.4.2/de/dc7/fitellipse_8cpp-example.html
    Public Class FitEllipse_Direct_CPP : Inherits TaskParent
        Dim options As New Options_MinArea
        Public Sub New()
            labels(2) = "The FitEllipse_Direct C++ "
            desc = "Use FitEllipse to draw around a set of points"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If Not taskAlg.heartBeat Then Exit Sub
            options.Run()
            Dim dataSrc(options.srcPoints.Count * 2 - 1) As Single

            dst2.SetTo(0)
            For Each pt In options.srcPoints
                DrawCircle(dst2, pt, taskAlg.DotSize, white)
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
            If size.Width < taskAlg.lineWidth + 1 Or size.Height < taskAlg.lineWidth + 1 Then Exit Sub

            Dim box = New cv.RotatedRect(center, size, angle)
            dst2.Ellipse(box, cv.Scalar.Yellow, taskAlg.lineWidth, taskAlg.lineType)
        End Sub
    End Class







    Public Class FitEllipse_RedCloud : Inherits TaskParent
        Dim fitE As New FitEllipse_Basics
        Public Sub New()
            desc = "Create an ellipse from a contour"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If Not taskAlg.heartBeat Then Exit Sub
            dst2 = runRedList(src, labels(2))

            If taskAlg.oldrcD.contour Is Nothing Then Exit Sub
            fitE.inputPoints.Clear()
            For Each pt In taskAlg.oldrcD.contour
                fitE.inputPoints.Add(New cv.Point2f(pt.X, pt.Y))
            Next
            fitE.Run(src)
            dst3.SetTo(0)
            dst3(taskAlg.oldrcD.rect).SetTo(white, taskAlg.oldrcD.mask)
            DrawRect(dst3, taskAlg.oldrcD.rect, white)
            dst3(taskAlg.oldrcD.rect).Ellipse(fitE.box, cv.Scalar.Yellow, taskAlg.lineWidth, taskAlg.lineType)
        End Sub
    End Class






    Public Class FitEllipse_Rectangle : Inherits TaskParent
        Public noisyLine As New Eigen_Input
        Public rect As cv.RotatedRect
        Public vertices() As cv.Point2f
        Public ptList As New List(Of cv.Point2f)
        Public Sub New()
            desc = "Get a rectangle to fit a set of points by first fitting an ellipse."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then
                If taskAlg.heartBeatLT = False Then
                    Exit Sub
                Else
                    noisyLine.Run(src)
                    ptList = New List(Of cv.Point2f)(noisyLine.PointList)
                    dst2 = noisyLine.dst2
                End If
            End If

            rect = cv.Cv2.FitEllipse(ptList)
            vertices = rect.Points()
            For i = 0 To vertices.Count - 1
                vbc.DrawLine(dst2, vertices(i), vertices((i + 1) Mod 4), 255)
            Next
        End Sub
    End Class

End Namespace