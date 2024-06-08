Imports cv = OpenCvSharp
Public Class Mesh_Basics : Inherits VB_Parent
    Dim knn As New KNN_Core
    Public ptList As New List(Of cv.Point2f)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Number of nearest neighbors", 1, 10, 2)
        desc = "Build triangles from the ptList input of points."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static nabeSlider = FindSlider("Number of nearest neighbors")
        Dim nabeCount = nabeSlider.value
        dst2 = src
        If task.heartBeat And standaloneTest() Then
            Dim feat As New Feature_Basics
            feat.Run(src)
            ptList = task.features
        End If

        If ptList.Count <= 3 Then Exit Sub

        knn.queries = ptList
        knn.trainInput = knn.queries
        knn.Run(empty)

        For i = 0 To knn.queries.Count - 1
            Dim ptLast = knn.queries(i)
            For j = 1 To nabeCount - 1
                Dim pt = knn.queries(knn.result(i, j))
                DrawLine(dst2, ptLast, pt, white)
                ptLast = pt
            Next
        Next

        dst3.SetTo(0)
        For i = 0 To knn.queries.Count - 1
            DrawCircle(dst2,knn.queries(i), task.dotSize, cv.Scalar.Red)
            DrawCircle(dst3,knn.queries(i), task.dotSize, task.highlightColor)
        Next
        labels(2) = "Triangles built each input point and its " + CStr(nabeCount) + " nearest neighbors."
    End Sub
End Class






Public Class Mesh_Features : Inherits VB_Parent
    Dim feat As New Feature_Basics
    Dim mesh As New Mesh_Basics
    Public Sub New()
        FindSlider("Min Distance to next").Value = 10
        labels(2) = "Triangles built with each feature point and the specified number of nearest neighbors."
        UpdateAdvice(traceName + ": Use 'Options_Features' to update results.")
        desc = "Build triangles from feature points"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        If task.features.Count < 3 Then Exit Sub
        mesh.ptList = task.features
        mesh.Run(src)
        dst2 = mesh.dst2
        dst3 = mesh.dst3

        Dim pad = feat.options.templatePad
        Dim size = feat.options.templateSize
        Dim depthMiss As Integer
        For Each pt In task.features
            Dim depth = task.pcSplit(2).Get(Of Single)(pt.Y, pt.X)
            If depth = 0 Then
                Dim r = validateRect(New cv.Rect(pt.X - pad, pt.Y - pad, size, size))
                depth = task.pcSplit(2)(r).Mean(task.depthMask(r))(0)
                depthMiss += 1
            End If
            ' setTrueText(Format(depth, fmt1) + "m ", pt)
        Next

        labels(2) = mesh.labels(2)
        labels(3) = CStr(depthMiss) + " of " + CStr(mesh.ptList.Count) + " features had no depth at that location.  Depth is an average around it for those missing depth."
    End Sub
End Class
