Imports System.Runtime.InteropServices
Imports OpenCvSharp
Imports cv = OpenCvSharp
' https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_Basics : Inherits VB_Algorithm
    Dim detect As New MSER_CPP
    Dim matchCell As New RedCloud_MatchCell
    Public cellMap As cv.Mat
    Public mserCells As New List(Of rcData)
    Public boxes As New List(Of cv.Rect)
    Public floodPoints As New List(Of cv.Point)
    Public useOpAuto As Boolean = True
    Public Sub New()
        cellMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Create cells for each region in MSER output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If useOpAuto Then
            Static opAuto As New OpAuto_MSER
            opAuto.classCount = mserCells.Count
            opAuto.Run(src)
        End If

        detect.Run(src)
        boxes = New List(Of cv.Rect)(detect.boxes)
        floodPoints = New List(Of cv.Point)(detect.floodPoints)
        task.redOther = 0

        Dim prepCells As New SortedList(Of Integer, rcPrep)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To detect.boxes.Count - 1
            Dim rp As New rcPrep
            rp.rect = detect.boxes(i)
            rp.mask = detect.dst0(rp.rect).InRange(i, i)
            rp.maxDist = vbGetMaxDist(rp.mask)
            rp.floodPoint = floodPoints(i)
            rp.pixels = detect.maskCounts(i)
            prepCells.Add(rp.pixels, rp)
        Next

        If task.optionsChanged Then
            cellMap.SetTo(task.redOther)
            task.lastCells.Clear()
        End If

        matchCell.lastCellMap = cellMap.Clone
        task.lastCells = New List(Of rcData)(mserCells)
        matchCell.usedColors.Clear()
        matchCell.usedColors.Add(black)

        mserCells.Clear()
        mserCells.Add(New rcData) ' "Other"

        cellMap.SetTo(task.redOther)
        dst2.SetTo(0)
        For Each key In prepCells
            Dim rp = key.Value
            rp.maxDist = vbGetMaxDist(rp)

            rp.index = mserCells.Count
            matchCell.rp = rp
            matchCell.Run(Nothing)

            Dim rc = matchCell.rc

            mserCells.Add(rc)

            If mserCells.Count = 255 Then Exit For
        Next

        If task.paused = False Then
            For Each rc In task.lastCells
                Dim val = detect.dst0.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                If val = task.redOther And rc.index <> task.redOther Then
                    rc.index = mserCells.Count
                    mserCells.Add(rc)
                End If
            Next
        End If

        For Each rc In mserCells
            cellMap(rc.rect).SetTo(rc.index, rc.mask)
            vbDrawContour(dst2(rc.rect), rc.contour, rc.color, -1)
            setTrueText(CStr(rc.index), rc.maxDist)
        Next

        If src.Channels = 3 Then dst3 = src.Clone Else dst3 = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        For Each r In boxes
            dst3.Rectangle(r, task.highlightColor, task.lineWidth)
        Next

        If heartBeat() Then labels(2) = CStr(mserCells.Count) + " Cells identified"
    End Sub
End Class







'https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_Detect : Inherits VB_Algorithm
    Public boxes() As cv.Rect
    Public regions()() As cv.Point
    Public mser = cv.MSER.Create
    Public options As New Options_MSER
    Public classCount As Integer
    Public Sub New()
        desc = "Run the core MSER (Maximally Stable Extremal Region) algorithm"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        dst2 = src.Clone

        If task.optionsChanged Then
            mser = cv.MSER.Create(options.delta, options.minArea, options.maxArea, options.maxVariation, options.minDiversity,
                                  options.maxEvolution, options.areaThreshold, options.minMargin, options.edgeBlurSize)
            mser.Pass2Only = options.pass2Setting
        End If

        If options.graySetting And src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        mser.DetectRegions(src, regions, boxes)

        classCount = boxes.Count
        For Each z In boxes
            dst2.Rectangle(z, cv.Scalar.Yellow, 1)
        Next
    End Sub
End Class








' https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_SyntheticInput : Inherits VB_Algorithm
    Private Sub addNestedRectangles(img As cv.Mat, p0 As cv.Point, width() As Integer, color() As Integer, n As Integer)
        For i = 0 To n - 1
            img.Rectangle(New cv.Rect(p0.X, p0.Y, width(i), width(i)), color(i), 1)
            p0 += New cv.Point((width(i) - width(i + 1)) / 2, (width(i) - width(i + 1)) / 2)
            img.FloodFill(p0, color(i))
        Next
    End Sub
    Private Sub addNestedCircles(img As cv.Mat, p0 As cv.Point, width() As Integer, color() As Integer, n As Integer)
        For i = 0 To n - 1
            img.Circle(p0, width(i) / 2, color(i), task.lineWidth, task.lineType)
            img.FloodFill(p0, color(i))
        Next
    End Sub
    Public Sub New()
        desc = "Build a synthetic image for MSER (Maximal Stable Extremal Regions) testing"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim img = New cv.Mat(800, 800, cv.MatType.CV_8U, 0)
        Dim width() = {390, 380, 300, 290, 280, 270, 260, 250, 210, 190, 150, 100, 80, 70}
        Dim color1() = {80, 180, 160, 140, 120, 100, 90, 110, 170, 150, 140, 100, 220}
        Dim color2() = {81, 181, 161, 141, 121, 101, 91, 111, 171, 151, 141, 101, 221}
        Dim color3() = {175, 75, 95, 115, 135, 155, 165, 145, 85, 105, 115, 155, 35}
        Dim color4() = {173, 73, 93, 113, 133, 153, 163, 143, 83, 103, 113, 153, 33}

        addNestedRectangles(img, New cv.Point(10, 10), width, color1, 13)
        addNestedCircles(img, New cv.Point(200, 600), width, color2, 13)

        addNestedRectangles(img, New cv.Point(410, 10), width, color3, 13)
        addNestedCircles(img, New cv.Point(600, 600), width, color4, 13)

        img = img.Resize(New cv.Size(src.Rows, src.Rows))
        dst2(New cv.Rect(0, 0, src.Rows, src.Rows)) = img.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class








Public Class MSER_LeftRight : Inherits VB_Algorithm
    Dim left As New MSER_Left
    Dim right As New MSER_Right
    Public Sub New()
        labels = {"", "", "MSER_Basics output for left camera", "MSER_Basics output for right camera"}
        desc = "Test MSER (Maximally Stable Extremal Region) algorithm on the left and right views."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        left.Run(task.leftView)
        dst2 = left.dst2
        labels(2) = left.labels(2)

        right.Run(task.rightView)
        dst3 = right.dst2
        labels(3) = right.labels(2)
    End Sub
End Class







Public Class MSER_Left : Inherits VB_Algorithm
    Dim mBase As New MSER_Basics
    Public Sub New()
        labels = {"", "", "MSER_Basics output for left camera", "MSER_Basics rectangles found"}
        desc = "Test MSER (Maximally Stable Extremal Region) algorithm on the left and right views."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        mBase.Run(task.leftView)
        dst2 = mBase.dst2
        dst3 = mBase.dst3
        labels(2) = mBase.labels(2)
    End Sub
End Class








Public Class MSER_Right : Inherits VB_Algorithm
    Dim mBase As New MSER_Basics
    Public Sub New()
        labels = {"", "", "MSER_Basics output for right camera", "MSER_Basics rectangles found"}
        desc = "Test MSER (Maximally Stable Extremal Region) algorithm on the left and right views."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        mBase.Run(task.rightView)
        dst2 = mBase.dst2
        dst3 = mBase.dst3
        labels(2) = mBase.labels(2)
    End Sub
End Class







' https://github.com/opencv/opencv/blob/master/samples/python/mser.py
Public Class MSER_Hulls : Inherits VB_Algorithm
    Dim options As New Options_MSER
    Dim mBase As New MSER_Basics
    Public Sub New()
        desc = "Use MSER (Maximally Stable Extremal Region) but show the contours of each region."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        mBase.Run(src)
        dst2 = mBase.dst2

        Dim pixels As Integer
        dst3.SetTo(0)
        For Each rc In mBase.mserCells
            rc.hull = cv.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
            pixels += rc.pixels
            vbDrawContour(dst3(rc.rect), rc.hull, rc.color, -1)
        Next

        If heartBeat() Then labels(2) = CStr(mBase.mserCells.Count) + " Regions with average size " + CStr(CInt(pixels / mBase.mserCells.Count))
    End Sub
End Class







' https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_TestSynthetic : Inherits VB_Algorithm
    Dim options As New Options_MSER
    Dim synth As New MSER_SyntheticInput
    Dim mBase As New MSER_Basics
    Public Sub New()
        findCheckBox("Use grayscale input").Checked = True
        labels = {"", "", "Synthetic input", "Output from MSER (Maximally Stable Extremal Region)"}
        desc = "Test MSER (Maximally Stable Extremal Region) with the synthetic image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        synth.Run(src)
        dst2 = synth.dst2.Clone()

        mBase.Run(dst2)
        dst3 = mBase.dst3
    End Sub
End Class








Public Class MSER_RedCloud : Inherits VB_Algorithm
    Dim mBase As New MSER_Basics
    Dim colorC As New RedCloud_ColorOnly
    Public Sub New()
        desc = "Use the MSER_Basics output as input to RedCloud_Color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        mBase.Run(src)

        colorC.Run(mBase.dst2)
        dst2 = colorC.dst2
    End Sub
End Class







Public Class MSER_Grayscale : Inherits VB_Algorithm
    Dim mBase As New MSER_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        findCheckBox("Use grayscale input").Checked = True
        desc = "Run MSER (Maximally Stable Extremal Region) with grayscale input"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)

        mBase.Run(reduction.dst2)
        dst2 = mBase.dst3
        labels(2) = mBase.labels(2)
    End Sub
End Class







Public Class MSER_ReducedRGB : Inherits VB_Algorithm
    Dim mBase As New MSER_Basics
    Dim reduction As New Reduction_RGB
    Public Sub New()
        findCheckBox("Use grayscale input").Checked = False
        desc = "Run MSER (Maximally Stable Extremal Region) with a reduced RGB input"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)

        mBase.Run(reduction.dst2)
        dst2 = mBase.dst3
        labels(2) = mBase.labels(2)
    End Sub
End Class









Public Class MSER_RegionLeft : Inherits VB_Algorithm
    Dim regions As New MSER_Regions
    Public Sub New()
        desc = "Identify the region using the left image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        regions.Run(task.leftView)
        dst2 = regions.dst2
    End Sub
End Class









Public Class MSER_RegionRight : Inherits VB_Algorithm
    Dim regions As New MSER_Regions
    Public Sub New()
        desc = "Identify the region using the right image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        regions.Run(task.rightView)
        dst2 = regions.dst2
    End Sub
End Class








Public Class MSER_RegionLeftRight : Inherits VB_Algorithm
    Dim left As New MSER_RegionLeft
    Dim right As New MSER_RegionRight
    Public Sub New()
        desc = "Identify the region using both left and right images"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        left.Run(task.leftView)
        dst2 = left.dst2

        right.Run(task.rightView)
        dst3 = right.dst2
    End Sub
End Class






'https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_ROI : Inherits VB_Algorithm
    Public containers As New List(Of cv.Rect)
    Dim options As New Options_MSER
    Dim core As New MSER_Detect
    Public Sub New()
        desc = "Identify the main regions of interest with MSER (Maximally Stable Extremal Region)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        dst2 = src.Clone
        dst3 = src.Clone

        core.Run(src)

        Dim sortedBoxes As New SortedList(Of Integer, cv.Rect)(New compareAllowIdenticalIntegerInverted)
        For Each box In core.boxes
            sortedBoxes.Add(box.Width * box.Height, box)
        Next

        Dim boxList As New List(Of cv.Rect)
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
                Dim center = New cv.Point(CInt(b.X + b.Width / 2), CInt(b.Y + b.Height / 2))
                If center.X >= box.X And center.X <= (box.X + box.Width) Then
                    If center.Y >= box.Y And center.Y <= (box.Y + box.Height) Then
                        removeBoxes.Add(i)
                        dst3.Rectangle(b, task.highlightColor, task.lineWidth)
                    End If
                End If
            Next

            For i = removeBoxes.Count - 1 To 0 Step -1
                boxList.RemoveAt(removeBoxes.ElementAt(i))
            Next
        End While

        For Each rect In containers
            dst2.Rectangle(rect, task.highlightColor, task.lineWidth)
        Next

        labels(2) = CStr(containers.Count) + " consolidated regions of interest located"
        labels(3) = CStr(sortedBoxes.Count) + " total rectangles found with MSER"
    End Sub
End Class








Module MSER_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MSER_Open(delta As Integer, minArea As Integer, maxArea As Integer, maxVariation As Single, minDiversity As Single,
                              maxEvolution As Integer, areaThreshold As Single, minMargin As Single, edgeBlurSize As Integer,
                              pass2Setting As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub MSER_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MSER_Rects(cPtr As IntPtr) As IntPtr
    End Function

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MSER_FloodPoints(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MSER_MaskCounts(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MSER_Count(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MSER_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Integer, cols As Integer, channels As Integer) As IntPtr
    End Function
End Module








Public Class MSER_Regions : Inherits VB_Algorithm
    Dim core As New MSER_Detect
    Public mserCells As New List(Of rcData)
    Dim matchCell As New RedCloud_MatchCell
    Public cellMap As cv.Mat
    Public useOpAuto As Boolean = True
    Public Sub New()
        cellMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Latest frame only - no accumulation"
        desc = "Tag and track the MSER (Maximally Stable Extremal Region) regions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If useOpAuto Then
            Static opAuto As New OpAuto_MSER
            opAuto.classCount = mserCells.Count
            opAuto.Run(src)
        End If
        Dim mserLast = 255

        core.Run(src)

        Dim prepCells As New SortedList(Of Integer, rcPrep)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To core.boxes.Count - 1
            Dim rp As New rcPrep
            rp.rect = core.boxes(i)
            rp.mask = New cv.Mat(rp.rect.Height, rp.rect.Width, cv.MatType.CV_8U, 0)
            rp.floodPoint = core.regions(i)(0)
            rp.pixels = core.regions(i).Count
            For Each pt In core.regions(i)
                rp.mask.Set(Of Byte)(pt.Y - rp.rect.Y, pt.X - rp.rect.X, i Mod 255)
            Next
            prepCells.Add(rp.pixels, rp)
        Next

        If task.optionsChanged Then
            cellMap.SetTo(mserLast)
            task.lastCells.Clear()
        End If

        matchCell.lastCellMap = cellMap.Clone
        task.lastCells = New List(Of rcData)(mserCells)
        matchCell.usedColors.Clear()
        matchCell.usedColors.Add(black)

        mserCells.Clear()
        cellMap.SetTo(mserLast)
        Dim lastDst2 = dst2.Clone
        If heartBeat() Then dst2.SetTo(0)
        Dim spotsRemoved As Integer
        dst3.SetTo(0)
        For Each key In prepCells
            Dim rp = key.Value
            rp.maxDist = vbGetMaxDist(rp)

            Dim spotTakenTest = cellMap.Get(Of Byte)(rp.maxDist.Y, rp.maxDist.X)
            If spotTakenTest <> mserLast Then
                spotsRemoved += 1
                Continue For
            End If

            rp.index = mserCells.Count
            matchCell.rp = rp
            matchCell.Run(Nothing)

            Dim rc = matchCell.rc

            If rc.pixels > 0 And rc.pixels < task.minPixels Then Continue For
            Dim color = lastDst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
            If color <> black Then rc.color = color
            mserCells.Add(rc)

            cellMap(rc.rect).SetTo(rc.index, rc.mask)
            vbDrawContour(dst2(rc.rect), rc.contour, rc.color, -1)
            vbDrawContour(dst3(rc.rect), rc.contour, rc.color, -1)
            If mserCells.Count = 254 Then Exit For
        Next

        labels(2) = "Cells identified " + CStr(mserCells.Count) + "  Overlapping cells removed " + CStr(spotsRemoved)
    End Sub
End Class







Public Class MSER_CPP : Inherits VB_Algorithm
    Dim Options As New Options_MSER
    Public boxes As New List(Of cv.Rect)
    Public floodPoints As New List(Of cv.Point)
    Public maskCounts As New List(Of Integer)
    Public Sub New()
        findCheckBox("Use grayscale input").Checked = False
        Options.RunVB()
        cPtr = MSER_Open(Options.delta, Options.minArea, Options.maxArea, Options.maxVariation, Options.minDiversity,
                         Options.maxEvolution, Options.areaThreshold, Options.minMargin, Options.edgeBlurSize, Options.pass2Setting)
        desc = "C++ version of MSER basics."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Options.RunVB()
        If task.optionsChanged Then
            MSER_Close(cPtr)
            cPtr = MSER_Open(Options.delta, Options.minArea, Options.maxArea, Options.maxVariation, Options.minDiversity,
                             Options.maxEvolution, Options.areaThreshold, Options.minMargin, Options.edgeBlurSize, Options.pass2Setting)
        End If

        If Options.graySetting And src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = MSER_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels)
        handleSrc.Free()

        dst0 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, imagePtr).Clone

        Dim count = MSER_Count(cPtr)
        If count = 0 Then Exit Sub

        Dim ptData = New cv.Mat(count, 1, cv.MatType.CV_32SC2, MSER_FloodPoints(cPtr))
        Dim maskData = New cv.Mat(count, 1, cv.MatType.CV_32S, MSER_MaskCounts(cPtr))
        Dim rectData = New cv.Mat(count, 1, cv.MatType.CV_32SC4, MSER_Rects(cPtr))
        boxes.Clear()
        floodPoints.Clear()
        maskCounts.Clear()
        For i = 0 To count - 1
            boxes.Add(rectData.Get(Of cv.Rect)(i, 0))
            floodPoints.Add(ptData.Get(Of cv.Point)(i, 0))
            maskCounts.Add(maskData.Get(Of Integer)(i, 0))
        Next

        If standalone Or testIntermediate(traceName) Then
            dst2 = vbPalette(dst0 * 255 / count)

            dst3 = src
            For Each r In boxes
                dst3.Rectangle(r, task.highlightColor, task.lineWidth)
            Next
        End If
        labels(2) = CStr(count) + " regions identified"
    End Sub
    Public Sub Close()
        MSER_Close(cPtr)
    End Sub
End Class





' https://github.com/shimat/opencvsharp/wiki/MSER
Public Class MSER_TestExample : Inherits VB_Algorithm
    Dim image As cv.Mat
    Dim mser As cv.MSER
    Dim options As New Options_MSER
    Public Sub New()
        labels(2) = "Contour regions from MSER"
        labels(3) = "Box regions from MSER"
        If standalone Then gOptions.displayDst0.Checked = True
        desc = "Maximally Stable Extremal Regions example - still image"
        image = cv.Cv2.ImRead(task.homeDir + "Data/MSERtestfile.jpg", cv.ImreadModes.Color)
        mser = cv.MSER.Create()
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Dim regions()() As cv.Point
        Dim boxes() As cv.Rect

        dst0 = image.Clone
        dst2 = image.Clone
        dst3 = image.Clone()

        If task.optionsChanged Then
            mser = cv.MSER.Create(options.delta, options.minArea, options.maxArea, options.maxVariation, options.minDiversity,
                                  options.maxEvolution, options.areaThreshold, options.minMargin, options.edgeBlurSize)
            mser.Pass2Only = options.pass2Setting
        End If

        mser.DetectRegions(dst2, regions, boxes)
        Dim index As Integer
        For Each pts In regions
            Dim color = task.vecColors(index Mod 255)
            For Each pt In pts
                dst2.Set(Of cv.Vec3b)(pt.Y, pt.X, color)
            Next
            index += 1
        Next

        For Each box In boxes
            dst3.Rectangle(box, task.highlightColor, task.lineWidth + 1, task.lineType)
        Next
        labels(2) = CStr(boxes.Count) + " regions were found using MSER"
    End Sub
End Class