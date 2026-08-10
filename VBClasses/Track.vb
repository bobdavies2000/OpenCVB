Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Track_Basics : Inherits TaskParent
        Dim redC As New RedC_Basics
        Public Sub New()
            desc = "Track the selected cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then
                Static color8u As New Color8U_Basics
                color8u.Run(src)
                src = color8u.dst2
            End If

            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Static rclast As rcData = task.rcD
            If rclast Is Nothing Then Exit Sub
            If rclast.mapID <> task.rcD.mapID And task.mouseClickFlag = False Then
                For Each rc In redC.rcList
                    If rc.mapID = rclast.mapID Then
                        If rc.rect.Contains(task.clickPoint) Then
                            task.rcD = rc
                            Exit For
                        End If
                    End If
                Next
            End If

            Dim clickIndex = redC.rcMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
            Circle(dst2, task.rcD.maxDist, task.DotSize + 2, task.highlight, -1)

            task.clickPoint = task.rcD.maxDist
            labels(3) = "Map ID = " + CStr(task.rcD.mapID)

            strOut = Utility_Basics.selectCell(redC.rcMap, redC.rcList)
            SetTrueText(strOut, 3)
        End Sub
    End Class




    Public Class Track_Simple : Inherits TaskParent
        Dim redC As New RedC_Basics
        Dim lostCell As Boolean
        Public Sub New()
            desc = "Track the selected cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then
                Static color8u As New Color8U_Basics
                color8u.Run(src)
                src = color8u.dst2
            End If

            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Static rclast As rcData = task.rcD
            If rclast IsNot Nothing Then

                If lostCell And task.mouseClickFlag = False Then
                    SetTrueText("Unable to find the cell" + vbCrLf + "Click any cell to start tracking again.", 3)
                    Exit Sub
                Else
                    lostCell = False
                End If

                Dim clickIndex = redC.rcMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
                If task.rcD IsNot Nothing Then
                    Circle(dst2, task.rcD.maxDist, task.DotSize + 2, task.highlight, -1)

                    task.clickPoint = task.rcD.maxDist
                    labels(3) = "Map ID = " + CStr(task.rcD.mapID)

                    If rclast.mapID <> task.rcD.mapID And task.mouseClickFlag = False Then
                        lostCell = True
                        Exit Sub
                    End If
                End If
                strOut = Utility_Basics.selectCell(redC.rcMap, redC.rcList)
                SetTrueText(strOut, 3)
            End If
            rclast = task.rcD
        End Sub
    End Class




    Public Class Track_FindNearest : Inherits TaskParent
        Dim redC As New RedC_Basics
        Dim knn As New KNN_Basics
        Public Sub New()
            desc = "Find the nearest cell with the same mapID."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then
                Static color8u As New Color8U_Basics
                color8u.Run(src)
                src = color8u.dst2
            End If

            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
            Static rclast As rcData = task.rcD
            If task.rcD Is Nothing Or rclast Is Nothing Then Exit Sub

            Dim clickIndex = redC.rcMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
            Circle(dst2, task.rcD.maxDist, task.DotSize + 2, task.highlight, -1)

            knn.trainInput.Clear()
            Dim indexList As New List(Of Integer)
            For Each rc In redC.rcList
                If rc.mapID = task.rcD.mapID Then
                    knn.trainInput.Add(New Point2f(rc.maxDist.X, rc.maxDist.Y))
                    indexList.Add(rc.index)
                End If
            Next

            If knn.trainInput.Count = 0 Then
                strOut = "Map ID " + CStr(rclast.mapID) + " was lost.  Click any cell to track it."
                SetTrueText(strOut, 3)
                Exit Sub
            End If

            knn.queries.Clear()
            knn.queries.Add(New Point2f(rclast.maxDist.X, rclast.maxDist.Y))
            knn.Run(task.emptyMat)

            For i = 0 To knn.result.Length - 1
                Dim rc = redC.rcList(indexList(knn.result(0, i)))
                Circle(dst2, rc.maxDist, task.DotSize + 1, task.highlight, -1)
                SetTrueText(CStr(knn.result(0, i)), rc.maxDist)
                SetTrueText(CStr(knn.result(0, i)), rc.maxDist, 3)
            Next
            task.clickPoint = task.rcD.maxDist
            labels(3) = "Map ID = " + CStr(task.rcD.mapID)
            SetTrueText(redC.strOut, 1)

            rclast = task.rcD
        End Sub
    End Class
End Namespace