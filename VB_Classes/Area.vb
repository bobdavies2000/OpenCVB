Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module MinTriangle_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub MinTriangle_Run(inputPtr As IntPtr, numberOfPoints As integer, outputTriangle As IntPtr)
    End Sub
End Module

Public Class Area_MinTriangle_CPP : Inherits VBparent
    Dim numberOfPoints As integer
    Public srcPoints() As cv.Point2f
    Public srcData() As Byte
    Public dstData() As Byte
    Public triangle As cv.Mat
    Private Sub setup()
        numberOfPoints = sliders.trackbar(0).Value
        ReDim srcPoints(numberOfPoints)
        ReDim srcData(numberOfPoints * Marshal.SizeOf(numberOfPoints) * 2 - 1) ' input is a list of points.
        ReDim dstData(3 * Marshal.SizeOf(numberOfPoints) * 2 - 1) ' minTriangle returns 3 points
    End Sub
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Area Number of Points", 1, 30, 5)
            sliders.setupTrackBar(1, "Area size", 10, 300, 200)
        End If
        setup()
        task.desc = "Find minimum containing triangle for a set of points."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static pointCountSlider = findSlider("Area Number of Points")
        Static sizeSlider = findSlider("Area size")
        If numberOfPoints <> pointCountSlider.Value Then setup()
        Dim squareWidth = sizeSlider.Value / 2

        dst2.SetTo(0)
        For i = 0 To srcPoints.Length - 1
            srcPoints(i).X = msRNG.Next(src.Width / 2 - squareWidth, src.Width / 2 + squareWidth)
            srcPoints(i).Y = msRNG.Next(src.Height / 2 - squareWidth, src.Height / 2 + squareWidth)
            dst2.Circle(srcPoints(i), task.dotSize, cv.Scalar.White, -1, task.lineType)
        Next

        Dim input As New cv.Mat(numberOfPoints, 1, cv.MatType.CV_32FC2, srcPoints)
        Marshal.Copy(input.Data, srcData, 0, srcData.Length)
        Dim srcHandle = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim dstHandle = GCHandle.Alloc(dstData, GCHandleType.Pinned)
        MinTriangle_Run(srcHandle.AddrOfPinnedObject(), numberOfPoints, dstHandle.AddrOfPinnedObject)
        srcHandle.Free() ' free the pinned memory...
        dstHandle.Free()
        triangle = New cv.Mat(3, 1, cv.MatType.CV_32FC2, dstData)

        For i = 0 To 2
            Dim p1 = triangle.Get(Of cv.Point2f)(i)
            Dim p2 = triangle.Get(Of cv.Point2f)((i + 1) Mod 3)
            dst2.Line(p1, p2, cv.Scalar.White, task.lineWidth + 1, task.lineType)
        Next
    End Sub
End Class




Public Class Area_MinRect : Inherits VBparent
    Dim numberOfPoints As Integer
    Public srcPoints() As cv.Point2f
    Public minRect As cv.RotatedRect
    Private Sub setup(_numberOfPoints As Integer)
        numberOfPoints = _numberOfPoints
        ReDim srcPoints(numberOfPoints)
    End Sub
    Public Sub New()

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Area Number of Points", 1, 30, 5)
            sliders.setupTrackBar(1, "Area size", 10, 300, 200)
        End If

        setup(sliders.trackbar(0).Value)

        task.desc = "Find minimum containing rectangle for a set of points."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static pointCountSlider = findSlider("Area Number of Points")
        Static sizeSlider = findSlider("Area size")
        If numberOfPoints <> pointCountSlider.Value Then setup(pointCountSlider.value)
        Dim squareWidth = sizeSlider.Value / 2

        dst2.SetTo(0)
        For i = 0 To srcPoints.Length - 1
            srcPoints(i).X = msRNG.Next(src.Width / 2 - squareWidth, src.Width / 2 + squareWidth)
            srcPoints(i).Y = msRNG.Next(src.Height / 2 - squareWidth, src.Height / 2 + squareWidth)
            dst2.Circle(srcPoints(i), task.dotSize, cv.Scalar.White, -1, task.lineType)
        Next

        minRect = cv.Cv2.MinAreaRect(srcPoints)
        drawRotatedRectangle(minRect, dst2, cv.Scalar.Yellow)
    End Sub
End Class





Public Class Area_MinMotionRect : Inherits VBparent
    Dim bgSub As New BGSubtract_MOG
    Public Sub New()
        findSlider("MOG Learn Rate").Value = 100 ' low threshold to maximize motion
        task.desc = "Use minRectArea to encompass detected motion"
        labels(2) = "MinRectArea of MOG motion"
    End Sub

    Private Function motionRectangles(gray As cv.Mat, colors() As cv.Vec3b) As cv.Mat
        Dim contours As cv.Point()()
        contours = cv.Cv2.FindContoursAsArray(gray, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)

        For i = 0 To contours.Length - 1
            Dim minRect = cv.Cv2.MinAreaRect(contours(i))
            Dim nextColor = New cv.Scalar(colors(i Mod 255).Item0, colors(i Mod 255).Item1, colors(i Mod 255).Item2)
            drawRotatedRectangle(minRect, gray, nextColor)
        Next
        Return gray
    End Function
    Public Sub Run(src As cv.Mat) ' Rank = 1
        bgSub.RunClass(src)
        Dim gray As cv.Mat
        If bgSub.dst2.Channels = 1 Then gray = bgSub.dst2 Else gray = bgSub.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = motionRectangles(gray, task.vecColors)
        dst2.SetTo(cv.Scalar.All(255), gray)
    End Sub
End Class







Public Class Area_FindNonZero : Inherits VBparent
    Public nonZero As cv.Mat
    Public Sub New()
        labels(2) = "Coordinates of non-zero points"
        labels(3) = "Non-zero original points"
        task.desc = "Use FindNonZero API to get coordinates of non-zero points."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 2
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
            dst3.Circle(nonZero.Get(Of cv.Point)(0, i), task.dotSize + 2, cv.Scalar.White, -1, task.lineType)
        Next

        If standalone Then
            Dim outstr As String = "Coordinates of the non-zero points (ordered by row - top to bottom): " + vbCrLf + vbCrLf
            For i = 0 To nonZero.Rows - 1
                Dim pt = nonZero.Get(Of cv.Point)(0, i)
                outstr += "X = " + vbTab + CStr(pt.X) + vbTab + " y = " + vbTab + CStr(pt.Y) + vbCrLf
            Next
            setTrueText(outstr)
        End If
    End Sub
End Class







Public Class Area_TimeViewSingletons : Inherits VBparent
    Dim tView As New PointCloud_Singletons
    Dim nZero As New Area_FindNonZero
    Public Sub New()
        task.desc = "Use FindNonZero to get the coordinates of the nonzero points in the TimeView output"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        tView.RunClass(src)
        dst2 = tView.dst3

        nZero.RunClass(dst2)
        dst3 = nZero.dst3
        labels(3) = CStr(nZero.nonZero.Rows) + " points found"
    End Sub
End Class