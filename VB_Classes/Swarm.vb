Imports cv = OpenCvSharp
Imports System.Windows.Forms
Public Class Swarm_Basics : Inherits TaskParent
    Public knn As New KNN_Basics
    Public lpList As New List(Of linePoints)
    Public distanceAvg As Single
    Public directionAvg As Single
    Public distanceMax As Single
    Public options As New Options_Swarm
    Dim cornerHistory As New List(Of List(Of cv.Point2f))
    Public Sub New()
        Application.DoEvents()
        optiBase.FindSlider("Feature Sample Size").Value = 1000
        ' optiBase.findslider("Blocksize").Value = 1
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Track the GoodFeatures across a frame history and connect the first and last good.corners in the history."
    End Sub
    Public Function DrawLines() As cv.Mat
        Dim dst = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Dim queries = knn.queries
        Dim trainInput = knn.trainInput
        Dim neighbors = knn.neighbors
        For i = 0 To queries.Count - 1
            Dim nabList = neighbors(i)
            Dim pt = queries(i)
            For j = 0 To Math.Min(nabList.Count, options.ptCount)
                Dim ptNew = trainInput(nabList(j))
                DrawLine(dst, pt, ptNew, white, task.lineWidth)
                If ptNew.X < options.border Then DrawLine(dst, New cv.Point2f(0, ptNew.Y), ptNew, white, task.lineWidth)
                If ptNew.Y < options.border Then DrawLine(dst, New cv.Point2f(ptNew.X, 0), ptNew, white, task.lineWidth)
                If ptNew.X > dst.Width - options.border Then DrawLine(dst, New cv.Point2f(dst.Width, ptNew.Y), ptNew, white, task.lineWidth)
                If ptNew.Y > dst.Height - options.border Then DrawLine(dst, New cv.Point2f(ptNew.X, dst.Height), ptNew, white, task.lineWidth)
            Next
        Next
        Return dst
    End Function
    Public Overrides Sub runAlg(src As cv.Mat)
        options.RunOpt()

        getFeatures(src)
        dst3 = task.feat.dst2

        If task.optionsChanged Then cornerHistory.Clear()

        Dim histCount = task.frameHistoryCount
        cornerHistory.Add(New List(Of cv.Point2f)(task.features))

        Dim lastIndex = cornerHistory.Count - 1
        knn.trainInput = New List(Of cv.Point2f)(cornerHistory.ElementAt(0))
        knn.queries = New List(Of cv.Point2f)(cornerHistory.ElementAt(lastIndex))
        knn.Run(src)

        dst2.SetTo(0)
        lpList.Clear()
        Dim disList As New List(Of Single)
        Dim dirList As New List(Of Single) ' angle in radians
        For i = 0 To knn.queries.Count - 1
            Dim nabList = knn.neighbors(i)
            Dim trainIndex = nabList(0) ' index of the matched train input
            Dim pt = knn.queries(i)
            Dim ptNew = knn.trainInput(trainIndex)
            Dim nextDist = pt.DistanceTo(ptNew)
            DrawLine(dst2, pt, ptNew, white)
            disList.Add(nextDist)
            lpList.Add(New linePoints(pt, ptNew))
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
            labels(2) = "Avg distance = " + Format(distanceAvg, fmt1) + vbCrLf + "Max Distance = " + Format(distanceMax, fmt1) + " (all units in pixels) "
        End If
        If dirList.Count > 0 Then
            directionAvg = dirList.Average
            labels(3) += " " + Format(directionAvg, fmt1) + " average direction (radians)"
        End If
        If cornerHistory.Count >= histCount Then cornerHistory.RemoveAt(0)
    End Sub
End Class






Public Class Swarm_RightFeatures : Inherits TaskParent
    Public rightList As New List(Of cv.Point2f)
    Public Sub New()
        labels = {"", "", "Left view feature points", "Right view feature points"}
        desc = "Double the votes on motion by collecting features for both left and right images."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        getFeatures(task.rightView)
        rightList = New List(Of cv.Point2f)(task.features)
        dst2 = task.feat.dst2.Clone
    End Sub
End Class





Public Class Swarm_LeftFeatures : Inherits TaskParent
    Public leftList As New List(Of cv.Point2f)
    Public Sub New()
        labels = {"", "", "Left view feature points", "Right view feature points"}
        desc = "Double the votes on motion by collecting features for both left and right images."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        getFeatures(task.leftView)
        leftList = New List(Of cv.Point2f)(task.features)
        dst2 = task.feat.dst2.Clone
    End Sub
End Class






Public Class Swarm_LeftRightFeatures : Inherits TaskParent
    Dim swLeft As New Swarm_LeftFeatures
    Dim swRight As New Swarm_RightFeatures
    Public Sub New()
        labels = {"", "", "Left view feature points", "Right view feature points"}
        desc = "Double the votes on motion by collecting features for both left and right images."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        swLeft.Run(src)
        dst2 = swLeft.dst2
        swRight.Run(src)
        dst3 = swRight.dst2
    End Sub
End Class







Public Class Swarm_LeftRight : Inherits TaskParent
    Public leftDistance As Single
    Public leftDirection As Single
    Public leftMax As Single
    Public rightDistance As Single
    Public rightDirection As Single
    Public rightMax As Single
    Dim swarm As New Swarm_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        labels = {"", "", "Left view feature points", "Right view feature points"}
        desc = "Get direction and distance from the left and right images."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        swarm.Run(task.leftView)
        leftDistance = swarm.distanceAvg
        leftDirection = swarm.directionAvg
        leftMax = swarm.distanceMax
        dst2 = task.leftView
        dst2.SetTo(cv.Scalar.White, swarm.DrawLines())

        swarm.Run(task.rightView)
        rightDistance = swarm.distanceAvg
        rightDirection = swarm.directionAvg
        rightMax = swarm.distanceMax
        dst3 = task.rightView
        dst3.SetTo(cv.Scalar.White, swarm.DrawLines())

        strOut = swarm.labels(2) + vbCrLf + swarm.labels(3)
        SetTrueText(strOut, 1)
    End Sub
End Class








Public Class Swarm_Percentage : Inherits TaskParent
    Dim swarm As New Swarm_Flood
    Dim options As New Options_SwarmPercent
    Public Sub New()
        desc = "Use features to segment a percentage of the image then use RedCloud with a mask for the rest of the image."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        options.RunOpt()

        swarm.Run(src)
        dst2 = swarm.dst2

        dst3.SetTo(0)
        Dim pixels As Integer
        Dim count As Integer
        For Each rc In task.redCells
            dst3(rc.rect).SetTo(If(task.redOptions.NaturalColor.Checked, rc.naturalColor, rc.color), rc.mask)
            pixels += rc.pixels
            count += 1
            If pixels / src.Total >= options.percent Then Exit For
        Next
        labels(3) = "The top " + CStr(count) + " cells by size = " + Format(options.percent, "0%") + " of the pixels"
    End Sub
End Class







Public Class Swarm_Flood : Inherits TaskParent
    Dim swarm As New Swarm_Basics
    Public flood As New Flood_BasicsMask
    Dim color8U As New Color8U_Basics
    Public Sub New()
        desc = "Floodfill the color image using the swarm outline as a mask"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        swarm.Run(src)

        color8U.Run(src)

        flood.cellGen.removeContour = False
        flood.inputMask = swarm.dst2
        flood.Run(color8U.dst2)
        dst2 = flood.dst2

        task.setSelectedCell()
        labels(2) = flood.cellGen.labels(2)
    End Sub
End Class







Public Class Swarm_Flood2 : Inherits TaskParent
    Public lines As New Line_KNN
    Public flood As New Flood_BasicsMask
    Dim color8U As New Color8U_Basics
    Public Sub New()
        flood.cellGen.removeContour = False
        desc = "Floodfill the color image using the swarm outline as a mask"
    End Sub
    Public Function runRedCloud(src As cv.Mat) As cv.Mat
        lines.Run(src)
        color8U.Run(src)

        flood.inputMask = lines.dst3
        flood.Run(color8U.dst2)
        Return flood.dst2
    End Function
    Public Overrides sub runAlg(src As cv.Mat)
        If task.heartBeat = False Then Exit Sub

        dst2 = runRedCloud(src).Clone()
        dst3 = lines.dst3.Clone

        task.setSelectedCell()
        labels(1) = "Color8U_Basics input = " + task.redOptions.ColorSource.Text
        labels(2) = flood.cellGen.labels(2)
        labels(3) = lines.labels(2)
    End Sub
End Class







Public Class Swarm_Flood3 : Inherits TaskParent
    Dim swarm As New Swarm_Flood2
    Public Sub New()
        desc = "Create RedCloud cells every heartbeat and compare the results against RedCloud cells created with the current frame."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        swarm.Run(src)
        dst2 = swarm.dst2
        labels(2) = swarm.labels(2)

        dst3 = swarm.runRedCloud(src)
        labels(3) = swarm.labels(2)
    End Sub
End Class
