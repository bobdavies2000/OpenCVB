Imports cvb = OpenCvSharp
Public Class DCT_Basics : Inherits VB_Parent
    Public options As New Options_DCT
    Public Sub New()
        labels(3) = "Difference from original"
        UpdateAdvice(traceName + ": local options control the Discrete Cosine Transform'")
        desc = "Apply OpenCV's Discrete Cosine Transform to a grayscale image and use slider to remove the highest frequencies."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Options.RunOpt()

        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2Gray)

        Dim src32f As New cvb.Mat
        src.ConvertTo(src32f, cvb.MatType.CV_32F, 1 / 255)

        Dim frequencies As New cvb.Mat
        cvb.Cv2.Dct(src32f, frequencies, options.removeFrequency)

        Dim roi As New cvb.Rect(0, 0, options.removeFrequency, src32f.Height)
        If roi.Width > 0 Then frequencies(roi).SetTo(0)
        labels(2) = "Frequencies below " + CStr(options.removeFrequency) + " removed"

        cvb.Cv2.Dct(frequencies, src32f, cvb.DctFlags.Inverse)
        src32f.ConvertTo(dst2, cvb.MatType.CV_8UC1, 255)

        cvb.Cv2.Subtract(src, dst2, dst3)
    End Sub
End Class





Public Class DCT_RGB : Inherits VB_Parent
    Public dct As New DCT_Basics
    Public Sub New()
        labels(3) = "Difference from original"
        desc = "Apply OpenCV's Discrete Cosine Transform to a BGR image and use slider to remove the highest frequencies."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dct.Options.RunOpt()

        Dim srcPlanes = src.Split()

        Dim freqPlanes(2) As cvb.Mat
        For i = 0 To srcPlanes.Count - 1
            Dim src32f As New cvb.Mat
            srcPlanes(i).ConvertTo(src32f, cvb.MatType.CV_32FC3, 1 / 255)
            freqPlanes(i) = New cvb.Mat
            cvb.Cv2.Dct(src32f, freqPlanes(i), cvb.DctFlags.None)

            Dim roi As New cvb.Rect(0, 0, dct.options.removeFrequency, src32f.Height)
            If roi.Width > 0 Then freqPlanes(i)(roi).SetTo(0)

            cvb.Cv2.Dct(freqPlanes(i), src32f, dct.options.dctFlag)
            src32f.ConvertTo(srcPlanes(i), cvb.MatType.CV_8UC1, 255)
        Next
        labels(2) = dct.labels(2)

        cvb.Cv2.Merge(srcPlanes, dst2)

        cvb.Cv2.Subtract(src, dst2, dst3)
    End Sub
End Class




Public Class DCT_Depth : Inherits VB_Parent
    Dim dct As New DCT_Basics
    Public Sub New()
        labels(3) = "Subtract DCT inverse from Grayscale depth"
        desc = "Find featureless surfaces in the depth data - expected to be useful only on the K4A for Azure camera."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim gray = task.depthRGB.CvtColor(cvb.ColorConversionCodes.BGR2Gray)
        Dim frequencies As New cvb.Mat
        Dim src32f As New cvb.Mat
        gray.ConvertTo(src32f, cvb.MatType.CV_32F, 1 / 255)
        cvb.Cv2.Dct(src32f, frequencies, dct.options.dctFlag)

        Dim roi As New cvb.Rect(0, 0, dct.options.removeFrequency, src32f.Height)
        If roi.Width > 0 Then frequencies(roi).SetTo(0)
        labels(2) = dct.labels(2)

        cvb.Cv2.Dct(frequencies, src32f, cvb.DctFlags.Inverse)
        src32f.ConvertTo(dst2, cvb.MatType.CV_8UC1, 255)

        cvb.Cv2.Subtract(gray, dst2, dst3)
    End Sub
End Class





Public Class DCT_FeatureLess : Inherits VB_Parent
    Public dct As New DCT_Basics
    Public Sub New()
        desc = "Find surfaces that lack any texture.  Remove just the highest frequency from the DCT to get horizontal lines through the image."
        labels(3) = "FeatureLess BGR regions"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dct.Run(src)

        dst2.SetTo(0)
        For i = 0 To dct.dst2.Rows - 1
            Dim runLen = 0
            Dim runStart = 0
            For j = 1 To dct.dst2.Cols - 1
                If dct.dst2.Get(Of Byte)(i, j) = dct.dst2.Get(Of Byte)(i, j - 1) Then
                    runLen += 1
                Else
                    If runLen > dct.options.runLengthMin Then
                        Dim roi = New cvb.Rect(runStart, i, runLen, 1)
                        dst2(roi).SetTo(255)
                    End If
                    runStart = j
                    runLen = 1
                End If
            Next
        Next

        dst3.SetTo(0)
        If dst2.Channels() = 3 Then
            dst2 = dst2.CvtColor(cvb.ColorConversionCodes.BGR2Gray).Threshold(1, 255, cvb.ThresholdTypes.Binary)
        Else
            dst2 = dst2.Threshold(1, 255, cvb.ThresholdTypes.Binary)
        End If
        src.CopyTo(dst3, Not dst2)
        labels(2) = "Mask of DCT with highest frequency removed"
    End Sub
End Class





Public Class DCT_Surfaces_debug : Inherits VB_Parent
    Dim mats As New Mat_4to1
    Dim dct As New DCT_FeatureLess
    Dim flow As New Font_FlowText
    Dim plane As New Plane_CellColor
    Public Sub New()
        flow.parentData = Me
        labels = {"", "", "Stats on the largest region below DCT threshold", "Various views of regions with DCT below threshold"}
        task.gOptions.displayDst0.Checked = False
        desc = "Find plane equation for a featureless surface - debugging one region for now."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        ' If task.heartBeat Then flow.msgs.Clear()

        mats.mat(0) = src.Clone
        mats.mat(0).SetTo(cvb.Scalar.White, task.gridMask)

        dct.Run(src)
        mats.mat(1) = dct.dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR).Clone()
        mats.mat(2) = dct.dst3.Clone()

        Dim mask = dct.dst2.Clone() ' result1 contains the DCT mask of featureless surfaces.
        task.pcSplit(2).SetTo(0, Not mask) ' remove non-featureless surface depth data.

        ' find the most featureless roi
        Dim maxIndex As Integer
        Dim roiCounts(task.gridRects.Count - 1)
        For i = 0 To task.gridRects.Count - 1
            roiCounts(i) = mask(task.gridRects(i)).CountNonZero
            If roiCounts(i) > roiCounts(maxIndex) Then maxIndex = i
        Next

        mats.mat(3) = New cvb.Mat(src.Size(), cvb.MatType.CV_8UC3, cvb.Scalar.All(0))
        src(task.gridRects(maxIndex)).CopyTo(mats.mat(3)(task.gridRects(maxIndex)), mask(task.gridRects(maxIndex)))
        mats.Run(empty)
        dst3 = mats.dst2

        Dim roi = task.gridRects(maxIndex) ' this is where the debug comes in.  We just want to look at one region which hopefully is a single plane.
        If roi.X = task.gridRects(maxIndex).X And roi.Y = task.gridRects(maxIndex).Y Then
            If roiCounts(maxIndex) > roi.Width * roi.Height / 4 Then
                Dim fitPoints As New List(Of cvb.Point3f)
                Dim minDepth = Single.MaxValue, maxDepth = Single.MinValue
                For j = 0 To roi.Height - 1
                    For i = 0 To roi.Width - 1
                        Dim nextD = task.pcSplit(2)(roi).Get(Of Single)(j, i)
                        If nextD <> 0 Then
                            If minDepth > nextD Then minDepth = nextD
                            If maxDepth < nextD Then maxDepth = nextD
                            Dim wpt = New cvb.Point3f(roi.X + i, roi.Y + j, nextD)
                            fitPoints.Add(getWorldCoordinates(wpt))
                        End If
                    Next
                Next
                If fitPoints.Count > 0 Then
                    Dim eq = fitDepthPlane(fitPoints)
                    If Single.IsNaN(eq(0)) = False Then
                        flow.nextMsg = "a=" + Format(eq(0), fmt2) + " b=" + Format(eq(1), fmt2) + " c=" + Format(Math.Abs(eq(2)), fmt2) +
                              vbTab + "depth=" + Format(-eq(3), fmt2) + "m " + "roi(x,y) = " + Format(roi.X, "000") + "," +
                              Format(roi.Y, "000") + vbTab + "Min=" + Format(minDepth, fmt1) + "m " + " Max=" + Format(maxDepth, fmt1) + "m"
                    End If
                End If
            End If
        End If
        flow.Run(empty)
    End Sub
End Class