Imports cv = OpenCvSharp
Public Class Object_Basics : Inherits VBparent
    Dim km As New KMeans_CCompMasks
    Public Sub New()
        labels(2) = "Objects - click centroid to confirm"
        task.desc = "Identify objects in RGB"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        km.RunClass(src)
        dst2 = km.dst2
        dst3 = km.dst3
    End Sub
End Class


