Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class EMax_Basics : Inherits VBparent
    Dim inputDataMask As cv.Mat
    Public basics As New EMax_Raw
    Public Sub New()
        labels(2) = "Emax regions around clusters"
        task.desc = "Use EMax - Expectation Maximization - to classify a series of points"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        basics.RunClass(Nothing)
        labels(2) = basics.labels(2)

        Dim regions = basics.options.regionCount
        Dim regionCount(regions - 1, regions - 1)
        For i = 0 To basics.options.samples.Rows - 1
            Dim p = basics.options.samples.Get(Of cv.Point2f)(i, 0)
            Dim pt = New cv.Point(CInt(p.X), CInt(p.Y))
            If pt.X >= 0 And pt.Y >= 0 And pt.X < dst3.Width And pt.Y < dst3.Height Then
                Dim label = basics.options.elabels.Get(Of Integer)(i, 0)
                Dim eGrp = basics.dst3.Get(Of Byte)(CInt(pt.Y), CInt(pt.X))
                If eGrp < regions Then regionCount(label, eGrp) += 1
            End If
        Next

        Dim maxCount As Integer
        For i = 0 To regions - 1
            maxCount = 0
            For j = 0 To regions - 1
                Dim index = regionCount(i, j)
                If index Is Nothing Then Continue For
                If maxCount < index Then
                    maxCount = index
                    task.palette.gradientColorMap.Set(Of cv.Vec3b)(0, j, basics.options.regionColors(i))
                End If
            Next
        Next
        task.palette.RunClass(basics.dst3)
        dst2 = task.palette.dst2
    End Sub
End Class






' https://docs.opencv.org/3.0-beta/modules/ml/doc/expectation_maximization.html
' https://github.com/opencv/opencv/blob/master/samples/cpp/em.cpp
Public Class EMax_Raw : Inherits VBparent
    Dim inputDataMask As cv.Mat
    Dim EMax_Raw As IntPtr
    Public options As New EMax_Setup
    Public Sub New()
        EMax_Raw = EMax_Raw_Open()

        labels(3) = "Emax regions as integers"
        task.desc = "Use EMax - Expectation Maximization - to classify a series of points"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        options.RunClass(Nothing)
        Dim inCount = options.samples.Rows
        labels(2) = CStr(inCount) + " Random samples in " + CStr(options.regionCount) + " clusters"
        If options.regionCount <= 0 Then Exit Sub

        Dim srcData((inCount - 1) * 2) As Single
        Dim handleSrc As GCHandle
        handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Marshal.Copy(options.samples.Data, srcData, 0, srcData.Length)

        Dim labelData(inCount - 1) As Integer
        Dim handleLabels As GCHandle
        handleLabels = GCHandle.Alloc(labelData, GCHandleType.Pinned)
        Marshal.Copy(options.elabels.Data, labelData, 0, labelData.Length)

        Dim imagePtr = EMax_Raw_Run(EMax_Raw, handleSrc.AddrOfPinnedObject(), handleLabels.AddrOfPinnedObject(), inCount, 2,
                                       dst2.Rows, dst2.Cols, options.regionCount, options.predictionStepSize, options.covarianceMatrixType)
        handleLabels.Free() ' free the pinned memory...
        handleSrc.Free() ' free the pinned memory...

        dst3 = New cv.Mat(dst3.Rows, dst3.Cols, cv.MatType.CV_8U, imagePtr)

        task.palette.RunClass(dst3 * 255 / options.regionCount)
        dst2 = task.palette.dst2
        If standalone Or task.intermediateName = caller Then
            inputDataMask = options.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)
            dst2.SetTo(cv.Scalar.White, inputDataMask)
        End If
    End Sub
    Public Sub Close()
        EMax_Raw_Close(EMax_Raw)
    End Sub
End Class







Public Class EMax_Setup : Inherits VBparent
    Public grid As New Thread_Grid
    Public regionCount As Integer
    Public regionColors() As cv.Vec3b
    Public covarianceMatrixType = 0
    Public samples As cv.Mat
    Public elabels As cv.Mat
    Public predictionStepSize As Integer
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "EMax Number of Samples", 1, 200, 100)
            sliders.setupTrackBar(1, "EMax Prediction Step Size", 1, 20, 5)
            sliders.setupTrackBar(2, "EMax Sigma (spread)", 1, 100, 30)
        End If

        findSlider("ThreadGrid Width").Value = dst2.Width / 3
        findSlider("ThreadGrid Height").Value = dst2.Height / 3

        If radio.Setup(caller, 3) Then
            radio.check(0).Text = "EMax matrix type Spherical"
            radio.check(1).Text = "EMax matrix type Diagonal"
            radio.check(2).Text = "EMax matrix type Generic"
            radio.check(0).Checked = True
        End If

        labels(2) = "EMax algorithms input samples"
        task.desc = "Options for EMax algorithms."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static sampleSlider = findSlider("EMax Number of Samples")
        Static stepSlider = findSlider("EMax Prediction Step Size")
        Static sigmaSlider = findSlider("EMax Sigma (spread)")

        task.palette.RunClass(Nothing)
        grid.RunClass(Nothing)
        If regionCount <> grid.roiList.Count - 1 Then
            regionCount = grid.roiList.Count - 1
            ReDim regionColors(regionCount - 1)
        End If
        Dim colorMap As cv.Mat = task.palette.gradientColorMap.Row(0)
        Dim spread = 255 / regionCount
        For i = 0 To regionCount - 1
            regionColors(i) = colorMap.Get(Of cv.Vec3b)(0, i * spread)
        Next

        Static emaxFrm = findfrm(caller + " Radio Options")
        For i = 0 To emaxFrm.check.Length - 1
            If emaxFrm.check(i).Checked = True Then
                covarianceMatrixType = Choose(i + 1, cv.EM.Types.CovMatSpherical, cv.EM.Types.CovMatDiagonal, cv.EM.Types.CovMatGeneric)
            End If
        Next

        samples = New cv.Mat(sampleSlider.Value, 2, cv.MatType.CV_32FC1, 0).Reshape(2, 0)
        If regionCount > sampleSlider.Value / 2 Then regionCount = sampleSlider.Value / 2
        elabels = New cv.Mat(sampleSlider.Value, 1, cv.MatType.CV_32S, 0)

        Dim sigma = sigmaSlider.Value
        predictionStepSize = stepSlider.value
        For i = 0 To regionCount - 1
            Dim samples_part = samples.RowRange(i * samples.Rows / regionCount, (i + 1) * samples.Rows / regionCount)
            elabels.RowRange(i * samples.Rows / regionCount, (i + 1) * samples.Rows / regionCount).SetTo(i)
            Dim r = grid.roiList(i)
            cv.Cv2.Randn(samples_part, New cv.Scalar(r.X + r.Width / 2, r.Y + r.Height / 2), cv.Scalar.All(sigma))
        Next

        samples = samples.Reshape(1, 0)

        If standalone Or task.intermediateName = caller Then
            dst2.SetTo(cv.Scalar.Black)
            ' draw the clustered samples
            For i = 0 To samples.Rows - 1
                Dim pt = samples.Get(Of cv.Point2f)(i, 0)
                Dim label = elabels.Get(Of Integer)(i, 0)
                dst2.Circle(pt, task.dotSize + 2, regionColors(label), -1, task.lineType)
            Next
        End If
    End Sub
End Class








' https://docs.opencv.org/3.0-beta/modules/ml/doc/expectation_maximization.html
' https://github.com/opencv/opencv/blob/master/samples/cpp/em.cpp
Public Class EMax_VB_Failing : Inherits VBparent
    Dim options As New EMax_Setup
    Public Sub New()
        task.desc = "OpenCV expectation maximization example."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone Or task.intermediateName = caller Then
            setTrueText("The EMax algorithm fails as a result of a bug in em_model.Predict2.  See code for details." + vbCrLf +
                          "The C++ version works fine (EMax_Raw) and the 2 are functionally identical.", 20, 100)

            Exit Sub ' comment this line to see the bug in the VB.Net version of this Predict2 below.

            options.RunClass(Nothing)
            Dim em_model = cv.EM.Create()
            em_model.ClustersNumber = options.regionCount
            em_model.CovarianceMatrixType = options.covarianceMatrixType
            em_model.TermCriteria = New cv.TermCriteria(cv.CriteriaTypes.Eps + cv.CriteriaTypes.Count, 300, 1.0)
            em_model.TrainEM(options.samples, Nothing, options.elabels, Nothing)

            ' now classify every image pixel based on the samples.
            Dim sample As New cv.Mat(1, 2, cv.MatType.CV_32FC1, 0)  ' tried doubles but it fails as well...
            For i = 0 To dst2.Rows - 1
                For j = 0 To dst2.Cols - 1
                    sample.Set(Of Single)(0, 0, CSng(j))
                    sample.Set(Of Single)(0, 1, CSng(i))

                    Dim response = Math.Round(em_model.Predict2(sample).Item1)

                    Dim c = task.vecColors(response)
                    dst2.Circle(New cv.Point(j, i), task.dotSize, c, -1)
                Next
            Next
        End If
    End Sub
End Class





Module EMax_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function EMax_Raw_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub EMax_Raw_Close(EMax_RawPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function EMax_Raw_Run(EMax_RawPtr As IntPtr, samplesPtr As IntPtr, labelsPtr As IntPtr, rows As Integer, cols As Integer, imgRows As Integer,
                                    imgCols As Integer, clusters As Integer, stepSize As Integer, covarianceMatrixType As Integer) As IntPtr
    End Function
End Module








Public Class EMax_Centroids : Inherits VBparent
    Public emaxCPP As New EMax_Basics
    Public flood As New FloodFill_Basics
    Public Sub New()
        findSlider("FloodFill LoDiff").Value = 0
        findSlider("FloodFill HiDiff").Value = 1
        findSlider("ThreadGrid Width").Value = dst2.Width * 170 / 640
        task.desc = "Get the Emax cluster centroids using floodfill "
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        emaxCPP.RunClass(src)
        flood.RunClass(emaxCPP.dst2.Clone)
        dst2 = flood.dst2

        Static lastCentroids As New List(Of cv.Point2f)
        For i = 0 To flood.centroids.Count - 1
            dst2.Circle(flood.centroids(i), task.dotSize + 2, cv.Scalar.White, -1, task.lineType)
            If i < lastCentroids.Count Then
                dst2.Circle(lastCentroids(i), task.dotSize + 2, cv.Scalar.Red, -1, task.lineType)
            End If
        Next
        lastCentroids = New List(Of cv.Point2f)(flood.centroids)
    End Sub
End Class







Public Class EMax_PointTracker : Inherits VBparent
    Dim pTrack As New KNN_PointTracker
    Dim emax As New EMax_Centroids
    Public Sub New()
        findCheckBox("Draw rectangle and centroid for each mask").Checked = False
        findSlider("FloodFill Minimum Size").Value = 100

        labels(2) = "Original before KNN/Kalman tracking (red=previous)"
        task.desc = "Use KNN and Kalman to track the EMax Centroids and map consisten colors"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        emax.RunClass(src)
        dst2 = emax.dst2

        pTrack.queryPoints = emax.flood.centroids
        pTrack.queryMasks = emax.flood.masks
        pTrack.queryRects = emax.flood.rects
        pTrack.RunClass(src)
        dst3 = pTrack.dst2

        ' this is to verify that the colors are remaining largely consistent (they may change if more centroids appear.)
        Static lastImage = dst3
        Dim tallyErrors = 0
        For Each pt In emax.flood.centroids
            Dim v1 = dst3.Get(Of cv.Vec3b)(pt.Y, pt.X)
            Dim v2 = lastImage.Get(Of cv.Vec3b)(pt.Y, pt.X)
            If v1 <> v2 Then tallyErrors += 1
        Next
        lastImage = dst3.Clone
        Static totalErrors = 0
        Static generationCount = 0
        Static saveCount = 0
        If emax.emaxCPP.basics.options.grid.roiList.Count <> saveCount Then
            saveCount = emax.emaxCPP.basics.options.grid.roiList.Count
            totalErrors = 0
            generationCount = 0
        End If
        totalErrors += tallyErrors
        generationCount += 1
        labels(3) = "After: there were " + Format(totalErrors / generationCount, "0.0") + " average errors matching centroids"
    End Sub
End Class

