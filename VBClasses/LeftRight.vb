Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class LeftRight_Basics : Inherits TaskParent
        Public Sub New()
            labels = {"", "", "Left camera image", "Right camera image"}
            desc = "Display the left and right views as they came from the camera."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = task.leftView
            dst3 = task.rightView
        End Sub
    End Class







    Public Class LeftRight_Raw : Inherits TaskParent
        Dim options As New Options_LeftRight
        Public Sub New()
            desc = "Show slices of the left and right view next to each other for visual comparison"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst2 = task.leftView
            dst3 = task.rightView

            'Dim r1 = New cv.Rect(0, options.sliceY, task.leftView.Width, options.sliceHeight)
            'Dim r2 = New cv.Rect(0, 25, task.leftView.Width, options.sliceHeight)
            'dst2.SetTo(0)
            'task.leftView(r1).CopyTo(dst2(r2))

            'r2.Y += options.sliceHeight
            'task.rightView(r1).CopyTo(dst2(r2))
            'dst3 = task.rightView
        End Sub
    End Class







    Public Class LeftRight_RawLeft : Inherits TaskParent
        Public Sub New()
            task.drawRect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
            desc = "Match the raw left image with the color image with a drawRect"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src
            If task.drawRect.Width > 0 And task.drawRect.Height > 0 Then dst3 = src(task.drawRect)
        End Sub
    End Class





    Public Class LeftRight_Palettized : Inherits TaskParent
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








    Public Class LeftRight_BRISK : Inherits TaskParent
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





    Public Class LeftRight_RGBAlignLeft : Inherits TaskParent
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






    Public Class LeftRight_ContourLeft : Inherits TaskParent
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







    Public Class LeftRight_Edges : Inherits TaskParent
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







    Public Class LeftRight_EdgesColor : Inherits TaskParent
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
End Namespace