Imports cv = OpenCvSharp
Public Class Highlight_Basics
    Inherits VBparent
    Dim reduction As Reduction_KNN_Color
    Public highlightPoint As New cv.Point
    Dim highlightRect As New cv.Rect
    Dim preKalmanRect As New cv.Rect
    Dim highlightMask As New cv.Mat
    Public viewObjects As New SortedList(Of Single, viewObject)(New compareAllowIdenticalSingleInverted)
    Public Sub New()
        initParent()
        If standalone Then reduction = New Reduction_KNN_Color()
        task.desc = "Pixels are grouped by reduction.  Highlight the rectangle and centroid nearest the mouse click"
        ' task.rank = 1
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        If standalone or task.intermediateReview = caller Then
            reduction.src = src
            reduction.Run()
            viewObjects = reduction.pTrack.drawRC.viewObjects
            src = reduction.dst1
        End If

        dst1 = src
        If task.mouseClickFlag Then
            highlightPoint = task.mouseClickPoint
            task.mouseClickFlag = False ' absorb the mouse click here only
        End If
        If highlightPoint <> New cv.Point And viewObjects.Count > 0 Then
            Dim index = findNearestPoint(highlightPoint, viewObjects)
            highlightPoint = viewObjects.ElementAt(index).Value.centroid
            highlightRect = viewObjects.ElementAt(index).Value.rectInHist
            highlightMask = New cv.Mat
            highlightMask = viewObjects.ElementAt(index).Value.mask
            preKalmanRect = viewObjects.ElementAt(index).Value.preKalmanRect

            dst1.Circle(highlightPoint, 5, cv.Scalar.Red, -1, task.lineType)
            dst1.Rectangle(highlightRect, cv.Scalar.Red, 2)
            Dim rect = New cv.Rect(0, 0, highlightMask.Width, highlightMask.Height)
            task.color.CopyTo(dst2)
            dst2(preKalmanRect).SetTo(cv.Scalar.Yellow, highlightMask)
            label2 = "Highlighting the selected region."
        End If
    End Sub
End Class


