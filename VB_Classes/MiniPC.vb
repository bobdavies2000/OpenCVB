Imports cv = OpenCvSharp
Public Class MiniPC_Basics : Inherits VBparent
    Dim resize As Resize_Percentage
    Public rect As cv.Rect
    Dim gCloud As New Depth_PointCloud_IMU
    Public Sub New()
        resize = New Resize_Percentage
        task.desc = "Create a mini point cloud for use with histograms"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        gCloud.Run(src)
        resize.Run(gCloud.dst1)

        Dim split = resize.dst1.Split()
        split(2).SetTo(0, task.noDepthMask.Resize(split(2).Size))
        rect = New cv.Rect(0, 0, resize.dst1.Width, resize.dst1.Height)
        If rect.Height < dst1.Height / 2 Then rect.Y = dst1.Height / 4 ' move it below the dst1 caption
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst1(rect) = split(2).ConvertScaleAbs(255)
        dst1.Rectangle(rect, cv.Scalar.White, 1)
        cv.Cv2.Merge(split, dst2)
        label1 = "MiniPC is " + CStr(rect.Width) + "x" + CStr(rect.Height) + " total pixels = " + CStr(rect.Width * rect.Height)
    End Sub
End Class








Public Class MiniPC_Rotate : Inherits VBparent
    Public mini As New MiniPC_Basics
    Public histogram As New cv.Mat
    Public angleY As Integer
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        label2 = "Side view after resize percentage"
        task.desc = "Create a histogram for the mini point cloud"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static angleYslider = findSlider("Amount to rotate pointcloud around Y-axis (degrees)")

        Dim input = src
        If standalone Then
            mini.Run(input)
            input = mini.dst2 ' the task.pointcloud
            angleY = angleYslider.value
        End If

        Dim cx As Double = 1, sx As Double = 0, cy As Double = 1, sy As Double = 0, cz As Double = 1, sz As Double = 0
        Dim gM(,) As Single = {{cx * 1 + -sx * 0 + 0 * 0, cx * 0 + -sx * cz + 0 * sz, cx * 0 + -sx * -sz + 0 * cz},
                               {sx * 1 + cx * 0 + 0 * 0, sx * 0 + cx * cz + 0 * sz, sx * 0 + cx * -sz + 0 * cz},
                               {0 * 1 + 0 * 0 + 1 * 0, 0 * 0 + 0 * cz + 1 * sz, 0 * 0 + 0 * -sz + 1 * cz}}
        '[cos(a) 0 -sin(a)]
        '[0      1       0]
        '[sin(a) 0   cos(a] rotate the point cloud around the y-axis.
        cy = Math.Cos(angleY * cv.Cv2.PI / 180)
        sy = Math.Sin(angleY * cv.Cv2.PI / 180)
        gM = {{gM(0, 0) * cy + gM(0, 1) * 0 + gM(0, 2) * sy}, {gM(0, 0) * 0 + gM(0, 1) * 1 + gM(0, 2) * 0}, {gM(0, 0) * -sy + gM(0, 1) * 0 + gM(0, 2) * cy},
              {gM(1, 0) * cy + gM(1, 1) * 0 + gM(1, 2) * sy}, {gM(1, 0) * 0 + gM(1, 1) * 1 + gM(1, 2) * 0}, {gM(1, 0) * -sy + gM(1, 1) * 0 + gM(1, 2) * cy},
              {gM(2, 0) * cy + gM(2, 1) * 0 + gM(2, 2) * sy}, {gM(2, 0) * 0 + gM(2, 1) * 1 + gM(2, 2) * 0}, {gM(2, 0) * -sy + gM(2, 1) * 0 + gM(2, 2) * cy}}

        Dim gMat = New cv.Mat(3, 3, cv.MatType.CV_32F, gM)
        Dim gInput = input.Reshape(1, input.Rows * input.Cols)
        Dim gOutput = (gInput * gMat).ToMat
        input = gOutput.Reshape(3, input.Rows)

        Dim split = input.Split()
        Dim mask = split(2).Threshold(task.minDepth / 1000, 255, cv.ThresholdTypes.BinaryInv)
        input.SetTo(0, mask.ConvertScaleAbs(255)) ' remove zero depth pixels with non-zero x and y.

        Dim ranges() = New cv.Rangef() {New cv.Rangef(-task.maxY, task.maxY), New cv.Rangef(0, task.maxZ)}
        Dim histSize() = {input.Height, input.Width}
        cv.Cv2.CalcHist(New cv.Mat() {input}, New Integer() {1, 2}, New cv.Mat, histogram, 2, histSize, ranges)

        dst1(mini.rect) = histogram.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        dst2(mini.rect) = input.ConvertScaleAbs(255)
    End Sub
End Class







Public Class MiniPC_RotateAngle : Inherits VBparent
    Dim peak As New MiniPC_Rotate
    Dim mats As New Mat_4to1
    Public plot As New Plot_OverTime
    Dim resetCheck As Windows.Forms.CheckBox
    Public Sub New()
        plot.controlScale = True ' we are controlling the scale...
        plot.maxScale = 1
        plot.minScale = 0
        resetCheck = findCheckBox("Reset the plot scale")
        resetCheck.Checked = False

        peak.angleY = -45

        label1 = "peak dst1, peak dst2, changed mask, maxvalues history"
        label2 = "Blue is mean*100, red is maxVal/100, green mask count"
        task.desc = "Find a peak value in the side view histograms"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Type <> cv.MatType.CV_32FC3 Then
            peak.mini.Run(src)
            src = peak.mini.dst2
        Else
            src = src
        End If

        Dim r = peak.mini.rect
        Dim sz = New cv.Size(r.Width, r.Height)
        peak.Run(src)
        peak.angleY += 1
        If peak.angleY > 45 Then peak.angleY = -45

        Dim minLoc As cv.Point, maxLoc As cv.Point
        peak.histogram.MinMaxLoc(minVal, maxVal, minLoc, maxLoc)

        Dim mean = peak.histogram.Mean().Item(0) * 100
        Dim mask = peak.histogram.Threshold(mean, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        mats.mat(2) = mask

        Dim metric = maxVal 'mean, mask.CountNonZero(), maxVal)
        If plot.maxScale < metric Then
            plot.maxScale = metric + 0.1 * metric
            resetCheck.Checked = True
        End If
        plot.plotData = New cv.Scalar(metric)
        plot.Run(Nothing)
        dst2 = plot.dst1
        mats.mat(3) = peak.histogram.ConvertScaleAbs(255)

        mats.mat(0) = peak.dst1(peak.mini.rect)
        mats.mat(1) = peak.dst2(peak.mini.rect)
        mats.Run(src)
        dst1 = mats.dst1
    End Sub
End Class









Public Class MiniPC_RotateSinglePass : Inherits VBparent
    Dim peak As New MiniPC_Rotate
    Public Sub New()
        peak.angleY = -90
        task.desc = "Same operation as New MiniPC_RotateAngle but in a single pass."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        peak.mini.Run(src)

        Dim r = peak.mini.rect
        Dim sz = New cv.Size(r.Width, r.Height)
        Dim maxHist = Single.MinValue
        Dim minLoc As cv.Point, maxLoc As cv.Point
        Dim bestAngle As Integer
        Dim bestLoc As cv.Point
        For i = -90 To 90
            peak.Run(peak.mini.dst2)
            peak.angleY = i
            peak.histogram.MinMaxLoc(minVal, maxVal, minLoc, maxLoc)
            If maxVal > maxHist Then
                maxHist = maxVal
                bestAngle = i
                bestLoc = maxLoc
            End If
        Next
        peak.Run(peak.mini.dst2)
        peak.angleY = bestAngle
        dst1 = peak.dst1
        dst2 = peak.dst2
    End Sub
End Class
