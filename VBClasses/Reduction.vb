Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class Reduction_Basics : Inherits TaskParent
    Public classCount As Integer
    Public Sub New()
        desc = "Reduction: a simpler way to KMeans by reducing color resolution"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = task.gray

        Dim reduction = task.fOptions.ReductionColor.Value
        classCount = Math.Ceiling(255 / reduction)

        dst2 = src / reduction
        labels(2) = "Reduced image - factor = " + CStr(reduction)

        dst3 = Palettize(dst2 + 1, 0)

        labels(2) = CStr(classCount) + " colors after reduction - 8uC1 below"
    End Sub
End Class




Public Class Reduction_BasicsParmInput : Inherits TaskParent
    Public classCount As Integer
    Public reductionFactor As Integer = 100
    Public Sub New()
        desc = "Reduction: a simpler way to KMeans by reducing color resolution"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = task.gray

        classCount = Math.Ceiling(255 / reductionFactor)

        dst2 = src / reductionFactor
        labels(2) = "Reduced image - factor = " + CStr(reductionFactor)

        dst3 = Palettize(dst2 + 1, 0)
        labels(2) = CStr(classCount) + " colors after reduction - 8uC1 below"
    End Sub
End Class










Public Class XR_Reduction_HeatMapLines1 : Inherits TaskParent
    Dim heat As New HeatMap_Basics
    Public setupSide As New Cloud_SetupSide
    Public setupTop As New Cloud_SetupTop
    Dim reduction As New Reduction_PointCloud
    Dim core As New Line_Core
    Public Sub New()
        labels(2) = "Gravity rotated Side View with detected lines"
        labels(3) = "Gravity rotated Top View width detected lines"
        desc = "Present both the top and side view to minimize pixel counts."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        reduction.Run(src)
        heat.Run(src)

        Dim _core_cvt As New cv.Mat
        CvtColor(heat.dst2, _core_cvt, cv.ColorConversionCodes.BGR2GRAY)
        core.Run(_core_cvt)

        setupTop.Run(heat.dst2)
        dst2 = setupTop.dst2

        For Each lp In core.lpList
            Line(dst2, lp.p1, lp.p2, white, task.lineWidth)
        Next

        CvtColor(heat.dst3, _core_cvt, cv.ColorConversionCodes.BGR2GRAY)
        core.Run(_core_cvt)

        setupSide.Run(heat.dst3)
        dst3 = setupSide.dst2

        For Each lp In core.lpList
            Line(dst3, lp.p1, lp.p2, white, task.lineWidth)
        Next
    End Sub
End Class






Public Class XR_Reduction_HeatMapLines : Inherits TaskParent
    Dim heat As New HeatMap_Basics
    Public setupSide As New Cloud_SetupSide
    Public setupTop As New Cloud_SetupTop
    Dim reduction As New Reduction_PointCloud
    Dim rawLines As New Line_Core
    Public Sub New()
        labels(2) = "Gravity rotated Side View with detected lines"
        labels(3) = "Gravity rotated Top View width detected lines"
        desc = "Present both the top and side view to minimize pixel counts."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        reduction.Run(src)
        heat.Run(src)

        rawLines.Run(heat.dst2)
        setupTop.Run(heat.dst2)
        dst2 = setupTop.dst2

        rawLines.Run(heat.dst3)
        setupSide.Run(heat.dst3)
        dst3 = setupSide.dst2
    End Sub
End Class






Public Class XR_Reduction_XYZ : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim options As New Options_ReductionXYZ
    Public Sub New()
        desc = "Use reduction to slice the point cloud in 3 dimensions"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud
        Dim splitMats As cv.Mat() = Split(src)
        For i = 0 To splitMats.Length - 1
            If options.reduceXYZ(i) Then
                splitMats(i) *= 1000
                splitMats(i).ConvertTo(dst0, cv.MatType.CV_32S)
                reduction.Run(dst0)
                Dim mm As mmData = GetMinMax(reduction.dst2)
                reduction.dst2.ConvertTo(splitMats(i), cv.MatType.CV_32F)
            End If
        Next

        Merge(splitMats, dst3)
        dst3.SetTo(0, task.noDepthMask)
        SetTrueText("task.PointCloud (or 32fc3 input) has been reduced and is in dst3")
    End Sub
End Class






Public Class XR_Reduction_Histogram : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim plot As New PlotBar_Basics
    Public Sub New()
        plot.createHistogram = True
        plot.removeZeroEntry = False
        labels = {"", "", "Reduction image", "Histogram of the reduction"}
        desc = "Visualize a reduction with a histogram"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        reduction.Run(src)
        dst2 = reduction.dst2 * 255 / reduction.classCount

        plot.Run(dst2)
        dst3 = plot.dst2

        labels(2) = "ClassCount = " + CStr(reduction.classCount)
    End Sub
End Class





Public Class XR_Reduction_BGR : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim mats As New Mat_4Click
    Public Sub New()
        desc = "Reduce BGR image in parallel"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim splitMats As cv.Mat() = Split(src)

        For i = 0 To 2
            reduction.Run(splitMats(i))
            If standaloneTest() Then
                mats.mat(i) = Palettize(reduction.dst2)
            End If
        Next

        mats.mat(3) = (mats.mat(0) + mats.mat(1) + mats.mat(2))
        mats.Run(emptyMat)
        dst3 = mats.dst2

        Merge(splitMats, dst2)
    End Sub
End Class







Public Class XR_Reduction_MotionTest : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim diff As New Diff_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Compare reduction with and without motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)

        reduction.Run(src)
        dst2 = reduction.dst3
        If task.optionsChanged Then
            dst3 = dst2
        Else
            dst2.CopyTo(dst3, task.motion.motionMask)

            CvtColor(dst2, diff.lastFrame, cv.ColorConversionCodes.BGR2GRAY)
            diff.Run(dst3)
            dst1 = diff.dst2
        End If
    End Sub
End Class






Public Class Reduction_PointCloud : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Public Sub New()
        labels = {"", "", "8-bit reduced depth", "Palettized output of the different depth levels found"}
        desc = "Use reduction to smooth depth data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pcSplit(2)

        src *= 255 / task.MaxZmeters
        src.ConvertTo(dst0, cv.MatType.CV_32S)
        reduction.Run(dst0)
        reduction.dst2.ConvertTo(dst2, cv.MatType.CV_32F)

        dst2.ConvertTo(dst2, cv.MatType.CV_8U)
        dst3 = Palettize(dst2 + 1)
    End Sub
End Class




Public Class XR_Reduction_Edges : Inherits TaskParent
    Dim edges As New Edge_Laplacian
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Get the edges after reducing the image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        reduction.Run(src)
        dst2 = reduction.dst2 * 255 / reduction.classCount

        labels(2) = "Reduced image"
        labels(3) = "Laplacian edges of reduced image"
        edges.Run(dst2)
        dst3 = edges.dst2
    End Sub
End Class





Public Class XR_Reduction_NoDepth : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Reduce the grayscale image where there is no depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = task.gray
        dst3.SetTo(0, task.depthmask)
        reduction.Run(dst3)
        dst2 = reduction.dst3
    End Sub
End Class
