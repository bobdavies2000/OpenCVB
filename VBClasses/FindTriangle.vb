Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Namespace VBClasses
    Public Class FindTriangle_Basics : Inherits TaskParent
        Public triangle As cv.Mat
        Public options As New Options_MinArea
        Public srcPoints As List(Of cv.Point2f)
        Public Sub New()
            desc = "Find minimum containing triangle for a set of points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            If taskA.heartBeat Then
                srcPoints = New List(Of cv.Point2f)(options.srcPoints)
            Else
                If srcPoints.Count < 3 Then Exit Sub ' not enough points
            End If

            Dim dataSrc(srcPoints.Count * 2 - 1) As Single ' input is a list of points.
            Dim dstData(3 * 2 - 1) As Single ' minTriangle returns 3 points

            dst2.SetTo(white)

            Dim input As cv.Mat = cv.Mat.FromPixelData(1, srcPoints.Count, cv.MatType.CV_32FC2, srcPoints.ToArray)
            Marshal.Copy(input.Data, dataSrc, 0, dataSrc.Length)
            Dim srcHandle = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
            Dim dstHandle = GCHandle.Alloc(dstData, GCHandleType.Pinned)
            MinTriangle_Run(srcHandle.AddrOfPinnedObject(), srcPoints.Count, dstHandle.AddrOfPinnedObject)
            srcHandle.Free()
            dstHandle.Free()
            triangle = cv.Mat.FromPixelData(3, 1, cv.MatType.CV_32FC2, dstData)

            For i = 0 To 2
                Dim pt = triangle.Get(Of cv.Point2f)(i)
                Dim p1 = New cv.Point(pt.X, pt.Y)
                pt = triangle.Get(Of cv.Point2f)((i + 1) Mod 3)
                Dim p2 = New cv.Point(pt.X, pt.Y)
                vbc.DrawLine(dst2, p1, p2, cv.Scalar.Black)
            Next

            For Each pt In srcPoints
                DrawCircle(dst2, pt, taskA.DotSize + 1, cv.Scalar.Red)
            Next
        End Sub
    End Class
End Namespace