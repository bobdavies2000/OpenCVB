Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedMask_Basics : Inherits TaskParent
    Public inputRemoved As cv.Mat
    Public classCount As Integer
    Public rectList As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public depth As New List(Of Single)
    Public Sub New()
        cPtr = RedColor_Open()
        inputRemoved = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Run the C++ RedCloud interface with or without a mask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then
            Static color As New Color8U_Basics
            color.Run(src)
            src = color.dst2
        End If
        Dim inputData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim maskData(src.Total - 1) As Byte
        Marshal.Copy(inputRemoved.Data, maskData, 0, maskData.Length)
        Dim handleMask = GCHandle.Alloc(maskData, GCHandleType.Pinned)

        Dim imagePtr = RedColor_Run(cPtr, handleInput.AddrOfPinnedObject(),
                                    handleMask.AddrOfPinnedObject(), src.Rows, src.Cols, task.rcMinSize)
        handleMask.Free()
        handleInput.Free()
        dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone

        classCount = Math.Min(RedColor_Count(cPtr), 255)
        If classCount = 0 Then Exit Sub ' no data to process.

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedColor_Rects(cPtr))

        Dim rects(classCount * 4) As Integer
        Marshal.Copy(rectData.Data, rects, 0, rects.Length)

        rectList.Clear()
        For i = 0 To classCount * 4 - 4 Step 4
            Dim r = New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3))
            ' If r.Size = dst2.Size Then Continue For ' RedColor_Run finds a cell this big.  
            If r.Width * r.Height < task.rcMinSize Then Continue For
            rectList.Add(r)
        Next

        masks.Clear()
        dst1.SetTo(0)
        Dim map = task.redC.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        map = map.Threshold(0, 255, cv.ThresholdTypes.Binary)
        For i = 0 To rectList.Count - 1
            Dim rect = rectList(i)
            Dim mask = dst2(rect).InRange(i + 1, i + 1)
            Dim contour = ContourBuild(mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            DrawContour(mask, contour, 255, -1)
            masks.Add(mask.Clone)

            Dim test As cv.Mat = map(rect) And mask
            If test.CountNonZero Then dst1(rect).SetTo(255, mask)
        Next

        classCount = rectList.Count

        If standaloneTest() Then dst3 = ShowPalette(dst2 * 255 / classCount)

        If task.heartBeat Then labels(2) = "CV_8U result with " + CStr(classCount) + " regions."
        If task.heartBeat Then labels(3) = "Palette version of the data in dst2 with " + CStr(classCount) + " regions."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedColor_Close(cPtr)
    End Sub
End Class






Public Class RedMask_Both : Inherits TaskParent
    Dim redMask As New RedMask_Basics
    Public Sub New()
        desc = "Create masks for both color and color"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redMask.Run(src)


    End Sub
End Class
