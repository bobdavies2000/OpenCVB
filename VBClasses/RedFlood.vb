Imports System.Runtime.InteropServices
Imports VBClasses
Imports cv = OpenCvSharp







Public Class XR_RedFlood_KNN : Inherits TaskParent
    Public rcList As New List(Of rcDataOld)
    Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Dim redCore As New RedFlood_CPP
    Dim fLess As New FeatureLess_DepthFull
    Dim knn As New KNN_Minimal
    Public fLessGridRects As New List(Of List(Of Integer))
    Public trainInput As New List(Of cv.Point3f)
    Public queries As New List(Of cv.Point3f)
    Public Sub New()
        queries.Add(New cv.Point3f)
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Use KNN to identify the previous cell for each current cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(src)
        dst2 = fLess.dst3
        labels(2) = fLess.labels(2)

        redCore.Run(dst2)
        Dim classcount = redCore.classCount

        Dim rcListLast As New List(Of rcDataOld)(rcList)

        fLessGridRects.Clear()
        For i = 0 To classcount
            fLessGridRects.Add(New List(Of Integer))
        Next
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            Dim index = redCore.dst2.Get(Of Byte)(r.Y, r.X)
            If index > 0 Then fLessGridRects(index).Add(i)
        Next

        trainInput.Clear()
        For Each rc In rcList
            trainInput.Add(New cv.Point3f(rc.maxDist.X, rc.maxDist.Y, rc.pixels))
        Next

        If redCore.rects Is Nothing Then Exit Sub ' nothing to work on...
        dst3.SetTo(0)
        rcMap.SetTo(0)
        rcList.Clear()
        For i = 0 To classcount - 1
            Dim r = redCore.rects(i)
            Dim mask255 = redCore.dst2(r).InRange(i + 1, i + 1)
            Dim mask As New cv.Mat(mask255.Size, cv.MatType.CV_8U, 0)
            redCore.dst2(r).CopyTo(mask, mask255)
            Dim rc As New rcDataOld(mask, r, i + 1)
            rc.color = task.scalarColors((rcList.Count + 1) Mod 255)

            queries(0) = New cv.Point3f(rc.maxDist.X, rc.maxDist.Y, rc.pixels)

            Dim dimension = 3
            knn.queryMat = cv.Mat.FromPixelData(queries.Count, dimension, cv.MatType.CV_32F, queries.ToArray)
            knn.trainMat = cv.Mat.FromPixelData(trainInput.Count, dimension, cv.MatType.CV_32F, trainInput.ToArray)
            If knn.trainMat.Rows = 0 Then knn.trainMat = knn.queryMat.Clone
            knn.Run(emptyMat)

            If trainInput.Count > 0 Then
                Dim index = knn.result(0, 0)
                If rcListLast.Count > 0 Then
                    Dim rcLast = rcListLast(index)
                    If rcLast.rect.IntersectsWith(task.gridRects(rc.gridIndex)) Then
                        Dim gridList = task.gridNabes(rcLast.gridIndex)
                        rc.color = rcLast.color
                        rc.age = rcLast.age + 1
                        If rc.age > 1000 Then rc.age = 2
                    End If
                End If
            End If

            rc.mapID = rcList.Count + 1
            rcMap(rc.rect).SetTo(rc.mapID, rc.mask)
            dst3(rc.rect).SetTo(rc.color, rc.mask)
            rcList.Add(rc)
        Next

        strOut = Utility_Basics.selectCell(rcMap, rcList)
        SetTrueText(strOut, 1)
        If task.rcD IsNot Nothing Then task.clickPoint = task.rcD.maxDist

        For Each rc In rcList
            dst3.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
            SetTrueText(CStr(rc.mapID) + ", " + CStr(rc.age), rc.maxDist, 3)
        Next

        labels(3) = "Palette version of the data in dst2 with " + CStr(classcount) + " regions."
    End Sub
End Class






Public Class RedFlood_CPP : Inherits TaskParent
    Implements IDisposable
    Public classCount As Integer
    Public rects() As cv.Rect
    Public Sub New()
        cPtr = RedFlood_Open()
        desc = "Run the C++ RedMask to create a list of mask, rect, and other info about image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then dst1 = Mat_Basics.srcMustBe8U(src) Else dst1 = src

        Dim inputData(dst1.Total - 1) As Byte
        dst1.GetArray(Of Byte)(inputData)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim minSize As Integer = dst2.Total * 0.001
        Dim imagePtr = RedFlood_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols, minSize)
        handleInput.Free()

        dst2 = cv.Mat.FromPixelData(dst0.Rows + 2, dst0.Cols + 2, cv.MatType.CV_8U, imagePtr).Clone
        dst2 = dst2(New cv.Rect(1, 1, dst2.Width - 2, dst2.Height - 2))

        classCount = RedFlood_Count(cPtr)
        If classCount <= 1 Then Exit Sub ' no data to process.

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedFlood_Rects(cPtr)).Clone
        ReDim rects(classCount - 1)
        rectData.GetArray(Of cv.Rect)(rects)

        If standaloneTest() Then dst3 = Palettize(dst2)

        labels(2) = "CV_8U result with " + CStr(classCount) + " regions."
        labels(3) = "Palette version of the data in dst2 with " + CStr(classCount) + " regions."
    End Sub
    Protected Overrides Sub Finalize()
        If cPtr <> 0 Then cPtr = RedFlood_Close(cPtr)
    End Sub
End Class




Public Class RedFlood_MapAndList : Inherits TaskParent
    Public rcList As New List(Of rcDataOld)
    Dim redCore As New RedFlood_CPP
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
        desc = "Run the C++ RedMask to create a list of mask, rect, and other info about image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then dst1 = Mat_Basics.srcMustBe8U(src) Else dst1 = src

        redCore.Run(dst1)
        Dim classcount = redCore.classCount
        If classcount <= 1 Then Exit Sub ' no data to process.

        dst2.SetTo(0)
        rcList.Clear()
        For i = 0 To classcount - 1
            Dim rc As New rcDataOld
            rc.rect = redCore.rects(i)
            rc.mask = redCore.dst2(rc.rect).InRange(i + 1, i + 1)
            rc = New rcDataOld(rc.mask, rc.rect, -1)
            rc.mapID = rcList.Count + 1

            rc.contour = ContourBuild(rc.mask)
            Dim listOfPoints = New List(Of List(Of cv.Point))({rc.contour})
            rc.mask = New cv.Mat(rc.mask.Size, cv.MatType.CV_8U, 0)
            cv.Cv2.DrawContours(rc.mask, listOfPoints, 0, cv.Scalar.All(rc.mapID), -1, cv.LineTypes.Link4)

            rc.color = task.scalarColors(rc.mapID Mod 255)
            dst2(rc.rect).SetTo(rc.mapID, rc.mask)
            rcList.Add(rc)
        Next

        If standaloneTest() Then dst3 = Palettize(dst2, 0)

        labels(2) = "CV_8U result with " + CStr(classcount) + " regions."
        labels(3) = "Palette version of the data in dst2 with " + CStr(classcount) + " regions."
    End Sub
End Class





Public Class RedFlood_Delaunay : Inherits TaskParent
    Dim subdiv As New cv.Subdiv2D
    Dim redC As New RedColor_Basics
    Dim facetList As New List(Of List(Of cv.Point))
    Dim rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(3) = "The colors below match the color of the corresponding featureless region in dst2."
        desc = "Fill the delaunay map with the index for each cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst3
        labels(2) = redC.labels(3)

        subdiv.InitDelaunay(New cv.Rect(0, 0, dst2.Width, dst2.Height))

        Dim inputPoints As New List(Of cv.Point2f)
        For Each rc In redC.rcList
            inputPoints.Add(rc.maxDist)
        Next
        subdiv.Insert(inputPoints)

        Dim facets = New cv.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        facetList.Clear()
        For i = 0 To facets.Length - 1
            Dim nextFacet As New List(Of cv.Point)
            For j = 0 To facets(i).Length - 1
                nextFacet.Add(New cv.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            Dim rc = redC.rcList(i)
            rcMap.FillConvexPoly(nextFacet, rc.mapID, cv.LineTypes.Link4)
            If standaloneTest() Then dst3.FillConvexPoly(nextFacet, rc.color, cv.LineTypes.Link4)
            facetList.Add(nextFacet)
        Next

        SetTrueText(redC.strOut, 1)
    End Sub
End Class