Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Namespace VBClasses
    Public Class RedMask_Basics : Inherits TaskParent
        Implements IDisposable
        Public mdList As New List(Of maskData)
        Public classCount As Integer
        Public Sub New()
            cPtr = RedMask_Open()
            desc = "Run the C++ RedMask to create a list of mask, rect, and other info about image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1 = Mat_Basics.srcMustBe8U(src)

            Dim inputData(dst1.Total - 1) As Byte
            dst1.GetArray(Of Byte)(inputData)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            Dim imagePtr = RedMask_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols,
                                       dst2.Total * 0.001)
            handleInput.Free()
            dst2 = cv.Mat.FromPixelData(dst1.Rows + 2, dst1.Cols + 2, cv.MatType.CV_8U, imagePtr).Clone
            dst2 = dst2(New cv.Rect(1, 1, dst2.Width - 2, dst2.Height - 2))

            classCount = RedMask_Count(cPtr)
            If classCount <= 1 Then Exit Sub ' no data to process.

            Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedCloud_Rects(cPtr))
            Dim rects(classCount - 1) As cv.Rect
            rectData.GetArray(Of cv.Rect)(rects)

            Dim rectlist = rects.ToList
            mdList.Clear()
            mdList.Add(New maskData) ' add a placeholder for zero...
            For i = 0 To classCount - 1
                Dim md As New maskData
                md.rect = rectlist(i)
                If md.rect.Size = dst2.Size Then Continue For
                If md.rect.Width * md.rect.Height < dst2.Total * 0.001 Then Continue For
                md.mask = dst2(md.rect).InRange(i + 1, i + 1)
                md.contour = ContourBuild(md.mask)
                DrawTour(md.mask, md.contour, 255, -1)
                md.pixels = md.mask.CountNonZero
                md.maxDist = Distance_Basics.GetMaxDist(md)
                md.mm = vbc.GetMinMax(task.pcSplit(2)(md.rect), task.depthmask(md.rect))
                md.index = mdList.Count
                mdList.Add(md)
            Next

            classCount = mdList.Count

            dst3 = PaletteBlackZero(dst2)

            labels(2) = "CV_8U result with " + CStr(classCount) + " regions."
            labels(3) = "Palette version of the data in dst2 with " + CStr(classCount) + " regions."
        End Sub
        Protected Overrides Sub Finalize()
            If cPtr <> 0 Then cPtr = RedMask_Close(cPtr)
        End Sub
    End Class






    Public Class NR_RedMask_Redraw : Inherits TaskParent
        Public redMask As New RedMask_Basics
        Public Sub New()
            desc = "Redraw the image using the mean color of each cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redMask.Run(src)

            src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            Dim color As cv.Scalar
            Dim emptyVec As New cv.Vec3b
            For Each md In redMask.mdList
                Dim c = src.Get(Of cv.Vec3b)(md.maxDist.Y, md.maxDist.X)
                color = New cv.Scalar(c.Item0, c.Item1, c.Item2)
                dst2(md.rect).SetTo(color, md.mask)
            Next
            labels(2) = redMask.labels(2)
        End Sub
    End Class
End Namespace