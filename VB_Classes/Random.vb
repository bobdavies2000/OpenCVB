Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Random_Basics : Inherits VBparent
    Public Points(0) As cv.Point
    Public Points2f(0) As cv.Point2f
    Public rangeRect As cv.Rect
    Public plotPoints As Boolean = False
    Public countSlider As Windows.Forms.TrackBar
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Random Pixel Count", 1, dst1.Cols * dst1.Rows, 20)
            countSlider = sliders.trackbar(0)
        Else
            countSlider = findSlider("Random Pixel Count")
        End If

        rangeRect = New cv.Rect(0, 0, dst1.Cols, dst1.Rows)
        task.desc = "Create a uniform random mask with a specificied number of pixels."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 3
        If Points.Length <> countSlider.Value Then
            ReDim Points(countSlider.Value - 1)
            ReDim Points2f(countSlider.Value - 1)
        End If
        dst1.SetTo(0)
        For i = 0 To Points.Length - 1
            Dim x = msRNG.Next(rangeRect.X, rangeRect.X + rangeRect.Width)
            Dim y = msRNG.Next(rangeRect.Y, rangeRect.Y + rangeRect.Height)
            Points(i) = New cv.Point2f(x, y)
            Points2f(i) = New cv.Point2f(x, y)
            If standalone Or plotPoints = True Then dst1.Circle(Points(i), task.dotSize + 2, cv.Scalar.Gray, -1, task.lineType, 0)
        Next
    End Sub
End Class




Public Class Random_Shuffle : Inherits VBparent
    Dim myRNG As New cv.RNG
    Public Sub New()
        task.desc = "Use randomShuffle to reorder an image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        src.CopyTo(dst1)
        cv.Cv2.RandShuffle(dst1, 1.0, myRNG) ' don't remove that myRNG!  It will fail in RandShuffle.
        label1 = "Random_shuffle - wave at camera"
    End Sub
End Class



Public Class Random_LUTMask : Inherits VBparent
    Dim random As New Random_Basics
    Dim km As New KMeans_Basics
    Public Sub New()
        task.desc = "Use a random Look-Up-Table to modify few colors in a kmeans image."
        label2 = "kmeans run To Get colors"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static lutMat As cv.Mat
        If lutMat Is Nothing Or task.frameCount Mod 10 = 0 Then
            random.Run(Nothing)
            lutMat = cv.Mat.Zeros(New cv.Size(1, 256), cv.MatType.CV_8UC3)
            Dim lutIndex = 0
            km.Run(src)
            dst1 = km.dst1
            For i = 0 To random.Points.Length - 1
                Dim x = random.Points(i).X
                Dim y = random.Points(i).Y
                lutMat.Set(lutIndex, 0, dst1.Get(Of cv.Vec3b)(y, x))
                lutIndex += 1
                If lutIndex >= lutMat.Rows Then Exit For
            Next
        End If
        dst2 = src.LUT(lutMat)
        label1 = "Using kmeans colors with interpolation"
    End Sub
End Class



Public Class Random_UniformDist : Inherits VBparent
    Public Sub New()
        minVal = 0
        maxVal = 255
        task.desc = "Create a uniform distribution."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U)
        cv.Cv2.Randu(dst1, minVal, maxVal)
    End Sub
End Class



Public Class Random_NormalDist : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Random_NormalDist Blue Mean", 0, 255, 125)
            sliders.setupTrackBar(1, "Random_NormalDist Green Mean", 0, 255, 25)
            sliders.setupTrackBar(2, "Random_NormalDist Red Mean", 0, 255, 180)
            sliders.setupTrackBar(3, "Random_NormalDist Stdev", 0, 255, 50)
        End If

        If check.Setup(caller, 1) Then
            check.Box(0).Text = "Use Grayscale image"
        End If

        task.desc = "Create a normal distribution in all 3 colors with a variable standard deviation."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static grayCheck = findCheckBox("Use Grayscale image")
        If grayCheck.checked And dst1.Channels <> 1 Then dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U)
        cv.Cv2.Randn(dst1, New cv.Scalar(sliders.trackbar(0).Value, sliders.trackbar(1).Value, sliders.trackbar(2).Value), cv.Scalar.All(sliders.trackbar(3).Value))
    End Sub
End Class



Public Class Random_CheckUniformSmoothed : Inherits VBparent
    Dim histogram As New Histogram_Basics
    Dim rUniform As New Random_UniformDist
    Public Sub New()
        task.desc = "Display the smoothed histogram for a uniform distribution."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        rUniform.Run(src)
        dst1 = rUniform.dst1
        histogram.plotHist.maxRange = 255
        histogram.Run(dst1)
        dst2 = histogram.dst1
    End Sub
End Class






Public Class Random_CheckUniformDist : Inherits VBparent
    Dim histogram As New Histogram_Graph
    Dim rUniform As New Random_UniformDist
    Public Sub New()
        task.desc = "Display the histogram for a uniform distribution."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        rUniform.Run(src)
        dst1 = rUniform.dst1
        histogram.plotRequested = True
        histogram.Run(dst1)
        dst2 = histogram.dst1
    End Sub
End Class






Public Class Random_CheckNormalDist : Inherits VBparent
    Dim histogram As New Histogram_Graph
    Dim normalDist As New Random_NormalDist
    Public Sub New()
        task.desc = "Display the histogram for a Normal distribution."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        normalDist.Run(src)
        dst1 = normalDist.dst1
        histogram.plotRequested = True
        histogram.Run(dst1)
        dst2 = histogram.dst1
    End Sub
End Class





Public Class Random_CheckNormalDistSmoothed : Inherits VBparent
    Dim histogram As New Histogram_Basics
    Dim normalDist As New Random_NormalDist
    Public Sub New()
        histogram.plotHist.minRange = 1
        task.desc = "Display the histogram for a Normal distribution."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        normalDist.Run(src)
        dst1 = normalDist.dst1
        histogram.Run(dst1)
        dst2 = histogram.dst1
    End Sub
End Class





Module Random_PatternGenerator_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Random_PatternGenerator_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Random_PatternGenerator_Close(Random_PatternGeneratorPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Random_PatternGenerator_Run(Random_PatternGeneratorPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function


    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Random_DiscreteDistribution_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Random_DiscreteDistribution_Close(rPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Random_DiscreteDistribution_Run(rPtr As IntPtr, rows As Integer, cols As Integer, channels As Integer) As IntPtr
    End Function
End Module







Public Class Random_PatternGenerator_CPP : Inherits VBparent
    Dim Random_PatternGenerator As IntPtr
    Public Sub New()
        Random_PatternGenerator = Random_PatternGenerator_Open()
        task.desc = "Generate random patterns for use with 'Random Pattern Calibration'"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        Dim imagePtr = Random_PatternGenerator_Run(Random_PatternGenerator, src.Rows, src.Cols)

        If imagePtr <> 0 Then
            Dim dstData(src.Total - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, dstData)
        End If
    End Sub
    Public Sub Close()
        Random_PatternGenerator_Close(Random_PatternGenerator)
    End Sub
End Class








Public Class Random_CustomDistribution : Inherits VBparent
    Public inputCDF As cv.Mat ' place a cumulative distribution function here (or just put the histogram that reflects the desired random number distribution)
    Public outputRandom = New cv.Mat(10000, 1, cv.MatType.CV_32S, 0) ' allocate the desired number of random numbers - size can be just one to get the next random value
    Public outputHistogram As cv.Mat
    Public plotHist As New Plot_Histogram
    Public Sub New()
        Dim loadedDice() As Single = {1, 3, 0.5, 0.5, 0.75, 0.25}
        inputCDF = New cv.Mat(loadedDice.Length, 1, cv.MatType.CV_32F, loadedDice)
        task.desc = "Create a custom random number distribution from any histogram"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim lastValue = inputCDF.Get(Of Single)(inputCDF.Rows - 1, 0)
        If Not (lastValue > 0.99 And lastValue <= 1.0) Then ' convert the input histogram to a cdf.
            inputCDF *= 1 / (inputCDF.Sum().Item(0))
            For i = 1 To inputCDF.Rows - 1
                inputCDF.Set(Of Single)(i, 0, inputCDF.Get(Of Single)(i - 1, 0) + inputCDF.Get(Of Single)(i, 0))
            Next
        End If
        outputHistogram = New cv.Mat(inputCDF.Size(), cv.MatType.CV_32F, 0)
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

        If standalone Or task.intermediateName = caller Then
            plotHist.hist = outputHistogram
            plotHist.Run(src)
            dst1 = plotHist.dst1
        End If
    End Sub
End Class






' https://www.khanacademy.org/computing/computer-programming/programming-natural-simulations/programming-randomness/a/custom-distribution-of-random-numbers
Public Class Random_MonteCarlo : Inherits VBparent
    Public plotHist As New Plot_Histogram
    Public outputRandom = New cv.Mat(4000, 1, cv.MatType.CV_32S, 0) ' allocate the desired number of random numbers - size can be just one to get the next random value
    Public Sub New()
        plotHist.fixedMaxVal = 100

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Number of bins", 1, 255, 91)
        End If
        task.desc = "Generate random numbers but prefer higher values - a linearly increasing random distribution"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 2
        Dim dimension = sliders.trackbar(0).Value
        Dim histogram = New cv.Mat(dimension, 1, cv.MatType.CV_32F, 0)
        For i = 0 To outputRandom.rows - 1
            While (1)
                Dim r1 = msRNG.NextDouble()
                Dim r2 = msRNG.NextDouble()
                If r2 < r1 Then
                    Dim index = CInt(dimension * r1)
                    histogram.Set(Of Single)(index, 0, histogram.Get(Of Single)(index, 0) + 1)
                    outputRandom.set(Of Integer)(i, 0, index)
                    Exit While
                End If
            End While
        Next

        If standalone Or task.intermediateName = caller Then
            plotHist.hist = histogram
            plotHist.Run(src)
            dst1 = plotHist.dst1
        End If
    End Sub
End Class






Public Class Random_CustomHistogram : Inherits VBparent
    Public random As New Random_CustomDistribution
    Public hist As New Histogram_Simple
    Public saveHist As cv.Mat
    Public Sub New()
        random.outputRandom = New cv.Mat(1000, 1, cv.MatType.CV_32S, 0)

        label1 = "Histogram of the grayscale image"
        label2 = "Custom random distribution that reflects dst1 image"
        task.desc = "Create a random number distribution that reflects histogram of a grayscale image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        hist.plotHist.fixedMaxVal = 0 ' we are sharing the plothist with the code below...
        hist.Run(src)
        dst1 = hist.dst1.Clone()
        saveHist = hist.plotHist.hist.Clone()

        random.inputCDF = saveHist ' it will convert the histogram into a cdf where the last value must be near one.
        random.Run(src)

        If standalone Or task.intermediateName = caller Then
            hist.plotHist.fixedMaxVal = 100
            hist.plotHist.hist = random.outputHistogram
            hist.plotHist.Run(src)
            dst2 = hist.plotHist.dst1
        End If
    End Sub
End Class







' https://github.com/spmallick/learnopencv/tree/master/
Public Class Random_60sTV : Inherits VBparent
    Public Sub New()

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Range of noise to apply (from 0 to this value)", 0, 255, 50)
            sliders.setupTrackBar(1, "Percentage of pixels to include noise", 0, 100, 20)
        End If

        task.drawRect = New cv.Rect(100, 100, 100, 100)
        label1 = "Draw anywhere to select a test region"
        label2 = "Resized selection rectangle in dst1"
        task.desc = "Imitate an old TV appearance using randomness."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static valSlider = findSlider("Range of noise to apply (from 0 to this value)")
        Static threshSlider = findSlider("Percentage of pixels to include noise")
        Dim val = valSlider.value
        Dim thresh = threshSlider.value

        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = dst1(task.drawRect)
        For y = 0 To dst2.Height - 1
            For x = 0 To dst2.Width - 1
                If 255 * Rnd() <= thresh Then
                    Dim v = dst2.Get(Of Byte)(y, x)
                    dst2.Set(Of Byte)(y, x, If(2 * Rnd() = 0, Math.Min(v + (val + 1) * Rnd(), 255), Math.Max(v - (val + 1) * Rnd(), 0)))
                End If
            Next
        Next
    End Sub
End Class






' https://github.com/spmallick/learnopencv/tree/master/
Public Class Random_60sTVFaster : Inherits VBparent
    Dim random As New Random_UniformDist
    Dim mats As New Mat_4to1
    Dim options As New Random_60sTV
    Public Sub New()
        label2 = "Changed pixels, add/sub mask, plusMask, minusMask"
        task.desc = "A faster way to apply noise to imitate an old TV appearance using randomness and thresholding."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static valSlider = findSlider("Range of noise to apply (from 0 to this value)")
        Static percentSlider = findSlider("Percentage of pixels to include noise")

        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        random.Run(src)
        mats.mat(0) = random.dst1.Threshold(255 - percentSlider.value * 255 / 100, 255, cv.ThresholdTypes.Binary)
        Dim nochangeMask = random.dst1.Threshold(255 - percentSlider.value * 255 / 100, 255, cv.ThresholdTypes.BinaryInv)

        Dim valMat As New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        cv.Cv2.Randu(valMat, 0, valSlider.value)
        valMat.SetTo(0, nochangeMask)

        random.Run(src)
        Dim plusMask = random.dst1.Threshold(128, 255, cv.ThresholdTypes.Binary)
        Dim minusMask = random.dst1.Threshold(128, 255, cv.ThresholdTypes.BinaryInv)

        mats.mat(2) = plusMask
        mats.mat(3) = minusMask
        mats.mat(1) = (plusMask + minusMask).ToMat.SetTo(0, nochangeMask)

        cv.Cv2.Add(dst1, valMat, dst1, plusMask)
        cv.Cv2.Subtract(dst1, valMat, dst1, minusMask)
        mats.Run(Nothing)
        dst2 = mats.dst1
    End Sub
End Class







' https://github.com/spmallick/learnopencv/tree/master/
Public Class Random_60sTVFastSimple : Inherits VBparent
    Dim random As New Random_UniformDist
    Dim options As New Random_60sTV
    Public Sub New()
        task.desc = "Remove diagnostics from the faster algorithm to simplify code."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static valSlider = findSlider("Range of noise to apply (from 0 to this value)")
        Static percentSlider = findSlider("Percentage of pixels to include noise")

        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        random.Run(src)
        Dim nochangeMask = random.dst1.Threshold(255 - percentSlider.value * 255 / 100, 255, cv.ThresholdTypes.BinaryInv)

        dst2 = New cv.Mat(dst1.Size, cv.MatType.CV_8U)
        cv.Cv2.Randu(dst2, 0, valSlider.value)
        dst2.SetTo(0, nochangeMask)

        Dim tmp As New cv.Mat(dst1.Size, cv.MatType.CV_8U)
        cv.Cv2.Randu(tmp, 0, 255)
        Dim plusMask = tmp.Threshold(128, 255, cv.ThresholdTypes.Binary)
        Dim minusMask = tmp.Threshold(128, 255, cv.ThresholdTypes.BinaryInv)

        cv.Cv2.Add(dst1, dst2, dst1, plusMask)
        cv.Cv2.Subtract(dst1, dst2, dst1, minusMask)
        label2 = "Mat of random values < " + CStr(valSlider.value)
    End Sub
End Class






Public Class Random_KalmanPoints : Inherits VBparent
    Dim random As New Random_Basics
    Dim knn As New KNN_1_to_1FIFO
    Dim kalman As New Kalman_Basics
    Dim kalmanPoints As New List(Of cv.Point2f)
    Dim refreshPoints As Boolean = True
    Dim savePoints(0) As cv.Point
    Public Sub New()
        Dim offset = 100
        random.rangeRect = New cv.Rect(offset, offset, dst1.Width - offset * 2, dst1.Height - offset * 2)
        findSlider("Random Pixel Count").Value = 10
        task.desc = "Smoothly transition a random point from location to location."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 2
        If refreshPoints Then
            random.Run(Nothing)
            knn.lastSet = New List(Of cv.Point2f)(random.Points2f)
            random.Run(Nothing) ' now find the new locations.
            knn.currSet = New List(Of cv.Point2f)(random.Points2f)
            refreshPoints = False

            If knn.lastSet.Count * 2 <> kalman.kInput.Length Then
                ReDim kalman.kInput(knn.lastSet.Count * 2 - 1)
                ReDim savePoints(knn.currSet.Count - 1)
            End If
            For i = 0 To savePoints.Count - 1
                Dim pt = knn.lastSet(i)
                savePoints(i) = New cv.Point(CInt(pt.X), CInt(pt.Y))
                kalman.kInput(i * 2) = pt.X
                kalman.kInput(i * 2 + 1) = pt.Y
            Next
        End If

        kalman.Run(src)
        For i = 0 To kalman.kOutput.Count - 1 Step 2
            knn.currSet(i / 2) = New cv.Point2f(kalman.kOutput(i), kalman.kOutput(i + 1))
        Next

        dst1.SetTo(0)
        For i = 0 To knn.currSet.Count - 1
            dst1.Circle(knn.currSet(i), task.dotSize + 2, cv.Scalar.Yellow, -1, task.lineType)
            dst1.Circle(knn.lastSet(i), task.dotSize + 2, cv.Scalar.Red, -1, task.lineType)
        Next

        Dim noChanges As Boolean = True
        For i = 0 To savePoints.Count - 1
            Dim kPt = knn.currSet(i)
            Dim pt = New cv.Point(CInt(kPt.X), CInt(kPt.Y))
            If savePoints(i) <> pt Then noChanges = False
        Next
        If noChanges Then refreshPoints = True
    End Sub
End Class
