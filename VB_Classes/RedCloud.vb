Imports cv = OpenCvSharp
Public Class RedCloud_Basics : Inherits TaskParent
    Public Sub New()
        If standalone Then task.gOptions.HistBinBar.Value = 255
        task.redOptions.UseDepth.Checked = True
        desc = "Reduction transform for the point cloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim split(3 - 1) As cv.Mat
        split(0) = New cv.Mat
        split(1) = New cv.Mat
        split(2) = New cv.Mat

        Dim reduceAmt = task.redOptions.rcReductionSlider.Value
        task.pcSplit(0).ConvertTo(split(0), cv.MatType.CV_32S, 1000 / reduceAmt)
        task.pcSplit(1).ConvertTo(split(1), cv.MatType.CV_32S, 1000 / reduceAmt)
        task.pcSplit(2).ConvertTo(split(2), cv.MatType.CV_32S, 1000 / reduceAmt)

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
        mm = GetMinMax(dst2)

        If standaloneTest() Then
            Static plot As New Plot_Histogram
            If task.heartBeat Then
                plot.createHistogram = True
                plot.maxRange = mm.maxVal
                plot.Run(dst2)
                dst3 = plot.dst2
                labels(3) = CStr(mm.maxVal) + " different levels in the prepared data."
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
            For Each rc In task.redCells
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
        dst2 = ShowPalette(rCloud.dst3)
        labels(2) = rCloud.labels(2)
    End Sub
End Class






Public Class RedCloud_XZ : Inherits TaskParent
    Dim stats As New Cell_Basics
    Dim rCloud As New RedCloud_Basics
    Public Sub New()
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
        dst2 = ShowPalette(rCloud.dst3)
        labels(2) = rCloud.labels(2)
    End Sub
End Class






Public Class RedCloud_World : Inherits TaskParent
    Dim rCloud As New RedCloud_Basics
    Dim world As New Depth_World
    Public Sub New()
        optiBase.FindSlider("RedCloud_Basics Reduction").Value = 1000
        labels(3) = "Generated pointcloud"
        desc = "Display the output of a generated pointcloud as RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        world.Run(src)
        task.pcSplit = world.dst2.Split()

        rCloud.Run(src)
        dst2 = rCloud.dst3
        labels(2) = rCloud.labels(2)
    End Sub
End Class





Public Class RedCloud_Combine : Inherits TaskParent
    Dim color8U As New Color8U_Basics
    Public guided As New GuidedBP_Depth
    Public combinedCells As New List(Of rcData)
    Dim maxDepth As New Depth_MaxMask
    Dim rCloud As New RedCloud_Basics
    Public Sub New()
        desc = "Combine the color and cloud as indicated in the RedOptions panel."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        maxDepth.Run(src)
        If task.redOptions.UseColorOnly.Checked Or task.redOptions.UseGuidedProjection.Checked Then
            task.redC.inputRemoved.SetTo(0)
            If src.Channels() = 3 Then
                color8U.Run(src)
                dst2 = color8U.dst2.Clone
            Else
                dst2 = src
            End If
        Else
            task.redC.inputRemoved = task.noDepthMask
            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        End If

        If task.redOptions.UseDepth.Checked Or task.redOptions.UseGuidedProjection.Checked Then
            Select Case task.redOptions.depthInputIndex
                Case 0 ' "GuidedBP_Depth"
                    guided.Run(src)
                    If color8U.classCount > 0 Then guided.dst2 += color8U.classCount
                    guided.dst2.CopyTo(dst2, task.depthMask)
                Case 1 ' "RedCloud_Basics"
                    rCloud.Run(task.pointCloud)
                    If color8U.classCount > 0 Then rCloud.dst2 += color8U.classCount
                    rCloud.dst2.CopyTo(dst2, task.depthMask)
            End Select
        End If

        dst2 = runRedC(dst2, labels(2))

        combinedCells.Clear()
        Dim drawRectOnlyRun As Boolean
        If task.drawRect.Width * task.drawRect.Height > 10 Then drawRectOnlyRun = True
        For Each rc In task.redCells
            If drawRectOnlyRun Then If task.drawRect.Contains(rc.floodPoint) = False Then Continue For
            combinedCells.Add(rc)
        Next
        labels(2) = CStr(combinedCells.Count) + " cells were found.  Dots indicate maxDist points."
    End Sub
End Class







Public Class RedCloud_Depth : Inherits TaskParent
    Dim flood As New Flood_Basics
    Public Sub New()
        task.redOptions.UseDepth.Checked = True
        desc = "Create RedCloud output using only depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        flood.Run(src)
        dst2 = flood.dst2
        labels(2) = flood.labels(2)
    End Sub
End Class