Imports System.Windows.Documents
Imports cv = OpenCvSharp
Public Class LeftRight_Basics : Inherits VB_Algorithm
    Dim Options As New Options_LeftRight
    Public Sub New()
        If task.cameraName = "MYNT-EYE-D1000" Then findSlider("Brightness Alpha (contrast)").Value = 1100
        labels = {"", "", "Left camera image", If(task.cameraName = "Azure Kinect 4K", "No right image", "Right camera image")}
        desc = "Enhance the left/right views with brightness and contrast."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Options.RunVB()

        If task.cameraName = "StereoLabs ZED 2/2i" Then
            dst2 = task.leftview
            dst3 = task.rightview
        Else
            dst2 = (task.leftview * cv.Scalar.All(Options.alpha) + Options.beta).ToMat
            dst3 = (task.rightview * cv.Scalar.All(Options.alpha) + Options.beta).ToMat
        End If
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





Public Class LeftRight_GoodFeatures : Inherits VB_Algorithm
    Public good As New Feature_Basics
    Public Sub New()
        findSlider("Sample Size").Value = 200
        findSlider("Min Distance to next").Value = 10
        desc = "Find GoodFeatures in the left and right depalettized infrared images"
        labels(2) = "Left Image"
        labels(3) = "Right Image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        good.Run(task.leftview)
        task.rightview.CopyTo(dst3)
        For i = 0 To good.corners.Count - 1
            dst3.Circle(good.corners(i), task.dotSize + 1, cv.Scalar.White, -1, task.lineType)
        Next

        good.Run(task.leftview)
        task.leftview.CopyTo(dst2)
        For i = 0 To good.corners.Count - 1
            dst2.Circle(good.corners(i), task.dotSize + 1, cv.Scalar.White, -1, task.lineType)
        Next
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









Public Class LeftRight_FloodFill : Inherits VB_Algorithm
    Dim flood As New Flood_RedColor
    Public Sub New()
        desc = "Use floodfill on both the left and right images"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static leftCells As New List(Of rcData)
        Static rightCells As New List(Of rcData)
        Static leftMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Static rightMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)

        task.redCells = New List(Of rcData)(leftCells)
        task.cellMap = leftMap.Clone

        flood.Run(task.leftview)
        dst2 = flood.dst2.Clone

        leftCells = New List(Of rcData)(task.redCells)
        leftMap = task.cellMap.Clone

        task.redCells = New List(Of rcData)(rightCells)
        task.cellMap = rightMap.Clone

        flood.Run(task.rightview)
        dst3 = flood.dst2.Clone

        rightCells = New List(Of rcData)(task.redCells)
        rightMap = task.cellMap.Clone
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
    Dim reduction As New Reduction_Basics
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
        If standalone Then gOptions.displayDst0.Checked = True
        If standalone Then gOptions.displayDst1.Checked = True
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels = {"", "", "Reduced Left Image", "Reduced Right Image"}
        desc = "Use the left/right reductions to find hard markers - neighboring pixels of identical values"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redView.Run(src)
        dst2 = redView.dst2
        dst3 = redView.dst3

        ' find combinations in the left image - they are markers.
        Dim impList As New List(Of List(Of Integer))
        Dim lineLen = gOptions.DebugSlider.Value
        For y = 0 To dst2.Height - 1
            Dim important As New List(Of Integer)
            Dim impCounts As New List(Of Integer)
            For x = 0 To dst2.Width - 1
                Dim m1 = dst2.Get(Of Byte)(y, x)
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

        For i = 0 To dst2.Rows - 1
            Dim important = impList(i * 2)
            Dim impcounts = impList(i * 2 + 1)
            Dim maxVal = important(impcounts.IndexOf(impcounts.Max))

            Dim tmp = dst2.Row(i).InRange(maxVal, maxVal)
            dst0.Row(i).SetTo(255, tmp)

            tmp = dst3.Row(i).InRange(maxVal, maxVal)
            dst1.Row(i).SetTo(255, tmp)
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
