Imports System.Windows.Input
Imports cv = OpenCvSharp
Public Class Quartile_Basics : Inherits VB_Algorithm
    Dim mats As New Mat_4to1
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels(1) = "Contour counts for each roi in gridList.  Click on any roi to display all 4 quartiles"
        labels(3) = "Quartiles for the selected grid element - brightest, less bright, less dark, darkest"
        desc = "Highlight the contours for each grid element with stats for each."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static index = task.gridToRoiIndex.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
        If task.mousePicTag = 1 Then index = task.gridToRoiIndex.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
        Dim roiSave = If(index < task.gridList.Count, task.gridList(index), New cv.Rect)

        If task.optionsChanged Then index = 0

        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim matList(3) As cv.Mat
        For i = 0 To matList.Count - 1
            mats.mat(i) = New cv.Mat(mats.mat(i).Size, cv.MatType.CV_8U, 0)
        Next

        Dim quadrant As Integer
        If gOptions.DebugCheckBox.Checked Then
            Static binaryV As New Quartile_SplitValley
            binaryV.Run(src)
            binaryV.mats.Run(empty)
            dst2 = binaryV.mats.dst2
            dst1 = binaryV.mats.dst3 * 0.5
            matList = binaryV.mats.mat
            quadrant = binaryV.mats.quadrant
        Else
            Static binary As New Quartile_SplitMean
            binary.Run(src)
            binary.mats.Run(empty)
            dst2 = binary.mats.dst2
            dst1 = binary.mats.dst3 * 0.5
            matList = binary.mats.mat
            quadrant = binary.mats.quadrant
        End If

        Dim counts(3, task.gridList.Count) As Integer
        Dim contourCounts As New List(Of List(Of Integer))
        Dim means As New List(Of List(Of Single))

        Dim allContours As cv.Point()()
        For i = 0 To counts.GetUpperBound(0)
            For j = 0 To task.gridList.Count - 1
                Dim roi = task.gridList(j)
                Dim tmp = matList(i)(roi)
                cv.Cv2.FindContours(tmp, allContours, Nothing, cv.RetrievalModes.External, cv.ContourApproximationModes.ApproxSimple)
                If i = 0 Then
                    contourCounts.Add(New List(Of Integer))
                    means.Add(New List(Of Single))
                End If
                contourCounts(j).Add(allContours.Count)
                means(j).Add(src(roi).Mean(tmp).Item(0))
                If i = quadrant Then setTrueText(CStr(allContours.Count), roi.TopLeft, 1)
                counts(i, j) = allContours.Count
            Next
        Next

        Static labelStr(3) As String, points(3) As cv.Point
        Dim bump = 3
        Dim ratio = dst2.Height / task.gridList(0).Height
        For i = 0 To matList.Count - 1
            Dim tmp = matList(i)(roiSave)
            Dim nextCount = tmp.CountNonZero
            Dim r = New cv.Rect(0, 0, tmp.Width * ratio, tmp.Height * ratio)
            mats.mat(i)(r) = tmp.Resize(New cv.Size(r.Width, r.Height))
            If task.heartBeat Then
                Dim plus = mats.mat(i)(r).Width / 2
                points(i) = Choose(i + 1, New cv.Point(bump + plus, bump), New cv.Point(bump + dst2.Width / 2 + plus, bump),
                                          New cv.Point(bump + plus, bump + dst2.Height / 2),
                                          New cv.Point(bump + dst2.Width / 2 + plus, bump + dst2.Height / 2))
                labelStr(i) = (CStr(nextCount) + " pixels" + vbCrLf + CStr(contourCounts(index)(i)) + " contours" + vbCrLf +
                               Format(means(index)(i), fmt0) + " mean")
            End If
        Next

        For i = 0 To labelStr.Count - 1
            setTrueText(labelStr(i), points(i), 3)
        Next

        mats.Run(src)
        dst3 = mats.dst2

        dst1.Rectangle(roiSave, cv.Scalar.White, task.lineWidth)
        task.color.Rectangle(roiSave, cv.Scalar.White, task.lineWidth)
    End Sub
End Class









Public Class Quartile_RegionsLeftRight : Inherits VB_Algorithm
    Dim binary As New Quartile_SplitMean
    Public classCount = 4 ' 4-way split
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Add the 4-way split of left and right views."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        binary.Run(src)

        dst0.SetTo(1, binary.mats.mat(0))
        dst0.SetTo(2, binary.mats.mat(1))
        dst0.SetTo(3, binary.mats.mat(2))
        dst0.SetTo(4, binary.mats.mat(3))

        dst2 = vbPalette((dst0 * 255 / classCount).ToMat)

        binary.Run(task.rightView)

        dst1.SetTo(1, binary.mats.mat(0))
        dst1.SetTo(2, binary.mats.mat(1))
        dst1.SetTo(3, binary.mats.mat(2))
        dst1.SetTo(4, binary.mats.mat(3))

        dst3 = vbPalette((dst1 * 255 / classCount).ToMat)
    End Sub
End Class








Public Class Quartile_Canny : Inherits VB_Algorithm
    Dim edges As New Edge_Canny
    Dim binary As New Quartile_SplitMean
    Dim mats As New Mat_4Click
    Public Sub New()
        labels(2) = "Edges between halves, lightest, darkest, and the combo"
        desc = "Collect edges from binarized images"
    End Sub
    Public Sub RunVB(src As cv.Mat)

        binary.Run(src)

        edges.Run(binary.mats.mat(0))  ' the light and dark halves
        mats.mat(0) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        mats.mat(3) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

        edges.Run(binary.mats.mat(1))  ' the lightest of the light half
        mats.mat(1) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        mats.mat(3) = mats.mat(1) Or mats.mat(3)

        edges.Run(binary.mats.mat(3))  ' the darkest of the dark half
        mats.mat(2) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        mats.mat(3) = mats.mat(2) Or mats.mat(3)
        mats.Run(empty)
        dst2 = mats.dst2
        If mats.dst3.Channels = 3 Then
            labels(3) = "Combo of first 3 below.  Click quadrants in dst2."
            dst3 = mats.mat(3)
        Else
            dst3 = mats.dst3
        End If
    End Sub
End Class







Public Class Quartile_Regions : Inherits VB_Algorithm
    Dim binary As New Quartile_SplitMean
    Public classCount = 4 ' 4-way split 
    Public Sub New()
        dst2 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Add the 4-way split of images to define the different regions."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        binary.Run(src)

        dst2.SetTo(1, binary.mats.mat(0))
        dst2.SetTo(2, binary.mats.mat(1))
        dst2.SetTo(3, binary.mats.mat(2))
        dst2.SetTo(4, binary.mats.mat(3))

        dst3 = vbPalette((dst2 * 255 / classCount).ToMat)
    End Sub
End Class








Public Class Quartile_Sobel : Inherits VB_Algorithm
    Dim edges As New Edge_Sobel_Old
    Dim binary As New Quartile_SplitMean
    Public mats As New Mat_4Click
    Public Sub New()
        findSlider("Sobel kernel Size").Value = 5
        labels(2) = "Edges between halves, lightest, darkest, and the combo"
        labels(3) = "Click any quadrant in dst2 to view it in dst3"
        desc = "Collect Sobel edges from binarized images"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        binary.Run(src)

        edges.Run(binary.mats.mat(0)) ' the light and dark halves
        mats.mat(0) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        mats.mat(3) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

        edges.Run(binary.mats.mat(1)) ' the lightest of the light half
        mats.mat(1) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        mats.mat(3) = mats.mat(1) Or mats.mat(3)

        edges.Run(binary.mats.mat(3))  ' the darkest of the dark half
        mats.mat(2) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        mats.mat(3) = mats.mat(2) Or mats.mat(3)

        mats.Run(empty)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class







Public Class Edge_BinarizedReduction : Inherits VB_Algorithm
    Dim edges As New Quartile_Sobel
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Visualize the impact of reduction on Edge_BinarizeSobel"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)
        dst3 = reduction.dst2
        edges.Run(dst3)
        dst2 = edges.dst2
    End Sub
End Class








Public Class Edge_Consistent : Inherits VB_Algorithm
    Dim edges As New Quartile_Sobel
    Public Sub New()
        findSlider("Sobel kernel Size").Value = 5
        desc = "Edges that are consistent for x number of frames"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static saveFrames As New List(Of cv.Mat)
        If task.optionsChanged Then saveFrames = New List(Of cv.Mat)

        edges.Run(src)

        Dim tmp = If(edges.dst2.Channels = 1, edges.dst2.Clone, edges.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        saveFrames.Add(tmp)
        If saveFrames.Count > task.frameHistoryCount Then saveFrames.RemoveAt(0)

        dst2 = saveFrames(0)
        For i = 1 To saveFrames.Count - 1
            dst2 = saveFrames(i) And dst2
        Next

        dst3.SetTo(0)
        src.CopyTo(dst3, Not edges.dst3)
    End Sub
End Class






Public Class Quartile_SplitMean : Inherits VB_Algorithm
    Dim binary As New Binarize_Simple
    Public mats As New Mat_4Click
    Public Sub New()
        labels(2) = "A 4-way split - lightest (upper left) to darkest (lower right)"
        desc = "Binarize an image and split it into quartiles using peaks."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim gray = If(src.Channels = 1, src.Clone, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

        binary.Run(gray)
        Dim mask = binary.dst2.Clone

        Dim midColor = binary.meanScalar(0)
        Dim topColor = cv.Cv2.Mean(gray, mask)(0)
        Dim botColor = cv.Cv2.Mean(gray, Not mask)(0)
        mats.mat(0) = gray.InRange(topColor, 255)
        mats.mat(1) = gray.InRange(midColor, topColor)
        mats.mat(2) = gray.InRange(botColor, midColor)
        mats.mat(3) = gray.InRange(0, botColor)

        If standaloneTest() Then
            mats.Run(empty)
            dst2 = mats.dst2
            dst3 = mats.dst3
            labels(3) = mats.labels(3)
        End If
    End Sub
End Class






Public Class Quartile_SplitValley : Inherits VB_Algorithm
    Dim binary As New Binarize_Simple
    Dim valley As New HistValley_Basics
    Public mats As New Mat_4Click
    Public Sub New()
        labels(2) = "A 4-way split - lightest (upper left) to darkest (lower right)"
        desc = "Binarize an image using the valleys provided by HistValley_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim gray = If(src.Channels = 1, src.Clone, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

        binary.Run(gray)
        Dim mask = binary.dst2.Clone

        If task.heartBeat Then valley.Run(gray)

        mats.mat(0) = gray.InRange(valley.valleys(3), 255)
        mats.mat(1) = gray.InRange(valley.valleys(2), valley.valleys(3) - 1)
        mats.mat(2) = gray.InRange(valley.valleys(1), valley.valleys(2) - 1)
        mats.mat(3) = gray.InRange(0, valley.valleys(1) - 1)

        If standaloneTest() Then
            mats.Run(empty)
            dst2 = mats.dst2
            dst3 = mats.dst3
            labels(3) = mats.labels(3)
        End If
    End Sub
End Class
