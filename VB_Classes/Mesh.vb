Imports cv = OpenCvSharp
Public Class Mesh_Basics : Inherits VB_Algorithm
    Dim knn As New KNN_Core
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Number of nearest neighbors", 0, 10, 2)

        labels(2) = "Triangles built with each random point and its 2 nearest neighbors."
        vbAddAdvice(traceName + ": Adjust the number of points with the options_random.")
        desc = "Build triangles from random points"
    End Sub
    Public Function showMesh(pointList As List(Of cv.Point2f)) As cv.Mat
        Static nabeSlider = findSlider("Number of nearest neighbors")
        Dim nabeCount = nabeSlider.value

        If pointList.Count <= 3 Then Return dst2 ' Not enough points To draw...

        knn.queries = pointList
        knn.trainInput = knn.queries
        knn.Run(empty)

        For i = 0 To knn.queries.Count - 1
            Dim ptLast = knn.queries(i)
            For j = 1 To nabeCount - 1
                Dim pt = knn.queries(knn.result(i, j))
                dst2.Line(ptLast, pt, white, task.lineWidth, task.lineType)
                ptLast = pt
            Next
        Next
        dst3.SetTo(0)
        For i = 0 To knn.queries.Count - 1
            dst2.Circle(knn.queries(i), task.dotSize, cv.Scalar.Red, -1, task.lineType)
            dst3.Circle(knn.queries(i), task.dotSize, task.highlightColor, -1, task.lineType)
        Next
        Return dst2
    End Function
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then
            If standaloneTest() Then
                Static random As New Random_Basics
                random.Run(empty)
                dst2.SetTo(0)
                showMesh(random.pointList)
            End If
        End If
    End Sub
End Class






Public Class Mesh_Features : Inherits VB_Algorithm
    Dim feat As New Feature_Basics
    Dim mesh As New Mesh_Basics
    Public Sub New()
        findSlider("Min Distance to next").Value = 10
        labels(2) = "Triangles built with each feature point and the specified number of nearest neighbors."
        vbAddAdvice(traceName + ": Use 'Options_Features' to update results.")
        desc = "Build triangles from feature points"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        If task.features.Count < 3 Then Exit Sub
        mesh.dst2 = src
        dst2 = mesh.showMesh(task.features)
        dst3 = mesh.dst3
    End Sub
End Class
