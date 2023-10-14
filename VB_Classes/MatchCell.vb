Imports System.Security.Cryptography
Imports cv = OpenCvSharp
Public Class MatchCell_Basics : Inherits VB_Algorithm
    Dim prep As New MatchCell_PrepareData
    Public redCells As New List(Of rcData)
    Public cellMap As cv.Mat
    Public Sub New()
        If standalone Then
            gOptions.displayDst0.Checked = True
            gOptions.displayDst1.Checked = True
        End If
        labels = {"RedCloud output", "Floodfill output ", "Merged RedCloud cells based on BGR cells", ""}
        desc = "Survey the BGR contour pixels to find the RedCloud cells that can be merged"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        prep.Run(src)
        dst0 = prep.dst2
        dst1 = prep.dst3

        Dim usedRedCells As New List(Of Integer)
        Dim rcLists(prep.rgbCells.Count - 1) As List(Of Integer)
        For Each rc In prep.rgbCells
            rcLists(rc.index) = New List(Of Integer)
            For Each pt In rc.contour
                Dim index = prep.cellMap.Get(Of Byte)(pt.Y, pt.X)
                If usedRedCells.Contains(index) = False Then
                    usedRedCells.Add(index)
                    rcLists(rc.index).Add(index)
                End If
            Next
        Next

        dst2.SetTo(0)
        cellMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        redCells = New List(Of rcData)
        For i = 0 To rcLists.Count - 1
            Dim rclist = rcLists(i)
            Dim rc = prep.rgbCells(i)
            For Each index In rclist
                Dim rcX = prep.redCells(index)
                vbDrawContour(dst2(rcX.rect), rcX.contour, rc.color, -1)

                rcX.color = rc.color
                rcX.index = i
                redCells.Add(rcX)
                vbDrawContour(cellMap(rc.rect), rcX.contour, i, -1)
            Next
        Next
    End Sub
End Class






Public Class MatchCell_PrepareData : Inherits VB_Algorithm
    Public flood As New Flood_RedColor
    Public redCells As New List(Of rcData)
    Public rgbCells As New List(Of rcData)
    Public cellMap As cv.Mat
    Public rgbcellMap As cv.Mat
    Dim redC As New RedCloud_Basics
    Public Sub New()
        redC.depthOnly = True
        cellMap = New cv.Mat(task.workingRes, cv.MatType.CV_8U, 0)
        rgbcellMap = New cv.Mat(task.workingRes, cv.MatType.CV_8U, 0)
        labels = {"", "", "RedCloud output for depth", "RedCloud output for BGR"}
        desc = "Prepare data for use with MatchCells algorithms."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        cellMap.CopyTo(task.cellMap)
        task.redCells = New List(Of rcData)(redCells)

        redC.Run(src)
        dst2 = redC.dst2

        redCells = New List(Of rcData)(task.redCells)
        cellMap = task.cellMap.Clone

        task.redCells = New List(Of rcData)(rgbCells)
        rgbcellMap.CopyTo(task.cellMap)

        flood.Run(src)
        dst3 = flood.dst2

        rgbCells = New List(Of rcData)(task.redCells)
        rgbcellMap = task.cellMap.Clone
    End Sub
End Class








Public Class MatchCell_Shape : Inherits VB_Algorithm
    Dim options As New Options_MatchShapes
    Dim lastHulls As New List(Of rcData)
    Dim hulls As New RedCloud_Hulls
    Public Sub New()
        labels(2) = "Highlighted cells matched the previous generation cell in the same area."
        desc = "Use OpenCV's MatchShape API to validate that stable and current contours agree"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst3 = src.Clone

        hulls.Run(src)
        dst2 = hulls.dst2

        Dim matchCount As Integer
        If lastHulls.Count > 0 Then
            For Each rc In task.redCells
                If rc.indexLast >= lastHulls.Count Then Continue For
                Dim rcPrev = lastHulls(rc.indexLast)
                If rc.hull Is Nothing Or rcPrev.hull Is Nothing Then Continue For

                Dim matchVal = cv.Cv2.MatchShapes(rc.hull, rcPrev.hull, options.matchOption)
                If matchVal >= options.matchThreshold Then
                    vbDrawContour(dst3(rcPrev.rect), rcPrev.hull, task.highlightColor)
                    matchCount += 1
                Else
                    vbDrawContour(dst2(rcPrev.rect), rcPrev.hull, task.highlightColor)
                End If
            Next
        End If
        lastHulls = New List(Of rcData)(task.redCells)

        labels(3) = CStr(matchCount) + " of " + CStr(task.redCells.Count) + " RedCloud cells below did NOT match the cell shape for the previous generation."
    End Sub
End Class