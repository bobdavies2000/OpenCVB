Imports System.Windows.Documents
Imports OpenCvSharp
Imports cvb = OpenCvSharp
Public Class FPoint_Basics : Inherits TaskParent
    Dim feat As New Feature_Basics
    Dim delaunay As New Delaunay_Basics
    Dim ptList As New List(Of cvb.Point2f)
    Public Sub New()
        FindSlider("Feature Sample Size").Value = 255 ' keep within a byte boundary.
        desc = "Divide up the image based on the features found."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)

        If task.FirstPass Then
            ptList = New List(Of cvb.Point2f)(task.features)
        End If

        delaunay.inputPoints.Clear()
        For Each pt In ptList
            Dim val = task.motionMask.Get(Of Byte)(pt.Y, pt.X)
            If val = 0 Then delaunay.inputPoints.Add(pt)
        Next

        For Each pt In task.features
            Dim val = task.motionMask.Get(Of Byte)(pt.Y, pt.X)
            If val <> 0 Then delaunay.inputPoints.Add(pt)
        Next

        delaunay.Run(src)
        dst2 = delaunay.dst2
        dst3 = delaunay.dst3

        ptList = New List(Of cvb.Point2f)(delaunay.inputPoints)
        For Each pt In ptList
            DrawCircle(dst3, pt, task.DotSize, cvb.Scalar.White, -1)
        Next

        If task.heartBeat Then labels(3) = CStr(ptList.Count) + " feature grid facets."
    End Sub
End Class





Public Class FPoint_BasicsNew : Inherits TaskParent
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

        Static ptList As New List(Of cvb.Point2f)
        If task.FirstPass Then
            ptList = New List(Of cvb.Point2f)(task.features)
        Else
            Dim newSet As New List(Of cvb.Point2f)
            For Each pt In ptList
                Dim val = task.motionMask.Get(Of Byte)(pt.Y, pt.X)
                If val = 0 Then newSet.Add(pt)
            Next

            For Each pt In task.features
                Dim val = task.motionMask.Get(Of Byte)(pt.Y, pt.X)
                If val <> 0 Then newSet.Add(pt)
            Next
            ptList = New List(Of cvb.Point2f)(newSet)
        End If





        'Dim facetList As New List(Of List(Of cvb.Point))
        'Dim facet32s As New cvb.Mat(dst2.Size, cvb.MatType.CV_32S, 0)

        'subdiv.InitDelaunay(New cvb.Rect(0, 0, dst2.Width, dst2.Height))
        'subdiv.Insert(ptList)

        'Dim facets1 = New cvb.Point2f()() {Nothing}
        'subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets1, Nothing)

        'facetList.Clear()
        'For i = 0 To facets1.Length - 1
        '    Dim nextFacet As New List(Of cvb.Point)
        '    For j = 0 To facets1(i).Length - 1
        '        nextFacet.Add(New cvb.Point(facets1(i)(j).X, facets1(i)(j).Y))
        '    Next

        '    facet32s.FillConvexPoly(nextFacet, i, task.lineType)
        '    facetList.Add(nextFacet)
        'Next
        'facet32s.ConvertTo(dst1, cvb.MatType.CV_8U)





        subdiv.InitDelaunay(New cvb.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(ptList)

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
            fp.pt = New cvb.Point2f(xlist.Average, ylist.Average)

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





Public Class FPoint_Delaunay : Inherits TaskParent
    Public inputPoints As New List(Of cvb.Point2f)
    Public facetList As New List(Of List(Of cvb.Point))
    Public facet32s As cvb.Mat
    Dim randEnum As New Random_Enumerable
    Dim subdiv As New cvb.Subdiv2D
    Public Sub New()
        facet32s = New cvb.Mat(dst2.Size(), cvb.MatType.CV_32SC1, 0)
        dst1 = New cvb.Mat(dst1.Size, cvb.MatType.CV_8U, 0)
        labels(3) = "CV_8U map of Delaunay cells"
        desc = "Subdivide an image based on the points provided."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.heartBeat And standalone Then
            randEnum.Run(empty)
            inputPoints = New List(Of cvb.Point2f)(randEnum.points)
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