Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp








Public Class Spectrum_X : Inherits TaskParent
    Public options As New Options_Spectrum
    Dim redC As New RedColor_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Given a RedCloud cell, create a spectrum that contains the depth ranges."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If standaloneTest() Then
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            SetTrueText(redC.strOut, 1)
        End If

        If task.heartBeat And task.rcD.mapID > 0 Then
            Dim ranges = options.buildDepthRanges(task.pcSplit(0)(task.rcD.rect).Clone, " pointcloud X ")
            strOut = options.strOut
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class Spectrum_Y : Inherits TaskParent
    Public options As New Options_Spectrum
    Dim redC As New RedColor_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Given a RedCloud cell, create a spectrum that contains the depth ranges."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If standaloneTest() Then
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            SetTrueText(redC.strOut, 1)
        End If

        If task.heartBeat And task.rcD.mapID > 0 Then
            Dim ranges = options.buildDepthRanges(task.pcSplit(1)(task.rcD.rect).Clone, " pointcloud Y ")
            strOut = options.strOut
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class Spectrum_Z : Inherits TaskParent
    Public options As New Options_Spectrum
    Dim redC As New RedColor_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Given a RedCloud cell, create a spectrum that contains the depth ranges."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        If standaloneTest() Then
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            SetTrueText(redC.strOut, 1)
        End If

        If task.rcD IsNot Nothing Then
            If task.heartBeat And task.rcD.mapID > 0 Then
                Dim ranges = options.buildDepthRanges(task.pcSplit(2)(task.rcD.rect).Clone, " pointcloud Z ")
                strOut = options.strOut
            End If
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class









Public Class Spectrum_Cloud : Inherits TaskParent
    Public options As New Options_Spectrum
    Dim specX As New Spectrum_X
    Dim specY As New Spectrum_Y
    Dim specZ As New Spectrum_Z
    Dim redC As New RedColor_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Given a RedCloud cell, create a spectrum that contains the ranges for X, Y, and Z in the cv.Point cloud."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If standaloneTest() Then
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            SetTrueText(redC.strOut, 1)
        End If

        If task.heartBeat Then
            specX.Run(src)
            strOut = specX.strOut + vbCrLf
            specY.Run(src)
            strOut += specY.strOut + vbCrLf
            specZ.Run(src)
            strOut += specZ.strOut
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class








Public Class XR_Spectrum_GrayAndCloud : Inherits TaskParent
    Dim options As New Options_Spectrum
    Dim gSpec As New Spectrum_Gray
    Dim sCloud As New Spectrum_Cloud
    Dim redC As New RedColor_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Given a RedCloud cell, create a spectrum that contains the ranges for X, Y, and Z in the cv.Point cloud."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If standaloneTest() Then
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            SetTrueText(redC.strOut, 1)
        End If

        If task.heartBeat Then
            sCloud.Run(src)
            strOut = sCloud.strOut + vbCrLf
            gSpec.Run(src)
            strOut += gSpec.strOut
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class XR_Spectrum_RGB : Inherits TaskParent
    Dim options As New Options_Spectrum
    Dim gSpec As New Spectrum_Gray
    Dim redC As New RedColor_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Create a spectrum of the RGB values for a given RedCloud cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If standaloneTest() Then
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            SetTrueText(redC.strOut, 1)
        End If

        Dim splitMats() As Mat = Split(src)
        gSpec.typeSpec = " blue "
        gSpec.Run(splitMats(0))
        If task.heartBeat Then strOut = gSpec.strOut + vbCrLf

        gSpec.typeSpec = " green "
        gSpec.Run(splitMats(1))
        If task.heartBeat Then strOut += gSpec.strOut + vbCrLf

        gSpec.typeSpec = " red "
        gSpec.Run(splitMats(2))
        If task.heartBeat Then strOut += gSpec.strOut

        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class XR_Spectrum_CellZoom : Inherits TaskParent
    Dim proportion As New Resize_Proportional
    Dim breakdown As New Spectrum_Breakdown
    Dim redC As New RedCloud_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "Cell trimming information", "", "White is after trimming, gray is before trim, black is outside the cell mask."}
        If standaloneTest() Then task.gOptions.displayDst1.Checked = True
        desc = "Zoom in on the selected RedCloud cell before and after Spectrum filtering."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        breakdown.options.Run()

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        If task.heartBeat Then
            breakdown.Run(src)
            SetTrueText(breakdown.strOut, 1)

            proportion.Run(breakdown.dst3)
            dst3 = proportion.dst2
            strOut = breakdown.options.strOut
        End If

        SetTrueText(redC.strOut, 1)
    End Sub
End Class








Public Class Spectrum_Breakdown : Inherits TaskParent
    Public options As New Options_Spectrum
    Public buildMaskOnly As Boolean
    Dim proportion As New Resize_Proportional
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Breakdown a cell if possible."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        If standaloneTest() Then
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
        End If
        Exit Sub
        Dim rc = task.rcD
        If rc Is Nothing Then Exit Sub
        Dim ranges As List(Of rangeData), input As Mat
        Dim depthPixels = CountNonZero(task.depthmask(rc.rect))
        If depthPixels / rc.pixels < 0.5 Then
            input = New Mat(rc.mask.Size(), MatType.CV_8U, Scalar.All(0))
            src(rc.rect).CopyTo(input, rc.mask)
            CvtColor(input, input, ColorConversionCodes.BGR2GRAY)
        Else
            input = New Mat(rc.mask.Size(), MatType.CV_32F, Scalar.All(0))
            task.pcSplit(2)(rc.rect).CopyTo(input, rc.mask)
        End If
        ranges = options.buildColorRanges(input, "GrayScale")

        If ranges.Count = 0 Then Exit Sub ' all the counts were too small - rare but it happens.

        Dim maxRange As rangeData = Nothing, maxPixels As Integer
        For Each r In ranges
            If r.pixels > maxPixels Then
                maxPixels = r.pixels
                maxRange = r
            End If
        Next

        Dim rangeClip As New Mat(input.Size(), MatType.CV_8U, Scalar.All(0))
        If input.Type = MatType.CV_8U Then
            InRange(input, maxRange.start, maxRange.ending, rangeClip)
            Threshold(rangeClip, rangeClip, 0, 255, ThresholdTypes.Binary)
            ConvertScaleAbs(rangeClip, rangeClip)
        Else
            rangeClip = New Mat(rc.mask.Size(), MatType.CV_32F, Scalar.All(0))
            input.CopyTo(rangeClip, rc.mask)

            InRange(rangeClip, maxRange.start / 100, maxRange.ending / 100, rangeClip)
            Dim _thr2 As New Mat
            Threshold(rangeClip, rangeClip, 0, 255, ThresholdTypes.Binary)
            ConvertScaleAbs(rangeClip, rangeClip)
        End If

        If buildMaskOnly = False Then
            Threshold(rc.mask, dst3, 0, 128, ThresholdTypes.Binary)
            dst3.SetTo(255, rangeClip)
        End If

        If standaloneTest() Then
            proportion.Run(dst3)
            dst3 = proportion.dst2
        End If

        Threshold(rc.mask, rc.mask, 0, 255, ThresholdTypes.Binary)
        task.rcD = rc
    End Sub
End Class








Public Class XR_Spectrum_RedCloud : Inherits TaskParent
    Dim breakdown As New Spectrum_Breakdown
    Dim redC As New RedColor_Basics
    Public Sub New()
        desc = "Breakdown each cell in rclist."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        breakdown.options.Run()

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        For Each task.rcD In redC.rcList
            breakdown.Run(src)
            dst3(task.rcD.rect).SetTo(task.rcD.color, task.rcD.mask)
        Next
    End Sub
End Class








Public Class XR_Spectrum_Mask : Inherits TaskParent
    Dim gSpec As New Spectrum_Gray
    Public Sub New()
        If standalone Then strOut = "Select a cell to see its depth spectrum"
        desc = "Create a mask from the Spectrum ranges"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        gSpec.Run(src)
        dst2 = gSpec.dst2
        labels(2) = gSpec.labels(2)
        If task.heartBeat Then strOut = gSpec.strOut
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class Spectrum_Gray : Inherits TaskParent
    Dim options As New Options_Spectrum
    Public typeSpec As String = "GrayScale"
    Dim redC As New RedColor_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Given a RedCloud cell, create a spectrum that contains the color ranges."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        SetTrueText(redC.strOut, 1)

        Dim input = src(task.rcD.rect)
        If input.Type <> MatType.CV_8U Then CvtColor(input, input, ColorConversionCodes.BGR2GRAY)
        Dim ranges = options.buildColorRanges(input, typeSpec)
        strOut = options.strOut
        SetTrueText(strOut, 3)
    End Sub
End Class
