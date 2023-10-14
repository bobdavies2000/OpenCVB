Imports  cv = OpenCvSharp
Public Class Highlight_Basics1 : Inherits VBparent
    Public highlightPoint As New cv.Point
    Dim highlightRect As New cv.Rect
    Dim preKalmanRect As New cv.Rect
    Dim highlightMask As New cv.Mat
    Public viewObjects As New SortedList(Of Single, viewObject)(New compareAllowIdenticalSingleInverted)
    Public Sub New()
        labels(2) = "Click near any dot to highlight object"
        highlightPoint = New cv.Point(dst2.Width / 2, dst3.Height / 2)
        task.desc = "Pixels are grouped by reduction.  Highlight the rectangle and centroid nearest the mouse click"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            task.trueText("In standalone mode, this algorithm does nothing.  See Reduction_KNN_Color to see how to use it.", 10, 40, 3)
        End If

        dst2 = src
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

            dst2.Circle(highlightPoint, task.dotSize + 1, cv.Scalar.Red, -1, task.lineType)
            dst2.Rectangle(highlightRect, cv.Scalar.Red, 2)
            Dim rect = New cv.Rect(0, 0, highlightMask.Width, highlightMask.Height)
            task.color.CopyTo(dst3)
            dst3(preKalmanRect).SetTo(cv.Scalar.Yellow, highlightMask)
            labels(3) = "Highlighting the selected region."
        End If
    End Sub
End Class