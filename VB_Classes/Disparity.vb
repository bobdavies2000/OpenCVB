Imports cv = OpenCvSharp
Public Class Disparity_GoodCells : Inherits TaskParent
    Dim grid As New Grid_Basics
    Public gridList As New List(Of cv.Rect)
    Dim options As New Options_Disparity
    Public Sub New()
        grid.myGrid = True ' private grid
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        labels(3) = "Depth image for cells with good visibility"
        desc = "Create the grid of cells with good visibility and can be used to find disparity"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        If task.optionsChanged Then
            grid.inputGridSize = options.cellSize
            grid.Run(src)
        End If

        Dim emptyRect As New cv.Rect
        Dim goodRects As Integer
        gridList.Clear()
        Dim mask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        For Each r In grid.gridRects
            If task.pcSplit(2)(r).CountNonZero = r.Width * r.Height Then
                gridList.Add(r)
                goodRects += 1
                mask(r).SetTo(255)
            Else
                gridList.Add(emptyRect)
            End If
        Next

        mask.SetTo(0, Not task.motionMask) ' no need to copy where there is no motion
        dst3.SetTo(0, task.motionMask)
        task.pointCloud.CopyTo(dst3, mask)

        If standaloneTest() Then
            dst2 = src
            For Each r In gridList
                If r.Width = 0 Then Continue For
                dst2.Rectangle(r, cv.Scalar.White, task.lineWidth)
            Next
            If task.heartBeat Then labels(2) = CStr(goodRects) + " grid cells have the maximum depth pixels."
        End If
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
