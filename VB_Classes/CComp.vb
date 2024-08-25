Imports OpenCvSharp
Imports cvb = OpenCvSharp
'https://github.com/oreillymedia/Learning-OpenCV-3_examples/blob/master/example_14-03.cpp
Public Class CComp_Basics : Inherits VB_Parent
    Public connectedComponents As ConnectedComponents
    Public rects As New List(Of cvb.Rect)
    Public centroids As New List(Of cvb.Point2f)
    Dim lastImage As cvb.Mat
    Dim options As New Options_CComp
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        UpdateAdvice(traceName + ": only the local options for threshold is used in CComp_Basics.")
        labels(2) = "Input to ConnectedComponenetsEx"
        desc = "Draw bounding boxes around BGR binarized connected Components"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        options.RunVB()

        rects.Clear()
        centroids.Clear()

        Dim input = src
        If input.Channels() = 3 Then input = input.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        dst2 = input.Threshold(options.threshold, 255, cvb.ThresholdTypes.BinaryInv) '  + cvb.ThresholdTypes.Otsu

        connectedComponents = cvb.Cv2.ConnectedComponentsEx(dst2)
        connectedComponents.renderblobs(dst3)

        Dim count As Integer = 0
        For Each blob In connectedComponents.Blobs
            Dim rect = ValidateRect(blob.Rect)
            Dim m = cvb.Cv2.Moments(dst2(rect), True)
            If m.M00 = 0 Then Continue For ' avoid divide by zero...
            rects.Add(rect)
            centroids.Add(New cvb.Point(CInt(m.M10 / m.M00 + rect.X), CInt(m.M01 / m.M00 + rect.Y)))
            count += 1
        Next

        lastImage = dst2
        labels(3) = CStr(count) + " items found "
    End Sub
End Class









' https://www.csharpcodi.com/csharp-examples/OpenCvSharp.ConnectedComponents.RenderBlobs(OpenCvSharp.Mat)/
Public Class CComp_Shapes : Inherits VB_Parent
    Dim shapes As cvb.Mat
    Dim mats As New Mat_4Click
    Public Sub New()
        shapes = New cvb.Mat(task.HomeDir + "Data/Shapes.png", cvb.ImreadModes.Color)
        labels(2) = "Largest connected component"
        labels(3) = "RectView, LabelView, Binary, grayscale"
        desc = "Use connected components to isolate objects in image."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        Dim gray = shapes.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Dim binary = gray.Threshold(0, 255, cvb.ThresholdTypes.Otsu + cvb.ThresholdTypes.Binary)
        Dim labelview = shapes.EmptyClone()
        Dim rectView = binary.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        Dim cc = cvb.Cv2.ConnectedComponentsEx(binary)
        If cc.LabelCount <= 1 Then Exit Sub

        cc.RenderBlobs(labelview)
        For Each blob In cc.Blobs.Skip(1)
            rectView.Rectangle(blob.Rect, cvb.Scalar.Red, 2)
        Next

        Dim maxBlob = cc.GetLargestBlob()
        Dim filtered = New cvb.Mat
        cc.FilterByBlob(shapes, filtered, maxBlob)
        ' dst3 = filtered.Resize(dst2.Size())

        mats.mat(0) = rectView
        mats.mat(1) = labelview
        mats.mat(2) = binary
        mats.mat(3) = gray
        mats.Run(empty)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class






'https://github.com/oreillymedia/Learning-OpenCV-3_examples/blob/master/example_14-03.cpp
Public Class CComp_Both : Inherits VB_Parent
    Dim above As New CComp_Stats
    Dim below As New CComp_Stats
    Public Sub New()
        labels = {"", "", "Connected components in both the lighter and darker halves", "Connected components in the darker half of the image"}
        desc = "Prepare the connected components for both above and below the threshold"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        above.options.RunVB()

        Dim light = src.Threshold(above.options.light, 255, cvb.ThresholdTypes.Binary)
        below.Run(light)
        dst2 = below.dst3
        dst1 = below.dst1
        labels(3) = above.labels(3)

        'Dim dark = src.Threshold(above.options.dark, 255, cvb.ThresholdTypes.Binary)
        'above.Run(dark)
        'dst3 = above.dst3
        'dst1 += (above.dst1 + below.numberOfLabels)
        'dst2 += dst3
        'labels(2) = above.labels(3)
    End Sub
End Class







Public Class CComp_Hulls : Inherits VB_Parent
    Dim ccomp As New CComp_Both
    Dim hulls As New RedCloud_Hulls
    Public Sub New()
        desc = "Create connected components using RedCloud Hulls"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        ccomp.Run(src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY))
        dst2 = ccomp.dst3
        ccomp.dst1.ConvertTo(dst1, cvb.MatType.CV_8U)
        hulls.Run(dst1)
        dst2 = hulls.dst3
        labels(2) = hulls.labels(3)
    End Sub
End Class







' https://docs.opencvb.org/master/de/d01/samples_2cpp_2connected_components_8cpp-example.html
Public Class CComp_Stats : Inherits VB_Parent
    Public masks As New List(Of cvb.Mat)
    Public rects As New List(Of cvb.Rect)
    Public areas As New List(Of Integer)
    Public centroids As New List(Of cvb.Point)
    Public numberOfLabels As Integer
    Public options As New Options_CComp
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Use a threshold slider on the CComp input"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        dst2 = src
        options.RunVB()

        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        If standaloneTest() Then src = src.Threshold(options.light, 255, cvb.ThresholdTypes.BinaryInv)

        Dim stats As New cvb.Mat
        Dim centroidRaw As New cvb.Mat
        numberOfLabels = src.ConnectedComponentsWithStats(dst1, stats, centroidRaw)

        rects.Clear()
        areas.Clear()
        centroids.Clear()

        Dim colors As New List(Of cvb.Vec3b)
        Dim maskOrder As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
        Dim unsortedMasks As New List(Of cvb.Mat)
        Dim unsortedRects As New List(Of cvb.Rect)
        Dim unsortedCentroids As New List(Of cvb.Point)
        Dim index As New List(Of Integer)

        For i = 0 To Math.Min(256, stats.Rows) - 1
            Dim area = stats.Get(Of Integer)(i, 4)
            If area < 10 Then Continue For
            Dim r1 = ValidateRect(stats.Get(Of cvb.Rect)(i, 0))
            Dim r = ValidateRect(New cvb.Rect(r1.X, r1.Y, r1.Width, r1.Height))
            If (r.Width = dst2.Width And r.Height = dst2.Height) Or (r.Width = 1 And r.Height = 1) Then Continue For
            areas.Add(area)
            unsortedRects.Add(r)
            dst2.Rectangle(r, task.HighlightColor, task.lineWidth)
            index.Add(i)
            colors.Add(task.vecColors(colors.Count))
            maskOrder.Add(area, unsortedMasks.Count)
            unsortedMasks.Add(dst1.InRange(i, i)(r))
            Dim c = New cvb.Point(CInt(centroidRaw.Get(Of Double)(i, 0)), CInt(centroidRaw.Get(Of Double)(i, 1)))
            unsortedCentroids.Add(c)
        Next

        masks.Clear()
        For i = 0 To maskOrder.Count - 1
            Dim mIndex = maskOrder.ElementAt(i).Value
            masks.Add(unsortedMasks(mIndex))
            rects.Add(unsortedRects(mIndex))
            centroids.Add(unsortedCentroids(mIndex))
        Next

        dst1.ConvertTo(dst0, cvb.MatType.CV_8U)
        dst3 = ShowPalette(dst0 * 255 / centroids.Count)
        labels(3) = CStr(masks.Count) + " Connected Components"
    End Sub
End Class