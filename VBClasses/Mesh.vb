Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Mesh_Basics : Inherits TaskParent
        Dim knn As New KNN_Basics
        Public ptList As New List(Of cv.Point2f)
        Dim options As New Options_Mesh
        Dim feat As New Feature_General
        Public Sub New()
            desc = "Build triangles from the ptList input of points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            feat.Run(algTask.grayStable)

            dst2 = src
            If algTask.heartBeat And standaloneTest() Then ptList = algTask.features

            If ptList.Count <= 3 Then Exit Sub

            knn.queries = ptList
            knn.trainInput = knn.queries
            knn.Run(src)

            For i = 0 To knn.queries.Count - 1
                Dim ptLast = knn.queries(i)
                For j = 1 To options.nabeCount - 1
                    Dim pt = knn.queries(knn.result(i, j))
                    vbc.DrawLine(dst2, ptLast, pt, white)
                    ptLast = pt
                Next
            Next

            dst3.SetTo(0)
            For i = 0 To knn.queries.Count - 1
                DrawCircle(dst2, knn.queries(i), algTask.DotSize, cv.Scalar.Red)
                DrawCircle(dst3, knn.queries(i), algTask.DotSize, algTask.highlight)
            Next
            labels(2) = "Triangles built each input point and its " + CStr(options.nabeCount) + " nearest neighbors."
        End Sub
    End Class






    Public Class Mesh_Features : Inherits TaskParent
        Dim mesh As New Mesh_Basics
        Dim feat As New Feature_General
        Public Sub New()
            labels(2) = "Triangles built with each feature point and the specified number of nearest neighbors."
            desc = "Build triangles from feature points"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            feat.Run(algTask.grayStable)

            If algTask.features.Count < 3 Then Exit Sub
            mesh.ptList = algTask.features
            mesh.Run(src)
            dst2 = mesh.dst2
            dst3 = mesh.dst3

            Dim pad = algTask.brickSize / 2
            Dim depthMiss As Integer
            For Each pt In algTask.features
                Dim depth = algTask.pcSplit(2).Get(Of Single)(pt.Y, pt.X)
                If depth = 0 Then
                    Dim r = ValidateRect(New cv.Rect(pt.X - pad, pt.Y - pad, algTask.brickSize, algTask.brickSize))
                    depth = algTask.pcSplit(2)(r).Mean(algTask.depthMask(r))(0)
                    depthMiss += 1
                End If
                ' SetTrueText(Format(depth, fmt1) + "m ", pt)
            Next

            labels(2) = mesh.labels(2)
            labels(3) = CStr(depthMiss) + " of " + CStr(mesh.ptList.Count) + " features had no depth at that location.  Depth is an average around it for those missing depth."
        End Sub
    End Class
End Namespace