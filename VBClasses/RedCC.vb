Imports cv = OpenCvSharp
Public Class RedCC_Basics : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Public rcList As List(Of rcData)
    Public rcMap As cv.Mat
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(1) = "Contours of each RedCloud cell - if missing some, CV_8U is the problem."
        desc = "Insert the RedCloud cells into the RedColor_Basics input."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        runRedCloud(src, labels(3))
        reduction.Run(src)

        Dim index = reduction.classCount + 1
        For Each rc In task.redCloud.rcList
            reduction.dst2(rc.rect).SetTo(index, rc.mask)
            Dim listOfPoints = New List(Of List(Of cv.Point))({rc.contour})
            cv.Cv2.DrawContours(reduction.dst2(rc.rect), listOfPoints, 0,
                                cv.Scalar.All(255), 1, cv.LineTypes.Link8)
            index += 1
            If index >= 254 Then Exit For
        Next

        dst1 = reduction.dst2.InRange(255, 255)

        dst2 = runRedColor(reduction.dst2, labels(2))
        rcList = New List(Of rcData)(task.redColor.rcList)
        rcMap = task.redColor.rcMap

        RedCloud_Cell.selectCell(rcMap, rcList)
        If task.rcD IsNot Nothing Then strOut = task.rcD.displayCell()
        SetTrueText(strOut, 3)

        dst2.Rectangle(task.motionRect, task.highlight, task.lineWidth)
    End Sub
End Class





Public Class RedCC_Color8U : Inherits TaskParent
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






Public Class RedCC_Merge : Inherits TaskParent
    Public redSweep As New RedCloud_Sweep
    Public color8u As New Color8U_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Merge the color and reduced depth data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redSweep.Run(src)
        dst1 = redSweep.dst3
        dst1.SetTo(0, redSweep.prepEdges.dst2)

        color8u.Run(task.gray)
        dst3 = color8u.dst3

        dst2 = PaletteBlackZero(color8u.dst2 + dst1)

        RedCloud_Cell.selectCell(redSweep.rcMap, redSweep.rcList)
        If task.rcD IsNot Nothing Then strOut = task.rcD.displayCell()
        SetTrueText(strOut, 1)

        If task.rcD IsNot Nothing Then dst3(task.rcD.rect).SetTo(white, task.rcD.mask)
    End Sub
End Class





Public Class RedCC_Histograms : Inherits TaskParent
    Dim hist As New Hist_Basics
    Public redCC As New RedCC_Color8U
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
        For i = 1 To hist.histArray.Count - 1
            If hist.histArray(i) Then actualClasses += 1
        Next
        If task.gOptions.HistBinBar.Maximum >= actualClasses + 1 Then
            task.gOptions.HistBinBar.Value = actualClasses + 1
        End If

        For Each rc In task.redCloud.rcList
            Dim tmp = dst1(rc.rect)
            tmp.SetTo(0, Not rc.mask)

            Dim mm = GetMinMax(tmp)
            hist.Run(tmp)

            rc.colorIDs = New List(Of Integer)
            For i = 1 To hist.histArray.Count - 1 ' ignore zeros
                If hist.histArray(i) Then rc.colorIDs.Add(i)
            Next
            If standaloneTest() Then
                dst2.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
                strOut = ""
                For Each index In rc.colorIDs
                    strOut += CStr(index) + ","
                Next
                SetTrueText(strOut, rc.maxDist, 2)
                SetTrueText(strOut, rc.maxDist, 3)
            End If
        Next
        If task.rcD IsNot Nothing Then dst3.Rectangle(task.rcD.rect, white, task.lineWidth)
    End Sub
End Class




Public Class RedCC_UseHistIDs : Inherits TaskParent
    Dim histID As New RedCC_Histograms
    Public Sub New()
        desc = "Add the colors to the cell mask if they are in the use rc.colorIDs"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        histID.Run(src)
        dst2 = histID.dst2
        labels(2) = histID.labels(2)

        For Each rc In task.redCloud.rcList
            Dim colorMask As New cv.Mat(rc.rect.Size, cv.MatType.CV_8U, 0)
            For Each index In rc.colorIDs
                colorMask = colorMask Or histID.redCC.color8u.dst2(rc.rect).InRange(index, index)
            Next
            rc.mask = rc.mask Or colorMask
        Next

        RedCloud_Cell.selectCell(task.redCloud.rcMap, task.redCloud.rcList)
        If task.rcD IsNot Nothing Then
            strOut = task.rcD.displayCell
            dst3.SetTo(0)
            dst3(task.rcD.rect).SetTo(white, task.rcD.mask)
            task.color(task.rcD.rect).SetTo(white, task.rcD.mask)
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class





Public Class RedCC_CellHistogram : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Dim redCC As New RedCC_Basics
    Public Sub New()
        task.gOptions.setHistogramBins(100)
        If standalone Then task.gOptions.displayDst1.Checked = True
        plot.createHistogram = True
        desc = "Display the histogram of a selected RedCloud cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redCC.Run(src)
        dst2 = redCC.dst2
        labels(2) = redCC.labels(2)

        RedCloud_Cell.selectCell(task.redCloud.rcMap, task.redCloud.rcList)
        If task.rcD Is Nothing Then
            RedCloud_Cell.selectCell(task.redColor.rcMap, task.redColor.rcList)
            If task.rcD Is Nothing Then
                labels(3) = "Select a RedCloud cell to see the histogram"
                Exit Sub
            End If
        End If

        SetTrueText(task.rcD.displayCell, 1)

        Dim depth As cv.Mat = task.pcSplit(2)(task.rcD.rect)
        depth.SetTo(0, task.noDepthMask(task.rcD.rect))
        plot.minRange = 0
        plot.maxRange = task.MaxZmeters
        plot.Run(depth)
        labels(3) = "0 meters to " + Format(task.MaxZmeters, fmt0) + " meters - vertical lines every meter"

        Dim incr = dst2.Width / task.MaxZmeters
        For i = 1 To CInt(task.MaxZmeters - 1)
            Dim x = incr * i
            DrawLine(dst3, New cv.Point(x, 0), New cv.Point(x, dst2.Height), cv.Scalar.White)
        Next
        dst3 = plot.dst2
    End Sub
End Class