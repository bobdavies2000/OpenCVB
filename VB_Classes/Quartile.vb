Imports cv = OpenCvSharp
Public Class Quartile_Basics : Inherits VB_Algorithm
    Dim binary As New Quartile_SplitMean
    Dim mats As New Mat_4to1
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels(1) = "Contour counts for each roi in gridList.  Click on any roi to display all 4 quartiles"
        labels(3) = "Quartiles for the selected grid element - brightest, less bright, less dark, darkest"
        desc = "Highlight the contours for each grid element."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static index = task.gridToRoiIndex.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
        If task.mousePicTag = 1 Then index = task.gridToRoiIndex.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
        Dim roiSave = If(index < task.gridList.Count, task.gridList(index), New cv.Rect)

        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        binary.Run(src)
        binary.mats.Run(empty)
        dst2 = binary.mats.dst2
        dst1 = binary.mats.dst3 * 0.5

        Dim counts(3, task.gridList.Count) As Integer
        Dim contourCounts As New List(Of List(Of Integer))
        Dim means As New List(Of List(Of Single))
        For i = 0 To counts.GetUpperBound(0)
            For j = 0 To task.gridList.Count - 1
                Dim roi = task.gridList(j)
                Dim allContours As cv.Point()()
                Dim tmp = binary.mats.mat(i)(roi)
                cv.Cv2.FindContours(tmp, allContours, Nothing, cv.RetrievalModes.External, cv.ContourApproximationModes.ApproxSimple)
                If i = 0 Then
                    contourCounts.Add(New List(Of Integer))
                    means.Add(New List(Of Single))
                End If
                contourCounts(j).Add(allContours.Count)
                means(j).Add(src(roi).Mean(tmp).Item(0))
                If i = binary.mats.quadrant Then setTrueText(CStr(allContours.Count), roi.TopLeft, 1)
                counts(i, j) = allContours.Count
            Next
        Next

        Static labelStr(3) As String, points(3) As cv.Point
        Dim bump = 3
        For i = 0 To mats.mat.Count - 1
            mats.mat(i) = binary.mats.mat(i)(roiSave) * 0.5
            If task.heartBeat Then
                points(i) = Choose(i + 1, New cv.Point(bump, bump), New cv.Point(bump + dst2.Width / 2, bump),
                                          New cv.Point(bump, bump + dst2.Height / 2),
                                          New cv.Point(bump + dst2.Width / 2, bump + dst2.Height / 2))
                labelStr(i) = (CStr(mats.mat(i).CountNonZero) + " pixels" + vbCrLf + CStr(contourCounts(index)(i)) + " contours" + vbCrLf +
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





Public Class Quartile_Basics1 : Inherits VB_Algorithm
    Dim binary As New Quartile_SplitMean
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels(1) = "Contour counts for each roi in gridList.  Click on any roi to display all 4 quartiles"
        desc = "Highlight the contours for each grid element."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim index = task.gridToRoiIndex.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
        Dim roiSave = task.gridList(index)

        binary.Run(src)
        binary.mats.Run(empty)
        dst2 = binary.mats.dst2
        dst3 = binary.mats.dst3
        labels(3) = CStr(dst3.CountNonZero) + " pixels in this quartile"
        dst1 = dst3 * 0.5

        Dim counts(3, task.gridList.Count) As Integer
        For i = 0 To counts.GetUpperBound(0)
            For j = 0 To task.gridList.Count - 1
                Dim roi = task.gridList(j)
                Dim allContours As cv.Point()()
                cv.Cv2.FindContours(dst3(roi), allContours, Nothing, cv.RetrievalModes.External, cv.ContourApproximationModes.ApproxSimple)
                setTrueText(CStr(allContours.Count), roi.TopLeft, 1)
                counts(i, j) = allContours.Count
            Next
        Next

        dst1.Rectangle(roiSave, cv.Scalar.White, task.lineWidth)
        task.color.Rectangle(roiSave, cv.Scalar.White, task.lineWidth)
    End Sub
End Class







Public Class Quartile_SplitMean : Inherits VB_Algorithm
    Dim binary As New Binarize_Simple
    Public mats As New Mat_4Click
    Public Sub New()
        labels(2) = "A 4-way split - lightest (upper left) to darkest (lower right)"
        desc = "Binarize an image twice using masks"
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








Public Class Edge_BinarizedSobel : Inherits VB_Algorithm
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






Public Class Quartile_Input : Inherits VB_Algorithm
    Dim qMean As New Quartile_SplitMean

    Public Sub New()
        desc = "Select which binary image is to be analyzed further"
    End Sub
    Public Sub RunVB(src As cv.Mat)
    End Sub
End Class
