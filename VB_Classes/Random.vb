Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Public Class Random_Basics : Inherits VB_Parent
    Public PointList As New List(Of cvb.Point2f)
    Public range As cvb.Rect
    Public options As New Options_Random
    Public Sub New()
        range = New cvb.Rect(0, 0, dst2.Cols, dst2.Rows)
        desc = "Create a uniform random mask with a specificied number of pixels."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        options.RunVB()

        Dim sizeRequest = options.count
        If task.paused = False Then
            pointList.Clear()
            While pointList.Count < sizeRequest
                pointList.Add(New cvb.Point2f(msRNG.Next(range.X, range.X + range.Width),
                                             msRNG.Next(range.Y, range.Y + range.Height)))
            End While
            If standaloneTest() Then
                dst2.SetTo(0)
                For Each pt In pointList
                    DrawCircle(dst2, pt, task.DotSize, cvb.Scalar.Yellow)
                Next
            End If
        End If
    End Sub
End Class





Public Class Random_Point2d : Inherits VB_Parent
    Public PointList As New List(Of cvb.Point2d)
    Public range As cvb.Rect
    Dim options As New Options_Random
    Public Sub New()
        range = New cvb.Rect(0, 0, dst2.Cols, dst2.Rows)
        desc = "Create a uniform random mask with a specificied number of pixels."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        options.RunVB()

        PointList.Clear()
        If task.paused = False Then
            For i = 0 To options.count - 1
                PointList.Add(New cvb.Point2d(msRNG.Next(range.X, range.X + range.Width), msRNG.Next(range.Y, range.Y + range.Height)))
            Next
            If standaloneTest() Then
                dst2.SetTo(0)
                For Each pt In PointList
                    dst2.Circle(pt, task.DotSize, cvb.Scalar.Yellow, -1, task.lineType)
                Next
            End If
        End If
    End Sub
End Class






Public Class Random_Enumerable : Inherits VB_Parent
    Public options As New Options_Random
    Public points() As cvb.Point2f
    Public Sub New()
        FindSlider("Random Pixel Count").Value = 100
        desc = "Create an enumerable list of points using a lambda function"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        options.RunVB()

        points = Enumerable.Range(0, options.count).Select(Of cvb.Point2f)(
            Function(i)
                Return New cvb.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            End Function).ToArray
        dst2.SetTo(0)
        For Each pt In points
            DrawCircle(dst2, pt, task.DotSize, cvb.Scalar.Yellow)
        Next
    End Sub
End Class





Public Class Random_Basics3D : Inherits VB_Parent
    Public Points3f() As cvb.Point3f
    Dim options As New Options_Random
    Public PointList As New List(Of cvb.Point3f)
    Public ranges() As Single = {0, dst2.Width, 0, dst2.Height, 0, task.MaxZmeters}
    Public Sub New()
        FindSlider("Random Pixel Count").Value = 20
        FindSlider("Random Pixel Count").Maximum = dst2.Cols * dst2.Rows
        desc = "Create a uniform random mask with a specificied number of pixels."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        options.RunVB()

        PointList.Clear()
        If task.paused = False Then
            For i = 0 To options.count - 1
                PointList.Add(New cvb.Point3f(msRNG.Next(ranges(0), ranges(1)), msRNG.Next(ranges(2), ranges(3)), msRNG.Next(ranges(4), ranges(5))))
            Next
            If standaloneTest() Then
                dst2.SetTo(0)
                For Each pt In PointList
                    DrawCircle(dst2, New cvb.Point2f(pt.X, pt.Y), task.DotSize, cvb.Scalar.Yellow)
                Next
            End If
            Points3f = PointList.ToArray
        End If
    End Sub
End Class






Public Class Random_Basics4D : Inherits VB_Parent
    Public vec4f() As cvb.Vec4f
    Public PointList As New List(Of cvb.Vec4f)
    Public ranges() As Single = {0, dst2.Width, 0, dst2.Height, 0, task.MaxZmeters, 0, task.MaxZmeters}
    Dim options As New Options_Random
    Dim countSlider As System.Windows.Forms.TrackBar
    Public Sub New()
        desc = "Create a uniform random mask with a specificied number of pixels."
        countSlider = FindSlider("Random Pixel Count")
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        PointList.Clear()
        Dim count = countSlider.Value
        If task.paused = False Then
            For i = 0 To count - 1
                PointList.Add(New cvb.Vec4f(msRNG.Next(ranges(0), ranges(1)), msRNG.Next(ranges(2), ranges(3)),
                                           msRNG.Next(ranges(4), ranges(5)), msRNG.Next(ranges(6), ranges(7))))
            Next
            If standaloneTest() Then
                dst2.SetTo(0)
                For Each v In PointList
                    DrawCircle(dst2, New cvb.Point2f(v(0), v(1)), task.DotSize, cvb.Scalar.Yellow)
                Next
            End If
            vec4f = PointList.ToArray
        End If
    End Sub
End Class





Public Class Random_Shuffle : Inherits VB_Parent
    Dim myRNG As New cvb.RNG
    Public Sub New()
        desc = "Use randomShuffle to reorder an image."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        src.CopyTo(dst2)
        cvb.Cv2.RandShuffle(dst2, 1.0, myRNG) ' don't remove that myRNG!  It will fail in RandShuffle.
        labels(2) = "Random_shuffle - wave at camera"
    End Sub
End Class






Public Class Random_LUTMask : Inherits VB_Parent
    Dim random As New Random_Basics
    Dim km As New KMeans_Image
    Dim lutMat As cvb.Mat
    Public Sub New()
        desc = "Use a random Look-Up-Table to modify few colors in a kmeans image."
        labels(3) = "kmeans run to get colors"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        If task.heartBeat Or task.frameCount < 10 Then
            random.Run(empty)
            lutMat = New cvb.Mat(New cvb.Size(1, 256), cvb.MatType.CV_8UC3, cvb.Scalar.All(0))
            Dim lutIndex = 0
            km.Run(src)
            dst2 = km.dst2
            For Each pt In random.PointList
                lutMat.Set(lutIndex, 0, dst2.Get(Of cvb.Vec3b)(pt.Y, pt.X))
                lutIndex += 1
                If lutIndex >= lutMat.Rows Then Exit For
            Next
        End If

        dst3 = src.LUT(lutMat)
        labels(2) = "Using kmeans colors with interpolation"
    End Sub
End Class






Public Class Random_UniformDist : Inherits VB_Parent
    Dim minVal As Double = 0, maxVal As Double = 255
    Public Sub New()
        desc = "Create a uniform distribution."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        cvb.Cv2.Randu(dst2, minVal, maxVal)
    End Sub
End Class






Public Class Random_NormalDist : Inherits VB_Parent
    Dim options As New Options_NormalDist
    Public Sub New()
        desc = "Create a normal distribution in all 3 colors with a variable standard deviation."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        options.RunVB()

        If options.grayChecked And dst2.Channels() <> 1 Then dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U)
        cvb.Cv2.Randn(dst2, New cvb.Scalar(options.blueVal, options.greenVal, options.redVal), cvb.Scalar.All(options.stdev))
    End Sub
End Class






Public Class Random_CheckUniformSmoothed : Inherits VB_Parent
    Dim histogram As New Hist_Basics
    Dim rUniform As New Random_UniformDist
    Public Sub New()
        desc = "Display the smoothed histogram for a uniform distribution."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        rUniform.Run(src)
        dst2 = rUniform.dst2
        histogram.plot.maxRange = 255
        histogram.Run(dst2)
        dst3 = histogram.dst2
    End Sub
End Class






Public Class Random_CheckUniformDist : Inherits VB_Parent
    Dim histogram As New Hist_Graph
    Dim rUniform As New Random_UniformDist
    Public Sub New()
        desc = "Display the histogram for a uniform distribution."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        rUniform.Run(src)
        dst2 = rUniform.dst2
        histogram.plotRequested = True
        histogram.Run(dst2)
        dst3 = histogram.dst2
    End Sub
End Class






Public Class Random_CheckNormalDist : Inherits VB_Parent
    Dim histogram As New Hist_Graph
    Dim normalDist As New Random_NormalDist
    Public Sub New()
        desc = "Display the histogram for a Normal distribution."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        normalDist.Run(src)
        dst3 = normalDist.dst2
        histogram.plotRequested = True
        histogram.Run(dst3)
        dst2 = histogram.dst2
    End Sub
End Class





Public Class Random_CheckNormalDistSmoothed : Inherits VB_Parent
    Dim histogram As New Hist_Basics
    Dim normalDist As New Random_NormalDist
    Public Sub New()
        histogram.plot.minRange = 1
        desc = "Display the histogram for a Normal distribution."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        normalDist.Run(src)
        dst3 = normalDist.dst2
        histogram.Run(dst3)
        dst2 = histogram.dst2
    End Sub
End Class







Public Class Random_PatternGenerator_CPP_VB : Inherits VB_Parent
    Public Sub New()
        cPtr = Random_PatternGenerator_Open()
        desc = "Generate random patterns for use with 'Random Pattern Calibration'"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim imagePtr = Random_PatternGenerator_Run(cPtr, src.Rows, src.Cols)
        dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8UC1, imagePtr).Clone
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Random_PatternGenerator_Close(cPtr)
    End Sub
End Class








Public Class Random_CustomDistribution : Inherits VB_Parent
    Public inputCDF As cvb.Mat ' place a cumulative distribution function here (or just put the histogram that reflects the desired random number distribution)
    Public outputRandom = New cvb.Mat(10000, 1, cvb.MatType.CV_32S, cvb.Scalar.All(0)) ' allocate the desired number of random numbers - size can be just one to get the next random value
    Public outputHistogram As cvb.Mat
    Public plot As New Plot_Histogram
    Public Sub New()
        Dim loadedDice() As Single = {1, 3, 0.5, 0.5, 0.75, 0.25}
        inputCDF = cvb.Mat.FromPixelData(loadedDice.Length, 1, cvb.MatType.CV_32F, loadedDice)
        desc = "Create a custom random number distribution from any histogram"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        Dim lastValue = inputCDF.Get(Of Single)(inputCDF.Rows - 1, 0)
        If Not (lastValue > 0.99 And lastValue <= 1.0) Then ' convert the input histogram to a cdf.
            inputCDF *= 1 / (inputCDF.Sum()(0))
            For i = 1 To inputCDF.Rows - 1
                inputCDF.Set(Of Single)(i, 0, inputCDF.Get(Of Single)(i - 1, 0) + inputCDF.Get(Of Single)(i, 0))
            Next
        End If
        outputHistogram = New cvb.Mat(inputCDF.Size(), cvb.MatType.CV_32F, cvb.Scalar.All(0))
        Dim size = outputHistogram.Rows
        For i = 0 To outputRandom.rows - 1
            Dim uniformR1 = msRNG.NextDouble()
            For j = 0 To size - 1
                If uniformR1 < inputCDF.Get(Of Single)(j, 0) Then
                    outputHistogram.Set(Of Single)(j, 0, outputHistogram.Get(Of Single)(j, 0) + 1)
                    outputRandom.set(Of Integer)(i, 0, j) ' the output is an integer reflecting a bin in the histogram.
                    Exit For
                End If
            Next
        Next

        plot.Run(outputHistogram)
        dst2 = plot.dst2
    End Sub
End Class






' https://www.khanacademy.org/computing/computer-programming/programming-natural-simulations/programming-randomness/a/custom-distribution-of-random-numbers
Public Class Random_MonteCarlo : Inherits VB_Parent
    Public plot As New Plot_Histogram
    Dim options As New Options_MonteCarlo
    Public outputRandom = New cvb.Mat(New cvb.Size(1, 4000), cvb.MatType.CV_32S, 0) ' allocate the desired number of random numbers - size can be just one to get the next random value
    Public Sub New()
        plot.maxValue = 100
        desc = "Generate random numbers but prefer higher values - a linearly increasing random distribution"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        options.RunVB()

        Dim histogram = New cvb.Mat(options.dimension, 1, cvb.MatType.CV_32F, cvb.Scalar.All(0))
        For i = 0 To outputRandom.rows - 1
            While (1)
                Dim r1 = msRNG.NextDouble()
                Dim r2 = msRNG.NextDouble()
                If r2 < r1 Then
                    Dim index = CInt(options.dimension * r1)
                    histogram.Set(Of Single)(index, 0, histogram.Get(Of Single)(index, 0) + 1)
                    outputRandom.set(Of Integer)(i, 0, index)
                    Exit While
                End If
            End While
        Next

        If standaloneTest() Then
            plot.Run(histogram)
            dst2 = plot.dst2
        End If
    End Sub
End Class






Public Class Random_CustomHistogram : Inherits VB_Parent
    Public random As New Random_CustomDistribution
    Public hist As New Hist_Simple
    Public saveHist As cvb.Mat
    Public Sub New()
        random.outputRandom = New cvb.Mat(1000, 1, cvb.MatType.CV_32S, cvb.Scalar.All(0))

        labels(2) = "Histogram of the grayscale image"
        labels(3) = "Custom random distribution that reflects dst2 image"
        desc = "Create a random number distribution that reflects histogram of a grayscale image"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        hist.plot.maxValue = 0 ' we are sharing the plot with the code below...
        hist.Run(src)
        dst2 = hist.dst2.Clone()
        saveHist = hist.plot.histogram.Clone()

        random.inputCDF = saveHist ' it will convert the histogram into a cdf where the last value must be near one.
        random.Run(src)

        If standaloneTest() Then
            hist.plot.maxValue = 100
            hist.plot.Run(random.outputHistogram)
            dst3 = hist.plot.dst2
        End If
    End Sub
End Class







' https://github.com/spmallick/learnopencv/tree/master/
Public Class Random_StaticTV : Inherits VB_Parent
    Dim options As New Options_StaticTV
    Public Sub New()
        task.drawRect = New cvb.Rect(10, 10, 50, 50)
        labels(2) = "Draw anywhere to select a test region"
        labels(3) = "Resized selection rectangle in dst2"
        desc = "Imitate an old TV appearance using randomness."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        options.RunVB()

        dst2 = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        dst3 = dst2(task.drawRect)
        For y = 0 To dst3.Height - 1
            For x = 0 To dst3.Width - 1
                If 255 * Rnd() <= options.threshPercent Then
                    Dim v = dst3.Get(Of Byte)(y, x)
                    dst3.Set(Of Byte)(y, x, If(2 * Rnd() = 0, Math.Min(v + (options.rangeVal + 1) * Rnd(), 255),
                                                              Math.Max(v - (options.rangeVal + 1) * Rnd(), 0)))
                End If
            Next
        Next
    End Sub
End Class






' https://github.com/spmallick/learnopencv/tree/master/
Public Class Random_StaticTVFaster : Inherits VB_Parent
    Dim random As New Random_UniformDist
    Dim mats As New Mat_4to1
    Dim options As New Random_StaticTV
    Public Sub New()
        labels(3) = "Changed pixels, add/sub mask, plusMask, minusMask"
        desc = "A faster way to apply noise to imitate an old TV appearance using randomness and thresholding."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        Static valSlider = FindSlider("Range of noise to apply (from 0 to this value)")
        Static percentSlider = FindSlider("Percentage of pixels to include noise")

        dst2 = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        random.Run(src)
        mats.mat(0) = random.dst2.Threshold(255 - percentSlider.Value * 255 / 100, 255, cvb.ThresholdTypes.Binary)
        Dim nochangeMask = random.dst2.Threshold(255 - percentSlider.Value * 255 / 100, 255, cvb.ThresholdTypes.BinaryInv)

        Dim valMat As New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        cvb.Cv2.Randu(valMat, cvb.Scalar.All(0), cvb.Scalar.All(valSlider.Value))
        valMat.SetTo(0, nochangeMask)

        random.Run(src)
        Dim plusMask = random.dst2.Threshold(128, 255, cvb.ThresholdTypes.Binary)
        Dim minusMask = random.dst2.Threshold(128, 255, cvb.ThresholdTypes.BinaryInv)

        mats.mat(2) = plusMask
        mats.mat(3) = minusMask
        mats.mat(1) = (plusMask + minusMask).ToMat.SetTo(0, nochangeMask)

        cvb.Cv2.Add(dst2, valMat, dst2, plusMask)
        cvb.Cv2.Subtract(dst2, valMat, dst2, minusMask)
        mats.Run(empty)
        dst3 = mats.dst2
    End Sub
End Class







' https://github.com/spmallick/learnopencv/tree/master/
Public Class Random_StaticTVFastSimple : Inherits VB_Parent
    Dim random As New Random_UniformDist
    Dim options As New Random_StaticTV
    Public Sub New()
        desc = "Remove diagnostics from the faster algorithm to simplify code."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        Static valSlider = FindSlider("Range of noise to apply (from 0 to this value)")
        Static percentSlider = FindSlider("Percentage of pixels to include noise")

        dst2 = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        random.Run(src)
        Dim nochangeMask = random.dst2.Threshold(255 - percentSlider.Value * 255 / 100, 255, cvb.ThresholdTypes.BinaryInv)

        dst3 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U)
        cvb.Cv2.Randu(dst3, cvb.Scalar.All(0), cvb.Scalar.All(valSlider.Value))
        dst3.SetTo(0, nochangeMask)

        Dim tmp As New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U)
        cvb.Cv2.Randu(tmp, 0, 255)
        Dim plusMask = tmp.Threshold(128, 255, cvb.ThresholdTypes.Binary)
        Dim minusMask = tmp.Threshold(128, 255, cvb.ThresholdTypes.BinaryInv)

        cvb.Cv2.Add(dst2, dst3, dst2, plusMask)
        cvb.Cv2.Subtract(dst2, dst3, dst2, minusMask)
        labels(3) = "Mat of random values < " + CStr(valSlider.Value)
    End Sub
End Class






Public Class Random_KalmanPoints : Inherits VB_Parent
    Dim random As New Random_Basics
    Dim kalman As New Kalman_Basics
    Dim targetSet As New List(Of cvb.Point2f)
    Dim currSet As New List(Of cvb.Point2f)
    Dim refreshPoints As Boolean = True
    Public Sub New()
        Dim offset = dst2.Width / 5
        random.range = New cvb.Rect(offset, offset, Math.Abs(dst2.Width - offset * 2), Math.Abs(dst2.Height - offset * 2))
        FindSlider("Random Pixel Count").Value = 10
        desc = "Smoothly transition a random point from location to location."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        If refreshPoints Then
            random.Run(empty)
            targetSet = New List(Of cvb.Point2f)(random.PointList)
            currSet = New List(Of cvb.Point2f)(random.PointList) ' just to get the updated size
            refreshPoints = False

            If targetSet.Count * 2 <> kalman.kInput.Length Then ReDim kalman.kInput(targetSet.Count * 2 - 1)

        End If

        For i = 0 To targetSet.Count - 1
            Dim pt As cvb.Point = targetSet(i)
            kalman.kInput(i * 2) = pt.X
            kalman.kInput(i * 2 + 1) = pt.Y
        Next

        kalman.Run(src)
        For i = 0 To kalman.kOutput.Count - 1 Step 2
            currSet(i / 2) = New cvb.Point(kalman.kOutput(i), kalman.kOutput(i + 1))
        Next

        dst2.SetTo(0)
        For i = 0 To currSet.Count - 1
            DrawCircle(dst2, currSet(i), task.DotSize + 2, cvb.Scalar.Yellow)
            DrawCircle(dst2, targetSet(i), task.DotSize + 2, cvb.Scalar.Red)
        Next

        Dim noChanges As Boolean = True
        For i = 0 To currSet.Count - 1
            Dim pt = currSet(i)
            If Math.Abs(targetSet(i).X - pt.X) > 1 And Math.Abs(targetSet(i).Y - pt.Y) > 1 Then noChanges = False
        Next
        If noChanges Then refreshPoints = True
    End Sub
End Class









Public Class Random_Clusters : Inherits VB_Parent
    Public clusterLabels As New List(Of List(Of Integer))
    Public clusters As New List(Of List(Of cvb.Point2f))
    Dim options As New Options_Clusters
    Public Sub New()
        task.scalarColors(0) = cvb.Scalar.Yellow
        task.scalarColors(1) = cvb.Scalar.Blue
        task.scalarColors(2) = cvb.Scalar.Red
        labels = {"", "", "Colorized sets", ""}
        desc = "Use OpenCV's randN API to create a cluster around a random mean with a requested stdev"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        If Not task.heartBeat Then Exit Sub
        options.RunVB()

        Dim ptMat As cvb.Mat = New cvb.Mat(1, 1, cvb.MatType.CV_32FC2)
        dst2.SetTo(0)
        clusters.Clear()
        clusterLabels.Clear()
        For i = 0 To options.numClusters - 1
            Dim mean = New cvb.Scalar(msRNG.Next(dst2.Width / 8, dst2.Width * 7 / 8), msRNG.Next(dst2.Height / 8, dst2.Height * 7 / 8), 0)
            Dim cList As New List(Of cvb.Point2f)
            Dim labelList As New List(Of Integer)
            For j = 0 To options.numPoints - 1
                cvb.Cv2.Randn(ptMat, mean, cvb.Scalar.All(options.stdev))
                Dim pt = ptMat.Get(Of cvb.Point2f)(0, 0)
                If pt.X < 0 Then pt.X = 0
                If pt.X >= dst2.Width Then pt.X = dst2.Width - 1
                If pt.Y < 0 Then pt.Y = 0
                If pt.Y >= dst2.Height Then pt.Y = dst2.Height - 1
                DrawCircle(dst2, pt, task.DotSize, task.scalarColors(i Mod 256))

                cList.Add(pt)
                labelList.Add(i)
            Next
            clusterLabels.Add(labelList)
            clusters.Add(cList)
        Next
    End Sub
End Class