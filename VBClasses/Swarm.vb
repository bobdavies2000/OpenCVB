Imports VBClasses
Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class Swarm_Basics : Inherits TaskParent
    Public knn As New KNN_Basics
    Public lpList As New List(Of lpData)
    Public distanceAvg As Single
    Public directionAvg As Single
    Public distanceMax As Single
    Public options As New Options_Swarm
    Public optionsEx As New Options_Features
    Dim cornerHistory As New List(Of List(Of cv.Point))
    Dim feat As New Feature_Basics
    Public Sub New()
        task.fOptions.FrameHistoryCount.Value = task.fOptions.FrameHistoryCount.Maximum
        dst2 = New Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0))
        dst3 = New Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0))
        desc = "Track the GoodFeatures across a frame history and connect the first and last good.corners in the history."
    End Sub
    Public Function DrawLines() As Mat
        Dim dst = New Mat(dst2.Size, MatType.CV_8U, 0)
        Dim queries = knn.queries
        Dim trainInput = knn.trainInput
        For i = 0 To queries.Count - 1
            Dim pt = queries(i)
            For j = 0 To Math.Min(knn.trainInput.Count, options.ptCount) - 1
                Dim ptNew = trainInput(knn.result(i, j))
                Line(dst, pt, ptNew, white, task.lineWidth, task.lineType)
                If ptNew.X < options.border Then Line(dst, New Point2f(0, ptNew.Y), ptNew, white, task.lineWidth, task.lineType)
                If ptNew.Y < options.border Then Line(dst, New Point2f(ptNew.X, 0), ptNew, white, task.lineWidth, task.lineType)
                If ptNew.X > dst.Width - options.border Then Line(dst, New Point2f(dst.Width, ptNew.Y), ptNew, white, task.lineWidth, task.lineType)
                If ptNew.Y > dst.Height - options.border Then Line(dst, New Point2f(ptNew.X, dst.Height), ptNew, white, task.lineWidth, task.lineType)
            Next
        Next
        Return dst
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        feat.Run(task.gray)

        dst3 = feat.dst2

        If task.optionsChanged Then cornerHistory.Clear()

        Dim histCount = task.fOptions.FrameHistoryCount.Value
        cornerHistory.Add(feat.features)

        Dim lastIndex = cornerHistory.Count - 1
        knn.ptListTrain = New List(Of cv.Point)(cornerHistory.ElementAt(0))
        knn.ptListQuery = New List(Of cv.Point)(cornerHistory.ElementAt(lastIndex))
        knn.Run(src)

        dst2.SetTo(0)
        lpList.Clear()
        Dim disList As New List(Of Single)
        Dim dirList As New List(Of Single) ' angle in radians
        For i = 0 To knn.queries.Count - 1
            Dim trainIndex = knn.result(i, 0) ' index of the matched train input
            Dim pt = knn.queries(i)
            Dim ptNew = knn.trainInput(trainIndex)
            Dim nextDist = pt.DistanceTo(ptNew)
            Line(dst2, pt, ptNew, white, task.lineWidth, task.lineType)
            disList.Add(nextDist)
            lpList.Add(New lpData(pt, ptNew))
            If nextDist > 0 Then
                If pt.Y <> ptNew.Y Then
                    Dim nextDirection = Math.Atan((pt.X - ptNew.X) / (pt.Y - ptNew.Y))
                    dirList.Add(nextDirection)
                End If
            End If
        Next
        dst2 = DrawLines().Clone

        labels(3) = CStr(lpList.Count) + " points were matched to the previous set of features."
        distanceAvg = 0
        If task.heartBeat Then distanceMax = 0
        If disList.Count > 10 Then
            distanceAvg = disList.Average
            distanceMax = Math.Max(distanceMax, disList.Max)
            labels(2) = "Avg distance = " + distanceAvg.ToString(fmt1) + vbCrLf + "Max Distance = " + distanceMax.ToString(fmt1) + " (all units in pixels) "
        End If
        If dirList.Count > 0 Then
            directionAvg = dirList.Average
            labels(3) += " " + directionAvg.ToString(fmt1) + " average direction (radians)"
        End If
        If cornerHistory.Count >= histCount Then cornerHistory.RemoveAt(0)
    End Sub
End Class









Public Class XR_Swarm_LeftRight : Inherits TaskParent
    Public leftDistance As Single
    Public leftDirection As Single
    Public leftMax As Single
    Public rightDistance As Single
    Public rightDirection As Single
    Public rightMax As Single
    Dim swarm As New Swarm_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "", "Left view feature points", "Right view feature points"}
        desc = "Get direction and distance from the left and right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        swarm.Run(task.leftView)
        leftDistance = swarm.distanceAvg
        leftDirection = swarm.directionAvg
        leftMax = swarm.distanceMax
        dst2 = task.leftView
        dst2.SetTo(Scalar.White, swarm.DrawLines())

        swarm.Run(task.rightView)
        rightDistance = swarm.distanceAvg
        rightDirection = swarm.directionAvg
        rightMax = swarm.distanceMax
        dst3 = task.rightView
        dst3.SetTo(Scalar.White, swarm.DrawLines())

        strOut = swarm.labels(2) + vbCrLf + swarm.labels(3)
        SetTrueText(strOut, 1)
    End Sub
End Class
