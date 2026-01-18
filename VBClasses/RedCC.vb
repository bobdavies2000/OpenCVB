Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedCC_Basics : Inherits TaskParent
        Dim reduction As New Reduction_Basics
        Public rcList As List(Of rcData)
        Public rcMap As cv.Mat
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Insert the RedCloud cells into the RedColor_Basics input."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedCloud(src, labels(2))
            reduction.Run(src)
            dst3 = runRedColor(reduction.dst2, labels(3))

            Static picTag As Integer = task.mousePicTag
            If task.mouseClickFlag Then picTag = task.mousePicTag
            If picTag = 2 Then
                RedCloud_Cell.selectCell(task.redCloud.rcMap, task.redCloud.rcList)
            Else
                RedCloud_Cell.selectCell(task.redColor.rcMap, task.redColor.rcList)
            End If

            If task.rcD IsNot Nothing Then strOut = task.rcD.displayCell()
            SetTrueText(strOut, 1)
        End Sub
    End Class





    Public Class NR_RedCC_BasicsOld : Inherits TaskParent
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






    Public Class NR_RedCC_Merge : Inherits TaskParent
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

            dst2 = PaletteFull(color8u.dst2 + dst1)

            RedCloud_Cell.selectCell(redSweep.rcMap, redSweep.rcList)
            If task.rcD IsNot Nothing Then strOut = task.rcD.displayCell()
            SetTrueText(strOut, 1)

            If task.rcD IsNot Nothing Then dst3(task.rcD.rect).SetTo(white, task.rcD.mask)
        End Sub
    End Class





    Public Class NR_RedCC_CellHistogram : Inherits TaskParent
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
                vbc.DrawLine(dst3, New cv.Point(x, 0), New cv.Point(x, dst2.Height), cv.Scalar.White)
            Next
            dst3 = plot.dst2
        End Sub
    End Class
End Namespace