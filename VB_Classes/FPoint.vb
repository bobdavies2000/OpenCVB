Imports OpenCvSharp
Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class FPoint_Basics : Inherits TaskParent
    Dim fpt As New FPoint_Core
    Dim fInfo As New FPoint_Info
    Public Sub New()
        FindSlider("Min Distance to next").Value = task.fPointMinDistance
        FindSlider("Feature Sample Size").Value = 250 ' keep within a byte boundary.
        desc = "Create the fpList with rect, mask, index, and facets"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        fpt.Run(src)
        Static lastMap As cvb.Mat = fpt.delaunayF.dst3.Clone
        Static lastList As New List(Of fPoint)(task.fpList)

        dst2.SetTo(0)
        Dim currList As New List(Of fPoint)(task.fpList)
        task.fpList.Clear()
        For i = 0 To task.features.Count - 1
            Dim pt = task.features(i)
            Dim index = lastMap.Get(Of Byte)(pt.Y, pt.X)
            Dim fp = lastList(index)
            fp.pt = pt
            task.fpList.Add(fp)
            dst2(fp.rect).SetTo(fp.index, fp.mask)
        Next

        lastMap = dst2.Clone
        lastList = New List(Of fPoint)(task.fpList)

        task.fpMap = dst2.Clone
        dst2.SetTo(0, task.fpOutline)
        If task.heartBeat Then labels(2) = CStr(task.features.Count) + " feature grid cells."

        If task.ClickPoint <> New cvb.Point Then
            Dim index = task.fpMap.Get(Of Byte)(task.ClickPoint.Y, task.ClickPoint.X)
            task.fpSelected = task.fpList(index)

            If task.ClickPoint.DistanceTo(task.fpSelected.pt) < task.fpSelected.rect.Width / 2 And
                task.ClickPoint.DistanceTo(task.fpSelected.pt) < task.fpSelected.rect.Height / 2 Then
                task.ClickPoint = task.fpSelected.pt
            End If

            dst2(task.fpSelected.rect).SetTo(255, task.fpSelected.mask)
            dst2.Rectangle(task.fpSelected.rect, 255, task.lineWidth)
            fInfo.Run(empty)
            SetTrueText(fInfo.strOut, 3)
        End If
    End Sub
End Class





Public Class FPoint_Core : Inherits TaskParent
    Dim feat As New Feature_Basics
    Public delaunayF As New Delaunay_FPoint
    Public Sub New()
        FindSlider("Feature Sample Size").Value = 255 ' keep within a byte boundary.
        desc = "Divide up the image based on the features found."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)

        delaunayF.inputPoints = task.features
        delaunayF.Run(src)

        dst2 = delaunayF.dst2
        dst3 = delaunayF.dst3

        If standaloneTest() Then
            For i = 0 To delaunayF.inputPoints.Count - 1
                Dim pt = delaunayF.inputPoints(i)
                DrawCircle(dst3, pt, task.DotSize, 255, -1)
            Next
        End If
        If task.heartBeat Then labels(3) = CStr(task.features.Count) + " feature grid cells."
    End Sub
End Class






Public Class FPoint_Info : Inherits TaskParent
    Public Sub New()
        desc = "Display the contents of the FPoint cell."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.fpSelected.index > 0 Then
            strOut = "FPoint selected: " + vbCrLf
            strOut += "Feature point: " + task.fpSelected.pt.ToString + vbCrLf
            strOut += task.fpSelected.rect.ToString + vbCrLf
            strOut += "index = " + CStr(task.fpSelected.index) + vbCrLf
            strOut += "Facet count = " + CStr(task.fpSelected.facet2f.Count) + " facets" + vbCrLf
            strOut += "ClickPoint = " + task.ClickPoint.ToString + vbCrLf
        End If
        If standalone Then
            SetTrueText("Select a feature grid cell to get more information.", 2)
        End If
    End Sub
End Class








Public Class FPoint_BasicsOld : Inherits TaskParent
    Public fpList As New List(Of fPoint)
    Public fpMap As New cvb.Mat(dst2.Size, cvb.MatType.CV_8U, 0)
    Dim feat As New Feature_Basics
    Dim subdiv As New cvb.Subdiv2D
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size, cvb.MatType.CV_8U, 0)
        FindSlider("Feature Sample Size").Value = 255 ' keep within a byte boundary.
        desc = "Divide up the image based on the features found and track each cell."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)

        subdiv.InitDelaunay(New cvb.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(task.features)

        Dim facets = New cvb.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        fpList.Clear()
        fpList.Add(New fPoint) ' index = 0
        Dim mask32s As New cvb.Mat(dst2.Size, cvb.MatType.CV_32S, 0)
        For i = 0 To facets.Length - 1
            Dim fp = New fPoint
            fp.facet2f = New List(Of Point2f)(facets(i))
            fp.facets = New List(Of cvb.Point)

            Dim xlist As New List(Of Integer)
            Dim ylist As New List(Of Integer)
            For j = 0 To facets(i).Length - 1
                Dim pt = New cvb.Point(facets(i)(j).X, facets(i)(j).Y)
                If pt.X < 0 Then pt.X = 0
                If pt.Y < 0 Then pt.Y = 0
                If pt.X >= dst2.Width Then pt.X = dst2.Width - 1
                If pt.Y >= dst2.Height Then pt.Y = dst2.Height - 1
                xlist.Add(pt.X)
                ylist.Add(pt.Y)
                fp.facets.Add(pt)
            Next

            fp.rect = New cvb.Rect(xlist.Min, ylist.Min, xlist.Max - xlist.Min, ylist.Max - ylist.Min)

            mask32s(fp.rect).SetTo(0)
            mask32s.FillConvexPoly(fp.facets, 255, task.lineType)
            mask32s(fp.rect).ConvertTo(fp.mask, cvb.MatType.CV_8U)
            fpList.Add(fp)
        Next

        dst3.SetTo(0)
        If task.heartBeat Then
            For i = 1 To fpList.Count - 1
                Dim fp = fpList(i)
                fp.index = i
                fpList(i) = fp
            Next
        Else
            Dim usedList(fpList.Count - 1) As Boolean
            For i = 1 To fpList.Count - 1
                Dim fp = fpList(i)
                If usedList(fp.index) Then
                    For j = 0 To usedList.Count - 1
                        If usedList(j) = False Then
                            fp.index = j
                            Exit For
                        End If
                    Next
                End If
                usedList(fp.index) = True
                fpList(i) = fp
            Next
        End If

        For i = 1 To fpList.Count - 1
            Dim fp = fpList(i)
            dst3(fp.rect).SetTo(fp.index, fp.mask)
        Next

        dst2 = ShowPalette(dst3)
        fpMap = dst3.Clone

        For i = 1 To fpList.Count - 1
            Dim fp = fpList(i)
            DrawCircle(dst3, fp.pt, task.DotSize, cvb.Scalar.White)
        Next
        If task.heartBeat Then labels(3) = CStr(fpList.Count) + " feature grid entries."
    End Sub
End Class





Public Class FPoint_NoTracking : Inherits TaskParent
    Public inputPoints As New List(Of cvb.Point2f)
    Public facetList As New List(Of List(Of cvb.Point))
    Public facet32s As cvb.Mat
    Dim subdiv As New cvb.Subdiv2D
    Public Sub New()
        facet32s = New cvb.Mat(dst2.Size(), cvb.MatType.CV_32SC1, 0)
        dst1 = New cvb.Mat(dst1.Size, cvb.MatType.CV_8U, 0)
        labels(3) = "CV_8U map of Delaunay cells"
        desc = "Subdivide an image based on the points provided."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standalone Then
            Static feat As New Feature_Basics
            feat.Run(src)
            inputPoints = New List(Of cvb.Point2f)(task.features)
        End If

        subdiv.InitDelaunay(New cvb.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(inputPoints)

        Dim facets = New cvb.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        facetList.Clear()
        For i = 0 To facets.Length - 1
            Dim nextFacet As New List(Of cvb.Point)
            For j = 0 To facets(i).Length - 1
                nextFacet.Add(New cvb.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            facet32s.FillConvexPoly(nextFacet, i, task.lineType)
            facetList.Add(nextFacet)
        Next

        dst1.SetTo(0)
        For i = 0 To facets.Length - 1
            Dim ptList As New List(Of cvb.Point)
            For j = 0 To facets(i).Length - 1
                ptList.Add(New cvb.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            DrawContour(dst1, ptList, 255, 1)
        Next

        facet32s.ConvertTo(dst3, cvb.MatType.CV_8U)
        dst2 = ShowPalette(dst3 * 255 / (facets.Length + 1))

        dst3.SetTo(0, dst1)
        dst2.SetTo(cvb.Scalar.White, dst1)
        labels(2) = traceName + ": " + Format(inputPoints.Count, "000") + " cells were present."
    End Sub
End Class





Public Class FPoint_Lines : Inherits TaskParent
    Dim lines As New Line_Basics
    Dim feat As New Feature_Basics
    Dim edges As New Edge_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        labels = {"", "Edge_Canny", "Line_Basics output", "Feature_Basics Output"}
        desc = "Run Feature_Basics and Line_Basics for comparison."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        edges.Run(src)
        dst1 = edges.dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)

        lines.Run(src)
        dst2 = lines.dst3

        feat.Run(src)
        dst3 = feat.dst2

        For Each pt In task.features
            DrawCircle(dst1, pt, task.DotSize, task.HighlightColor)
        Next
    End Sub
End Class
