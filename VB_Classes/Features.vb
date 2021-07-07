Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Features_GoodFeatures : Inherits VBparent
    Public goodFeatures As New List(Of cv.Point2f)
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Number of Points", 10, 1000, 200)
            sliders.setupTrackBar(1, "Quality Level", 1, 100, 1)
            sliders.setupTrackBar(2, "Distance", 1, 100, 30)
        End If
        task.desc = "Find good features to track in an RGB image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim numPoints = sliders.trackbar(0).Value
        Dim quality = sliders.trackbar(1).Value / 100
        Dim minDistance = sliders.trackbar(2).Value
        Dim features = cv.Cv2.GoodFeaturesToTrack(src, numPoints, quality, minDistance, Nothing, 7, True, 3)

        src.CopyTo(dst2)
        goodFeatures.Clear()
        For i = 0 To features.Length - 1
            goodFeatures.Add(features.ElementAt(i))
            dst2.Circle(features(i), task.dotSize, cv.Scalar.White, -1, task.lineType)
        Next
    End Sub
End Class





Public Class Features_PointTracker : Inherits VBparent
    Dim features As New Features_GoodFeatures
    Dim pTrack As New KNN_PointTracker
    Dim rRadius = 10
    Public Sub New()
        findCheckBox("Draw rectangle and centroid for each mask").Checked = False
        findSlider("Minimum size of object in pixels").Value = 1

        labels(2) = "Good features without Kalman"
        labels(3) = "Good features with Kalman"
        task.desc = "Find good features and track them"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        features.RunClass(src)
        dst2 = features.dst2

        pTrack.queryPoints.Clear()
        pTrack.queryRects.Clear()
        pTrack.queryMasks.Clear()

        For i = 0 To features.goodFeatures.Count - 1
            Dim pt = features.goodFeatures(i)
            pTrack.queryPoints.Add(pt)
            Dim r = New cv.Rect(pt.X - rRadius, pt.Y - rRadius, rRadius * 2, rRadius * 2)
            pTrack.queryRects.Add(r)
            pTrack.queryMasks.Add(New cv.Mat)
        Next

        pTrack.RunClass(src)

        dst3.SetTo(0)
        For Each obj In pTrack.drawRC.viewObjects
            Dim r = obj.Value.rectInHist
            If r.Width > 0 And r.Height > 0 Then
                If r.X + r.Width < dst3.Width And r.Y + r.Height < dst3.Height Then src(obj.Value.rectInHist).CopyTo(dst3(obj.Value.rectInHist))
            End If
        Next
    End Sub
End Class






Module Feature_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Agast_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Agast_Close(Harris_FeaturesPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Agast_Run(Harris_FeaturesPtr As IntPtr, inputPtr As IntPtr, rows As Integer, cols As Integer, count As IntPtr) As IntPtr
    End Function
End Module







Public Class Features_Agast : Inherits VBparent
    Dim srcData() As Byte
    Dim ptCount(1) As Integer
    Dim AgastPtr As IntPtr
    Public FeaturePoints As New List(Of cv.Point2f)
    Public Sub New()
        ReDim srcData(dst2.Total * dst2.ElemSize - 1)
        AgastPtr = Agast_Open()
        task.desc = "Use the Agast Feature Detector in the OpenCV Contrib"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)

        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim handleCount = GCHandle.Alloc(ptCount, GCHandleType.Pinned)
        Dim ptPtr = Agast_Run(AgastPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, handleCount.AddrOfPinnedObject())
        handleSrc.Free()
        handleCount.Free()
        Dim minX = Single.MaxValue, maxX = Single.MinValue, minY = Single.MaxValue, maxY = Single.MinValue
        If ptCount(0) > 1 And ptPtr <> 0 Then
            Dim pts((ptCount(0) - 1) * 2 - 1) As Single
            Marshal.Copy(ptPtr, pts, 0, ptCount(0))
            Dim ptMat = New cv.Mat(ptCount(0), 7, cv.MatType.CV_32F, pts)
            dst2 = src.Clone
            FeaturePoints.Clear()
            For i = 0 To ptMat.Rows - 1
                FeaturePoints.Add(New cv.Point2f(ptMat.Get(Of Single)(i, 0), ptMat.Get(Of Single)(i, 1)))
                dst2.Circle(FeaturePoints(i), task.dotSize + 2, cv.Scalar.Yellow, -1, task.lineType)
                If minX > FeaturePoints(i).X Then minX = FeaturePoints(i).X
                If maxX < FeaturePoints(i).X And FeaturePoints(i).X < dst2.Width Then maxX = FeaturePoints(i).X
                If minY > FeaturePoints(i).Y Then minY = FeaturePoints(i).Y
                If maxY < FeaturePoints(i).Y And FeaturePoints(i).Y < dst2.Height Then maxY = FeaturePoints(i).Y
            Next
        End If
        labels(2) = "Found " + CStr(FeaturePoints.Count) + " features maxX = " + Format(maxX, "0.0") + " maxY = " + Format(maxY, "0.0")
        setTrueText("This algorithm has some problems (probably my fault.)" + vbCrLf + "The points in the top 3rd look mostly correct." + vbCrLf +
                    "but below that the bottom 2/3rd's are missing.  Was it expecting grayscale? " + vbCrLf +
                    "It works with both but same results", 10, 100, 3)
    End Sub
    Public Sub Close()
        Agast_Close(AgastPtr)
    End Sub
End Class