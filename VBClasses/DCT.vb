Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class DCT_Basics : Inherits TaskParent
        Public options As New Options_DCT
        Public Sub New()
            labels(3) = "Difference from original"
            desc = "Apply OpenCV's Discrete Cosine Transform to a grayscale image and use slider to remove the highest frequencies."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim src32f As New cv.Mat
            taskAlg.gray.ConvertTo(src32f, cv.MatType.CV_32F, 1 / 255)

            Dim frequencies As New cv.Mat
            cv.Cv2.Dct(src32f, frequencies, options.removeFrequency)

            Dim roi As New cv.Rect(0, 0, options.removeFrequency, src32f.Height)
            If roi.Width > 0 Then frequencies(roi).SetTo(0)
            labels(2) = "Frequencies below " + CStr(options.removeFrequency) + " removed"

            cv.Cv2.Dct(frequencies, src32f, cv.DctFlags.Inverse)
            src32f.ConvertTo(dst2, cv.MatType.CV_8UC1, 255)

            cv.Cv2.Subtract(taskAlg.gray, dst2, dst3)
        End Sub
    End Class





    Public Class DCT_RGB : Inherits TaskParent
        Public dct As New DCT_Basics
        Public Sub New()
            labels(3) = "Difference from original"
            desc = "Apply OpenCV's Discrete Cosine Transform to a BGR image and use slider to remove the highest frequencies."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dct.Options.Run()

            Dim srcPlanes = src.Split()

            Dim freqPlanes(2) As cv.Mat
            For i = 0 To srcPlanes.Count - 1
                Dim src32f As New cv.Mat
                srcPlanes(i).ConvertTo(src32f, cv.MatType.CV_32FC3, 1 / 255)
                freqPlanes(i) = New cv.Mat
                cv.Cv2.Dct(src32f, freqPlanes(i), cv.DctFlags.None)

                Dim roi As New cv.Rect(0, 0, dct.options.removeFrequency, src32f.Height)
                If roi.Width > 0 Then freqPlanes(i)(roi).SetTo(0)

                cv.Cv2.Dct(freqPlanes(i), src32f, dct.options.dctFlag)
                src32f.ConvertTo(srcPlanes(i), cv.MatType.CV_8UC1, 255)
            Next
            labels(2) = dct.labels(2)

            cv.Cv2.Merge(srcPlanes, dst2)

            cv.Cv2.Subtract(src, dst2, dst3)
        End Sub
    End Class




    Public Class DCT_Depth : Inherits TaskParent
        Dim dct As New DCT_Basics
        Public Sub New()
            labels(3) = "Subtract DCT inverse from Grayscale depth"
            desc = "Find featureless surfaces in the depth data - expected to be useful only on the K4A for Azure camera."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim gray = taskAlg.depthRGB.CvtColor(cv.ColorConversionCodes.BGR2Gray)
            Dim frequencies As New cv.Mat
            Dim src32f As New cv.Mat
            gray.ConvertTo(src32f, cv.MatType.CV_32F, 1 / 255)
            cv.Cv2.Dct(src32f, frequencies, dct.options.dctFlag)

            Dim roi As New cv.Rect(0, 0, dct.options.removeFrequency, src32f.Height)
            If roi.Width > 0 Then frequencies(roi).SetTo(0)
            labels(2) = dct.labels(2)

            cv.Cv2.Dct(frequencies, src32f, cv.DctFlags.Inverse)
            src32f.ConvertTo(dst2, cv.MatType.CV_8UC1, 255)

            cv.Cv2.Subtract(gray, dst2, dst3)
        End Sub
    End Class





    Public Class DCT_FeatureLess : Inherits TaskParent
        Public dct As New DCT_Basics
        Public Sub New()
            desc = "Find surfaces that lack any texture.  Remove just the highest frequency from the DCT to get horizontal lines through the image."
            labels(3) = "FeatureLess BGR regions"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
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
                            Dim roi = New cv.Rect(runStart, i, runLen, 1)
                            dst2(roi).SetTo(255)
                        End If
                        runStart = j
                        runLen = 1
                    End If
                Next
            Next

            dst3.SetTo(0)
            If dst2.Channels() = 3 Then
                dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2Gray).Threshold(1, 255, cv.ThresholdTypes.Binary)
            Else
                dst2 = dst2.Threshold(1, 255, cv.ThresholdTypes.Binary)
            End If
            src.CopyTo(dst3, Not dst2)
            labels(2) = "Mask of DCT with highest frequency removed"
        End Sub
    End Class





    Public Class DCT_Surfaces_debug : Inherits TaskParent
        Dim mats As New Mat_4to1
        Dim dct As New DCT_FeatureLess
        Dim flow As New Font_FlowText
        Dim plane As New Plane_CellColor
        Public Sub New()
            flow.parentData = Me
            labels = {"", "", "Stats on the largest region below DCT threshold", "Various views of regions with DCT below threshold"}
            If standalone Then taskAlg.gOptions.displayDst0.Checked = False
            desc = "Find plane equation for a featureless surface - debugging one region for now."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            ' If taskAlg.heartBeat Then flow.msgs.Clear()

            mats.mat(0) = src.Clone
            mats.mat(0).SetTo(white, taskAlg.gridMask)

            dct.Run(src)
            mats.mat(1) = dct.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR).Clone()
            mats.mat(2) = dct.dst3.Clone()

            Dim mask = dct.dst2.Clone() ' result1 contains the DCT mask of featureless surfaces.
            taskAlg.pcSplit(2).SetTo(0, Not mask) ' remove non-featureless surface depth data.

            ' find the most featureless roi
            Dim maxIndex As Integer
            Dim roiCounts(taskAlg.gridRects.Count - 1)
            For i = 0 To taskAlg.gridRects.Count - 1
                roiCounts(i) = mask(taskAlg.gridRects(i)).CountNonZero
                If roiCounts(i) > roiCounts(maxIndex) Then maxIndex = i
            Next

            mats.mat(3) = New cv.Mat(src.Size(), cv.MatType.CV_8UC3, cv.Scalar.All(0))
            src(taskAlg.gridRects(maxIndex)).CopyTo(mats.mat(3)(taskAlg.gridRects(maxIndex)), mask(taskAlg.gridRects(maxIndex)))
            mats.Run(emptyMat)
            dst3 = mats.dst2

            Dim roi = taskAlg.gridRects(maxIndex) ' this is where the debug comes in.  We just want to look at one region which hopefully is a single plane.
            If roi.X = taskAlg.gridRects(maxIndex).X And roi.Y = taskAlg.gridRects(maxIndex).Y Then
                If roiCounts(maxIndex) > roi.Width * roi.Height / 4 Then
                    Dim fitPoints As New List(Of cv.Point3f)
                    Dim minDepth = Single.MaxValue, maxDepth = Single.MinValue
                    For j = 0 To roi.Height - 1
                        For i = 0 To roi.Width - 1
                            Dim nextD = taskAlg.pcSplit(2)(roi).Get(Of Single)(j, i)
                            If nextD <> 0 Then
                                If minDepth > nextD Then minDepth = nextD
                                If maxDepth < nextD Then maxDepth = nextD
                                Dim wpt = New cv.Point3f(roi.X + i, roi.Y + j, nextD)
                                fitPoints.Add(Cloud_Basics.worldCoordinates(wpt))
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
            flow.Run(src)
        End Sub
    End Class
End Namespace