Imports cv = OpenCvSharp
Public Class RedCC_Basics : Inherits TaskParent
    Public color8u As New Color8U_Basics
    Public Sub New()
        desc = "Map the colors in the point cloud."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedCloud(src, labels(2))

        color8u.Run(task.gray)
        dst3 = color8u.dst3
        labels(3) = color8u.labels(2)

        If standaloneTest() Then
            For Each rc In task.redCloud.rcList
                dst2.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
                SetTrueText(CStr(rc.age), rc.maxDist)
            Next
        End If
    End Sub
End Class





Public Class RedCC_Histograms : Inherits TaskParent
    Dim hist As New Hist_Basics
    Dim redCC As New RedCC_Basics
    Public Sub New()
        desc = "Add Color8U id's to each RedCloud cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redCC.Run(src)
        dst2 = redCC.dst2
        dst1 = redCC.color8u.dst2
        dst3 = redCC.color8u.dst3
        labels = redCC.labels

        hist.Run(dst1)
        Dim actualClasses As Integer
        For i = 0 To hist.histArray.Count - 1
            If hist.histArray(i) Then actualClasses += 1
        Next
        If task.gOptions.HistBinBar.Maximum >= actualClasses + 1 Then
            task.gOptions.HistBinBar.Value = actualClasses + 1
        End If

        For Each rc In task.redCloud.rcList
            Dim input = dst1(rc.rect).Clone
            input.SetTo(0, Not rc.contourMask)
            hist.Run(input)
            rc.colorIDs = New List(Of Integer)
            For i = 1 To hist.histArray.Count - 1 ' ignore zeros
                If hist.histArray(i) Then rc.colorIDs.Add(i)
            Next
            dst2.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
            strOut = ""
            For Each index In rc.colorIDs
                strOut += CStr(index) + ","
            Next
            SetTrueText(strOut, rc.maxDist, 2)
            SetTrueText(strOut, rc.maxDist, 3)
        Next
    End Sub
End Class





Public Class RedCC_Merge : Inherits TaskParent
    Public redSweep As New RedCloud_Sweep
    Public color8u As New Color8U_Basics
    Public rcList As New List(Of rcData)
    Public Sub New()
        desc = "Merge the color and reduced depth data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redSweep.Run(src)
        dst1 = redSweep.dst3
        dst1.SetTo(0, redSweep.prepEdges.dst2)

        color8u.Run(task.gray)
        dst3 = color8u.dst2 + dst1

        dst2 = PaletteBlackZero(dst3)

        rcList = RedCloud_Sweep.sweepImage(dst3)

        'strOut = RedCell_Basics.selectCell(rcMap, rcList)
        'If task.rcD IsNot Nothing Then task.color(task.rcD.rect).SetTo(white, task.rcD.contourMask)
        'SetTrueText(strOut, 3)
    End Sub
End Class

