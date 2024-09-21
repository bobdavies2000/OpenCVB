Imports cvb = OpenCvSharp
Public Class FrameRate_Basics : Inherits VB_Parent
    Dim mats As New Mat_4to1
    Dim frameCounts(4 - 1) As Integer
    Public Sub New()
        desc = "Compare each frame to its last to figure out which frames really changed for each invocation."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static lastImages() As cvb.Mat = {task.color.Clone, task.leftview.Clone,
                                         task.rightview.Clone, task.depthRGB.Clone}
        For i = 0 To frameCounts.Count - 1
            mats.mat(i) = Choose(i + 1, task.color, task.leftview, task.rightview, task.depthRGB).clone()
            mats.mat(i) -= lastImages(i)
            Dim count = mats.mat(i).Sum()
            If count(0) > 0 Or count(1) > 0 Or count(2) > 0 Then frameCounts(i) += 1
            mats.mat(i) = mats.mat(i).Threshold(0, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs
        Next
        If task.heartBeat Then
            strOut = ""
            For i = 0 To frameCounts.Count - 1
                strOut += Choose(i + 1, "Color", "Left", "Right", "Depth") + vbTab + " image frameCount = " + vbTab
                strOut += Format(frameCounts(i), fmt0) + vbTab + " frameCount = " + CStr(task.frameCount) + vbCrLf
            Next
        End If
        SetTrueText(strOut, 3)
        mats.Run(empty)
        dst2 = mats.dst2
        lastImages = {task.color.Clone, task.leftview.Clone, task.rightview.Clone, task.depthRGB.Clone}
    End Sub
End Class







Public Class FrameRate_BasicsGray : Inherits VB_Parent
    Dim mats As New Mat_4to1
    Dim frameCounts(4 - 1) As Integer
    Public Sub New()
        desc = "Compare each frame to its last to figure out which frames really changed for each invocation."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static lastImages() As cvb.Mat = {task.color.Clone, task.leftview.Clone,
                                         task.rightview.Clone, task.depthRGB.Clone}
        For i = 0 To frameCounts.Count - 1
            mats.mat(i) = Choose(i + 1, task.color, task.leftView, task.rightView, task.depthRGB).clone()
            If mats.mat(i).Channels > 1 Then
                mats.mat(i) = mats.mat(i).CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
                lastImages(i) = lastImages(i).CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
            Else
                mats.mat(i) = mats.mat(i)
                lastImages(i) = lastImages(i)
            End If
            mats.mat(i) -= lastImages(i)
            Dim count = mats.mat(i).CountNonZero()
            If count > 0 Then frameCounts(i) += 1
            mats.mat(i) = mats.mat(i).Threshold(0, 255, cvb.ThresholdTypes.Binary)
        Next
        If task.heartBeat Then
            strOut = ""
            For i = 0 To frameCounts.Count - 1
                strOut += Choose(i + 1, "Color", "Left", "Right", "Depth") + vbTab + " image frameCount = " + vbTab
                strOut += Format(frameCounts(i), fmt0) + vbTab + " frameCount = " + CStr(task.frameCount) + vbCrLf
            Next
        End If
        SetTrueText(strOut, 3)
        mats.Run(empty)
        dst2 = mats.dst2

        lastImages = {task.color.Clone, task.leftview.Clone, task.rightview.Clone, task.depthRGB.Clone}
    End Sub
End Class