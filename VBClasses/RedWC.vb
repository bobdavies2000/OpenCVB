Imports System.Runtime.InteropServices
Imports System.Windows.Documents
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedWG_Basics : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Dim currSet As New List(Of cv.Point)
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Identify where RedCloud world coordinates are changing"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Dim lastSet As New List(Of cv.Point)(currSet)
            dst2.SetTo(0)
            Static count As Integer
            If task.heartBeatLT Or task.frameCount = 2 Then
                dst3.SetTo(0)
                count = 0
            End If
            currSet.Clear()
            For Each rc In redC.rcList
                currSet.Add(rc.wGrid)
                If lastSet.Contains(rc.wGrid) Then
                    dst2(rc.rect).SetTo(rc.color, rc.mask)
                Else
                    dst3(rc.rect).SetTo(rc.color, rc.mask)
                    count += 1
                End If
            Next

            strOut = RedCloud_Cell.selectCell(redC.rcMap, redC.rcList)
            SetTrueText(strOut, 1)

            labels(3) = CStr(count) + " cells were not matched using wc since the last heartbeat"
        End Sub
    End Class





    Public Class RedWG_ValidateRows : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Public Sub New()
            desc = "Validate how consistent the world grid entries are."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Dim ptY As New List(Of Integer)
            For Each rc In redC.rcList
                ptY.Add(rc.wGrid.Y)
            Next

            Static row As Integer = ptY.Min

            For Each rc In redC.rcList
                If rc.wGrid.Y = row Then dst2(rc.rect).SetTo(white, rc.mask)
            Next

            If task.heartBeat Then row += 1
            SetTrueText("World Grid Row " + CStr(row) + " highlighted", 3)
            If row > ptY.Max Then row = ptY.Min
        End Sub
    End Class





    Public Class RedWG_ValidateCols : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Public Sub New()
            desc = "Validate how consistent the world grid entries are."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Dim ptX As New List(Of Integer)
            For Each rc In redC.rcList
                ptX.Add(rc.wGrid.X)
            Next

            Static column As Integer = -10
            If column < ptX.Min Then column = ptX.Min

            For Each rc In redC.rcList
                If rc.wGrid.X = column Then dst2(rc.rect).SetTo(white, rc.mask)
            Next

            If task.heartBeat Then column += 1
            SetTrueText("World Grid Col " + CStr(column) + " highlighted", 3)
            If column > ptX.Max Then column = ptX.Min
        End Sub
    End Class

End Namespace