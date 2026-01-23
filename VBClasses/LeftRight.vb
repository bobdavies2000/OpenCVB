Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class LeftRight_Basics : Inherits TaskParent
        Public meanLeft As Double
        Public meanRight As Double
        Dim brightness = New Brightness_Basics
        Public Sub New()
            labels = {"", "", "Left camera image", "Right camera image"}
            desc = "Display the left and right views as they came from the camera."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            brightness.run(task.leftView)
            Dim tmpLeft As cv.Mat = brightness.dst2 ' input array conflict
            task.leftView = tmpLeft.Normalize(100, 150, cv.NormTypes.MinMax)
            If standaloneTest() Then dst2 = task.leftView

            brightness.run(task.rightView)
            Dim tmpRight As cv.Mat = brightness.dst2 ' inputarray conflict
            task.rightView = tmpRight.Normalize(100, 150, cv.NormTypes.MinMax)
            If standaloneTest() Then dst3 = task.rightView
        End Sub
    End Class






    Public Class NR_LeftRight_RawLeft : Inherits TaskParent
        Public Sub New()
            task.drawRect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
            desc = "Match the raw left image with the color image with a drawRect"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src
            If task.drawRect.Width > 0 And task.drawRect.Height > 0 Then dst3 = src(task.drawRect)
        End Sub
    End Class





    Public Class NR_LeftRight_Palettized : Inherits TaskParent
        Public Sub New()
            desc = "Add color to the 8-bit infrared images."
            labels(2) = "Left Image"
            labels(3) = "Right Image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = PaletteFull(task.leftView)
            dst3 = PaletteFull(task.rightView)
        End Sub
    End Class








    Public Class NR_LeftRight_BRISK : Inherits TaskParent
        Dim brisk As New BRISK_Basics
        Dim options As New Options_Features
        Public Sub New()
            OptionParent.FindSlider("Min Distance").Value = 20
            labels = {"", "", "Left Image", "Right Image"}
            desc = "Find BRISK features in the left and right images."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            brisk.Run(task.leftView)
            dst2 = brisk.dst2.Clone

            brisk.Run(task.rightView)
            dst3 = brisk.dst2.Clone
        End Sub
    End Class







    Public Class LeftRight_Reduction : Inherits TaskParent
        Public reduction As New Reduction_Basics
        Public Sub New()
            labels = {"", "", "Reduced Left Image", "Reduced Right Image"}
            desc = "Reduce both the left and right color images"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            reduction.Run(task.leftView)
            dst2 = reduction.dst2.Clone

            reduction.Run(task.rightView)
            dst3 = reduction.dst2.Clone
        End Sub
    End Class






    Public Class LeftRight_RedRightGray : Inherits TaskParent
        Dim color8u As New Color8U_Basics
        Public redMask As New RedMask_Basics
        Public Sub New()
            desc = "Segment the right view image with RedMask_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            color8u.Run(task.rightView)
            redMask.Run(color8u.dst2)
            dst2 = redMask.dst2.Clone
            dst3 = PaletteFull(dst2)
            labels = redMask.labels
        End Sub
    End Class





    Public Class LeftRight_RedLeftGray : Inherits TaskParent
        Dim color8u As New Color8U_Basics
        Public redMask As New RedMask_Basics
        Public Sub New()
            desc = "Segment the left view image with RedMask_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            color8u.Run(task.leftView)
            redMask.Run(color8u.dst2)
            dst2 = redMask.dst2.Clone
            dst3 = PaletteFull(dst2)
            labels = redMask.labels
        End Sub
    End Class





    Public Class NR_LeftRight_RGBAlignLeft : Inherits TaskParent
        Dim options As New Options_RGBAlign
        Public Sub New()
            desc = "This is a crude method to align the left image with the RGB for the D435i camera only..."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.settings.cameraName <> "Intel(R) RealSense(TM) Depth Camera 435i" Then
                SetTrueText("This is just a crude way to align the left and rgb images." + vbCrLf +
                        "The parameters are set for only the Intel D435i camera.")
                Exit Sub
            End If

            options.Run()

            Dim w = dst0.Width
            Dim h = dst0.Height
            Dim xD = options.xDisp
            Dim yD = options.yDisp
            Dim xS = options.xShift
            Dim yS = options.yShift
            Dim rect = New cv.Rect(xD + xS, yD + yS, w - xD * 2, h - yD * 2)
            dst2 = task.leftView(rect).Resize(dst0.Size)

            dst3 = ShowAddweighted(dst2, src, labels(3))
        End Sub
    End Class






    Public Class NR_LeftRight_ContourLeft : Inherits TaskParent
        Dim color8U As New Color8U_Basics
        Public Sub New()
            If task.contours Is Nothing Then task.contours = New Contour_Basics_List
            desc = "Segment the left view with contour_basics_List"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            color8U.Run(task.leftView)
            task.contours.Run(color8U.dst2)
            dst2 = task.contours.dst2
        End Sub
    End Class







    Public Class NR_LeftRight_Edges : Inherits TaskParent
        Dim edges As New Edge_Basics
        Public Sub New()
            desc = "Display the edges in the left and right views"
            labels(2) = "Left Image"
            labels(3) = "Right Image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edges.Run(task.leftView)
            dst2 = edges.dst2

            edges.Run(task.rightView)
            dst3 = edges.dst2
        End Sub
    End Class







    Public Class NR_LeftRight_EdgesColor : Inherits TaskParent
        Dim edges As New Edge_Basics
        Public Sub New()
            If standalone Then task.gOptions.displayDst0.Checked = True
            desc = "Display the edges in the left, right, and color views"
            labels(2) = "Left Image"
            labels(3) = "Right Image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edges.Run(task.gray)
            dst0 = edges.dst2.Clone

            edges.Run(task.leftView)
            dst2 = edges.dst2.Clone

            edges.Run(task.rightView)
            dst3 = edges.dst2
        End Sub
    End Class





    Public Class LeftRight_RedMask : Inherits TaskParent
        Dim redLeft As New LeftRight_RedLeft
        Dim redRight As New LeftRight_RedRight
        Public Sub New()
            desc = "Display the RedMask_Basics output for both the left and right images."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redLeft.Run(task.leftView)
            dst2 = redLeft.dst2.Clone
            If standaloneTest() Then
                For Each md In redLeft.redMask.mdList
                    DrawCircle(dst2, md.maxDist, task.DotSize, task.highlight)
                Next
            End If

            redRight.Run(task.rightView)
            dst3 = redRight.dst2.Clone
            If standaloneTest() Then
                For Each md In redRight.redMask.mdList
                    DrawCircle(dst3, md.maxDist, task.DotSize, task.highlight)
                Next
            End If
            labels(2) = redLeft.labels(2)
            labels(3) = redRight.labels(2)
        End Sub
    End Class





    Public Class LeftRight_RedRight : Inherits TaskParent
        Dim fLess As New FeatureLess_Basics
        Public redMask As New RedMask_Basics
        Public Sub New()
            desc = "Segment the right view image with RedMask_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3 = task.leftView
            fLess.Run(dst3)

            dst2 = fLess.dst2
            redMask.Run(fLess.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            dst2 = PaletteFull(redMask.dst2)
            labels(2) = redMask.labels(2)
        End Sub
    End Class







    Public Class LeftRight_RedLeft : Inherits TaskParent
        Dim fLess As New FeatureLess_Basics
        Public redMask As New RedMask_Basics
        Public Sub New()
            desc = "Segment the left view image with RedMask_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3 = task.leftView
            fLess.Run(dst3)

            redMask.Run(fLess.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            dst2 = PaletteFull(redMask.dst2)
            labels(2) = redMask.labels(2)
        End Sub
    End Class
End Namespace
