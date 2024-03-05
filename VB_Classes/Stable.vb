Imports cv = OpenCvSharp
Public Class Stable_Basics : Inherits VB_Algorithm
    Public facetGen As New Delaunay_Generations
    Public ptList As New List(Of cv.Point2f)
    Public anchorPoint As cv.Point2f
    Public Sub New()
        desc = "Maintain the generation counts around the feature points."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If standaloneTest() Then
            Static good As New Feature_BasicsKNN
            good.Run(src)
            facetGen.inputPoints = New List(Of cv.Point2f)(good.featurePoints)
        End If

        facetGen.Run(src)
        If facetGen.inputPoints.Count = 0 Then Exit Sub ' nothing to work on ...

        ptList.Clear()
        Dim generations As New List(Of Integer)
        For Each pt In facetGen.inputPoints
            Dim fIndex = facetGen.facet.facet32s.Get(Of Integer)(pt.Y, pt.X)
            If fIndex >= facetGen.facet.facetList.Count Then Continue For ' new point
            Dim g = facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
            generations.Add(g)
            ptList.Add(pt)
            setTrueText(CStr(g), pt)
        Next

        Dim maxGens = generations.Max()
        Dim index = generations.IndexOf(maxGens)
        anchorPoint = ptList(index)
        If index < facetGen.facet.facetList.Count Then
            Dim bestFacet = facetGen.facet.facetList(index)
            dst2.FillConvexPoly(bestFacet, cv.Scalar.Black, task.lineType)
            vbDrawContour(dst2, bestFacet, task.highlightColor)
        End If

        dst2 = facetGen.dst2
        dst3 = src.Clone
        For i = 0 To ptList.Count - 1
            Dim pt = ptList(i)
            dst2.Circle(pt, task.dotSize, task.highlightColor, task.lineWidth, task.lineType)
            dst3.Circle(pt, task.dotSize, task.highlightColor, task.lineWidth, task.lineType)
        Next
        labels(2) = CStr(ptList.Count) + " stable points were identified with " + CStr(maxGens) + " generations at the anchor point"
    End Sub
End Class











Public Class Stable_BasicsCount : Inherits VB_Algorithm
    Public basics As New Stable_Basics
    Public feat As New Feature_Basics
    Public goodCounts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        desc = "Track the stable good features found in the BGR image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        feat.Run(src)
        basics.facetGen.inputPoints = New List(Of cv.Point2f)(feat.featurePoints)
        basics.Run(src)
        dst2 = basics.dst2
        dst3 = basics.dst3

        goodCounts.Clear()
        Dim g As Integer
        For i = 0 To basics.ptList.Count - 1
            Dim pt = basics.ptList(i)
            dst2.Circle(pt, task.dotSize, task.highlightColor, task.lineWidth, task.lineType)
            g = basics.facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
            goodCounts.Add(g, i)
            setTrueText(CStr(g), pt)
        Next

        labels(2) = CStr(feat.featurePoints.Count) + " good features were found and " +
                    CStr(basics.ptList.Count) + " were stable"
    End Sub
End Class







Public Class Stable_Lines : Inherits VB_Algorithm
    Public basics As New Stable_Basics
    Dim lines As New Line_Basics
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        desc = "Track the line end points found in the BGR image and keep those that are stable."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        lines.Run(src)

        basics.facetGen.inputPoints.Clear()
        dst1 = src.Clone
        For Each lp In lines.lpList
            basics.facetGen.inputPoints.Add(lp.p1)
            basics.facetGen.inputPoints.Add(lp.p2)
            dst1.Line(lp.p1, lp.p2, task.highlightColor, task.lineWidth, task.lineType)
        Next
        basics.Run(src)
        dst2 = basics.dst2
        dst3 = basics.dst3
        For Each pt In basics.ptList
            dst2.Circle(pt, task.dotSize + 1, task.highlightColor, -1, task.lineType)
            If standaloneTest() Then
                Dim g = basics.facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
                setTrueText(CStr(g), pt)
            End If
        Next
        labels(2) = basics.labels(2)
        labels(3) = CStr(lines.lpList.Count) + " line end points were found and " + CStr(basics.ptList.Count) + " were stable"
    End Sub
End Class








Public Class Stable_FAST : Inherits VB_Algorithm
    Public basics As New Stable_Basics
    ReadOnly fast As New Corners_FAST
    Public Sub New()
        findSlider("FAST Threshold").Value = 100
        desc = "Track the FAST feature points found in the BGR image and track those that appear stable."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        fast.Run(src)

        basics.facetGen.inputPoints.Clear()
        basics.facetGen.inputPoints = New List(Of cv.Point2f)(fast.features)
        basics.Run(src)
        dst3 = basics.dst3
        dst2 = basics.dst2
        For Each pt In basics.ptList
            dst2.Circle(pt, task.dotSize + 1, task.highlightColor, -1, task.lineType)
            If standaloneTest() Then
                Dim g = basics.facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
                setTrueText(CStr(g), pt)
            End If
        Next
        labels(2) = basics.labels(2)
        labels(3) = CStr(fast.features.Count) + " features were found and " + CStr(basics.ptList.Count) + " were stable"
    End Sub
End Class











Public Class Stable_GoodFeatures : Inherits VB_Algorithm
    Public basics As New Stable_Basics
    Public feat As New Feature_Basics
    Public genSorted As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Track the stable good features found in the BGR image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        dst3 = basics.dst3
        If feat.featurePoints.Count = 0 Then Exit Sub ' nothing to work on...

        basics.facetGen.inputPoints = New List(Of cv.Point2f)(feat.featurePoints)
        basics.Run(src)
        dst2 = basics.dst2

        dst1.SetTo(0)
        genSorted.Clear()
        For i = 0 To basics.ptList.Count - 1
            Dim pt = basics.ptList(i)
            If standaloneTest() Then dst2.Circle(pt, task.dotSize + 1, cv.Scalar.Yellow, -1, task.lineType)
            dst1.Set(Of Byte)(pt.Y, pt.X, 255)

            Dim g = basics.facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
            genSorted.Add(g, i)
            setTrueText(CStr(g), pt)
            dst2.Circle(pt, task.dotSize, task.highlightColor, task.lineWidth, task.lineType)
        Next
        labels(2) = basics.labels(2)
        labels(3) = CStr(feat.featurePoints.Count) + " good features were found and " + CStr(basics.ptList.Count) + " were stable"
    End Sub
End Class
