Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class FloodCell_Basics : Inherits VB_Algorithm
    Public inputMask As cv.Mat
    Public Sub New()
        cPtr = FloodCell_Open()
        gOptions.PixelDiffThreshold.Value = 0
        desc = "Floodfill an image so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then
            Static fless As New FeatureLess_Basics
            fless.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            src = fless.dst2
        End If

        Dim handlemask As GCHandle
        Dim maskPtr As IntPtr
        If inputMask IsNot Nothing Then
            Dim MaskData(inputMask.Total - 1) As Byte
            handlemask = GCHandle.Alloc(MaskData, GCHandleType.Pinned)
            Marshal.Copy(inputMask.Data, MaskData, 0, MaskData.Length)
            maskPtr = handlemask.AddrOfPinnedObject()
        End If

        Dim inputData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim imagePtr = FloodCell_Run(cPtr, handleInput.AddrOfPinnedObject(), maskPtr, src.Rows, src.Cols, src.Type,
                                     task.minPixels, gOptions.PixelDiffThreshold.Value)
        handleInput.Free()
        If maskPtr <> 0 Then handlemask.Free()

        dst3 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr)

        Dim classCount = FloodCell_Count(cPtr)
        If heartBeat() Then labels(3) = CStr(classCount) + " regions found"
        If classCount <= 1 Then Exit Sub

        Dim sizeData = New cv.Mat(classCount, 1, cv.MatType.CV_32S, FloodCell_Sizes(cPtr))
        Dim rectData = New cv.Mat(classCount, 1, cv.MatType.CV_32SC4, FloodCell_Rects(cPtr))
        Dim depthMean As cv.Scalar, depthStdev As cv.Scalar
        task.fCells.Clear()
        For i = 0 To classCount - 1
            Dim fc As New fcData
            fc.rect = validateRect(rectData.Get(Of cv.Rect)(i, 0))
            fc.pixels = sizeData.Get(Of Integer)(i, 0)
            fc.index = i + 1
            fc.mask = dst3(fc.rect).InRange(fc.index, fc.index)
            fc.maxDist = vbGetMaxDist(fc)

            fc.contour = contourBuild(fc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            vbDrawContour(fc.mask, fc.contour, fc.index, -1)

            Dim minLoc As cv.Point, maxLoc As cv.Point
            task.pcSplit(0)(fc.rect).MinMaxLoc(fc.minVec.X, fc.maxVec.X, minLoc, maxLoc, fc.mask)
            task.pcSplit(1)(fc.rect).MinMaxLoc(fc.minVec.Y, fc.maxVec.Y, minLoc, maxLoc, fc.mask)
            task.pcSplit(2)(fc.rect).MinMaxLoc(fc.minVec.Z, fc.maxVec.Z, minLoc, maxLoc, fc.mask)
            cv.Cv2.MeanStdDev(task.pointCloud(fc.rect), depthMean, depthStdev, fc.mask)

            fc.depthMean = New cv.Point3f(depthMean(0), depthMean(1), depthMean(2))
            fc.depthStdev = New cv.Point3f(depthStdev(0), depthStdev(1), depthStdev(2))

            cv.Cv2.MeanStdDev(task.color(fc.rect), fc.colorMean, fc.colorStdev, fc.mask)

            task.fCells.Add(fc)
        Next

        dst2 = vbPalette(dst3 * 255 / classCount)
        If heartBeat() Then labels(2) = CStr(task.fCells.Count) + " regions were identified."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = FloodCell_Close(cPtr)
    End Sub
End Class







Public Class FloodCell_Reduction : Inherits VB_Algorithm
    Dim floodCells As New FloodCell_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Floodfill a reduced image so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)

        floodCells.Run(reduction.dst2)

        dst2 = floodCells.dst2
        dst3 = floodCells.dst3
        labels(2) = floodCells.labels(2)
    End Sub
End Class






Public Class FloodCell_ReductionLR : Inherits VB_Algorithm
    Dim fCellsLeft As New FloodCell_Reduction
    Dim fCellsRight As New FloodCell_Reduction
    Public leftCells As New List(Of fcData)
    Public rightCells As New List(Of fcData)
    Public Sub New()
        desc = "Floodfill the reduced left and right images so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fCellsLeft.Run(task.leftView.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        leftCells = New List(Of fcData)(task.fCells)
        labels(2) = fCellsLeft.labels(2)

        dst2 = fCellsLeft.dst2

        fCellsRight.Run(task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        rightCells = New List(Of fcData)(task.fCells)
        labels(3) = fCellsRight.labels(2)

        dst3 = fCellsRight.dst2
    End Sub
End Class





Public Class FloodCell_Featureless : Inherits VB_Algorithm
    Dim floodCells As New FloodCell_Basics
    Public Sub New()
        desc = "Floodfill the featureless image so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static fless As New FeatureLess_Basics
        fless.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

        floodCells.inputMask = fless.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        floodCells.Run(fless.dst2)

        dst2 = floodCells.dst2
        dst3 = floodCells.dst3
        labels(2) = floodCells.labels(2)
    End Sub
End Class






Public Class FloodCell_FeatureLessLR : Inherits VB_Algorithm
    Dim fCellsLeft As New FloodCell_Basics
    Dim fCellsRight As New FloodCell_Basics
    Public leftCells As New List(Of fcData)
    Public rightCells As New List(Of fcData)
    Dim fless As New FeatureLess_Basics
    Public Sub New()
        desc = "Floodfill the featureless left and right images so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fless.Run(task.leftView.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

        fCellsLeft.inputMask = fless.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        fCellsLeft.Run(fless.dst2)
        leftCells = New List(Of fcData)(task.fCells)
        labels(2) = fCellsLeft.labels(2)

        dst2 = fCellsLeft.dst2

        fless.Run(task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

        fCellsRight.inputMask = fless.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        fCellsRight.Run(fless.dst2)
        rightCells = New List(Of fcData)(task.fCells)
        labels(3) = fCellsRight.labels(2)

        dst3 = fCellsRight.dst2
    End Sub
End Class







Public Class FloodCell_LUT : Inherits VB_Algorithm
    Dim floodCells As New FloodCell_Basics
    Dim lut As New LUT_Basics
    Public Sub New()
        desc = "Floodfill the LUT image so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lut.Run(src)

        floodCells.Run(lut.dst2)

        dst2 = floodCells.dst2
        dst3 = floodCells.dst3
        labels(2) = floodCells.labels(2)
    End Sub
End Class






Public Class FloodCell_LUTLeftRight : Inherits VB_Algorithm
    Dim fCellsLeft As New FloodCell_LUT
    Dim fCellsRight As New FloodCell_LUT
    Public leftCells As New List(Of fcData)
    Public rightCells As New List(Of fcData)
    Public Sub New()
        desc = "Floodfill the LUT image for left and right views so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fCellsLeft.Run(task.leftView.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        leftCells = New List(Of fcData)(task.fCells)
        labels(2) = fCellsLeft.labels(2)

        dst2 = fCellsLeft.dst2

        fCellsRight.Run(task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        rightCells = New List(Of fcData)(task.fCells)
        labels(3) = fCellsRight.labels(2)

        dst3 = fCellsRight.dst2
    End Sub
End Class







Public Class FloodCell_BP : Inherits VB_Algorithm
    Dim floodCells As New FloodCell_Basics
    Dim bpDoctor As New BackProject_Full
    Public Sub New()
        desc = "Floodfill the FloodCell_Basics image so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        bpDoctor.Run(src)

        floodCells.Run(bpDoctor.dst2)

        dst2 = floodCells.dst2
        dst3 = floodCells.dst3
        labels(2) = floodCells.labels(2)
    End Sub
End Class






Public Class FloodCell_BPLeftRight : Inherits VB_Algorithm
    Dim fCellsLeft As New FloodCell_BP
    Dim fCellsRight As New FloodCell_BP
    Public leftCells As New List(Of fcData)
    Public rightCells As New List(Of fcData)
    Public Sub New()
        desc = "Floodfill the FloodCell_Basics output image for left and right views so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fCellsLeft.Run(task.leftView.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        leftCells = New List(Of fcData)(task.fCells)
        labels(2) = fCellsLeft.labels(2)

        dst2 = fCellsLeft.dst2

        fCellsRight.Run(task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        rightCells = New List(Of fcData)(task.fCells)
        labels(3) = fCellsRight.labels(2)

        dst3 = fCellsRight.dst2
    End Sub
End Class