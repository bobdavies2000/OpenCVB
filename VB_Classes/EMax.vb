Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://docs.opencv.org/3.0-beta/modules/ml/doc/expectation_maximization.html
' https://github.com/opencv/opencv/blob/master/samples/cpp/em.cpp
Public Class EMax_Basics
    Inherits VBparent
    Public samples As cv.Mat
    Public labels As cv.Mat
    Public grid As Thread_Grid
    Public regionCount As Integer
    Public gridWidthSlider As System.Windows.Forms.TrackBar
    Public gridHeightSlider As System.Windows.Forms.TrackBar
    Public Sub New()
        initParent()
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Show EMax input in output"
        End If

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "EMax Number of Samples", 1, 200, 100)
            sliders.setupTrackBar(1, "EMax Prediction Step Size", 1, 20, 5)
            sliders.setupTrackBar(2, "EMax Sigma (spread)", 1, 100, 30)
        End If

        grid = New Thread_Grid
        gridWidthSlider = findSlider("ThreadGrid Width")
        gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = src.Width / 2
        gridHeightSlider.Value = src.Height / 2

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "EMax matrix type Spherical"
            radio.check(1).Text = "EMax matrix type Diagonal"
            radio.check(2).Text = "EMax matrix type Generic"
            radio.check(0).Checked = True
        End If

        task.desc = "OpenCV expectation maximization example."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If standalone or task.intermediateReview = caller Then
            ocvb.trueText("The EMax VBocvb class fails as a result of a bug in OpenCVSharp.  See code for details." + vbCrLf +
                          "The C++ version works fine (EMax_CPP) and the 2 are functionally identical.", 20, 100)
            Exit Sub ' comment this line to see the bug in the VB.Net version of this Predict2 below.
        End If

        grid.Run()
        regionCount = grid.roiList.Count - 1

        samples = New cv.Mat(sliders.trackbar(0).Value, 2, cv.MatType.CV_32FC1, 0)
        If regionCount > samples.Rows / 2 Then regionCount = samples.Rows / 2
        labels = New cv.Mat(sliders.trackbar(0).Value, 1, cv.MatType.CV_32S, 0)
        samples = samples.Reshape(2, 0)
        Dim sigma = sliders.trackbar(2).Value
        For i = 0 To regionCount - 1
            Dim samples_part = samples.RowRange(i * samples.Rows / regionCount, (i + 1) * samples.Rows / regionCount)
            labels.RowRange(i * samples.Rows / regionCount, (i + 1) * samples.Rows / regionCount).SetTo(i)
            Dim x = grid.roiList(i).X + grid.roiList(i).Width / 2
            Dim y = grid.roiList(i).Y + grid.roiList(i).Height / 2
            cv.Cv2.Randn(samples_part, New cv.Scalar(x, y), cv.Scalar.All(sigma))
        Next

        samples = samples.Reshape(1, 0)

        dst1.SetTo(cv.Scalar.Black)
        If standalone or task.intermediateReview = caller Then
            Dim em_model = cv.EM.Create()
            em_model.ClustersNumber = regionCount
            Static frm = findfrm("EMax_Basics Radio Options")
            For i = 0 To frm.check.length - 1
                If frm.check(i).Checked Then
                    em_model.CovarianceMatrixType = Choose(i + 1, cv.EM.Types.CovMatSpherical, cv.EM.Types.CovMatDiagonal, cv.EM.Types.CovMatGeneric)
                End If
            Next
            em_model.TermCriteria = New cv.TermCriteria(cv.CriteriaTypes.Eps + cv.CriteriaTypes.Count, 300, 1.0)
            em_model.TrainEM(samples, Nothing, labels, Nothing)

            ' now classify every image pixel based on the samples.
            Dim sample As New cv.Mat(1, 2, cv.MatType.CV_32FC1, 0)  ' tried doubles but it fails as well...
            For i = 0 To dst1.Rows - 1
                For j = 0 To dst1.Cols - 1
                    sample.Set(Of Single)(0, 0, CSng(j))
                    sample.Set(Of Single)(0, 1, CSng(i))

                    Dim response = Math.Round(em_model.Predict2(sample).Item1)

                    Dim c = task.vecColors(response)
                    dst1.Circle(New cv.Point(j, i), 1, c, -1)
                Next
            Next
        End If

        ' draw the clustered samples
        For i = 0 To samples.Rows - 1
            Dim pt = New cv.Point(Math.Round(samples.Get(Of Single)(i, 0)), Math.Round(samples.Get(Of Single)(i, 1)))
            dst1.Circle(pt, 4, task.vecColors(labels.Get(Of Integer)(i) + 1), -1, cv.LineTypes.AntiAlias) ' skip the first rColor - it might be used above.
        Next
    End Sub
End Class





Module EMax_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function EMax_Basics_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub EMax_Basics_Close(EMax_BasicsPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function EMax_Basics_Run(EMax_BasicsPtr As IntPtr, samplesPtr As IntPtr, labelsPtr As IntPtr, rows As Integer, cols As Integer, imgRows As Integer,
                                    imgCols As Integer, clusters As Integer, stepSize As Integer, covarianceMatrixType As Integer) As IntPtr
    End Function
End Module





Public Class EMax_CPP
    Inherits VBparent
    Public basics As EMax_Basics
    Dim inputDataMask As cv.Mat
    Dim EMax_Basics As IntPtr
    Public Sub New()
        initParent()
        basics = New EMax_Basics()

        EMax_Basics = EMax_Basics_Open()

        label2 = "Emax regions around clusters"
        task.desc = "Use EMax - Expectation Maximization - to classify a series of points"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        basics.Run()
        dst1 = basics.dst1
        Dim srcCount = basics.sliders.trackbar(0).Value
        label1 = CStr(srcCount) + " Random samples in " + CStr(basics.regionCount) + " clusters"
        If basics.regionCount <= 0 Then Exit Sub

        Dim covarianceMatrixType As Integer = 0
        For i = 0 To 3 - 1
            If basics.radio.check(i).Checked = True Then
                covarianceMatrixType = Choose(i + 1, cv.EM.Types.CovMatSpherical, cv.EM.Types.CovMatDiagonal, cv.EM.Types.CovMatGeneric)
            End If
        Next

        Dim srcData((srcCount - 1) * 2) As Single
        Dim handleSrc As GCHandle
        handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Marshal.Copy(basics.samples.Data, srcData, 0, srcData.Length)

        Dim labelData(srcCount - 1) As Integer
        Dim handleLabels As GCHandle
        handleLabels = GCHandle.Alloc(labelData, GCHandleType.Pinned)
        Marshal.Copy(basics.labels.Data, labelData, 0, labelData.Length)

        Dim imagePtr = EMax_Basics_Run(EMax_Basics, handleSrc.AddrOfPinnedObject(), handleLabels.AddrOfPinnedObject(), srcCount, 2,
                                       dst1.Rows, dst1.Cols, basics.regionCount, basics.sliders.trackbar(1).Value, covarianceMatrixType)
        handleLabels.Free() ' free the pinned memory...
        handleSrc.Free() ' free the pinned memory...

        If imagePtr <> 0 Then dst2 = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_8UC3, imagePtr)

        Static showInputCheck = findCheckBox("Show EMax input in output")
        If showInputCheck.Checked Then
            inputDataMask = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)
            dst1.CopyTo(dst2, inputDataMask)
        End If
    End Sub
    Public Sub Close()
        EMax_Basics_Close(EMax_Basics)
    End Sub
End Class







Public Class EMax_Centroids
    Inherits VBparent
    Public emaxCPP As EMax_CPP
    Public flood As FloodFill_Image
    Public Sub New()
        initParent()

        flood = New FloodFill_Image()

        emaxCPP = New EMax_CPP()
        Dim lowDiffslider = findSlider("FloodFill LoDiff")
        Dim highDiffslider = findSlider("FloodFill HiDiff")
        lowDiffslider.Value = 1
        highDiffslider.Value = 1

        Dim gridWidthSlider = findSlider("ThreadGrid Width")
        gridWidthSlider.Value = src.Width * 170 / 640

        task.desc = "Get the Emax cluster centroids using floodfill "
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        emaxCPP.Run()

        flood.src = emaxCPP.dst2.Clone
        flood.Run()
        dst1 = flood.dst2

        Static lastCentroids As New List(Of cv.Point2f)
        For i = 0 To flood.centroids.Count - 1
            dst1.Circle(flood.centroids(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
            If i < lastCentroids.count Then
                dst1.Circle(lastCentroids(i), 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            End If
        Next
        lastCentroids = New List(Of cv.Point2f)(flood.centroids)
    End Sub
End Class





Public Class EMax_PointTracker
    Inherits VBparent
    Dim pTrack As KNN_PointTracker
    Dim emax As EMax_Centroids
    Public Sub New()
        initParent()

        emax = New EMax_Centroids()

        pTrack = New KNN_PointTracker()
        Dim rectCheckbox = findCheckBox("Draw rectangle and centroid for each mask")
        rectCheckbox.Checked = False
        Dim floodMinSlider = findSlider("FloodFill Minimum Size")
        floodMinSlider.Value = 100

        label1 = "Original before KNN/Kalman tracking (red=previous)"
        task.desc = "Use KNN and Kalman to track the EMax Centroids and map consisten colors"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        emax.Run()
        dst1 = emax.dst1

        pTrack.queryPoints = emax.flood.centroids
        pTrack.queryMasks = emax.flood.masks
        pTrack.queryRects = emax.flood.rects
        pTrack.Run()
        dst2 = pTrack.dst1

        ' this is to verify that the colors are remaining largely consistent (they may change if more centroids appear.)
        Static lastImage = dst2
        Dim tallyErrors = 0
        For Each pt In emax.flood.centroids
            Dim v1 = dst2.Get(Of cv.Vec3b)(pt.Y, pt.X)
            Dim v2 = lastImage.Get(Of cv.Vec3b)(pt.Y, pt.X)
            If v1 <> v2 Then tallyErrors += 1
        Next
        lastImage = dst2.Clone
        Static totalErrors = 0
        Static generationCount = 0
        Static saveCount = 0
        If emax.emaxCPP.basics.grid.roiList.Count <> saveCount Then
            saveCount = emax.emaxCPP.basics.grid.roiList.Count
            totalErrors = 0
            generationCount = 0
        End If
        totalErrors += tallyErrors
        generationCount += 1
        label2 = "After: there were " + Format(totalErrors / generationCount, "0.0") + " average errors matching centroids"
    End Sub
End Class
