Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class FrameRate_Basics : Inherits TaskParent
    Dim mats As New Mat_4to1
    Dim frameCounts(4 - 1) As Integer
    Public Sub New()
        desc = "Compare each frame to its last to figure out which frames really changed for each invocation."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static lastImages() As cv.Mat = {task.color.Clone, task.leftView.Clone,
                                                 task.rightView.Clone, task.depthRGB.Clone}
        For i = 0 To frameCounts.Count - 1
            mats.mat(i) = Choose(i + 1, task.color, task.leftView, task.rightView, task.depthRGB).clone()
            mats.mat(i) -= lastImages(i)
            Dim count = cv.Cv2.Sum(mats.mat(i))
            If count(0) > 0 Or count(1) > 0 Or count(2) > 0 Then frameCounts(i) += 1
            cv.Cv2.Threshold(mats.mat(i), mats.mat(i), 0, 255, cv.ThresholdTypes.Binary)
            cv.Cv2.ConvertScaleAbs(mats.mat(i), mats.mat(i))
        Next
        If task.heartBeat Then
            strOut = ""
            For i = 0 To frameCounts.Count - 1
                strOut += Choose(i + 1, "Color", "Left", "Right", "Depth") + vbTab + " image frameCount = " + vbTab
                strOut += Format(frameCounts(i), fmt0) + vbTab + " frameCount = " + CStr(task.frameCount) + vbCrLf
            Next
        End If
        SetTrueText(strOut, 3)
        mats.Run(emptyMat)
        dst2 = mats.dst2
        lastImages = {task.color.Clone, task.leftView.Clone, task.rightView.Clone, task.depthRGB.Clone}
    End Sub
End Class







Public Class XR_FrameRate_BasicsGray : Inherits TaskParent
    Dim mats As New Mat_4to1
    Dim frameCounts(4 - 1) As Integer
    Public Sub New()
        desc = "Compare each frame to its last to figure out which frames really changed for each invocation."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static lastImages() As cv.Mat = {task.color.Clone, task.leftView.Clone,
                                             task.rightView.Clone, task.depthRGB.Clone}
        For i = 0 To frameCounts.Count - 1
            mats.mat(i) = Choose(i + 1, task.color, task.leftView, task.rightView, task.depthRGB).clone()
            If mats.mat(i).Channels > 1 Then
                cv.Cv2.CvtColor(mats.mat(i), mats.mat(i), cv.ColorConversionCodes.BGR2GRAY)
                cv.Cv2.CvtColor(lastImages(i), lastImages(i), cv.ColorConversionCodes.BGR2GRAY)
            Else
                mats.mat(i) = mats.mat(i)
                lastImages(i) = lastImages(i)
            End If
            mats.mat(i) -= lastImages(i)
            Dim count = cv.Cv2.CountNonZero(mats.mat(i))
            If count > 0 Then frameCounts(i) += 1
            cv.Cv2.Threshold(mats.mat(i), mats.mat(i), 0, 255, cv.ThresholdTypes.Binary)
        Next
        If task.heartBeat Then
            strOut = ""
            For i = 0 To frameCounts.Count - 1
                strOut += Choose(i + 1, "Color", "Left", "Right", "Depth") + vbTab + " image frameCount = " + vbTab
                strOut += Format(frameCounts(i), fmt0) + vbTab + " frameCount = " + CStr(task.frameCount) + vbCrLf
            Next
        End If
        SetTrueText(strOut, 3)
        mats.Run(emptyMat)
        dst2 = mats.dst2

        lastImages = {task.color.Clone, task.leftView.Clone, task.rightView.Clone, task.depthRGB.Clone}
    End Sub
End Class
