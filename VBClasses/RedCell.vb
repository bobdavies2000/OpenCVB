Imports cv = OpenCvSharp
Public Class RedCell_Basics : Inherits TaskParent
    Public Sub New()
        desc = "Display the output of a cell for RedCloud_Basics."
    End Sub
    Public Shared Function displayCellx() As cloudData
        Static clickIndex As Integer
        Static pcClick As cloudData
        clickIndex = task.redC.dst1.Get(Of Byte)(task.ClickPoint.Y, task.ClickPoint.X) - 1
        If clickIndex >= 0 Then pcClick = task.redC.pcList(clickIndex)
        If task.mouseClickFlag Then

        End If
        Return pcClick
    End Function
    Public Shared Function displayCell() As cloudData
        Dim clickIndex = task.redC.dst1.Get(Of Byte)(task.ClickPoint.Y, task.ClickPoint.X) - 1
        If clickIndex >= 0 Then Return task.redC.pcList(clickIndex)
        Return Nothing
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then dst2 = runRedC(src, labels(2))
        Dim pcClick = displayCell()
        If pcClick IsNot Nothing Then SetTrueText(pcClick.displayString, 3)
    End Sub
End Class
