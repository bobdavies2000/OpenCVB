Imports cv = OpenCvSharp
Public Class Projection_Basics : Inherits VB_Algorithm
    Dim heat As New HeatMap_Basics
    Dim redC As New RedCloud_Basics
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_32FC3, 0)
        If standalone Then gOptions.displayDst1.Checked = True
        gOptions.useHistoryCloud.Checked = False
        labels = {"", "Top View projection of the selected cell", "RedCloud_Basics output - select a cell to project at right and above", "Side projection of the selected cell"}
        desc = "Create a top and side projection of the selected cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        heat.Run(src)
        dst1 = heat.dst2.Clone
        dst3 = heat.dst3.Clone

        Dim rc = task.rcSelect

        dst0.SetTo(0)
        task.pointCloud(rc.rect).CopyTo(dst0(rc.rect), rc.mask)

        heat.Run(dst0)
        Dim maskTop = heat.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(0, 255, cv.ThresholdTypes.Binary)
        Dim maskSide = heat.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(0, 255, cv.ThresholdTypes.Binary)
        If maskTop.CountNonZero = 0 And maskSide.CountNonZero = 0 Then setTrueText("The selected cell has no depth data.", 3)
        dst1.SetTo(cv.Scalar.White, maskTop)
        dst3.SetTo(cv.Scalar.White, maskSide)
    End Sub
End Class







Public Class Projection_Lines : Inherits VB_Algorithm
    Dim heat As New HeatMap_Basics
    Dim lines As New Line_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Concentration threshold", 0, 100, 2)
        findCheckBox("Top View (Unchecked Side View)").Checked = False
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
