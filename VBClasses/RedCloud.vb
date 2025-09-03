Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedCloud_Basics : Inherits TaskParent
    Dim prep As New RedPrep_Basics
    Dim stats As New RedCell_Basics
    Public Sub New()
        OptionParent.findRadio("XY Reduction").Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Run the reduced pointcloud output through the RedColor_CPP algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst2 = runRedC(prep.dst2, labels(2))
        stats.Run(src)
        dst1 = stats.dst3
        SetTrueText(stats.strOut, 3)
    End Sub
End Class





Public Class RedCloud_BasicsXY : Inherits TaskParent
    Dim prep As New RedPrep_Depth
    Dim redMask As New RedMask_Basics
    Dim cellGen As New RedCell_Generate
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Run the reduced pointcloud output through the RedColor_CPP algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        redMask.Run(prep.dst2)

        If redMask.mdList.Count = 0 Then Exit Sub ' no data to process.
        cellGen.mdList = redMask.mdList
        cellGen.Run(redMask.dst2)

        dst2 = cellGen.dst2

        labels(2) = cellGen.labels(2)
    End Sub
End Class







Public Class RedCloud_World : Inherits TaskParent
    Dim world As New Depth_World
    Dim prep As New RedPrep_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(3) = "Generated pointcloud"
        desc = "Display the output of a generated pointcloud as RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        world.Run(src)

        prep.Run(world.dst2)

        dst2 = runRedC(prep.dst2, labels(2))
    End Sub
End Class









Public Class RedCloud_Mats : Inherits TaskParent
    Dim mats As New Mat_4Click
    Public Sub New()
        desc = "Simpler transforms for the point cloud using CalcHist instead of reduction."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim histogram As New cv.Mat

        For i = 0 To 2
            Select Case i
                Case 0 ' X Reduction
                    dst0 = task.pcSplit(0)
                Case 1 ' Y Reduction
                    dst0 = task.pcSplit(1)
                Case 2 ' Z Reduction
                    dst0 = task.pcSplit(2)
            End Select

            Dim mm = GetMinMax(dst0)
            Dim ranges = New cv.Rangef() {New cv.Rangef(mm.minVal, mm.maxVal)}
            cv.Cv2.CalcHist({dst0}, {0}, task.depthMask, histogram, 1, {task.histogramBins}, ranges)

            Dim histArray(histogram.Total - 1) As Single
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

            For j = 0 To histArray.Count - 1
                histArray(j) = j
            Next

            histogram = cv.Mat.FromPixelData(histogram.Rows, 1, cv.MatType.CV_32F, histArray)
            cv.Cv2.CalcBackProject({dst0}, {0}, histogram, dst0, ranges)
            dst0.ConvertTo(dst1, cv.MatType.CV_8U)
            mats.mat(i) = ShowPalette(dst1)
            mats.mat(i).SetTo(0, task.noDepthMask)
        Next

        mats.Run(emptyMat)
        dst2 = mats.dst2
    End Sub
End Class







Public Class RedCloud_PrepOutline : Inherits TaskParent
    Public prep As New RedPrep_Depth
    Public Sub New()
        desc = "Remove corners of RedCloud cells in the prep data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst2 = prep.dst2.Clone

        Dim val1 As Byte, val2 As Byte
        For y = 0 To dst2.Height - 2
            For x = 0 To dst2.Width - 2
                Dim zipData As Boolean = False

                val1 = dst2.Get(Of Byte)(y, x)
                val2 = dst2.Get(Of Byte)(y, x + 1)
                If val1 <> 0 And val2 <> 0 Then If val1 <> val2 Then zipData = True

                val2 = dst2.Get(Of Byte)(y + 1, x)
                If val1 <> 0 And val2 <> 0 Then If val1 <> val2 Then zipData = True

                If zipData Then
                    dst2.Set(Of Byte)(y, x, 0)
                    dst2.Set(Of Byte)(y, x + 1, 0)
                    dst2.Set(Of Byte)(y + 1, x, 0)
                    dst2.Set(Of Byte)(y + 1, x + 1, 0)
                End If
            Next
        Next

        dst3 = dst2.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
    End Sub
End Class








Public Class RedCloud_Contours : Inherits TaskParent
    Dim prep As New RedPrep_Depth
    Public Sub New()
        If task.contours Is Nothing Then task.contours = New Contour_Basics_List
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Run the reduced pointcloud output through the RedColor_CPP algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst3 = prep.dst3

        dst2 = task.contours.dst2
        labels(2) = task.contours.labels(2)
    End Sub
End Class






Public Class RedCloud_XYZ : Inherits TaskParent
    Dim prep As New RedPrep_Basics
    Public redMask As New RedMask_Basics
    Dim rcMask As cv.Mat
    Public Sub New()
        OptionParent.findRadio("XYZ Reduction").Checked = True
        rcMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Run the reduced pointcloud output through the RedColor_CPP algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)

        dst2 = runRedC(prep.dst2, labels(2))

        If task.heartBeat Then strOut = ""
        For i = 0 To task.redC.rcList.Count - 1
            Dim rc = task.redC.rcList(i)
            rcMask.SetTo(0)
            rcMask(rc.rect).SetTo(255, rc.mask)
            rc.mdList = New List(Of maskData)
            For Each md In redMask.mdList
                Dim index = rcMask.Get(Of Byte)(md.maxDist.Y, md.maxDist.X)
                If index > 0 Then rc.mdList.Add(md)
            Next
            If rc.mdList.Count > 0 Then
                For j = 0 To rc.mdList.Count - 1
                    Dim md = rc.mdList(j)
                    rcMask(md.rect) = rcMask(md.rect) And md.mask
                    md.mask = rcMask(md.rect).Clone
                    rc.mdList(j) = md
                Next
                task.redC.rcList(i) = rc
            End If
        Next

        SetTrueText(strOut, 3)
    End Sub
End Class








Public Class RedCloud_YZ : Inherits TaskParent
    Dim prep As New RedPrep_Basics
    Dim stats As New RedCell_Basics
    Public Sub New()
        OptionParent.findRadio("YZ Reduction").Checked = True
        labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Build YZ RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)

        dst2 = runRedC(prep.dst2, labels(2))

        stats.Run(src)
        dst1 = stats.dst3
        SetTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedCloud_XZ : Inherits TaskParent
    Dim prep As New RedPrep_Basics
    Dim stats As New RedCell_Basics
    Public Sub New()
        OptionParent.findRadio("XZ Reduction").Checked = True
        labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Build XZ RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)

        dst2 = runRedC(prep.dst2, labels(2))
        stats.Run(src)
        dst1 = stats.dst3
        SetTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedCloud_X : Inherits TaskParent
    Dim prep As New RedPrep_Basics
    Dim stats As New RedCell_Basics
    Public Sub New()
        OptionParent.findRadio("X Reduction").Checked = True
        labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Build X RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)

        dst2 = runRedC(prep.dst2, labels(2))
        stats.Run(src)
        dst1 = stats.dst3
        SetTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedCloud_Y : Inherits TaskParent
    Dim prep As New RedPrep_Basics
    Dim stats As New RedCell_Basics
    Public Sub New()
        OptionParent.findRadio("Y Reduction").Checked = True
        labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Build Y RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)

        dst2 = runRedC(prep.dst2, labels(2))
        stats.Run(src)
        dst1 = stats.dst3
        SetTrueText(stats.strOut, 3)
    End Sub
End Class





Public Class RedCloud_Z : Inherits TaskParent
    Dim prep As New RedPrep_Basics
    Dim stats As New RedCell_Basics
    Public Sub New()
        OptionParent.findRadio("Z Reduction").Checked = True
        labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Build Z RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)

        dst2 = runRedC(prep.dst2, labels(2))
        stats.Run(src)
        dst1 = stats.dst3
        SetTrueText(stats.strOut, 3)
    End Sub
End Class





Public Class RedCloud_XY : Inherits TaskParent
    Dim prep As New RedPrep_Basics
    Dim stats As New RedCell_Basics
    Public Sub New()
        OptionParent.findRadio("XY Reduction").Checked = True
        labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Build XY RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)

        dst2 = runRedC(prep.dst2, labels(2))
        stats.Run(src)
        dst1 = stats.dst3
        SetTrueText(stats.strOut, 3)
    End Sub
End Class





