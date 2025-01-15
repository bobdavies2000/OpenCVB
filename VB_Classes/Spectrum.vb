Imports cv = OpenCvSharp
Public Class Spectrum_Basics : Inherits TaskParent
    Dim dSpec As New Spectrum_Z
    Dim gSpec As New Spectrum_Gray
    Public options As New Options_Spectrum
    Public Sub New()
        desc = "Given a RedCloud cell, create a spectrum that contains the ranges of the depth and color."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))
        dSpec.Run(src)
        gSpec.Run(src)

        If task.heartBeat And task.rc.index > 0 Then
            strOut = dSpec.strOut + vbCrLf + vbCrLf
            strOut += gSpec.strOut
        End If

        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class Spectrum_X : Inherits TaskParent
    Public options As New Options_Spectrum
    Public Sub New()
        desc = "Given a RedCloud cell, create a spectrum that contains the depth ranges."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        If standaloneTest() Then dst2 = runRedC(src, labels(2))

        If task.heartBeat And task.rc.index > 0 Then
            Dim ranges = options.buildDepthRanges(task.pcSplit(0)(task.rc.rect).Clone, " pointcloud X ")
            strOut = options.strOut
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class Spectrum_Y : Inherits TaskParent
    Public options As New Options_Spectrum
    Public Sub New()
        desc = "Given a RedCloud cell, create a spectrum that contains the depth ranges."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        If standaloneTest() Then dst2 = runRedC(src, labels(2))

        If task.heartBeat And task.rc.index > 0 Then
            Dim ranges = options.buildDepthRanges(task.pcSplit(1)(task.rc.rect).Clone, " pointcloud Y ")
            strOut = options.strOut
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class Spectrum_Z : Inherits TaskParent
    Public options As New Options_Spectrum
    Public Sub New()
        desc = "Given a RedCloud cell, create a spectrum that contains the depth ranges."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()
        If standaloneTest() Then dst2 = runRedC(src, labels(2))

        If task.heartBeat And task.rc.index > 0 Then
            Dim ranges = options.buildDepthRanges(task.pcSplit(2)(task.rc.rect).Clone, " pointcloud Z ")
            strOut = options.strOut
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class









Public Class Spectrum_Cloud : Inherits TaskParent
    Public options As New Options_Spectrum
    Dim specX As New Spectrum_X
    Dim specY As New Spectrum_Y
    Dim specZ As New Spectrum_Z
    Public Sub New()
        desc = "Given a RedCloud cell, create a spectrum that contains the ranges for X, Y, and Z in the point cloud."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        If standaloneTest() Then dst2 = runRedC(src, labels(2))

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








Public Class Spectrum_GrayAndCloud : Inherits TaskParent
    Dim options As New Options_Spectrum
    Dim gSpec As New Spectrum_Gray
    Dim sCloud As New Spectrum_Cloud
    Public Sub New()
        desc = "Given a RedCloud cell, create a spectrum that contains the ranges for X, Y, and Z in the point cloud."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        If standaloneTest() Then dst2 = runRedC(src, labels(2))

        If task.heartBeat Then
            sCloud.Run(src)
            strOut = sCloud.strOut + vbCrLf
            gSpec.Run(src)
            strOut += gSpec.strOut
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class Spectrum_RGB : Inherits TaskParent
    Dim options As New Options_Spectrum
    Dim gSpec As New Spectrum_Gray
    Public Sub New()
        desc = "Create a spectrum of the RGB values for a given RedCloud cell."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        If standaloneTest() Then dst2 = runRedC(src, labels(2))

        Dim split = src.Split()
        gSpec.typeSpec = " blue "
        gSpec.Run(split(0))
        If task.heartBeat Then strOut = gSpec.strOut + vbCrLf

        gSpec.typeSpec = " green "
        gSpec.Run(split(1))
        If task.heartBeat Then strOut += gSpec.strOut + vbCrLf

        gSpec.typeSpec = " red "
        gSpec.Run(split(2))
        If task.heartBeat Then strOut += gSpec.strOut

        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class Spectrum_CellZoom : Inherits TaskParent
    Dim proportion As New Resize_Proportional
    Dim breakdown As New Spectrum_Breakdown
    Public Sub New()
        labels = {"", "Cell trimming information", "", "White is after trimming, gray is before trim, black is outside the cell mask."}
        If standaloneTest() Then task.gOptions.setDisplay1()
        desc = "Zoom in on the selected RedCloud cell before and after Spectrum filtering."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        breakdown.options.RunOpt()

        dst2 = runRedC(src, labels(2))

        If task.heartBeat Then
            breakdown.Run(src)
            SetTrueText(breakdown.strOut, 1)

            proportion.Run(breakdown.dst3)
            dst3 = proportion.dst2
            strOut = breakdown.options.strOut
        End If

        SetTrueText(strOut, 1)
    End Sub
End Class








Public Class Spectrum_Breakdown : Inherits TaskParent
    Public options As New Options_Spectrum
    Public buildMaskOnly As Boolean
    Dim proportion As New Resize_Proportional
    Public Sub New()
        desc = "Breakdown a cell if possible."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            options.RunOpt()
            dst2 = runRedC(src, labels(2))
        End If

        Dim rc = task.rc
        Dim ranges As List(Of rangeData), input As cv.Mat
        If rc.depthPixels / rc.pixels < 0.5 Then
            input = New cv.Mat(rc.mask.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            src(rc.rect).CopyTo(input, rc.mask)
            input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            input = New cv.Mat(rc.mask.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
            task.pcSplit(2)(rc.rect).CopyTo(input, rc.mask)
        End If
        ranges = options.buildColorRanges(input, "GrayScale")

        If ranges.Count = 0 Then Exit Sub ' all the counts were too small - rare but it happens.

        Dim maxRange As rangeData, maxPixels As Integer
        For Each r In ranges
            If r.pixels > maxPixels Then
                maxPixels = r.pixels
                maxRange = r
            End If
        Next

        Dim rangeClip As New cv.Mat(input.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        If input.Type = cv.MatType.CV_8U Then
            rangeClip = input.InRange(maxRange.start, maxRange.ending)
            rangeClip = rangeClip.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        Else
            rangeClip = New cv.Mat(rc.mask.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
            input.CopyTo(rangeClip, rc.mask)

            rangeClip = rangeClip.InRange(maxRange.start / 100, maxRange.ending / 100)
            rangeClip = rangeClip.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        End If

        If buildMaskOnly = False Then
            dst3 = rc.mask.Threshold(0, 128, cv.ThresholdTypes.Binary)
            dst3.SetTo(255, rangeClip)
        End If

        If standaloneTest() Then
            proportion.Run(dst3)
            dst3 = proportion.dst2
        End If

        rc.mask = rc.mask.Threshold(0, 255, cv.ThresholdTypes.Binary)
        task.rc = rc
    End Sub
End Class








Public Class Spectrum_RedCloud : Inherits TaskParent
    Dim breakdown As New Spectrum_Breakdown
    Public Sub New()
        desc = "Breakdown each cell in rcList."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        breakdown.options.RunOpt()
        dst2 = runRedC(src, labels(2))

        dst3.SetTo(0)
        For i = 0 To task.rcList.Count - 1
            task.rc = task.rcList(i)
            breakdown.Run(src)
            task.rcList(i) = task.rc
            dst3(task.rc.rect).SetTo(task.rc.colorTrack, task.rc.mask)
        Next
        breakdown.Run(src)
    End Sub
End Class








Public Class Spectrum_Mask : Inherits TaskParent
    Dim gSpec As New Spectrum_Gray
    Public Sub New()
        If standalone Then strOut = "Select a cell to see its depth spectrum"
        If standalone Then task.gOptions.setDisplay1()
        desc = "Create a mask from the Spectrum ranges"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        gSpec.Run(src)
        dst1 = gSpec.dst2
        labels(2) = gSpec.labels(2)
        If task.heartBeat Then strOut = gSpec.strOut
    End Sub
End Class







Public Class Spectrum_Gray : Inherits TaskParent
    Dim options As New Options_Spectrum
    Public typeSpec As String = "GrayScale"
    Public Sub New()
        desc = "Given a RedCloud cell, create a spectrum that contains the color ranges."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        If standaloneTest() Then dst2 = runRedC(src, labels(2))

        Dim input = src(task.rc.rect)
        If input.Type <> cv.MatType.CV_8U Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim ranges = options.buildColorRanges(input, typeSpec)
        strOut = options.strOut
        SetTrueText(strOut, 3)
    End Sub
End Class