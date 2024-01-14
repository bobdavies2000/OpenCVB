Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Windows.Media.Media3D

Public Class Area_MinTriangle_CPP : Inherits VB_Algorithm
    Public triangle As cv.Mat
    Public options As New Options_MinArea
    Public srcPoints As List(Of cv.Point2f)
    Public Sub New()
        desc = "Find minimum containing triangle for a set of points."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If heartBeat() Then
            srcPoints = New List(Of cv.Point2f)(options.srcPoints)
        Else
            If srcPoints.Count < 3 Then Exit Sub ' not enough points
        End If

        Dim dataSrc(srcPoints.Count * Marshal.SizeOf(Of Integer) * 2 - 1) As Single ' input is a list of points.
        Dim dstData(3 * Marshal.SizeOf(Of Integer) * 2 - 1) As Single ' minTriangle returns 3 points

        dst2.SetTo(cv.Scalar.White)

        Dim input As New cv.Mat(srcPoints.Count, 1, cv.MatType.CV_32FC2, srcPoints.ToArray)
        Marshal.Copy(input.Data, dataSrc, 0, dataSrc.Length)
        Dim srcHandle = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim dstHandle = GCHandle.Alloc(dstData, GCHandleType.Pinned)
        MinTriangle_Run(srcHandle.AddrOfPinnedObject(), srcPoints.Count, dstHandle.AddrOfPinnedObject)
        srcHandle.Free()
        dstHandle.Free()
        triangle = New cv.Mat(3, 1, cv.MatType.CV_32FC2, dstData)

        For i = 0 To 2
            Dim p1 = triangle.Get(Of cv.Point2f)(i)
            Dim p2 = triangle.Get(Of cv.Point2f)((i + 1) Mod 3)
            dst2.Line(p1, p2, cv.Scalar.Black, task.lineWidth, task.lineType)
        Next

        For Each pt In srcPoints
            dst2.Circle(pt, task.dotSize + 1, cv.Scalar.Red, -1, task.lineType)
        Next
    End Sub
End Class






Public Class Area_MinMotionRect : Inherits VB_Algorithm
    Dim bgSub As New BGSubtract_MOG
    Public Sub New()
        findSlider("MOG Learn Rate X1000").Value = 100 ' low threshold to maximize motion
        desc = "Use minRectArea to encompass detected motion"
        labels(2) = "MinRectArea of MOG motion"
    End Sub

    Private Function motionRectangles(gray As cv.Mat, colors() As cv.Vec3b) As cv.Mat
        Dim contours As cv.Point()()
        contours = cv.Cv2.FindContoursAsArray(gray, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)

        For i = 0 To contours.Length - 1
            Dim minRect = cv.Cv2.MinAreaRect(contours(i))
            Dim nextColor = New cv.Scalar(colors(i Mod 256)(0), colors(i Mod 256)(1), colors(i Mod 256)(2))
            drawRotatedRectangle(minRect, gray, nextColor)
        Next
        Return gray
    End Function
    Public Sub RunVB(src As cv.Mat)
        bgSub.Run(src)
        Dim gray As cv.Mat
        If bgSub.dst2.Channels = 1 Then gray = bgSub.dst2 Else gray = bgSub.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = motionRectangles(gray, task.vecColors)
        dst2.SetTo(cv.Scalar.All(255), gray)
    End Sub
End Class







Public Class Area_FindNonZero : Inherits VB_Algorithm
    Public nonZero As cv.Mat
    Public Sub New()
        labels(2) = "Coordinates of non-zero points"
        labels(3) = "Non-zero original points"
        desc = "Use FindNonZero API to get coordinates of non-zero points."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            src = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
            Dim srcPoints(100 - 1) As cv.Point ' doesn't really matter how many there are.
            For i = 0 To srcPoints.Length - 1
                srcPoints(i).X = msRNG.Next(0, src.Width)
                srcPoints(i).Y = msRNG.Next(0, src.Height)
                src.Set(Of Byte)(srcPoints(i).Y, srcPoints(i).X, 255)
            Next
        End If

        nonZero = src.FindNonZero()

        dst3 = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
        ' mark the points so they are visible...
        For i = 0 To nonZero.Rows - 1
            dst3.Circle(nonZero.Get(Of cv.Point)(0, i), task.dotSize, cv.Scalar.White, -1, task.lineType)
        Next

        Dim outstr As String = "Coordinates of the non-zero points (ordered by row - top to bottom): " + vbCrLf + vbCrLf
        For i = 0 To nonZero.Rows - 1
            Dim pt = nonZero.Get(Of cv.Point)(0, i)
            outstr += "X = " + vbTab + CStr(pt.X) + vbTab + " y = " + vbTab + CStr(pt.Y) + vbCrLf
            If i > 100 Then Exit For ' for when there are way too many points found...
        Next
        setTrueText(outstr)
    End Sub
End Class







Public Class Area_SoloPoints : Inherits VB_Algorithm
    Dim hotTop As New BackProject_SoloTop
    Dim hotSide As New BackProject_SoloSide
    Dim nZero As New Area_FindNonZero
    Public soloPoints As New List(Of cv.Point)
    Public Sub New()
        desc = "Find the solo points in the pointcloud histograms for top and side views."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hotTop.Run(src)
        dst2 = hotTop.dst3

        hotSide.Run(src)
        dst2 = dst2 Or hotSide.dst3

        nZero.Run(dst2)
        soloPoints.Clear()
        For i = 0 To nZero.nonZero.Rows - 1
            soloPoints.Add(nZero.nonZero.Get(Of cv.Point)(i, 0))
        Next

        labels(2) = CStr(soloPoints.Count) + " points found"
    End Sub
End Class






Public Class Area_MinRect : Inherits VB_Algorithm
    Public minRect As cv.RotatedRect
    Dim options As New Options_MinArea
    Public inputPoints As New List(Of cv.Point2f)
    Public Sub New()
        desc = "Find minimum containing rectangle for a set of points."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            If heartBeat() = False Then Exit Sub
            options.RunVB()
            inputPoints = quickRandomPoints(options.numPoints)
        End If

        minRect = cv.Cv2.MinAreaRect(inputPoints.ToArray)

        If standalone Or showIntermediate() Then
            dst2.SetTo(0)
            For Each pt In inputPoints
                dst2.Circle(pt, task.dotSize + 2, cv.Scalar.Red, -1, task.lineType)
            Next
            drawRotatedOutline(minRect, dst2, cv.Scalar.Yellow)
        End If
    End Sub
End Class