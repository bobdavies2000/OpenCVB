Imports cv = OpenCvSharp
Module FaceDetection_Exports
    Public Sub detectFace(ByRef src As cv.Mat, cascade As cv.CascadeClassifier)
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim faces() = cascade.DetectMultiScale(gray, 1.08, 3, cv.HaarDetectionTypes.ScaleImage, New cv.Size(30, 30))
        For Each face In faces
            src.Rectangle(face, cv.Scalar.Red, 1, task.lineType)
        Next
    End Sub
End Module

' https://docs.opencv.org/2.4/doc/tutorials/objdetect/cascade_classifier/cascade_classifier.html
Public Class Face_Haar_LBP
    Inherits VBparent
    Dim haarCascade As cv.CascadeClassifier
    Dim lbpCascade As cv.CascadeClassifier
    Public Sub New()
        initParent()
        haarCascade = New cv.CascadeClassifier(task.parms.homeDir + "Data/haarcascade_frontalface_default.xml")
        lbpCascade = New cv.CascadeClassifier(task.parms.homeDir + "Data/lbpcascade_frontalface.xml")
        task.desc = "Detect faces in the video stream."
		' task.rank = 1
        label1 = "Faces detected with Haar"
        label2 = "Faces detected with LBP"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        dst1 = src.Clone()
        detectFace(dst1, haarCascade)
        dst2 = src.Clone()
        detectFace(dst2, lbpCascade)
    End Sub
End Class



Public Class Face_Haar_Alt
    Inherits VBparent
    Dim haarCascade As cv.CascadeClassifier
    Public Sub New()
        initParent()
        haarCascade = New cv.CascadeClassifier(task.parms.homeDir + "Data/haarcascade_frontalface_alt.xml")
        task.desc = "Detect faces Haar_alt database."
		' task.rank = 1
        label1 = "Faces detected with Haar_Alt"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        dst1 = src.Clone()
        detectFace(dst1, haarCascade)
    End Sub
End Class




