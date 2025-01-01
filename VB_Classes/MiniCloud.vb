Imports cv = OpenCvSharp
Public Class MiniCloud_Basics : Inherits TaskParent
    Dim resize As Resize_Smaller
    Public rect As cv.Rect
    Public options As New Options_IMU
    Public Sub New()
        resize = New Resize_Smaller
        FindSlider("LowRes %").Value = 25
        desc = "Create a mini point cloud for use with histograms"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        resize.Run(task.pointCloud)

        Dim split = resize.dst2.Split()
        split(2).SetTo(0, task.noDepthMask.Resize(split(2).Size))
        rect = New cv.Rect(0, 0, resize.dst2.Width, resize.dst2.Height)
        If rect.Height < dst2.Height / 2 Then rect.Y = dst2.Height / 4 ' move it below the dst2 caption
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst2(rect) = split(2).ConvertScaleAbs(255)
        dst2.Rectangle(rect, white, 1)
        cv.Cv2.Merge(split, dst3)
        labels(2) = "MiniPC is " + CStr(rect.Width) + "x" + CStr(rect.Height) + " total pixels = " + CStr(rect.Width * rect.Height)
    End Sub
End Class








Public Class MiniCloud_Rotate : Inherits TaskParent
    Public mini As New MiniCloud_Basics
    Public histogram As New cv.Mat
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels(3) = "Side view after resize percentage - use Y-Axis slider to rotate image."
        desc = "Create a histogram for the mini point cloud"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        Static ySlider = FindSlider("Rotate pointcloud around Y-axis (degrees)")

        Dim input = src
        mini.Run(input)
        input = mini.dst3
        task.accRadians.Y = ySlider.Value

        Dim cx As Double = 1, sx As Double = 0, cy As Double = 1, sy As Double = 0, cz As Double = 1, sz As Double = 0
        Dim gM(,) As Single = {{cx * 1 + -sx * 0 + 0 * 0, cx * 0 + -sx * cz + 0 * sz, cx * 0 + -sx * -sz + 0 * cz},
                               {sx * 1 + cx * 0 + 0 * 0, sx * 0 + cx * cz + 0 * sz, sx * 0 + cx * -sz + 0 * cz},
                               {0 * 1 + 0 * 0 + 1 * 0, 0 * 0 + 0 * cz + 1 * sz, 0 * 0 + 0 * -sz + 1 * cz}}
        '[cos(a) 0 -sin(a)]
        '[0      1       0]
        '[sin(a) 0   cos(a] rotate the point cloud around the y-axis.
        cy = Math.Cos(task.accRadians.Y * cv.Cv2.PI / 180)
        sy = Math.Sin(task.accRadians.Y * cv.Cv2.PI / 180)
        gM = {{gM(0, 0) * cy + gM(0, 1) * 0 + gM(0, 2) * sy}, {gM(0, 0) * 0 + gM(0, 1) * 1 + gM(0, 2) * 0}, {gM(0, 0) * -sy + gM(0, 1) * 0 + gM(0, 2) * cy},
              {gM(1, 0) * cy + gM(1, 1) * 0 + gM(1, 2) * sy}, {gM(1, 0) * 0 + gM(1, 1) * 1 + gM(1, 2) * 0}, {gM(1, 0) * -sy + gM(1, 1) * 0 + gM(1, 2) * cy},
              {gM(2, 0) * cy + gM(2, 1) * 0 + gM(2, 2) * sy}, {gM(2, 0) * 0 + gM(2, 1) * 1 + gM(2, 2) * 0}, {gM(2, 0) * -sy + gM(2, 1) * 0 + gM(2, 2) * cy}}

        Dim gMat = cv.Mat.FromPixelData(3, 3, cv.MatType.CV_32F, gM)
        Dim gInput = input.Reshape(1, input.Rows * input.Cols)
        Dim gOutput = (gInput * gMat).ToMat
        input = gOutput.Reshape(3, input.Rows)

        Dim split = input.Split()
        Dim mask = split(2).Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
        input.SetTo(0, mask.ConvertScaleAbs(255)) ' remove zero depth pixels with non-zero x and y.

        Dim ranges() = New cv.Rangef() {New cv.Rangef(-task.yRange, task.yRange), New cv.Rangef(0, task.MaxZmeters)}
        cv.Cv2.CalcHist({input}, {1, 2}, New cv.Mat, histogram, 2, {input.Height, input.Width}, ranges)

        dst2(mini.rect) = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        dst3(mini.rect) = input.ConvertScaleAbs(255)
    End Sub
End Class








Public Class MiniCloud_RotateAngle : Inherits TaskParent
    Dim peak As New MiniCloud_Rotate
    Dim mats As New Mat_4to1
    Public plot As New Plot_OverTimeSingle
    Dim resetCheck As System.Windows.Forms.CheckBox
    Public Sub New()
        task.accRadians.Y = -cv.Cv2.PI / 2

        labels(2) = "peak dst2, peak dst3, changed mask, maxvalues history"
        labels(3) = "Blue is maxVal, green is mean * 100"
        desc = "Find a peak value in the side view histograms"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then
            peak.mini.Run(src)
            src = peak.mini.dst3
        End If

        Static ySlider = FindSlider("Rotate pointcloud around Y-axis (degrees)")
        If ySlider.Value + 1 >= ySlider.maximum Then ySlider.Value = ySlider.minimum Else ySlider.Value += 1

        peak.Run(src)
        Dim mm as mmData = GetMinMax(peak.histogram)

        Dim mean = peak.histogram.Mean()(0) * 100
        Dim mask = peak.histogram.Threshold(mean, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        mats.mat(2) = mask

        plot.plotData = New cv.Scalar(mm.maxVal)
        plot.Run(empty)
        dst3 = plot.dst2
        labels(3) = "Histogram maxVal = " + Format(mm.maxVal, fmt1) + " histogram mean = " + Format(mean, fmt1)
        mats.mat(3) = peak.histogram.ConvertScaleAbs(255)

        mats.mat(0) = peak.dst2(peak.mini.rect)
        mats.mat(1) = peak.dst3(peak.mini.rect)
        mats.Run(empty)
        dst2 = mats.dst2
    End Sub
End Class









Public Class MiniCloud_RotateSinglePass : Inherits TaskParent
    Dim peak As New MiniCloud_Rotate
    Public Sub New()
        task.accRadians.Y = -cv.Cv2.PI
        desc = "Same operation as New MiniCloud_RotateAngle but in a single pass."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        Static ySlider = FindSlider("Rotate pointcloud around Y-axis (degrees)")
        peak.mini.Run(src)

        Dim maxHist = Single.MinValue
        Dim bestAngle As Integer
        Dim bestLoc As cv.Point
        Dim mm As mmData
        For i = ySlider.minimum To ySlider.maximum - 1
            peak.Run(peak.mini.dst3)
            ySlider.Value = i
            mm = GetMinMax(peak.histogram)
            If mm.maxVal > maxHist Then
                maxHist = mm.maxVal
                bestAngle = i
                bestLoc = mm.maxLoc
            End If
        Next
        peak.Run(peak.mini.dst3)
        task.accRadians.Y = bestAngle
        dst2 = peak.dst2
        dst3 = peak.dst3

        SetTrueText("Peak concentration in the histogram is at angle " + CStr(bestAngle) + " degrees", 3)
    End Sub
End Class
