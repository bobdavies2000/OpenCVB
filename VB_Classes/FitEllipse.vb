Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module FitEllipse_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub FitEllipse_AMS(inputPtr As IntPtr, numberOfPoints As integer, outputTriangle As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub FitEllipse_Direct(inputPtr As IntPtr, numberOfPoints As integer, outputTriangle As IntPtr)
    End Sub
End Module



' https://docs.opencv.org/3.4.2/de/dc7/fitellipse_8cpp-example.html
Public Class FitEllipse_Basics_CPP
    Inherits VBparent
    Dim area As Area_MinTriangle_CPP
    Public dstData(5 * 4 - 1) As Byte ' enough space for a float describing angle, center, and width/height - this will be filled in on the C++ side.
    Public Sub New()
        initParent()
        area = New Area_MinTriangle_CPP()

        label1 = "Green FitEllipse, Yellow=AMS, Red=Direct"
        task.desc = "Use FitEllipse to draw around a set of points"
		task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        area.Run()  ' get some random clusters of points
        dst1.SetTo(0)
        If area.srcPoints.Count >= 5 Then
            Dim box = cv.Cv2.FitEllipse(area.srcPoints)
            ' draw a rotatedRectangle
            Dim vertices = box.Points()
            For j = 0 To vertices.Count - 1
                dst1.Line(vertices(j), vertices((j + 1) Mod 4), cv.Scalar.Green, 2, task.lineType)
            Next
            dst1.Ellipse(box, cv.Scalar.Green, 2, task.lineType)

            ' AMS method
            Dim input As New cv.Mat(area.srcPoints.Count, 1, cv.MatType.CV_32FC2, area.srcPoints)
            Marshal.Copy(input.Data, area.srcData, 0, area.srcData.Length)
            Dim srcHandle = GCHandle.Alloc(area.srcData, GCHandleType.Pinned)
            Dim dstHandle = GCHandle.Alloc(area.dstData, GCHandleType.Pinned)
            FitEllipse_AMS(srcHandle.AddrOfPinnedObject(), area.srcPoints.Count - 1, dstHandle.AddrOfPinnedObject)

            Dim output As New cv.Mat(5, 1, cv.MatType.CV_32F, area.dstData)
            Dim angle = output.Get(Of Single)(0)
            Dim center As New cv.Point2f(output.Get(Of Single)(1), output.Get(Of Single)(2))
            Dim size As New cv.Size2f(output.Get(Of Single)(3), output.Get(Of Single)(4))
            If Single.IsNaN(size.Width) = False And Single.IsNaN(size.Height) Then
                box = New cv.RotatedRect(center, size, angle)
                dst1.Ellipse(box, cv.Scalar.Yellow, 6, task.lineType)

                FitEllipse_Direct(srcHandle.AddrOfPinnedObject(), area.srcPoints.Count - 1, dstHandle.AddrOfPinnedObject)
                dstHandle.Free()

                angle = output.Get(Of Single)(0)
                center = New cv.Point2f(output.Get(Of Single)(1), output.Get(Of Single)(2))
                size = New cv.Size2f(output.Get(Of Single)(3), output.Get(Of Single)(4))
                box = New cv.RotatedRect(center, size, angle)
                dst1.Ellipse(box, cv.Scalar.Red, 2, task.lineType)
            End If
            ' draw the input dots on top of everything...
            For i = 0 To area.srcPoints.Count - 1
                dst1.Circle(area.srcPoints(i), 2, cv.Scalar.White, -1, task.lineType)
            Next
        End If
    End Sub
End Class


