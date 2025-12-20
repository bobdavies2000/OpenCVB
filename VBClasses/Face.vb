Imports cv = OpenCvSharp
' https://docs.opencvb.org/2.4/doc/tutorials/objdetect/cascade_classifier/cascade_classifier.html
Namespace VBClasses
    Public Class Face_Haar_LBP : Inherits TaskParent
        Dim haarCascade As cv.CascadeClassifier
        Dim lbpCascade As cv.CascadeClassifier
        Public Sub New()
            haarCascade = New cv.CascadeClassifier(taskAlg.homeDir + "Data/haarcascade_frontalface_default.xml")
            lbpCascade = New cv.CascadeClassifier(taskAlg.homeDir + "Data/lbpcascade_frontalface.xml")
            desc = "Detect faces in the video stream."
            labels(2) = "Faces detected with Haar"
            labels(3) = "Faces detected with LBP"
        End Sub
        Public Shared Sub DetectFace(ByRef src As cv.Mat, cascade As cv.CascadeClassifier)
            Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim faces() = cascade.DetectMultiScale(gray, 1.08, 3, cv.HaarDetectionTypes.ScaleImage, New cv.Size(30, 30))
            For Each fface In faces
                src.Rectangle(fface, cv.Scalar.Red)
            Next
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src.Clone()
            DetectFace(dst2, haarCascade)
            dst3 = src.Clone()
            DetectFace(dst3, lbpCascade)
        End Sub
    End Class





    Public Class Face_Haar_Alt : Inherits TaskParent
        Dim haarCascade As cv.CascadeClassifier
        Public Sub New()
            haarCascade = New cv.CascadeClassifier(taskAlg.homeDir + "Data/haarcascade_frontalface_alt.xml")
            desc = "Detect faces Haar_alt database."
            labels(2) = "Faces detected with Haar_Alt"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src.Clone()
            Face_Haar_LBP.DetectFace(dst2, haarCascade)
        End Sub
    End Class
End Namespace