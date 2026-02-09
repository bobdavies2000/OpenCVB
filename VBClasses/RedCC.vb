Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedCC_Basics : Inherits TaskParent
        Dim reduction As New Reduction_Basics
        Public rcList As List(Of rcData)
        Public rcMap As cv.Mat
        Public redC1 As New RedColor_Basics
        Dim redC2 As New RedCloud_Basics
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Insert the RedCloud cells into the RedColor_Basics input."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC2.Run(src)
            labels(2) = redC2.labels(2)
            dst2 = redC2.dst2

            reduction.Run(src)

            redC1.Run(reduction.dst2)
            labels(3) = redC1.labels(2)
            dst3 = redC1.dst2

            Static picTag As Integer = task.mousePicTag
            If task.mouseClickFlag Then picTag = task.mousePicTag
            If picTag = 2 Then
                RedCloud_Cell.selectCell(redC2.rcMap, redC2.rcList)
            Else
                RedCloud_Cell.selectCell(redC1.rcMap, redC1.rcList)
            End If

            If task.rcD IsNot Nothing Then strOut = task.rcD.displayCell()
            SetTrueText(strOut, 1)
        End Sub
    End Class





    Public Class RedCC_BasicsCombined : Inherits TaskParent
        Dim reduction As New Reduction_Basics
        Public rcList As List(Of rcData)
        Dim redC1 As New RedColor_Basics
        Dim redC2 As New RedCloud_Basics
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            labels(1) = "Contours of each RedCloud cell - if missing some, CV_8U is the problem."
            desc = "Insert the RedCloud cells into the RedColor_Basics input."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC2.Run(src)
            labels(3) = redC2.labels(2)

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

            redC1.Run(reduction.dst2)
            labels(2) = redC1.labels(2)
            dst2 = redC1.dst2

            rcList = New List(Of rcData)(redC1.rcList)
            RedCloud_Cell.selectCell(redC1.rcMap, rcList)
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
        Public redC As New RedCloud_Basics
        Public color8u As New Color8U_Basics
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Merge the color and reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst1 = redC.dst3
            'dst1.SetTo(0, redC.prepEdges.dst2)

            color8u.Run(task.gray)
            dst3 = color8u.dst3

            dst2 = PaletteFull(color8u.dst2 + dst1)

            RedCloud_Cell.selectCell(redC.rcMap, redC.rcList)
            If task.rcD IsNot Nothing Then strOut = task.rcD.displayCell()
            SetTrueText(strOut, 1)

            If task.rcD IsNot Nothing And task.rcD.rect.Width > 0 Then
                dst3(task.rcD.rect).SetTo(white, task.rcD.mask)
            End If
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
            RedCloud_Cell.selectCell(redCC.redC1.rcMap, redCC.redC1.rcList)
            labels(3) = "Select a RedCloud cell to see the histogram"

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