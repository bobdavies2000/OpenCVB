Imports cv = OpenCvSharp
Public Class KMeans_Basics : Inherits VBparent
    Public kmeansK As Integer
    Public resizeFactor = 1 ' update this to 2 or 4 to speed up the kmeans performance.
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "kMeans k", 2, 32, 4)
        End If

        task.desc = "Cluster the rgb image pixels using kMeans."
    End Sub
    Public Sub Run(src As cv.Mat)
        Dim kInput = src.Resize(New cv.Size(CInt(src.Width / resizeFactor), CInt(src.Height / resizeFactor)))
        Dim columnVector = kInput.Reshape(src.Channels, kInput.Height * kInput.Width)
        Dim src32f As New cv.Mat
        columnVector.ConvertTo(src32f, cv.MatType.CV_32FC3)
        Static lastClusterCount As Integer
        If lastClusterCount <> sliders.trackbar(0).Value Then
            lastClusterCount = sliders.trackbar(0).Value
            kmeansK = lastClusterCount
        End If

        Dim labels = New cv.Mat()
        Dim colors As New cv.Mat
        cv.Cv2.Kmeans(src32f, kmeansK, labels, term, 1, cv.KMeansFlags.PpCenters, colors)
        labels.Reshape(1, kInput.Height).ConvertTo(labels, cv.MatType.CV_8U)
        labels = labels.Resize(New cv.Size(src.Width, src.Height))

        Dim centroids As New List(Of cv.Point)
        For i = 0 To kmeansK - 1
            Dim mask = labels.InRange(i, i)
            Dim m = cv.Cv2.Moments(mask, True)
            centroids.Add(New cv.Point2f(m.M10 / m.M00, m.M01 / m.M00))
            dst1.SetTo(task.scalarColors(i), mask)
        Next

        For i = 0 To centroids.Count - 1
            dst1.Circle(centroids(i), 10, cv.Scalar.Yellow, -1, task.lineType)
        Next
    End Sub
End Class







Public Class KMeans_BasicsDepthColor : Inherits VBparent
    Public kmeansK As Integer
    Public resizeRequest As Boolean = True
    Public useDepthColor As Boolean = True
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "kMeans k", 2, 32, 4)
        End If
        task.desc = "Cluster the rgb image pixels using kMeans."
    End Sub
    Public Sub Run(src As cv.Mat)
        Dim resizeVal = If(resizeRequest, 4, 1)
        Dim small = src.Resize(New cv.Size(src.Width / resizeVal, src.Height / resizeVal))
        Dim rectMat = small.Clone
        Dim columnVector As New cv.Mat
        columnVector = rectMat.Reshape(src.Channels, small.Height * small.Width)
        Dim rgb32f As New cv.Mat
        columnVector.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
        Static lastClusterCount As Integer
        If lastClusterCount <> sliders.trackbar(0).Value Then
            lastClusterCount = sliders.trackbar(0).Value
            kmeansK = lastClusterCount
        End If
        Dim labels = New cv.Mat()
        Dim colors As New cv.Mat

        cv.Cv2.Kmeans(rgb32f, kmeansK, labels, term, 1, cv.KMeansFlags.PpCenters, colors)
        labels.Reshape(1, small.Height).ConvertTo(labels, cv.MatType.CV_8U)
        labels = labels.Resize(New cv.Size(src.Width, src.Height))

        ' color the result with the mean depth value for each label k
        If useDepthColor Then
            For i = 0 To kmeansK - 1
                Dim mask = labels.InRange(i, i)
                Dim mean = task.RGBDepth.Mean(mask)
                dst1.SetTo(mean, mask)
            Next
        Else
            dst1 = labels ' they just want the labels.
        End If
    End Sub
End Class





Public Class KMeans_Clusters : Inherits VBparent
    Dim Mats As Mat_4to1
    Dim km As KMeans_BasicsDepthColor
    Public Sub New()
        Mats = New Mat_4to1()

        km = New KMeans_BasicsDepthColor()

        label1 = "kmeans - k=2,4,6,8"
        label2 = "Click any quadrant at left to view it below"
        task.desc = "Show clustering with various settings for cluster count.  Draw to select region of interest."
    End Sub
    Public Sub Run(src As cv.Mat)
        Static saveRect = task.drawRect
        task.drawRect = saveRect
        For i = 0 To 3
            km.kmeansK = Choose(i + 1, 2, 4, 6, 8)
            km.Run(src)
            Mats.mat(i) = km.dst1.Clone
        Next
        Mats.Run(Nothing)
        dst1 = Mats.dst1
        If task.mouseClickFlag And task.mousePicTag = RESULT1 Then setMyActiveMat()
        dst2 = Mats.mat(quadrantIndex)
    End Sub
End Class





Public Class KMeans_RGBFast : Inherits VBparent
    Public clusterColors() As cv.Vec3b
    Public resizeFactor = 2
    Public clusterCount = 6
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "kMeans k", 2, 32, 4)
        End If
        task.desc = "Cluster a small rgb image using kMeans.  Specify clusterCount value."
    End Sub
    Public Sub Run(src As cv.Mat)
        Dim small8uC3 = src.Resize(New cv.Size(CInt(src.Rows / resizeFactor), CInt(src.Cols / resizeFactor)))
        Dim columnVector As New cv.Mat
        columnVector = small8uC3.Reshape(small8uC3.Channels, small8uC3.Rows * small8uC3.Cols)
        Dim columnVectorRGB32f As New cv.Mat
        columnVector.ConvertTo(columnVectorRGB32f, cv.MatType.CV_32FC3)
        Dim labels = New cv.Mat()
        Dim centers As New cv.Mat
        Dim clusterCount = sliders.trackbar(0).Value

        cv.Cv2.Kmeans(columnVectorRGB32f, clusterCount, labels, term, 3, cv.KMeansFlags.PpCenters, centers)
        Dim labelImage = labels.Reshape(1, small8uC3.Rows)

        ReDim clusterColors(clusterCount - 1)
        For i = 0 To clusterCount - 1
            Dim c = centers.Get(Of cv.Vec3f)(i)
            clusterColors(i) = New cv.Vec3b(CInt(c(0)), CInt(c(1)), CInt(c(2)))
        Next
        For y = 0 To labelImage.Rows - 1
            For x = 0 To labelImage.Cols - 1
                Dim cIndex = labelImage.Get(Of Byte)(y, x)
                small8uC3.Set(Of cv.Vec3b)(y, x, clusterColors(cIndex))
            Next
        Next
        dst1 = small8uC3.Resize(dst1.Size())
    End Sub
End Class




Public Class KMeans_RGB_Plus_XYDepth : Inherits VBparent
    Dim km As KMeans_BasicsDepthColor
    Dim clusterColors() As cv.Vec6i
    Public Sub New()
        km = New KMeans_BasicsDepthColor()
        label1 = "kmeans - RGB, XY, and Depth Raw"
        task.desc = "Cluster with kMeans RGB, x, y, and depth."
    End Sub
    Public Sub Run(src As cv.Mat)
        km.Run(src) ' cluster the rgb image - output is in dst2
        Dim rgb32f As New cv.Mat
        km.dst1.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
        Dim xyDepth32f As New cv.Mat(rgb32f.Size(), cv.MatType.CV_32FC3, 0)
        For y = 0 To xyDepth32f.Rows - 1
            For x = 0 To xyDepth32f.Cols - 1
                Dim nextVal = task.depth32f.Get(Of Single)(y, x)
                If nextVal Then xyDepth32f.Set(Of cv.Vec3f)(y, x, New cv.Vec3f(x, y, nextVal))
            Next
        Next
        Dim img() = New cv.Mat() {rgb32f, xyDepth32f}
        Dim all32f = New cv.Mat(rgb32f.Size(), cv.MatType.CV_32FC(6)) ' output will have 6 channels!
        Dim mixed() = New cv.Mat() {all32f}
        Dim from_to() = New Integer() {0, 0, 0, 1, 0, 2, 3, 3, 4, 4, 5, 5}
        cv.Cv2.MixChannels(img, mixed, from_to)

        Dim columnVector As New cv.Mat
        columnVector = all32f.Reshape(all32f.Channels, all32f.Rows * all32f.Cols)
        Dim labels = New cv.Mat()
        Dim centers As New cv.Mat
        Dim clusterCount = km.sliders.trackbar(0).Value

        cv.Cv2.Kmeans(columnVector, clusterCount, labels, term, 3, cv.KMeansFlags.PpCenters, centers)
        Dim labelImage = labels.Reshape(1, all32f.Rows)

        ReDim clusterColors(clusterCount - 1)
        For i = 0 To clusterCount - 1
            Dim c = centers.Get(Of cv.Vec6f)(i)
            clusterColors(i) = New cv.Vec6i(CInt(c(0)), CInt(c(1)), CInt(c(2)), CInt(c(3)), CInt(c(4)), CInt(c(5)))
        Next
        For y = 0 To labelImage.Rows - 1
            For x = 0 To labelImage.Cols - 1
                Dim cIndex = labelImage.Get(Of Byte)(y, x)
                With clusterColors(cIndex)
                    dst1.Set(Of cv.Vec3b)(y, x, New cv.Vec3b(10 * .Item0 Mod 255, 10 * .Item1 Mod 255, 10 * .Item2 Mod 255))
                End With
            Next
        Next
    End Sub
End Class





Public Class KMeans_XYDepth : Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "kMeans k", 2, 32, 4)
        End If
        Dim w = dst1.Cols / 4
        Dim h = dst1.Rows / 4
        task.drawRect = New cv.Rect(w, h, w * 2, h * 2)
        label1 = "Draw rectangle anywhere..."
        label2 = "Currently selected region"
        task.desc = "Cluster with x, y, and depth using kMeans.  Draw on the image to select a region."
    End Sub
    Public Sub Run(src As cv.Mat)
        Dim roi = task.drawRect
        Dim xyDepth32f As New cv.Mat(task.depth32f(roi).Size(), cv.MatType.CV_32FC3, 0)
        For y = 0 To xyDepth32f.Rows - 1
            For x = 0 To xyDepth32f.Cols - 1
                Dim nextVal = task.depth32f(roi).Get(Of Single)(y, x)
                If nextVal Then xyDepth32f.Set(Of cv.Vec3f)(y, x, New cv.Vec3f(x, y, nextVal))
            Next
        Next
        Dim columnVector As New cv.Mat
        columnVector = xyDepth32f.Reshape(xyDepth32f.Channels, xyDepth32f.Rows * xyDepth32f.Cols)
        Dim labels = New cv.Mat()
        Dim colors As New cv.Mat
        cv.Cv2.Kmeans(columnVector, sliders.trackbar(0).Value, labels, term, 3, cv.KMeansFlags.PpCenters, colors)
        For i = 0 To columnVector.Rows - 1
            columnVector.Set(Of cv.Vec3f)(i, 0, colors.Get(Of cv.Vec3f)(labels.Get(Of Integer)(i)))
        Next
        task.RGBDepth.CopyTo(dst1)
        columnVector.Reshape(3, dst1(roi).Height).ConvertTo(dst1(roi), cv.MatType.CV_8U)
    End Sub
End Class




Public Class KMeans_Depth_FG_BG : Inherits VBparent
    Public Sub New()
        label1 = "Foreground Mask"
        label2 = "Background Mask"
        task.desc = "Separate foreground and background using Kmeans (with k=2) using the depth value of center point."
    End Sub
    Public Sub Run(src As cv.Mat)
        Dim columnVector As New cv.Mat
        columnVector = task.depth32f.Reshape(1, task.depth32f.Rows * task.depth32f.Cols)
        columnVector.ConvertTo(columnVector, cv.MatType.CV_32FC1)
        Dim labels = New cv.Mat()
        Dim depthCenters As New cv.Mat
        cv.Cv2.Kmeans(columnVector, 2, labels, term, 3, cv.KMeansFlags.PpCenters, depthCenters)
        labels = labels.Reshape(1, task.depth32f.Rows)

        Dim foregroundLabel = 0
        If depthCenters.Get(Of Single)(0, 0) > depthCenters.Get(Of Single)(1, 0) Then foregroundLabel = 1

        Dim mask = labels.InRange(foregroundLabel, foregroundLabel)
        Dim shadowMask = task.depth32f.Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
        mask.SetTo(0, shadowMask)
        dst1 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim backMask As New cv.Mat
        cv.Cv2.BitwiseNot(mask, backMask)
        dst2 = backMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class




Public Class KMeans_LAB : Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "kMeans k", 2, 32, 4)
        End If
        label1 = "kMeans_LAB - draw to select region"
        Dim w = dst1.Cols / 4
        Dim h = dst1.Rows / 4
        task.drawRect = New cv.Rect(w, h, w * 2, h * 2)
        task.desc = "Cluster the LAB image using kMeans.  Is it better?  Optionally draw on the image and select k."
    End Sub
    Public Sub Run(src As cv.Mat)
        Dim roi = task.drawRect
        Dim labMat = src(roi).CvtColor(cv.ColorConversionCodes.RGB2Lab)
        Dim columnVector As New cv.Mat
        columnVector = labMat.Reshape(src.Channels, roi.Height * roi.Width)
        Dim lab32f As New cv.Mat
        columnVector.ConvertTo(lab32f, cv.MatType.CV_32FC3)
        Dim clusterCount = sliders.trackbar(0).Value
        Dim labels = New cv.Mat()
        Dim colors As New cv.Mat

        cv.Cv2.Kmeans(lab32f, clusterCount, labels, term, 1, cv.KMeansFlags.PpCenters, colors)

        For i = 0 To columnVector.Rows - 1
            lab32f.Set(Of cv.Vec3f)(i, 0, colors.Get(Of cv.Vec3f)(labels.Get(Of Integer)(i)))
        Next
        src.CopyTo(dst1)
        lab32f.Reshape(3, roi.Height).ConvertTo(dst1(roi), cv.MatType.CV_8UC3)
        dst1(roi) = dst1(roi).CvtColor(cv.ColorConversionCodes.Lab2RGB)
        dst1.Rectangle(task.drawRect, cv.Scalar.White, 1)
    End Sub
End Class






Public Class KMeans_Color : Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "kMeans cluster count (k)", 2, 32, 3)
        End If
        task.desc = "Cluster the rgb image using kMeans.  Color each cluster by average depth."
    End Sub
    Public Sub Run(src As cv.Mat)
        Dim columnVector = src.Reshape(src.Channels, src.Height * src.Width)
        Dim rgb32f As New cv.Mat
        columnVector.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
        Dim clusterCount = sliders.trackbar(0).Value
        Dim labels = New cv.Mat()
        Dim colors As New cv.Mat

        cv.Cv2.Kmeans(rgb32f, clusterCount, labels, term, 1, cv.KMeansFlags.PpCenters, colors)
        labels.Reshape(1, src.Height).ConvertTo(labels, cv.MatType.CV_8U)

        For i = 0 To clusterCount - 1
            Dim mask = labels.InRange(i, i)
            Dim mean = task.RGBDepth.Mean(mask)
            dst1.SetTo(mean, mask)
        Next
    End Sub
End Class





Public Class KMeans_Color_MT : Inherits VBparent
    Public grid As Thread_Grid
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "kMeans k", 2, 32, 2)
        End If

        grid = New Thread_Grid
        findSlider("ThreadGrid Width").Value = 128
        findSlider("ThreadGrid Height").Value = 160

        task.desc = "Cluster the rgb image using kMeans.  Color each cluster by average depth."
    End Sub
    Public Sub Run(src As cv.Mat)
        grid.Run(Nothing)
        Dim clusterCount = sliders.trackbar(0).Value
        Parallel.ForEach(grid.roiList,
        Sub(roi)
            Dim zeroDepth = task.depth32f(roi).Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
            Dim color = src(roi).Clone()
            Dim columnVector = color.Reshape(src.Channels, roi.Height * roi.Width)
            Dim rgb32f As New cv.Mat
            columnVector.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
            Dim labels = New cv.Mat()
            Dim colors As New cv.Mat

            cv.Cv2.Kmeans(rgb32f, clusterCount, labels, term, 1, cv.KMeansFlags.PpCenters, colors)
            labels.Reshape(1, roi.Height).ConvertTo(labels, cv.MatType.CV_8U)

            dst1(roi).SetTo(0)
            For i = 0 To clusterCount - 1
                Dim mask = labels.InRange(i, i)
                mask.SetTo(0, zeroDepth) ' don't include the zeros in the mean depth computation.
                Dim mean = task.RGBDepth(roi).Mean(mask)
                dst1(roi).SetTo(mean, mask)
            Next
        End Sub)
    End Sub
End Class





Public Class KMeans_ColorDepth : Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "kMeans k", 2, 32, 3)
        End If

        task.desc = "Cluster the rgb+Depth using kMeans.  Color each cluster by average depth."
    End Sub
    Public Sub Run(src As cv.Mat)
        Dim rgb32f As New cv.Mat
        src.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
        Dim srcPlanes() As cv.Mat = Nothing
        srcPlanes = rgb32f.Split()
        ReDim Preserve srcPlanes(3)
        srcPlanes(3) = task.depth32f
        Dim zeroMask = srcPlanes(3).Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()

        Dim rgbDepth As New cv.Mat
        cv.Cv2.Merge(srcPlanes, rgbDepth)

        Dim columnVector = rgbDepth.Reshape(srcPlanes.Length, rgbDepth.Height * rgbDepth.Width)
        Dim clusterCount = sliders.trackbar(0).Value
        Dim labels = New cv.Mat()
        Dim colors As New cv.Mat

        cv.Cv2.Kmeans(columnVector, clusterCount, labels, term, 1, cv.KMeansFlags.PpCenters, colors)
        labels.Reshape(1, src.Height).ConvertTo(labels, cv.MatType.CV_8U)

        For i = 0 To clusterCount - 1
            Dim mask = labels.InRange(i, i)
            Dim mean = task.RGBDepth.Mean(mask)
            dst1.SetTo(mean, mask)
        Next
        dst1.SetTo(0, zeroMask)
    End Sub
End Class





Public Class KMeans_ColorDepth_MT : Inherits VBparent
    Public grid As Thread_Grid
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "kMeans k", 2, 32, 3)
        End If

        grid = New Thread_Grid
        grid.sliders.trackbar(0).Value = 32
        grid.sliders.trackbar(1).Value = 32

        task.desc = "Cluster the rgb+Depth using kMeans.  Color each cluster by average depth."
    End Sub
    Public Sub Run(src As cv.Mat)
        grid.Run(Nothing)

        Dim clusterCount = sliders.trackbar(0).Value
        Parallel.ForEach(grid.roiList,
       Sub(roi)
           Dim rgb32f As New cv.Mat
           src(roi).ConvertTo(rgb32f, cv.MatType.CV_32FC3)
           Dim srcPlanes() As cv.Mat = Nothing
           srcPlanes = rgb32f.Split()
           ReDim Preserve srcPlanes(4 - 1)
           srcPlanes(3) = task.depth32f(roi)

           Dim rgbDepth As New cv.Mat
           cv.Cv2.Merge(srcPlanes, rgbDepth)

           Dim columnVector = rgbDepth.Reshape(srcPlanes.Length, rgbDepth.Height * rgbDepth.Width)
           Dim labels = New cv.Mat()
           Dim colors As New cv.Mat

           cv.Cv2.Kmeans(columnVector, clusterCount, labels, term, 1, cv.KMeansFlags.PpCenters, colors)
           labels.Reshape(1, roi.Height).ConvertTo(labels, cv.MatType.CV_8U)

           dst1(roi).SetTo(0)
           For i = 0 To clusterCount - 1
               Dim mask = labels.InRange(i, i)
               Dim mean = task.RGBDepth(roi).Mean(mask)
               dst1(roi).SetTo(mean, mask)
           Next
       End Sub)
    End Sub
End Class







Public Class KMeans_Subdivision : Inherits VBparent
    Dim kmeans As KMeans_BasicsDepthColor
    Public Sub New()
        kmeans = New KMeans_BasicsDepthColor()
        kmeans.resizeRequest = False
        task.desc = "Use KMeans to subdivide an image and then subdivide it again."
    End Sub
    Public Sub Run(src As cv.Mat)
        Static kmeansKslider = findSlider("kMeans k")
        kmeansKslider.value = 2

        kmeans.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst1 = kmeans.dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim mean = dst1.Mean()
        Dim maskDark = dst1.Threshold(mean.Item(0), 255, cv.ThresholdTypes.BinaryInv)
        src.SetTo(0)
        src.CopyTo(src, maskDark)
        kmeansKslider.value = 3
        kmeans.Run(src)
        dst2 = kmeans.dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim maskLite = dst1.Threshold(mean.Item(0), 255, cv.ThresholdTypes.Binary)
        src.SetTo(0)
        src.CopyTo(src, maskLite)
        kmeansKslider.value = 3
        kmeans.Run(src)
        kmeans.dst1.CopyTo(dst2, maskLite)
    End Sub
End Class







Public Class KMeans_Subdivision1 : Inherits VBparent
    Dim kmeans As KMeans_BasicsDepthColor
    Public Sub New()
        kmeans = New KMeans_BasicsDepthColor()
        kmeans.resizeRequest = False
        kmeans.useDepthColor = False
        task.desc = "Use KMeans to subdivide an image and then subdivide it again."
    End Sub
    Public Sub Run(src As cv.Mat)
        Static kmeansKslider = findSlider("kMeans k")
        kmeansKslider.value = 2

        kmeans.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        Dim gray1 = kmeans.dst1.Clone

        Dim maskDark = gray1.Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
        src.SetTo(0)
        src.CopyTo(src, maskDark)
        kmeansKslider.value = 3
        kmeans.Run(src)
        Dim gray2 = kmeans.dst1.Clone

        Dim maskLite = gray1.Threshold(1, 255, cv.ThresholdTypes.Binary)
        src.SetTo(0)
        src.CopyTo(src, maskLite)
        kmeansKslider.value = 3
        kmeans.Run(src)
        kmeans.dst1.CopyTo(gray2, maskLite)

        Dim centroids As New List(Of cv.Point)
        For i = 0 To kmeans.kmeansK - 1
            Dim mask = gray1.InRange(i, i)
            Dim m = cv.Cv2.Moments(mask, True)
            centroids.Add(New cv.Point2f(m.M10 / m.M00, m.M01 / m.M00))
            dst1.SetTo(task.scalarColors(i), mask)
        Next

        For i = 0 To centroids.Count - 1
            dst1.Circle(centroids(i), 10, cv.Scalar.Yellow, -1, task.lineType)
        Next
    End Sub
End Class
