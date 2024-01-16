Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Distance_Basics : Inherits VB_Algorithm
    Dim options As New Options_Distance
    Public Sub New()
        labels = {"", "", "Distance transform - create a mask with threshold", ""}
        desc = "Distance algorithm basics."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If standalone Then src = task.depthRGB
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim dst0 = src.DistanceTransform(options.distanceType, options.kernelSize)
        dst1 = vbNormalize32f(dst0)
        dst1.ConvertTo(dst2, cv.MatType.CV_8UC1)
    End Sub
End Class






Public Class Distance_Labels : Inherits VB_Algorithm
    Dim options As New Options_Distance
    Public Sub New()
        labels(2) = "Distance results"
        labels(3) = "Input mask to distance transformm"
        desc = "Distance algorithm basics."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If standalone Then src = task.depthRGB
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        'Dim labels As cv.Mat
        'cv.Cv2.DistanceTransformWithLabels(src, dst0, labels, cv.DistanceTypes.L2, cv.DistanceTransformMasks.Precise)
        'Dim dist32f = dst0.Normalize(0, 255, cv.NormTypes.MinMax)
        'dist32f.ConvertTo(src, cv.MatType.CV_8UC1)
        'dst2 = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class






Public Class Distance_Foreground : Inherits VB_Algorithm
    Dim dist As New Distance_Basics
    Dim foreground As New Foreground_KMeans2
    Public useBackgroundAsInput As Boolean
    Public Sub New()
        labels(2) = "Distance results"
        labels(3) = "Input mask to distance transformm"
        desc = "Distance algorithm basics."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static cRadio = findRadio("C")
        Static l1Radio = findRadio("L1")

        foreground.Run(src)
        dst3 = If(useBackgroundAsInput, foreground.dst2, foreground.dst3)

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim DistanceType = cv.DistanceTypes.L2
        If cRadio.Checked Then DistanceType = cv.DistanceTypes.C
        If l1Radio.Checked Then DistanceType = cv.DistanceTypes.L1

        src = dst3 And src
        Dim kernelSize = 0 ' this is precise distance (there is no distance of 1)

        Dim dist = src.DistanceTransform(DistanceType, kernelSize)
        Dim dist32f = dist.Normalize(0, 255, cv.NormTypes.MinMax)
        dist32f.ConvertTo(src, cv.MatType.CV_8UC1)
        dst2 = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class







Public Class Distance_Background : Inherits VB_Algorithm
    Dim dist As New Distance_Foreground
    Public Sub New()
        dist.useBackgroundAsInput = True
        desc = "Use distance algorithm on the background"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        dist.Run(src)
        dst2 = dist.dst2
        dst3 = dist.dst3
        labels(2) = dist.labels(2)
        labels(3) = dist.labels(3)
    End Sub
End Class






Public Class Distance_Point3D : Inherits VB_Algorithm
    Public inPoint1 As cv.Point3f
    Public inPoint2 As cv.Point3f
    Public distance As Single
    Public Sub New()
        desc = "Compute the distance in meters between 3D points in the point cloud"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If standalone And heartBeat() Then
            inPoint1 = New cv.Point3f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height), msRNG.Next(0, 10000))
            inPoint2 = New cv.Point3f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height), msRNG.Next(0, 10000))

            dst2.SetTo(0)
            Dim p1 = New cv.Point(inPoint1.X, inPoint1.Y)
            Dim p2 = New cv.Point(inPoint2.X, inPoint2.Y)
            dst2.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)

            Dim vec1 = task.pointCloud.Get(Of cv.Point3f)(p1.Y, p1.X)
            Dim vec2 = task.pointCloud.Get(Of cv.Point3f)(p2.Y, p2.X)
        End If

        Dim x = inPoint1.X - inPoint2.X
        Dim y = inPoint1.Y - inPoint2.Y
        Dim z = inPoint1.Z - inPoint2.Z
        distance = Math.Sqrt(x * x + y * y + z * z)

        strOut = Format(inPoint1.X, fmt3) + ", " + Format(inPoint1.Y, fmt3) + ", " + Format(inPoint1.Z, fmt3) + vbCrLf
        strOut += Format(inPoint2.X, fmt3) + ", " + Format(inPoint2.Y, fmt3) + ", " + Format(inPoint2.Z, fmt3) + vbCrLf
        strOut += "Distance = " + Format(distance, fmt3)
        setTrueText(strOut, 3)
    End Sub
End Class






Public Class Distance_Point4D : Inherits VB_Algorithm
    Public inPoint1 As cv.Vec4f
    Public inPoint2 As cv.Vec4f
    Public distance As Single
    Public Sub New()
        desc = "Compute the distance between 4D points"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            inPoint1 = New cv.Vec4f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height),
                                    msRNG.Next(0, task.maxZmeters), msRNG.Next(0, task.maxZmeters))
            inPoint2 = New cv.Vec4f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height),
                                    msRNG.Next(0, task.maxZmeters), msRNG.Next(0, task.maxZmeters))
        End If

        Dim x = inPoint1(0) - inPoint2(0)
        Dim y = inPoint1(1) - inPoint2(1)
        Dim z = inPoint1(2) - inPoint2(2)
        Dim d = inPoint1(3) - inPoint2(3)
        distance = Math.Sqrt(x * x + y * y + z * z + d * d)

        strOut = inPoint1.ToString + vbCrLf + inPoint2.ToString + vbCrLf + "Distance = " + Format(distance, fmt1)
        setTrueText(strOut, New cv.Point(10, 10), 2)
    End Sub
End Class









Public Class Distance_Threshold : Inherits VB_Algorithm
    Dim accum As New Edge_MotionAccum
    Dim dist As New Distance_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold distance", 0, 100, 20)
        desc = "Find the top pixels in the distance algorithm."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = findSlider("Threshold distance")
        Dim testSlider = thresholdSlider.value

        accum.Run(src)

        dist.Run(Not accum.dst2)
        dst2 = dist.dst2
        dst3 = dst2.Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class







Public Class Distance_RedMin : Inherits VB_Algorithm
    Dim rMin As New RedMin_Basics
    Dim hColor As New Hist3Dcolor_Basics
    Public pixelVector As New List(Of List(Of Single))
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        redOptions.HistBinSlider.Value = 5
        labels(3) = "3D Histogram distance for each of the cells at left"
        desc = "Identify RedMin cells using each cell's 3D histogram distance from zero"
    End Sub
    Private Function distanceFromZero(histlist As List(Of Single)) As Double
        Dim result As Double
        For Each d In histlist
            result += d * d
        Next
        Return Math.Sqrt(result)
    End Function
    Public Sub RunVB(src As cv.Mat)
        rMin.Run(src)

        Static distances As New SortedList(Of Double, Integer)(New compareAllowIdenticalDoubleInverted)
        Static lastDistances As New SortedList(Of Double, Integer)(New compareAllowIdenticalDoubleInverted)
        Static lastMinCells As New List(Of segCell)
        pixelVector.Clear()
        distances.Clear()
        For i = 0 To rMin.minCells.Count - 1
            Dim rp = rMin.minCells(i)
            hColor.inputMask = rp.mask
            hColor.Run(src(rp.rect))

            Dim nextD = distanceFromZero(hColor.histArray.ToList)
            distances.Add(nextD, i)
        Next

        If heartBeat() Then
            strOut = "3D histogram distances from zero for each cell" + vbCrLf
            Dim index As Integer
            For Each el In distances
                strOut += "(" + CStr(el.Value) + ") "
                strOut += Format(el.Key, fmt1) + vbTab
                If index Mod 6 = 5 Then strOut += vbCrLf
                index += 1

                Dim rp = rMin.minCells(el.Value)
                setTrueText(CStr(el.Value), rp.maxDist)
            Next

            strOut += "----------------------" + vbCrLf
            index = 0
            For Each el In lastDistances
                strOut += "(" + CStr(el.Value) + ") "
                strOut += Format(el.Key, fmt1) + vbTab
                If index Mod 6 = 5 Then strOut += vbCrLf
                index += 1
                Dim rp = lastMinCells(el.Value)
                setTrueText(el.Value, New cv.Point(rp.maxDist.X, rp.maxDist.Y + 10))
            Next

            For Each el In distances
                Dim rp = rMin.minCells(el.Value)
                setTrueText(CStr(el.Value), rp.maxDist)
            Next
        End If

        For Each el In lastDistances
            Dim rp = lastMinCells(el.Value)
            setTrueText(el.Value, New cv.Point(rp.maxDist.X, rp.maxDist.Y + 10))
        Next

        setTrueText(strOut, 1)

        dst2.SetTo(0)
        dst3.SetTo(0)
        For i = 0 To distances.Count - 1
            Dim rp = rMin.minCells(distances.ElementAt(i).Value)
            task.color(rp.rect).CopyTo(dst2(rp.rect), rp.mask)
            dst3(rp.rect).SetTo(task.scalarColors(i), rp.mask)
        Next
        labels(2) = rMin.labels(3)

        lastDistances.Clear()
        For Each el In distances
            lastDistances.Add(el.Key, el.Value)
        Next

        lastMinCells = New List(Of segCell)(rMin.minCells)
    End Sub
End Class





Public Class Distance_D3Cells : Inherits VB_Algorithm
    Dim rMin As New RedMin_Basics
    Dim hColor As New Hist3Dcolor_Basics
    Dim valleys As New HistValley_Basics
    Public d3Cells As New List(Of segCell)
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        redOptions.HistBinSlider.Value = 5
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(3) = "CV_8U format of the backprojected cells - before vbPalette."
        desc = "Experiment that failed - backprojecting each cell from RedMin_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rMin.Run(src)

        d3Cells.Clear()
        For i = 0 To rMin.minCells.Count - 1
            Dim rm As New segCell
            Dim rp = rMin.minCells(i)
            rm.mask = rp.mask
            rm.rect = rp.rect
            rm.index = i + 1

            hColor.inputMask = rp.mask
            hColor.Run(src(rp.rect))
            rm.histogram = hColor.histogram.Clone
            rm.histList = hColor.histArray.ToList

            d3Cells.Add(rm)
        Next

        Dim tmp As New cv.Mat
        dst3.SetTo(0)
        For Each rm In d3Cells
            For i = 0 To rm.histList.Count - 1
                If rm.histList(i) <> 0 Then rm.histList(i) = rm.index
            Next
            Marshal.Copy(rm.histList.ToArray, 0, rm.histogram.Data, rm.histList.Count)

            cv.Cv2.CalcBackProject({src(rm.rect)}, {0, 1, 2}, rm.histogram, tmp, redOptions.rangesBGR)
            tmp.CopyTo(dst3(rm.rect), rm.mask)
        Next
        dst2 = vbPalette(dst3 * 255 / d3Cells.Count)

        labels(2) = rMin.labels(3)
    End Sub
End Class