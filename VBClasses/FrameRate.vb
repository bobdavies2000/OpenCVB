Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class FrameRate_Basics : Inherits TaskParent
        Dim mats As New Mat_4to1
        Dim frameCounts(4 - 1) As Integer
        Public Sub New()
            desc = "Compare each frame to its last to figure out which frames really changed for each invocation."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static lastImages() As cv.Mat = {taskAlg.color.Clone, taskAlg.leftView.Clone,
                                             taskAlg.rightView.Clone, taskAlg.depthRGB.Clone}
            For i = 0 To frameCounts.Count - 1
                mats.mat(i) = Choose(i + 1, taskAlg.color, taskAlg.leftView, taskAlg.rightView, taskAlg.depthRGB).clone()
                mats.mat(i) -= lastImages(i)
                Dim count = mats.mat(i).Sum()
                If count(0) > 0 Or count(1) > 0 Or count(2) > 0 Then frameCounts(i) += 1
                mats.mat(i) = mats.mat(i).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            Next
            If taskAlg.heartBeat Then
                strOut = ""
                For i = 0 To frameCounts.Count - 1
                    strOut += Choose(i + 1, "Color", "Left", "Right", "Depth") + vbTab + " image frameCount = " + vbTab
                    strOut += Format(frameCounts(i), fmt0) + vbTab + " frameCount = " + CStr(taskAlg.frameCount) + vbCrLf
                Next
            End If
            SetTrueText(strOut, 3)
            mats.Run(emptyMat)
            dst2 = mats.dst2
            lastImages = {taskAlg.color.Clone, taskAlg.leftView.Clone, taskAlg.rightView.Clone, taskAlg.depthRGB.Clone}
        End Sub
    End Class







    Public Class FrameRate_BasicsGray : Inherits TaskParent
        Dim mats As New Mat_4to1
        Dim frameCounts(4 - 1) As Integer
        Public Sub New()
            desc = "Compare each frame to its last to figure out which frames really changed for each invocation."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static lastImages() As cv.Mat = {taskAlg.color.Clone, taskAlg.leftview.Clone,
                                         taskAlg.rightview.Clone, taskAlg.depthRGB.Clone}
            For i = 0 To frameCounts.Count - 1
                mats.mat(i) = Choose(i + 1, taskAlg.color, taskAlg.leftView, taskAlg.rightView, taskAlg.depthRGB).clone()
                If mats.mat(i).Channels > 1 Then
                    mats.mat(i) = mats.mat(i).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                    lastImages(i) = lastImages(i).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                Else
                    mats.mat(i) = mats.mat(i)
                    lastImages(i) = lastImages(i)
                End If
                mats.mat(i) -= lastImages(i)
                Dim count = mats.mat(i).CountNonZero()
                If count > 0 Then frameCounts(i) += 1
                mats.mat(i) = mats.mat(i).Threshold(0, 255, cv.ThresholdTypes.Binary)
            Next
            If taskAlg.heartBeat Then
                strOut = ""
                For i = 0 To frameCounts.Count - 1
                    strOut += Choose(i + 1, "Color", "Left", "Right", "Depth") + vbTab + " image frameCount = " + vbTab
                    strOut += Format(frameCounts(i), fmt0) + vbTab + " frameCount = " + CStr(taskAlg.frameCount) + vbCrLf
                Next
            End If
            SetTrueText(strOut, 3)
            mats.Run(emptyMat)
            dst2 = mats.dst2

            lastImages = {taskAlg.color.Clone, taskAlg.leftview.Clone, taskAlg.rightview.Clone, taskAlg.depthRGB.Clone}
        End Sub
    End Class
End Namespace