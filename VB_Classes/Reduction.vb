Imports cvb = OpenCvSharp
Public Class Reduction_Basics : Inherits TaskParent
    Public classCount As Integer
    Public Sub New()
        task.redOptions.enableReductionTypeGroup(True)
        task.redOptions.enableReductionSliders(True)
        desc = "Reduction: a simpler way to KMeans by reducing color resolution"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        If task.redOptions.reductionType = "Use Bitwise Reduction" Then
            Dim bits = task.redOptions.getBitReductionBar()
            classCount = 255 / Math.Pow(2, bits)
            Dim zeroBits = Math.Pow(2, bits) - 1
            dst2 = src And New cvb.Mat(src.Size(), src.Type, cvb.Scalar.All(255 - zeroBits))
            dst2 = dst2 / zeroBits
        ElseIf task.redOptions.reductionType = "Use Simple Reduction" Then
            Dim reductionVal = task.redOptions.getSimpleReductionBar()
            classCount = Math.Ceiling(255 / reductionVal)

            dst2 = src / reductionVal
            labels(2) = "Reduced image - factor = " + CStr(task.redOptions.getSimpleReductionBar())
        Else
            dst2 = src
            labels(2) = "No reduction requested"
        End If

        dst3 = ShowPalette(dst2 * 255 / classCount)
        labels(2) = CStr(classCount) + " colors after reduction"
    End Sub
End Class





Public Class Reduction_Floodfill : Inherits TaskParent
    Public reduction As New Reduction_Basics
    Public redC As New RedCloud_Basics
    Public Sub New()
        task.redOptions.setIdentifyCells(True)
        task.redOptions.setUseColorOnly(True)
        labels(2) = "Reduced input to floodfill"
        task.redOptions.setBitReductionBar(32)
        desc = "Use the reduction output as input to floodfill to get masks of cells."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        reduction.Run(src)
        dst2 = ShowPalette(reduction.dst2 * 255 / reduction.classCount)
        redC.Run(reduction.dst2)
        dst3 = redC.dst2
        labels(3) = redC.labels(3)
    End Sub
End Class









Public Class Reduction_HeatMapLines : Inherits TaskParent
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
    Public Sub RunAlg(src As cvb.Mat)
        reduction.Run(src)
        heat.Run(src)

        lines.Run(heat.dst2)
        setupTop.Run(heat.dst2)
        dst2 = setupTop.dst2
        dst2.SetTo(cvb.Scalar.White, lines.dst3)

        lines.Run(heat.dst3)
        setupSide.Run(heat.dst3)
        dst3 = setupSide.dst2
        dst3.SetTo(cvb.Scalar.White, lines.dst3)
    End Sub
End Class






Public Class Reduction_PointCloud : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Public Sub New()
        task.redOptions.checkSimpleReduction(True)
        task.redOptions.setBitReductionBar(20)
        labels = {"", "", "8-bit reduced depth", "Palettized output of the different depth levels found"}
        desc = "Use reduction to smooth depth data"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Type <> cvb.MatType.CV_32FC3 Then src = task.pcSplit(2)

        src *= 255 / task.MaxZmeters
        src.ConvertTo(dst0, cvb.MatType.CV_32S)
        reduction.Run(dst0)
        reduction.dst2.ConvertTo(dst2, cvb.MatType.CV_32F)

        dst2.ConvertTo(dst2, cvb.MatType.CV_8U)
        dst3 = ShowPalette(dst2 * 255 / reduction.classCount)
    End Sub
End Class






Public Class Reduction_XYZ : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim options As New Options_Reduction
    Public Sub New()
        task.redOptions.SimpleReductionBar.Maximum = 1000
        task.redOptions.setBitReductionBar(400)
        desc = "Use reduction to slice the point cloud in 3 dimensions"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If src.Type <> cvb.MatType.CV_32FC3 Then src = task.pointCloud
        Dim split = src.Split()
        For i = 0 To split.Length - 1
            If options.reduceXYZ(i) Then
                split(i) *= 1000
                split(i).ConvertTo(dst0, cvb.MatType.CV_32S)
                reduction.Run(dst0)
                Dim mm As mmData = GetMinMax(reduction.dst2)
                reduction.dst2.ConvertTo(split(i), cvb.MatType.CV_32F)
            End If
        Next

        cvb.Cv2.Merge(split, dst3)
        dst3.SetTo(0, task.noDepthMask)
        SetTrueText("Task.PointCloud (or 32fc3 input) has been reduced and is in dst3")
    End Sub
End Class









Public Class Reduction_Edges : Inherits TaskParent
    Dim edges As New Edge_Laplacian
    Dim reduction As New Reduction_Basics
    Public Sub New()
        task.redOptions.checkSimpleReduction(True)
        desc = "Get the edges after reducing the image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        reduction.Run(src)
        dst2 = reduction.dst2 * 255 / reduction.classCount

        Dim reductionRequested = True
        If task.redOptions.reductionType = "No Reduction" Then reductionRequested = False
        labels(2) = If(reductionRequested, "Reduced image", "Original image")
        labels(3) = If(reductionRequested, "Laplacian edges of reduced image", "Laplacian edges of original image")
        edges.Run(dst2)
        dst3 = edges.dst2
    End Sub
End Class










Public Class Reduction_Histogram : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim plot As New Plot_Histogram
    Public Sub New()
        plot.createHistogram = True
        plot.removeZeroEntry = False
        labels = {"", "", "Reduction image", "Histogram of the reduction"}
        desc = "Visualize a reduction with a histogram"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        reduction.Run(src)
        dst2 = reduction.dst2 * 255 / reduction.classCount

        plot.Run(dst2)
        dst3 = plot.dst2

        labels(2) = "ClassCount = " + CStr(reduction.classCount)
    End Sub
End Class





Public Class Reduction_BGR : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim mats As New Mat_4Click
    Public Sub New()
        desc = "Reduce BGR image in parallel"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim split = src.Split

        For i = 0 To 2
            reduction.Run(split(i))
            If standaloneTest() Then mats.mat(i) = ShowPalette(reduction.dst2 * 255 / reduction.classCount)
            split(0) = reduction.dst2.Clone
        Next

        If standaloneTest() Then
            mats.mat(3) = (mats.mat(0) + mats.mat(1) + mats.mat(2))
            mats.Run(empty)
            dst3 = mats.dst2
        End If

        cvb.Cv2.Merge(split, dst2)
    End Sub
End Class
