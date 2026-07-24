'Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
'Namespace VBClasses
'    Public Class Neighbor_Basics : Inherits TaskParent
'        Dim knn As New KNN_Basics
'        Public runRedCflag As Boolean = False
'        Public options As New Options_Neighbors
'        Dim redC As New RedC_Basics
'        Public nabs As New List(Of Integer)
'        Public Sub New()
'            desc = "Find all the neighbors with KNN"
'        End Sub
'        Public Overrides Sub RunAlg(src As cv.Mat)
'            options.Run()

'            If standalone Or runRedCflag Then
'                redC.Run(src)
'                dst2 = redC.dst2
'                labels(2) = redC.labels(2)
'            End If

'            knn.queries.Clear()
'            For Each rc In redC.rcList
'                knn.queries.Add(rc.maxDist)
'            Next
'            knn.trainInput = New List(Of Point2f)(knn.queries)
'            knn.Run(src)

'            nabs.Clear()
'            For Each rc In redC.rcList
'                For i = 0 To Math.Min(knn.queries.Count, options.neighbors) - 1
'                    nabs.Add(knn.result(rc.mapID - 1, i))
'                Next
'            Next

'            If standalone Then
'                SetTrueText(redC.strOut, 3)
'                dst3.SetTo(0)
'                If task.rcD IsNot Nothing Then
'                    For Each index In task.rcD.nabs
'                        If index < redC.rcList.Count Then
'                            Circle(dst2, redC.rcList(index).maxDist, task.DotSize, task.highlight, -1, task.lineType)
'                        End If
'                    Next
'                End If
'            End If
'        End Sub
'    End Class







'    Public Class Neighbor_Intersects8u : Inherits TaskParent
'        Public nPoints As New List(Of cv.Point)
'        Public Sub New()
'            desc = "Find the corner points where multiple cells intersect."
'        End Sub
'        Public Overrides Sub RunAlg(src As cv.Mat)
'            If standaloneTest() Or src.Type <> MatType.CV_32S Then
'                Static redC As New RedColor_Basics
'                redC.Run(src)
'                dst2 = redC.dst2
'                labels(2) = redC.labels(2)
'                src = redC.rcMap
'            End If

'            Dim samples(src.Total - 1) As Byte
'            src.GetArray(Of Byte)(samples)

'            Dim w = dst2.Width
'            nPoints.Clear()
'            Dim kSize As Integer = 3
'            For y = 0 To dst1.Height - kSize
'                For x = 0 To dst1.Width - kSize
'                    Dim nabs As New SortedList(Of Integer, Integer)
'                    For yy = y To y + kSize - 1
'                        For xx = x To x + kSize - 1
'                            Dim val = samples(yy * w + xx)
'                            If val = 0 And removeZeroNeighbors Then Continue For
'                            If nabs.ContainsKey(val) = False Then nabs.Add(val, 0)
'                        Next
'                    Next
'                    If nabs.Count > 2 Then
'                        nPoints.Add(New cv.Point(x, y))
'                    End If
'                Next
'            Next

'            If standaloneTest() Then
'                dst3 = task.color.Clone
'                For Each pt In nPoints
'                    Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
'                    Circle(dst3, pt, task.DotSize, Scalar.Yellow, -1, task.lineType)
'                Next
'            End If

'            labels(3) = CStr(nPoints.Count) + " intersections with 3 or more cells were found"
'        End Sub
'    End Class







'    Public Class Neighbor_Intersects32S : Inherits TaskParent
'        Public nPoints As New List(Of cv.Point)
'        Public Sub New()
'            desc = "Find the corner points where multiple cells intersect."
'        End Sub
'        Public Overrides Sub RunAlg(src As cv.Mat)
'            If standaloneTest() Or src.Type <> MatType.CV_32S Then
'                Static redC As New RedCloud_Basics
'                redC.Run(src)
'                dst2 = redC.dst2
'                labels(2) = redC.labels(2)
'                src = redC.rcMap
'            End If

'            Dim samples(src.Total - 1) As Integer
'            src.GetArray(Of Integer)(samples)

'            Dim w = dst2.Width
'            nPoints.Clear()
'            Dim kSize As Integer = 3
'            For y = 0 To dst1.Height - kSize
'                For x = 0 To dst1.Width - kSize
'                    Dim nabs As New SortedList(Of Integer, Integer)
'                    For yy = y To y + kSize - 1
'                        For xx = x To x + kSize - 1
'                            Dim val = samples(yy * w + xx)
'                            If val = 0 And removeZeroNeighbors Then Continue For
'                            If nabs.ContainsKey(val) = False Then nabs.Add(val, 0)
'                        Next
'                    Next
'                    If nabs.Count > 2 Then
'                        nPoints.Add(New cv.Point(x, y))
'                    End If
'                Next
'            Next

'            If standaloneTest() Then
'                dst3 = task.color.Clone
'                For Each pt In nPoints
'                    Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
'                    Circle(dst3, pt, task.DotSize, Scalar.Yellow, -1, task.lineType)
'                Next
'            End If

'            labels(3) = CStr(nPoints.Count) + " intersections with 3 or more cells were found"
'        End Sub
'    End Class









'    Public Class Neighbor_ColorOnly : Inherits TaskParent
'        Dim corners As New Neighbor_Intersects8u
'        Dim redC As New RedColor_Basics
'        Public Sub New()
'            desc = "Find neighbors in a redColor cellMap"
'        End Sub
'        Public Overrides Sub RunAlg(src As cv.Mat)
'            redC.Run(src)
'            dst2 = redC.dst2

'            corners.Run(redC.rcMap.Clone())
'            For Each pt In corners.nPoints
'                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
'            Next

'            labels(2) = redC.labels(2) + " and " + CStr(corners.nPoints.Count) + " cell intersections"
'        End Sub
'    End Class
'End Namespace