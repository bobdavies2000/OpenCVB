Imports  cv = OpenCvSharp
Public Class ImageSeg_Basics : Inherits VBparent
    Dim addw As New AddWeighted_Basics

    Public maskSizes As New SortedList(Of Integer, Integer)(New CompareMaskSize)
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Public floodPoints As New List(Of cv.Point)

    Public flood As New FloodFill_FullImage
    Public Sub New()
        task.desc = "Get the image segments and their associated features - centroids, masks, size, and enclosing rectangles"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        flood.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst2 = flood.dst3

        maskSizes = New SortedList(Of Integer, Integer)(flood.maskSizes)
        rects = New List(Of cv.Rect)(flood.rects)
        masks = New List(Of cv.Mat)(flood.masks)
        centroids = New List(Of cv.Point2f)(flood.centroids)
        floodPoints = New List(Of cv.Point)(flood.floodPoints)

        addw.src2 = src
        addw.Run(dst2)
        dst3 = addw.dst2

        For Each pt In floodPoints
            dst2.Circle(pt, task.dotSize + 2, cv.Scalar.Yellow, -1, task.lineType)
        Next

        labels(3) = addw.labels(2).Replace("depth", "ImageSeg")
    End Sub
End Class







Public Class ImageSeg_InRange : Inherits VBparent
    Dim iSeg As New ImageSeg_Basics
    Public Sub New()
        task.desc = "Trim segments that are not in the range requested"
    End Sub
    Public Sub RunVB(src As cv.Mat)

        iSeg.Run(src)
        dst2 = iSeg.dst3

        For i = 0 To iSeg.maskSizes.Count - 1
            Dim mask = iSeg.masks(i)
            Dim r = iSeg.rects(i)
            Dim meanDepth = task.depth32f(r).Mean(mask)
            If meanDepth.Val0 >= task.maxDepth Then dst2(r).SetTo(0, mask)
            If meanDepth.Val0 <= task.minDepth Then dst2(r).SetTo(0, mask)
        Next
    End Sub
End Class








Public Class ImageSeg_MissingSegments : Inherits VBparent
    Public flood As New FloodFill_FullImage
    Public Sub New()
        task.desc = "Floodfill segments which were marked as missing and clear small unused segments"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static lenContourSlider = findSlider("Minimum length for missing contours")
        Static stepSlider = findSlider("FloodFill Step Size")
        Static fillSlider = findSlider("FloodFill point distance from edge")
        Dim maxLen = lenContourSlider.value
        Dim fill = fillSlider.value
        Dim stepSize = stepSlider.Value

        Static saveStepSize As Integer
        Static saveFillDistance As Integer
        Dim resetColors As Boolean
        If saveStepSize <> stepSize Or saveFillDistance <> fill Then
            resetColors = True
            saveStepSize = stepSize
            saveFillDistance = fill
        End If

        flood.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst2 = flood.dst3

        dst3 = flood.missingSegments
        Dim tmp As New cv.Mat
        flood.missingSegments.ConvertTo(tmp, cv.MatType.CV_32SC1)
        Dim contours0 = cv.Cv2.FindContoursAsArray(tmp, cv.RetrievalModes.FloodFill, cv.ContourApproximationModes.ApproxSimple)
        Dim contours As New List(Of cv.Point())
        For i = 0 To contours0.Length - 1
            Dim nextContour = cv.Cv2.ApproxPolyDP(contours0(i), 3, True)

            If nextContour.Length >= maxLen Then contours.Add(nextContour)
        Next
        cv.Cv2.DrawContours(dst3, contours.ToArray, -1, 128, -1, task.lineType)
        labels(3) = CStr(contours.Count) + " contours were found "
    End Sub
End Class








Public Class ImageSeg_Unstable : Inherits VBparent
    Dim iSeg As New ImageSeg_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar(0, "A segment is considered present after this many appearances", 1, 40, 20)
        End If

        task.desc = "Find the unstable segments and remove them"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static segSlider = findSlider("A segment is considered present after this many appearances")
        Dim refreshCount = segSlider.value

        iSeg.Run(src)
        dst2 = iSeg.dst2

        Dim tmp = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Static previousFrame = tmp

        If task.frameCount Mod refreshCount = 0 Then previousFrame = tmp
        If previousFrame.channels <> 1 Then previousFrame = previousFrame.cvtcolor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.Min(tmp, previousFrame, dst3)
        previousFrame = dst3

        task.palette.Run(dst3)
        dst3 = task.palette.dst2
        dst3.SetTo(0, iSeg.flood.mats.mat(1))
    End Sub
End Class







Public Class ImageSeg_CentroidTracker : Inherits VBparent
    Public iSeg As New ImageSeg_Basics
    Public pTrack As New KNN_PointTracker
    Public Sub New()
        findCheckBox("traceName will handle any drawing required").Checked = True

        labels(2) = "Output of ImageSeg_Basics"
        task.desc = "Track the centroids that are found consistently from frame to frame."
    End Sub
    Public Sub RunVB(src As cv.Mat)

        iSeg.Run(src)
        dst2 = iSeg.dst2

        Dim tmp = If(iSeg.flood.dst2.Channels = 3, iSeg.flood.dst2, iSeg.flood.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
        pTrack.queryPoints = New List(Of cv.Point2f)(iSeg.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(iSeg.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(iSeg.masks)
        pTrack.floodPoints = New List(Of cv.Point)(iSeg.floodPoints)
        pTrack.Run(tmp)
        dst3 = dst2.Clone
        For Each vo In pTrack.drawRC.viewObjects
            dst3.Circle(vo.Value.centroid, task.dotSize + 1, cv.Scalar.White, -1, task.lineType)
            dst3.Circle(vo.Value.centroid, task.dotSize, cv.Scalar.Blue, -1, task.lineType)
            dst3.Line(vo.Value.floodPoint, vo.Value.centroid, cv.Scalar.White, task.lineWidth, task.lineType)
        Next

        For Each pt In pTrack.floodPoints
            dst3.Circle(pt, task.dotSize + 2, cv.Scalar.Yellow, -1, task.lineType)
        Next
        labels(3) = "Centroid " + CStr(pTrack.drawRC.viewObjects.Count) + " blue, floodPoint " + CStr(pTrack.floodPoints.Count) + " yellow"
    End Sub
End Class
