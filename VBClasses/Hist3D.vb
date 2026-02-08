Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Namespace VBClasses
    Public Class Hist3D_Basics : Inherits TaskParent
        Dim hColor As New Hist3Dcolor_Basics
        Dim hCloud As New Hist3Dcloud_Basics
        Public classCount As Integer
        Dim options As New Options_Hist3D
        Public Sub New()
            labels = {"", "", "Sum of 8UC1 outputs of Hist3Dcolor_Basics and Hist3Dcloud_basics", ""}
            desc = "Build an 8UC1 image by adding Hist3Dcolor_Basics and Hist3Dcloud_Basics output"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            hColor.Run(src)
            dst2 = hColor.dst2

            hCloud.Run(src)
            hCloud.dst2 += hColor.classCount + 1
            hCloud.dst2.SetTo(0, tsk.noDepthMask)

            hCloud.dst2.CopyTo(dst2, tsk.depthmask)
            classCount = hColor.classCount + hCloud.classCount

            dst3 = PaletteFull(dst2)
            labels(3) = CStr(classCount) + " classes "
        End Sub
    End Class







    Public Class Hist3D_BuildHistogram : Inherits TaskParent
        Public threshold As Integer
        Public classCount As Integer
        Public histArray() As Single
        Dim plot As New Histogram_Depth
        Public Sub New()
            desc = "Build a guided 3D histogram from the 3D histogram supplied in src."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then
                tsk.gOptions.setHistogramBins(100)
                plot.Run(src)
                If plot.histogram.Rows = 0 Then Exit Sub
                src = plot.histogram
            End If

            ReDim histArray(src.Total - 1)
            Marshal.Copy(src.Data, histArray, 0, histArray.Length)

            classCount = 1
            Dim index As Integer
            For i = index To histArray.Count - 1
                For index = index To histArray.Count - 1
                    If histArray(index) > threshold Then Exit For
                    histArray(index) = classCount
                Next
                classCount += 1
                For index = index To histArray.Count - 1
                    If histArray(index) <= threshold Then Exit For
                    histArray(index) = classCount
                Next

                If index >= histArray.Count Then Exit For
            Next

            Dim minClass = histArray.Min - 1
            If minClass <> 0 Then
                src -= minClass
                For i = 0 To histArray.Count - 1
                    histArray(i) -= minClass
                Next
                classCount -= minClass
            End If
            dst2 = src.Clone
            Marshal.Copy(histArray, 0, dst2.Data, histArray.Length)
            labels(2) = "Histogram entries vary from " + CStr(histArray.Min) + " to " + CStr(classCount) + " inclusive"
        End Sub
    End Class








    Public Class NR_Hist3D_RedCloud : Inherits TaskParent
        Dim hist3D As New Hist3D_Basics
        Public Sub New()
            desc = "Run RedList_Basics on the combined Hist3D color/cloud output."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            hist3D.Run(src)
            dst2 = hist3D.dst3
            labels(2) = hist3D.labels(3)

            dst3 = runRedList(hist3D.dst2, labels(3))
        End Sub
    End Class








    Public Class NR_Hist3D_RedColor : Inherits TaskParent
        Dim hColor As New Hist3Dcolor_Basics
        Dim redC As New RedColor_Basics
        Public Sub New()
            desc = "Use the Hist3D color classes to segment the image with RedList_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            hColor.Run(src)
            dst3 = hColor.dst3
            labels(3) = hColor.labels(3)

            redC.Run(hColor.dst2)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
            If redC.rcList.Count > 0 Then dst2(tsk.rcD.rect).SetTo(white, tsk.rcD.mask)
        End Sub
    End Class







    Public Class NR_Hist3D_DepthWithMask : Inherits TaskParent
        Dim hColor As New Hist3Dcolor_Basics
        Public depthMask As New cv.Mat
        Dim fore As New Foreground_KMeans
        Public Sub New()
            desc = "Isolate the foreground and no depth in the image and run it through Hist3D_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then
                fore.Run(src)
                depthMask = fore.dst2 Or tsk.noDepthMask
            End If
            hColor.inputMask = depthMask
            dst0 = Not depthMask

            src.SetTo(0, dst0)

            hColor.Run(src)
            dst2 = hColor.dst2
            dst2.SetTo(0, dst0)

            dst3 = hColor.dst3
            dst3.SetTo(0, dst0)
            labels = hColor.labels
        End Sub
    End Class






    Public Class Hist3D_Pixel : Inherits TaskParent
        Public histogram As New cv.Mat
        Public histArray() As Single
        Public classCount As Integer
        Dim options As New Options_Hist3D
        Public Sub New()
            desc = "Classify each pixel using a 3D histogram backprojection."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If src.Channels() <> 3 Then src = tsk.color
            Dim bins = options.histogram3DBins
            cv.Cv2.CalcHist({src}, {0, 1, 2}, New cv.Mat, histogram, 3, {bins, bins, bins}, tsk.rangesBGR)

            ReDim histArray(histogram.Total - 1)
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

            For i = 0 To histArray.Count - 1
                histArray(i) = i + 1
            Next

            classCount = bins * bins * bins
            Marshal.Copy(histArray, 0, histogram.Data, histArray.Length)

            cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst2, tsk.rangesBGR)
            dst3 = PaletteFull(dst2)
        End Sub
    End Class








    Public Class NR_Hist3D_PixelCells : Inherits TaskParent
        Dim pixel As New Hist3D_Pixel
        Dim flood As New Flood_Basics
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            labels = {"", "", "Cell-by-cell backprojection of the Hist3D_Pixel algorithm", "Palette version of dst2"}
            desc = "After classifying each pixel, backproject each redCell using the same 3D histogram."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            flood.Run(src)

            pixel.Run(src)

            For Each rc In tsk.redList.oldrclist
                cv.Cv2.CalcBackProject({src(rc.rect)}, {0, 1, 2}, pixel.histogram, dst2(rc.rect), tsk.rangesBGR)
            Next

            dst3 = PaletteFull(dst2)
        End Sub
    End Class








    Public Class NR_Hist3D_PixelClassify : Inherits TaskParent
        Dim pixel As New Hist3D_Pixel
        Public Sub New()
            desc = "Classify each pixel with a 3D histogram backprojection and run RedList_Basics on the output."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            pixel.Run(src)

            dst2 = runRedList(pixel.dst2, labels(2))

            If tsk.redList.oldrclist.Count > 0 Then
                dst2(tsk.oldrcD.rect).SetTo(white, tsk.oldrcD.mask)
            End If
        End Sub
    End Class






    Public Class NR_Hist3D_PixelDiffMask : Inherits TaskParent
        Dim pixel As New Hist3D_Pixel
        Public Sub New()
            desc = "Build better image segmentation - remove unstable pixels from 3D color histogram backprojection"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            pixel.Run(src)
            Static lastImage As cv.Mat = pixel.dst2.Clone
            cv.Cv2.Absdiff(lastImage, pixel.dst2, dst3)
            dst2 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary)
            lastImage = pixel.dst2.Clone
        End Sub
    End Class






    Public Class NR_Hist3D_RedCloudGrid : Inherits TaskParent
        Dim pixels As New Pixel_Vectors
        Dim hVector As New Hist3Dcolor_Vector
        Public Sub New()
            tsk.gOptions.GridSlider.Value = 8
            desc = "Build RedCloud pixel vectors and then measure each grid element's distance to those vectors."
        End Sub
        Private Function distanceN(vec1 As List(Of Single), vec2 As List(Of Single)) As Double
            Dim accum As Double
            For i = 0 To vec1.Count - 1
                accum += (vec1(i) - vec2(i)) * (vec1(i) - vec2(i))
            Next
            Return Math.Sqrt(accum)
        End Function
        Private Function distanceN(vec1() As Single, vec2() As Single) As Double
            Dim accum As Double
            For i = 0 To vec1.Count - 1
                accum += (vec1(i) - vec2(i)) * (vec1(i) - vec2(i))
            Next
            Return Math.Sqrt(accum)
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            pixels.Run(src)
            dst2 = pixels.redC.rcMap
            dst0 = pixels.redC.rcMap

            dst3 = dst2.InRange(0, 0)
            If pixels.pixelVector.Count = 0 Then Exit Sub
            dst1.SetTo(0)
            For Each gr In tsk.gridRects
                If dst3(gr).CountNonZero Then
                    Dim candidates As New List(Of Integer)
                    For y = 0 To gr.Height - 1
                        For x = 0 To gr.Width - 1
                            Dim val = dst0(gr).Get(Of Byte)(y, x)
                            If val = 0 Then Continue For
                            If candidates.Contains(val) = False Then candidates.Add(val)
                        Next
                    Next
                    If candidates.Count > 1 Then
                        hVector.inputMask = dst3(gr)
                        hVector.Run(src(gr))
                        Dim distances As New List(Of Double)
                        For Each index In candidates
                            If index >= 0 And index < pixels.pixelVector.Count Then
                                Dim vec = pixels.pixelVector(index - 1)
                                distances.Add(distanceN(vec, hVector.histArray))
                            End If
                        Next
                        If distances.Count > 0 Then
                            Dim index1 = candidates(distances.IndexOf(distances.Min)) - 1
                            If index1 < pixels.rclist.Count Then
                                Dim cell = pixels.rclist(index1)
                                dst1(gr).SetTo(cell.color, dst3(gr))
                            End If
                        End If
                    ElseIf candidates.Count = 1 Then
                            Dim cell = pixels.rclist(candidates(0) - 1)
                        dst1(gr).SetTo(cell.color, dst3(gr))
                    End If
                End If
            Next
        End Sub
    End Class
End Namespace