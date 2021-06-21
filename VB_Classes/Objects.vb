Imports cv = OpenCvSharp
Public Class Object_Basics : Inherits VBparent
    Dim km As New KMeans_CCompMasks
    Public Sub New()
        label1 = "Objects - click centroid to confirm"
        task.desc = "Identify objects in RGB"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        km.Run(src)
        dst1 = km.dst1
        dst2 = km.dst2
    End Sub
End Class


