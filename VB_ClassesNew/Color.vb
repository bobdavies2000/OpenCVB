Imports cv = OpenCvSharp
Public Class Color_Basics : Inherits TaskParent
    Public options As New Options_Color
    Public Sub New()
        desc = "Choose a color source"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        If options.colorFormat Is Nothing Then options.colorFormat = "BGR" ' multiple invocations cause this to be necessary but how to fix?
        Select Case options.colorFormat
            Case "BGR"
                dst2 = src.Clone
            Case "LAB"
                dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2Lab)
            Case "HSV"
                dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
            Case "XYZ"
                dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2XYZ)
            Case "HLS"
                dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2HLS)
            Case "YUV"
                dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2YUV)
            Case "YCrCb"
                dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2YCrCb)
        End Select
    End Sub
End Class