
Imports cv = OpenCvSharp
'https://gist.github.com/kendricktan/93f0da88d0b25087d751ed2244cf770c
'https://medium.com/@anuj_shah/through-the-eyes-of-gabor-filter-17d1fdb3ac97
Public Class Gabor_Basics : Inherits VBparent
    Public gKernel As cv.Mat
    Public ksize As Double
    Public Sigma As Double
    Public theta As Double
    Public lambda As Double
    Public gamma As Double
    Public phaseOffset As Double
    Public Sub New()
        If sliders.Setup(caller, 6) Then
            sliders.setupTrackBar(0, "Gabor Kernel Size", 0, 50, 15)
            sliders.setupTrackBar(1, "Gabor Sigma", 0, 100, 4)
            sliders.setupTrackBar(2, "Gabor Theta (degrees)", 0, 180, 90)
            sliders.setupTrackBar(3, "Gabor lambda", 0, 100, 10)
            sliders.setupTrackBar(4, "Gabor gamma X10", 0, 10, 5)
            sliders.setupTrackBar(5, "Gabor Phase offset X100", 0, 100, 0)
        End If
        task.desc = "Explore Gabor kernel - Painterly Effect"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static ksizeSlider = findSlider("Gabor Kernel Size")
        Static sigmaSlider = findSlider("Gabor Sigma")
        Static lambdaSlider = findSlider("Gabor lambda")
        Static gammaSlider = findSlider("Gabor gamma X10")
        Static phaseSlider = findSlider("Gabor Phase offset X100")
        Static thetaSlider = findSlider("Gabor Theta (degrees)")
        If standalone Then
            ksize = ksizeSlider.Value * 2 + 1
            Sigma = sigmaSlider.Value
            lambda = lambdaSlider.Value
            gamma = gammaSlider.Value / 10
            phaseOffset = phaseSlider.Value / 1000
            theta = Math.PI * thetaSlider.value / 180
        End If
        gKernel = cv.Cv2.GetGaborKernel(New cv.Size(ksize, ksize), Sigma, theta, lambda, gamma, phaseOffset, cv.MatType.CV_32F)
        Dim multiplier = gKernel.Sum()
        gKernel /= 1.5 * multiplier.Item(0)
        dst2 = src.Filter2D(cv.MatType.CV_8UC3, gKernel)
    End Sub
End Class





Public Class Gabor_Basics_MT : Inherits VBparent
    Dim grid As New Thread_Grid
    Dim gabor(31) As Gabor_Basics
    Public Sub New()
        label2 = "The 32 kernels used"
        findSlider("ThreadGrid Width").Value = dst2.Width / 8
        findSlider("ThreadGrid Height").Value = dst2.Height / 4

        grid.Run(Nothing) ' we only run this one time!  It needs to be 32 Gabor filters only.

        For i = 0 To gabor.Length - 1
            gabor(i) = New Gabor_Basics()
            gabor(i).theta = i * 180 / gabor.Length
        Next

        task.desc = "Apply multiple Gabor filters sweeping through different values of theta - Painterly Effect."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static ksizeSlider = findSlider("Gabor Kernel Size")
        Static sigmaSlider = findSlider("Gabor Sigma")
        Static lambdaSlider = findSlider("Gabor lambda")
        Static gammaSlider = findSlider("Gabor gamma X10")
        Static phaseSlider = findSlider("Gabor Phase offset X100")
        Static thetaSlider = findSlider("Gabor Theta (degrees)")
        For i = 0 To gabor.Count - 1
            gabor(i).ksize = ksizeSlider.Value * 2 + 1
            gabor(i).Sigma = sigmaSlider.Value
            gabor(i).lambda = lambdaSlider.Value
            gabor(i).gamma = gammaSlider.Value / 10
            gabor(i).phaseOffset = phaseSlider.Value / 1000
            gabor(i).theta = Math.PI * i / gabor.Length
        Next

        Dim accum = src.Clone()
        dst3 = New cv.Mat(src.Height, src.Width, cv.MatType.CV_32F, 0)
        Parallel.For(0, grid.roiList.Count,
        Sub(i)
            Dim roi = grid.roiList(i)
            gabor(i).Run(src)
            SyncLock accum
                cv.Cv2.Max(accum, gabor(i).dst2, accum)
                dst3(roi) = gabor(i).gKernel.Normalize(0, 255, cv.NormTypes.MinMax).Resize(New cv.Size(roi.Width, roi.Height))
            End SyncLock
        End Sub)
        dst2 = accum
    End Sub
End Class


