Imports cv = OpenCvSharp
Imports System.Threading
Imports OpenCvSharp
'https://github.com/oreillymedia/Learning-OpenCV-3_examples/blob/master/example_14-03.cpp
Public Class CComp_Basics : Inherits VB_Algorithm
    Public connectedComponents As Object
    Public rects As New List(Of cv.Rect)
    Public centroids As New List(Of cv.Point2f)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("CComp threshold", 0, 255, 50)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(2) = "Input to ConnectedComponenetsEx"
        desc = "Draw bounding boxes around BGR binarized connected Components"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = findSlider("CComp threshold")

        Static lastImage As cv.Mat
        rects.Clear()
        centroids.Clear()

        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        dst2 = input.Threshold(thresholdSlider.Value, 255, cv.ThresholdTypes.BinaryInv) '  + cv.ThresholdTypes.Otsu

        connectedComponents = cv.Cv2.ConnectedComponentsEx(dst2)
        connectedComponents.renderblobs(dst3)

        Dim count As Integer = 0
        For Each blob In connectedComponents.Blobs
            Dim rect = validateRect(blob.Rect)
            Dim m = cv.Cv2.Moments(dst2(rect), True)
            If m.M00 = 0 Then Continue For ' avoid divide by zero...
            rects.Add(rect)
            centroids.Add(New cv.Point(CInt(m.M10 / m.M00 + rect.X), CInt(m.M01 / m.M00 + rect.Y)))
            count += 1
        Next

        lastImage = dst2
        labels(3) = CStr(count) + " items found "
    End Sub
End Class










' https://www.csharpcodi.com/csharp-examples/OpenCvSharp.ConnectedComponents.RenderBlobs(OpenCvSharp.Mat)/
Public Class CComp_Shapes : Inherits VB_Algorithm
    Dim shapes As cv.Mat
    Dim mats As New Mat_4Click
    Public Sub New()
        shapes = New cv.Mat(task.homeDir + "Data/Shapes.png", cv.ImreadModes.Color)
        labels(2) = "Largest connected component"
        labels(3) = "RectView, LabelView, Binary, grayscale"
        desc = "Use connected components to isolate objects in image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim gray = shapes.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim binary = gray.Threshold(0, 255, cv.ThresholdTypes.Otsu + cv.ThresholdTypes.Binary)
        Dim labelview = shapes.EmptyClone()
        Dim rectView = binary.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim cc = cv.Cv2.ConnectedComponentsEx(binary)
        If cc.LabelCount <= 1 Then Exit Sub

        cc.RenderBlobs(labelview)
        For Each blob In cc.Blobs.Skip(1)
            rectView.Rectangle(blob.Rect, cv.Scalar.Red, 2)
        Next

        Dim maxBlob = cc.GetLargestBlob()
        Dim filtered = New cv.Mat
        cc.FilterByBlob(shapes, filtered, maxBlob)
        ' dst3 = filtered.Resize(dst2.Size())

        mats.mat(0) = rectView
        mats.mat(1) = labelview
        mats.mat(2) = binary
        mats.mat(3) = gray
        mats.Run(Nothing)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class






'https://github.com/oreillymedia/Learning-OpenCV-3_examples/blob/master/example_14-03.cpp
Public Class CComp_Both : Inherits VB_Algorithm
    Dim above As New CComp_Stats
    Dim below As New CComp_Stats
    Public Sub New()
        labels = {"", "", "Connected components in both the lighter and darker halves", "Connected components in the darker half of the image"}
        desc = "Prepare the connected components for both above and below the threshold"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        above.options.RunVB()

        Dim light = src.Threshold(above.options.light, 255, cv.ThresholdTypes.Binary)
        below.Run(light)
        dst2 = below.dst3
        dst1 = below.dst1
        labels(3) = above.labels(3)

        'Dim dark = src.Threshold(above.options.dark, 255, cv.ThresholdTypes.Binary)
        'above.Run(dark)
        'dst3 = above.dst3
        'dst1 += (above.dst1 + below.numberOfLabels)
        'dst2 += dst3
        'labels(2) = above.labels(3)
    End Sub
End Class







Public Class CComp_Hulls : Inherits VB_Algorithm
    Dim ccomp As New CComp_Both
    Dim hulls As New RedCloud_Hulls
    Public Sub New()
        desc = "Create connected components using RedCloud Hulls"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        ccomp.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst2 = ccomp.dst3
        ccomp.dst1.ConvertTo(dst1, cv.MatType.CV_8U)
        hulls.Run(dst1)
        dst2 = hulls.dst3
        labels(2) = hulls.labels(3)
    End Sub
End Class







' https://docs.opencv.org/master/de/d01/samples_2cpp_2connected_components_8cpp-example.html
Public Class CComp_Stats : Inherits VB_Algorithm
    Public masks As New List(Of cv.Mat)
    Public rects As New List(Of cv.Rect)
    Public areas As New List(Of Integer)
    Public centroids As New List(Of cv.Point)
    Dim colorMap As cv.Mat
    Public numberOfLabels As Integer
    Public options As New Options_CComp
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Use a threshold slider on the CComp input"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If firstPass Then
            task.palette.Run(src)
            colorMap = task.palette.gradientColorMap.Row(0).Clone
        End If

        options.RunVB()

        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If standalone Then src = src.Threshold(options.light, 255, cv.ThresholdTypes.BinaryInv)

        Dim stats As New cv.Mat
        Dim centroidRaw As New cv.Mat
        dst2 = src
        numberOfLabels = src.ConnectedComponentsWithStats(dst1, stats, centroidRaw)

        rects.Clear()
        areas.Clear()
        centroids.Clear()

        Dim colors As New List(Of cv.Vec3b)
        Dim maskOrder As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
        Dim unsortedMasks As New List(Of cv.Mat)
        Dim unsortedRects As New List(Of cv.Rect)
        Dim unsortedCentroids As New List(Of cv.Point)
        Dim index As New List(Of Integer)

        For i = 0 To Math.Min(256, stats.Rows) - 1
            Dim area = stats.Get(Of Integer)(i, 4)
            Dim r1 = validateRect(stats.Get(Of cv.Rect)(i, 0))
            Dim r = validateRect(New cv.Rect(r1.X, r1.Y, r1.Width, r1.Height))
            If (r.Width = dst2.Width And r.Height = dst2.Height) Or (r.Width = 1 And r.Height = 1) Then Continue For
            areas.Add(area)
            unsortedRects.Add(r)
            index.Add(i)
            colors.Add(task.vecColors(colors.Count))
            maskOrder.Add(area, unsortedMasks.Count)
            unsortedMasks.Add(dst1.InRange(i, i)(r))
            Dim c = New cv.Point(CInt(centroidRaw.Get(Of Double)(i, 0)), CInt(centroidRaw.Get(Of Double)(i, 1)))
            unsortedCentroids.Add(c)
        Next

        masks.Clear()
        For i = 0 To maskOrder.Count - 1
            Dim mIndex = maskOrder.ElementAt(i).Value
            masks.Add(unsortedMasks(mIndex))
            rects.Add(unsortedRects(mIndex))
            centroids.Add(unsortedCentroids(mIndex))
        Next

        ' this does not fix the color flashing problem but if the component count is the same (for the same areas) the colors will be stable.
        task.palette.gradientColorMap = colorMap.Clone
        For i = 0 To colors.Count - 1
            task.palette.gradientColorMap.Set(Of cv.Vec3b)(0, index(i), colors(i))
        Next

        dst1.ConvertTo(dst0, cv.MatType.CV_8U)
        dst3 = vbPalette(dst0)
        labels(3) = CStr(masks.Count) + " Connected Components"
    End Sub
End Class