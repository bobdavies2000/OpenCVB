Imports cv = OpenCvSharp

Public Class KMeans_Basics : Inherits VBparent
    Public masks As New List(Of cv.Mat)
    Public colors As New cv.Mat
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "kMeans k", 2, 32, 4)
            sliders.setupTrackBar(1, "Resize Factor (used only with KMeans_BasicsFast)", 1, 8, 2)
            sliders.setupTrackBar(2, "Select Mask - light to dark or farthest to closest", 0, 100, 0)
            sliders.setupTrackBar(3, "Retain x frames to measure unstable pixels", 1, 20, 5)
            findSlider("Resize Factor (used only with KMeans_BasicsFast)").Enabled = False
        End If

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "Use PpCenters"
            radio.check(1).Text = "Use RandomCenters"
            radio.check(2).Text = "Use Initialized Labels"
            radio.check(0).Checked = True
        End If

        label1 = "KMeans_Basics output with just RGB input"
        task.desc = "Cluster the input image pixels using kMeans."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 5
        Static maskSlider = findSlider("Select Mask - light to dark or farthest to closest")
        Static radioPP = findRadio("Use PpCenters")
        Static radioLabels = findRadio("Use Initialized Labels")
        Static kSlider = findSlider("kMeans k")
        Static resizeSlider = findSlider("Resize Factor (used only with KMeans_BasicsFast)")
        Dim kMeansK = kSlider.value

        Dim input = src.Clone
        If standalone Then task.color.ConvertTo(input, cv.MatType.CV_32FC3)
        If input.Type = cv.MatType.CV_8UC3 Then input.ConvertTo(input, cv.MatType.CV_32FC3)
        If input.Type = cv.MatType.CV_8U Then input.ConvertTo(input, cv.MatType.CV_32F)
        Dim columnVector = input.Reshape(input.Channels, input.Height * input.Width)
        Dim labels As New cv.Mat()
        Static saveLabels As New cv.Mat()
        Static saveK = kMeansK
        Static saveSize As Integer
        If task.frameCount > 0 And saveK = kMeansK And saveSize = resizeSlider.value Then
            labels = saveLabels
            radioLabels.Checked = True
        Else
            radioPP.Checked = True
            saveK = kMeansK
            saveSize = resizeSlider.value
            labels = New cv.Mat()
            colors = New cv.Mat
        End If

        Dim kmeansFlag = cv.KMeansFlags.RandomCenters
        If radioPP.checked Then kmeansFlag = cv.KMeansFlags.PpCenters
        If radioLabels.checked Then kmeansFlag = cv.KMeansFlags.UseInitialLabels

        cv.Cv2.Kmeans(columnVector, kMeansK, labels, term, 1, kmeansFlag, colors)
        saveLabels = labels.Clone
        labels.Reshape(1, input.Height).ConvertTo(labels, cv.MatType.CV_8U)

        Dim range255 As Boolean = True
        For i = 0 To colors.Rows - 1
            Dim val = colors.Get(Of Single)(i, 0)
            If val > 255 Then range255 = False
        Next
        If range255 = False Then
            colors = colors.Normalize(0, 255, cv.NormTypes.MinMax)
        End If

        Dim maskOrder As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
        For i = 0 To colors.Rows - 1
            Dim val = colors.Get(Of Single)(i, 0)
            If val > 255 Then range255 = False
            maskOrder.Add(val, i)
        Next

        masks.Clear()
        For i = 0 To maskOrder.Count - 1
            Dim index = maskOrder.ElementAt(i).Value
            Dim mask = labels.InRange(index, index)
            masks.Add(mask)
            dst1 = dst1.Resize(input.Size)
            If input.Channels = 3 Then
                dst1.SetTo(colors.Get(Of cv.Vec3f)(i, 0), mask)
            Else
                ' if the input was not 3-channel, then just use the first channel of colors.  Be sure that the first input channel was RGB/grayscale...
                Dim gray = colors.Get(Of Single)(index, 0)
                dst1.SetTo(cv.Scalar.All(gray), mask)
            End If
        Next

        Static saveMaskCount As Integer
        If saveMaskCount <> masks.Count Then
            maskSlider.value = 0
            maskSlider.maximum = masks.Count - 1
            saveMaskCount = masks.Count
        End If
        dst2 = masks(maskSlider.value)
    End Sub
End Class







Public Class KMeans_BasicsFast : Inherits VBparent
    Public km As New KMeans_Basics
    Public Sub New()
        findSlider("Resize Factor (used only with KMeans_BasicsFast)").Enabled = True
        task.desc = "Speed up the KMeans_Basics with a resize factor"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static resizeSlider = findSlider("Resize Factor (used only with KMeans_BasicsFast)")
        Dim resizeFactor = resizeSlider.value

        Dim small8uC3 = src.Resize(New cv.Size(CInt(src.Rows / resizeFactor), CInt(src.Cols / resizeFactor)))
        Dim small32fC3 As New cv.Mat
        small8uC3.ConvertTo(small32fC3, cv.MatType.CV_32FC3)
        km.Run(small32fC3)
        dst1 = km.dst1.Resize(dst1.Size())
    End Sub
End Class







Public Class KMeans_DepthPlusGray : Inherits VBparent
    Dim km As New KMeans_Basics
    Dim grayPlus(2 - 1) As cv.Mat
    Public Sub New()
        grayPlus(0) = New cv.Mat(task.color.Size, cv.MatType.CV_32F, 0)
        task.desc = "Cluster the rgb+depth image pixels using kMeans"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static kSlider = findSlider("kMeans k")
        Dim kMeansK = kSlider.value

        Dim gray = task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        gray.ConvertTo(grayPlus(0), cv.MatType.CV_32F)
        grayPlus(0).SetTo(0, task.noDepthMask)
        grayPlus(1) = task.depth32f

        Dim merge As New cv.Mat
        cv.Cv2.Merge(grayPlus, merge)
        km.Run(merge)
        dst1 = km.dst1
        dst1.SetTo(0, task.noDepthMask)
    End Sub
End Class






Public Class KMeans_Depth : Inherits VBparent
    Dim km As New KMeans_Basics
    Public Sub New()
        task.desc = "Cluster just depth using kMeans"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static kSlider = findSlider("kMeans k")
        Dim kMeansK = kSlider.value

        km.Run(task.depth32f)
        dst1 = km.dst1
        dst1.SetTo(0, task.noDepthMask)
    End Sub
End Class






Public Class KMeans_DepthPlusColor : Inherits VBparent
    Dim km As New KMeans_Basics
    Public Sub New()
        task.desc = "Cluster the rgb+depth image pixels using kMeans"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static kSlider = findSlider("kMeans k")
        Dim kMeansK = kSlider.value
        Dim rgb32f As New cv.Mat
        task.color.ConvertTo(rgb32f, cv.MatType.CV_32FC3)

        Dim split = rgb32f.Split()
        ReDim Preserve split(4 - 1)
        split(3) = task.depth32f
        split(3).SetTo(100000, task.noDepthMask)

        Dim merge As New cv.Mat
        cv.Cv2.Merge(split, merge)
        km.Run(merge)
        dst1 = km.dst1
        dst1.SetTo(0, task.noDepthMask)
    End Sub
End Class






Public Class KMeans_k2_to_k8 : Inherits VBparent
    Dim Mats As New Mat_4Click
    Dim km2 As New KMeans_BasicsFast
    Dim km4 As New KMeans_BasicsFast
    Dim km6 As New KMeans_BasicsFast
    Dim km8 As New KMeans_BasicsFast
    Public Sub New()
        label1 = "kmeans - k=2,4,6,8"
        label2 = "Click any quadrant at left to view it below"
        task.desc = "Show clustering with various settings for cluster count.  Draw to select region of interest."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static kSlider = findSlider("kMeans k")
        Static saveRect = task.drawRect
        task.drawRect = saveRect
        Dim rgb32f = New cv.Mat
        task.color.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
        For i = 0 To 3
            kSlider.value = Choose(i + 1, 2, 4, 6, 8)
            Dim km = Choose(i + 1, km2, km4, km6, km8)
            km.Run(rgb32f)
            Mats.mat(i) = km.dst1.Clone
        Next
        Mats.Run(src)
        dst1 = Mats.dst1
        dst2 = Mats.dst2
    End Sub
End Class






Public Class KMeans_Depth_FG_BG : Inherits VBparent
    Dim km As New KMeans_Basics
    Public Sub New()
        findSlider("kMeans k").Value = 2
        label1 = "Background Mask"
        label2 = "Foreground Mask"
        dst1 = New cv.Mat(task.color.Size, cv.MatType.CV_8U)
        dst2 = New cv.Mat(task.color.Size, cv.MatType.CV_8U)
        task.desc = "Separate foreground and background using Kmeans (with k=2) using the depth value of center point."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        km.Run(task.depth32f)

        Dim minDistance = Single.MaxValue
        Dim minIndex As Integer
        For i = 0 To km.colors.Rows - 1
            Dim distance = km.colors.Get(Of Single)(i, 0)
            If minDistance > distance And distance > 0 Then
                minDistance = distance
                minIndex = i
            End If
        Next
        dst1.SetTo(0)
        dst1.SetTo(255, km.masks(minIndex))
        dst1.SetTo(0, task.noDepthMask)

        cv.Cv2.BitwiseNot(dst1, dst2)
        dst2.SetTo(0, task.noDepthMask)
    End Sub
End Class







Public Class KMeans_LAB : Inherits VBparent
    Dim km As New KMeans_Basics
    Public Sub New()
        task.desc = "Cluster the LAB image using kMeans.  Is it better?"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim labMat = task.color.CvtColor(cv.ColorConversionCodes.RGB2Lab)
        Dim lab32f As New cv.Mat
        labMat.ConvertTo(lab32f, cv.MatType.CV_32FC3)
        km.Run(lab32f)
        dst1 = km.dst1
    End Sub
End Class








Public Class KMeans_Fuzzy : Inherits VBparent
    Dim km As New KMeans_Basics
    Public fuzzyD As New Fuzzy_Basics
    Public Sub New()
        task.desc = "Use the KMeans output as input to the Fuzzy detector"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        km.Run(src)
        dst1 = km.dst1
        fuzzyD.Run(km.dst1)
        dst2 = fuzzyD.dst2
    End Sub
End Class







Public Class KMeans_CComp : Inherits VBparent
    Dim ccomp As New CComp_GrayScale
    Dim km As New KMeans_Basics
    Public Sub New()
        task.desc = "Use each KMeans mask with CComp"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static maskSlider = findSlider("Select Mask - light to dark or farthest to closest")
        Dim maskIndex = maskSlider.value
        km.Run(src)
        dst1 = km.dst1

        ccomp.Run(km.masks(maskIndex))
        dst2 = ccomp.dst2
    End Sub
End Class







Public Class KMeans_CCompImage1 : Inherits VBparent
    Dim ccomp() As CComp_GrayScale
    Dim km As New KMeans_Basics
    Public Sub New()
        task.desc = "First attempt at coloring the entire image with connected components"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static kSlider = findSlider("kMeans k")
        Static k = -1
        If k <> kSlider.value Then
            k = kSlider.value
            ReDim ccomp(k)
            For i = 0 To k - 1
                ccomp(i) = New CComp_GrayScale
            Next
        End If

        km.Run(src)
        dst1 = km.dst1

        dst2.SetTo(0)
        For i = 0 To k - 1
            ccomp(i).Run(km.masks(i))
            cv.Cv2.BitwiseOr(dst2, ccomp(i).dst2, dst2)
        Next
    End Sub
End Class







Public Class KMeans_CCompImage : Inherits VBparent
    Dim km As New KMeans_CCompMasks
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        task.desc = "A second, better attempt at coloring an entire image with connected components.  All masks are available."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        km.Run(src)
        dst1 = km.dst1

        dst2.SetTo(0)
        Dim incr = 255 / km.masks.Count
        For i = 0 To km.masks.Count - 1
            Dim r As cv.Rect = km.rects(i)
            Dim m As cv.Mat = km.masks(i)
            dst2(r).SetTo(cv.Scalar.All((i + 1) * incr), m)
        Next

        task.palette.Run(dst2)
        dst2 = task.palette.dst1
    End Sub
End Class







Public Class KMeans_CCompMasks : Inherits VBparent
    Dim ccomp() As CComp_Basics
    Dim km As New KMeans_Basics
    Public masks As New List(Of cv.Mat)
    Public rects As New List(Of cv.Rect)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        label1 = "Click the centroid to display the mask in dst2"
        task.desc = "Use each KMeans mask with CComp"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 5
        Static kSlider = findSlider("kMeans k")
        Static k = -1
        If k <> kSlider.value Then
            k = kSlider.value
            ReDim ccomp(k)
            For i = 0 To k - 1
                ccomp(i) = New CComp_Basics
            Next
        End If

        km.Run(src)
        dst1 = km.dst1

        masks.Clear()
        rects.Clear()
        dst2.SetTo(0)
        Dim sortMasks As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        Dim centroids As New List(Of cv.Point)
        For i = 0 To k - 1
            ccomp(i).Run(km.masks(i))
            For j = 0 To ccomp(i).masks.Count - 1
                Dim r = ccomp(i).rects(j)
                sortMasks.Add(r.Width * r.Height, masks.Count)
                masks.Add(ccomp(i).masks(j))
                rects.Add(r)
                Dim c = ccomp(i).centroids(j)
                centroids.Add(c)
                dst1.Circle(c, task.dotSize + 3, cv.Scalar.White, -1, task.lineType)
                dst1.Circle(c, task.dotSize, cv.Scalar.Black, -1, task.lineType)
                setTrueText(CStr(masks.Count - 1), c.X + 10, c.Y)
            Next
        Next

        Static minIndex As Integer
        If task.mouseClickPoint <> New cv.Point Or minIndex >= masks.Count Then
            Dim minDistance As Single = Single.MaxValue
            For i = 0 To centroids.Count - 1
                Dim distance = task.mouseClickPoint.DistanceTo(centroids(i))
                If minDistance > distance Then
                    minDistance = distance
                    minIndex = i
                End If
            Next
        End If

        dst2(rects(minIndex)) = masks(minIndex)
        label2 = "Pixel count = " + CStr(sortMasks.ElementAt(minIndex).Key)
    End Sub
End Class