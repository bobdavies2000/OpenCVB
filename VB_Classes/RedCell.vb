Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedCell_Basics : Inherits VB_Algorithm
    Dim fCell As New RedCell_CPP
    Dim lastMap As cv.Mat
    Public Sub New()
        lastMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Match fCells from the current generation to the last."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim lastf As New List(Of fcData)(task.fCells)
        Dim lastMap = dst3.Clone

        fCell.Run(src)
        dst3 = fCell.dst3

        Dim fCells As New List(Of fcData)
        Dim lfc As fcData
        Dim usedColors1 As New List(Of cv.Vec3b)
        For Each fc In task.fCells
            Dim prev = lastMap.Get(Of Byte)(fc.maxDist.Y, fc.maxDist.X)
            If prev < lastf.Count And prev <> 0 Then
                lfc = lastf(prev)
                fc.indexLast = lfc.index
                fc.color = lfc.color
                fc.maxDStable = lfc.maxDStable
                Dim stableCheck = lastMap.Get(Of Byte)(lfc.maxDStable.Y, lfc.maxDStable.X)
                If stableCheck = fc.indexLast Then fc.maxDStable = lfc.maxDStable ' keep maxDStable if cell matched to previous
            End If
            If usedColors1.Contains(fc.color) Then
                fc.color = New cv.Vec3b(msRNG.Next(30, 240), msRNG.Next(30, 240), msRNG.Next(30, 240))
            End If
            usedColors1.Add(fc.color)
            fCells.Add(fc)
        Next

        dst2.SetTo(0)
        dst3.SetTo(0)
        Dim usedColors2 As New List(Of cv.Vec3b)
        For Each fc In fCells
            If usedColors2.Contains(fc.color) Then
                fc.color = New cv.Vec3b(msRNG.Next(30, 240), msRNG.Next(30, 240), msRNG.Next(30, 240))
            End If
            dst2(fc.rect).SetTo(fc.color, fc.mask)
            dst3(fc.rect).SetTo(fc.index, fc.mask)
        Next

        task.fCells = New List(Of fcData)(fCells)
        If task.clickPoint <> New cv.Point Then
            Dim index = dst3.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
            If index < task.fCells.Count Then
                task.fcSelect = task.fCells(index)
                Dim fc = task.fcSelect
                dst2(fc.rect).SetTo(white, fc.mask)
                task.color(fc.rect).SetTo(white, fc.mask)
            End If
        End If

        labels(2) = fCell.labels(2)
    End Sub
End Class






Public Class RedCell_BasicsNew : Inherits VB_Algorithm
    Dim fCell As New RedCell_NewCPP
    Dim lastMap As cv.Mat
    Public Sub New()
        lastMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Match fCells from the current generation to the last."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim lastCells As New List(Of rcData)(task.fNewCells)
        Dim lastMap = dst3.Clone

        fCell.Run(src)
        dst3 = fCell.dst3

        Dim fCells As New List(Of rcData)
        Dim lrc As rcData
        Dim usedColors1 As New List(Of cv.Vec3b)
        For Each rc In task.fNewCells
            Dim prev = lastMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            If prev < lastCells.Count And prev <> 0 Then
                lrc = lastCells(prev)
                rc.indexLast = lrc.index
                rc.color = lrc.color
                rc.maxDStable = lrc.maxDStable
                Dim stableCheck = lastMap.Get(Of Byte)(lrc.maxDStable.Y, lrc.maxDStable.X)
                If stableCheck = rc.indexLast Then rc.maxDStable = lrc.maxDStable ' keep maxDStable if cell matched to previous
            End If
            If usedColors1.Contains(rc.color) Then
                rc.color = New cv.Vec3b(msRNG.Next(30, 240), msRNG.Next(30, 240), msRNG.Next(30, 240))
            End If
            usedColors1.Add(rc.color)
            fCells.Add(rc)
        Next

        dst2.SetTo(0)
        dst3.SetTo(0)
        Dim usedColors2 As New List(Of cv.Vec3b)
        For Each fc In fCells
            If usedColors2.Contains(fc.color) Then
                fc.color = New cv.Vec3b(msRNG.Next(30, 240), msRNG.Next(30, 240), msRNG.Next(30, 240))
            End If
            dst2(fc.rect).SetTo(fc.color, fc.mask)
            dst3(fc.rect).SetTo(fc.index, fc.mask)
        Next

        task.fNewCells = New List(Of rcData)(fCells)
        If task.clickPoint <> New cv.Point Then
            Dim index = dst3.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
            If index < task.fNewCells.Count Then
                task.fcNewSelect = task.fNewCells(index)
                Dim fc = task.fcSelect
                dst2(fc.rect).SetTo(white, fc.mask)
                task.color(fc.rect).SetTo(white, fc.mask)
            End If
        End If

        labels(2) = fCell.labels(2)
    End Sub
End Class








Public Class RedCell_NewCPP : Inherits VB_Algorithm
    Dim reduction As New Reduction_Basics
    Public Sub New()
        cPtr = FCell_Open()
        gOptions.PixelDiffThreshold.Value = 0
        desc = "Floodfill an image so each cell can be tracked.  NOTE: cells are not matched to previous image.  Use RedCell_Basics for matching."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then
            reduction.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            src = reduction.dst2
        End If

        Dim inputData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim imagePtr = FCell_Run(cPtr, handleInput.AddrOfPinnedObject(), 0, src.Rows, src.Cols, src.Type,
                                 task.minPixels, gOptions.PixelDiffThreshold.Value)
        handleInput.Free()

        dst3 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr)

        Dim classCount = FCell_Count(cPtr)
        If heartBeat() Then labels(3) = CStr(classCount) + " regions found"
        If classCount <= 1 Then Exit Sub

        Dim sizeData = New cv.Mat(classCount, 1, cv.MatType.CV_32S, FCell_Sizes(cPtr))
        Dim rectData = New cv.Mat(classCount, 1, cv.MatType.CV_32SC4, FCell_Rects(cPtr))
        Dim depthMean As cv.Scalar, depthStdev As cv.Scalar
        task.fNewCells.Clear()
        task.fNewCells.Add(New rcData) ' placeholder so index aligns with offset.
        If standalone Or testIntermediate(traceName) Then dst2.SetTo(0)
        For i = 0 To classCount - 1
            Dim rc As New rcData
            rc.rect = validateRect(rectData.Get(Of cv.Rect)(i, 0))
            rc.pixels = sizeData.Get(Of Integer)(i, 0)
            rc.index = task.fNewCells.Count
            rc.mask = dst3(rc.rect).InRange(rc.index, rc.index)
            rc.color = task.vecColors(i) ' never more than 255...
            rc.maxDist = vbGetMaxDist(rc)
            rc.maxDStable = rc.maxDist ' assume it has to use the latest.

            rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            vbDrawContour(rc.mask, rc.contour, rc.index, -1)

            Dim minLoc As cv.Point, maxLoc As cv.Point
            task.pcSplit(0)(rc.rect).MinMaxLoc(rc.minVec.X, rc.maxVec.X, minLoc, maxLoc, rc.mask)
            task.pcSplit(1)(rc.rect).MinMaxLoc(rc.minVec.Y, rc.maxVec.Y, minLoc, maxLoc, rc.mask)
            task.pcSplit(2)(rc.rect).MinMaxLoc(rc.minVec.Z, rc.maxVec.Z, minLoc, maxLoc, rc.mask)
            cv.Cv2.MeanStdDev(task.pointCloud(rc.rect), depthMean, depthStdev, rc.mask)

            rc.depthMean = New cv.Point3f(depthMean(0), depthMean(1), depthMean(2))
            rc.depthStdev = New cv.Point3f(depthStdev(0), depthStdev(1), depthStdev(2))

            cv.Cv2.MeanStdDev(task.color(rc.rect), rc.colorMean, rc.colorStdev, rc.mask)

            task.fNewCells.Add(rc)

            dst2(rc.rect).SetTo(rc.color, rc.mask)
        Next

        If heartBeat() Then labels(2) = CStr(task.fNewCells.Count) + " regions were identified - use RedCell_Basics to match to the previous image."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = FCell_Close(cPtr)
    End Sub
End Class







Public Class RedCell_CPP : Inherits VB_Algorithm
    Dim reduction As New Reduction_Basics
    Public Sub New()
        cPtr = FCell_Open()
        gOptions.PixelDiffThreshold.Value = 0
        desc = "Floodfill an image so each cell can be tracked.  NOTE: cells are not matched to previous image.  Use RedCell_Basics for matching."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then
            reduction.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            src = reduction.dst2
        End If

        Dim inputData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim imagePtr = FCell_Run(cPtr, handleInput.AddrOfPinnedObject(), 0, src.Rows, src.Cols, src.Type,
                                 task.minPixels, gOptions.PixelDiffThreshold.Value)
        handleInput.Free()

        dst3 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr)

        Dim classCount = FCell_Count(cPtr)
        If heartBeat() Then labels(3) = CStr(classCount) + " regions found"
        If classCount <= 1 Then Exit Sub

        Dim sizeData = New cv.Mat(classCount, 1, cv.MatType.CV_32S, FCell_Sizes(cPtr))
        Dim rectData = New cv.Mat(classCount, 1, cv.MatType.CV_32SC4, FCell_Rects(cPtr))
        Dim depthMean As cv.Scalar, depthStdev As cv.Scalar
        task.fCells.Clear()
        task.fCells.Add(New fcData) ' placeholder so index aligns with offset.
        If standalone Or testIntermediate(traceName) Then dst2.SetTo(0)
        For i = 0 To classCount - 1
            Dim fc As New fcData
            fc.rect = validateRect(rectData.Get(Of cv.Rect)(i, 0))
            fc.pixels = sizeData.Get(Of Integer)(i, 0)
            fc.index = task.fCells.Count
            fc.mask = dst3(fc.rect).InRange(fc.index, fc.index)
            fc.color = task.vecColors(i) ' never more than 255...
            fc.maxDist = vbGetMaxDist(fc)
            fc.maxDStable = fc.maxDist ' assume it has to use the latest.

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

            dst2(fc.rect).SetTo(fc.color, fc.mask)
        Next

        If heartBeat() Then labels(2) = CStr(task.fCells.Count) + " regions were identified - use RedCell_Basics to match to the previous image."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = FCell_Close(cPtr)
    End Sub
End Class







Public Class RedCell_Reduction : Inherits VB_Algorithm
    Dim fCell As New RedCell_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Floodfill a reduced image so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)

        fCell.Run(reduction.dst2)

        dst2 = fCell.dst2
        dst3 = fCell.dst3
        labels(2) = fCell.labels(2)
    End Sub
End Class






Public Class RedCell_ReductionLR : Inherits VB_Algorithm
    Dim fCellsLeft As New RedCell_Reduction
    Dim fCellsRight As New RedCell_Reduction
    Public leftCells As New List(Of fcData)
    Public rightCells As New List(Of fcData)
    Public Sub New()
        desc = "Floodfill the reduced left and right images so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.fCells = New List(Of fcData)(leftCells)
        fCellsLeft.Run(task.leftView.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        leftCells = New List(Of fcData)(task.fCells)
        labels(2) = fCellsLeft.labels(2)

        dst2 = fCellsLeft.dst2

        task.fCells = New List(Of fcData)(rightCells)
        fCellsRight.Run(task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        rightCells = New List(Of fcData)(task.fCells)
        labels(3) = fCellsRight.labels(2)

        dst3 = fCellsRight.dst2
    End Sub
End Class





Public Class RedCell_Featureless : Inherits VB_Algorithm
    Dim fCell As New RedCell_Basics
    Public Sub New()
        desc = "Floodfill the featureless image so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static fless As New FeatureLess_Basics
        fless.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

        fCell.Run(fless.dst2)

        dst2 = fCell.dst2
        dst3 = fCell.dst3
        labels(2) = fCell.labels(2)
    End Sub
End Class






Public Class RedCell_FeatureLessLR : Inherits VB_Algorithm
    Dim fCellsLeft As New RedCell_Featureless
    Dim fCellsRight As New RedCell_Featureless
    Public leftCells As New List(Of fcData)
    Public rightCells As New List(Of fcData)
    Public Sub New()
        desc = "Floodfill the featureless left and right images so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.fCells = New List(Of fcData)(leftCells)
        fCellsLeft.Run(task.leftView)
        leftCells = New List(Of fcData)(task.fCells)
        labels(2) = fCellsLeft.labels(2)

        dst2 = fCellsLeft.dst2

        task.fCells = New List(Of fcData)(rightCells)
        fCellsRight.Run(task.rightView)
        rightCells = New List(Of fcData)(task.fCells)
        labels(3) = fCellsRight.labels(2)

        dst3 = fCellsRight.dst2
    End Sub
End Class







Public Class RedCell_LUT : Inherits VB_Algorithm
    Dim fCell As New RedCell_Basics
    Dim lut As New LUT_Basics
    Public Sub New()
        desc = "Floodfill the LUT image so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lut.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

        fCell.Run(lut.dst2)

        dst2 = fCell.dst2
        dst3 = fCell.dst3
        labels(2) = fCell.labels(2)
    End Sub
End Class






Public Class RedCell_LUTLeftRight : Inherits VB_Algorithm
    Dim fCellsLeft As New RedCell_LUT
    Dim fCellsRight As New RedCell_LUT
    Public leftCells As New List(Of fcData)
    Public rightCells As New List(Of fcData)
    Public Sub New()
        desc = "Floodfill the LUT image for left and right views so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.fCells = New List(Of fcData)(leftCells)
        fCellsLeft.Run(task.leftView)
        leftCells = New List(Of fcData)(task.fCells)
        labels(2) = fCellsLeft.labels(2)

        dst2 = fCellsLeft.dst2

        task.fCells = New List(Of fcData)(rightCells)
        fCellsRight.Run(task.rightView)
        rightCells = New List(Of fcData)(task.fCells)
        labels(3) = fCellsRight.labels(2)

        dst3 = fCellsRight.dst2
    End Sub
End Class







Public Class RedCell_BP : Inherits VB_Algorithm
    Dim fCell As New RedCell_Basics
    Dim bpDoctor As New BackProject_Full
    Public Sub New()
        labels(3) = "The flooded cells numbered from largest (1) to smallast (x < 255)"
        desc = "Floodfill the RedCell_Basics image so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        bpDoctor.Run(src)

        fCell.Run(bpDoctor.dst2)

        dst2 = fCell.dst2
        dst3 = fCell.dst3
        labels(2) = fCell.labels(2)
    End Sub
End Class






Public Class RedCell_BPLeftRight : Inherits VB_Algorithm
    Dim fCellsLeft As New RedCell_BP
    Dim fCellsRight As New RedCell_BP
    Public leftCells As New List(Of fcData)
    Public rightCells As New List(Of fcData)
    Public Sub New()
        desc = "Floodfill the RedCell_Basics output image for left and right views so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.fCells = New List(Of fcData)(leftCells)
        fCellsLeft.Run(task.leftView.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        leftCells = New List(Of fcData)(task.fCells)
        labels(2) = fCellsLeft.labels(2)

        dst2 = fCellsLeft.dst2

        task.fCells = New List(Of fcData)(rightCells)
        fCellsRight.Run(task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        rightCells = New List(Of fcData)(task.fCells)
        labels(3) = fCellsRight.labels(2)

        dst3 = fCellsRight.dst2
    End Sub
End Class









Public Class RedCell_Binarize : Inherits VB_Algorithm
    Dim binarize As New Binarize_RecurseAdd
    Dim fCell As New RedCell_Basics
    Public Sub New()
        labels(3) = "A 4-way split of the input grayscale image based on the amount of light"
        desc = "Use RedCloud on a 4-way split based on light to dark in the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        binarize.Run(src)
        dst3 = vbPalette(binarize.dst1 * 255 / 4)

        fCell.Run(binarize.dst1)
        dst2 = fCell.dst2
        labels(2) = fCell.labels(2)
    End Sub
End Class







' https://docs.opencv.org/master/de/d01/samples_2cpp_2connected_components_8cpp-example.html
Public Class RedCell_CComp : Inherits VB_Algorithm
    Dim ccomp As New CComp_Both
    Dim fCell As New RedCell_Basics
    Public Sub New()
        desc = "Identify each Connected component as a RedCloud Cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ccomp.Run(src)
        dst3 = vbNormalize32f(ccomp.dst1)
        fCell.Run(dst3)
        dst2 = fCell.dst2
        labels(2) = fCell.labels(2)
    End Sub
End Class







Public Class RedCell_KMeans : Inherits VB_Algorithm
    Dim fCell As New RedCell_Basics
    Dim km As New KMeans_MultiChannel
    Public Sub New()
        labels(3) = "The flooded cells numbered from largest (1) to smallast (x < 255)"
        desc = "Floodfill the KMeans output so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        km.Run(src)

        fCell.Run(km.dst2)

        dst2 = fCell.dst2
        dst3 = fCell.dst3
        labels(2) = fCell.labels(2)
    End Sub
End Class








Public Class RedCell_CCompBinarized : Inherits VB_Algorithm
    Dim edges As New Edge_BinarizedSobel
    Dim ccomp As New RedCell_Binarize
    Public Sub New()
        labels(3) = "Binarized Sobel output"
        desc = "Use the binarized edges to find the different blobs in the image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        edges.Run(src)
        dst3 = edges.dst2

        ccomp.Run(dst3)
        dst2 = ccomp.dst2
        labels(2) = ccomp.labels(2)
    End Sub
End Class







Public Class RedCell_Neighbors : Inherits VB_Algorithm
    Dim nabs As New Neighbor_Basics
    Dim fCell As New RedCell_Basics
    Public Sub New()
        desc = "Find all the neighbors for a RedCell cellmap"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fCell.Run(src)
        dst2 = fCell.dst2

        nabs.cellCount = task.fCells.Count
        nabs.Run(fCell.dst3)

        For i = 0 To task.fCells.Count - 1
            task.fCells(i).neighbors = New List(Of Byte)(nabs.nabList(i))
        Next

        dst3.SetTo(0)
        dst3(task.fcSelect.rect).SetTo(task.fcSelect.color, task.fcSelect.mask)
        For Each index In task.fcSelect.neighbors
            If index >= task.fCells.Count Then Continue For
            Dim rc = task.fCells(index)
            dst3(rc.rect).SetTo(rc.color, rc.mask)
        Next

        task.color(task.fcSelect.rect).SetTo(white, task.fcSelect.mask)

        setTrueText(strOut, 3)
        If heartBeat() Then labels(2) = CStr(task.fCells.Count) + " regions identified."
    End Sub
End Class