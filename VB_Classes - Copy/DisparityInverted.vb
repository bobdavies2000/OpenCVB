Imports cvb = OpenCvSharp
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
Public Class DisparityFunction_Basics : Inherits TaskParent
    Dim match As New FeatureLeftRight_Basics
    Dim depthStr As String
    Dim dispStr As String
    Public Sub New()
        labels = {"", "", "AddWeighted output: lines show disparity between left and right images",
                  "Disparity as a function of depth"}
        desc = "Using FeatureMatch results build a function for disparity given depth"
    End Sub
    Public Function disparityFormula(depth As Single) As Integer
        If depth = 0 Then Return 0
        Return task.baseline * 1000 * task.focalLength / depth
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        If task.cameraName = "Azure Kinect 4K" Then
            SetTrueText("Kinect for Azure does not have a left and right view to compute disparities", 2)
            Exit Sub
        End If
        match.Run(src)
        dst2 = match.dst1
        If match.mpList.Count = 0 Then Exit Sub ' no data...

        Dim disparity As New SortedList(Of Integer, Single)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To match.mpList.Count - 1
            Dim mp = match.mpList(i)
            disparity.Add(mp.p1.X - mp.p2.X, match.mpCorrelation(i))
        Next

        If task.heartBeat Then
            dispStr = "Disparity: " + vbCrLf
            depthStr = "Depth: " + vbCrLf
            Dim index As Integer
            For Each entry In disparity
                dispStr += CStr(entry.Key) + ", "
                depthStr += Format(entry.Value, fmt1) + ", "
                index += 1
                If index Mod 20 = 0 Then strOut += vbCrLf
                If index Mod 20 = 0 Then depthStr += vbCrLf
            Next

            Dim testIndex = Math.Min(disparity.Count - 1, 10)
            Dim actualDisparity = task.disparityAdjustment * disparity.ElementAt(testIndex).Key
            Dim actualDepth = disparity.ElementAt(testIndex).Value

            strOut = "Computing disparity from depth: disparity = "
            strOut += "baseline * focal length / actual depth" + vbCrLf
            strOut += "A disparity adjustment that is dependent on working resolution is used here " + vbCrLf
            strOut += "to adjust the observed disparity to match the formula." + vbCrLf
            strOut += "At working resolution = " + CStr(task.dst2.Width) + "x" + CStr(task.dst2.Height)
            strOut += " the adjustment factor is " + Format(task.disparityAdjustment, fmt1) + vbCrLf + vbCrLf

            Dim disparityformulaoutput = disparityFormula(actualDepth)
            strOut += "At actual depth " + Format(actualDepth, fmt3) + vbCrLf

            strOut += "Disparity formula is: " + Format(task.baseline, fmt3)
            strOut += " * " + Format(task.focalLength, fmt3) + " * 1000 / " + Format(actualDepth, fmt3) + vbCrLf

            strOut += "Disparity formula:" + vbTab + Format(disparityformulaoutput, fmt1) + " pixels" + vbCrLf
            strOut += "Disparity actual:" + vbTab + vbTab + Format(actualDisparity, fmt1) + " pixels" + vbCrLf
            strOut += "Predicted disparity = " + "baseline * focal length * 1000 / actual depth " +
                      "/ disparityAdjustment" + vbCrLf
            strOut += "Predicted disparity at " + Format(actualDepth, fmt3) + "m = " +
                       CStr(CInt(disparityformulaoutput / task.disparityAdjustment)) + " pixels"
        End If
        SetTrueText(depthStr + vbCrLf + vbCrLf + dispStr, 3)
        SetTrueText(strOut, New cvb.Point(0, dst2.Height / 3), 3)
    End Sub
End Class
