Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedMask_Basics : Inherits TaskParent
    Public inputRemoved As cv.Mat
    Public mdList As New List(Of maskData)
    Public classCount As Integer
    Public Sub New()
        cPtr = RedMask_Open()
        inputRemoved = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Run the C++ RedMask to create a list of mask, rect, and other info about image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        src = srcMustBe8U(src)

        Dim inputData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim maskData(src.Total - 1) As Byte
        Marshal.Copy(inputRemoved.Data, maskData, 0, maskData.Length)
        Dim handleMask = GCHandle.Alloc(maskData, GCHandleType.Pinned)

        Dim imagePtr = RedMask_Run(cPtr, handleInput.AddrOfPinnedObject(), handleMask.AddrOfPinnedObject(),
                                   src.Rows, src.Cols, task.rcPixelThreshold)
        handleMask.Free()
        handleInput.Free()
        dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone

        classCount = RedMask_Count(cPtr)
        If classCount <= 1 Then Exit Sub ' no data to process.

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedMask_Rects(cPtr))
        Dim rects(classCount * 4) As Integer
        Marshal.Copy(rectData.Data, rects, 0, rects.Length)
        Dim rectlist As New List(Of cv.Rect)
        For i = 0 To rects.Count - 4 Step 4
            rectlist.Add(New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3)))
        Next

        mdList.Clear()
        mdList.Add(New maskData) ' add a placeholder for zero...
        For i = 0 To classCount - 1
            Dim md As New maskData
            md.rect = rectlist(i)
            If md.rect.Size = dst2.Size Then Continue For
            If md.rect.Width * md.rect.Height < task.rcPixelThreshold Then Continue For
            md.mask = dst2(md.rect).InRange(i + 1, i + 1)
            md.contour = ContourBuild(md.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            DrawContour(md.mask, md.contour, 255, -1)
            md.pixels = md.mask.CountNonZero
            md.maxDist = GetMaxDist(md)
            md.mm = GetMinMax(task.pcSplit(2)(md.rect), task.depthMask(md.rect))
            md.index = mdList.Count
            mdList.Add(md)
        Next

        classCount = mdList.Count

        If standaloneTest() Then dst3 = ShowPalette(dst2)

        If task.heartBeat Then labels(2) = "CV_8U result with " + CStr(classCount) + " regions."
        If task.heartBeat Then labels(3) = "Palette version of the data in dst2 with " + CStr(classCount) + " regions."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedMask_Close(cPtr)
    End Sub
End Class






Public Class RedMask_Redraw : Inherits TaskParent
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
