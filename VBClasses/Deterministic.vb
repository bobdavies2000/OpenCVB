Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class NR_Deterministic_Basics : Inherits TaskParent
        Dim deter As New Edge_Canny
        Dim diff As New Diff_Basics
        Public Sub New()
            desc = "Is an algorithm deterministic?  Will the same input produce the same output?  Find out here."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            deter.Run(src.Clone)
            dst2 = deter.dst2.Clone

            deter.Run(src)

            If deter.dst2.Channels <> 1 Then
                diff.lastFrame = deter.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Else
                diff.lastFrame = deter.dst2.Clone
            End If

            diff.Run(dst2)
            dst3 = diff.dst2

            Dim count = dst3.CountNonZero
            labels(2) = If(count, "Algorithm is NOT deterministic", "Algorithm is deterministic.") + " - there were " + CStr(count) +
                        " pixels changed."
        End Sub
    End Class






    Public Class Deterministic_MotionMask : Inherits TaskParent
        Dim deter As New Edge_Canny
        Dim diff As New Diff_Basics
        Public Sub New()
            labels(3) = "A mask of the differences between the original and motion-filtered color image output."
            desc = "Run the algorithm with and without using the motion mask.  Can a motion-filtered color image get the same sharedResults.images.."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            deter.Run(src.Clone) ' run with the unfiltered image.
            dst2 = deter.dst2.Clone

            Static lastFrame As cv.Mat = tsk.color.Clone
            Dim dst1 = lastFrame.Clone
            src.CopyTo(dst1, tsk.motionRGB.motionMask)
            deter.Run(dst1)

            If deter.dst2.Channels <> 1 Then
                diff.lastFrame = deter.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Else
                diff.lastFrame = deter.dst2.Clone
            End If

            diff.Run(dst2)
            dst3 = diff.dst2

            Dim count = dst3.CountNonZero
            labels(2) = "There were " + CStr(count) + " pixels changed when running with the motion-filtered color image."

            lastFrame = src.Clone
        End Sub
    End Class






    Public Class NR_Deterministic_Histogram : Inherits TaskParent
        Dim deter As New Deterministic_MotionMask
        Dim plothist As New Plot_Histogram
        Public Sub New()
            plothist.removeZeroEntry = False
            plothist.minRange = 0
            plothist.maxRange = 255
            plothist.createHistogram = True
            tsk.gOptions.setHistogramBins(255)
            labels(2) = "Histogram bins range from 0 to 255."
            If standalone Then tsk.gOptions.displayDst1.Checked = True
            desc = "Build a histogram from the differences in an attempt to answer why are the images different."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            deter.Run(src)

            If tsk.heartBeat = False Then Exit Sub
            dst3 = deter.dst3

            plothist.histMask = deter.dst3.Clone
            plothist.Run(src)
            dst2 = plothist.dst2
        End Sub
    End Class







    Public Class NR_Deterministic_BackProject : Inherits TaskParent
        Dim deter As New Deterministic_MotionMask
        Dim bProject As New BackProject_Basics
        Public Sub New()
            If standalone Then tsk.gOptions.displayDst0.Checked = True
            If standalone Then tsk.gOptions.displayDst1.Checked = True
            tsk.gOptions.CrossHairs.Checked = False
            labels(3) = "Mask of pixels that differ between original image and motion-filtered image."
            desc = "Build a histogram from the differences in an attempt to answer why are the images different."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            deter.Run(tsk.gray)

            If tsk.heartBeat = False Then Exit Sub
            dst3 = deter.dst3

            bProject.hist.histMask = deter.dst3.Clone
            bProject.Run(src)
            dst2 = bProject.dst2

            Static saveMouseMove As cv.Point
            If saveMouseMove <> tsk.mouseMovePoint Then
                saveMouseMove = tsk.mouseMovePoint
                dst1.SetTo(0)
            End If

            bProject.dst0.SetTo(0, Not dst3)
            dst1.SetTo(cv.Scalar.Yellow, bProject.dst0)
            Dim mask = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            dst0 = src.Clone
            dst1.CopyTo(dst0, mask)

            Dim mm = GetMinMax(tsk.gray, deter.dst3)
            labels(2) = "Active histogram bins range from " + CStr(mm.minVal) + " to " + CStr(mm.maxVal) + ".  X-axis is 0 to 255"
            SetTrueText("Pixels in the selected histogram bin - move mouse to reset to 0.", 1)
        End Sub
    End Class


End Namespace