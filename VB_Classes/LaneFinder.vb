Imports cv = OpenCvSharp
Imports System.IO
Public Class LaneFinder_Basics : Inherits TaskParent
    Dim lane As New LaneFinder_SlopeIntercept
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true
        If standalone Then task.gOptions.displaydst1.checked = true
        desc = "The basics of lane-finding.  A better name than LaneFinder_SlopeIntercept"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        lane.Run(src)
        dst0 = lane.dst0
        dst1 = lane.dst1
        dst2 = lane.dst2
        dst3 = lane.dst3
    End Sub
End Class





' https://github.com/mohamedameen93/Lane-lines-detection-using-Python-and-OpenCV
Public Class LaneFinder_Videos : Inherits TaskParent
    Public video As New Video_Basics
    Dim options As New Options_LaneFinder
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true
        If standalone Then task.gOptions.displaydst1.checked = true

        Dim inputfile = New FileInfo(task.HomeDir + options.inputName)
        video.options.fileInfo = New FileInfo(inputfile.FullName)
        desc = "Read in the videos showing road conditions."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        If task.optionsChanged Then
            Dim inputfile = New FileInfo(task.HomeDir + options.inputName)
            If inputfile.Exists Then video.options.fileInfo = New FileInfo(inputfile.FullName)
        End If

        video.Run(src)
        dst2 = video.dst2
    End Sub
End Class






' https://github.com/mohamedameen93/Lane-lines-detection-using-Python-and-OpenCV
Public Class LaneFinder_Edges : Inherits TaskParent
    Dim input As New LaneFinder_Videos
    Dim edges As New Edge_Basics
    Public Sub New()
        desc = "Using the videos provided, find the lane markers."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        input.Run(src)
        dst0 = input.dst2
        edges.Run(dst0)
        dst2 = edges.dst2
    End Sub
End Class








' https://github.com/mohamedameen93/Lane-lines-detection-using-Python-and-OpenCV
Public Class LaneFinder_HLSColor : Inherits TaskParent
    Public input As New LaneFinder_Videos
    Public Sub New()
        labels = {"HLS color conversion", "InRange White", "InRange Yellow", "Combined InRange White and InRange Yellow results"}
        desc = "Isolate the colors for the white and yellow"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        input.Run(src)
        dst0 = input.dst2.CvtColor(cv.ColorConversionCodes.BGR2HLS)
        dst1 = dst0.InRange(New cv.Scalar(0, 200, 0), New cv.Scalar(255, 255, 255))
        dst2 = dst0.InRange(New cv.Scalar(10, 0, 100), New cv.Scalar(40, 255, 255))
        dst3 = dst1 Or dst2
    End Sub
End Class








' https://github.com/mohamedameen93/Lane-lines-detection-using-Python-and-OpenCV
Public Class LaneFinder_ROI : Inherits TaskParent
    Dim hls As New LaneFinder_HLSColor
    Dim pListList = New cv.Point()() {Nothing}
    Public Sub New()
        labels = {"Original input", "Mask showing ROI", "HLS version with ROI outline", "HLS Mask with ROI outline"}
        desc = "Define the ROI for the location of the lanes"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        hls.Run(src)

        If task.optionsChanged Then
            Dim w = hls.input.video.dst2.Width
            Dim h = hls.input.video.dst2.Height

            Dim bl = New cv.Point(w * 0.1, h * 0.95)
            Dim tl = New cv.Point(w * 0.4, h * 0.6)
            Dim br = New cv.Point(w * 0.95, h * 0.95)
            Dim tr = New cv.Point(w * 0.6, h * 0.6)

            Dim pList() As cv.Point = {bl, tl, tr, br}
            dst1 = New cv.Mat(New cv.Size(w, h), cv.MatType.CV_8U, cv.Scalar.All(0))
            dst1.FillConvexPoly(pList, white, task.lineType)
            pListList(0) = pList
        End If

        dst0 = hls.input.video.dst2
        dst2 = hls.dst0
        dst3 = hls.dst3
        cv.Cv2.Polylines(dst0, pListList, True, cv.Scalar.White, task.lineWidth, task.lineType, 0)
        cv.Cv2.Polylines(dst2, pListList, True, cv.Scalar.White, task.lineWidth, task.lineType, 0)
        cv.Cv2.Polylines(dst3, pListList, True, cv.Scalar.White, task.lineWidth, task.lineType, 0)
    End Sub
End Class








Public Class LaneFinder_SlopeIntercept : Inherits TaskParent
    Dim hough As New Hough_LaneFinder
    Public leftLaneIntercept As Single
    Public rightLaneIntercept As Single
    Public leftAvgSlope As Single
    Public rightAvgSlope As Single
    Public Sub New()
        desc = "Use the Hough lines found to build a slope intercept format line."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        hough.Run(src)
        dst0 = hough.dst0
        dst1 = hough.dst2
        dst2 = hough.dst3
        dst3 = hough.dst0.Clone

        If hough.segments.Count = 0 Then Exit Sub
        Dim leftIntercept As New List(Of Single), leftSlope As New List(Of Single), leftWeight As New List(Of Single)
        Dim rightIntercept As New List(Of Single), rightSlope As New List(Of Single), rightWeight As New List(Of Single)
        For Each line In hough.segments
            If line.P1.X = line.P2.X Then Continue For
            Dim slope = (line.P1.Y - line.P2.Y) / (line.P1.X - line.P2.X)
            If slope < 0 Then
                leftIntercept.Add(line.P1.Y - (slope * line.P1.X))
                leftSlope.Add(slope)
                leftWeight.Add(line.P1.DistanceTo(line.P2))
            Else
                rightIntercept.Add(line.P1.Y - (slope * line.P1.X))
                rightSlope.Add(slope)
                rightWeight.Add(line.P1.DistanceTo(line.P2))
            End If
        Next
        Dim mat1 = cv.Mat.FromPixelData(leftWeight.Count, 1, cv.MatType.CV_32F, leftWeight.ToArray)
        Dim mat2 = cv.Mat.FromPixelData(leftSlope.Count, 1, cv.MatType.CV_32F, leftSlope.ToArray)
        Dim mat3 = cv.Mat.FromPixelData(leftIntercept.Count, 1, cv.MatType.CV_32F, leftIntercept.ToArray)
        Dim weight = leftWeight.Sum()
        leftLaneIntercept = mat1.Dot(mat3) / weight
        leftAvgSlope = mat1.Dot(mat2) / weight

        mat1 = cv.Mat.FromPixelData(rightWeight.Count, 1, cv.MatType.CV_32F, rightWeight.ToArray)
        mat2 = cv.Mat.FromPixelData(rightSlope.Count, 1, cv.MatType.CV_32F, rightSlope.ToArray)
        mat3 = cv.Mat.FromPixelData(rightIntercept.Count, 1, cv.MatType.CV_32F, rightIntercept.ToArray)
        weight = rightWeight.Sum()
        rightLaneIntercept = mat1.Dot(mat3) / weight
        rightAvgSlope = mat1.Dot(mat2) / weight

        SetTrueText("Left lane intercept = " + Format(leftLaneIntercept, fmt1) +
                    " Right lane intercept = " + Format(rightLaneIntercept, fmt1) + vbCrLf +
                    "Left slope = " + Format(leftAvgSlope, fmt3) +
                    " Right slope = " + Format(rightAvgSlope, fmt3), 3)

        Dim tmp = dst2.EmptyClone()

        Dim p1 = New cv.Point(0, leftLaneIntercept)
        Dim p2 = New cv.Point(-leftLaneIntercept / leftAvgSlope, 0)
        tmp.Line(p1, p2, white, task.lineWidth, task.lineType)

        p1 = New cv.Point(0, rightLaneIntercept)
        p2 = New cv.Point((dst0.Height - rightLaneIntercept) / rightAvgSlope, dst2.Height)
        tmp.Line(p1, p2, white, task.lineWidth, task.lineType)

        tmp.CopyTo(dst2, hough.mask)
        dst2.CopyTo(dst3, dst2)
    End Sub
End Class