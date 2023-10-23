Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class FloodCell_Basics : Inherits VB_Algorithm
    Public redCells As New List(Of rcData)
    Public inputMask As cv.Mat
    Public Sub New()
        cPtr = FloodCell_Open()
        gOptions.PixelDiffThreshold.Value = 0
        desc = "Floodfill an image so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then
            Static guided As New GuidedBP_Depth
            guided.Run(src)
            src = guided.backProject
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
        redCells.Clear()
        For i = 0 To classCount - 1
            Dim rc As New rcData
            rc.rect = validateRect(rectData.Get(Of cv.Rect)(i, 0))
            rc.pixels = sizeData.Get(Of Integer)(i, 0)
            rc.index = i + 1
            rc.mask = dst3(rc.rect).InRange(rc.index, rc.index)

            rc.maxDist = vbGetMaxDist(rc)
            rc.maxDStable = rc.maxDist ' assume it has to use the latest.

            rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1

            Dim minLoc As cv.Point, maxLoc As cv.Point
            task.pcSplit(0)(rc.rect).MinMaxLoc(rc.minVec.X, rc.maxVec.X, minLoc, maxLoc, rc.mask)
            task.pcSplit(1)(rc.rect).MinMaxLoc(rc.minVec.Y, rc.maxVec.Y, minLoc, maxLoc, rc.mask)
            task.pcSplit(2)(rc.rect).MinMaxLoc(rc.minVec.Z, rc.maxVec.Z, minLoc, maxLoc, rc.mask)
            cv.Cv2.MeanStdDev(task.pointCloud(rc.rect), depthMean, depthStdev, rc.mask)

            ' if there is no depth within the mask, then estimate this color only cell with depth in the rect.
            If depthMean(2) = 0 Then
                rc.colorOnly = True
                cv.Cv2.MeanStdDev(task.pointCloud(rc.rect), depthMean, depthStdev)
            End If
            rc.depthMean = New cv.Point3f(depthMean(0), depthMean(1), depthMean(2))
            rc.depthStdev = New cv.Point3f(depthStdev(0), depthStdev(1), depthStdev(2))

            cv.Cv2.MeanStdDev(task.color(rc.rect), rc.colorMean, rc.colorStdev, rc.mask)

            rc.eq = build3PointEquation(rc)
            'Dim pcaPoints As New List(Of cv.Point3f)
            'Dim ePoints As New List(Of cv.Point2f)
            'For Each pt In rc.contour
            '    ePoints.Add(New cv.Point2f(rc.rect.X + pt.X, rc.rect.Y + pt.Y))
            '    Dim vec = task.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X)
            '    If vec.Z > 0 Then pcaPoints.Add(vec)
            'Next
            'If ePoints.Count > 5 Then rc.box = cv.Cv2.FitEllipse(ePoints)

            'If pcaPoints.Count >= 3 Then
            '    Dim inputMat = New cv.Mat(pcaPoints.Count, 3, cv.MatType.CV_32F, pcaPoints.ToArray)
            '    pcaAnalysis = New cv.PCA(inputMat, New cv.Mat, cv.PCA.Flags.DataAsRow)

            '    Dim valList As New List(Of Single)
            '    For j = 0 To 3 - 1
            '        Dim val = pcaAnalysis.Eigenvalues.Get(Of Single)(0, j)
            '        valList.Add(val)
            '    Next

            '    Dim bestIndex = valList.IndexOf(valList.Min)
            '    rc.pcaVec = New cv.Point3f()
            '    For j = 0 To 3 - 1
            '        rc.pcaVec(j) = pcaAnalysis.Eigenvectors.Get(Of Single)(bestIndex, j)
            '    Next
            'End If

            redCells.Add(rc)
        Next

        dst2 = vbPalette(dst3 * 255 / classCount)
        If standalone Then dst2.SetTo(0, task.noDepthMask)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = FloodCell_Close(cPtr)
    End Sub
End Class






Module GuidedBP_Cell_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_Rects(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_Sizes(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_Count(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_Run(
                cPtr As IntPtr, dataPtr As IntPtr, maskPtr As IntPtr, rows As Integer, cols As Integer,
                type As Integer, minPixels As Integer, diff As Integer) As IntPtr
    End Function
End Module






Public Class FloodCell_Color : Inherits VB_Algorithm
    Dim fBuild As New FloodCell_Basics
    Public Sub New()
        desc = "Floodfill an image so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static reduction As New Reduction_Basics
        reduction.Run(src)

        fBuild.Run(reduction.dst2)

        dst2 = fBuild.dst2
        dst3 = fBuild.dst3
    End Sub
End Class






Public Class FloodCell_Featureless : Inherits VB_Algorithm
    Dim fCells As New FloodCell_Basics
    Public Sub New()
        desc = "Floodfill the featureless image so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static fless As New FeatureLess_Basics
        fless.Run(src)

        fCells.inputMask = fless.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        fCells.Run(fless.dst2)

        dst2 = fCells.dst2
        dst3 = fCells.dst3
    End Sub
End Class