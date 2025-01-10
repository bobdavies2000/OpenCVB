Imports cv = OpenCvSharp
Public Class FeatureLess_Basics : Inherits TaskParent
    Dim edges As New EdgeDraw_Basics
    Public classCount As Integer = 2
    Public Sub New()
        labels = {"", "", "EdgeDraw_Basics output", ""}
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels(2) = "CV_8UC1 output of EdgeDraw_Basics - only 0 and 1"
        desc = "Access the EdgeDraw_Basics algorithm directly rather than through the CPP_Basics interface - more efficient"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        edges.Run(src)
        dst2.SetTo(0)
        dst2.SetTo(1, edges.dst2)
        If standaloneTest() Then dst3 = ShowPalette((dst2 * 255 / classCount).ToMat)
    End Sub
End Class




Public Class FeatureLess_WithoutMotion : Inherits TaskParent
    Dim edges As New EdgeDraw_Basics
    Public classCount As Integer = 2
    Public Sub New()
        labels = {"", "", "EdgeDraw_Basics output", ""}
        desc = "Access the EdgeDraw_Basics algorithm directly rather than through the CPP_Basics interface - more efficient"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        edges.Run(src)
        dst2 = edges.dst2
    End Sub
End Class






Public Class FeatureLess_Canny : Inherits TaskParent
    Dim edges As New Edge_Basics
    Dim options As New Options_Sobel()
    Public Sub New()
        desc = "Use Canny edges to define featureless regions."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        edges.Run(src)
        dst2 = Not edges.dst2.Threshold(options.distanceThreshold, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class








Public Class FeatureLess_Sobel : Inherits TaskParent
    Dim edges As New Edge_Sobel
    Dim options As New Options_Sobel()
    Public Sub New()
        desc = "Use Sobel edges to define featureless regions."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        edges.Run(src)
        dst2 = Not edges.dst2.Threshold(options.distanceThreshold, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class







Public Class FeatureLess_UniquePixels : Inherits TaskParent
    Dim fless As New Hough_FeatureLessTopX
    Dim sort As New Sort_1Channel
    Public Sub New()
        If standalone Then optiBase.FindSlider("Threshold for sort input").Value = 0
        labels = {"", "Gray scale input to sort/remove dups", "Unique pixels", ""}
        desc = "Find the unique gray pixels for the featureless regions"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        fless.Run(src)
        dst2 = fless.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        sort.Run(dst2)
        dst3 = sort.dst2
    End Sub
End Class







Public Class FeatureLess_Unique3Pixels : Inherits TaskParent
    Dim fless As New Hough_FeatureLessTopX
    Dim sort3 As New Sort_3Channel
    Public Sub New()
        desc = "Find the unique 3-channel pixels for the featureless regions"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        fless.Run(src)
        dst2 = fless.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        sort3.Run(fless.dst2)
        dst3 = sort3.dst2
    End Sub
End Class






Public Class FeatureLess_Histogram : Inherits TaskParent
    Dim backP As New BackProject_FeatureLess
    Public Sub New()
        desc = "Create a histogram of the featureless regions"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        backP.Run(src)
        dst2 = backP.dst2
        dst3 = backP.dst3
        labels = backP.labels
    End Sub
End Class










Public Class FeatureLess_DCT : Inherits TaskParent
    Dim dct As New DCT_FeatureLess
    Public Sub New()
        labels(3) = "Largest FeatureLess Region"
        desc = "Use DCT to find featureless regions."
    End Sub

    Public Overrides sub RunAlg(src As cv.Mat)
        dct.Run(src)
        dst2 = dct.dst2
        dst3 = dct.dst3

        Dim mask = dst2.Clone()
        Dim objectSize As New List(Of Integer)
        Dim regionCount = 1
        For y = 0 To mask.Rows - 1
            For x = 0 To mask.Cols - 1
                If mask.Get(Of Byte)(y, x) = 255 Then
                    Dim pt As New cv.Point(x, y)
                    Dim floodCount = mask.FloodFill(pt, regionCount)
                    objectSize.Add(floodCount)
                    regionCount += 1
                End If
            Next
        Next

        Dim maxSize As Integer, maxIndex As Integer
        For i = 0 To objectSize.Count - 1
            If maxSize < objectSize.ElementAt(i) Then
                maxSize = objectSize.ElementAt(i)
                maxIndex = i
            End If
        Next

        Dim label = mask.InRange(maxIndex + 1, maxIndex + 1)
        Dim nonZ = label.CountNonZero()
        labels(3) = "Largest FeatureLess Region (" + CStr(nonZ) + " " + Format(nonZ / label.Total, "#0.0%") + " pixels)"
        dst3.SetTo(white, label)
    End Sub
End Class








Public Class FeatureLess_LeftRight : Inherits TaskParent
    Dim fLess As New FeatureLess_Basics
    Public Sub New()
        labels = {"", "", "FeatureLess Left mask", "FeatureLess Right mask"}
        desc = "Find the featureless regions of the left and right images"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        fLess.Run(task.leftView)
        dst2 = fLess.dst2.Clone

        fLess.Run(task.rightView)
        dst3 = fLess.dst2
    End Sub
End Class









Public Class FeatureLess_History : Inherits TaskParent
    Dim fLess As New FeatureLess_Basics
    Dim frames As New History_Basics
    Public Sub New()
        desc = "Accumulate the edges over a span of X images."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        fLess.Run(src)
        dst2 = fLess.dst2

        frames.Run(dst2)
        dst3 = frames.dst2
    End Sub
End Class







Public Class FeatureLess_Groups : Inherits TaskParent
    Dim redCPP As New RedColor_CPP
    Dim fless As New FeatureLess_Basics
    Public classCount As Integer
    Public Sub New()
        desc = "Group RedCloud cells by the value of their featureless maxDist"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        fless.Run(src)
        dst2 = fless.dst2
        labels(2) = fless.labels(2)

        redCPP.Run(dst2)
        classCount = redCPP.classCount
        dst3 = redCPP.dst2
        labels(3) = CStr(classCount) + " featureless regions were found."
    End Sub
End Class