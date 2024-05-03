Imports cv = OpenCvSharp
Public Class Swarm_Basics : Inherits VB_Algorithm
    Public knn As New KNN_Core
    Dim feat As New Feature_Basics
    Public mpList As New List(Of pointPair)
    Public distanceAvg As Single
    Public directionAvg As Single
    Public distanceMax As Single
    Public options As New Options_Swarm
    Public Sub New()
        findSlider("Feature Sample Size").Value = 1000
        findSlider("Blocksize").Value = 1
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Track the GoodFeatures across a frame history and connect the first and last good.corners in the history."
    End Sub
    Public Sub drawLines(dst As cv.Mat)
        Dim queries = knn.queries
        Dim trainInput = knn.trainInput
        Dim neighbors = knn.neighbors
        For i = 0 To queries.Count - 1
            Dim nabList = neighbors(i)
            Dim pt = queries(i)
            For j = 0 To Math.Min(nabList.Count, options.ptCount)
                Dim ptNew = trainInput(nabList(j))
                dst.Line(pt, ptNew, cv.Scalar.White, task.lineWidth, task.lineType)
                If ptNew.X < options.border Then dst.Line(New cv.Point2f(0, ptNew.Y), ptNew, cv.Scalar.White, task.lineWidth, task.lineType)
                If ptNew.Y < options.border Then dst.Line(New cv.Point2f(ptNew.X, 0), ptNew, cv.Scalar.White, task.lineWidth, task.lineType)
                If ptNew.X > dst.Width - options.border Then dst.Line(New cv.Point2f(dst.Width, ptNew.Y), ptNew, cv.Scalar.White, task.lineWidth, task.lineType)
                If ptNew.Y > dst.Height - options.border Then dst.Line(New cv.Point2f(ptNew.X, dst.Height), ptNew, cv.Scalar.White, task.lineWidth, task.lineType)
            Next
        Next
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        feat.Run(src)
        dst3 = feat.dst2

        Static cornerHistory As New List(Of List(Of cv.Point2f))
        If task.optionsChanged Then cornerHistory.Clear()

        Dim histCount = task.frameHistoryCount
        cornerHistory.Add(New List(Of cv.Point2f)(task.features))

        Dim lastIndex = cornerHistory.Count - 1
        knn.trainInput = New List(Of cv.Point2f)(cornerHistory.ElementAt(0))
        knn.queries = New List(Of cv.Point2f)(cornerHistory.ElementAt(lastIndex))
        knn.Run(empty)

        dst2.SetTo(0)
        mpList.Clear()
        Dim disList As New List(Of Single)
        Dim dirList As New List(Of Single) ' angle in radians
        For i = 0 To knn.queries.Count - 1
            Dim nabList = knn.neighbors(i)
            Dim trainIndex = nabList(0) ' index of the matched train input
            Dim pt = knn.queries(i)
            Dim ptNew = knn.trainInput(trainIndex)
            Dim nextDist = pt.DistanceTo(ptNew)
            dst2.Line(pt, ptNew, cv.Scalar.White, task.lineWidth, task.lineType)
            disList.Add(nextDist)
            mpList.Add(New pointPair(pt, ptNew))
            If nextDist > 0 Then
                If pt.Y <> ptNew.Y Then
                    Dim nextDirection = Math.Atan((pt.X - ptNew.X) / (pt.Y - ptNew.Y))
                    dirList.Add(nextDirection)
                End If
            End If
        Next
        drawLines(dst2)

        labels(3) = CStr(mpList.Count) + " points were matched to the previous set of features."
        distanceAvg = 0
        If task.heartBeat Then distanceMax = 0
        If disList.Count > 10 Then
            distanceAvg = disList.Average
            distanceMax = Math.Max(distanceMax, disList.Max)
            labels(2) = "Avg distance = " + Format(distanceAvg, fmt1) + vbCrLf + "Max Distance = " + Format(distanceMax, fmt1) + " (all units in pixels) "
        End If
        If dirList.Count > 0 Then
            directionAvg = dirList.Average
            labels(3) = Format(directionAvg, fmt1) + " average direction (radians)"
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
        leftList = New List(Of cv.Point2f)(task.features)
        dst2 = feat.dst2.Clone

        feat.Run(task.rightView)
        rightList = New List(Of cv.Point2f)(task.features)
        dst3 = feat.dst2.Clone
    End Sub
End Class






Public Class Swarm_LeftRight : Inherits VB_Algorithm
    Public leftDistance As Single
    Public leftDirection As Single
    Public leftMax As Single
    Public rightDistance As Single
    Public rightDirection As Single
    Public rightMax As Single
    Dim swarm As New Swarm_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"", "", "Left view feature points", "Right view feature points"}
        desc = "Get direction and distance from the left and right images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        swarm.Run(task.leftView)
        leftDistance = swarm.distanceAvg
        leftDirection = swarm.directionAvg
        leftMax = swarm.distanceMax
        dst2 = task.leftView
        swarm.drawLines(dst2)

        swarm.Run(task.rightView)
        rightDistance = swarm.distanceAvg
        rightDirection = swarm.directionAvg
        rightMax = swarm.distanceMax
        dst3 = task.rightView
        swarm.drawLines(dst3)

        strOut = swarm.labels(2) + vbCrLf + swarm.labels(3)
        setTrueText(strOut, 1)
    End Sub
End Class








Public Class Swarm_Percentage : Inherits VB_Algorithm
    Dim swarm As New Swarm_Flood
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Cells map X percent", 1, 100, 50)
        desc = "Use features to segment a percentage of the image then use RedCloud with a mask for the rest of the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static percentSlider = findSlider("Cells map X percent")
        Dim percent = percentSlider.value / 100

        swarm.Run(src)
        dst2 = swarm.dst2

        dst3.SetTo(0)
        Dim pixels As Integer
        For Each rc In task.redCells
            dst3(rc.rect).SetTo(rc.color, rc.mask)
            pixels += rc.pixels
            If pixels / src.Total >= percent Then Exit For
        Next
    End Sub
End Class







Public Class Swarm_Flood : Inherits VB_Algorithm
    Dim swarm As New Swarm_Basics
    Public flood As New Flood_BasicsMask
    Dim colorC As New Color_Basics
    Public Sub New()
        redOptions.IdentifyCells.Checked = True
        desc = "Floodfill the color image using the swarm outline as a mask"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        swarm.Run(src)

        colorC.Run(src)

        flood.genCells.removeContour = False
        flood.inputMask = swarm.dst2
        flood.Run(colorC.dst2)
        dst2 = flood.dst2

        setSelectedContour()
        labels(2) = flood.genCells.labels(2)
    End Sub
End Class







Public Class Swarm_Flood2 : Inherits VB_Algorithm
    Public lines As New Line_KNN
    Public flood As New Flood_BasicsMask
    Public colorC As New Color_Basics
    Public Sub New()
        redOptions.IdentifyCells.Checked = True
        flood.genCells.removeContour = False
        desc = "Floodfill the color image using the swarm outline as a mask"
    End Sub
    Public Function runRedCloud(src As cv.Mat) As cv.Mat
        lines.Run(src)
        colorC.Run(src)

        flood.inputMask = lines.dst3
        flood.Run(colorC.dst2)
        Return flood.dst2
    End Function
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat = False Then Exit Sub

        dst2 = runRedCloud(src).Clone()
        dst3 = lines.dst3.Clone

        setSelectedContour()
        labels(2) = flood.genCells.labels(2)
        labels(3) = lines.labels(2)
    End Sub
End Class







Public Class Swarm_Flood3 : Inherits VB_Algorithm
    Dim swarm As New Swarm_Flood2
    Public Sub New()
        desc = "Create RedCloud cells every heartbeat and compare the results against RedCloud cells created with the current frame."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        swarm.Run(src)
        dst2 = swarm.dst2
        labels(2) = swarm.labels(2)

        dst3 = swarm.runRedCloud(src)
        labels(3) = swarm.labels(2)
    End Sub
End Class
