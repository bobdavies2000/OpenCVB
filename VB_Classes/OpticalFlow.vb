Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module OpticalFlowModule_Exports
    ' https://docs.opencv.org/3.4/db/d7f/tutorial_js_lucas_kanade.html
    Public Function opticalFlow_Dense(oldGray As cv.Mat, gray As cv.Mat, pyrScale As Single, levels As integer, winSize As integer, iterations As integer,
                                polyN As Single, polySigma As Single, OpticalFlowFlags As cv.OpticalFlowFlags) As cv.Mat
        Dim flow As New cv.Mat
        If pyrScale >= 1 Then pyrScale = 0.99

        ' When running "Test All", the OpenGL code requires full resolution which switches to low resolution (if active) after completion.
        ' The first frame after switching will mean oldgray is full resolution and gray is low resolution.  This "If" avoids the problem.
        ' if another algorithm lexically follows the OpenGL algorithms, this may change (or be deleted!)
        If oldGray.Size() <> gray.Size() Then oldGray = gray.Clone()

        cv.Cv2.CalcOpticalFlowFarneback(oldGray, gray, flow, pyrScale, levels, winSize, iterations, polyN, polySigma, OpticalFlowFlags)
        Dim flowVec(1) As cv.Mat
        flowVec = flow.Split()

        Dim hsv As New cv.Mat
        Dim hsv0 As New cv.Mat
        Dim hsv1 As New cv.Mat(gray.Rows, gray.Cols, cv.MatType.CV_8UC1, 255)
        Dim hsv2 As New cv.Mat

        Dim magnitude As New cv.Mat
        Dim angle As New cv.Mat
        cv.Cv2.CartToPolar(flowVec(0), flowVec(1), magnitude, angle)
        angle.ConvertTo(hsv0, cv.MatType.CV_8UC1, 180 / Math.PI / 2)
        cv.Cv2.Normalize(magnitude, hsv2, 0, 255, cv.NormTypes.MinMax, cv.MatType.CV_8UC1)

        Dim hsvVec() As cv.Mat = {hsv0, hsv1, hsv2}
        cv.Cv2.Merge(hsvVec, hsv)
        Return hsv
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OpticalFlow_CPP_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OpticalFlow_CPP_Close(sPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OpticalFlow_CPP_Run(sPtr As IntPtr, bgrPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function
    Public Sub calcOpticalFlowPyrLK_Native(gray1 As cv.Mat, gray2 As cv.Mat, features1 As cv.Mat, features2 As cv.Mat)
        Dim hGray1 As GCHandle
        Dim hGray2 As GCHandle
        Dim hF1 As GCHandle
        Dim hF2 As GCHandle

        Dim grayData1(gray1.Total - 1)
        Dim grayData2(gray2.Total - 1)
        Dim fData1(features1.Total * features1.ElemSize - 1)
        Dim fData2(features2.Total * features2.ElemSize - 1)
        hGray1 = GCHandle.Alloc(grayData1, GCHandleType.Pinned)
        hGray2 = GCHandle.Alloc(grayData2, GCHandleType.Pinned)
        hF1 = GCHandle.Alloc(fData1, GCHandleType.Pinned)
        hF2 = GCHandle.Alloc(fData2, GCHandleType.Pinned)
    End Sub
End Module






'https://www.learnopencv.com/optical-flow-in-opencv/?ck_subscriber_id=785741175
Public Class OpticalFlow_DenseBasics : Inherits VB_Algorithm
    Dim options As New Options_OpticalFlow
    Public Sub New()
        desc = "Use dense optical flow algorithm  "
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Options.RunVB()
        Static lastGray As cv.Mat = src.Clone
        Dim hsv = opticalFlow_Dense(lastGray, src, options.pyrScale, options.levels, options.winSize, options.iterations, options.polyN,
                                    options.polySigma, options.OpticalFlowFlags)

        dst2 = hsv.CvtColor(cv.ColorConversionCodes.HSV2RGB)
        dst2 = dst2.ConvertScaleAbs(options.outputScaling)
        dst3 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        lastGray = src.Clone()
    End Sub
End Class







' https://www.learnopencv.com/optical-flow-in-opencv/?ck_subscriber_id=785741175
Public Class OpticalFlow_Sparse : Inherits VB_Algorithm
    Public features As New List(Of cv.Point2f)
    Dim good As New Feature_Basics
    Dim sumScale As cv.Mat, sScale As cv.Mat
    Dim errScale As cv.Mat, qScale As cv.Mat, rScale As cv.Mat
    Dim options As New Options_OpticalFlowSparse
    Public Sub New()
        desc = "Show the optical flow of a sparse matrix."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()

        dst2 = src.Clone()
        dst3 = src.Clone()

        If task.optionsChanged Then
            errScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 1)
            qScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0.004)
            rScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0.5)
            sumScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0)
            sScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0)
        End If

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Static lastGray As cv.Mat = src.Clone
        good.Run(src)
        features = good.corners
        Dim features1 = New cv.Mat(features.Count, 1, cv.MatType.CV_32FC2, features.ToArray)
        Dim features2 = New cv.Mat
        Dim status As New cv.Mat, err As New cv.Mat, winSize As New cv.Size(3, 3)
        cv.Cv2.CalcOpticalFlowPyrLK(src, lastgray, features1, features2, status, err, winSize, 3, term, options.OpticalFlowFlag)
        features = New List(Of cv.Point2f)
        Dim lastFeatures As New List(Of cv.Point2f)
        For i = 0 To status.Rows - 1
            If status.Get(Of Byte)(i, 0) Then
                Dim pt1 = features1.Get(Of cv.Point2f)(i, 0)
                Dim pt2 = features2.Get(Of cv.Point2f)(i, 0)
                Dim length = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y))
                If length < 30 Then
                    features.Add(pt1)
                    lastFeatures.Add(pt2)
                    dst2.Line(pt1, pt2, cv.Scalar.Red, task.lineWidth + task.lineWidth + 2, task.lineType)
                    dst3.Circle(pt1, task.dotSize + 3, cv.Scalar.White, -1, task.lineType)
                    dst3.Circle(pt2, task.dotSize + 1, cv.Scalar.Red, -1, task.lineType)
                End If
            End If
        Next
        labels(2) = "Matched " + CStr(features.Count) + " points "

        If heartBeat() Then lastGray = src.Clone()
        lastGray = src.Clone()
    End Sub
End Class