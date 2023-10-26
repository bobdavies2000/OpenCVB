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
                lfc = lastf(prev - 1)
                fc.indexLast = lfc.index
                fc.color = lfc.color
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

        labels(2) = fCell.labels(2)
    End Sub
End Class







Public Class RedCell_CPP : Inherits VB_Algorithm
    Public inputMask As cv.Mat
    Dim reduction As New Reduction_Basics
    Public Sub New()
        cPtr = FCell_Open()
        gOptions.PixelDiffThreshold.Value = 0
        desc = "Floodfill an image so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then
            reduction.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            src = reduction.dst2
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

        Dim imagePtr = FCell_Run(cPtr, handleInput.AddrOfPinnedObject(), maskPtr, src.Rows, src.Cols, src.Type,
                                     task.minPixels, gOptions.PixelDiffThreshold.Value)
        handleInput.Free()
        If maskPtr <> 0 Then handlemask.Free()

        dst3 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr)

        Dim classCount = FCell_Count(cPtr)
        If heartBeat() Then labels(3) = CStr(classCount) + " regions found"
        If classCount <= 1 Then Exit Sub

        Dim sizeData = New cv.Mat(classCount, 1, cv.MatType.CV_32S, FCell_Sizes(cPtr))
        Dim rectData = New cv.Mat(classCount, 1, cv.MatType.CV_32SC4, FCell_Rects(cPtr))
        Dim depthMean As cv.Scalar, depthStdev As cv.Scalar
        task.fCells.Clear()
        If standalone Or testIntermediate(traceName) Then dst2.SetTo(0)
        For i = 0 To classCount - 1
            Dim fc As New fcData
            fc.rect = validateRect(rectData.Get(Of cv.Rect)(i, 0))
            fc.pixels = sizeData.Get(Of Integer)(i, 0)
            fc.index = i + 1
            fc.mask = dst3(fc.rect).InRange(fc.index, fc.index)
            fc.color = task.vecColors(i) ' never more than 255...
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

            dst2(fc.rect).SetTo(fc.color, fc.mask)
        Next

        If heartBeat() Then labels(2) = CStr(task.fCells.Count) + " regions were identified."
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







Public Class RedCell_HistValley : Inherits VB_Algorithm
    Dim fCell As New RedCell_Binarize
    Dim valley As New HistValley_Basics
    Dim dValley As New HistValley_Depth
    Dim canny As New Edge_Canny
    Public Sub New()
        desc = "Use RedCloudY_Basics with the output of HistValley_Basics."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        valley.Run(src)
        dst1 = valley.dst1.Clone

        dValley.Run(src)
        canny.Run(dValley.dst1)
        dst1.SetTo(0, canny.dst2)

        canny.Run(valley.dst1)
        dst1.SetTo(0, canny.dst2)

        fCell.Run(dst1)
        dst2 = fCell.dst2
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







Public Class RedCell_PrepCloud : Inherits VB_Algorithm
    Dim fCell As New RedCell_Basics
    Dim prep As New RedCloud_PrepPointCloud
    Dim reduction As New Reduction_Basics
    Public Sub New()
        gOptions.useHistoryCloud.Checked = False ' no artifacts.
        labels(3) = "The flooded cells numbered from largest (1) to smallast (x < 255)"
        desc = "Floodfill the prep'd pointcloud output so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src.Clone)
        dst3 = reduction.dst2

        prep.Run(Nothing)
        prep.dst2.ConvertScaleAbs().CopyTo(dst3, task.depthMask)

        fCell.Run(dst3)

        dst2 = fCell.dst2
        labels(2) = fCell.labels(2)
    End Sub
End Class









Public Class RedCell_PrepDataOnly : Inherits VB_Algorithm
    Dim fCell As New RedCell_Basics
    Dim prep As New RedCloud_PrepPointCloud
    Public Sub New()
        gOptions.useHistoryCloud.Checked = False ' no artifacts.
        desc = "Run RedCell_Basics only on the prep'd data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        prep.Run(Nothing)

        fCell.Run(prep.dst2)

        dst2 = fCell.dst2
        dst2.SetTo(0, task.noDepthMask)
        labels(2) = fCell.labels(2)
    End Sub
End Class









Public Class RedCell_PrepNeighborsVB : Inherits VB_Algorithm
    Dim prep As New RedCloud_PrepPointCloud
    Public Sub New()
        desc = "Find neighbors using the output of the RedCloud_PrepPointCloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        prep.Run(Nothing)
        dst2 = prep.dst2
        dst3 = prep.dst2.Clone

        Dim samples(dst2.Total - 1) As Byte
        Marshal.Copy(dst2.Data, samples, 0, samples.Length)

        Dim nPoints As New List(Of cv.Point)
        Dim w = dst2.Width
        Dim cellData As New List(Of String)
        Dim kSize As Integer = 3
        For y = 0 To dst2.Height - kSize
            For x = 0 To dst2.Width - kSize
                Dim neighbors As New SortedList(Of Byte, Byte)
                For yy = y To y + kSize - 1
                    For xx = x To x + kSize - 1
                        Dim val = samples(yy * w + xx)
                        If val > 1 And val < 255 Then If neighbors.ContainsKey(val) = False Then neighbors.Add(val, 0)
                    Next
                Next
                If neighbors.Count > 2 Then
                    Dim series As String = ""
                    For Each ele In neighbors
                        series += CStr(ele.Key) + " "
                    Next
                    If cellData.Contains(series) = False Then
                        cellData.Add(series)
                        nPoints.Add(New cv.Point(x + 1, y + 1))
                    End If
                End If
            Next
        Next

        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        strOut = ""
        For i = 0 To nPoints.Count - 1
            Dim pt = nPoints(i)
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
            If pt.DistanceTo(task.clickPoint) < 5 Then
                strOut += cellData(i)
                strOut += vbCrLf
            End If
        Next

        setTrueText(strOut, 3)
        labels(2) = CStr(nPoints.Count) + " region intersection points were identified."
    End Sub
End Class