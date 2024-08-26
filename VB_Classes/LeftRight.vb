Imports cvb = OpenCvSharp
Public Class LeftRight_Basics : Inherits VB_Parent
    Public Sub New()
        If task.cameraName = "MYNT-EYE-D1000" Then FindSlider("Alpha (contrast)").Value = 1100
        labels = {"", "", "Left camera image", If(task.cameraName = "Azure Kinect 4K", "No right image", "Right camera image")}
        desc = "Display the left and right views as they came from the camera."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = task.leftView
        dst3 = task.rightView
    End Sub
End Class







Public Class LeftRight_CompareRaw : Inherits VB_Parent
    Dim options As New Options_LeftRight
    Public Sub New()
        desc = "Show slices of the left and right view next to each other for visual comparison"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim r1 = New cvb.Rect(0, options.sliceY, task.leftView.Width, options.sliceHeight)
        Dim r2 = New cvb.Rect(0, 25, task.leftView.Width, options.sliceHeight)
        dst2.SetTo(0)
        task.leftView(r1).CopyTo(dst2(r2))

        r2.Y += options.sliceHeight
        task.rightView(r1).CopyTo(dst2(r2))
        dst3 = task.rightView
    End Sub
End Class





Public Class LeftRight_Palettized : Inherits VB_Parent
    Public Sub New()
        desc = "Add color to the 8-bit infrared images."
        labels(2) = "Left Image"
        labels(3) = "Right Image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = ShowPalette(task.leftview)
        dst3 = ShowPalette(task.rightview)
    End Sub
End Class








Public Class LeftRight_BRISK : Inherits VB_Parent
    Dim brisk As New BRISK_Basics
    Dim options As New Options_Features
    Public Sub New()
        FindSlider("Min Distance").Value = 20
        labels = {"", "", "Left Image", "Right Image"}
        desc = "Add color to the 8-bit infrared images."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        brisk.Run(task.leftview)
        dst2 = brisk.dst2.Clone

        brisk.Run(task.rightview)
        dst3 = brisk.dst2.Clone
    End Sub
End Class







Public Class LeftRight_Edges : Inherits VB_Parent
    Dim edges As New Edge_Canny
    Public Sub New()
        desc = "Display the edges in the left and right views"
        labels(2) = "Left Image"
        labels(3) = "Right Image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        edges.Run(task.leftview)
        dst2 = edges.dst2

        edges.Run(task.rightview)
        dst3 = edges.dst2
    End Sub
End Class






Public Class LeftRight_Reduction : Inherits VB_Parent
    Public reduction As New Reduction_Basics
    Public Sub New()
        labels = {"", "", "Reduced Left Image", "Reduced Right Image"}
        desc = "Reduce both the left and right color images"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        reduction.Run(task.leftview)
        dst2 = reduction.dst2.Clone

        reduction.Run(task.rightview)
        dst3 = reduction.dst2.Clone
    End Sub
End Class






Public Class LeftRight_Markers : Inherits VB_Parent
    Dim redView As New LeftRight_Reduction
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        If standaloneTest() Then task.gOptions.setDisplay1()
        dst0 = New cvb.Mat(dst0.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        dst1 = New cvb.Mat(dst1.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        labels = {"", "", "Reduced Left Image", "Reduced Right Image"}
        desc = "Use the left/right reductions to find hard markers - neighboring pixels of identical values"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redView.Run(src)
        dst2 = redView.reduction.dst3.Clone
        dst3 = redView.reduction.dst3.Clone

        Dim left = redView.dst2
        Dim right = redView.dst3

        ' find combinations in the left image - they are markers.
        Dim impList As New List(Of List(Of Integer))
        Dim lineLen = task.gOptions.DebugSliderValue
        For y = 0 To left.Height - 1
            Dim important As New List(Of Integer)
            Dim impCounts As New List(Of Integer)
            For x = 0 To left.Width - 1
                Dim m1 = left.Get(Of Byte)(y, x)
                If important.Contains(m1) = False Then
                    important.Add(m1)
                    impCounts.Add(1)
                Else
                    impCounts(important.IndexOf(m1)) += 1
                End If
            Next
            impList.Add(important)
            impList.Add(impCounts)
        Next

        dst0.SetTo(0)
        dst1.SetTo(0)

        For i = 0 To left.Rows - 1
            Dim important = impList(i * 2)
            Dim impcounts = impList(i * 2 + 1)
            Dim maxVal = important(impcounts.IndexOf(impcounts.Max))

            Dim tmp = left.Row(i).InRange(maxVal, maxVal)
            dst0.Row(i).SetTo(255, tmp)

            tmp = right.Row(i).InRange(maxVal, maxVal)
            dst1.Row(i).SetTo(255, tmp)
        Next
    End Sub
End Class








Public Class LeftRight_Markers1 : Inherits VB_Parent
    Dim redView As New LeftRight_Reduction
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        labels = {"", "", "Reduced Left Image", "Reduced Right Image"}
        desc = "Use the left/right reductions to find markers - neighboring pixels of identical values"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redView.Run(src)
        dst0 = redView.dst2
        dst1 = redView.dst3

        ' find combinations in the left image - they are markers.
        Dim impList As New List(Of List(Of Integer))
        Dim lineLen = task.gOptions.DebugSliderValue
        For y = 0 To dst2.Height - 1
            Dim important As New List(Of Integer)
            Dim impCounts As New List(Of Integer)
            For x = 0 To dst0.Width - 1
                Dim m1 = dst0.Get(Of Byte)(y, x)
                If important.Contains(m1) = False Then
                    important.Add(m1)
                    impCounts.Add(1)
                Else
                    impCounts(important.IndexOf(m1)) += 1
                End If
            Next
            impList.Add(important)
            impList.Add(impCounts)
        Next

        dst2.SetTo(0)
        dst3.SetTo(0)

        For i = 0 To dst2.Rows - 1
            Dim important = impList(i * 2)
            Dim impcounts = impList(i * 2 + 1)
            Dim maxVal = important(impcounts.IndexOf(impcounts.Max))

            Dim tmp = dst0.Row(i).InRange(maxVal, maxVal)
            dst2.Row(i).SetTo(255, tmp)

            tmp = dst1.Row(i).InRange(maxVal, maxVal)
            dst3.Row(i).SetTo(255, tmp)
        Next
    End Sub
End Class







Public Class LeftRight_Lines : Inherits VB_Parent
    Dim lines As New Line_Basics
    Public Sub New()
        labels = {"", "", "Left camera lines", "Right camera lines"}
        desc = "Find the lines in the Left and Right images."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lines.Run(task.leftview)
        dst2 = lines.dst2.Clone

        lines.Run(task.rightview)
        dst3 = lines.dst2
    End Sub
End Class








Public Class LeftRight_RedCloudRight : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        task.redOptions.setUseColorOnly(True)
        desc = "Segment the right view image with RedCloud"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(task.rightView)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
    End Sub
End Class







Public Class LeftRight_RedCloudLeft : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        task.redOptions.setUseColorOnly(True)
        desc = "Segment the left view image with RedCloud"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(task.leftView)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
    End Sub
End Class








Public Class LeftRight_RedCloudBoth : Inherits VB_Parent
    Dim stLeft As New LeftRight_RedCloudRight
    Dim stRight As New LeftRight_RedCloudLeft
    Public Sub New()
        desc = "Match cells in the left view to the right view - something is flipped here..."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        stRight.Run(empty)
        dst2 = stRight.dst2
        labels(2) = "Left view - " + stRight.labels(2)

        stLeft.Run(empty)
        dst3 = stLeft.dst2
        labels(3) = "Right view - " + stLeft.labels(2)
    End Sub
End Class






Public Class LeftRight_Features : Inherits VB_Parent
    Dim feat As New FeatureLeftRight_Basics
    Public Sub New()
        desc = "Placeholder to make it easier to find FeatureLeftRight_Basics"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)
        dst2 = feat.dst2
        dst3 = feat.dst3
        labels = feat.labels
    End Sub
End Class
