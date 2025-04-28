Imports OpenCvSharp
Imports cv = OpenCvSharp
Public Class Line3D_Basics : Inherits TaskParent
    Dim lines As New Line_Basics
    Public lines3D As New List(Of cv.Point3f)
    Public lines3DMat As New cv.Mat
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find all the lines in 3D using the structured slices through the bricks."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone

        lines.Run(src)
        dst2 = lines.dst2
        labels(2) = lines.labels(2)

        Static brickList As New List(Of gcData)(task.brickList)

        For i = 0 To task.gridRects.Count - 1
            Dim gc = task.brickList(i)
            Dim val = task.motionMask.Get(Of Byte)(gc.center.Y, gc.center.X)
            If val Then brickList(i) = gc
        Next

        lines3D.Clear()

        For Each lp In task.lpList
            Dim gc1 = brickList(task.brickMap.Get(Of Single)(lp.p1.Y, lp.p1.X))
            If gc1.depth = 0 Then Continue For

            Dim gc2 = brickList(task.brickMap.Get(Of Single)(lp.p2.Y, lp.p2.X))
            If gc2.depth = 0 Then Continue For

            lines3D.Add(New cv.Point3f(0, 0.9, 0.9))
            Dim p1 = getWorldCoordinates(gc1.center, gc1.depth)
            Dim p2 = getWorldCoordinates(gc2.center, gc2.depth)
            'p1.Z -= 0.5
            'p2.Z -= 0.5 ' so the line will appear in front of the pointcloud data by 0.5 meter
            lines3D.Add(p1)
            lines3D.Add(p2)
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link8)
        Next

        lines3DMat = cv.Mat.FromPixelData(lines3D.Count / 3, 1, cv.MatType.CV_32FC3, lines3D.ToArray)

        If task.heartBeat Then
            strOut = CStr(lines3D.Count / 3) + " 3D lines are prepared in lines3D." + vbCrLf +
                     CStr(task.lpList.Count - lines3D.Count / 3) + " lines occurred in areas with no depth and were skipped."
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class Line3D_Draw : Inherits TaskParent
    Public p1 As cv.Point, p2 As cv.Point
    Dim plot As New Plot_OverTimeScalar
    Dim toggleFirstSecond As Boolean
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        plot.plotCount = 2

        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))

        p1 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        p2 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        labels(2) = "Click twice in the image below to draw a line and that line's depth is correlated in X to Z and Y to Z in the plot at right"
        desc = "Determine where a 3D line is close to the real depth data"
    End Sub
    Private Function findCorrelation(pts1 As cv.Mat, pts2 As cv.Mat) As Single
        Dim correlationMat As New cv.Mat
        cv.Cv2.MatchTemplate(pts1, pts2, correlationMat, cv.TemplateMatchModes.CCoeffNormed)
        Return correlationMat.Get(Of Single)(0, 0)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            If task.mouseClickFlag Then
                If toggleFirstSecond = False Then
                    p1 = task.ClickPoint
                Else
                    p2 = task.ClickPoint
                    toggleFirstSecond = False
                End If
            End If
        End If

        If toggleFirstSecond Then Exit Sub ' wait until the second point is selected...

        dst1 = src
        DrawLine(dst1, p1, p2, task.highlight)
        dst0.SetTo(0)
        DrawLine(dst0, p1, p2, 255)
        dst1.SetTo(0)
        task.pcSplit(0).CopyTo(dst1, dst0)
        Dim points = dst1.FindNonZero()

        Dim nextList As New List(Of cv.Point3f)
        For i = 0 To points.Rows - 1
            Dim pt = points.Get(Of cv.Point)(i, 0)
            nextList.Add(task.pointCloud.Get(Of cv.Point3f)(pt.Y, pt.X))
        Next
        If nextList.Count = 0 Then Exit Sub ' line is completely in area with no depth.

        Dim pts As cv.Mat = cv.Mat.FromPixelData(nextList.Count, 1, cv.MatType.CV_32FC3, nextList.ToArray)
        Dim zSplit = pts.Split()
        Dim c1 = findCorrelation(zSplit(0), zSplit(2))
        Dim c2 = findCorrelation(zSplit(1), zSplit(2))

        plot.plotData = New cv.Scalar(c1, c2, 0)

        plot.Run(src)
        dst2 = plot.dst2
        dst3 = plot.dst3
        labels(3) = "using " + CStr(nextList.Count) + " points, the correlation of X to Z = " + Format(c1, fmt3) + " (blue), correlation of Y to Z = " + Format(c2, fmt3) + " (green)"
    End Sub
End Class







Public Class Line3D_Constructed : Inherits TaskParent
    Dim lines As New Line3D_Basics
    Public Sub New()
        desc = "Build the 3D lines found in Line3D_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2
        dst3 = lines.dst3
        labels(2) = lines.labels(2)
    End Sub
End Class
