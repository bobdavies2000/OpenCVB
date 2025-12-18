Imports cv = OpenCvSharp
' https://docs.opencvb.org/3.4/dc/df6/tutorial_py_Hist_backprojection.html
Namespace VBClasses
    Public Class BackProject2D_Basics : Inherits TaskParent
        Public hist2d As New Hist2D_Basics
        Public colorFmt As New Color_Basics
        Public backProjectByGrid As Boolean = True
        Public classCount As Integer
        Public Sub New()
            desc = "A 2D histogram is built from 2 channels of any 3-channel input and the results are displayed."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim index As Integer = Task.gridMap.Get(Of Integer)(Task.mouseMovePoint.Y, Task.mouseMovePoint.X)
            Dim roi = Task.gridRects(index)

            colorFmt.Run(Task.color)
            hist2d.Run(colorFmt.dst2)
            dst2 = hist2d.dst2

            If standaloneTest() Then DrawRect(dst2, roi, white)

            Dim histogram As New cv.Mat
            If backProjectByGrid Then
                histogram = Task.gridMap.Clone
            Else
                histogram = New cv.Mat(hist2d.histogram.Size, cv.MatType.CV_32F, cv.Scalar.All(0))
                hist2d.histogram(roi).CopyTo(histogram(roi))
            End If
            cv.Cv2.CalcBackProject({colorFmt.dst2}, hist2d.channels, histogram, dst0, hist2d.ranges)

            Dim bpCount = hist2d.histogram(roi).CountNonZero

            If backProjectByGrid Then
                Dim mm = GetMinMax(dst0)
                classCount = mm.maxVal
                dst3 = PaletteFull(dst0)
            Else
                dst3.SetTo(0)
                dst3.SetTo(cv.Scalar.Yellow, dst0)
            End If
            If Task.heartBeat Then
                labels(2) = colorFmt.options.colorFormat + " format " + If(classCount > 0, CStr(classCount) + " classes", " ")
                Dim c1 = Task.channels(0), c2 = Task.channels(1)
                labels(3) = "That combination of channel " + CStr(c1) + "/" + CStr(c2) + " has " + CStr(bpCount) +
                            " pixels while image total is " + Format(dst0.Total, "0")
            End If
            SetTrueText("Use Global Algorithm Option 'grid Square Size' to control the 2D backprojection",
                        New cv.Point(10, dst3.Height - 20), 3)
        End Sub
    End Class






    ' https://docs.opencvb.org/3.4/dc/df6/tutorial_py_Hist_backprojection.html
    'Public Class BackProject2D_BasicsOld : Inherits TaskParent
    '    Public hist2d As New Hist2D_Basics
    '    Public xRange As Integer = 255
    '    Public yRange As Integer = 255
    '    Public minX As Single, maxX As Single, minY As Single, maxY As Single
    '    Public colorFmt As New Color_Basics
    '    Public bpCol As Integer, bpRow As Integer
    '    Public Sub New()
    '        If standalone Then task.gOptions.setGridSize(5)
    '        desc = "A 2D histogram is built from 2 channels of any 3-channel input and the results are displayed."
    '    End Sub
    '    Public Overrides sub RunAlg(src As cv.Mat)
    '        bpCol = Math.Floor(task.mouseMovePoint.X / task.bricksPerRow)
    '        bpRow = Math.Floor(task.mouseMovePoint.Y / task.bricksPerCol)

    '        colorFmt.Run(src)
    '        hist2d.Run(colorFmt.dst2)
    '        dst2 = hist2d.dst2

    '        minX = bpRow * xRange / task.brickSize
    '        maxX = (bpRow + 1) * xRange / task.brickSize
    '        minY = bpCol * yRange / task.brickSize
    '        maxY = (bpCol + 1) * yRange / task.brickSize

    '        Dim ranges() = New cv.Rangef() {New cv.Rangef(minX, maxX), New cv.Rangef(minY, maxY)}
    '        cv.Cv2.CalcBackProject({src}, task.gOptions.channels, hist2d.histogram, dst0, ranges)
    '        Dim bpCount = hist2d.histogram.Get(Of Single)(bpRow, bpCol)

    '        dst3.SetTo(0)
    '        dst3.SetTo(cv.Scalar.Yellow, dst0)
    '        If task.heartBeat Then
    '            labels(2) = colorFmt.options.colorFormat + ": Cell minX/maxX " + Format(minX, "0") + "/" + Format(maxX, "0") + " minY/maxY " +
    '                                Format(minY, "0") + "/" + Format(maxY, "0")
    '            Dim c1 = task.gOptions.channels(0), c2 = task.gOptions.channels(1)
    '            labels(3) = "That combination of channel " + CStr(c1) + "/" + CStr(c2) + " has " + CStr(bpCount) +
    '                        " pixels while image total is " + Format(dst0.Total, "0")
    '        End If
    '        SetTrueText("Use Global Algorithm Option 'grid Square Size' to control the 2D histogram at left",
    '                    New cv.Point(10, dst3.Height - 20), 3)
    '    End Sub
    'End Class





    Public Class BackProject2D_Compare : Inherits TaskParent
        Dim hueSat As New PhotoShop_HSV
        Dim backP As New BackProject2D_Basics
        Dim mats As New Mat_4Click
        Public Sub New()
            labels(2) = "Hue (upper left), sat (upper right), highlighted backprojection (bottom left)"
            If standalone Then Task.gOptions.GridSlider.Value = 10
            desc = "Compare the hue and brightness images and the results of the Hist_backprojection2d"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            hueSat.Run(Task.color.Clone)
            mats.mat(0) = hueSat.dst2
            mats.mat(1) = hueSat.dst3

            backP.Run(Task.color)
            mats.mat(2) = backP.dst3

            If Task.firstPass Then mats.quadrant = 3
            mats.Run(emptyMat)
            dst2 = mats.dst2
            dst3 = mats.dst3

            labels(3) = backP.labels(3)

            SetTrueText("Use Global Algorithm Option 'grid Square Size' to control this 2D histogram." + vbCrLf +
                    "Move mouse in 2D histogram to select a cell to backproject." + vbCrLf +
                    "Click any quadrant at left to display that quadrant here." + vbCrLf,
                    New cv.Point(10, dst3.Height - dst3.Height / 4), 3)
        End Sub
    End Class






    Public Class BackProject2D_Top : Inherits TaskParent
        Dim heat As New HeatMap_Basics
        Public Sub New()
            labels = {"", "", "Top Down HeatMap", "BackProject2D for the top-down view"}
            desc = "Backproject the output of the Top View."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            heat.Run(src)
            dst2 = heat.dst2

            cv.Cv2.CalcBackProject({Task.pointCloud}, Task.channelsTop, heat.histogramTop, dst1, Task.rangesTop)
            dst1 = dst1.ConvertScaleAbs()
            dst1.ConvertTo(dst1, cv.MatType.CV_8U)
            dst3 = PaletteFull(dst1)
        End Sub
    End Class





    Public Class BackProject2D_Side : Inherits TaskParent
        Dim heat As New HeatMap_Basics
        Public Sub New()
            labels = {"", "", "Side View HeatMap", "BackProject2D for the side view"}
            desc = "Backproject the output of the Side View."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            heat.Run(src)
            dst2 = heat.dst3

            cv.Cv2.CalcBackProject({Task.pointCloud}, Task.channelsSide, heat.histogramSide, dst1, Task.rangesSide)
            dst1 = dst1.ConvertScaleAbs()
            dst1.ConvertTo(dst1, cv.MatType.CV_8U)
            dst3 = PaletteFull(dst1)
        End Sub
    End Class








    Public Class BackProject2D_Filter : Inherits TaskParent
        Public threshold As Integer
        Public histogram As New cv.Mat
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_32FC3, 0)
            Task.gOptions.setHistogramBins(100) ' extra bins to help isolate the stragglers.
            desc = "Filter a 2D histogram for the backprojection."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then
                cv.Cv2.CalcHist({Task.pointCloud}, Task.channelsSide, New cv.Mat, histogram, 2, Task.bins2D, Task.rangesSide)
            End If
            'histogram.Col(0).SetTo(0)
            dst2 = histogram.Threshold(threshold, 255, cv.ThresholdTypes.Binary)
        End Sub
    End Class







    Public Class BackProject2D_FilterSide : Inherits TaskParent
        Public filter As New BackProject2D_Filter
        Dim options As New Options_HistXD
        Public Sub New()
            desc = "Backproject the output of the Side View after removing low sample bins."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim histogram As New cv.Mat
            cv.Cv2.CalcHist({Task.pointCloud}, Task.channelsSide, New cv.Mat, histogram, 2, Task.bins2D, Task.rangesSide)

            filter.threshold = options.sideThreshold
            filter.histogram = histogram
            filter.Run(src)

            cv.Cv2.CalcBackProject({Task.pointCloud}, Task.channelsSide, filter.histogram, dst1, Task.rangesSide)
            dst1.ConvertTo(dst1, cv.MatType.CV_8U)

            dst2.SetTo(0)
            Task.pointCloud.CopyTo(dst2, dst1)
        End Sub
    End Class








    Public Class BackProject2D_FilterTop : Inherits TaskParent
        Dim filter As New BackProject2D_Filter
        Dim options As New Options_HistXD
        Public Sub New()
            desc = "Backproject the output of the Side View after removing low sample bins."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim histogram As New cv.Mat
            cv.Cv2.CalcHist({Task.pointCloud}, Task.channelsSide, New cv.Mat, histogram, 2, Task.bins2D, Task.rangesSide)

            filter.threshold = options.topThreshold
            filter.histogram = histogram
            filter.Run(src)

            cv.Cv2.CalcBackProject({Task.pointCloud}, Task.channelsTop, filter.dst2, dst1, Task.rangesTop)
            dst1.ConvertTo(dst1, cv.MatType.CV_8U)

            dst2.SetTo(0)
            Task.pointCloud.CopyTo(dst2, dst1)
        End Sub
    End Class








    Public Class BackProject2D_FilterBoth : Inherits TaskParent
        Dim filterSide As New BackProject2D_FilterSide
        Dim filterTop As New BackProject2D_FilterTop
        Public Sub New()
            desc = "Backproject the output of the both the top and side views after removing low sample bins."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            filterSide.Run(src)
            filterTop.Run(src)

            dst2.SetTo(0)
            Task.pointCloud.CopyTo(dst2, filterSide.dst1)
            Task.pointCloud.CopyTo(dst3, filterTop.dst1)
        End Sub
    End Class






    Public Class BackProject2D_Full : Inherits TaskParent
        Dim backP As New BackProject2D_Basics
        Public classCount As Integer
        Public Sub New()
            backP.backProjectByGrid = True
            desc = "Backproject the 2D histogram marking each grid element's backprojection"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            backP.Run(src)
            dst2 = backP.dst0
            If standaloneTest() Then dst3 = backP.dst3
            classCount = backP.classCount
            labels = backP.labels
        End Sub
    End Class








    Public Class BackProject2D_RowCol : Inherits TaskParent
        Dim backp As New BackProject2D_Basics
        Dim options As New Options_BackProject2D
        Public Sub New()
            OptionParent.findRadio("HSV").Checked = True
            If standalone Then Task.gOptions.displayDst1.Checked = True
            Task.gOptions.GridSlider.Value = 10
            desc = "Backproject the whole row or column of the 2D histogram"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst0 = Task.color.Clone

            Dim selection = If(options.backProjectRow, "Row", "Col")
            labels(2) = "Histogram 2D with Backprojection by " + selection

            backp.Run(Task.color)
            dst2 = Mat_Convert.Mat_32f_To_8UC3(backp.dst2) * 255

            Dim roi = Task.gridRects(Task.gridMap.Get(Of Integer)(Task.mouseMovePoint.Y, Task.mouseMovePoint.X))
            Dim rect As cv.Rect
            If options.backProjectRow Then
                rect = New cv.Rect(0, roi.Y, dst2.Width, roi.Height)
            Else
                rect = New cv.Rect(roi.X, 0, roi.Width, dst2.Height)
            End If
            dst2.Rectangle(rect, Task.highlight, Task.lineWidth)
            Dim histData As New cv.Mat(backp.hist2d.histogram.Size, cv.MatType.CV_32F, cv.Scalar.All(0))
            backp.hist2d.histogram(rect).CopyTo(histData(rect))

            Dim ranges() = backp.hist2d.ranges
            cv.Cv2.CalcBackProject({Task.color}, backp.hist2d.channels, histData, dst1, ranges)

            dst3.SetTo(0)
            dst3.SetTo(cv.Scalar.Yellow, dst1)
            dst0.SetTo(0, dst1)

            If Task.heartBeat Then
                Dim count = histData(rect).Sum
                labels(3) = "Selected " + selection + " = " + CStr(histData(rect).CountNonZero) + " non-zero histogram entries representing total pixels of " + CStr(count)
            End If

            If Task.heartBeat Then
                strOut = "Use Global Algorithm Option 'grid Square Size' to control the 2D histogram." + vbCrLf +
                     "Move mouse in 2D histogram to select a row or column to backproject."
            End If

            SetTrueText(strOut, 1)
        End Sub
    End Class
End Namespace