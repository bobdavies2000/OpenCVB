Imports cvb = OpenCvSharp
Public Class Distance_Basics : Inherits TaskParent
    Dim options As New Options_Distance
    Public Sub New()
        labels = {"", "", "Distance transform - create a mask with threshold", ""}
        UpdateAdvice(traceName + ": use local options to control which method is used.")
        desc = "Distance algorithm basics."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If standaloneTest() Then src = task.depthRGB
        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        dst0 = src.DistanceTransform(options.distanceType, 0)
        dst1 = Convert32f_To_8UC3(dst0)
        dst1.ConvertTo(dst2, cvb.MatType.CV_8UC1)
    End Sub
End Class






Public Class Distance_Labels : Inherits TaskParent
    Dim options As New Options_Distance
    Public Sub New()
        labels(2) = "Distance results"
        labels(3) = "Input mask to distance transformm"
        desc = "Distance algorithm basics."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If standaloneTest() Then src = task.depthRGB
        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        'Dim labels As cvb.Mat
        'cvb.Cv2.DistanceTransformWithLabels(src, dst0, labels, cvb.DistanceTypes.L2, cvb.DistanceTransformMasks.Precise)
        'Dim dist32f = dst0.Normalize(0, 255, cvb.NormTypes.MinMax)
        'dist32f.ConvertTo(src, cvb.MatType.CV_8UC1)
        'dst2 = src.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class






Public Class Distance_Foreground : Inherits TaskParent
    Dim dist As New Distance_Basics
    Dim foreground As New Foreground_KMeans
    Public useBackgroundAsInput As Boolean
    Public Sub New()
        labels(2) = "Distance results"
        labels(3) = "Input mask to distance transformm"
        desc = "Distance algorithm basics."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static cRadio = FindRadio("C")
        Static l1Radio = FindRadio("L1")

        foreground.Run(src)
        dst3 = If(useBackgroundAsInput, foreground.dst2, foreground.dst3)

        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Dim DistanceType = cvb.DistanceTypes.L2
        If cRadio.Checked Then DistanceType = cvb.DistanceTypes.C
        If l1Radio.Checked Then DistanceType = cvb.DistanceTypes.L1

        src = dst3 And src
        Dim dist = src.DistanceTransform(DistanceType, cvb.DistanceTransformMasks.Precise)
        Dim dist32f = dist.Normalize(0, 255, cvb.NormTypes.MinMax)
        dist32f.ConvertTo(src, cvb.MatType.CV_8UC1)
        dst2 = src.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class







Public Class Distance_Background : Inherits TaskParent
    Dim dist As New Distance_Foreground
    Public Sub New()
        dist.useBackgroundAsInput = True
        desc = "Use distance algorithm on the background"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dist.Run(src)
        dst2 = dist.dst2
        dst3 = dist.dst3
        labels(2) = dist.labels(2)
        labels(3) = dist.labels(3)
    End Sub
End Class






Public Class Distance_Point3D : Inherits TaskParent
    Public inPoint1 As cvb.Point3f
    Public inPoint2 As cvb.Point3f
    Public distance As Single
    Public Sub New()
        desc = "Compute the distance in meters between 3D points in the point cloud"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() And task.heartBeat Then
            inPoint1 = New cvb.Point3f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height), msRNG.Next(0, 10000))
            inPoint2 = New cvb.Point3f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height), msRNG.Next(0, 10000))

            dst2.SetTo(0)
            Dim p1 = New cvb.Point(inPoint1.X, inPoint1.Y)
            Dim p2 = New cvb.Point(inPoint2.X, inPoint2.Y)
            DrawLine(dst2, p1, p2, task.HighlightColor)

            Dim vec1 = task.pointCloud.Get(Of cvb.Point3f)(p1.Y, p1.X)
            Dim vec2 = task.pointCloud.Get(Of cvb.Point3f)(p2.Y, p2.X)
        End If

        Dim x = inPoint1.X - inPoint2.X
        Dim y = inPoint1.Y - inPoint2.Y
        Dim z = inPoint1.Z - inPoint2.Z
        distance = Math.Sqrt(x * x + y * y + z * z)

        strOut = Format(inPoint1.X, fmt3) + ", " + Format(inPoint1.Y, fmt3) + ", " + Format(inPoint1.Z, fmt3) + vbCrLf
        strOut += Format(inPoint2.X, fmt3) + ", " + Format(inPoint2.Y, fmt3) + ", " + Format(inPoint2.Z, fmt3) + vbCrLf
        strOut += "Distance = " + Format(distance, fmt3)
        SetTrueText(strOut, 3)
    End Sub
End Class






Public Class Distance_Point4D : Inherits TaskParent
    Public inPoint1 As cvb.Vec4f
    Public inPoint2 As cvb.Vec4f
    Public distance As Single
    Public Sub New()
        desc = "Compute the distance between 4D points"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            inPoint1 = New cvb.Vec4f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height),
                                    msRNG.Next(0, task.MaxZmeters), msRNG.Next(0, task.MaxZmeters))
            inPoint2 = New cvb.Vec4f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height),
                                    msRNG.Next(0, task.MaxZmeters), msRNG.Next(0, task.MaxZmeters))
        End If

        Dim x = inPoint1(0) - inPoint2(0)
        Dim y = inPoint1(1) - inPoint2(1)
        Dim z = inPoint1(2) - inPoint2(2)
        Dim d = inPoint1(3) - inPoint2(3)
        distance = Math.Sqrt(x * x + y * y + z * z + d * d)

        strOut = inPoint1.ToString + vbCrLf + inPoint2.ToString + vbCrLf + "Distance = " + Format(distance, fmt1)
        SetTrueText(strOut, New cvb.Point(10, 10), 2)
    End Sub
End Class








Public Class Distance_RedCloud : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Dim hColor As New Hist3Dcolor_Basics
    Public pixelVector As New List(Of List(Of Single))
    Dim distances As New SortedList(Of Double, Integer)(New compareAllowIdenticalDoubleInverted)
    Dim lastDistances As New SortedList(Of Double, Integer)(New compareAllowIdenticalDoubleInverted)
    Dim lastredCells As New List(Of rcData)
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        task.redOptions.setUseColorOnly(True)
        task.redOptions.HistBinBar3D.Value = 5
        labels(1) = "3D Histogram distance for each of the cells at left"
        desc = "Identify RedCloud cells using the cell's 3D histogram distance from zero"
    End Sub
    Private Function distanceFromZero(histlist As List(Of Single)) As Double
        Dim result As Double
        For Each d In histlist
            result += d * d
        Next
        Return Math.Sqrt(result)
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)

        pixelVector.Clear()
        distances.Clear()
        For i = 0 To task.redCells.Count - 1
            Dim rc = task.redCells(i)
            hColor.inputMask = rc.mask
            hColor.Run(src(rc.rect))

            Dim nextD = distanceFromZero(hColor.histArray.ToList)
            distances.Add(nextD, i)
        Next

        If task.heartBeat Then
            strOut = "3D histogram distances from zero for each cell" + vbCrLf
            Dim index As Integer
            For Each el In distances
                strOut += "(" + CStr(el.Value) + ") "
                strOut += Format(el.Key, fmt1) + vbTab
                If index Mod 6 = 5 Then strOut += vbCrLf
                index += 1

                Dim rc = task.redCells(el.Value)
                SetTrueText(CStr(el.Value), rc.maxDist)
            Next

            strOut += "----------------------" + vbCrLf
            index = 0
            For Each el In lastDistances
                strOut += "(" + CStr(el.Value) + ") "
                strOut += Format(el.Key, fmt1) + vbTab
                If index Mod 6 = 5 Then strOut += vbCrLf
                index += 1
                Dim rc = lastredCells(el.Value)
                SetTrueText(el.Value, New cvb.Point(rc.maxDist.X, rc.maxDist.Y + 10))
            Next

            For Each el In distances
                Dim rc = task.redCells(el.Value)
                SetTrueText(CStr(el.Value), rc.maxDist)
            Next
        End If

        For Each el In lastDistances
            Dim rp = lastredCells(el.Value)
            SetTrueText(el.Value, New cvb.Point(rp.maxDist.X, rp.maxDist.Y + 10))
        Next

        SetTrueText(strOut, 1)

        dst2.SetTo(0)
        dst3.SetTo(0)
        For i = 0 To distances.Count - 1
            Dim rp = task.redCells(distances.ElementAt(i).Value)
            task.color(rp.rect).CopyTo(dst2(rp.rect), rp.mask)
            dst3(rp.rect).SetTo(task.scalarColors(i), rp.mask)
        Next
        labels(2) = redC.labels(3)

        lastDistances.Clear()
        For Each el In distances
            lastDistances.Add(el.Key, el.Value)
        Next

        lastredCells = New List(Of rcData)(task.redCells)
    End Sub
End Class





Public Class Distance_BinaryImage : Inherits TaskParent
    Dim binary As New Binarize_Simple
    Dim distance As New Distance_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Measure the fragmentation of a binary image by using the distance transform"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        binary.Run(src)
        dst2 = binary.dst2
        labels(2) = binary.labels(2) + " Draw a rectangle to measure specific area."

        If task.drawRect.Width > 0 Then
            distance.Run(dst2(task.drawRect))
        Else
            distance.Run(dst2)
        End If
        dst3 = distance.dst2
        dst1 = dst3.Threshold(task.gOptions.DebugSliderValue, 255, cvb.ThresholdTypes.Binary)
    End Sub
End Class
