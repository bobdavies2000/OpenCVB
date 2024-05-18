Imports cv = OpenCvSharp
Public Class MatchFeature_Basics : Inherits VB_Algorithm
    Dim feat As New Feature_Basics
    Public options As New Options_Features
    Dim inputMask As New cv.Mat
    Public Sub New()
        desc = "Identify features with Feature_Basics but manage them with MatchTemplate"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst2 = src.Clone
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static features As New List(Of cv.Point2f)(task.features)
        Static featureMat As New List(Of cv.Mat)

        'Dim correlationmat As New cv.Mat
        'For Each mat In featureMat
        '    cv.Cv2.MatchTemplate(task.leftView(RECT), task.rightView(r), correlationmat, cv.TemplateMatchModes.CCoeffNormed)
        'Next
        'task.features = cv.Cv2.GoodFeaturesToTrack(src, options.featurePoints, options.quality, options.minDistance, inputMask,
        '                                           options.blockSize, options.useHarrisDetector, options.k).ToList

    End Sub
End Class
