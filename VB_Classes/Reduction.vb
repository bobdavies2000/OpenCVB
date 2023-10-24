Imports cv = OpenCvSharp
Public Class Reduction_Basics : Inherits VB_Algorithm
    Public classCount As Integer
    Public Sub New()
        desc = "Reduction: a simpler way to KMeans by reducing color resolution"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        If redOptions.reduction = OptionsRedCloud.bitwiseReduce Then
            Dim bits = redOptions.BitwiseReductionSlider.Value
            classCount = 255 / Math.Pow(2, bits)
            Dim zeroBits = Math.Pow(2, bits) - 1
            dst0 = src And New cv.Mat(src.Size, src.Type, cv.Scalar.All(255 - zeroBits))
            dst2 = dst0 / zeroBits
        ElseIf redOptions.reduction = OptionsRedCloud.simpleReduce Then
            Dim reductionVal = redOptions.ColorReductionSlider.Value
            classCount = Math.Ceiling(255 / reductionVal)

            dst0 = src / reductionVal
            dst2 = dst0 * reductionVal
            labels(2) = "Reduced image - factor = " + CStr(redOptions.ColorReductionSlider.Value)
        Else
            dst2 = src
            labels(2) = "No reduction requested"
        End If

        If standalone Or testIntermediate(traceName) Then dst3 = vbPalette(dst2)
        labels(2) = CStr(classCount) + " colors after reduction"
    End Sub
End Class





Public Class Reduction_Floodfill : Inherits VB_Algorithm
    Public reduction As New Reduction_Basics
    Public flood As New FloodCell_Basics
    Public Sub New()
        labels(2) = "Reduced input to floodfill"
        redOptions.ColorReductionSlider.Value = 32
        desc = "Use the reduction output as input to floodfill to get masks of large masses."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)
        dst2 = vbPalette(reduction.dst2)
        flood.Run(reduction.dst2)
        dst3 = flood.dst2
        labels(3) = "Floodfill found " + CStr(task.fCells.Count) + " regions"
    End Sub
End Class









Public Class Reduction_HeatMapLines : Inherits VB_Algorithm
    Dim heat As New HeatMap_Basics
    Public lines As New Line_Basics
    Public setupSide As New PointCloud_SetupSide
    Public setupTop As New PointCloud_SetupTop
    Dim reduction As New Reduction_PointCloud
    Public Sub New()
        labels(2) = "Gravity rotated Side View with detected lines"
        labels(3) = "Gravity rotated Top View width detected lines"
        desc = "Present both the top and side view to minimize pixel counts."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)
        heat.Run(src)

        lines.Run(heat.dst2)
        setupTop.Run(heat.dst2)
        dst2 = setupTop.dst2
        dst2.SetTo(cv.Scalar.White, lines.dst3)

        lines.Run(heat.dst3)
        setupSide.Run(heat.dst3)
        dst3 = setupSide.dst2
        dst3.SetTo(cv.Scalar.White, lines.dst3)
    End Sub
End Class






Public Class Reduction_PointCloud : Inherits VB_Algorithm
    Dim reduction As New Reduction_Basics
    Public Sub New()
        redOptions.SimpleReduction.Checked = True
        redOptions.ColorReductionSlider.Value = 20
        labels(2) = "Reduced depth"
        labels(3) = "Palettized output of the different depth levels found"
        desc = "Use reduction to smooth depth data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pcSplit(2)

        src *= 255 / task.maxZmeters
        src.ConvertTo(dst0, cv.MatType.CV_32S)
        reduction.Run(dst0)
        reduction.dst2.ConvertTo(dst2, cv.MatType.CV_32F)

        dst2.ConvertTo(dst2, cv.MatType.CV_8U)
        dst3 = vbPalette(dst2)
    End Sub
End Class






Public Class Reduction_XYZ : Inherits VB_Algorithm
    Dim reduction As New Reduction_Basics
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Slice point cloud in X direction")
            check.addCheckBox("Slice point cloud in Y direction")
            check.addCheckBox("Slice point cloud in Z direction")
            check.Box(0).Checked = True
            check.Box(1).Checked = True
        End If
        redOptions.ColorReductionSlider.Maximum = 1000
        redOptions.ColorReductionSlider.Value = 400
        desc = "Use reduction to slice the point cloud in 3 dimensions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim input = src.Clone
        If input.Type <> cv.MatType.CV_32FC3 Then input = task.pointCloud.Clone
        For i = 0 To task.pcSplit.Length - 1
            If check.Box(i).Checked Then
                task.pcSplit(i) *= 1000
                task.pcSplit(i).ConvertTo(input, cv.MatType.CV_32S)
                reduction.Run(input)
                reduction.dst2.ConvertTo(task.pcSplit(i), cv.MatType.CV_32F)
                task.pcSplit(i) *= 0.001
            End If
        Next

        cv.Cv2.Merge(task.pcSplit, dst3)
        setTrueText("Task.PointCloud has been reduced and is in dst3")
    End Sub
End Class









Public Class Reduction_Edges : Inherits VB_Algorithm
    Dim edges As New Edge_Laplacian
    Dim reduction As New Reduction_Basics
    Public Sub New()
        redOptions.SimpleReduction.Checked = True
        desc = "Get the edges after reducing the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)
        dst2 = reduction.dst2.Clone

        Dim reductionRequested = True
        If redOptions.reduction = OptionsRedCloud.noReduce Then reductionRequested = False
        labels(2) = If(reductionRequested, "Reduced image", "Original image")
        labels(3) = If(reductionRequested, "Laplacian edges of reduced image", "Laplacian edges of original image")
        edges.Run(dst2)
        dst3 = edges.dst2
    End Sub
End Class










Public Class Reduction_Histogram : Inherits VB_Algorithm
    Dim reduction As New Reduction_Basics
    Dim plot As New Plot_Histogram
    Public Sub New()
        plot.createHistogram = True
        plot.noZeroEntry = False
        labels = {"", "", "Reduction image", "Histogram of the reduction"}
        desc = "Visualize a reduction with a histogram"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)
        dst2 = reduction.dst2

        plot.Run(dst2)
        dst3 = plot.dst2

        labels(2) = "ClassCount = " + CStr(reduction.classCount)
    End Sub
End Class






Public Class Reduction_RGB : Inherits VB_Algorithm
    Dim reduction As New Reduction_Basics
    Dim mats As New Mat_4Click
    Public Sub New()
        redOptions.ColorReductionSlider.Value = 200
        desc = "Reduce RGB in parallel"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim split = src.Split

        For i = 0 To 2
            reduction.Run(split(i))
            If standalone Then mats.mat(i) = vbPalette(reduction.dst2)
            split(0) = reduction.dst2.Clone
        Next

        If standalone Then
            mats.mat(3) = (mats.mat(0) + mats.mat(1) + mats.mat(2))
            mats.Run(Nothing)
            dst3 = mats.dst2
        End If

        cv.Cv2.Merge(split, dst2)
    End Sub
End Class









Public Class Reduction_Depth : Inherits VB_Algorithm
    Dim prep As New RedCloud_PrepPointCloud
    Dim colorizer As New Depth_Colorizer_CPP
    Public Sub New()
        desc = "Use reduction to smooth depth data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32S Then
            src = task.pcSplit(2) * 1000
            src.ConvertTo(src, cv.MatType.CV_32S)
        End If
        prep.Run(Nothing)
        dst2 = prep.dst2
        dst2.ConvertTo(dst1, cv.MatType.CV_32F)
        colorizer.Run(dst1 / 1000)
        dst3 = colorizer.dst2
        labels(2) = prep.labels(2)
    End Sub
End Class