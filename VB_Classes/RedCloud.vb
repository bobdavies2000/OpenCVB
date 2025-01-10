Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedCloud_Basics : Inherits TaskParent
    Public classCount As Integer
    Dim options As New Options_RedColorEx
    Public Sub New()
        task.redOptions.UseDepth.Checked = True
        desc = "Reduction transform for the point cloud"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        options.RunOpt()

        task.pointCloud.ConvertTo(dst0, cv.MatType.CV_32S, 1000 / options.reduceAmt)

        Dim split = dst0.Split()

        Select Case task.redOptions.PointCloudReduction
            Case 0 ' "X Reduction"
                dst0 = (split(0) * options.reduceAmt).ToMat
            Case 1 ' "Y Reduction"
                dst0 = (split(1) * options.reduceAmt).ToMat
            Case 2 ' "Z Reduction"
                dst0 = (split(2) * options.reduceAmt).ToMat
            Case 3 ' "XY Reduction"
                dst0 = (split(0) * options.reduceAmt + split(1) * options.reduceAmt).ToMat
            Case 4 ' "XZ Reduction"
                dst0 = (split(0) * options.reduceAmt + split(2) * options.reduceAmt).ToMat
            Case 5 ' "YZ Reduction"
                dst0 = (split(1) * options.reduceAmt + split(2) * options.reduceAmt).ToMat
            Case 6 ' "XYZ Reduction"
                dst0 = (split(0) * options.reduceAmt + split(1) * options.reduceAmt + split(2) * options.reduceAmt).ToMat
        End Select

        Dim mm As mmData = GetMinMax(dst0)
        dst0 = (dst0 - mm.minVal)
        dst2 = dst0 * 255 / (mm.maxVal - mm.minVal)
        dst2.ConvertTo(dst2, cv.MatType.CV_8U)
        mm = GetMinMax(dst0)

        labels(2) = task.redOptions.PointCloudReductionLabel + " with reduction factor = " + CStr(options.reduceAmt)
    End Sub
End Class







Public Class RedCloud_BasicsHist : Inherits TaskParent
    Dim reduce As New RedCloud_Basics
    Dim plot As New Plot_Histogram
    Public Sub New()
        plot.createHistogram = True
        desc = "Display the histogram of the RedCloud_Basics output"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        reduce.Run(src)
        dst2 = reduce.dst2
        Dim mm = GetMinMax(dst2, task.depthMask)
        plot.minRange = mm.minVal
        plot.maxRange = mm.maxVal
        plot.Run(dst2)
        dst3 = plot.dst2
        labels(2) = reduce.labels(2)
    End Sub
End Class






Public Class RedCloud_BasicsTest : Inherits TaskParent
    Dim redInput As New RedCloud_Basics
    Public Sub New()
        desc = "Run RedCloud with the depth reduction."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        redInput.Run(src)

        dst2 = getRedColor(redInput.dst2, labels(2))
    End Sub
End Class
