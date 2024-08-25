Imports cvb = OpenCvSharp
' https://docs.opencvb.org/2.4/doc/tutorials/objdetect/cascade_classifier/cascade_classifier.html
Public Class Face_Haar_LBP : Inherits VB_Parent
    Dim haarCascade As cvb.CascadeClassifier
    Dim lbpCascade As cvb.CascadeClassifier
    Public Sub New()
        haarCascade = New cvb.CascadeClassifier(task.HomeDir + "Data/haarcascade_frontalface_default.xml")
        lbpCascade = New cvb.CascadeClassifier(task.HomeDir + "Data/lbpcascade_frontalface.xml")
        desc = "Detect faces in the video stream."
        labels(2) = "Faces detected with Haar"
        labels(3) = "Faces detected with LBP"
    End Sub
    Public Sub RunVB(src as cvb.Mat)
        dst2 = src.Clone()
        detectFace(dst2, haarCascade)
        dst3 = src.Clone()
        detectFace(dst3, lbpCascade)
    End Sub
End Class





Public Class Face_Haar_Alt : Inherits VB_Parent
    Dim haarCascade As cvb.CascadeClassifier
    Public Sub New()
        haarCascade = New cvb.CascadeClassifier(task.HomeDir + "Data/haarcascade_frontalface_alt.xml")
        desc = "Detect faces Haar_alt database."
        labels(2) = "Faces detected with Haar_Alt"
    End Sub
    Public Sub RunVB(src as cvb.Mat)
        dst2 = src.Clone()
        detectFace(dst2, haarCascade)
    End Sub
End Class