Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedPrep_Basics : Inherits TaskParent
        Dim prepEdges As New RedPrep_Edges_CPP
        Public options As New Options_RedPrep
        Dim reductionTarget As Integer
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "Reduction transform for the point cloud"
        End Sub
        Private Function reduceChan(chan As cv.Mat, noDepthmask As cv.Mat) As cv.Mat
            chan *= reductionTarget
            Dim mm As mmData = GetMinMax(chan)
            Dim dst32f As New cv.Mat
            If Math.Abs(mm.minVal) > mm.maxVal Then
                mm.minVal = -mm.maxVal
                chan.ConvertTo(dst32f, cv.MatType.CV_32F)
                Dim mask = dst32f.Threshold(mm.minVal, mm.minVal, cv.ThresholdTypes.BinaryInv)
                mask.ConvertTo(mask, cv.MatType.CV_8U)
                dst32f.SetTo(mm.minVal, mask)
            End If
            chan = (chan - mm.minVal) * 255 / (mm.maxVal - mm.minVal)
            chan.ConvertTo(chan, cv.MatType.CV_8U)
            chan.SetTo(0, noDepthmask)
            Return chan
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            reductionTarget = task.fOptions.ReductionSlider.Value

            If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud.Clone

            Dim pc32S As New cv.Mat
            src.ConvertTo(pc32S, cv.MatType.CV_32SC3, 1000 / reductionTarget)
            Dim split = pc32S.Split()

            dst2.SetTo(0)
            Dim saveNoDepth As cv.Mat = Nothing
            If src.Size <> task.workRes Then
                saveNoDepth = task.noDepthMask.Clone
                task.noDepthMask = task.noDepthMask.Resize(src.Size)
            End If
            If options.PrepX Then
                prepEdges.Run(reduceChan(split(0), task.noDepthMask))
                If dst2.Size <> src.Size Then
                    dst2 = dst2.Resize(src.Size)
                    dst2 = dst2 Or prepEdges.dst3
                Else
                    dst2 = dst2 Or prepEdges.dst3
                End If
            End If

            If options.PrepY Then
                prepEdges.Run(reduceChan(split(1), task.noDepthMask))
                dst2 = dst2 Or prepEdges.dst3
            End If

            If options.PrepZ Then
                prepEdges.Run(reduceChan(split(2), task.noDepthMask))
                dst2 = dst2 Or prepEdges.dst3
            End If

            ' this is not as good as the operations above.
            'prepEdges.Run(reduceChan(split(0) + split(1) + split(2)))
            'dst2 = prepEdges.dst3

            ' this rectangle prevents bleeds at the image edges.  It is necessary.  Test without it to see the impact.
            dst2.Rectangle(New cv.Rect(0, 0, dst2.Width, dst2.Height), 255, 2)

            If src.Size <> task.workRes Then task.noDepthMask = saveNoDepth.Clone
            labels(2) = "Using reduction factor = " + CStr(reductionTarget)
        End Sub
    End Class







    Public Class RedPrep_Depth : Inherits TaskParent
        Implements IDisposable
        Public Sub New()
            cPtr = PrepXY_Open()
            desc = "Run the C++ PrepXY to create a list of mask, rect, and other info about image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim inputX(task.pcSplit(0).Total * task.pcSplit(0).ElemSize - 1) As Byte
            Dim inputY(task.pcSplit(1).Total * task.pcSplit(1).ElemSize - 1) As Byte

            Marshal.Copy(task.pcSplit(0).Data, inputX, 0, inputX.Length)
            Marshal.Copy(task.pcSplit(1).Data, inputY, 0, inputY.Length)

            Dim handleX = GCHandle.Alloc(inputX, GCHandleType.Pinned)
            Dim handleY = GCHandle.Alloc(inputY, GCHandleType.Pinned)

            Dim imagePtr = PrepXY_Run(cPtr, handleX.AddrOfPinnedObject(), handleY.AddrOfPinnedObject(), src.Rows, src.Cols,
                                  task.xRange, task.yRange, task.histogramBins)
            handleX.Free()
            handleY.Free()

            dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
            dst2.SetTo(0, task.noDepthMask)

            dst3 = PaletteBlackZero(dst2)
        End Sub
        Protected Overrides Sub Finalize()
            If cPtr <> 0 Then cPtr = PrepXY_Close(cPtr)
        End Sub
    End Class








    Public Class NR_RedPrep_VB : Inherits TaskParent
        Public Sub New()
            desc = "Simpler transforms for the point cloud using CalcHist instead of reduction."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim histogram As New cv.Mat

            Dim ranges As cv.Rangef() = Nothing, zeroCount As Integer
            For i = 0 To 1
                Select Case i
                    Case 0 ' X Reduction
                        dst1 = task.pcSplit(0)
                        ranges = New cv.Rangef() {New cv.Rangef(-task.xRange, task.xRange)}
                    Case 1 ' Y Reduction
                        dst1 = task.pcSplit(1)
                        ranges = New cv.Rangef() {New cv.Rangef(-task.yRange, task.yRange)}
                End Select

                cv.Cv2.CalcHist({dst1}, {0}, task.depthmask, histogram, 1, {task.histogramBins}, ranges)

                Dim histArray(histogram.Total - 1) As Single
                Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

                For j = 0 To histArray.Count - 1
                    If histArray(j) = 0 Then zeroCount += 1
                    histArray(j) = j
                Next

                histogram = cv.Mat.FromPixelData(histogram.Rows, 1, cv.MatType.CV_32F, histArray)
                cv.Cv2.CalcBackProject({dst1}, {0}, histogram, dst1, ranges)

                If i = 0 Then dst3 = dst1.Clone Else dst3 += dst1
            Next

            dst3.ConvertTo(dst2, cv.MatType.CV_8U)
            dst2.SetTo(0, task.noDepthMask)

            labels(2) = CStr(task.histogramBins * 2 - zeroCount) + " depth regions mapped (control with histogram bins.)"
        End Sub
    End Class






    Public Class NR_RedPrep_DepthEdges : Inherits TaskParent
        Dim prep As New RedPrep_Depth
        Dim edges As New Edge_Basics
        Public Sub New()
            desc = "Find the edges of XY depth boundaries."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)
            dst3 = prep.dst3

            edges.Run(dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            dst2 = edges.dst2
            labels(2) = edges.labels(2)
        End Sub
    End Class






    Public Class NR_RedPrep_DepthTiers : Inherits TaskParent
        Dim prep As New RedPrep_Depth
        Dim tiers As New Depth_Tiers
        Public Sub New()
            labels(3) = "RedPrep_Depth output define regions with common XY."
            desc = "Find the edges of XY depth boundaries."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)
            dst3 = prep.dst3
            dst1 = prep.dst2

            tiers.Run(src)
            dst1 += tiers.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            dst2 = PaletteFull(dst1)
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

            dst3 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
            If src.Size <> task.noDepthMask.Size Then
                dst3.SetTo(255, task.noDepthMask.Resize(src.Size))
                dst2 = dst2.Resize(src.Size)
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




    Public Class NR_RedPrep_CloudAndColor : Inherits TaskParent
        Dim prepEdges As New RedPrep_Edges_CPP
        Public options As New Options_RedPrep
        Dim redSimple As New RedColor_Basics
        Dim edges As New EdgeLine_Basics
        Dim reductionTarget As Integer
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "Reduction transform for the point cloud"
        End Sub
        Private Function reduceChan(chan As cv.Mat) As cv.Mat
            chan *= reductionTarget
            Dim mm As mmData = GetMinMax(chan)
            Dim dst32f As New cv.Mat
            If Math.Abs(mm.minVal) > mm.maxVal Then
                mm.minVal = -mm.maxVal
                chan.ConvertTo(dst32f, cv.MatType.CV_32F)
                Dim mask = dst32f.Threshold(mm.minVal, mm.minVal, cv.ThresholdTypes.BinaryInv)
                mask.ConvertTo(mask, cv.MatType.CV_8U)
                dst32f.SetTo(mm.minVal, mask)
            End If
            chan = (chan - mm.minVal) * 255 / (mm.maxVal - mm.minVal)
            chan.ConvertTo(chan, cv.MatType.CV_8U)
            chan.SetTo(0, task.noDepthMask)
            Return chan
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            reductionTarget = task.fOptions.ReductionSlider.Value

            Dim pc32S As New cv.Mat
            task.pointCloud.ConvertTo(pc32S, cv.MatType.CV_32SC3, 1000 / reductionTarget)
            Dim split = pc32S.Split()

            dst2.SetTo(0)
            If options.PrepX Then
                prepEdges.Run(reduceChan(split(0)))
                dst2 = dst2 Or prepEdges.dst3
            End If

            If options.PrepY Then
                prepEdges.Run(reduceChan(split(1)))
                dst2 = dst2 Or prepEdges.dst3
            End If

            If options.PrepZ Then
                prepEdges.Run(reduceChan(split(2)))
                dst2 = dst2 Or prepEdges.dst3
            End If

            redSimple.Run(src)
            edges.Run(redSimple.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            dst3 = edges.dst2
            dst3.CopyTo(dst2, task.noDepthMask)

            dst2.Rectangle(New cv.Rect(0, 0, dst2.Width - 1, dst2.Height - 1), 255, task.lineWidth)
            labels(2) = "Using reduction factor = " + CStr(reductionTarget)
        End Sub
    End Class





    Public Class NR_RedPrep_EdgesX : Inherits TaskParent
        Dim edges As New RedPrep_Basics
        Public Sub New()
            OptionParent.FindCheckBox("Prep Edges in Y").Checked = False
            OptionParent.FindCheckBox("Prep Edges in Z").Checked = False
            desc = "Find X depth edges in the pointcloud data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edges.Run(src)
            dst2 = edges.dst2
            labels(2) = edges.labels(2)

            dst2.SetTo(0, task.noDepthMask)
        End Sub
    End Class






    Public Class NR_RedPrep_EdgesY : Inherits TaskParent
        Dim edges As New RedPrep_Basics
        Public Sub New()
            OptionParent.FindCheckBox("Prep Edges in X").Checked = False
            OptionParent.FindCheckBox("Prep Edges in Z").Checked = False
            desc = "Find Y depth edges in the pointcloud data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edges.Run(src)
            dst2 = edges.dst2
            labels(2) = edges.labels(2)

            dst2.SetTo(0, task.noDepthMask)
        End Sub
    End Class






    Public Class RedPrep_EdgesZ : Inherits TaskParent
        Dim edges As New RedPrep_Basics
        Public Sub New()
            OptionParent.FindCheckBox("Prep Edges in X").Checked = False
            OptionParent.FindCheckBox("Prep Edges in Y").Checked = False
            desc = "Find Z depth edges in the pointcloud data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edges.Run(src)
            dst2 = edges.dst2
            dst2.SetTo(0, task.noDepthMask)

            labels(2) = edges.labels(2)
        End Sub
    End Class




    Public Class RedPrep_Core : Inherits TaskParent
        Public options As New Options_RedPrep
        Public optionsPrep As New Options_PrepData
        Public reduced32s As New cv.Mat
        Public reduced32f As New cv.Mat
        Public Sub New()
            desc = "Reduction transform for the point cloud"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            optionsPrep.Run()

            Dim reductionFactor = task.fOptions.ReductionSlider.Value

            Dim split() = {New cv.Mat, New cv.Mat, New cv.Mat}
            task.pcSplit(0).ConvertTo(split(0), cv.MatType.CV_32S, 1000 / reductionFactor)
            task.pcSplit(1).ConvertTo(split(1), cv.MatType.CV_32S, 1000 / reductionFactor)
            task.pcSplit(2).ConvertTo(split(2), cv.MatType.CV_32S, 1000 / reductionFactor)

            Select Case optionsPrep.reductionName
                Case "X Reduction"
                    reduced32s = split(0) * reductionFactor
                Case "Y Reduction"
                    reduced32s = split(1) * reductionFactor
                Case "Z Reduction"
                    reduced32s = split(2) * reductionFactor
                Case "XY Reduction"
                    reduced32s = (split(0) + split(1)) * reductionFactor
                Case "XZ Reduction"
                    reduced32s = (split(0) + split(2)) * reductionFactor
                Case "YZ Reduction"
                    reduced32s = (split(1) + split(2)) * reductionFactor
                Case "XYZ Reduction"
                    reduced32s = (split(0) + split(1) + split(2)) * reductionFactor
            End Select

            Dim mm As mmData
            mm.minVal = -1000
            mm.maxVal = 1000

            reduced32s.ConvertTo(reduced32f, cv.MatType.CV_32F)

            dst2 = (reduced32s - mm.minVal) * 255 / (mm.maxVal - mm.minVal)
            dst2.ConvertTo(dst2, cv.MatType.CV_8U)
            dst2.SetTo(0, task.noDepthMask)

            labels(2) = "Using reduction amount = " + CStr(reductionFactor)

            dst3 = PaletteBlackZero(dst2)
        End Sub
    End Class






    Public Class RedPrep_EdgeMask : Inherits TaskParent
        Public reductionName As String
        Public prep As New RedPrep_Core
        Public Sub New()
            OptionParent.findRadio("XY Reduction").Checked = True
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Get the edges in the RedPrep_Core output"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)
            dst2 = prep.dst2
            labels(2) = prep.labels(2)

            dst3.SetTo(0)
            For y = 1 To dst2.Height - 2
                For x = 1 To dst2.Width - 2
                    Dim pix1 = dst2.Get(Of Byte)(y, x)
                    Dim pix2 = dst2.Get(Of Byte)(y, x + 1)
                    If pix1 <> 0 And pix2 <> 0 And pix1 <> pix2 Then dst3.Set(Of Byte)(y, x, 255)

                    pix2 = dst2.Get(Of Byte)(y + 1, x)
                    If pix1 <> 0 And pix2 <> 0 And pix1 <> pix2 Then dst3.Set(Of Byte)(y, x, 255)

                    pix2 = dst2.Get(Of Byte)(y + 1, x + 1)
                    If pix1 <> 0 And pix2 <> 0 And pix1 <> pix2 Then dst3.Set(Of Byte)(y, x, 255)
                Next
            Next

            dst2.SetTo(0, dst3)
        End Sub
    End Class
End Namespace