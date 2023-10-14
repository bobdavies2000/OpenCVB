Imports cv = OpenCvSharp
Public Class SteadyCam_Basics : Inherits VB_Algorithm
    Dim flow As New Font_FlowText
    Dim features As New Feature_tCellTracker
    Dim plot As New Plot_OverTimeSingle
    Public Sub New()
        flow.dst = RESULT_DST1
        If standalone Then gOptions.displayDst1.Checked = True
        findSlider("Sample Size").Value = 100
        labels(3) = "Plot of fraction of points that matched - move camera to see what happens."
        desc = "Find the Feature_Points and determine if most are steady.  If so, the camera is not moving."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        features.Run(src)
        dst2 = src

        If features.tcells.Count = 0 Then
            setTrueText("There were no points available to steady the camera.", 3)
            Exit Sub
        End If

        Static lastImage As cv.Mat = dst2.Clone
        For Each tc In features.tcells
            dst2.Circle(tc.center, task.dotSize, cv.Scalar.White, -1, cv.LineTypes.Link8)
        Next

        Dim hitCount As Integer
        For Each tc In features.tcells
            Dim val = lastImage.Get(Of cv.Vec3b)(tc.center.Y, tc.center.X)
            If val = white Then hitCount += 1
        Next

        Dim percent = hitCount / features.tcells.Count
        If standalone Then
            flow.msgs.Add(Format(hitCount, "00") + " tracked points were identical - " + Format(percent, "00%") + " of the points identified")
            flow.Run(src)
        End If

        plot.plotData = percent
        lastImage = dst2.Clone
        plot.Run(src)
        dst3 = plot.dst2
        labels(2) = features.labels(2)
    End Sub
End Class