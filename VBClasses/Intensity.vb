Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Intensity_Basics : Inherits TaskParent
        Public Sub New()
            If radio.Setup(traceName) Then
                radio.addRadio("Log transform")
                radio.addRadio("Gamma correction")
                radio.addRadio("Autoscaling")
                radio.addRadio("Contrast stretching")
                radio.addRadio("BIMEF")
                radio.addRadio("BIMEF with exposure")
                radio.check(0).Checked = True
            End If
            If sliders.Setup(traceName) Then
                sliders.setupTrackBar("Intensity gamma x100", 10, 300, 50)
                sliders.setupTrackBar("BIMEF exposure x100", 50, 300, 150)
            End If
            labels(3) = "Input"
            desc = "Apply cv::intensity_transform through Cv2.IntensityTransform."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Type <> MatType.CV_8UC3 Then src = task.color
            dst3 = src

            Static gammaSlider = OptionParent.FindSlider("Intensity gamma x100")
            Static exposureSlider = OptionParent.FindSlider("BIMEF exposure x100")
            Static frm = OptionParent.FindFrm(traceName + " Radio Buttons")
            Dim mode As Integer
            For mode = 0 To frm.check.Count - 1
                If frm.check(mode).Checked Then Exit For
            Next

            Dim output As New Mat
            Try
                Select Case mode
                    Case 0
                        Cv2.IntensityTransform.LogTransform(src, output)
                        labels(2) = "Cv2.IntensityTransform.LogTransform"
                    Case 1
                        Dim gamma = CSng(gammaSlider.Value / 100.0)
                        Cv2.IntensityTransform.GammaCorrection(src, output, gamma)
                        labels(2) = "Cv2.IntensityTransform.GammaCorrection gamma=" + gamma.ToString(fmt2)
                    Case 2
                        Cv2.IntensityTransform.Autoscaling(src, output)
                        labels(2) = "Cv2.IntensityTransform.Autoscaling"
                    Case 3
                        Cv2.IntensityTransform.ContrastStretching(src, output, 64, 32, 192, 224)
                        labels(2) = "Cv2.IntensityTransform.ContrastStretching r1=64 s1=32 r2=192 s2=224"
                    Case 4
                        Cv2.IntensityTransform.Bimef(src, output)
                        labels(2) = "Cv2.IntensityTransform.Bimef"
                    Case Else
                        Dim exposure = CSng(exposureSlider.Value / 100.0)
                        Cv2.IntensityTransform.Bimef(src, output, exposure, 0.5F, -0.3293F, 1.1258F)
                        labels(2) = "Cv2.IntensityTransform.Bimef exposure=" + exposure.ToString(fmt2)
                End Select
                dst2 = output
            Catch ex As OpenCVException
                dst2 = src
                labels(2) = ex.Message
                SetTrueText(ex.Message, 2)
            End Try
        End Sub
    End Class
End Namespace
