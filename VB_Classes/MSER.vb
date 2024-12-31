Imports System.Runtime.InteropServices
Imports OpenCvSharp.Flann
Imports cvb = OpenCvSharp
' https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_Basics : Inherits TaskParent
    Dim detect As New MSER_CPP_VB
    Public mserCells As New List(Of rcData)
    Public floodPoints As New List(Of cvb.Point)

    Public Sub New()
        desc = "Create cells for each region in MSER output"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        detect.Run(src)
        Dim boxInput = New List(Of cvb.Rect)(detect.boxes)
        Dim boxes As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To boxInput.Count - 1
            Dim r = boxInput(i)
            boxes.Add(r.Width * r.Height, i)
        Next
        floodPoints = New List(Of cvb.Point)(detect.floodPoints)

        Dim sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)

        Dim matched As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)

        dst0 = detect.dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        For i = 0 To boxes.Count - 1
            Dim index = boxes.ElementAt(i).Value
            Dim rc As New rcData
            rc.rect = boxInput(index)
            Dim val = dst0.Get(Of Byte)(floodPoints(index).Y, floodPoints(index).X)
            rc.mask = dst0(rc.rect).InRange(val, val)
            rc.pixels = detect.maskCounts(index)

            rc.contour = ContourBuild(rc.mask, cvb.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            DrawContour(rc.mask, rc.contour, 255, -1)

            rc.floodPoint = floodPoints(index)
            rc.maxDist = GetMaxDist(rc)

            rc.indexLast = task.redMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            If rc.indexLast <> 0 And rc.indexLast < task.redCells.Count Then
                Dim lrc = task.redCells(rc.indexLast)
                rc.maxDStable = lrc.maxDStable
                rc.color = lrc.color
                matched.Add(rc.indexLast, rc.indexLast)
            Else
                rc.maxDStable = rc.maxDist
            End If

            cvb.Cv2.MeanStdDev(task.color(rc.rect), rc.colorMean, rc.colorStdev, rc.mask)
            rc.naturalColor = New cvb.Vec3b(CByte(rc.colorMean(0)), CByte(rc.colorMean(1)), CByte(rc.colorMean(2)))
            If rc.pixels > 0 Then sortedCells.Add(rc.pixels, rc)
        Next

        dst2 = RebuildCells(sortedCells)

        labels(2) = CStr(task.redCells.Count) + " cells were identified and " + CStr(matched.Count) + " were matched."
    End Sub
End Class






'https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_Detect : Inherits TaskParent
    Public boxes() As cvb.Rect
    Public regions()() As cvb.Point
    Public mser = cvb.MSER.Create
    Public options As New Options_MSER
    Public classCount As Integer
    Public Sub New()
        desc = "Run the core MSER (Maximally Stable Extremal Region) algorithm"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        options.RunOpt()

        dst2 = src.Clone

        If task.optionsChanged Then
            mser = cvb.MSER.Create(options.delta, options.minArea, options.maxArea, options.maxVariation, options.minDiversity,
                                  options.maxEvolution, options.areaThreshold, options.minMargin, options.edgeBlurSize)
            mser.Pass2Only = options.pass2Setting
        End If

        If options.graySetting And src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        mser.DetectRegions(src, regions, boxes)

        classCount = boxes.Count
        For Each z In boxes
            dst2.Rectangle(z, cvb.Scalar.Yellow, 1)
        Next
    End Sub
End Class








' https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_SyntheticInput : Inherits TaskParent
    Private Sub addNestedRectangles(img As cvb.Mat, p0 As cvb.Point, width() As Integer, color() As Integer, n As Integer)
        For i = 0 To n - 1
            img.Rectangle(New cvb.Rect(p0.X, p0.Y, width(i), width(i)), color(i), 1)
            p0 += New cvb.Point((width(i) - width(i + 1)) / 2, (width(i) - width(i + 1)) / 2)
            img.FloodFill(p0, color(i))
        Next
    End Sub
    Private Sub addNestedCircles(img As cvb.Mat, p0 As cvb.Point, width() As Integer, color() As Integer, n As Integer)
        For i = 0 To n - 1
            DrawCircle(img, p0, width(i) / 2, color(i))
            img.FloodFill(p0, color(i))
        Next
    End Sub
    Public Sub New()
        desc = "Build a synthetic image for MSER (Maximal Stable Extremal Regions) testing"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        Dim img = New cvb.Mat(800, 800, cvb.MatType.CV_8U, cvb.Scalar.All(0))
        Dim width() = {390, 380, 300, 290, 280, 270, 260, 250, 210, 190, 150, 100, 80, 70}
        Dim color1() = {80, 180, 160, 140, 120, 100, 90, 110, 170, 150, 140, 100, 220}
        Dim color2() = {81, 181, 161, 141, 121, 101, 91, 111, 171, 151, 141, 101, 221}
        Dim color3() = {175, 75, 95, 115, 135, 155, 165, 145, 85, 105, 115, 155, 35}
        Dim color4() = {173, 73, 93, 113, 133, 153, 163, 143, 83, 103, 113, 153, 33}

        addNestedRectangles(img, New cvb.Point(10, 10), width, color1, 13)
        addNestedCircles(img, New cvb.Point(200, 600), width, color2, 13)

        addNestedRectangles(img, New cvb.Point(410, 10), width, color3, 13)
        addNestedCircles(img, New cvb.Point(600, 600), width, color4, 13)

        img = img.Resize(New cvb.Size(src.Rows, src.Rows))
        dst2(New cvb.Rect(0, 0, src.Rows, src.Rows)) = img.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class








Public Class MSER_LeftRight : Inherits TaskParent
    Dim left As New MSER_Left
    Dim right As New MSER_Right
    Public Sub New()
        labels = {"", "", "MSER_Basics output for left camera", "MSER_Basics output for right camera"}
        desc = "Test MSER (Maximally Stable Extremal Region) algorithm on the left and right views."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        left.Run(task.leftView)
        dst2 = left.dst2
        labels(2) = left.labels(2)

        right.Run(task.rightView)
        dst3 = right.dst2
        labels(3) = right.labels(2)
    End Sub
End Class







Public Class MSER_Left : Inherits TaskParent
    Dim mBase As New MSER_Basics
    Public Sub New()
        labels = {"", "", "MSER_Basics output for left camera", "MSER_Basics rectangles found"}
        desc = "Test MSER (Maximally Stable Extremal Region) algorithm on the left and right views."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        mBase.Run(task.leftView)
        dst2 = mBase.dst2
        dst3 = mBase.dst3
        labels(2) = mBase.labels(2)
    End Sub
End Class








Public Class MSER_Right : Inherits TaskParent
    Dim mBase As New MSER_Basics
    Public Sub New()
        labels = {"", "", "MSER_Basics output for right camera", "MSER_Basics rectangles found"}
        desc = "Test MSER (Maximally Stable Extremal Region) algorithm on the left and right views."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        mBase.Run(task.rightView)
        dst2 = mBase.dst2
        dst3 = mBase.dst3
        labels(2) = mBase.labels(2)
    End Sub
End Class







' https://github.com/opencv/opencv/blob/master/samples/python/mser.py
Public Class MSER_Hulls : Inherits TaskParent
    Dim options As New Options_MSER
    Dim mBase As New MSER_Basics
    Public Sub New()
        desc = "Use MSER (Maximally Stable Extremal Region) but show the contours of each region."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        options.RunOpt()

        mBase.Run(src)
        dst2 = mBase.dst2

        Dim pixels As Integer
        dst3.SetTo(0)
        For Each rc In mBase.mserCells
            rc.hull = cvb.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
            pixels += rc.pixels
            DrawContour(dst3(rc.rect), rc.hull, rc.color, -1)
        Next

        If task.heartBeat Then labels(2) = CStr(mBase.mserCells.Count) + " Regions with average size " + If(mBase.mserCells.Count > 0,
                                          CStr(CInt(pixels / mBase.mserCells.Count)), "0")
    End Sub
End Class







' https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_TestSynthetic : Inherits TaskParent
    Dim options As New Options_MSER
    Dim synth As New MSER_SyntheticInput
    Dim mBase As New MSER_Basics
    Public Sub New()
        FindCheckBox("Use grayscale input").Checked = True
        labels = {"", "", "Synthetic input", "Output from MSER (Maximally Stable Extremal Region)"}
        desc = "Test MSER (Maximally Stable Extremal Region) with the synthetic image."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        options.RunOpt()

        synth.Run(src)
        dst2 = synth.dst2.Clone()

        mBase.Run(dst2)
        dst3 = mBase.dst3
    End Sub
End Class








Public Class MSER_Grayscale : Inherits TaskParent
    Dim mBase As New MSER_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        FindCheckBox("Use grayscale input").Checked = True
        desc = "Run MSER (Maximally Stable Extremal Region) with grayscale input"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        reduction.Run(src)

        mBase.Run(reduction.dst3)
        dst2 = mBase.dst2
        labels(2) = mBase.labels(2)
    End Sub
End Class







Public Class MSER_ReducedRGB : Inherits TaskParent
    Dim mBase As New MSER_Basics
    Dim reduction As New Reduction_BGR
    Public Sub New()
        FindCheckBox("Use grayscale input").Checked = False
        desc = "Run MSER (Maximally Stable Extremal Region) with a reduced RGB input"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        reduction.Run(src)

        mBase.Run(reduction.dst2)
        dst2 = mBase.dst3
        labels(2) = mBase.labels(2)
    End Sub
End Class







'https://github.com/opencv/opencv/blob/master/samples/cpp/detect_rr.cpp
Public Class MSER_ROI : Inherits TaskParent
    Public containers As New List(Of cvb.Rect)
    Dim options As New Options_MSER
    Dim core As New MSER_Detect
    Public Sub New()
        desc = "Identify the main regions of interest with MSER (Maximally Stable Extremal Region)"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        options.RunOpt()

        dst2 = src.Clone
        dst3 = src.Clone

        core.Run(src)

        Dim sortedBoxes As New SortedList(Of Integer, cvb.Rect)(New compareAllowIdenticalIntegerInverted)
        For Each box In core.boxes
            sortedBoxes.Add(box.Width * box.Height, box)
        Next

        Dim boxList As New List(Of cvb.Rect)
        For i = 0 To sortedBoxes.Count - 1
            boxList.Add(sortedBoxes.ElementAt(i).Value)
        Next

        containers.Clear()
        While boxList.Count > 0
            Dim box = boxList(0)
            containers.Add(box)
            Dim removeBoxes As New List(Of Integer)
            For i = 0 To boxList.Count - 1
                Dim b = boxList(i)
                Dim center = New cvb.Point(CInt(b.X + b.Width / 2), CInt(b.Y + b.Height / 2))
                If center.X >= box.X And center.X <= (box.X + box.Width) Then
                    If center.Y >= box.Y And center.Y <= (box.Y + box.Height) Then
                        removeBoxes.Add(i)
                        dst3.Rectangle(b, task.HighlightColor, task.lineWidth)
                    End If
                End If
            Next

            For i = removeBoxes.Count - 1 To 0 Step -1
                boxList.RemoveAt(removeBoxes.ElementAt(i))
            Next
        End While

        For Each rect In containers
            dst2.Rectangle(rect, task.HighlightColor, task.lineWidth)
        Next

        labels(2) = CStr(containers.Count) + " consolidated regions of interest located"
        labels(3) = CStr(sortedBoxes.Count) + " total rectangles found with MSER"
    End Sub
End Class







' https://github.com/shimat/opencvsharp/wiki/MSER
Public Class MSER_TestExample : Inherits TaskParent
    Dim image As cvb.Mat
    Dim mser As cvb.MSER
    Dim options As New Options_MSER
    Public Sub New()
        labels(2) = "Contour regions from MSER"
        labels(3) = "Box regions from MSER"
        If standaloneTest() Then task.gOptions.setDisplay1()
        desc = "Maximally Stable Extremal Regions example - still image"
        image = cvb.Cv2.ImRead(task.HomeDir + "Data/MSERtestfile.jpg", cvb.ImreadModes.Color)
        mser = cvb.MSER.Create()
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        options.RunOpt()

        Dim regions()() As cvb.Point
        Dim boxes() As cvb.Rect

        dst0 = image.Clone
        dst2 = image.Clone
        dst3 = image.Clone()

        If task.optionsChanged Then
            mser = cvb.MSER.Create(options.delta, options.minArea, options.maxArea, options.maxVariation, options.minDiversity,
                                  options.maxEvolution, options.areaThreshold, options.minMargin, options.edgeBlurSize)
            mser.Pass2Only = options.pass2Setting
        End If

        mser.DetectRegions(dst2, regions, boxes)
        Dim index As Integer
        For Each pts In regions
            Dim color = task.vecColors(index Mod 256)
            For Each pt In pts
                dst2.Set(Of cvb.Vec3b)(pt.Y, pt.X, color)
            Next
            index += 1
        Next

        For Each box In boxes
            dst3.Rectangle(box, task.HighlightColor, task.lineWidth + 1, task.lineType)
        Next
        labels(2) = CStr(boxes.Count) + " regions were found using MSER"
    End Sub
End Class






Public Class MSER_RedCloud : Inherits TaskParent
    Dim mBase As New MSER_Basics
    Public Sub New()
        desc = "Use the MSER_Basics output as input to RedCloud_Basics"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        mBase.Run(src)

        task.redC.Run(mBase.dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY))
        dst2 = task.redC.dst2
        labels(2) = task.redC.labels(2)
    End Sub
End Class







Public Class MSER_Mask_CPP_VB : Inherits TaskParent
    Dim options As New Options_MSER
    Dim redC As New RedCloud_Cells
    Public classCount As Integer
    Public Sub New()
        task.redOptions.setUseColorOnly(True)
        FindCheckBox("Use grayscale input").Checked = False
        options.RunOpt()
        cPtr = MSER_Open(options.delta, options.minArea, options.maxArea, options.maxVariation, options.minDiversity,
                         options.maxEvolution, options.areaThreshold, options.minMargin, options.edgeBlurSize, options.pass2Setting)
        desc = "MSER in a nutshell: intensity threshold, stability, maximize region, adaptive threshold."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        options.RunOpt()
        If task.optionsChanged Then
            MSER_Close(cPtr)
            cPtr = MSER_Open(options.delta, options.minArea, options.maxArea, options.maxVariation, options.minDiversity,
                             options.maxEvolution, options.areaThreshold, options.minMargin, options.edgeBlurSize, options.pass2Setting)
        End If

        If options.graySetting And src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        If task.heartBeat Then
            Dim cppData(src.Total * src.ElemSize - 1) As Byte
            Marshal.Copy(src.Data, cppData, 0, cppData.Length)
            Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
            Dim imagePtr = MSER_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels)
            handleSrc.Free()
            classCount = MSER_Count(cPtr)
            If classCount = 0 Then Exit Sub

            dst3 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8UC1, imagePtr).InRange(255, 255)
        End If
        labels(3) = CStr(classCount) + " regions identified"

        src.SetTo(white, dst3)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
    End Sub
    Public Sub Close()
        MSER_Close(cPtr)
    End Sub
End Class






Public Class MSER_Binarize : Inherits TaskParent
    Dim mser As New MSER_Basics
    Dim bin4 As New Bin4Way_Regions
    Public Sub New()
        desc = "Instead of a BGR src, try using the color output of Bin4Way_Regions"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        bin4.Run(src)
        dst2 = ShowPalette(bin4.dst2 * 255 / 4)

        mser.Run(dst2)
        dst3 = mser.dst2
        labels(3) = mser.labels(2)
    End Sub
End Class





Public Class MSER_Basics1 : Inherits TaskParent
    Dim detect As New MSER_CPP_VB
    Public Sub New()
        desc = "Create cells for each region in MSER output"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        detect.Run(src)
        dst3 = detect.dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        task.redC.Run(dst3)
        dst2 = task.redC.dst2
        labels(2) = task.redC.labels(2)
    End Sub
End Class





Public Class MSER_BasicsNew : Inherits TaskParent
    Dim detect As New MSER_CPP_VB
    Dim displaycount As Integer
    Public Sub New()
        desc = "Create cells for each region in MSER output"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        detect.Run(src)

        Dim boxInput = New List(Of cvb.Rect)(detect.boxes)
        Dim boxes As New SortedList(Of Integer, cvb.Rect)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To boxInput.Count - 1
            Dim r = boxInput(i)
            boxes.Add(r.Width * r.Height, r)
        Next

        dst3 = src
        For i = 0 To boxes.Count - 1
            Dim r = boxes.ElementAt(i).Value
            dst3.Rectangle(r, task.HighlightColor, task.lineWidth)
            If i >= displaycount Then Exit For
        Next

        If task.heartBeat Then
            labels(2) = "Displaying the largest " + CStr(displaycount) + " rectangles out of " + CStr(boxes.Count) + " found"
            ' displaycount += 1
            If displaycount >= boxes.Count Then displaycount = 0
        End If
    End Sub
End Class





Public Class MSER_Basics2 : Inherits TaskParent
    Dim detect As New MSER_CPP_VB
    Dim cellMap As New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
    Public Sub New()
        dst1 = New cvb.Mat(dst1.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Create cells for each region in MSER output"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        detect.Run(src)
        dst3 = detect.dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        Dim floodPoints = New List(Of cvb.Point)(detect.floodPoints)
        Dim boxInput = New List(Of cvb.Rect)(detect.boxes)
        Dim boxes As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To boxInput.Count - 1
            Dim r = boxInput(i)
            boxes.Add(r.Width * r.Height, i)
        Next

        Dim redCells As New List(Of rcData)({New rcData})
        dst1.SetTo(0)
        dst2.SetTo(0)
        Dim lastMap = cellMap.Clone
        cellMap.SetTo(0)
        Dim matchCount As Integer
        For i = 0 To floodPoints.Count - 1
            Dim rc As New rcData
            rc.index = redCells.Count
            rc.floodPoint = floodPoints(i)
            Dim val = dst3.Get(Of Byte)(rc.floodPoint.Y, rc.floodPoint.X)
            rc.rect = boxInput(boxes.ElementAt(i).Value)
            rc.mask = dst3(rc.rect).InRange(val, val)
            dst1(rc.rect).SetTo(rc.index, rc.mask)
            rc.pixels = detect.maskCounts(i)

            rc.maxDist = GetMaxDist(rc)
            rc.indexLast = lastMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)

            cvb.Cv2.MeanStdDev(task.color(rc.rect), rc.colorMean, rc.colorStdev, rc.mask)
            rc.color = New cvb.Vec3b(rc.colorMean(0), rc.colorMean(1), rc.colorMean(2))
            If rc.indexLast <> 0 Then matchCount += 1

            redCells.Add(rc)
            cellMap(rc.rect).SetTo(rc.index, rc.mask)
            dst2(rc.rect).SetTo(rc.color, rc.mask)
        Next

        If task.heartBeat Then labels(2) = detect.labels(2) + " and " + CStr(matchCount) + " were matched to the previous frame"
    End Sub
End Class








Public Class MSER_CPP_VB : Inherits TaskParent
    Dim options As New Options_MSER
    Public boxes As New List(Of cvb.Rect)
    Public floodPoints As New List(Of cvb.Point)
    Public maskCounts As New List(Of Integer)
    Public classcount As Integer
    Public Sub New()
        FindCheckBox("Use grayscale input").Checked = False
        options.RunOpt()
        cPtr = MSER_Open(options.delta, options.minArea, options.maxArea, options.maxVariation, options.minDiversity,
                         options.maxEvolution, options.areaThreshold, options.minMargin, options.edgeBlurSize, options.pass2Setting)
        desc = "C++ version of MSER basics."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        options.RunOpt()
        If task.optionsChanged Then
            MSER_Close(cPtr)
            cPtr = MSER_Open(options.delta, options.minArea, options.maxArea, options.maxVariation, options.minDiversity,
                             options.maxEvolution, options.areaThreshold, options.minMargin, options.edgeBlurSize, options.pass2Setting)
        End If

        If options.graySetting And src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = MSER_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels)
        handleSrc.Free()

        dst0 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8UC1, imagePtr).Clone

        classcount = MSER_Count(cPtr)
        If classcount = 0 Then Exit Sub

        Dim ptData = cvb.Mat.FromPixelData(classcount, 1, cvb.MatType.CV_32SC2, MSER_FloodPoints(cPtr))
        Dim maskData = cvb.Mat.FromPixelData(classcount, 1, cvb.MatType.CV_32S, MSER_MaskCounts(cPtr))
        Dim rectData = cvb.Mat.FromPixelData(classcount, 1, cvb.MatType.CV_32SC4, MSER_Rects(cPtr))

        Dim sortedBoxes As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        Dim rects As New List(Of cvb.Rect)
        For i = 0 To classcount - 1
            Dim r = rectData.Get(Of cvb.Rect)(i, 0)
            If rects.Contains(r) Then Continue For
            rects.Add(r)
            sortedBoxes.Add(r.Width * r.Height, i)
        Next

        boxes.Clear()
        floodPoints.Clear()
        maskCounts.Clear()
        For i = 0 To sortedBoxes.Count - 1
            Dim index = sortedBoxes.ElementAt(i).Value
            boxes.Add(rectData.Get(Of cvb.Rect)(index, 0))
            floodPoints.Add(ptData.Get(Of cvb.Point)(index, 0))
            maskCounts.Add(maskData.Get(Of Integer)(index, 0))
        Next

        dst2 = ShowPalette(dst0 * 255 / classcount)
        If standaloneTest() Then
            dst3 = src
            For i = 0 To boxes.Count - 1
                dst3.Rectangle(boxes(i), task.HighlightColor, task.lineWidth)
                If i < task.redOptions.identifyCount Then SetTrueText(CStr(i + 1), boxes(i).TopLeft, 3)
            Next
        End If
        labels(2) = CStr(classcount) + " regions identified"
    End Sub
    Public Sub Close()
        MSER_Close(cPtr)
    End Sub
End Class