Imports System.Runtime.InteropServices
Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class RedPrep_Basics : Inherits TaskParent
    Dim prepEdges As New RedPrep_Edges_CPP
    Public options As New Options_RedPrep
    Public Sub New()
        dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
        dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
        desc = "Reduction transform for the cv.Point cloud"
    End Sub
    Private Function reduceChan(chan As Mat, noDepthmask As Mat) As Mat
        chan *= task.fOptions.ReductionDepth.Value
        Dim mm As mmData = GetMinMax(chan)
        Dim dst32f As New Mat
        If Math.Abs(mm.minVal) > mm.maxVal Then
            mm.minVal = -mm.maxVal
            chan.ConvertTo(dst32f, MatType.CV_32F)
            Dim mask As New Mat
            Threshold(dst32f, mask, mm.minVal, mm.minVal, ThresholdTypes.BinaryInv)
            mask.ConvertTo(mask, MatType.CV_8U)
            dst32f.SetTo(Scalar.All(mm.minVal), mask)
        End If
        chan = (chan - mm.minVal) * 255 / (mm.maxVal - mm.minVal)
        chan.ConvertTo(chan, MatType.CV_8U)
        chan.SetTo(0, noDepthmask)
        Return chan
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If src.Type <> MatType.CV_32FC3 Then src = task.pointCloud.Clone

        Dim pc32S As New Mat
        src.ConvertTo(pc32S, MatType.CV_32SC3, 1000 / task.fOptions.ReductionDepth.Value)
        Dim splitMats() As Mat = Split(pc32S)

        dst2.SetTo(0)
        dst1.SetTo(0)

        If options.PrepX Then
            prepEdges.Run(reduceChan(splitMats(0), task.noDepthMask))
            dst1 += prepEdges.dst2
            dst2 = dst2 Or prepEdges.dst3
        End If

        If options.PrepY Then
            prepEdges.Run(reduceChan(splitMats(1), task.noDepthMask))
            Normalize(prepEdges.dst2, prepEdges.dst2, 0, 255, NormTypes.MinMax)
            dst1 += prepEdges.dst2
            Normalize(dst1, dst1, 0, 255, NormTypes.MinMax)
            dst2 = dst2 Or prepEdges.dst3
        End If

        If options.PrepZ Then
            prepEdges.Run(reduceChan(splitMats(2), task.noDepthMask))
            dst1 += prepEdges.dst2
            dst2 = dst2 Or prepEdges.dst3
        End If

        ' this is not as good as the operations above.
        'prepEdges.Run(reduceChan(splitMats(0) + splitMats(1) + splitMats(2)))
        'dst2 = prepEdges.dst3

        ' this rectangle prevents bleeds at the image edges.  It is necessary.  Test without it to see the impact.
        Rectangle(dst2, New cv.Rect(0, 0, dst2.Width, dst2.Height), Scalar.All(255), 2)

        labels(2) = "Using reduction factor = " + CStr(task.fOptions.ReductionDepth.Value)
    End Sub
End Class






Public Class RedPrep_Edges_CPP : Inherits TaskParent
    Implements IDisposable
    Public Sub New()
        cPtr = RedPrep_CPP_Open()
        desc = "Isolate each depth region"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            Static prep As New RedPrep_Core
            prep.Run(src)
            dst2 = prep.dst2
            labels(2) = prep.labels(2)
        Else
            dst2 = src
        End If

        Dim cppData(dst2.Total - 1) As Byte
        Marshal.Copy(dst2.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = RedPrep_CPP_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), dst2.Rows, dst2.Cols)
        handleSrc.Free()

        dst3 = Mat.FromPixelData(src.Rows, src.Cols, MatType.CV_8U, imagePtr).Clone
        If src.Size <> task.noDepthMask.Size Then
            Resize(task.noDepthMask, task.noDepthMask, src.Size)
            dst3.SetTo(255, task.noDepthMask)
            Resize(dst2, dst2, src.Size)
            dst2.SetTo(0, dst3)
        Else
            dst3.SetTo(255, task.noDepthMask)
            dst2.SetTo(0, dst3)
        End If
    End Sub
    Protected Overrides Sub Finalize()
        RedPrep_CPP_Close(cPtr)
    End Sub
End Class




Public Class RedPrep_Core : Inherits TaskParent
    Public options As New Options_RedPrep
    Public optionsPrep As New Options_PrepData
    Public reduced32s As New Mat
    Public reduced32f As New Mat
    Public presetReductionName As String = ""
    Public Sub New()
        desc = "Reduction transform for the cv.Point cloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        optionsPrep.Run()

        Dim reduction = task.fOptions.ReductionDepth.Value
        Dim split() = {New Mat, New Mat, New Mat}
        task.pcSplit(0).ConvertTo(split(0), MatType.CV_32S, 1000 / reduction)
        task.pcSplit(1).ConvertTo(split(1), MatType.CV_32S, 1000 / reduction)
        task.pcSplit(2).ConvertTo(split(2), MatType.CV_32S, 1000 / reduction)

        If presetReductionName <> "" Then task.reductionName = presetReductionName

        Select Case task.reductionName
            Case "X Reduction"
                reduced32s = split(0) * reduction
            Case "Y Reduction"
                reduced32s = split(1) * reduction
            Case "Z Reduction"
                reduced32s = split(2) * reduction
            Case "XY Reduction"
                reduced32s = (split(0) + split(1)) * reduction
            Case "XZ Reduction"
                reduced32s = (split(0) + split(2)) * reduction
            Case "YZ Reduction"
                reduced32s = (split(1) + split(2)) * reduction
            Case "XYZ Reduction"
                reduced32s = (split(0) + split(1) + split(2)) * reduction
        End Select

        reduced32s.ConvertTo(reduced32f, MatType.CV_32F)

        ' everything gets slammed between -1000 and 1000.  Good idea? I dunno...
        dst2 = (reduced32s - wcMinVal) * 254 / (wcMaxVal - wcMinVal)
        dst2.ConvertTo(dst2, MatType.CV_8U)
        dst2 += 1
        dst2.SetTo(0, task.noDepthMask)

        labels(2) = "Using reduction amount = " + CStr(reduction)

        If standalone Then
            Dim ranges = New Rangef() {New Rangef(-1, 256)}
            Dim histogram As New Mat
            CalcHist({dst2}, {0}, task.depthmask, histogram, 1, {256}, ranges)
            Dim histArray(255) As Single
            histogram.GetArray(Of Single)(histArray)
            Dim histSum = Sum(histogram)(0)
            If histSum <> CountNonZero(task.depthmask) Then Throw New Exception("can't happen.")
        End If

        dst3 = Palettize(dst2, 0)
    End Sub
End Class


