Imports cv = OpenCvSharp
Public Class FeatureLess_Basics : Inherits TaskParent
    Dim edgeline As New EdgeLine_Basics
    Public Sub New()
        If task.contours Is Nothing Then task.contours = New Contour_Basics_List
        desc = "Use Contour_Basics to get the contour data for the top contours by size."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edgeline.Run(task.grayStable)
        If src.Type <> cv.MatType.CV_8U Then
            task.contours.Run(edgeline.dst2)
            dst2 = task.contours.dst2
            labels = task.contours.labels
        Else
            task.contours.Run(src)
            dst2 = task.contours.dst2
            labels = task.contours.labels
        End If
    End Sub
End Class







Public Class FeatureLess_Canny : Inherits TaskParent
    Dim edges As New Edge_Basics
    Dim options As New Options_Sobel()
    Public Sub New()
        desc = "Use Canny edges to define featureless regions."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

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
        options.Run()

        edges.Run(src)
        dst2 = Not edges.dst2.Threshold(options.distanceThreshold, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class







Public Class FeatureLess_UniquePixels : Inherits TaskParent
    Dim fless As New Hough_FeatureLessTopX
    Dim sort As New Sort_1Channel
    Public Sub New()
        If standalone Then OptionParent.FindSlider("Threshold for sort input").Value = 0
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







Public Class FeatureLess_History : Inherits TaskParent
    Dim fLess As New FeatureLess_Basics
    Dim frames As New History_Basics
    Public Sub New()
        desc = "Accumulate the edges over a span of X images."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        fLess.Run(src)
        dst2 = fLess.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

        frames.Run(dst2)
        dst3 = frames.dst2
    End Sub
End Class







Public Class FeatureLess_Groups : Inherits TaskParent
    Dim redCPP As New RedList_CPP
    Dim fless As New FeatureLess_Basics
    Public classCount As Integer
    Public Sub New()
        desc = "Group RedCloud cells by the value of their featureless maxDist"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fless.Run(src)
        dst1 = fless.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        labels(2) = fless.labels(2)

        If task.optionsChanged Then dst2 = dst1.Clone Else dst1.CopyTo(dst2, task.motionMask)
        redCPP.Run(dst2 - 1)
        classCount = redCPP.classCount
        dst3 = PaletteFull(redCPP.dst2)
        labels(3) = CStr(classCount) + " featureless regions were found."
    End Sub
End Class







Public Class FeatureLess_LeftRight : Inherits TaskParent
    Dim fLess As New FeatureLess_Basics
    Public Sub New()
        labels = {"", "", "FeatureLess Left mask", "FeatureLess Right mask"}
        desc = "Find the featureless regions of the left and right images"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            dst0 = task.leftView
            dst1 = task.rightView
        End If

        fLess.Run(task.leftView)
        dst2 = fLess.dst2.Clone

        fLess.Run(task.rightView)
        dst3 = fLess.dst2.Clone
    End Sub
End Class





Public Class FeatureLess_RedColor : Inherits TaskParent
    Dim fLess As New FeatureLess_Basics
    Public Sub New()
        desc = "Use the featureLess_Basics output as input to RedList_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(src)
        dst3 = fLess.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = runRedList(dst3, labels(2))
    End Sub
End Class