Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Area_MinTriangle_CPP_VB : Inherits TaskParent
    Public triangle As cvb.Mat
    Public options As New Options_MinArea
    Public srcPoints As List(Of cvb.Point2f)
    Public Sub New()
        desc = "Find minimum containing triangle for a set of points."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        options.RunOpt()
        If task.heartBeat Then
            srcPoints = New List(Of cvb.Point2f)(options.srcPoints)
        Else
            If srcPoints.Count < 3 Then Exit Sub ' not enough points
        End If

        Dim dataSrc(srcPoints.Count * 2 - 1) As Single ' input is a list of points.
        Dim dstData(3 * 2 - 1) As Single ' minTriangle returns 3 points

        dst2.SetTo(white)

        Dim input As cvb.Mat = cvb.Mat.FromPixelData(1, srcPoints.Count, cvb.MatType.CV_32FC2, srcPoints.ToArray)
        Marshal.Copy(input.Data, dataSrc, 0, dataSrc.Length)
        Dim srcHandle = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim dstHandle = GCHandle.Alloc(dstData, GCHandleType.Pinned)
        MinTriangle_Run(srcHandle.AddrOfPinnedObject(), srcPoints.Count, dstHandle.AddrOfPinnedObject)
        srcHandle.Free()
        dstHandle.Free()
        triangle = cvb.Mat.FromPixelData(3, 1, cvb.MatType.CV_32FC2, dstData)

        For i = 0 To 2
            Dim pt = triangle.Get(Of cvb.Point2f)(i)
            Dim p1 = New cvb.Point(pt.X, pt.Y)
            pt = triangle.Get(Of cvb.Point2f)((i + 1) Mod 3)
            Dim p2 = New cvb.Point(pt.X, pt.Y)
            DrawLine(dst2, p1, p2, cvb.Scalar.Black)
        Next

        For Each pt In srcPoints
            DrawCircle(dst2, pt, task.DotSize + 1, cvb.Scalar.Red)
        Next
    End Sub
End Class






Public Class Area_MinMotionRect : Inherits TaskParent
    Dim bgSub As New BGSubtract_Basics
    Public Sub New()
        FindSlider("MOG Learn Rate X1000").Value = 100 ' low threshold to maximize motion
        desc = "Use minRectArea to encompass detected motion"
        labels(2) = "MinRectArea of MOG motion"
    End Sub

    Private Function motionRectangles(gray As cvb.Mat, colors() As cvb.Vec3b) As cvb.Mat
        Dim contours As cvb.Point()()
        contours = cvb.Cv2.FindContoursAsArray(gray, cvb.RetrievalModes.Tree, cvb.ContourApproximationModes.ApproxSimple)

        For i = 0 To contours.Length - 1
            Dim minRect = cvb.Cv2.MinAreaRect(contours(i))
            Dim nextColor = New cvb.Scalar(colors(i Mod 256)(0), colors(i Mod 256)(1), colors(i Mod 256)(2))
            DrawRotatedRect(minRect, gray, nextColor)
        Next
        Return gray
    End Function
    Public Overrides sub runAlg(src As cvb.Mat)
        bgSub.Run(src)
        Dim gray As cvb.Mat
        If bgSub.dst2.Channels() = 1 Then gray = bgSub.dst2 Else gray = bgSub.dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        dst2 = motionRectangles(gray, task.vecColors)
        dst2.SetTo(cvb.Scalar.All(255), gray)
    End Sub
End Class







Public Class Area_FindNonZero : Inherits TaskParent
    Public nonZero As cvb.Mat
    Public Sub New()
        labels(2) = "Coordinates of non-zero points"
        labels(3) = "Non-zero original points"
        desc = "Use FindNonZero API to get coordinates of non-zero points."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        If standalone Then
            src = New cvb.Mat(src.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
            Dim srcPoints(100 - 1) As cvb.Point ' doesn't really matter how many there are.
            For i = 0 To srcPoints.Length - 1
                srcPoints(i).X = msRNG.Next(0, src.Width)
                srcPoints(i).Y = msRNG.Next(0, src.Height)
                src.Set(Of Byte)(srcPoints(i).Y, srcPoints(i).X, 255)
            Next
        End If

        nonZero = src.FindNonZero()

        dst3 = New cvb.Mat(src.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        ' mark the points so they are visible...
        For i = 0 To nonZero.Rows - 1
            DrawCircle(dst3, nonZero.Get(Of cvb.Point)(0, i), task.DotSize, white)
        Next

        Dim outstr As String = "Coordinates of the non-zero points (ordered by row - top to bottom): " + vbCrLf + vbCrLf
        For i = 0 To nonZero.Rows - 1
            Dim pt = nonZero.Get(Of cvb.Point)(0, i)
            outstr += "X = " + vbTab + CStr(pt.X) + vbTab + " y = " + vbTab + CStr(pt.Y) + vbCrLf
            If i > 100 Then Exit For ' for when there are way too many points found...
        Next
        SetTrueText(outstr)
    End Sub
End Class







Public Class Area_SoloPoints : Inherits TaskParent
    Dim hotTop As New BackProject_SoloTop
    Dim hotSide As New BackProject_SoloSide
    Dim nZero As New Area_FindNonZero
    Public soloPoints As New List(Of cvb.Point)
    Public Sub New()
        desc = "Find the solo points in the pointcloud histograms for top and side views."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        hotTop.Run(src)
        dst2 = hotTop.dst3

        hotSide.Run(src)
        dst2 = dst2 Or hotSide.dst3

        nZero.Run(dst2)
        soloPoints.Clear()
        For i = 0 To nZero.nonZero.Rows - 1
            soloPoints.Add(nZero.nonZero.Get(Of cvb.Point)(i, 0))
        Next

        If task.heartBeat Then labels(2) = $"There were {soloPoints.Count} points found"
    End Sub
End Class






Public Class Area_MinRect : Inherits TaskParent
    Public minRect As cvb.RotatedRect
    Dim options As New Options_MinArea
    Public inputPoints As New List(Of cvb.Point2f)
    Public Sub New()
        desc = "Find minimum containing rectangle for a set of points."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        If standaloneTest() Then
            If Not task.heartBeat Then Exit Sub
            options.RunOpt()
            inputPoints = quickRandomPoints(options.numPoints)
        End If

        minRect = cvb.Cv2.MinAreaRect(inputPoints.ToArray)

        If standaloneTest() Then
            dst2.SetTo(0)
            For Each pt In inputPoints
                DrawCircle(dst2, pt, task.DotSize + 2, cvb.Scalar.Red)
            Next
            DrawRotatedOutline(minRect, dst2, cvb.Scalar.Yellow)
        End If
    End Sub
End Class