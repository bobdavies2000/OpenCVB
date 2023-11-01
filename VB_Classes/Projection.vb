Imports cv = OpenCvSharp
Public Class Projection_Basics : Inherits VB_Algorithm
    Dim heat As New HeatMap_Basics
    Dim redC As New RedCloud_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"", "Top View projection of the selected cell", "RedCloud_Basics output - select a cell to project at right and above", "Side projection of the selected cell"}
        desc = "Create a top and side projection of the selected cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
        End If

        Dim input = task.pointCloud(task.rcSelect.rect)
        input.SetTo(0, Not task.rcSelect.mask)
        heat.Run(input)
        dst1 = heat.dst2
        dst3 = heat.dst3
        setTrueText("Select a cell to see projections", 3)
    End Sub
End Class








Public Class Projection_FeatureLess : Inherits VB_Algorithm
    Public fLess As New Flood_RedColor
    Dim heat As New HeatMap_Basics
    Public Sub New()
        labels = {"", "", "FeatureLess cells identified by RedCloud", "Click on any FeatureLess cell to see projection below"}
        desc = "Use projection for RedCloud FeatureLess cells"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fLess.Run(src)
        dst2 = fLess.dst2
        labels(2) = fLess.labels(2)

        Dim rc = task.rcSelect
        Dim input = task.pointCloud(rc.rect)
        input.SetTo(0, Not rc.mask)
        heat.Run(input)
        dst3 = heat.dst0.Threshold(0, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class







Public Class Projection_Lines : Inherits VB_Algorithm
    Dim heat As New HeatMap_Basics
    Dim lines As New Line_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Concentration threshold", 0, 100, 2)
        findCheckBox("Top View (Unchecked Side View)").Checked = False
        findCheckBox("Show Frustrum").Checked = False
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels = {"", "Lines found in the threshold output", "FeatureLess cells found", "Projections of each of the FeatureLess cells"}
        desc = "Search for surfaces among the FeatureLess regions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = findSlider("Concentration threshold")
        Static topCheck = findCheckBox("Top View (Unchecked Side View)")
        If heartBeat() Then
            dst1.SetTo(0)
            dst3.SetTo(0)
        End If
        heat.Run(src)
        If topCheck.checked Then dst2 = heat.dst2 Else dst2 = heat.dst3
        dst1 = dst2.Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary)

        lines.Run(dst1)
        dst3 += lines.dst3
    End Sub
End Class
