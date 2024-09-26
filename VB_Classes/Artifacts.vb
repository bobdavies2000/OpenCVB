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






Public Class Artifacts_CellSize : Inherits VB_Parent
    Dim feat As New Feature_Basics
    Dim lowRes As New Artifacts_LowRes
    Dim recompute As Boolean = True
    Public distance As Integer
    Public Sub New()
        FindSlider("Min Distance to next").Value = 3
        desc = "Identify the cell size from the Low Res image features"
    End Sub
    Public Sub recomputeDistance()
        If recompute Then
            Dim sortedOffsets As New SortedList(Of Integer, Boolean)
            Dim x = dst2.Width / 2
            Dim y = dst2.Height / 2
            For i = 0 To dst2.Width - 2
                Dim v1 = dst2.Get(Of cvb.Vec3b)(y, i)
                Dim v2 = dst2.Get(Of cvb.Vec3b)(y, i + 1)
                sortedOffsets.Add(i, v1 <> v2)
            Next

            If sortedOffsets.Count = 0 Then ' search vertically if nothing found horizontally (a black image will fail.
                For i = 0 To dst2.Height - 2
                    Dim v1 = dst2.Get(Of cvb.Vec3b)(i, x)
                    Dim v2 = dst2.Get(Of cvb.Vec3b)(i + 1, x)
                    sortedOffsets.Add(i, v1 <> v2)
                Next
            End If

            Dim lastOffset As Integer = -1
            Dim offsets As New List(Of Integer)
            For Each ele In sortedOffsets
                If ele.Value Then
                    offsets.Add(ele.Key - lastOffset)
                    lastOffset = ele.Key
                End If
            Next
            If offsets.Count = 0 Then Exit Sub ' try again later...
            distance = offsets.Min
            recompute = False
        End If
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.optionsChanged Then recompute = True

        lowRes.Run(src)
        dst2 = lowRes.dst2

        If recompute Then recomputeDistance()

        If distance <> 0 Then
            feat.Run(lowRes.dst2)

            task.featurePoints.Clear()
            For Each pt In task.features
                Dim p1 = New cvb.Point2f(pt.X - (pt.X Mod distance), pt.Y - (pt.Y Mod distance))
                DrawCircle(dst2, p1, task.DotSize, task.HighlightColor)
                task.featurePoints.Add(p1)
            Next
            strOut = "Found " + CStr(task.features.Count) + " features" + vbCrLf
            strOut += "Average = " + Format(distance, fmt1) + ", " + CStr(Math.Floor(distance)) + " is the cell size (square) "
            SetTrueText(strOut, 3)

            If standaloneTest() Then
                feat.Run(src)
                For Each pt In task.features
                    DrawCircle(dst2, pt, task.DotSize, cvb.Scalar.Black)
                Next
            End If
        End If
    End Sub
End Class
