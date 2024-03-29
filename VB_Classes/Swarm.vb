Imports cv = OpenCvSharp
Public Class Swarm_Basics : Inherits VB_Algorithm
    Dim knn As New KNN_Core
    Dim feat As New Feature_Basics
    Public mpList As New List(Of pointPair)
    Public distance As Single
    Public direction As Single
    Public Sub New()
        findSlider("Feature Sample Size").Value = 1000
        findSlider("Blocksize").Value = 1
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Track the GoodFeatures across a frame history and connect the first and last good.corners in the history."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        dst3 = feat.dst2

        Static cornerHistory As New List(Of List(Of cv.Point2f))
        If task.optionsChanged Then cornerHistory.Clear()

        Dim histCount = task.frameHistoryCount
        cornerHistory.Add(New List(Of cv.Point2f)(feat.featurePoints))

        Dim lastIndex = cornerHistory.Count - 1
        knn.trainInput = New List(Of cv.Point2f)(cornerHistory.ElementAt(0))
        knn.queries = New List(Of cv.Point2f)(cornerHistory.ElementAt(lastIndex))
        If knn.queries.Count = 0 Then Exit Sub
        knn.Run(empty)

        dst2.SetTo(0)
        mpList.Clear()
        Dim distanceList As New List(Of Single)
        Dim directionList As New List(Of Single) ' angle in radians
        For i = 0 To Math.Min(knn.neighbors.Count, knn.trainInput.Count) - 1
            Dim trainIndex = knn.neighbors(i)(0) ' index of the matched train input
            Dim pt = knn.trainInput(trainIndex)
            Dim ptNew = knn.queries(i)
            Dim nextDist = pt.DistanceTo(ptNew)
            distanceList.Add(nextDist)
            dst2.Line(pt, ptNew, 255, task.lineWidth, task.lineType)
            If distance > 0 Then
                If pt.Y <> ptNew.Y Then
                    Dim nextDirection = Math.Atan((pt.X - ptNew.X) / (pt.Y - ptNew.Y))
                    directionList.Add(nextDirection)
                End If
            End If
            mpList.Add(New pointPair(pt, ptNew))
        Next
        labels(3) = CStr(mpList.Count) + " points were matched to the previous set of features."
        distance = 0
        If distanceList.Count > 10 Then
            distance = distanceList.Average
            labels(2) = Format(distance, fmt1) + " average distance with max = " + Format(distanceList.Max, fmt1) + " (all units in pixels.)"
        End If
        If directionList.Count > 0 Then
            direction = directionList.Average
            labels(3) = Format(direction, fmt1) + " average direction (radians) with max = " + Format(directionList.Max, fmt1)
        End If
        If cornerHistory.Count >= histCount Then cornerHistory.RemoveAt(0)
    End Sub
End Class






Public Class Swarm_LeftRightFeatures : Inherits VB_Algorithm
    Public leftList As New List(Of cv.Point2f)
    Public rightList As New List(Of cv.Point2f)
    Dim feat As New Feature_Basics
    Public Sub New()
        labels = {"", "", "Left view feature points", "Right view feature points"}
        desc = "Double the votes on motion by collecting features for both left and right images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(task.leftView)
        leftList = New List(Of cv.Point2f)(feat.featurePoints)
        dst2 = feat.dst2.Clone

        feat.Run(task.rightView)
        rightList = New List(Of cv.Point2f)(feat.featurePoints)
        dst3 = feat.dst2.Clone
    End Sub
End Class






Public Class Swarm_LeftRight : Inherits VB_Algorithm
    Public leftDistance As Single
    Public leftDirection As Single
    Public rightDistance As Single
    Public rightDirection As Single
    Dim swarm As New Swarm_Basics
    Public Sub New()
        labels = {"", "", "Left view feature points", "Right view feature points"}
        desc = "Get direction and distance from the left and right images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        swarm.Run(task.leftView)
        leftDistance = swarm.distance
        leftdirection = swarm.direction
        dst2 = swarm.dst2.Clone

        swarm.Run(task.rightView)
        rightDistance = swarm.distance
        rightDirection = swarm.direction
        dst3 = swarm.dst2

        strOut = "Left distance/direction = " + Format(leftDistance, fmt1) + "/" + Format(leftDirection, fmt1) + vbCrLf
        strOut += "Right distance/direction = " + Format(rightDistance, fmt1) + "/" + Format(rightDirection, fmt1)
        setTrueText(strOut, 3)
    End Sub
End Class
