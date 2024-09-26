Imports MS.Internal
Imports cvb = OpenCvSharp

Public Class Artifacts_LowRes : Inherits VB_Parent
    Dim options As New Options_Resize
    Public dst As New cvb.Mat
    Public dstDepth As New cvb.Mat
    Public Sub New()
        FindRadio("WarpFillOutliers").Enabled = False
        FindRadio("WarpInverseMap").Enabled = False
        desc = "Build a low-res image to start the process of finding artifacts."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim pct = options.resizePercent
        dst = src.Resize(New cvb.Size(pct * src.Width, pct * src.Height), 0, 0, options.warpFlag)
        dst2 = dst.Resize(New cvb.Size(src.Width, src.Height), 0, 0, options.warpFlag)

        dstDepth = task.depthRGB.Resize(New cvb.Size(pct * src.Width, pct * src.Height), 0, 0, options.warpFlag)
        dst3 = dstDepth.Resize(New cvb.Size(src.Width, src.Height), 0, 0, options.warpFlag)
    End Sub
End Class







Public Class Artifacts_Reduction : Inherits VB_Parent
    Dim lowRes As New Artifacts_LowRes
    Dim color8U As New Color8U_Basics
    Public Sub New()
        desc = "Build a lowRes image after reduction"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        color8U.Run(src)

        lowRes.Run(color8U.dst3)
        dst2 = lowRes.dst3
    End Sub
End Class






Public Class Artifacts_Features : Inherits VB_Parent
    Dim lowRes As New Artifacts_LowRes
    Dim feat As New Feature_Basics
    Public Sub New()
        task.redOptions.ColorSource.SelectedItem() = "Color8U_Grayscale"
        FindSlider("Resize Percentage (%)").Value = 20
        labels = {"", "", "LowRes image with highlighted features", "Same features highlighted in the original image."}
        desc = "Find features in a low res image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lowRes.Run(src)
        feat.Run(lowRes.dst2)
        dst2 = feat.dst2

        dst3 = src
        For Each pt In task.features
            DrawCircle(dst3, pt, task.DotSize, task.HighlightColor)
        Next
    End Sub
End Class








Public Class Artifacts_CellSize1 : Inherits VB_Parent
    Dim feat As New Feature_Basics
    Dim lowRes As New Artifacts_LowRes
    Dim knn As New KNN_Basics
    Public Sub New()
        FindSlider("Min Distance to next").Value = 10
        desc = "Identify the cell size from the Low Res image features"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lowRes.Run(src)
        feat.Run(lowRes.dst2)
        dst2 = lowRes.dst2
        knn.queries = New List(Of cvb.Point2f)(task.features)
        If task.FirstPass Then knn.trainInput = New List(Of cvb.Point2f)(task.features)
        knn.Run(Nothing)

        Dim distances As New List(Of Single)
        For i = 0 To knn.queries.Count - 1
            Dim p1 = knn.queries(i)
            Dim p2 = knn.trainInput(knn.result(i, 1))
            distances.Add(p1.DistanceTo(p2))
            DrawCircle(dst2, p1, task.DotSize, task.HighlightColor)
        Next
        Dim avg = distances.Average
        If task.heartBeat Then
            strOut = "Found " + CStr(knn.queries.Count) + " features" + vbCrLf
            strOut += Format(avg, fmt1) + " is the cell size (square) - so grid is " + CStr(CInt(avg)) + " pixels"
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class








Public Class Artifacts_CellSize : Inherits VB_Parent
    Dim feat As New Feature_Basics
    Dim lowRes As New Artifacts_LowRes
    Public Sub New()
        desc = "Identify the cell size from the Low Res image features"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lowRes.Run(src)
        dst2 = lowRes.dst2

        Dim offsets As New List(Of Integer)
        For i = 0 To dst2.Width - 2
            Dim v1 = dst2.Get(Of cvb.Vec3b)(0, i)
            Dim v2 = dst2.Get(Of cvb.Vec3b)(0, i + 1)
            If v1 <> v2 Then offsets.Add(i)
        Next

        Dim distances As New List(Of Integer)
        For i = 0 To offsets.Count - 2
            distances.Add(offsets(i + 1) - offsets(i))
        Next

        feat.Run(lowRes.dst2)

        For Each pt In task.features
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
        Next
        If task.heartBeat Then
            strOut = "Found " + CStr(task.features.Count) + " features" + vbCrLf
            strOut += CStr(CInt(distances.Average)) + " is the cell size (square) "
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class
