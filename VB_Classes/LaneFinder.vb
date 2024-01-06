Imports cv = OpenCvSharp
Imports  System.IO
Public Class LaneFinder_Basics : Inherits VB_Algorithm
    Dim lane As New LaneFinder_SlopeIntercept
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "The basics of lane-finding.  A better name than LaneFinder_SlopeIntercept"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        lane.Run(src)
        dst0 = lane.dst0
        dst1 = lane.dst1
        dst2 = lane.dst2
        dst3 = lane.dst3
    End Sub
End Class





' https://github.com/mohamedameen93/Lane-lines-detection-using-Python-and-OpenCV
Public Class LaneFinder_Videos : Inherits VB_Algorithm
    Public video As New Video_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        If standalone Then gOptions.displayDst1.Checked = True
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("challenge.mp4")
            radio.addRadio("solidWhiteRight.mp4")
            radio.addRadio("solidYellowLeft.mp4")
            radio.check(0).Checked = True
        End If
        desc = "Read in the videos showing road conditions."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If task.optionsChanged Then
            Static frm = findfrm(traceName + " Radio Buttons")
            Dim inputfile As New FileInfo(task.homeDir + "/Data/" + findRadioText(frm.check))
            If inputfile.Exists Then video.fileNameForm.filename.Text = inputfile.FullName
        End If
        video.Run(empty)
        dst2 = video.dst2
    End Sub
End Class






' https://github.com/mohamedameen93/Lane-lines-detection-using-Python-and-OpenCV
Public Class LaneFinder_Edges : Inherits VB_Algorithm
    Dim input As New LaneFinder_Videos
    Dim edges As New Edge_All
    Public Sub New()
        desc = "Using the videos provided, find the lane markers."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        input.Run(empty)
        dst0 = input.dst2
        edges.Run(dst0)
        dst2 = edges.dst2
    End Sub
End Class








' https://github.com/mohamedameen93/Lane-lines-detection-using-Python-and-OpenCV
Public Class LaneFinder_HLSColor : Inherits VB_Algorithm
    Public input As New LaneFinder_Videos
    Public Sub New()
        labels = {"HLS color conversion", "InRange White", "InRange Yellow", "Combined InRange White and InRange Yellow results"}
        desc = "Isolate the colors for the white and yellow"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        input.Run(empty)
        dst0 = input.dst2.CvtColor(cv.ColorConversionCodes.BGR2HLS)
        dst1 = dst0.InRange(New cv.Scalar(0, 200, 0), New cv.Scalar(255, 255, 255))
        dst2 = dst0.InRange(New cv.Scalar(10, 0, 100), New cv.Scalar(40, 255, 255))
        dst3 = dst1 Or dst2
    End Sub
End Class








' https://github.com/mohamedameen93/Lane-lines-detection-using-Python-and-OpenCV
Public Class LaneFinder_ROI : Inherits VB_Algorithm
    Dim hls As New LaneFinder_HLSColor
    Public Sub New()
        labels = {"Original input", "Mask showing ROI", "HLS version with ROI outline", "HLS Mask with ROI outline"}
        desc = "Define the ROI for the location of the lanes"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static pListList = New cv.Point()() {Nothing}
        hls.Run(empty)

        If task.optionsChanged Then
            Dim w = hls.input.video.dst2.Width
            Dim h = hls.input.video.dst2.Height

            Dim bl = New cv.Point(w * 0.1, h * 0.95)
            Dim tl = New cv.Point(w * 0.4, h * 0.6)
            Dim br = New cv.Point(w * 0.95, h * 0.95)
            Dim tr = New cv.Point(w * 0.6, h * 0.6)

            Dim pList() As cv.Point = {bl, tl, tr, br}
            dst1 = New cv.Mat(New cv.Size(w, h), cv.MatType.CV_8U, 0)
            dst1.FillConvexPoly(pList, cv.Scalar.White, task.lineType)
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








Public Class LaneFinder_SlopeIntercept : Inherits VB_Algorithm
    Dim hough As New Hough_LaneFinder
    Public leftLaneIntercept As Single
    Public rightLaneIntercept As Single
    Public leftAvgSlope As Single
    Public rightAvgSlope As Single
    Public Sub New()
        desc = "Use the Hough lines found to build a slope intercept format line."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hough.Run(empty)
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
        Dim mat1 = New cv.Mat(leftWeight.Count, 1, cv.MatType.CV_32F, leftWeight.ToArray)
        Dim mat2 = New cv.Mat(leftSlope.Count, 1, cv.MatType.CV_32F, leftSlope.ToArray)
        Dim mat3 = New cv.Mat(leftIntercept.Count, 1, cv.MatType.CV_32F, leftIntercept.ToArray)
        Dim weight = leftWeight.Sum()
        leftLaneIntercept = mat1.Dot(mat3) / weight
        leftAvgSlope = mat1.Dot(mat2) / weight

        mat1 = New cv.Mat(rightWeight.Count, 1, cv.MatType.CV_32F, rightWeight.ToArray)
        mat2 = New cv.Mat(rightSlope.Count, 1, cv.MatType.CV_32F, rightSlope.ToArray)
        mat3 = New cv.Mat(rightIntercept.Count, 1, cv.MatType.CV_32F, rightIntercept.ToArray)
        weight = rightWeight.Sum()
        rightLaneIntercept = mat1.Dot(mat3) / weight
        rightAvgSlope = mat1.Dot(mat2) / weight

        setTrueText("Left lane intercept = " + Format(leftLaneIntercept, fmt1) +
                    " Right lane intercept = " + Format(rightLaneIntercept, fmt1) + vbCrLf +
                    "Left slope = " + Format(leftAvgSlope, fmt3) +
                    " Right slope = " + Format(rightAvgSlope, fmt3), 3)

        Dim tmp = dst2.EmptyClone()

        Dim p1 = New cv.Point(0, leftLaneIntercept)
        Dim p2 = New cv.Point(-leftLaneIntercept / leftAvgSlope, 0)
        tmp.Line(p1, p2, cv.Scalar.White, task.lineWidth, task.lineType)

        p1 = New cv.Point(0, rightLaneIntercept)
        p2 = New cv.Point((dst0.Height - rightLaneIntercept) / rightAvgSlope, dst2.Height)
        tmp.Line(p1, p2, cv.Scalar.White, task.lineWidth, task.lineType)

        tmp.CopyTo(dst2, hough.mask)
        dst2.CopyTo(dst3, dst2)
    End Sub
End Class