Imports System.Runtime.InteropServices
Imports Microsoft.VisualBasic.Logging
Imports cv = OpenCvSharp
' https://docs.opencvb.org/3.0-beta/modules/ml/doc/expectation_maximization.html
' https://github.com/opencv/opencv/blob/master/samples/cpp/em.cpp
Namespace VBClasses
    Public Class EMax_Basics : Inherits TaskParent
        Public emaxInput As New EMax_InputClusters
        ' algorithms using this must provide the following items.
        Public eLabels As New List(Of Integer)
        Public eSamples As New List(Of cv.Point2f)
        Public dimension = 2 ' point2f for the basics...
        Public regionCount As Integer
        Public centers As New List(Of cv.Point2f)
        Dim options As New Options_Emax
        Dim useInputClusters As Boolean
        Dim palette As New Palette_Variable
        Public Sub New()
            cPtr = EMax_Open()
            OptionParent.FindSlider("EMax Number of Samples per region").Value = 1
            labels(3) = "Emax regions as integers"
            desc = "Use EMax - Expectation Maximization - to classify the regions around a series of labeled points"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If eLabels.Count = 0 Or useInputClusters Then
                useInputClusters = True
                emaxInput.Run(src.Clone)
                eLabels = New List(Of Integer)(emaxInput.eLabels.ToList)
                eSamples = New List(Of cv.Point2f)(emaxInput.eSamples)
                regionCount = emaxInput.regionCount
            End If
            If centers.Count = 0 Then centers = New List(Of cv.Point2f)(emaxInput.centers)

            labels(2) = CStr(eLabels.Count) + " samples provided in " + CStr(regionCount) + " regions"
            Dim handleSrc = GCHandle.Alloc(eSamples.ToArray, GCHandleType.Pinned)
            Dim handleLabels = GCHandle.Alloc(eLabels.ToArray, GCHandleType.Pinned)

            Dim imagePtr = EMax_Run(cPtr, handleSrc.AddrOfPinnedObject(), handleLabels.AddrOfPinnedObject(), eLabels.Count, dimension,
                                    dst2.Rows, dst2.Cols, regionCount, options.predictionStepSize, options.covarianceType)
            handleLabels.Free()
            handleSrc.Free()

            dst1 = cv.Mat.FromPixelData(dst3.Rows, dst3.Cols, cv.MatType.CV_32S, imagePtr).Clone
            dst1.ConvertTo(dst0, cv.MatType.CV_8U)

            palette.colors.Clear()
            Dim newLabels(regionCount) As cv.Vec3b
            For i = 0 To eLabels.Count - 1
                Dim pt = eSamples(i)
                ' emaxInput samples are random so they may be off the image...
                If pt.X < 0 Or pt.X >= dst2.Width Or pt.Y < 0 Or pt.Y >= dst2.Height Then Continue For
                Dim newLabel = dst0.Get(Of Byte)(pt.Y, pt.X)
                Dim original = eLabels(i)
                Dim c = palette.originalColorMap.Get(Of cv.Vec3b)(0, original Mod 256)
                If newLabels.Contains(c) = False And newLabel <= regionCount Then newLabels(newLabel) = c
            Next
            palette.colors = New List(Of cv.Vec3b)(newLabels)
            palette.Run(dst0)
            dst2 = palette.dst2
            centers = New List(Of cv.Point2f)(emaxInput.centers)
        End Sub
        Public Sub Close()
            If cPtr <> 0 Then cPtr = EMax_Close(cPtr)
        End Sub
    End Class








    Public Class EMax_Centers : Inherits TaskParent
        Dim emax As New EMax_Basics
        Public Sub New()
            labels(2) = "Centers are highlighted, Previous centers are black"
            desc = "Display the Emax centers as they move"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            emax.Run(src)
            dst2 = emax.dst2
            Static lastCenters As New List(Of cv.Point2f)(emax.centers)
            For i = 0 To emax.centers.Count - 1
                DrawCircle(dst2, emax.centers(i), task.DotSize + 1, task.highlight)
                If i < lastCenters.Count Then
                    DrawCircle(dst2, lastCenters(i), task.DotSize + 2, cv.Scalar.Black)
                End If
            Next
            lastCenters = New List(Of cv.Point2f)(emax.centers)
        End Sub
    End Class





    Public Class EMax_InputClusters : Inherits TaskParent
        Public regionCount As Integer
        Public eLabels() As Integer
        Public eSamples As New List(Of cv.Point2f)
        Public centers As New List(Of cv.Point2f)
        Dim options As New Options_EmaxInputClusters
        Dim grid As New Grid_Rectangles
        Public Sub New()
            labels(2) = "EMax algorithms input samples"
            OptionParent.FindSlider("EMax Cell Size").Value = CInt(dst2.Width / 3)
            grid.gridWidth = CInt(dst2.Width / 3)
            grid.gridHeight = CInt(dst2.Width / 3)
            desc = "Options for EMax algorithms."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If task.optionsChanged Then grid.Run(dst2)
            regionCount = grid.gridRects.Count

            Dim samples = New cv.Mat(regionCount * options.samplesPerRegion, 2, cv.MatType.CV_32F).Reshape(2, 0)
            Dim eLabelMat = New cv.Mat(regionCount * options.samplesPerRegion, 1, cv.MatType.CV_32S)

            For i = 0 To regionCount - 1
                eLabelMat.RowRange(i * options.samplesPerRegion, (i + 1) * options.samplesPerRegion).SetTo(i)
                Dim tmp = samples.RowRange(i * options.samplesPerRegion, (i + 1) * options.samplesPerRegion)
                cv.Cv2.Randn(tmp, New cv.Scalar(grid.gridWidth / 2, grid.gridHeight / 2),
                         cv.Scalar.All(options.sigma))
            Next

            samples = samples.Reshape(1, 0)

            dst2.SetTo(0)
            eSamples.Clear()
            centers.Clear()
            Dim gIndex As Integer = -1
            For i = 0 To regionCount * options.samplesPerRegion - 1
                If i Mod options.samplesPerRegion = 0 Then gIndex += 1
                Dim roi = grid.gridRects(gIndex)
                Dim pt = samples.Get(Of cv.Point2f)(i, 0)
                centers.Add(pt)
                Dim ePt = New cv.Point2f(CInt(roi.X + pt.X), CInt(roi.Y + pt.Y))
                eSamples.Add(ePt) ' easier to debug with just integers...
                Dim label = eLabelMat.Get(Of Integer)(i)
                DrawCircle(dst2, ePt, task.DotSize + 2, task.highlight)
            Next

            ReDim eLabels(eLabelMat.Rows - 1)
            Marshal.Copy(eLabelMat.Data, eLabels, 0, eLabels.Length)
        End Sub
    End Class






    ' https://docs.opencvb.org/3.0-beta/modules/ml/doc/expectation_maximization.html
    ' https://github.com/opencv/opencv/blob/master/samples/cpp/em.cpp
    Public Class EMax_VB_Failing : Inherits TaskParent
        Public emaxInput As New EMax_InputClusters
        Public eLabels As New List(Of Integer)
        Public eSamples As New List(Of cv.Point2f)
        Public dimension = 2 ' point2f for the basics...
        Public regionCount As Integer
        Dim em_model As cv.EM
        Public Sub New()
            desc = "OpenCV expectation maximization example."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            emaxInput.Run(src)
            eLabels = New List(Of Integer)(emaxInput.eLabels.ToList)
            eSamples = New List(Of cv.Point2f)(emaxInput.eSamples)
            regionCount = emaxInput.regionCount
            SetTrueText("The EMax algorithm fails as a result of a bug in em_model.Predict2.  See code for details." + vbCrLf +
                      "The C++ version works fine (EMax_RedCloud) and the 2 are functionally identical.", New cv.Point(20, 100))

            Exit Sub ' comment this line to see the bug in the VB.Net version of this Predict2 below.  Any answers would be gratefully received.

            If em_model Is Nothing Then em_model = cv.EM.Create()
            em_model.ClustersNumber = regionCount
            em_model.CovarianceMatrixType = cv.EMTypes.CovMatSpherical
            em_model.TermCriteria = New cv.TermCriteria(cv.CriteriaTypes.Eps + cv.CriteriaTypes.Count, 300, 1.0)
            Dim samples = cv.Mat.FromPixelData(eSamples.Count, 2, cv.MatType.CV_32FC1, eSamples.ToArray)
            Dim eLabelsMat = cv.Mat.FromPixelData(eLabels.Count, 1, cv.MatType.CV_32S, eLabels.ToArray)
            em_model.TrainEM(samples, Nothing, eLabelsMat, Nothing)

            ' now classify every image pixel based on the samples.
            Dim sample As cv.Mat = cv.Mat.FromPixelData(1, 2, cv.MatType.CV_32FC1, 0)  ' tried doubles but it fails as well...
            For i = 0 To dst2.Rows - 1
                For j = 0 To dst2.Cols - 1
                    sample.Set(Of Single)(0, 0, CSng(j))
                    sample.Set(Of Single)(0, 1, CSng(i))

                    Dim response = Math.Round(em_model.Predict2(sample)(1))

                    Dim c = task.vecColors(response)
                    DrawCircle(dst2, New cv.Point(j, i), task.DotSize, c)
                Next
            Next
        End Sub
        Public Sub Close()
            If em_model IsNot Nothing Then em_model.Dispose()
        End Sub
    End Class








    Public Class EMax_PointTracker : Inherits TaskParent
        Dim knn As New KNN_Basics
        Dim emax As New EMax_Basics
        Public Sub New()
            labels = {"", "", "Output of EMax_RedCloud", "Emax centers tracked and smoothed."}
            desc = "Use KNN to track the EMax Centers"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            emax.Run(src)
            dst2 = emax.dst2

            knn.queries = New List(Of cv.Point2f)(emax.centers)
            knn.Run(src)
            If task.firstPass Then
                knn.trainInput = New List(Of cv.Point2f)(knn.queries)
                Exit Sub
            End If

            dst3.SetTo(0)
            For i = 0 To knn.queries.Count - 1
                Dim p1 = knn.queries(i)
                Dim p2 = knn.trainInput(knn.result(i, 0))
                DrawCircle(dst3, p1, task.DotSize, task.highlight)
                DrawCircle(dst3, p2, task.DotSize, cv.Scalar.Red)
                vbc.DrawLine(dst3, p1, p2, white)
            Next
            knn.trainInput = New List(Of cv.Point2f)(knn.queries)
            dst2 = dst2 Or emax.emaxInput.dst2
        End Sub
    End Class








    Public Class EMax_RandomClusters : Inherits TaskParent
        Dim clusters As New Random_Clusters
        Dim emax As New EMax_Basics
        Public Sub New()
            OptionParent.FindSlider("Number of points per cluster").Value = 1
            labels = {"", "", "Random_Clusters output", "EMax layout for the random clusters supplied"}
            desc = "Build an EMax layout for random set of clusters (not a grid)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static regionSlider = OptionParent.FindSlider("Number of Clusters")
            emax.regionCount = regionSlider.Value

            clusters.Run(src)
            dst3 = clusters.dst2
            emax.eLabels.Clear()
            emax.eSamples.Clear()
            For i = 0 To emax.regionCount - 1
                Dim cList = clusters.clusters(i)
                Dim cLabels = clusters.clusterLabels(i)
                For j = 0 To cList.Count - 1
                    emax.eSamples.Add(cList(j))
                    emax.eLabels.Add(cLabels(j))
                Next
            Next

            emax.Run(src)
            dst2 = emax.dst2
        End Sub
    End Class
End Namespace