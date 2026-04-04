Imports cv = OpenCvSharp
'http://study.marearts.com/2014/12/opencv-meanshiftfiltering-example.html
Imports VBClasses
    Public Class PyrFilter_Basics_TA : Inherits TaskParent
        Dim options As New Options_PyrFilter
        Public Sub New()
            desc = "Use PyrMeanShiftFiltering to segment an image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            cv.Cv2.PyrMeanShiftFiltering(src, dst2, options.spatialRadius, options.colorRadius, options.maxPyramid)
        End Sub
    End Class







    Public Class NR_PyrFilter_RedCloud : Inherits TaskParent
        Dim reduction As New Reduction_Basics
        Dim pyr As New PyrFilter_Basics_TA
        Dim redC As New RedCloud_Basics
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            labels = {"", "", "RedMask_List output", "PyrFilter output before reduction"}
            desc = "Use RedColor to segment the output of PyrFilter"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            pyr.Run(src)
            dst3 = pyr.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            reduction.Run(dst3)

            redC.Run(reduction.dst2)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            SetTrueText(redC.strOut, 1)
        End Sub
    End Class
