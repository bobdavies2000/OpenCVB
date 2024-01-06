Imports cv = OpenCvSharp
Public Class FrameRate_Basics : Inherits VB_Algorithm
    Dim mats As New Mat_4to1
    Dim frameCounts(4 - 1) As Integer
    Public Sub New()
        desc = "Compare each frame to its last to figure out which frames really changed for each invocation."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static lastImages() As cv.Mat = {task.color.Clone, task.leftview.Clone,
                                         task.rightview.Clone, task.depthRGB.Clone}
        For i = 0 To frameCounts.Count - 1
            mats.mat(i) = Choose(i + 1, task.color, task.leftview, task.rightview, task.depthRGB).clone()
            mats.mat(i) -= lastImages(i)
            Dim count = mats.mat(i).Sum()
            If count(0) > 0 Or count(1) > 0 Or count(2) > 0 Then frameCounts(i) += 1
            mats.mat(i) = mats.mat(i).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        Next
        If heartBeat() Then
            strOut = ""
            For i = 0 To frameCounts.Count - 1
                strOut += Choose(i + 1, "Color", "Left", "Right", "Depth") + vbTab + " image frameCount = " + vbTab
                strOut += Format(frameCounts(i), fmt0) + vbTab + " frameCount = " + CStr(task.frameCount) + vbCrLf
            Next
        End If
        setTrueText(strOut, 3)
        mats.Run(empty)
        dst2 = mats.dst2
        lastImages = {task.color.Clone, task.leftview.Clone, task.rightview.Clone, task.depthRGB.Clone}
    End Sub
End Class







Public Class FrameRate_BasicsGray : Inherits VB_Algorithm
    Dim mats As New Mat_4to1
    Dim frameCounts(4 - 1) As Integer
    Public Sub New()
        desc = "Compare each frame to its last to figure out which frames really changed for each invocation."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static lastImages() As cv.Mat = {task.color.Clone, task.leftview.Clone,
                                         task.rightview.Clone, task.depthRGB.Clone}
        For i = 0 To frameCounts.Count - 1
            mats.mat(i) = Choose(i + 1, task.color, task.leftview, task.rightview, task.depthRGB).clone()
            mats.mat(i) = mats.mat(i).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            lastImages(i) = lastImages(i).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            mats.mat(i) -= lastImages(i)
            Dim count = mats.mat(i).CountNonZero()
            If count > 0 Then frameCounts(i) += 1
            mats.mat(i) = mats.mat(i).Threshold(0, 255, cv.ThresholdTypes.Binary)
        Next
        If heartBeat() Then
            strOut = ""
            For i = 0 To frameCounts.Count - 1
                strOut += Choose(i + 1, "Color", "Left", "Right", "Depth") + vbTab + " image frameCount = " + vbTab
                strOut += Format(frameCounts(i), fmt0) + vbTab + " frameCount = " + CStr(task.frameCount) + vbCrLf
            Next
        End If
        setTrueText(strOut, 3)
        mats.Run(empty)
        dst2 = mats.dst2

        lastImages = {task.color.Clone, task.leftview.Clone, task.rightview.Clone, task.depthRGB.Clone}
    End Sub
End Class