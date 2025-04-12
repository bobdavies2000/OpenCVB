Imports cv = OpenCvSharp
Public Class GridPoint_Basics : Inherits TaskParent
    Dim sobel As New Edge_SobelQT
    Public features As New List(Of cv.Point2f)
    Public featurePoints As New List(Of cv.Point)
    Public matchedPoints As New List(Of cv.Point)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 255)
        desc = "Find the max Sobel point in each grid cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sobel.Run(task.gray)
        dst3 = sobel.dst2

        Dim bestPoints As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalIntegerInverted)

        For Each gc In task.gcList
            Dim mm = GetMinMax(dst3(gc.rect))
            Dim pt = New cv.Point(mm.maxLoc.X + gc.rect.X, mm.maxLoc.Y + gc.rect.Y)
            Dim val = dst3.Get(Of Byte)(mm.maxLoc.Y, mm.maxLoc.X)
            bestPoints.Add(val, pt)
        Next

        dst1.SetTo(255, task.motionMask)
        featurePoints.Clear()
        matchedPoints.Clear()
        For Each ele In bestPoints
            Dim pt = ele.Value
            If dst1.Get(Of Byte)(pt.Y, pt.X) Then
                Dim gc = task.gcList(task.gcMap.Get(Of Single)(pt.Y, pt.X))
                If gc.feature <> newPoint Then
                    matchedPoints.Add(gc.feature)
                    featurePoints.Add(gc.feature)
                Else
                    featurePoints.Add(pt)
                End If
            End If
        Next

        dst1.SetTo(0)
        features.Clear()
        For Each pt In featurePoints
            Dim gc = task.gcList(task.gcMap.Get(Of Single)(pt.Y, pt.X))
            gc.feature = pt
            dst1.Circle(pt, task.DotSize + 2, 255, -1)
            features.Add(New cv.Point2f(pt.X, pt.Y))
        Next

        dst2 = src.Clone
        For Each pt In matchedPoints
            dst2.Circle(pt, task.DotSize, task.highlight, -1)
        Next

        labels(2) = "Of the " + CStr(bestPoints.Count) + " candidates, " + CStr(features.Count) + " were saved and " +
                    CStr(matchedPoints.Count) + " were matched to a previous grid point"
    End Sub
End Class






Public Class GridPoint_SobelMax : Inherits TaskParent
    Dim sobel As New Edge_SobelQT
    Public features As New List(Of cv.Point2f)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 255)
        desc = "Find the maximum Sobel entry in each GridCell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sobel.Run(task.gray)
        dst3 = sobel.dst2

        Dim bestPoints As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalIntegerInverted)

        For Each gc In task.gcList
            Dim mm = GetMinMax(dst3(gc.rect))
            Dim val = dst3(gc.rect).Get(Of Byte)(mm.maxLoc.Y, mm.maxLoc.X)
            gc.prevFeature = New cv.Point2f(mm.maxLoc.X + gc.rect.X, mm.maxLoc.Y + gc.rect.Y)
            If val = 255 Then bestPoints.Add(gc.index, gc.feature)
        Next

        features.Clear()
        dst1.SetTo(255, task.motionMask)
        For Each ele In bestPoints
            If dst1.Get(Of Byte)(ele.Value.Y, ele.Value.X) Then
                If task.toggleOn Then
                    Dim gc = task.gcList(ele.Key)
                    features.Add(gc.prevFeature)
                    gc.feature = gc.prevFeature
                    task.gcList(ele.Key) = gc
                Else
                    features.Add(ele.Value)
                End If
            End If
        Next

        dst2 = src.Clone
        dst1.SetTo(0)
        For Each pt In features
            dst2.Circle(pt, task.DotSize, task.highlight, -1)
            dst1.Circle(pt, task.DotSize + 2, 255, -1)
        Next
        If task.heartBeat Then labels(2) = CStr(features.Count) + " features were found with maximum Sobel difference."
    End Sub
End Class






Public Class GridPoint_TopSobel : Inherits TaskParent
    Dim sobel As New Edge_SobelQT
    Public features As New List(Of cv.Point2f)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 255)
        desc = "Find the maximum Sobel entry in each GridCell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.algorithmPrep = False Then Exit Sub ' a direct call from another algorithm is unnecessary - already been run...
        sobel.Run(task.gray)
        dst3 = sobel.dst2
        labels(3) = sobel.labels(2)

        Dim sortedPoints As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalIntegerInverted)

        For Each gc In task.gcList
            Dim mm = GetMinMax(dst3(gc.rect))
            Dim val = dst3(gc.rect).Get(Of Byte)(mm.maxLoc.Y, mm.maxLoc.X)
            sortedPoints.Add(val, New cv.Point2f(mm.maxLoc.X + gc.rect.X, mm.maxLoc.Y + gc.rect.Y))
        Next

        Dim nextList As New List(Of cv.Point)
        For Each ele In sortedPoints
            If ele.Key < 200 Then Exit For
            nextList.Add(ele.Value)
        Next

        features.Clear()
        dst1.SetTo(255, task.motionMask)
        For Each pt In nextList
            If dst1.Get(Of Byte)(pt.Y, pt.X) Then features.Add(pt)
        Next

        dst2 = src.Clone
        dst1.SetTo(0)
        For Each pt In features
            dst2.Circle(pt, task.DotSize, task.highlight, -1)
            dst1.Circle(pt, task.DotSize, 255, -1)
        Next
        If task.heartBeat Then labels(2) = CStr(features.Count) + " features were found with maximum Sobel difference."
        If task.toggleOn Then labels(3) = "ToggleOn"
    End Sub
End Class