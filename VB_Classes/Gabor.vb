
Imports cv = OpenCvSharp
'https://gist.github.com/kendricktan/93f0da88d0b25087d751ed2244cf770c
'https://medium.com/@anuj_shah/through-the-eyes-of-gabor-filter-17d1fdb3ac97
Public Class Gabor_Basics : Inherits TaskParent
    Public options As New Options_Gabor
    Public Sub New()
        desc = "Explore Gabor kernel"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        dst2 = src.Filter2D(cv.MatType.CV_8UC3, options.gKernel)
    End Sub
End Class



