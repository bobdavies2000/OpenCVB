﻿Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
' https://docs.opencv.org/3.4/dc/df6/tutorial_py_histogram_backprojection.html
Public Class BackProject2D_Basics : Inherits VB_Algorithm
    Public hist2d As New Histogram2D_Basics
    Public histogram As New cv.Mat
    Public xRange As Integer = 255
    Public yRange As Integer = 255
    Public minX As Single, maxX As Single, minY As Single, maxY As Single
    Public options As New Options_ColorFormat
    Public bpCol As Integer, bpRow As Integer, brickW As Integer, brickH As Integer
    Public Sub New()
        If standalone Then gOptions.GridSize.Value = 5
        If standalone Then hist2d.histRowsCols = {gOptions.GridSize.Value, gOptions.GridSize.Value}
        desc = "A 2D histogram is built from 2 channels of any 3-channel input and the results are displayed and explored."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim dimension = gOptions.GridSize.Value
        brickW = task.gridCols
        brickH = task.gridRows
        bpCol = Math.Floor(task.mouseMovePoint.X / brickW)
        bpRow = Math.Floor(task.mouseMovePoint.Y / brickH)

        options.RunVB()
        src = options.dst2

        hist2d.Run(src)
        histogram = hist2d.histogram
        dst2 = hist2d.histogram

        minX = bpRow * xRange / dimension
        maxX = (bpRow + 1) * xRange / dimension
        minY = bpCol * yRange / dimension
        maxY = (bpCol + 1) * yRange / dimension

        Dim ranges() = New cv.Rangef() {New cv.Rangef(minX, maxX), New cv.Rangef(minY, maxY)}
        cv.Cv2.CalcBackProject({src}, redOptions.channels, histogram, dst0, ranges)
        Dim bpCount = histogram.Get(Of Single)(bpRow, bpCol)

        dst3.SetTo(0)
        dst3.SetTo(cv.Scalar.Yellow, dst0)
        If heartBeat() Then
            labels(2) = options.colorFormat + " format image: Selected cell has minX/maxX " + Format(minX, "0") + "/" + Format(maxX, "0") + " minY/maxY " + Format(minY, "0") + "/" +
                        Format(maxY, "0") + " - brighter means higher population"
            labels(3) = "That combination of channel 0/1 has " + CStr(bpCount) + " pixels while image total is " + Format(dst0.Total, "0")
        End If
        setTrueText("Use Global Algorithm Option 'Grid Square Size' to control the 2D histogram at left",
                    New cv.Point(10, dst3.Height - 20), 3)
    End Sub
End Class












Public Class BackProject2D_Compare : Inherits VB_Algorithm
    Dim hueSat As New PhotoShop_Hue
    Dim backP As New BackProject2D_Basics
    Dim mats As New Mat_4Click
    Public Sub New()
        labels(2) = "Hue (upper left), sat (upper right), highlighted backprojection (bottom left)"
        If standalone Then gOptions.GridSize.Value = 10
        desc = "Compare the hue and brightness images and the results of the histogram_backprojection2d"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hueSat.Run(src.Clone)
        mats.mat(0) = hueSat.dst2
        mats.mat(1) = hueSat.dst3

        backP.Run(src)
        mats.mat(2) = backP.dst3

        If firstPass Then task.quadrantIndex = 3
        mats.Run(Nothing)
        dst2 = mats.dst2
        dst3 = mats.dst3

        labels(3) = backP.labels(3)

        setTrueText("Use Global Algorithm Option 'Grid Square Size' to control this 2D histogram." + vbCrLf +
                    "Move mouse in 2D histogram to select a cell to backproject." + vbCrLf +
                    "Click any quadrant at left to display that quadrant here." + vbCrLf,
                    New cv.Point(10, dst3.Height - dst3.Height / 4), 3)
    End Sub
End Class








Public Class BackProject2D_RowCol : Inherits VB_Algorithm
    Dim backp As New BackProject2D_Basics
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("BackProject Row")
            radio.addRadio("BackProject Col")
            radio.check(0).Checked = True
        End If

        findRadio("HSV").Checked = True
        If standalone Then gOptions.displayDst0.Checked = True
        gOptions.GridSize.Value = 10
        desc = "Backproject the whole row or column of the 2D histogram"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst0 = src.Clone

        Static rowRadio = findRadio("BackProject Row")
        If task.mouseClickFlag Then rowRadio.checked = Not rowRadio.checked
        Dim selection = If(rowRadio.checked, "Col", "Row")
        labels = {"", "", "Histogram 2D with Backprojection by " + selection, ""}

        backp.Run(src)
        dst2 = backp.dst2

        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, 255), New cv.Rangef(backp.minY, backp.maxY)}
        If rowRadio.checked Then ranges = New cv.Rangef() {New cv.Rangef(backp.minX, backp.maxX), New cv.Rangef(0, 255)}

        cv.Cv2.CalcBackProject({src}, redOptions.channels, backp.histogram, dst1, ranges)

        dst3.SetTo(0)
        dst3.SetTo(cv.Scalar.Yellow, dst1)
        dst0.SetTo(0, dst1)

        With backp
            If standalone Then
                If Not rowRadio.checked Then dst2.Rectangle(New cv.Rect(.bpCol * .brickW, 0, .brickW, dst2.Height), cv.Scalar.Yellow, task.lineWidth, task.lineType)
                If rowRadio.checked Then dst2.Rectangle(New cv.Rect(0, .bpRow * .brickH, dst2.Width, .brickH), cv.Scalar.Yellow, task.lineWidth, task.lineType)
            End If
            Dim count = If(rowRadio.checked, backp.histogram.Row(.bpRow).Sum, backp.histogram.Col(.bpCol).Sum)
            labels(3) = selection + " = " + CStr(If(rowRadio.checked, .bpRow, .bpCol)) + "   " + CStr(dst1.CountNonZero) + " pixels with " + selection + " total = " + CStr(count)
        End With
        setTrueText("Use Global Algorithm Option 'Grid Square Size' to control this 2D histogram." + vbCrLf +
                    "Move mouse in 2D histogram to select a row or column to backproject." + vbCrLf +
                    "An algorithm option 'BackProject Row' or 'BackProject Col' controls the row/col selection." + vbCrLf +
                    "Click anywhere to switch from backprojecting a row to backprojecting a column." + vbCrLf,
                    New cv.Point(10, dst0.Height - dst0.Height / 4), 2)
    End Sub
End Class








Public Class BackProject2D_FullImage : Inherits VB_Algorithm
    Public masks As New List(Of cv.Mat)
    Dim backp As New BackProject2D_Basics
    Public classCount As Integer
    Public Sub New()
        findRadio("BGR").Checked = True
        gOptions.GridSize.Value = 3
        dst0 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Create masks for each of the non-zero histogram entries and build a full-size image with all the masks."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        backp.Run(src)
        dst2 = backp.dst2

        With backp
            If standalone Then dst2.Rectangle(New cv.Rect(.bpCol * .brickW, .bpRow * .brickH, .brickW - 1, .brickH - 1),
                                              cv.Scalar.Yellow, task.lineWidth, task.lineType)
        End With

        labels = backp.labels

        dst0.SetTo(1)
        masks.Clear()
        classCount = 1
        For row = 0 To gOptions.GridSize.Value - 1
            For col = 0 To gOptions.GridSize.Value - 1
                Dim count = backp.histogram.Get(Of Single)(row, col)
                If count > 100 Then
                    Dim ranges() = New cv.Rangef() {New cv.Rangef(row * 255 / gOptions.GridSize.Value, (row + 1) * 255 / gOptions.GridSize.Value),
                                                    New cv.Rangef(col * 255 / gOptions.GridSize.Value, (col + 1) * 255 / gOptions.GridSize.Value)}
                    cv.Cv2.CalcBackProject({backp.options.dst2}, redOptions.channels,
                                           backp.histogram, dst1, ranges)
                    If dst1.CountNonZero > 100 Then
                        classCount += 1
                        dst0.SetTo(classCount, dst1)
                        masks.Add(dst1.Clone)
                    End If
                End If
            Next
        Next

        dst3 = vbPalette(dst0 * 255 / classCount)
    End Sub
End Class









Public Class BackProject2D_Top : Inherits VB_Algorithm
    Dim heat As New HeatMap_Basics
    Public Sub New()
        findCheckBox("Show Frustrum").Checked = False
        labels = {"", "", "Top Down HeatMap", "BackProject2D for the top-down view"}
        desc = "Backproject the output of the Top View."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        heat.Run(src)
        dst2 = heat.dst2

        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsTop, heat.dst0, dst3, task.rangesTop)

        dst3 = vbNormalize32f(dst3)

        dst3 = vbPalette(dst3)
    End Sub
End Class





Public Class BackProject2D_Side : Inherits VB_Algorithm
    Dim heat As New HeatMap_Basics
    Public Sub New()
        findCheckBox("Show Frustrum").Checked = False
        labels = {"", "", "Side View HeatMap", "BackProject2D for the side view"}
        desc = "Backproject the output of the Side View."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        heat.Run(src)
        dst2 = heat.dst3
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, heat.dst1, dst3, task.rangesSide)

        dst3 = vbNormalize32f(dst3)

        dst3 = vbPalette(dst3)
    End Sub
End Class
