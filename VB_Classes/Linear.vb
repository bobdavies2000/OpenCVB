﻿Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Linear_Basics : Inherits TaskParent
    Dim inputX As New Linear_InputX
    Dim inputY As New Linear_InputY
    Dim inputZ As New Linear_InputZ
    Public options As New Options_LinearInput
    Public cloud As New cv.Mat
    Public Sub New()
        desc = "Confine derivatives to linear values"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        inputZ.Run(src)
        Dim mask As cv.Mat = inputZ.dst2

        inputX.Run(src)
        dst2 = inputX.dst2.Clone
        dst0 = task.pcSplit(0).Clone
        Dim fixCount As Integer
        For y = 0 To mask.Height - 1
            For x = 0 To mask.Width - 1
                Dim val = dst0.Get(Of Single)(y, x)
                Dim mVal = mask.Get(Of Byte)(y, x)
                If mVal Then
                    Dim i = x + 1
                    Dim vNext As Single
                    For i = i To mask.Width - 1
                        vNext = dst0.Get(Of Single)(y, i)
                        If vNext > val And vNext <> 0 Then Exit For Else fixCount += 1
                    Next

                    Dim incr = (vNext - val) / (i - x)
                    For j = x + 1 To i
                        dst0.Set(Of Single)(y, j, val + incr)
                        incr += incr
                    Next
                    x = i
                End If
            Next
        Next

        inputY.Run(src)
        dst3 = inputY.dst2.Clone
        'dst1 = task.pcSplit(1).Clone
        'For y = 0 To mask.Height - 1
        '    For x = 0 To mask.Width - 1
        '        Dim val = dst1.Get(Of Single)(y, x) 
        '        Dim mVal = mask.Get(Of Byte)(y, x)
        '        If mval Then
        '            Dim i = y + 1
        '            Dim vNext As Single
        '            For i = i To mask.Height - 1
        '                vNext = dst1.Get(Of Single)(i, x)
        '                If vNext > val Then Exit For
        '            Next

        '            Dim incr = (vNext - val) / (i - y)
        '            For j = y + 1 To i
        '                dst1.Set(Of Single)(j, x, val + incr)
        '                incr += incr
        '            Next
        '        End If
        '    Next
        'Next

        cv.Cv2.Merge({dst2, dst3, task.pcSplit(2)}, cloud)
    End Sub
End Class





Public Class Linear_Visualize : Inherits TaskParent
    Public plotHist As New Plot_Histogram
    Public roi As New cv.Rect(0, 0, dst2.Width, dst2.Height)
    Public pc As cv.Mat
    Public options As New Options_LinearInput
    Dim mats As New Mat_4to1
    Dim matPlots As New Mat_4to1
    Public Sub New()
        plotHist.createHistogram = True
        plotHist.removeZeroEntry = True

        'If standalone Then task.gOptions.displaydst1.checked = true
        'labels(1) = "Mask of differences > deltaZ (only last shown)"
        labels(3) = "Histograms showing the range of pointcloud differences for X, Y, and Z"
        desc = "Provide a mask for pixels that are within x mm depth of its neighbor"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()
        Dim r1 As cv.Rect, r2 As cv.Rect

        For i = 0 To task.pcSplit.Count - 1
            pc = task.pcSplit(i)(roi)

            ' toggle between the pixel to the right or below
            If task.toggleOnOff Then
                r1 = New cv.Rect(0, 0, task.cols - 1, task.rows)
                r2 = New cv.Rect(1, 0, r1.Width, r1.Height)
            Else
                r1 = New cv.Rect(0, 0, task.cols, task.rows - 1)
                r2 = New cv.Rect(0, 1, r1.Width, r1.Height)
            End If

            cv.Cv2.Absdiff(pc(r2), pc(r1), dst0)

            mats.mat(i) = dst0.Resize(roi.Size, 0, 0, cv.InterpolationFlags.Nearest)
            dst1 = mats.mat(i).Threshold(options.delta, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs

            mats.mat(i).SetTo(0, dst1)

            If task.optionsChanged Then
                plotHist.minRange = 0
                plotHist.maxRange = options.delta
            End If
            labels(2) = "Pointcloud data where neighbors are less than " +
                        CStr(CInt(options.delta * 1000)) + " mm's apart in the X, Y, or Z direction"
            plotHist.Run(mats.mat(i))
            matPlots.mat(i) = plotHist.dst2.Clone
            If i = 2 Then mats.mat(2) = mats.mat(2).Threshold(0, 255, cv.ThresholdTypes.Binary)
            mats.mat(i) = mats.mat(i).Normalize(0, 255, cv.NormTypes.MinMax).ConvertScaleAbs
        Next

        mats.Run(src)
        dst2 = mats.dst2

        matPlots.Run(src)
        dst3 = matPlots.dst2

        SetTrueText("Lower left is a mask showing where depth is" + vbCrLf + "within " +
                     Str(CInt(options.delta * 1000)) + " mm's of its neighbor" + vbCrLf + vbCrLf +
                     "Toggle is between using the pixel to the right " + vbCrLf + "or below",
                     New cv.Point(task.cols / 2 + 5, task.rows / 2 + 5))
    End Sub
End Class





Public Class Linear_Input : Inherits TaskParent
    Public plotHist As New Plot_Histogram
    Public roi As New cv.Rect(0, 0, dst2.Width, dst2.Height)
    Public pc As cv.Mat
    Public options As New Options_LinearInput
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true

        plotHist.createHistogram = True
        plotHist.removeZeroEntry = True

        labels = {"", "Mask of differences > deltaX", "Point Cloud deltaX data", ""}
        desc = "Find pixels that are withing X mm's of a neighbor in the X direction"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        pc = task.pcSplit(options.dimension)(roi)

        ' use the pixel below for Y dimension
        Dim r1 As cv.Rect, r2 As cv.Rect
        If options.dimension <> 1 Or (options.dimension = 2 And options.zy) Then
            r1 = New cv.Rect(0, 0, task.cols - 1, task.rows)
            r2 = New cv.Rect(1, 0, r1.Width, r1.Height)
        Else
            r1 = New cv.Rect(0, 0, task.cols, task.rows - 1)
            r2 = New cv.Rect(0, 1, r1.Width, r1.Height)
        End If

        cv.Cv2.Absdiff(pc(r2), pc(r1), dst0)

        dst2 = dst0.Resize(roi.Size, 0, 0, cv.InterpolationFlags.Nearest)
        dst1 = dst2.Threshold(options.delta, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs

        dst2.SetTo(0, dst1)
        Dim msg = Choose(options.dimension + 1, "X direction", "Y direction", "Z in X-direction")
        labels(2) = "Pointcloud data in " + msg + " where neighbors are less than " +
                    CStr(CInt(options.delta * 1000)) + " mm's apart"
        If standaloneTest() Then
            If task.optionsChanged Then
                plotHist.minRange = 0
                plotHist.maxRange = options.delta
                labels(3) = "0 to " + CStr(CInt(options.delta * 1000)) + " mm's difference from neighbor "
            End If
            plotHist.Run(dst2)
            dst3 = plotHist.dst2
        End If
    End Sub
End Class





Public Class Linear_InputX : Inherits TaskParent
    Dim input As New Linear_Input
    Public Sub New()
        optibase.findRadio("X Direction").Checked = True
        desc = "Find pixels that are withing X mm's of a neighbor in the X direction"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        input.Run(src)
        dst2 = input.dst2
        labels = input.labels
    End Sub
End Class




Public Class Linear_InputY : Inherits TaskParent
    Dim input As New Linear_Input
    Public Sub New()
        optibase.findRadio("Y Direction").Checked = True
        desc = "Find pixels that are withing X mm's of a neighbor in the Y direction"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        input.Run(src)
        dst2 = input.dst2
        labels = input.labels
    End Sub
End Class





Public Class Linear_InputZ : Inherits TaskParent
    Dim input As New Linear_Input
    Public Sub New()
        optibase.findRadio("Z in X-Direction").Checked = True
        desc = "Find pixels that are withing X mm's of a neighbor in the Z direction"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        input.Run(src)
        dst2 = input.dst2
        dst3 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        labels = input.labels
    End Sub
End Class






Public Class Linear_Slices : Inherits TaskParent
    Dim options As New Options_LinearInput
    Dim plotSLR As New SLR_Basics
    Public Sub New()
        plotSLR.plot.minX = 0
        plotSLR.plot.maxX = dst2.Width
        labels(3) = "Move mouse in the depth image above to display line of data."
        desc = "Isolate and display a line segment through the point cloud data"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        Dim pt = task.mouseMovePoint
        If standalone And task.mouseMovePoint = newPoint Then
            pt = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        End If

        Dim rowCol As cv.Mat, p1 As cv.Point, p2 As cv.Point
        If options.dimension = 1 Or (options.dimension = 2 And options.zy) Then
            rowCol = task.pcSplit(options.dimension).Col(pt.X).Clone
            rowCol = cv.Mat.FromPixelData(1, rowCol.Rows, cv.MatType.CV_32FC1, rowCol.Data)
            p1 = New cv.Point(pt.X, 0)
            p2 = New cv.Point(pt.X, dst2.Height)
            plotSLR.plot.minY = -task.yRange
            plotSLR.plot.maxY = task.yRange
        Else
            rowCol = task.pcSplit(options.dimension).Row(pt.Y)
            p1 = New cv.Point(0, pt.Y)
            p2 = New cv.Point(dst2.Width, pt.Y)
            plotSLR.plot.minY = -task.xRange
            plotSLR.plot.maxY = task.xRange
        End If
        task.depthRGB.Line(p1, p2, task.highlight, task.lineWidth)

        plotSLR.slrCore.inputX.Clear()
        plotSLR.slrCore.inputY.Clear()
        For i = 0 To rowCol.Cols - 1
            plotSLR.slrCore.inputX.Add(i)
            plotSLR.slrCore.inputY.Add(rowCol.Get(Of Single)(0, i))
        Next

        If plotSLR.slrCore.inputX.Count = 0 Then
            SetTrueText("There were no depth points in that line...", 3)
        Else
            plotSLR.Run(src)
            dst2 = plotSLR.dst2
            dst3 = plotSLR.dst3
            If task.heartBeat Then labels(2) = plotSLR.plot.labels(2)
        End If
    End Sub
End Class






Public Class Linear_ImageX : Inherits TaskParent
    Dim options As New Options_SLR
    Dim inputX As New List(Of Double)
    Dim slr As New SLR()
    Public Sub New()
        For i = 0 To dst2.Width - 1
            inputX.Add(i)
        Next
        desc = "Create SLR slices for the X dimension of an entire image"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        dst2 = task.pcSplit(0).Clone

        Dim outputX As New List(Of Double)
        Dim outputY As New List(Of Double)
        Dim output As New List(Of cv.Point2d)

        For y = 0 To dst2.Height - 1
            Dim rowCol As cv.Mat = task.pcSplit(0).Row(y)

            Dim dataY(rowCol.Total - 1) As Single
            Marshal.Copy(rowCol.Data, dataY, 0, dataY.Length)
            Dim inputY As New List(Of Double)
            For Each ele In dataY
                inputY.Add(CDbl(ele))
            Next

            outputX.Clear()
            outputY.Clear()
            slr.SegmentedRegressionFast(inputX, inputY, options.tolerance, options.halfLength,
                                        outputX, outputY)
            For x = 0 To outputY.Count - 1
                dst2.Set(Of Single)(y, x, CSng(outputY(x)))
            Next
        Next
        cv.Cv2.Merge({dst2, task.pcSplit(1), task.pcSplit(2)}, dst3)
        dst3.SetTo(0, task.noDepthMask)
    End Sub
End Class






Public Class Linear_ImageY : Inherits TaskParent
    Dim options As New Options_SLR
    Dim inputX As New List(Of Double)
    Dim slr As New SLR()
    Public Sub New()
        For i = 0 To dst2.Height - 1
            inputX.Add(i)
        Next
        desc = "Create SLR slices for the Y dimension of an entire image"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        dst2 = task.pcSplit(1).Clone

        Dim outputX As New List(Of Double)
        Dim outputY As New List(Of Double)
        Dim output As New List(Of cv.Point2d)

        For x = 0 To dst2.Width - 1
            Dim rowCol = task.pcSplit(1).Col(x).Clone
            rowCol = cv.Mat.FromPixelData(1, rowCol.Rows, cv.MatType.CV_32FC1, rowCol.Data)

            Dim dataY(rowCol.Total - 1) As Single
            Marshal.Copy(rowCol.Data, dataY, 0, dataY.Length)
            Dim inputY As New List(Of Double)
            For Each ele In dataY
                inputY.Add(CDbl(ele))
            Next

            outputX.Clear()
            outputY.Clear()
            slr.SegmentedRegressionFast(inputX, inputY, options.tolerance, options.halfLength,
                                        outputX, outputY)
            For y = 0 To outputY.Count - 1
                dst2.Set(Of Single)(y, x, CSng(outputY(y)))
            Next
        Next
        cv.Cv2.Merge({task.pcSplit(0), dst2, task.pcSplit(2)}, dst3)
        dst3.SetTo(0, task.noDepthMask)
    End Sub
End Class
