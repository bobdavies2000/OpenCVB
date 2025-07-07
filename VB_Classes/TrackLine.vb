Imports cv = OpenCvSharp
Public Class TrackLine_Basics : Inherits TaskParent
    Public lpInput As lpData
    Public foundLine As Boolean
    Dim match As New Match_Line
    Public Sub New()
        desc = "Track an individual line as best as possible."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lplist = task.lineRGB.lpList
        If lplist.Count = 0 Then Exit Sub
        If standalone And foundLine = False Then lpInput = task.lineRGB.lpList(0)

        ' 1) check if the line is still there in the rgb image.
        Dim index = task.lineRGB.lpMap.Get(Of Byte)(lpInput.center.Y, lpInput.center.X)
        If index > 0 Then
            Dim lp = lplist(index - 1)
            If lpInput.ID = lp.ID Then
                foundLine = True
            Else
                ' 2) check if endpoint nabeRects match the previous image.
                match.lpInput = lpInput
                match.Run(src)

                foundLine = match.correlation1 >= task.fCorrThreshold And match.correlation2 >= task.fCorrThreshold
                If foundLine Then
                    Dim p1 = New cv.Point(lpInput.p1.X + match.offsetX1, lpInput.p1.X + match.offsetY1)
                    Dim p2 = New cv.Point(lpInput.p2.X + match.offsetX2, lpInput.p2.Y + match.offsetY2)
                    lpInput = New lpData(p1, p2)
                Else
                    lpInput = lplist(0)
                End If
            End If
        Else
            lpInput = lplist(0)
        End If

        dst2 = src
        dst2.Line(lpInput.p1, lpInput.p2, task.highlight, task.lineWidth, task.lineType)
    End Sub
End Class

