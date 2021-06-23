Imports cv = OpenCvSharp
Public Class Highlight_Basics : Inherits VBparent
    Public highlightPoint As New cv.Point
    Dim highlightRect As New cv.Rect
    Dim preKalmanRect As New cv.Rect
    Dim highlightMask As New cv.Mat
    Public viewObjects As New SortedList(Of Single, viewObject)(New compareAllowIdenticalSingleInverted)
    Public Sub New()
        label1 = "Click near any dot to highlight object"
        highlightPoint = New cv.Point(dst2.Width / 2, dst3.Height / 2)
        task.desc = "Pixels are grouped by reduction.  Highlight the rectangle and centroid nearest the mouse click"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        'If standalone Or task.intermediateName = caller Then
        '    Static reduction As New Reduction_KNN_Color
        '    reduction.Run(src)
        '    viewObjects = reduction.pTrack.drawRC.viewObjects
        '    src = reduction.dst2
        '    cv.Cv2.ImShow("src", src)
        'End If
        If standalone Or task.intermediateName = caller Then
            task.trueText("In standalone mode, this algorithm does nothing.  See Reduction_KNN_Color to see how to use it.")
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

            dst2.Circle(highlightPoint, task.dotSize + 4, cv.Scalar.Red, -1, task.lineType)
            dst2.Rectangle(highlightRect, cv.Scalar.Red, 2)
            Dim rect = New cv.Rect(0, 0, highlightMask.Width, highlightMask.Height)
            task.color.CopyTo(dst3)
            dst3(preKalmanRect).SetTo(cv.Scalar.Yellow, highlightMask)
            label2 = "Highlighting the selected region."
        End If
    End Sub
End Class


