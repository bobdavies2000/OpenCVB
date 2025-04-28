Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class FitLine_Basics : Inherits TaskParent
    Dim fitE As New FitEllipse_Rectangle
    Public ptList As New List(Of cv.Point2f)
    Public lp As New lpData
    Public Sub New()
        desc = "Use FitEllipse to build the FitLine solution."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone And task.heartBeatLT Then
            Static noisyLine As New Eigen_Input
            noisyLine.Run(src)
            ptList = New List(Of cv.Point2f)(noisyLine.PointList)
            dst2 = noisyLine.dst2
        End If

        Dim rect = cv.Cv2.FitEllipse(ptList)
        Dim v = rect.Points()

        Dim p1 = New cv.Point((v(0).X + v(3).X) / 2, (v(0).Y + v(3).Y) / 2)
        Dim p2 = New cv.Point((v(1).X + v(2).X) / 2, (v(1).Y + v(2).Y) / 2)
        lp = New lpData(p1, p2)
        dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
    End Sub
End Class






Public Class FitLine_Conventional : Inherits TaskParent
    Public lp As lpData
    Public ptList As New List(Of cv.Point2f)
    Public center As cv.Point2f
    Public Sub New()
        desc = "Show how Fitline API works with simple data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone And task.heartBeatLT Then
            Static noisyLine As New Eigen_Input
            noisyLine.Run(src)
            ptList = New List(Of cv.Point2f)(noisyLine.PointList)
            dst2 = noisyLine.dst2
        End If

        Dim line2d = cv.Cv2.FitLine(ptList, cv.DistanceTypes.L2, 0, 0, 0)
        center = New cv.Point2f(line2d.X1, line2d.Y1)
        Dim slope = line2d.Vy / line2d.Vx
        Dim leftY = Math.Round(-line2d.X1 * slope + line2d.Y1)
        Dim rightY = Math.Round((src.Cols - line2d.X1) * slope + line2d.Y1)
        lp = New lpData(New cv.Point(0, leftY), New cv.Point(src.Cols - 1, rightY))
        If standaloneTest() Then
            dst2.SetTo(0)
            DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Red)
        End If
    End Sub
End Class






' https://docs.opencvb.org/3.4/js_contour_features_fitLine.html
Public Class FitLine_Lines : Inherits TaskParent
    Dim options As New Options_FitLine
    Public draw As New Draw_Lines
    Public lines As New List(Of cv.Point)
    Public Sub New()
        optiBase.FindSlider("DrawCount").Value = 2
        labels(2) = "If the contours overlap, then one line the trendline for both is found.  Otherwise, 2 lines are found."
        labels(3) = "FitLine_Basics contour input - if they overlap, a trendline will be found."
        desc = "Show how Fitline API works.  When the lines overlap the image has a single contour and the lines are occasionally not found."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If Not task.heartBeatLT Then Exit Sub
        options.Run()

        If standaloneTest() Then
            draw.Run(src)
            dst3 = draw.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)
            dst2 = dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Else
            lines.Clear()
        End If

        Dim contours As cv.Point()()
        contours = cv.Cv2.FindContoursAsArray(dst3, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        For i = 0 To contours.Length - 1
            Dim cnt = contours(i)
            Dim line2d = cv.Cv2.FitLine(cnt, cv.DistanceTypes.L2, 0, options.radiusAccuracy, options.angleAccuracy)
            Dim slope = line2d.Vy / line2d.Vx
            Dim leftY = Math.Round(-line2d.X1 * slope + line2d.Y1)
            Dim rightY = Math.Round((src.Cols - line2d.X1) * slope + line2d.Y1)
            Dim p1 = New cv.Point(0, leftY)
            Dim p2 = New cv.Point(src.Cols - 1, rightY)
            If standaloneTest() Then
                lines.Add(p1)
                lines.Add(p2)
            End If
            DrawLine(dst2, p1, p2, cv.Scalar.Red)
        Next
    End Sub
End Class






Public Class FitLine_Simple3D : Inherits TaskParent
    Public ptList As New List(Of cv.Point3f)
    Public lpResult As lpData
    Public center As cv.Point2f
    Dim options As New Options_FitLine
    Public Sub New()
        labels(2) = "With only a few points, resulting vector can be anywhere but it will be horizontal with uniformly distributed points."
        desc = "A simple demo of using fitline with uniformly distributed 3D points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If standalone Then
            Static random As New Random_Basics3D
            If task.firstPass Then optiBase.FindSlider("Random Pixel Count").Value = 200
            random.Run(src)
            dst2.SetTo(0)
            ptList.Clear()
            For Each pt In random.PointList
                DrawCircle(dst2, New cv.Point2f(pt.X, pt.Y), task.DotSize, cv.Scalar.Yellow)
                ptList.Add(pt)
            Next
        End If

        ' Fit a line to the 3D points
        Dim line = cv.Cv2.FitLine(ptList.ToArray, cv.DistanceTypes.L2, 0, 0, 0)
        center = New cv.Point2f(line.X1, line.Y1)

        Dim p2 = New cv.Point(dst2.Width, -dst2.Width * line.Vy / line.Vx + center.Y)
        Dim lp = New lpData(center, p2)
        lpResult = findEdgePoints(lp)
        dst2.Line(lpResult.p1, lpResult.p2, task.highlight, task.lineWidth, task.lineType)
        dst2.Circle(center, task.DotSize + 2, cv.Scalar.Blue, -1)
    End Sub
End Class






Public Class FitLine_Example2D : Inherits TaskParent
    Dim noisyLine As New Eigen_Input
    Dim fitLine As New FitLine_Conventional
    Public Sub New()
        desc = "A way to test the fitline using 3D data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone And task.heartBeat = False Then Exit Sub
        noisyLine.Run(src)
        dst2 = noisyLine.dst2

        fitLine.ptList.Clear()
        For Each pt In noisyLine.PointList
            fitLine.ptList.Add(New cv.Point2f(pt.X, pt.Y))
        Next
        fitLine.Run(src)

        dst2.Line(fitLine.lp.p1, fitLine.lp.p2, task.highlight, task.lineWidth, task.lineType)
        dst2.Circle(fitLine.center, task.DotSize + 2, cv.Scalar.Blue, -1)
    End Sub
End Class





Public Class FitLine_Basics3D : Inherits TaskParent
    Public ptList As New List(Of cv.Point3f)
    Public lp As lpData
    Public center As cv.Point2f
    Public Sub New()
        labels(2) = "The input is a noisy trendline but the result should track pretty well."
        desc = "Use fitline to find the trendline in the input data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            Static noisyLine As New Eigen_Input3D
            If task.heartBeat = False Then Exit Sub
            noisyLine.Run(src)
            dst2.SetTo(0)
            ptList.Clear()
            For Each pt In noisyLine.PointList
                DrawCircle(dst2, New cv.Point2f(pt.X, pt.Y), task.DotSize, task.highlight)
                ptList.Add(pt)
            Next
        End If

        ' Fit a line to the 3D points
        Dim line = cv.Cv2.FitLine(ptList.ToArray, cv.DistanceTypes.L2, 0, 0, 0)
        Dim m = line.Vy / line.Vx
        Dim bb = line.Y1 - m * line.X1
        Dim p1 = New cv.Point(line.X1, line.Y1)
        Dim p2 = New cv.Point(src.Width, line.Vy / line.Vx * src.Width + bb)

        lp = findEdgePoints(New lpData(p1, p2))
        dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
        dst2.Circle(New cv.Point2f(line.X1, line.Y1), task.DotSize + 2, cv.Scalar.Blue, -1)
    End Sub
End Class






Public Class FitLine_Grid : Inherits TaskParent
    Dim nZero As New FindNonZero_Basics
    Dim edges As New Edge_Basics
    Dim fitline As New FitLine_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find lines within each grid cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(src)
        dst2 = edges.dst2

        dst3.SetTo(0)
        For Each gc In task.brickList
            If dst2(gc.rect).CountNonZero >= 5 Then
                nZero.Run(dst2(gc.rect))

                fitline.ptList.Clear()
                For i = 0 To nZero.ptMat.Rows - 1
                    fitline.ptList.Add(nZero.ptMat.Get(Of cv.Point)(i, 0))
                Next

                fitline.Run(dst2(gc.rect))
                dst3(gc.rect).Line(fitline.lp.p1, fitline.lp.p2, 255, task.lineWidth, task.lineType)
            End If
        Next
    End Sub
End Class
