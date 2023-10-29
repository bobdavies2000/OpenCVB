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
        If standalone And src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
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

        dst2.Reshape(1, src.Height).ConvertTo(dst0, cv.MatType.CV_8U)
        dst2 = dst0 * 255 / classCount
        dst3 = vbPalette(dst2)
        lastK = classCount
        labels(2) = "KMeans labels 0-" + CStr(lastK) + " spread out across 255 values."
    End Sub
End Class







Public Class KMeans_MultiChannel : Inherits VB_Algorithm
    Public colors As New cv.Mat
    Dim km As New KMeans_Basics
    Public Sub New()
        labels = {"", "", "KMeans_Basics output with just BGR input", "dst3 contains the labels spread across the palette (dst0 contains the exact labels)"}
        desc = "Cluster the input using kMeans."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then task.color.ConvertTo(src, cv.MatType.CV_32FC3)
        If src.Type = cv.MatType.CV_8UC3 Then src.ConvertTo(src, cv.MatType.CV_32FC3)
        If src.Type = cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_32F)
        km.Run(src)
        dst3 = km.dst2

        dst2 = vbPalette(dst3)
    End Sub
End Class









Public Class KMeans_BasicsFast : Inherits VB_Algorithm
    Public km As New KMeans_Basics
    Public Sub New()
        desc = "Speed up the KMeans_Basics with a resize factor"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static resizeSlider = findSlider("Resize Factor")
        dst3 = src.Resize(New cv.Size(CInt(src.Rows / resizeSlider.Value), CInt(src.Cols / resizeSlider.Value)))
        If dst3.Channels <> 1 Then dst3 = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        km.Run(dst3)
        dst2 = km.dst2.Resize(dst2.Size())
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
        Mats.mat(kmIndex) = km.dst2

        mats.Run(Nothing)
        dst2 = Mats.dst2
        dst3 = Mats.dst3
    End Sub
End Class






Public Class KMeans_Foreground : Inherits VB_Algorithm
    Dim km As New KMeans_Image
    Public Sub New()
        findSlider("KMeans k").Value = 2
        labels = {"", "", "Foreground Mask", "Background Mask"}
        dst2 = New cv.Mat(task.workingRes, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(task.workingRes, cv.MatType.CV_8U, 0)
        desc = "Separate foreground and background using Kmeans (with k=2) using the depth value of center point."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        km.Run(task.pcSplit(2))

        Dim minDistance = Single.MaxValue
        Dim minIndex As Integer
        For i = 0 To km.km.colors.Rows - 1
            Dim distance = km.km.colors.Get(Of Single)(i, 0)
            If minDistance > distance And distance > 0 Then
                minDistance = distance
                minIndex = i
            End If
        Next
        dst2.SetTo(0)
        dst2.SetTo(255, km.masks(minIndex))
        dst2.SetTo(0, task.noDepthMask)

        dst3 = Not dst2
        dst3.SetTo(0, task.noDepthMask)
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










Module KMeans_MultiGaussian_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KMeans_MultiGaussian_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KMeans_MultiGaussian_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KMeans_MultiGaussian_RunCPP(cPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function
End Module





' http://man.hubwiz.com/docset/OpenCV.docset/Contents/Resources/Documents/d9/dde/samples_2cpp_2kmeans_8cpp-example.html
Public Class KMeans_MultiGaussian_CPP : Inherits VB_Algorithm
    Public Sub New()
        cPtr = KMeans_MultiGaussian_Open()
        desc = "Use KMeans on a random multi-gaussian distribution."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim imagePtr = KMeans_MultiGaussian_RunCPP(cPtr, src.Rows, src.Cols)
        If imagePtr <> 0 And heartBeat() Then dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr).Clone()
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

        If standalone Then
            Static random = New Random_Basics
            Static randslider = findSlider("Random Pixel Count")
            If firstPass Then randslider.Value = 50
            If randslider.Value < k Then randslider.Value = k
            If heartBeat() Then random.Run(Nothing)

            Dim input As New List(Of Single)
            For Each pt In random.pointlist
                input.Add(pt.x)
                input.Add(pt.y)
            Next
            dst0 = New cv.Mat(input.Count, 1, cv.MatType.CV_32F, input.ToArray)
        End If

        km.Run(dst0)
        dst2 = km.dst2.Clone
    End Sub
End Class









Module Kmeans_Simple_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Kmeans_Simple_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Kmeans_Simple_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Kmeans_Simple_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32, minVal As Single, maxVal As Single) As IntPtr
    End Function
End Module







Public Class Kmeans_Simple_CPP : Inherits VB_Algorithm
    Public Sub New()
        cPtr = Kmeans_Simple_Open()
        gOptions.MaxDepth.Value = 4
        desc = "Split the input into 3 levels - zero (no depth), closer to min, closer to max."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then src = task.pcSplit(2)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim mm = vbMinMax(src, task.depthMask)

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








Public Class KMeans_FloodFill : Inherits VB_Algorithm
    Dim edges As New Edge_Canny
    Public flood As New Flood_RedColor
    Public km As New KMeans_Image
    Public classCount As Integer
    Public Sub New()
        labels(2) = "FloodFill Results - click to select another region"
        desc = "Use each KMeans mask with floodfill to identify each segment in the image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        edges.Run(src)
        src.SetTo(cv.Scalar.White, edges.dst2)

        km.Run(src)
        dst3 = km.dst2
        classCount = km.classCount

        flood.Run(km.dst2)
        If task.redCells.Count = 0 Then Exit Sub ' image is likely very dark and nothing is actually seen...
        dst2 = flood.dst2

        labels(3) = CStr(task.redCells.Count) + " regions"
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
        dst2 = km.dst2
        dst2.SetTo(0, task.noDepthMask)
        dst3 = vbPalette(dst2)
        dst0 = dst2 * kCount.classCount / 255
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
        dst2 = vbPalette(km.dst2)
        classCount = km.options.kMeansK

        masks.Clear()
        counts.Clear()
        Dim k = km.options.kMeansK
        For i = 0 To k - 1
            Dim mask = km.dst0.InRange(i, i)
            masks.Add(mask)
            counts.Add(mask.CountNonZero)
        Next
        Static maskIndex As Integer
        If heartBeat() Then maskIndex += 1
        If maskIndex >= masks.Count Then maskIndex = 0
        dst3 = masks(maskIndex)
    End Sub
End Class










Public Class KMeans_Depth : Inherits VB_Algorithm
    Dim km As New KMeans_Basics
    Dim tiers As New KMeans_TierCount
    Public Sub New()
        labels(2) = "task.pcSplit(2) with no clustering."
        desc = "Cluster depth using kMeans - use KMeans_TierCount to determine 'K'"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        tiers.Run(src)

        Static kSlider = findSlider("KMeans k")
        kSlider.value = tiers.classCount
        Dim kMeansK = kSlider.Value

        km.Run(task.pcSplit(2))
        dst2 = km.dst2
        dst2.SetTo(0, task.noDepthMask)

        dst3 = vbPalette(km.dst0 * 255 / tiers.classCount)
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
        Dim gray = task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        gray.ConvertTo(grayPlus(0), cv.MatType.CV_32F)
        grayPlus(0).SetTo(0, task.noDepthMask)
        grayPlus(1) = task.pcSplit(2)

        Dim merge As New cv.Mat
        cv.Cv2.Merge(grayPlus, merge)
        km.Run(merge)

        Dim k = km.options.kMeansK
        dst3 = km.dst2
        dst3.SetTo(0, task.noDepthMask)

        If standalone Then dst2 = vbPalette(km.dst0 * 255 / k)
    End Sub
End Class








Public Class KMeans_Direct : Inherits VB_Algorithm
    Public colors As New cv.Mat
    Public buildPaletteOutput As Boolean = True
    Public options As New Options_KMeans
    Public classCount As Integer
    Public Sub New()
        labels = {"", "", "Kmeans labels for the input image", "Palette output for the kMeans labels"}
        desc = "Cluster the input using kMeans - but set the options directly.  NOTE: no options above"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone And src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        options.RunVB()
        classCount = options.kMeansK
        Static lastK = classCount
        Static saveLabels As New cv.Mat
        Dim kMeansflag = cv.KMeansFlags.UseInitialLabels
        If task.optionsChanged Or lastK <> classCount Then
            kMeansflag = cv.KMeansFlags.PpCenters
            saveLabels = New cv.Mat
        End If

        Dim columnVector = src.Reshape(src.Channels, src.Height * src.Width)
        dst2 = saveLabels

        If columnVector.ElemSize Mod 4 <> 0 Or columnVector.Type = cv.MatType.CV_32S Then columnVector.ConvertTo(columnVector, cv.MatType.CV_32F)
        Try
            cv.Cv2.Kmeans(columnVector, classCount, dst2, term, 1, kMeansflag, colors)
        Catch ex As Exception
            columnVector.SetTo(0)
            cv.Cv2.Kmeans(columnVector, classCount, dst2, term, 1, kMeansflag, colors)
            Console.WriteLine("Huge or NaN values in the input... Can happen on an initial useInitialValues run... It is corrected here...")
        End Try

        saveLabels = dst2

        dst3 = dst2.Clone
        dst2.Reshape(1, src.Height).ConvertTo(dst0, cv.MatType.CV_8U)
        dst2 = dst0 * 255 / classCount
        dst3 = vbPalette(dst2)
        lastK = classCount
    End Sub
End Class











'Public Class KMeans_AccordYinYang : Inherits VB_Algorithm
'    Public options As New Options_KMeans
'    Dim data As New Classify_YinYangData
'    Public classifications() As Integer
'    Public Sub New()
'        data.Run(Nothing)
'        dst2 = data.dst2
'        findSlider("KMeans k").Value = 2
'        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
'        labels = {"", "", "Input to KMeans", ""}
'        desc = "Accord: use the KMeans version in Accord rather than the OpenCV version."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        options.RunVB()
'        Dim k = options.kMeansK

'        Static saveK As Integer
'        If k <> saveK Then
'            saveK = k
'            Dim KMeans = New KMeans(k)

'            Dim observations(data.pointList.Count - 1)() As Double
'            Dim index As Integer
'            For Each pt In data.pointList
'                ReDim observations(index)(2 - 1)
'                observations(index)(0) = pt.X
'                observations(index)(1) = pt.Y
'                index += 1
'            Next

'            Dim Clustering As KMeansClusterCollection = KMeans.Learn(observations)
'            classifications = Clustering.Decide(observations)
'        End If

'        dst1.SetTo(0)
'        For i = 0 To data.pointList.Count - 1
'            Dim color = classifications(i) + 1
'            dst1.Circle(data.pointList(i), task.dotSize + 2, color * 255 / k, -1, task.lineType)
'        Next
'        dst3 = vbPalette(dst1)

'        labels(3) = "Output of KMeans with k = " + CStr(k)
'    End Sub
'End Class









'Public Class KMeans_Accord : Inherits VB_Algorithm
'    Public options As New Options_KMeans
'    Public classifications() As Integer
'    Public inputData()() As Double
'    Public dimension As Integer = 2
'    Public Sub New()
'        If standalone Then
'            Static data As New Classify_YinYangData
'            data.Run(Nothing)

'            ReDim inputData(data.pointList.Count - 1)
'            Dim index As Integer
'            For Each pt In data.pointList
'                ReDim inputData(index)(2 - 1)
'                inputData(index)(0) = pt.X
'                inputData(index)(1) = pt.Y
'                index += 1
'            Next
'            findSlider("KMeans k").Value = 2
'        End If
'        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
'        desc = "Accord: use the KMeans version in Accord rather than the OpenCV version."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        options.RunVB()
'        options.kMeansK = Math.Min(options.kMeansK, inputData.Length)

'        Dim KMeans = New KMeans(options.kMeansK)
'        Dim Clustering As KMeansClusterCollection = KMeans.Learn(inputData)
'        classifications = Clustering.Decide(inputData)

'        dst1.SetTo(0)
'        For i = 0 To inputData.GetUpperBound(0) - 1
'            Dim pt = New cv.Point2f(inputData(i)(0) * dst2.Width, inputData(i)(1) * dst2.Height)
'            Dim color = classifications(i) + 1
'            dst1.Circle(pt, task.dotSize + 2, color * 255 / options.kMeansK, -1, task.lineType)
'        Next

'        dst2 = vbPalette(dst1)
'        labels(2) = "Output of KMeans with k = " + CStr(options.kMeansK)
'    End Sub
'End Class








'Public Class KMeans_PlaneClusters : Inherits VB_Algorithm
'    Public rcc As New RedBP_Basics
'    Public km As New KMeans_Accord
'    Public classCount As Integer
'    Public Sub New()
'        km.dimension = 4
'        labels = {"", "Input data to KMeans", "Output of RedBP_Basics", ""}
'        desc = "Accord: Cluster the plane equations to find major planes in the image"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        If heartBeat() Then
'            rcc.Run(src)
'            dst2 = rcc.dst2
'            Dim xAxis As New List(Of Double)
'            Dim yAxis As New List(Of Double)
'            Dim zAxis As New List(Of Double)
'            Dim b As New List(Of Double)
'            For Each rc In task.redCells
'                xAxis.Add(rc.eq.Item0)
'                yAxis.Add(rc.eq.Item1)
'                zAxis.Add(rc.eq.Item2)
'                b.Add(rc.eq.Item3)
'            Next

'            Dim inputX As New cv.Mat(task.redCells.Count, 1, cv.MatType.CV_64F, xAxis.ToArray)
'            Dim inputY As New cv.Mat(task.redCells.Count, 1, cv.MatType.CV_64F, yAxis.ToArray)
'            Dim inputZ As New cv.Mat(task.redCells.Count, 1, cv.MatType.CV_64F, zAxis.ToArray)
'            Dim inputB As New cv.Mat(task.redCells.Count, 1, cv.MatType.CV_64F, b.ToArray)
'            inputX = inputX.Normalize(0, 1, cv.NormTypes.MinMax)
'            inputY = inputY.Normalize(0, 1, cv.NormTypes.MinMax)
'            inputZ = inputZ.Normalize(0, 1, cv.NormTypes.MinMax)
'            inputB = inputB.Normalize(0, 1, cv.NormTypes.MinMax)

'            Dim input As New cv.Mat
'            cv.Cv2.Merge({inputX, inputY, inputZ, inputB}, input)

'            ReDim km.inputData(task.redCells.Count - 1)
'            For i = 0 To xAxis.Count - 1
'                ReDim km.inputData(i)(km.dimension)
'                km.inputData(i)(0) = input.Get(Of Double)(i, 0)
'                km.inputData(i)(1) = input.Get(Of Double)(i, 1)
'                km.inputData(i)(2) = input.Get(Of Double)(i, 2)
'                km.inputData(i)(3) = input.Get(Of Double)(i, 3)
'            Next

'            dst1.SetTo(0)
'            For i = 0 To task.redCells.Count - 1
'                Dim pt = New cv.Point2f(km.inputData(i)(0), km.inputData(i)(1))
'                dst1.Circle(pt, task.dotSize + 2, cv.Scalar.White, -1, task.lineType)
'            Next

'            km.Run(input)
'            classCount = km.options.kMeansK
'            dst3 = km.dst2

'            labels(3) = "Clusters for the plane Data with k = " + CStr(classCount)
'        End If
'    End Sub
'End Class









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
                Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                gray.ConvertTo(gray, cv.MatType.CV_32F)
                km.Run(gray)
            Case 2 ' pointcloud x and y
                cv.Cv2.Merge({task.pcSplit(0), task.pcSplit(1)}, merge)
                km.Run(merge)
            Case 3 ' pointcloud dimensions
                cv.Cv2.Merge({task.pcSplit(0), task.pcSplit(1), task.pcSplit(2)}, merge)
                km.Run(merge)
            Case 4 ' color + depth
                src.ConvertTo(src, cv.MatType.CV_32F)
                task.pcSplit(2) = task.pcSplit(2).Normalize(0, 255, cv.NormTypes.MinMax)
                cv.Cv2.Merge({src, task.pcSplit(2)}, merge)
                km.Run(merge)
            Case 5 ' color + pcSplit(0) and pcSplit(1)
                src.ConvertTo(src, cv.MatType.CV_32F)
                task.pcSplit(0) = task.pcSplit(0).Normalize(0, 255, cv.NormTypes.MinMax)
                task.pcSplit(1) = task.pcSplit(1).Normalize(0, 255, cv.NormTypes.MinMax)
                cv.Cv2.Merge({src, task.pcSplit(0), task.pcSplit(1)}, merge)
                km.Run(merge)
            Case 6 ' color + pointcloud
                src.ConvertTo(src, cv.MatType.CV_32F)
                Dim tmp1 = task.pcSplit(0).Normalize(0, 255, cv.NormTypes.MinMax)
                Dim tmp2 = task.pcSplit(1).Normalize(0, 255, cv.NormTypes.MinMax)
                Dim tmp3 = task.pcSplit(2).Normalize(0, 255, cv.NormTypes.MinMax)
                cv.Cv2.Merge({src, tmp1, tmp2, tmp3}, merge)
                km.Run(merge)
        End Select

        labels(2) = "Dimension = " + CStr(dimSlider.value)
        labels(3) = labels(2)

        dst2 = km.dst2
        dst3 = vbPalette(dst2)
    End Sub
End Class
