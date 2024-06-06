
Imports cv = OpenCvSharp
'https://gist.github.com/kendricktan/93f0da88d0b25087d751ed2244cf770c
'https://medium.com/@anuj_shah/through-the-eyes-of-gabor-filter-17d1fdb3ac97
Public Class Gabor_Basics : Inherits VB_Parent
    Public gKernel As cv.Mat
    Public ksize As Double
    Public Sigma As Double
    Public theta As Double
    Public lambda As Double
    Public gamma As Double
    Public phaseOffset As Double
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Gabor Kernel Size", 0, 50, 15)
            sliders.setupTrackBar("Gabor Sigma", 0, 100, 4)
            sliders.setupTrackBar("Gabor Theta (degrees)", 0, 180, 90)
            sliders.setupTrackBar("Gabor lambda", 0, 100, 10)
            sliders.setupTrackBar("Gabor gamma X10", 0, 10, 5)
            sliders.setupTrackBar("Gabor Phase offset X100", 0, 100, 0)
        End If
        desc = "Explore Gabor kernel"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static ksizeSlider = FindSlider("Gabor Kernel Size")
        Static sigmaSlider = FindSlider("Gabor Sigma")
        Static lambdaSlider = FindSlider("Gabor lambda")
        Static gammaSlider = FindSlider("Gabor gamma X10")
        Static phaseSlider = FindSlider("Gabor Phase offset X100")
        Static thetaSlider = FindSlider("Gabor Theta (degrees)")
        If standaloneTest() Then
            ksize = ksizeSlider.Value * 2 + 1
            Sigma = sigmaSlider.Value
            lambda = lambdaSlider.Value
            gamma = gammaSlider.Value / 10
            phaseOffset = phaseSlider.Value / 1000
            theta = Math.PI * thetaSlider.Value / 180
        End If
        gKernel = cv.Cv2.GetGaborKernel(New cv.Size(ksize, ksize), Sigma, theta, lambda, gamma, phaseOffset, cv.MatType.CV_32F)
        Dim multiplier = gKernel.Sum()
        gKernel /= 1.5 * multiplier(0)
        dst2 = src.Filter2D(cv.MatType.CV_8UC3, gKernel)
    End Sub
End Class





Public Class Gabor_Basics_MT : Inherits VB_Parent
    Dim gabor(31) As Gabor_Basics
    Dim grid As New Grid_Rectangles
    Public Sub New()
        labels(3) = "The 32 kernels used"
        FindSlider("Grid Cell Width").Maximum = dst2.Width / 2
        FindSlider("Grid Cell Height").Maximum = dst2.Height / 2
        FindSlider("Grid Cell Width").Value = dst2.Width / 8
        FindSlider("Grid Cell Height").Value = dst2.Height / 4

        For i = 0 To gabor.Length - 1
            gabor(i) = New Gabor_Basics()
            gabor(i).theta = i * 180 / gabor.Length
        Next

        desc = "Apply multiple Gabor filters sweeping through different values of theta."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        grid.Run(src)

        Static ksizeSlider = FindSlider("Gabor Kernel Size")
        Static sigmaSlider = FindSlider("Gabor Sigma")
        Static lambdaSlider = FindSlider("Gabor lambda")
        Static gammaSlider = FindSlider("Gabor gamma X10")
        Static phaseSlider = FindSlider("Gabor Phase offset X100")
        Static thetaSlider = FindSlider("Gabor Theta (degrees)")
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
        Parallel.For(0, task.gridList.Count,
        Sub(i)
            Dim roi = task.gridList(i)
            gabor(i).Run(src)
            SyncLock accum
                cv.Cv2.Max(accum, gabor(i).dst2, accum)
                dst3(roi) = gabor(i).gKernel.Normalize(0, 255, cv.NormTypes.MinMax).Resize(New cv.Size(roi.Width, roi.Height))
            End SyncLock
        End Sub)
        dst2 = accum
    End Sub
End Class


