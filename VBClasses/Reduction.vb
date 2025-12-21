Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Reduction_Basics : Inherits TaskParent
        Public classCount As Integer
        Public alwaysDisplay As Boolean
        Public options As New Options_Reduction
        Public Sub New()
            desc = "Reduction: a simpler way to KMeans by reducing color resolution"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            If options.bitwiseReduction Then
                Dim bits = options.bitwiseValue
                classCount = 255 / Math.Pow(2, bits)
                Dim zeroBits = Math.Pow(2, bits) - 1
                dst2 = src And New cv.Mat(src.Size(), src.Type, cv.Scalar.All(255 - zeroBits))
                dst2 = dst2 / zeroBits
            ElseIf options.simpleReduction Then
                classCount = Math.Ceiling(255 / options.simpleReductionValue)

                dst2 = src / options.simpleReductionValue + 1 ' map the input to values ranging from 1 to X - no zeros allowed.
                labels(2) = "Reduced image - factor = " + CStr(options.simpleReductionValue)
            Else
                dst2 = src
                labels(2) = "No reduction requested"
            End If

            If standaloneTest() Or alwaysDisplay Then dst3 = PaletteBlackZero(dst2)
            labels(2) = CStr(classCount) + " colors after reduction - 8uC1 below"
        End Sub
    End Class





    Public Class Reduction_Floodfill : Inherits TaskParent
        Public reduction As New Reduction_Basics
        Public Sub New()
            labels(2) = "Reduced input to floodfill"
            desc = "Use the reduction output as input to floodfill to get masks of cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            reduction.Run(src)
            dst2 = PaletteFull(reduction.dst2)
            dst3 = runRedList(reduction.dst2, labels(3))
        End Sub
    End Class









    Public Class Reduction_HeatMapLines : Inherits TaskParent
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







    Public Class Reduction_XYZ : Inherits TaskParent
        Dim reduction As New Reduction_Basics
        Dim options As New Options_ReductionXYZ
        Public Sub New()
            OptionParent.FindSlider("Simple Reduction").Maximum = 1000
            OptionParent.FindSlider("Simple Reduction").Value = 400
            desc = "Use reduction to slice the point cloud in 3 dimensions"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If src.Type <> cv.MatType.CV_32FC3 Then src = taskAlg.pointCloud
            Dim split = src.Split()
            For i = 0 To split.Length - 1
                If options.reduceXYZ(i) Then
                    split(i) *= 1000
                    split(i).ConvertTo(dst0, cv.MatType.CV_32S)
                    reduction.Run(dst0)
                    Dim mm As mmData = GetMinMax(reduction.dst2)
                    reduction.dst2.ConvertTo(split(i), cv.MatType.CV_32F)
                End If
            Next

            cv.Cv2.Merge(split, dst3)
            dst3.SetTo(0, taskAlg.noDepthMask)
            SetTrueText("taskAlg.PointCloud (or 32fc3 input) has been reduced and is in dst3")
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
        Public Overrides Sub RunAlg(src As cv.Mat)
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
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim split = src.Split

            For i = 0 To 2
                reduction.Run(split(i))
                If standaloneTest() Then
                    mats.mat(i) = PaletteFull(reduction.dst2)
                End If
            Next

            mats.mat(3) = (mats.mat(0) + mats.mat(1) + mats.mat(2))
            mats.Run(emptyMat)
            dst3 = mats.dst2

            cv.Cv2.Merge(split, dst2)
        End Sub
    End Class







    Public Class Reduction_MotionTest : Inherits TaskParent
        Dim reduction As New Reduction_Basics
        Dim diff As New Diff_Basics
        Public Sub New()
            reduction.alwaysDisplay = True
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Compare reduction with and without motion."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)

            reduction.Run(src)
            dst2 = reduction.dst3
            If taskAlg.optionsChanged Then
                dst3 = dst2
            Else
                dst2.CopyTo(dst3, taskAlg.motionMask)

                diff.lastFrame = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                diff.Run(dst3)
                dst1 = diff.dst2
            End If
        End Sub
    End Class






    Public Class Reduction_PointCloud : Inherits TaskParent
        Dim reduction As New Reduction_Basics
        Public Sub New()
            OptionParent.findRadio("Use Simple Reduction").Checked = True
            labels = {"", "", "8-bit reduced depth", "Palettized output of the different depth levels found"}
            desc = "Use reduction to smooth depth data"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Type <> cv.MatType.CV_32FC3 Then src = taskAlg.pcSplit(2)

            src *= 255 / taskAlg.MaxZmeters
            src.ConvertTo(dst0, cv.MatType.CV_32S)
            reduction.Run(dst0)
            reduction.dst2.ConvertTo(dst2, cv.MatType.CV_32F)

            dst2.ConvertTo(dst2, cv.MatType.CV_8U)
            dst3 = PaletteFull(dst2 + 1)
        End Sub
    End Class




    Public Class Reduction_Edges : Inherits TaskParent
        Dim edges As New Edge_Laplacian
        Dim reduction As New Reduction_Basics
        Public Sub New()
            OptionParent.findRadio("Use Simple Reduction").Checked = True
            desc = "Get the edges after reducing the image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            reduction.Run(src)
            dst2 = reduction.dst2 * 255 / reduction.classCount

            labels(2) = If(reduction.options.noReduction, "Reduced image", "Original image")
            labels(3) = If(reduction.options.noReduction, "Laplacian edges of reduced image", "Laplacian edges of original image")
            edges.Run(dst2)
            dst3 = edges.dst2
        End Sub
    End Class





    Public Class Reduction_NoDepth : Inherits TaskParent
        Dim reduction As New Reduction_Basics
        Public Sub New()
            desc = "Reduce the grayscale image where there is no depth."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3 = taskAlg.grayStable
            dst3.SetTo(0, taskAlg.depthMask)
            reduction.Run(dst3)
            dst2 = reduction.dst3
        End Sub
    End Class
End Namespace