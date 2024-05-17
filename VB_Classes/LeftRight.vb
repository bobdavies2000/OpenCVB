Imports System.Windows.Documents
Imports cv = OpenCvSharp
Public Class LeftRight_Basics : Inherits VB_Algorithm
    Public Sub New()
        If task.cameraName = "MYNT-EYE-D1000" Then findSlider("Alpha (contrast)").Value = 1100
        labels = {"", "", "Left camera image", If(task.cameraName = "Azure Kinect 4K", "No right image", "Right camera image")}
        desc = "Display the left and right views as they came from the camera."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = task.leftView
        dst3 = task.rightView
    End Sub
End Class







Public Class LeftRight_CompareRaw : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Slice Starting Y", 0, 300, 25)
            sliders.setupTrackBar("Slice Height", 1, (dst2.Rows - 10) / 2, 20)
        End If

        desc = "Show slices of the left and right view next to each other for visual comparison"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static startYSlider = findSlider("Slice Starting Y")
        Static hSlider = findSlider("Slice Height")

        Dim sliceY = startYSlider.Value
        Dim slideHeight = hSlider.Value
        Dim r1 = New cv.Rect(0, sliceY, task.leftview.Width, slideHeight)
        Dim r2 = New cv.Rect(0, 25, task.leftview.Width, slideHeight)
        dst2.SetTo(0)
        task.leftview(r1).CopyTo(dst2(r2))

        r2.Y += slideHeight
        task.rightview(r1).CopyTo(dst2(r2))
        dst3 = task.rightview
    End Sub
End Class





Public Class LeftRight_Palettized : Inherits VB_Algorithm
    Public Sub New()
        desc = "Add color to the 8-bit infrared images."
        labels(2) = "Left Image"
        labels(3) = "Right Image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = vbPalette(task.leftview)
        dst3 = vbPalette(task.rightview)
    End Sub
End Class








Public Class LeftRight_BRISK : Inherits VB_Algorithm
    Dim brisk As New BRISK_Basics
    Public Sub New()
        findSlider("BRISK Radius Threshold").Value = 20
        labels = {"", "", "Left Image", "Right Image"}
        desc = "Add color to the 8-bit infrared images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        brisk.Run(task.leftview)
        dst2 = brisk.dst2.Clone

        brisk.Run(task.rightview)
        dst3 = brisk.dst2.Clone
    End Sub
End Class







Public Class LeftRight_Edges : Inherits VB_Algorithm
    Dim edges As New Edge_Canny
    Public Sub New()
        desc = "Display the edges in the left and right views"
        labels(2) = "Left Image"
        labels(3) = "Right Image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        edges.Run(task.leftview)
        dst2 = edges.dst2

        edges.Run(task.rightview)
        dst3 = edges.dst2
    End Sub
End Class






Public Class LeftRight_Reduction : Inherits VB_Algorithm
    Public reduction As New Reduction_Basics
    Public Sub New()
        labels = {"", "", "Reduced Left Image", "Reduced Right Image"}
        desc = "Reduce both the left and right color images"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(task.leftview)
        dst2 = reduction.dst2.Clone

        reduction.Run(task.rightview)
        dst3 = reduction.dst2.Clone
    End Sub
End Class






Public Class LeftRight_Markers : Inherits VB_Algorithm
    Dim redView As New LeftRight_Reduction
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst0.Checked = True
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels = {"", "", "Reduced Left Image", "Reduced Right Image"}
        desc = "Use the left/right reductions to find hard markers - neighboring pixels of identical values"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redView.Run(src)
        dst2 = redView.reduction.dst3.Clone
        dst3 = redView.reduction.dst3.Clone

        Dim left = redView.dst2
        Dim right = redView.dst3

        ' find combinations in the left image - they are markers.
        Dim impList As New List(Of List(Of Integer))
        Dim lineLen = gOptions.DebugSlider.Value
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








Public Class LeftRight_Markers1 : Inherits VB_Algorithm
    Dim redView As New LeftRight_Reduction
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels = {"", "", "Reduced Left Image", "Reduced Right Image"}
        desc = "Use the left/right reductions to find markers - neighboring pixels of identical values"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redView.Run(src)
        dst0 = redView.dst2
        dst1 = redView.dst3

        ' find combinations in the left image - they are markers.
        Dim impList As New List(Of List(Of Integer))
        Dim lineLen = gOptions.DebugSlider.Value
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







Public Class LeftRight_Lines : Inherits VB_Algorithm
    Dim lines As New Line_Basics
    Public Sub New()
        labels = {"", "", "Left camera lines", "Right camera lines"}
        desc = "Find the lines in the Left and Right images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lines.Run(task.leftview)
        dst2 = lines.dst2.Clone

        lines.Run(task.rightview)
        dst3 = lines.dst2
    End Sub
End Class








Public Class LeftRight_RedCloudRight : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        redOptions.UseColorOnly.Checked = True
        desc = "Segment the right view image with RedCloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(task.rightView)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
    End Sub
End Class







Public Class LeftRight_RedCloudLeft : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        redOptions.UseColorOnly.Checked = True
        desc = "Segment the left view image with RedCloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(task.leftView)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
    End Sub
End Class








Public Class LeftRight_RedCloudBoth : Inherits VB_Algorithm
    Dim stLeft As New LeftRight_RedCloudRight
    Dim stRight As New LeftRight_RedCloudLeft
    Public Sub New()
        desc = "Match cells in the left view to the right view - something is flipped here..."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        stRight.Run(empty)
        dst2 = stRight.dst2
        labels(2) = "Left view - " + stRight.labels(2)

        stLeft.Run(empty)
        dst3 = stLeft.dst2
        labels(3) = "Right view - " + stLeft.labels(2)
    End Sub
End Class






Public Class LeftRight_Features : Inherits VB_Algorithm
    Dim feat As New FeatureMatch_Basics
    Public Sub New()
        desc = "Placeholder to make it easier to find FeatureMatch_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        dst2 = feat.dst2
        dst3 = feat.dst3
        labels = feat.labels
    End Sub
End Class
