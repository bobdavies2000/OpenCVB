Imports cv = OpenCvSharp
Module FaceDetection_Exports
    Public Sub detectFace(ByRef src As cv.Mat, cascade As cv.CascadeClassifier)
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim faces() = cascade.DetectMultiScale(gray, 1.08, 3, cv.HaarDetectionTypes.ScaleImage, New cv.Size(30, 30))
        For Each face In faces
            src.Rectangle(face, cv.Scalar.Red, task.lineWidth, task.lineType)
        Next
    End Sub
End Module




' https://docs.opencv.org/2.4/doc/tutorials/objdetect/cascade_classifier/cascade_classifier.html
Public Class Face_Haar_LBP : Inherits VB_Algorithm
    Dim haarCascade As cv.CascadeClassifier
    Dim lbpCascade As cv.CascadeClassifier
    Public Sub New()
        haarCascade = New cv.CascadeClassifier(task.homeDir + "Data/haarcascade_frontalface_default.xml")
        lbpCascade = New cv.CascadeClassifier(task.homeDir + "Data/lbpcascade_frontalface.xml")
        desc = "Detect faces in the video stream."
        labels(2) = "Faces detected with Haar"
        labels(3) = "Faces detected with LBP"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        dst2 = src.Clone()
        detectFace(dst2, haarCascade)
        dst3 = src.Clone()
        detectFace(dst3, lbpCascade)
    End Sub
End Class





Public Class Face_Haar_Alt : Inherits VB_Algorithm
    Dim haarCascade As cv.CascadeClassifier
    Public Sub New()
        haarCascade = New cv.CascadeClassifier(task.homeDir + "Data/haarcascade_frontalface_alt.xml")
        desc = "Detect faces Haar_alt database."
        labels(2) = "Faces detected with Haar_Alt"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        dst2 = src.Clone()
        detectFace(dst2, haarCascade)
    End Sub
End Class




