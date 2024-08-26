
Imports cvb = OpenCvSharp
'https://gist.github.com/kendricktan/93f0da88d0b25087d751ed2244cf770c
'https://medium.com/@anuj_shah/through-the-eyes-of-gabor-filter-17d1fdb3ac97
Public Class Gabor_Basics : Inherits VB_Parent
    Public options As New Options_Gabor
    Public Sub New()
        desc = "Explore Gabor kernel"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst2 = src.Filter2D(cvb.MatType.CV_8UC3, options.gKernel)
    End Sub
End Class



