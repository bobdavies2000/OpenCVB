Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class KMeans_Basics : Inherits VB_Algorithm
    Public options As New Options_KMeans
    Public colors As New cv.Mat
    Public buildPaletteOutput As Boolean = True
    Public saveLabels As New cv.Mat
    Public classCount As Integer
    Public Sub New()
        labels = {"", "", "", "Palette output for the kMeans labels"}
        desc = "Cluster the input using kMeans."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() And src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        options.RunVB()
        classCount = options.kMeansK
        Static lastK = classCount
        If task.optionsChanged Or lastK <> classCount Then
            options.kMeansFlag = cv.KMeansFlags.PpCenters
            saveLabels = New cv.Mat
        End If

        Dim columnVector = src.Reshape(src.Channels, src.Height * src.Width)
        dst2 = saveLabels

        If columnVector.ElemSize Mod 4 <> 0 Or columnVector.Type = cv.MatType.CV_32S Then columnVector.ConvertTo(columnVector, cv.MatType.CV_32F)
        Try
            ' some samples are NAN and breaks the kmeans call.  The following lines were helpful to debug this problem.
            'Dim samples(columnVector.Total - 1) As Single
            'Marshal.Copy(columnVector.Data, samples, 0, samples.Length)
            'Dim val = columnVector.Get(Of Single)(columnVector.Rows, 0)
            'Dim updated As Boolean
            'For i = 0 To samples.Count - 1
            '    If Single.IsNaN(samples(i)) Or Single.IsInfinity(samples(i)) Then
            '        samples(i) = 0
            '        updated = True
            '    End If
            'Next
            'If updated Then Marshal.Copy(samples, columnVector.Data, 0, samples.Length)
            If colors.Width = 0 Or colors.Height = 0 Then options.kMeansFlag = cv.KMeansFlags.PpCenters
            cv.Cv2.Kmeans(columnVector, classCount, dst2, term, 1, options.kMeansFlag, colors)
        Catch ex As Exception
            columnVector.SetTo(0)
            dst2.SetTo(0)
            cv.Cv2.Kmeans(columnVector, classCount, dst2, term, 1, options.kMeansFlag, colors)
            ' Console.WriteLine("Huge or NaN values in the input... Can happen on an initial useInitialValues run... It is corrected here...")
        End Try

        saveLabels = dst2.Clone

        dst2.Reshape(1, src.Height).ConvertTo(dst2, cv.MatType.CV_8U)
        If standaloneTest() Then dst3 = vbPalette(dst2 * 255 / classCount)
        lastK = classCount
        labels(2) = "KMeans labels 0-" + CStr(lastK) + " spread out across 255 values."
    End Sub
End Class








Public Class KMeans_MultiChannel : Inherits VB_Algorithm
    Public colors As New cv.Mat
    Dim km As New KMeans_Basics
    Public Sub New()
        labels = {"", "", "KMeans_Basics output with BGR input", "dst3 contains the labels spread across the palette (dst0 contains the exact labels)"}
        desc = "Cluster the input using kMeans."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then task.color.ConvertTo(src, cv.MatType.CV_32FC3)
        If src.Type = cv.MatType.CV_8UC3 Then src.ConvertTo(src, cv.MatType.CV_32FC3)
        If src.Type = cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_32F)
        km.Run(src)
        dst3 = km.dst2

        dst2 = vbPalette(dst3 * 255 / km.classCount)
    End Sub
End Class







Public Class KMeans_k2_to_k8 : Inherits VB_Algorithm
    Dim Mats As New Mat_4Click
    Dim km As New KMeans_Basics
    Public Sub New()
        labels(2) = "kmeans - k=2,4,6,8"
        desc = "Show clustering with various settings for cluster count.  Draw to select region of interest."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static kSlider = findSlider("KMeans k")

        Static kmIndex As Integer
        If task.frameCount Mod 100 = 0 Then
            kmIndex += 1
            If kmIndex >= 4 Then kmIndex = 0
        End If

        kSlider.Value = Choose(kmIndex + 1, 2, 4, 6, 8)
        km.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        Mats.mat(kmIndex) = km.dst2 * 255 / km.classCount

        Mats.Run(empty)
        dst2 = Mats.dst2
        dst3 = Mats.dst3
    End Sub
End Class








Public Class KMeans_Fuzzy : Inherits VB_Algorithm
    Dim km As New KMeans_Image
    Public fuzzyD As New Fuzzy_Basics
    Public Sub New()
        labels(3) = "The white marks areas that are busy while the black marks areas that are consistent in color - not fuzzy."
        desc = "Use the KMeans output as input to the Fuzzy detector - those areas which have little info"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        km.Run(src)
        dst2 = km.km.dst2
        fuzzyD.Run(dst2)
        dst3 = fuzzyD.dst3
    End Sub
End Class






' http://man.hubwiz.com/docset/OpenCV.docset/Contents/Resources/Documents/d9/dde/samples_2cpp_2kmeans_8cpp-example.html
Public Class KMeans_MultiGaussian_CPP : Inherits VB_Algorithm
    Public Sub New()
        cPtr = KMeans_MultiGaussian_Open()
        desc = "Use KMeans on a random multi-gaussian distribution."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim imagePtr = KMeans_MultiGaussian_RunCPP(cPtr, src.Rows, src.Cols)
        If imagePtr <> 0 And task.heartBeat Then dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr).Clone()
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = KMeans_MultiGaussian_Close(cPtr)
    End Sub
End Class





Public Class KMeans_CustomData : Inherits VB_Algorithm
    Dim km As New KMeans_Basics
    Public centers = New cv.Mat()
    Public Sub New()
        desc = "Cluster the selected input using kMeans"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        km.options.RunVB()
        Dim k = km.options.kMeansK
        If src.Rows < k Then k = src.Rows

        If standaloneTest() Then
            Static random = New Random_Basics
            Static randslider = findSlider("Random Pixel Count")
            If firstPass Then randslider.Value = 50
            If randslider.Value < k Then randslider.Value = k
            If task.heartBeat Then random.Run(empty)

            Dim input As New List(Of Single)
            For Each pt In random.pointlist
                input.Add(pt.x)
                input.Add(pt.y)
            Next
            dst0 = New cv.Mat(input.Count, 1, cv.MatType.CV_32F, input.ToArray)
        End If

        km.Run(dst0)
        dst2 = vbPalette(km.dst2 * 255 / km.classCount)
    End Sub
End Class







Public Class KMeans_Simple_CPP : Inherits VB_Algorithm
    Public Sub New()
        cPtr = Kmeans_Simple_Open()
        gOptions.MaxDepth.Value = 4
        desc = "Split the input into 3 levels - zero (no depth), closer to min, closer to max."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then src = task.pcSplit(2)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim mm as mmData = vbMinMax(src, task.depthMask)

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Kmeans_Simple_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, CSng(mm.minVal), gOptions.MaxDepth.Value)
        handleSrc.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr)
        setTrueText("Use 'Max Depth' in the global options to set the boundary between blue and yellow.", 3)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Kmeans_Simple_Close(cPtr)
    End Sub
End Class








Public Class KMeans_Edges : Inherits VB_Algorithm
    Dim edges As New Edge_Canny
    Public km As New KMeans_Image
    Public classCount As Integer
    Dim redC As New RedCloud_Basics
    Public Sub New()
        redOptions.UseColor.Checked = True
        labels(3) = "KMeans with edges output"
        desc = "Use edges to isolate regions in the KMeans output - not much different from KMeans_Basics."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        edges.Run(src)
        src.SetTo(cv.Scalar.White, edges.dst2)

        km.Run(src)
        dst3 = km.dst2 + 1
        classCount = km.classCount

        redC.Run(dst3)
        dst2 = redC.dst2
        labels(2) = redC.labels(3)
    End Sub
End Class









Public Class KMeans_CompareMulti : Inherits VB_Algorithm
    Dim km As New KMeans_Image
    Dim multi As New KMeans_MultiChannel
    Public Sub New()
        labels = {"", "", "KMeans_Basics output", "KMeans on all 3 channels - recombined"}
        desc = "Compare the results of using grayscale KMeans with multi-channel KMeans"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        km.Run(src)
        dst2 = km.dst2

        dst2 = vbPalette(dst2)

        multi.Run(src)
        dst3 = multi.dst2
        labels(2) = ""
    End Sub
End Class









Public Class KMeans_TierCount : Inherits VB_Algorithm
    Dim km As New KMeans_Basics
    Dim kCount As New Depth_TierCount
    Public classCount As Integer
    Public Sub New()
        desc = "Use the Histogram valleys to find the best 'K' value for the current depth data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        kCount.Run(src)
        Static kSlider = findSlider("KMeans k")
        If kSlider.value <> kCount.classCount Then kSlider.value = Math.Max(kCount.classCount, kSlider.minimum)
        classCount = kCount.classCount

        km.Run(task.pcSplit(2))
        dst2 = km.dst2 * 255 / km.classCount
        dst2.SetTo(0, task.noDepthMask)
        dst3 = vbPalette(dst2)
        labels(2) = "There were " + CStr(classCount) + " tiers (on average) found in the depth valleys histogram."
    End Sub
End Class








Public Class KMeans_Image : Inherits VB_Algorithm
    Public km As New KMeans_Basics
    Public masks As New List(Of cv.Mat)
    Public counts As New List(Of Integer)
    Public classCount As Integer
    Public Sub New()
        labels = {"", "", "KMeans output after Palette run", "Each of the KMeans masks is displayed below in rotation."}
        desc = "Cluster the input image pixels using kMeans and allow any region to be selected for highlight in dst3."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        km.Run(src)
        dst2 = vbPalette(km.dst2 * 255 / km.classCount)
        classCount = km.options.kMeansK

        masks.Clear()
        counts.Clear()
        Dim k = km.options.kMeansK
        For i = 0 To k - 1
            Dim mask = km.dst2.InRange(i, i)
            masks.Add(mask)
            counts.Add(mask.CountNonZero)
        Next
        Static maskIndex As Integer
        If task.heartBeat Then maskIndex += 1
        If maskIndex >= masks.Count Then maskIndex = 0
        dst3 = masks(maskIndex)
    End Sub
End Class









Public Class KMeans_DepthPlusGray : Inherits VB_Algorithm
    Dim km As New KMeans_Basics
    Dim grayPlus(2 - 1) As cv.Mat
    Public Sub New()
        km.buildPaletteOutput = False
        labels(3) = "KMeans 8-bit results"
        grayPlus(0) = New cv.Mat(task.workingRes, cv.MatType.CV_32F, 0)
        desc = "Cluster the rgb+depth image pixels using kMeans"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        src.CvtColor(cv.ColorConversionCodes.BGR2GRAY).ConvertTo(grayPlus(0), cv.MatType.CV_32F)
        grayPlus(0).SetTo(0, task.noDepthMask)
        grayPlus(1) = task.pcSplit(2)

        Dim merge As New cv.Mat
        cv.Cv2.Merge(grayPlus, merge)
        km.Run(merge)

        Dim k = km.options.kMeansK
        dst3 = km.dst2
        dst3.SetTo(0, task.noDepthMask)

        If standaloneTest() Then dst2 = vbPalette(km.dst2 * 255 / k)
    End Sub
End Class










Public Class KMeans_Dimensions : Inherits VB_Algorithm
    Dim km As New KMeans_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Dimension", 1, 6, 1)
        desc = "Demonstrate how to use KMeans for a variety of dimensions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static dimSlider = findSlider("Dimension")

        Dim merge As New cv.Mat
        Select Case dimSlider.value
            Case 1 ' grayscale
                src.CvtColor(cv.ColorConversionCodes.BGR2GRAY).ConvertTo(merge, cv.MatType.CV_32F)
            Case 2 ' pointcloud x and y
                cv.Cv2.Merge({task.pcSplit(0), task.pcSplit(1)}, merge)
            Case 3 ' pointcloud dimensions
                merge = task.pointCloud
            Case 4 ' color + depth
                src.ConvertTo(src, cv.MatType.CV_32F)
                task.pcSplit(2) = task.pcSplit(2).Normalize(0, 255, cv.NormTypes.MinMax)
                cv.Cv2.Merge({src, task.pcSplit(2)}, merge)
            Case 5 ' color + pcSplit(0) and pcSplit(1)
                src.ConvertTo(src, cv.MatType.CV_32F)
                task.pcSplit(0) = task.pcSplit(0).Normalize(0, 255, cv.NormTypes.MinMax)
                task.pcSplit(1) = task.pcSplit(1).Normalize(0, 255, cv.NormTypes.MinMax)
                cv.Cv2.Merge({src, task.pcSplit(0), task.pcSplit(1)}, merge)
            Case 6 ' color + pointcloud
                src.ConvertTo(src, cv.MatType.CV_32F)
                Dim tmp1 = task.pcSplit(0).Normalize(0, 255, cv.NormTypes.MinMax)
                Dim tmp2 = task.pcSplit(1).Normalize(0, 255, cv.NormTypes.MinMax)
                Dim tmp3 = task.pcSplit(2).Normalize(0, 255, cv.NormTypes.MinMax)
                cv.Cv2.Merge({src, tmp1, tmp2, tmp3}, merge)
        End Select

        km.Run(merge)

        labels(2) = "Dimension = " + CStr(dimSlider.value)
        labels(3) = labels(2)

        dst2 = km.dst2 + 1
        dst3 = vbPalette(dst2 * 255 / km.classCount)
    End Sub
End Class







Public Class KMeans_Valleys : Inherits VB_Algorithm
    Dim km As New KMeans_Basics
    Dim tiers As New KMeans_TierCount
    Public Sub New()
        labels(2) = "8-Bit input to vbPalette output in dst3"
        desc = "Cluster depth using kMeans - use KMeans_TierCount to determine 'K'"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        tiers.Run(src)

        Static kSlider = findSlider("KMeans k")
        kSlider.value = tiers.classCount
        Dim kMeansK = kSlider.Value

        km.Run(task.pcSplit(2))
        dst2 = km.dst2 + 1

        dst3 = vbPalette(dst2 * 255 / tiers.classCount)
        dst3.SetTo(0, task.noDepthMask)
    End Sub
End Class








Public Class KMeans_Depth : Inherits VB_Algorithm
    Public km As New KMeans_Basics
    Public classCount As Integer
    Public Sub New()
        findSlider("KMeans k").Value = 10
        labels(2) =
        desc = "Cluster depth using kMeans - useful to split foreground and background"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        km.Run(task.pcSplit(2))
        dst2 = km.dst2 + 1
        dst2.SetTo(0, task.noDepthMask)

        classCount = km.classCount
        dst3 = vbPalette(dst2 * 255 / classCount)
        labels(2) = "Palettized version of the " + CStr(classCount) + " 8UC1 classes"
    End Sub
End Class








Public Class KMeans_SimKColor : Inherits VB_Algorithm
    Dim plot1D As New Hist3Dcolor_PlotHist1D
    Dim simK As New Hist3D_BuildHistogram
    Public classCount As Integer
    Dim histogram As New cv.Mat
    Public Sub New()
        desc = "Use the gaps in the 3D histogram of the color image to find 'k' and backproject the results."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then
            plot1D.Run(src)
            dst3 = plot1D.dst2
            labels(3) = "The 3D histogram of the RGB image stream in 1D - note the number of gaps"

            simK.Run(plot1D.histogram1D)
            histogram = simK.dst2
            classCount = simK.classCount
        End If

        cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst1, redOptions.rangesBGR)

        dst2 = vbPalette(dst1 * 255 / classCount)
        labels(2) = simK.labels(2) + " with " + CStr(redOptions.bins3D) + " histogram bins"
    End Sub
End Class





Public Class KMeans_SimKDepth : Inherits VB_Algorithm
    Dim plot1D As New Hist3Dcloud_PlotHist1D
    Dim simK As New Hist3D_BuildHistogram
    Public classCount As Integer
    Public Sub New()
        desc = "Use the gaps in the 3D histogram of depth to find simK and backproject the results."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud
        If task.heartBeat Then
            plot1D.Run(src)
            dst3 = plot1D.dst2
            labels(3) = "The 3D histogram of the depth stream in 1D"

            simK.Run(plot1D.histogram)
            plot1D.histogram = simK.dst2
            classCount = simK.classCount
        End If
        cv.Cv2.CalcBackProject({src}, {2}, plot1D.histogram, dst1, redOptions.rangesCloud)
        dst1 = dst1.ConvertScaleAbs

        dst2 = vbPalette(dst1 * 255 / classCount)

        labels(2) = simK.labels(2) + " with " + CStr(redOptions.bins3D) + " histogram bins"
    End Sub
End Class
