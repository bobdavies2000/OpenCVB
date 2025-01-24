Imports System.Web.UI
Imports cv = OpenCvSharp

Public Class Disparity_Basics : Inherits TaskParent
    Public correlations As New List(Of Single), means As New List(Of Single), stdevs As New List(Of Single)
    Public searchRect As cv.Rect, rect As cv.Rect
    Public bestCorrelation As Single, MeanDiff As Single, StdevDiff As Single
    Public Sub New()
        labels(2) = "Select an ideal depth cell to find its match in the right view."
        desc = "Given an ideal depth cell, find the match in the right view image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView
        Dim firstRect As cv.Rect
        For Each r In task.ideal.gridList
            If r.Width = 0 Then Continue For
            dst2.Rectangle(r, cv.Scalar.White, task.lineWidth)
            If firstRect.Width = 0 Or firstRect.X = 0 Then firstRect = r
        Next

        Dim val = task.ideal.grid.gridMap32S.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X)
        rect = task.ideal.grid.gridRects(val)
        If rect.X = 0 Then rect = firstRect
        dst2.Rectangle(rect, task.lineWidth, task.HighlightColor)

        rect.Width = 32
        rect.Height = 32 ' to improve accuracy.

        Dim correlation As New cv.Mat
        Dim opt = cv.TemplateMatchModes.CCoeffNormed
        correlations.Clear()
        means.Clear()
        stdevs.Clear()

        Dim meanT As Single, stdevT As Single, mean As Single, stdev As Single
        rect = ValidateRect(rect)
        cv.Cv2.MeanStdDev(dst2(rect), meanT, stdevT)
        dst3 = task.rightView.Clone
        Dim rr = rect
        Dim pcCorrs As New List(Of Single), maxPCcorr As Single
        For i = 1 To rr.X
            rr.X -= 1
            cv.Cv2.MeanStdDev(dst3(rect), mean, stdev)
            means.Add(Math.Abs(mean - meanT))
            stdevs.Add(Math.Abs(stdev - stdevT))
            cv.Cv2.MatchTemplate(dst2(rect), dst3(rr), correlation, opt)
            correlations.Add(correlation.Get(Of Single)(0, 0))
            cv.Cv2.MatchTemplate(task.pcSplit(2)(rect), task.pcSplit(2)(rr), correlation, opt)
            pcCorrs.Add(correlation.Get(Of Single)(0, 0))
        Next
        bestCorrelation = correlations.Max
        MeanDiff = means.Min
        StdevDiff = stdevs.Min

        searchRect = New cv.Rect(0, rect.Y, rect.X + task.idealCellSize, task.idealCellSize)

        dst3.Rectangle(searchRect, task.lineWidth, task.HighlightColor)
        If standalone Then
            Dim index = correlations.IndexOf(bestCorrelation)
            rr = New cv.Rect(rect.X - index, rect.Y, task.idealCellSize, task.idealCellSize)
            dst3.Rectangle(rr, 255, task.lineWidth)

            MeanDiff = means(index)
            StdevDiff = stdevs(index)
            maxPCcorr = pcCorrs(index)
        End If

        If task.heartBeat Then
            labels(3) = "Max correlation = " + Format(bestCorrelation, fmt3) + "  " +
                        "Min mean difference = " + Format(MeanDiff, fmt3) + "  " +
                        "Min stdev difference = " + Format(StdevDiff, fmt3) + "  " +
                        "Max PC correlation = " + Format(maxPCcorr, fmt3)
        End If
    End Sub
End Class






Public Class Disparity_MatchMean : Inherits TaskParent
    Dim disparity As New Disparity_Basics
    Public Sub New()
        desc = "Find and display the best cell with the smallest mean difference to the selected cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        disparity.Run(src)
        dst2 = disparity.dst2
        dst3 = disparity.dst3

        Dim rect = disparity.rect
        Dim r = rect

        Dim index = disparity.means.IndexOf(disparity.MeanDiff)
        r.X = rect.X - index

        dst3.Rectangle(r, 255, task.lineWidth)
        labels(3) = disparity.labels(3)
    End Sub
End Class





Public Class Disparity_MatchStdev : Inherits TaskParent
    Dim disparity As New Disparity_Basics
    Public Sub New()
        desc = "Find and display the best cell with the smallest stdev difference to the selected cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        disparity.Run(src)
        dst2 = disparity.dst2
        dst3 = disparity.dst3

        Dim rect = disparity.rect
        Dim r = rect

        Dim index = disparity.stdevs.IndexOf(disparity.StdevDiff)
        r.X = rect.X - index

        dst3.Rectangle(r, 255, task.lineWidth)
        labels(3) = disparity.labels(3)
    End Sub
End Class








Public Class Disparity_SearchRect : Inherits TaskParent
    Dim match As New Match_Basics
    Public Sub New()
        task.ClickPoint = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        desc = "Given an ideal depth cell, find the match in the right view image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim val = task.ideal.grid.gridMap32S.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X)
        dst2 = task.leftView
        Dim rect = task.ideal.grid.gridRects(val)
        dst2.Rectangle(rect, task.lineWidth, task.HighlightColor)

        match.template = dst2(rect)
        match.searchRect = New cv.Rect(0, rect.Y, rect.X + rect.Width, rect.Height)
        match.Run(task.rightView)
        dst3 = task.rightView
        dst3.Rectangle(match.matchRect, 255, task.lineWidth)
        labels(3) = "Correlation = " + Format(match.correlation, fmt3)
    End Sub
End Class






'The Function relating() depth To disparity Is

'Z = B * f / disparity
'where:

' Z Is the depth in meters
' B Is the baseline in meters
' f Is the focal length in pixels
' disparity Is the disparity in pixels
' The baseline Is the distance between the two cameras in a stereo setup.
' The focal length Is the distance between the camera's lens and the sensor. The disparity is the difference in the x-coordinates of the same point in the left and right images.

' For example, if the baseline Is 0.5 meters, the focal length Is 1000 pixels, And the disparity Is 100 pixels, then the depth Is

' Z = 0.5 * 1000 / 100 = 5 meters
' The Function() relating depth To disparity Is only valid For a calibrated stereo setup.
' If the stereo setup Is Not calibrated, then the function will not be accurate.
'Public Class DisparityFunction_Basics : Inherits TaskParent
'    Dim match As New FeatureLeftRight_Basics
'    Dim depthStr As String
'    Dim dispStr As String
'    Public Sub New()
'        labels = {"", "", "AddWeighted output: lines show disparity between left and right images",
'                  "Disparity as a function of depth"}
'        desc = "Using FeatureMatch results build a function for disparity given depth"
'    End Sub
'    Public Function disparityFormula(depth As Single) As Integer
'        If depth = 0 Then Return 0
'        Return task.baseline * 1000 * task.focalLength / depth
'    End Function
'    Public Overrides sub RunAlg(src As cv.Mat)
'        If task.cameraName = "Azure Kinect 4K" Then
'            SetTrueText("Kinect for Azure does not have a left and right view to compute disparities", 2)
'            Exit Sub
'        End If
'        match.Run(src)
'        dst2 = match.dst1
'        If match.lpList.Count = 0 Then Exit Sub ' no data...

'        Dim disparity As New SortedList(Of Integer, Single)(New compareAllowIdenticalIntegerInverted)
'        For i = 0 To match.lpList.Count - 1
'            Dim lp = match.lpList(i)
'            disparity.Add(lp.p1.X - lp.p2.X, match.mpCorrelation(i))
'        Next

'        If task.heartBeat Then
'            dispStr = "Disparity: " + vbCrLf
'            depthStr = "Depth: " + vbCrLf
'            Dim index As Integer
'            For Each entry In disparity
'                dispStr += CStr(entry.Key) + ", "
'                depthStr += Format(entry.Value, fmt1) + ", "
'                index += 1
'                If index Mod 20 = 0 Then strOut += vbCrLf
'                If index Mod 20 = 0 Then depthStr += vbCrLf
'            Next

'            Dim testIndex = Math.Min(disparity.Count - 1, 10)
'            Dim actualDisparity = task.disparityAdjustment * disparity.ElementAt(testIndex).Key
'            Dim actualDepth = disparity.ElementAt(testIndex).Value

'            strOut = "Computing disparity from depth: disparity = "
'            strOut += "baseline * focal length / actual depth" + vbCrLf
'            strOut += "A disparity adjustment that is dependent on working resolution is used here " + vbCrLf
'            strOut += "to adjust the observed disparity to match the formula." + vbCrLf
'            strOut += "At working resolution = " + CStr(task.dst2.Width) + "x" + CStr(task.dst2.Height)
'            strOut += " the adjustment factor is " + Format(task.disparityAdjustment, fmt1) + vbCrLf + vbCrLf

'            Dim disparityformulaoutput = disparityFormula(actualDepth)
'            strOut += "At actual depth " + Format(actualDepth, fmt3) + vbCrLf

'            strOut += "Disparity formula is: " + Format(task.baseline, fmt3)
'            strOut += " * " + Format(task.focalLength, fmt3) + " * 1000 / " + Format(actualDepth, fmt3) + vbCrLf

'            strOut += "Disparity formula:" + vbTab + Format(disparityformulaoutput, fmt1) + " pixels" + vbCrLf
'            strOut += "Disparity actual:" + vbTab + vbTab + Format(actualDisparity, fmt1) + " pixels" + vbCrLf
'            strOut += "Predicted disparity = " + "baseline * focal length * 1000 / actual depth " +
'                      "/ disparityAdjustment" + vbCrLf
'            strOut += "Predicted disparity at " + Format(actualDepth, fmt3) + "m = " +
'                       CStr(CInt(disparityformulaoutput / task.disparityAdjustment)) + " pixels"
'        End If
'        SetTrueText(depthStr + vbCrLf + vbCrLf + dispStr, 3)
'        SetTrueText(strOut, New cv.Point(0, dst2.Height / 3), 3)
'    End Sub
'End Class
