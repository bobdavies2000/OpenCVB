Imports cvb = OpenCvSharp
Public Class Linear_Basics : Inherits VB_Parent
    Dim inputX As New Linear_InputX
    Dim inputY As New Linear_InputY
    Dim inputZ As New Linear_InputZ
    Public options As New Options_LinearInput
    Public cloud As New cvb.Mat
    Public Sub New()
        desc = "Confine derivatives to linear values"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        inputZ.Run(src)
        Dim mask As cvb.Mat = inputZ.dst2

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
                        If vNext > val And vNext > 0 Then Exit For Else fixCount += 1
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

        cvb.Cv2.Merge({dst2, dst3, task.pcSplit(2)}, cloud)
    End Sub
End Class





Public Class Linear_Visualize : Inherits VB_Parent
    Public plotHist As New Plot_Histogram
    Public roi As New cvb.Rect(0, 0, dst2.Width, dst2.Height)
    Public pc As cvb.Mat
    Public options As New Options_LinearInput
    Dim mats As New Mat_4to1
    Dim matPlots As New Mat_4to1
    Public Sub New()
        plotHist.createHistogram = True
        plotHist.removeZeroEntry = True

        'If standalone Then task.gOptions.setDisplay1()
        'labels(1) = "Mask of differences > deltaZ (only last shown)"
        labels(3) = "Histograms showing the range of pointcloud differences for X, Y, and Z"
        desc = "Provide a mask for pixels that are within x mm depth of its neighbor"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        Dim r1 As cvb.Rect, r2 As cvb.Rect

        For i = 0 To task.pcSplit.Count - 1
            pc = task.pcSplit(i)(roi)

            ' toggle between the pixel to the right or below
            If task.toggleOnOff Then
                r1 = New cvb.Rect(0, 0, task.cols - 1, task.rows)
                r2 = New cvb.Rect(1, 0, r1.Width, r1.Height)
            Else
                r1 = New cvb.Rect(0, 0, task.cols, task.rows - 1)
                r2 = New cvb.Rect(0, 1, r1.Width, r1.Height)
            End If

            cvb.Cv2.Absdiff(pc(r2), pc(r1), dst0)

            mats.mat(i) = dst0.Resize(roi.Size, 0, 0, cvb.InterpolationFlags.Nearest)
            dst1 = mats.mat(i).Threshold(options.delta, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs

            mats.mat(i).SetTo(0, dst1)

            If task.optionsChanged Then
                plotHist.minRange = 0
                plotHist.maxRange = options.delta
            End If
            labels(2) = "Pointcloud data where neighbors are less than " +
                        CStr(CInt(options.delta * 1000)) + " mm's apart in the X, Y, or Z direction"
            plotHist.Run(mats.mat(i))
            matPlots.mat(i) = plotHist.dst2.Clone
            If i = 2 Then mats.mat(2) = mats.mat(2).Threshold(0, 255, cvb.ThresholdTypes.Binary)
            mats.mat(i) = mats.mat(i).Normalize(0, 255, cvb.NormTypes.MinMax).ConvertScaleAbs
        Next

        mats.Run(empty)
        dst2 = mats.dst2

        matPlots.Run(empty)
        dst3 = matPlots.dst2

        SetTrueText("Lower left is a mask showing where depth is" + vbCrLf + "within " +
                     Str(CInt(options.delta * 1000)) + " mm's of its neighbor" + vbCrLf + vbCrLf +
                     "Toggle is between using the pixel to the right " + vbCrLf + "or below",
                     New cvb.Point(task.cols / 2 + 5, task.rows / 2 + 5))
    End Sub
End Class





Public Class Linear_Input : Inherits VB_Parent
    Public plotHist As New Plot_Histogram
    Public roi As New cvb.Rect(0, 0, dst2.Width, dst2.Height)
    Public pc As cvb.Mat
    Public options As New Options_LinearInput
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()

        plotHist.createHistogram = True
        plotHist.removeZeroEntry = True

        labels = {"", "Mask of differences > deltaX", "Point Cloud deltaX data", ""}
        desc = "Find pixels that are withing X mm's of a neighbor in the X direction"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        pc = task.pcSplit(options.dimension)(roi)

        ' use the pixel below for Y dimension
        Dim r1 As cvb.Rect, r2 As cvb.Rect
        If options.dimension <> 1 Then
            r1 = New cvb.Rect(0, 0, task.cols - 1, task.rows)
            r2 = New cvb.Rect(1, 0, r1.Width, r1.Height)
        Else
            r1 = New cvb.Rect(0, 0, task.cols, task.rows - 1)
            r2 = New cvb.Rect(0, 1, r1.Width, r1.Height)
        End If

        cvb.Cv2.Absdiff(pc(r2), pc(r1), dst0)

        dst2 = dst0.Resize(roi.Size, 0, 0, cvb.InterpolationFlags.Nearest)
        dst1 = dst2.Threshold(options.delta, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs

        dst2.SetTo(0, dst1)
        Dim msg = Choose(options.dimension + 1, "X direction", "Y direction", "Z direction")
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





Public Class Linear_InputX : Inherits VB_Parent
    Dim input As New Linear_Input
    Public Sub New()
        FindRadio("X Direction").Checked = True
        desc = "Find pixels that are withing X mm's of a neighbor in the X direction"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        input.Run(src)
        dst2 = input.dst2
        labels = input.labels
    End Sub
End Class




Public Class Linear_InputY : Inherits VB_Parent
    Dim input As New Linear_Input
    Public Sub New()
        FindRadio("Y Direction").Checked = True
        desc = "Find pixels that are withing X mm's of a neighbor in the Y direction"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        input.Run(src)
        dst2 = input.dst2
        labels = input.labels
    End Sub
End Class





Public Class Linear_InputZ : Inherits VB_Parent
    Dim input As New Linear_Input
    Public Sub New()
        FindRadio("Z Direction").Checked = True
        desc = "Find pixels that are withing X mm's of a neighbor in the Z direction"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        input.Run(src)
        dst2 = input.dst2
        dst3 = dst2.Threshold(0, 255, cvb.ThresholdTypes.Binary)
        labels = input.labels
    End Sub
End Class
