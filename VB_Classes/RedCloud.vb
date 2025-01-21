Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedCloud_Basics : Inherits TaskParent
    Dim prep As New RedCloud_PrepData
    Public redMask As New RedMask_Basics
    Dim rcMask As cv.Mat
    Public Sub New()
        task.redOptions.rcReductionSlider.Value = 100
        If standalone Then task.gOptions.displayDst1.Checked = True
        rcMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Run the reduced pointcloud output through the RedColor_CPP algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))
        Dim redColorMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        redColorMask = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(0, 255, cv.ThresholdTypes.Binary)
        cv.Cv2.ImShow("redColorMask", redColorMask)

        prep.Run(src)
        redMask.Run(prep.dst2 And redColorMask)
        Dim mm = GetMinMax(redMask.dst2)
        dst1 = ShowPalette(255 * redMask.dst2 / mm.maxVal)
        dst1.SetTo(0, task.noDepthMask)
        labels(1) = CStr(redMask.mdList.Count) + " maskData cells were found in the point cloud."

        For i = 0 To task.rcList.Count - 1
            Dim rc = task.rcList(i)
            rcMask.SetTo(0)
            rcMask(rc.rect).SetTo(255, rc.mask)
            rc.mdList = New List(Of maskData)
            For Each md In redMask.mdList
                Dim index = rcMask.Get(Of Byte)(md.maxDist.Y, md.maxDist.X)
                If index > 0 Then rc.mdList.Add(md)
            Next
            If rc.mdList.Count > 0 Then
                'Dim mdMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
                If rc.index = 1 Then cv.Cv2.ImShow("rcmask before", rcMask.Clone)
                For j = 0 To rc.mdList.Count - 1
                    Dim md = rc.mdList(j)
                    rcMask(md.rect) = rcMask(md.rect) And md.mask
                    md.mask = rcMask(md.rect).Clone
                    rc.mdList(j) = md
                Next
                If rc.index = 1 Then dst3 = rcMask.Clone
                task.rcList(i) = rc
            End If
        Next

        For Each rc In task.rcList
            For Each md In rc.mdList
                DrawCircle(dst1, md.maxDist, task.DotSize, task.HighlightColor)
            Next
        Next
        'For Each rc In task.rcList
        '    For Each md In rc.mdList
        '        dst3(md.rect).SetTo(rc.color, md.mask)
        '    Next
        'Next
    End Sub
End Class






Public Class RedCloud_PrepData : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Public Sub New()
        desc = "Reduction transform for the point cloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim split() As cv.Mat = {New cv.Mat, New cv.Mat, New cv.Mat}
        Dim input() As cv.Mat = task.pcSplit
        If src.Type = cv.MatType.CV_32FC3 Then input = src.Split
        Dim reduceAmt = task.redOptions.rcReductionSlider.Value
        input(0).ConvertTo(split(0), cv.MatType.CV_32S, 1000 / reduceAmt)
        input(1).ConvertTo(split(1), cv.MatType.CV_32S, 1000 / reduceAmt)
        input(2).ConvertTo(split(2), cv.MatType.CV_32S, 1000 / reduceAmt)

        Select Case task.redOptions.PointCloudReduction
            Case 0 ' X Reduction
                dst0 = (split(0) * reduceAmt).ToMat
            Case 1 ' Y Reduction
                dst0 = (split(1) * reduceAmt).ToMat
            Case 2 ' Z Reduction
                dst0 = (split(2) * reduceAmt).ToMat
            Case 3 ' XY Reduction
                dst0 = (split(0) * reduceAmt + split(1) * reduceAmt).ToMat
            Case 4 ' XZ Reduction
                dst0 = (split(0) * reduceAmt + split(2) * reduceAmt).ToMat
            Case 5 ' YZ Reduction
                dst0 = (split(1) * reduceAmt + split(2) * reduceAmt).ToMat
            Case 6 ' XYZ Reduction
                dst0 = (split(0) * reduceAmt + split(1) * reduceAmt + split(2) * reduceAmt).ToMat
        End Select

        Dim mm As mmData = GetMinMax(dst0)
        dst2 = (dst0 - mm.minVal) * 255 / (mm.maxVal - mm.minVal)
        dst2.ConvertTo(dst2, cv.MatType.CV_8U)

        dst2.SetTo(0, task.noDepthMask)

        If standaloneTest() Then
            If task.heartBeat Then
                mm = GetMinMax(dst2)
                plot.createHistogram = True
                plot.removeZeroEntry = False
                plot.maxRange = mm.maxVal
                plot.Run(dst2)
                dst3 = plot.dst2

                For i = 0 To plot.histArray.Count - 1
                    plot.histArray(i) = i
                Next

                Marshal.Copy(plot.histArray, 0, plot.histogram.Data, plot.histArray.Length)
                cv.Cv2.CalcBackProject({dst2}, {0}, plot.histogram, dst1, plot.ranges)
                dst3 = ShowPalette(dst1 * 255 / task.gOptions.HistBinBar.Value)
                labels(3) = CStr(plot.histArray.Count) + " different levels in the prepared data."
            End If
        End If

        labels(2) = task.redOptions.PointCloudReductionLabel + " with reduction factor = " +
                    CStr(reduceAmt)
    End Sub
End Class







Public Class RedCloud_BasicsHist : Inherits TaskParent
    Dim rCloud As New RedCloud_Basics
    Dim plot As New Plot_Histogram
    Public Sub New()
        task.gOptions.setHistogramBins(64)
        labels(3) = "Plot of the depth of the tracking cells (in grayscale), zero to task.maxZmeters in depth"
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        plot.createHistogram = True
        desc = "Display the histogram of the RedCloud_Basics output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        rCloud.Run(src)
        If task.heartBeat Then
            dst2.SetTo(0)
            For Each rc In task.rcList
                dst2(rc.rect).SetTo(rc.depthMean, rc.mask)
            Next
            Dim mm = GetMinMax(dst2, task.depthMask)

            plot.minRange = mm.minVal
            plot.maxRange = mm.maxVal
            plot.Run(dst2)
        End If
        dst3 = plot.dst2
    End Sub
End Class






Public Class RedCloud_BasicsHist1 : Inherits TaskParent
    Dim rCloud As New RedCloud_Basics
    Dim plot As New Plot_Histogram
    Dim mm As mmData
    Public Sub New()
        task.gOptions.setHistogramBins(64)
        task.redOptions.ColorMean.Checked = True
        labels(3) = "Plot of the depth of the tracking cells (in grayscale), zero to task.maxZmeters in depth"
        plot.createHistogram = True
        desc = "Display the histogram of the RedCloud_Basics output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        rCloud.Run(src)
        If task.heartBeat Then
            dst2 = DisplayCells().CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            mm = GetMinMax(dst2, task.depthMask)

            plot.minRange = mm.minVal
            plot.maxRange = mm.maxVal
            plot.Run(dst2)
        End If
        dst3 = plot.dst2
        labels(2) = rCloud.labels(2)
    End Sub
End Class






Public Class RedCloud_BasicsTest : Inherits TaskParent
    Dim rCloud As New RedCloud_Basics
    Public Sub New()
        desc = "Run RedCloud with the depth reduction."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        rCloud.Run(src)

        dst2 = runRedC(rCloud.dst2, labels(2))
    End Sub
End Class









Public Class RedCloud_YZ : Inherits TaskParent
    Dim stats As New Cell_Basics
    Dim rCloud As New RedCloud_Basics
    Public Sub New()
        task.redOptions.IdentifyCountBar.Value = 100
        task.redOptions.YZReduction.Checked = True
        desc = "Build horizontal RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        rCloud.Run(src)

        stats.Run(src)
        dst0 = stats.dst0
        dst1 = stats.dst1
        dst2 = stats.dst2
        SetTrueText(stats.strOut, 3)

        rCloud.Run(src)
        dst2 = rCloud.dst2
        labels(2) = rCloud.labels(2)
    End Sub
End Class






Public Class RedCloud_XZ : Inherits TaskParent
    Dim stats As New Cell_Basics
    Dim rCloud As New RedCloud_Basics
    Public Sub New()
        task.redOptions.IdentifyCountBar.Value = 100
        task.redOptions.XZReduction.Checked = True
        desc = "Build vertical RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        stats.Run(src)
        dst0 = stats.dst0
        dst1 = stats.dst1
        dst2 = stats.dst2
        SetTrueText(stats.strOut, 3)

        rCloud.Run(src)
        dst2 = rCloud.dst2
        labels(2) = rCloud.labels(2)
    End Sub
End Class






Public Class RedCloud_World : Inherits TaskParent
    Dim world As New Depth_World
    Dim prep As New RedCloud_PrepData
    Public Sub New()
        task.redOptions.IdentifyCountBar.Value = 100
        task.redOptions.rcReductionSlider.Value = 1000
        labels(3) = "Generated pointcloud"
        desc = "Display the output of a generated pointcloud as RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        world.Run(src)

        prep.Run(world.dst2)

        dst2 = runRedC(prep.dst2, labels(2))
    End Sub
End Class





Public Class RedCloud_BasicsNew : Inherits TaskParent
    Dim prep As New RedCloud_PrepData
    Public redMask As New RedMask_Basics
    Dim redC As New RedColor_Basics
    Dim cellGen As New Cell_Generate
    Public Sub New()
        task.redOptions.rcReductionSlider.Value = 100
        task.gOptions.displayDst1.Checked = True
        desc = "Prepare the reduced pointcloud after masking it with the boundaries of the RedColor_Basics output."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst1 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)

        prep.Run(src)
        prep.dst2.SetTo(0, dst1)
        redMask.Run(prep.dst2)

        cellGen.maskList = New List(Of maskData)(redMask.mdList)
        cellGen.Run(redMask.dst2)
        ' dst2 = cellGen.dst2
        cv.Cv2.ImShow("cellGen.dst2", cellGen.dst2)

        dst3 = ShowPalette(redMask.dst2 * 255 / redMask.mdList.Count)
        If task.heartBeat Then
            labels(3) = CStr(redMask.mdList.Count) + " cells were identified using " +
                        "the reduced pointcloud data."
        End If

        'dst0 = redMask.dst2
        'dst0.SetTo(0, Not dst1)
        'Dim cellMask = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary)

        'Dim colorList(task.rcList.Count - 1) As List(Of Integer)
        'Dim maskList As New List(Of maskData)
        'For i = 0 To redMask.maskList.Count - 1
        '    Dim md = redMask.maskList(i)
        '    md.mask = cellMask(md.rect) And md.mask
        '    md.mask.SetTo(0, task.noDepthMask(md.rect))
        '    md.depthMean = task.pcSplit(2)(md.rect).Mean(md.mask)

        '    md.index = task.rcMap.Get(Of Byte)(md.maxDist.Y, md.maxDist.X)
        '    maskList.Add(md)
        '    If colorList(md.index) Is Nothing Then colorList(md.index) = New List(Of Integer)
        '    colorList(md.index).Add(i)
        'Next

        'dst1 = dst2.Clone
        'For i = 0 To colorList.Count - 1
        '    If colorList(i) Is Nothing Then Continue For
        '    If colorList(i).Count = 1 Then
        '        Dim rc = task.rcList(i)
        '        rc.depthMean = maskList(i).depthMean
        '        rc.depthMask = maskList(i).mask
        '        rc.depthPixels = rc.depthMask.CountNonZero
        '    End If
        '    Dim meanList As New List(Of Single)
        '    For j = 0 To colorList(i).Count - 1
        '        Dim index = colorList(i)(j)
        '        Dim md = redMask.maskList(index)
        '        meanList.Add(md.depthMean)
        '        dst1(md.rect).SetTo(task.scalarColors(i), md.mask)
        '    Next
        'Next
        'dst3 = ShowPalette(dst0 * 255 / redMask.maskList.Count)
        'labels(3) = redMask.labels(3)
    End Sub
End Class