Imports cv = OpenCvSharp
Public Class LeftRight_Basics : Inherits TaskParent
    Public Sub New()
        If task.cameraName = "MYNT-EYE-D1000" Then OptionParent.FindSlider("Alpha (contrast)").Value = 1100
#If AZURE_SUPPORT Then
        labels = {"", "", "Left camera image", If(task.cameraName = "Azure Kinect 4K", "No right image", "Right camera image")}
#Else
        labels = {"", "", "Left camera image", "Right camera image"}
#End If
        desc = "Display the left and right views as they came from the camera."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView
        dst3 = task.rightView
    End Sub
End Class







Public Class LeftRight_CompareRaw : Inherits TaskParent
    Dim options As New Options_LeftRight
    Public Sub New()
        desc = "Show slices of the left and right view next to each other for visual comparison"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim r1 = New cv.Rect(0, options.sliceY, task.leftView.Width, options.sliceHeight)
        Dim r2 = New cv.Rect(0, 25, task.leftView.Width, options.sliceHeight)
        dst2.SetTo(0)
        task.leftView(r1).CopyTo(dst2(r2))

        r2.Y += options.sliceHeight
        task.rightView(r1).CopyTo(dst2(r2))
        dst3 = task.rightView
    End Sub
End Class





Public Class LeftRight_Palettized : Inherits TaskParent
    Public Sub New()
        desc = "Add color to the 8-bit infrared images."
        labels(2) = "Left Image"
        labels(3) = "Right Image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = ShowPalette(task.leftView)
        dst3 = ShowPalette(task.rightView)
    End Sub
End Class








Public Class LeftRight_BRISK : Inherits TaskParent
    Dim brisk As New BRISK_Basics
    Dim options As New Options_Features
    Public Sub New()
        task.featureOptions.DistanceSlider.Value = 20
        labels = {"", "", "Left Image", "Right Image"}
        desc = "Add color to the 8-bit infrared images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        brisk.Run(task.leftView)
        dst2 = brisk.dst2.Clone

        brisk.Run(task.rightView)
        dst3 = brisk.dst2.Clone
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
        dst3 = ShowPalette(dst2)
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
        dst3 = ShowPalette(dst2)
        labels = redMask.labels
    End Sub
End Class





Public Class LeftRight_RGBAlignLeft : Inherits TaskParent
    Dim options As New Options_RGBAlign
    Public Sub New()
        desc = "This is a crude method to align the left image with the RGB for the D435i camera only..."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.cameraName <> "Intel(R) RealSense(TM) Depth Camera 435i" Then
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








Public Class LeftRight_Lines : Inherits TaskParent
    Public leftLines As New List(Of lpData)
    Public rightLines As New List(Of lpData)
    Dim lines As New LineRGB_RawSorted
    Public Sub New()
        labels = {"", "", "Left image lines", "Right image lines"}
        desc = "Find the lines in the Left and Right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        leftLines = New List(Of lpData)(task.lineRGB.lpList)
        dst2 = task.leftView.Clone
        For Each lp In leftLines
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next
        labels(2) = "There were " + CStr(leftLines.Count) + " lines found in the left view"

        lines.Run(task.rightView.Clone)
        rightLines = New List(Of lpData)(lines.lpList)
        dst3 = task.rightView.Clone
        For Each lp In rightLines
            dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next
        labels(3) = "There were " + CStr(rightLines.Count) + " lines found in the right view"
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
        dst3 = task.rightView.Clone
        fLess.Run(task.rightView)
        dst2 = fLess.dst2
        redMask.Run(fLess.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst2 = ShowPalette(redMask.dst2)
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
        fLess.Run(src)
        redMask.Run(fLess.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst2 = ShowPalette(redMask.dst2)
        labels(2) = redMask.labels(2)
    End Sub
End Class




Public Class LeftRight_ContourLeft : Inherits TaskParent
    Dim contours As New Contour_Basics_List
    Dim color8U As New Color8U_Basics
    Public Sub New()
        task.redOptions.ColorSource.SelectedItem = "Reduction_Basics"
        desc = "Segment the left view with contour_basics_List"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        color8U.Run(task.leftView)
        contours.Run(color8U.dst2)
        dst2 = contours.dst2
    End Sub
End Class
