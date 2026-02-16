Imports System.Net
Imports System.Runtime.InteropServices
Imports OpenCvSharp
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedCart_Basics : Inherits TaskParent
        Dim redCore As New RedPrep_Core
        Public classCount As Integer
        Public Sub New()
            task.fOptions.ReductionTargetSlider.Value = 50
            OptionParent.findRadio("X Reduction").Checked = True
            desc = "Prepare the grid of point cloud data."
        End Sub
        Public Shared Function countClasses(input As cv.Mat, ByRef count As Integer, colorIndex As Byte,
                                            ByRef label As String) As cv.Mat
            Dim histogram As New cv.Mat
            Dim mm = GetMinMax(input)
            Dim ranges = {New cv.Rangef(mm.minVal, mm.maxVal)}
            cv.Cv2.CalcHist({input}, {0}, task.depthmask, histogram, 1, {255}, ranges)
            Dim histArray(255) As Single
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

            count = 0
            Dim threshold = input.Total * 0.001 ' ignore regions less than 0.1% - 1/10th of 1%
            Dim lutArray As Byte() = New Byte(255) {}
            Dim lutIndex As Integer = 1
            For i = 0 To histArray.Count - 1
                If histArray(i) > threshold Then
                    lutArray(i) = lutIndex
                    lutIndex += 1
                    count += 1
                End If
            Next

            Dim togg As Boolean
            For i = 0 To lutArray.Count - 1
                If lutArray(i) <> 0 Then
                    If togg Then lutArray(i) = colorIndex Else lutArray(i) = 0
                    togg = Not togg
                End If
            Next

            Dim lut As New cv.Mat(1, 256, cv.MatType.CV_8U)
            lut.SetArray(Of Byte)(lutArray)

            label = CStr(count) + " non-zero regions."
            Return lut
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            redCore.Run(emptyMat)
            dst3 = redCore.dst2
            labels(3) = redCore.labels(2)

            Dim lut = RedCart_Basics.countClasses(redCore.dst2, classCount, 255, labels(2))
            dst2 = redCore.dst2.LUT(lut)

            Dim val = redCore.reduced32f.Get(Of Single)(task.clickPoint.Y, task.clickPoint.X)
            SetTrueText("Reduced value = " + Format(val, fmt3), 3)
        End Sub
    End Class





    Public Class RedCart_Debug : Inherits TaskParent
        Dim redCore As New RedPrep_Core
        Public classCount As Integer
        Public Sub New()
            desc = "Identify each region using the debug slider."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redCore.Run(emptyMat)
            dst3 = redCore.dst2
            labels(3) = redCore.labels(2)

            Dim lut = RedCart_Basics.countClasses(redCore.dst2, classCount, 255, labels(2))
            dst2 = redCore.dst2.LUT(lut)
        End Sub
    End Class





    Public Class RedCart_PrepData : Inherits TaskParent
        Dim redCore As New RedPrep_Core
        Public Sub New()
            task.fOptions.ReductionTargetSlider.Value = 50
            desc = "Prepare the grid of point cloud data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redCore.Run(emptyMat)
            dst2 = PaletteBlackZero(redCore.dst2)
            labels(2) = redCore.labels(2)

            Dim val = redCore.reduced32f.Get(Of Single)(task.clickPoint.Y, task.clickPoint.X)
            SetTrueText("Depth = " + Format(val, fmt3), 3)
        End Sub
    End Class




    Public Class RedCart_PrepStableY : Inherits TaskParent
        Public redCore As New RedPrep_Core
        Dim plotCore As New Plot_HistogramCoreRange
        Public lpList As New List(Of lpData)
        Public Sub New()
            plotCore.redCore = redCore
            task.gOptions.HistBinBar.Value = task.gOptions.HistBinBar.Maximum
            task.fOptions.ReductionTargetSlider.Value = 50
            desc = "Prep the Horizontal regions in the reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redCore.reductionName = "Y Reduction"
            redCore.Run(src)
            dst2 = redCore.dst3
            labels(2) = redCore.labels(2)

            Dim mm = GetMinMax(redCore.reduced32f)

            If task.heartBeat Then
                plotCore.Run(redCore.reduced32f)
                labels(3) = plotCore.labels(2)
            End If

            labels(3) = "Values range from " + Format(plotCore.minRange, fmt0) +
                        " to " + Format(plotCore.maxRange, fmt0)
            dst3 = plotCore.dst3

            Dim val = redCore.reduced32f.Get(Of Single)(task.clickPoint.Y, task.clickPoint.X)
            labels(3) = "Overall min/max " + CStr(mm.minVal) + "/" + CStr(mm.maxVal) + " " +
                        "Depth = " + Format(val, fmt0)
        End Sub
    End Class





    Public Class RedCart_Validate : Inherits TaskParent
        Dim redCart As New RedCart_PrepY
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            task.gOptions.displayDst1.Checked = True
            desc = "Identify the different regions in the RedCart_PrepX/Y using the debugslider"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redCart.Run(src)
            dst2 = redCart.dst2
            labels(2) = redCart.labels(2)

            Dim mm = GetMinMax(redCart.redCore.reduced32f)
            Dim ranges = {New cv.Rangef(mm.minVal, mm.maxVal)}
            Dim histogram As New cv.Mat
            Dim histBins As Integer = 500
            cv.Cv2.CalcHist({redCart.redCore.reduced32f}, {0}, task.depthmask, histogram, 1, {histBins}, ranges)
            Dim histArray(histogram.Rows - 1) As Single
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)
            Dim incr = mm.range / histBins

            dst1.SetTo(0)
            For i = 0 To histArray.Count - 1
                Dim tmp = redCart.redCore.reduced32f.InRange(mm.minVal + incr * i, mm.minVal + incr * (i + 1))
                dst1.SetTo(i + 1, tmp)
            Next
            dst1.SetTo(0, task.noDepthMask)

            dst3 = PaletteBlackZero(dst1)
        End Sub
    End Class




    Public Class RedCart_PrepXY : Inherits TaskParent
        Public redCore As New RedPrep_Core
        Public classCount As Integer
        Public Sub New()
            task.fOptions.ReductionTargetSlider.Value = 50
            desc = "Prep the XY regions in the reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redCore.reductionName = "XY Reduction"
            redCore.Run(src)
            dst2 = redCore.dst3

            Dim lut = RedCart_Basics.countClasses(redCore.dst2, classCount, 255, labels(2))
            dst2 = redCore.dst2.LUT(lut)
        End Sub
    End Class



    Public Class RedCart_CheckerBoardWall : Inherits TaskParent
        Public redCore As New RedPrep_Core
        Public classCount As Integer
        Dim edges As New Edge_Basics
        Public Sub New()
            task.fOptions.ReductionTargetSlider.Value = 50
            desc = "Use this algorithm to build a checkerboard when pointing at a wall."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            ' redCore.reductionName = "XY Reduction" ' default
            redCore.Run(src)

            Dim lut = RedCart_Basics.countClasses(redCore.dst2, classCount, 255, labels(2))
            dst2 = redCore.dst2.LUT(lut)

            edges.Run(dst2)
            dst3 = edges.dst2
        End Sub
    End Class




    Public Class RedCart_TriangleDots : Inherits TaskParent
        Dim checkers As New RedPrep_Core
        Public Sub New()
            task.fOptions.ReductionTargetSlider.Value = 50
            desc = "Find any "
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            checkers.Run(src)
            Dim kernel = Cv2.GetStructuringElement(MorphShapes.Rect, New Size(3, 3))

            cv.Cv2.Erode(checkers.dst2, dst2, kernel)

            Dim mask As New Mat(dst2.Rows + 2, dst2.Cols + 2, MatType.CV_8UC1)
            mask.SetTo(0)

            ' Cv2.FloodFill(dst2, mask, seedPoint, New Scalar(255))


        End Sub
    End Class





    Public Class RedCart_CPP : Inherits TaskParent
        Implements IDisposable
        Dim prep As New RedPrep_Core
        Public Sub New()
            task.fOptions.ReductionTargetSlider.Value = 50
            cPtr = RedCart_CPP_Open()
            desc = "Hit the locations where floodfill slips up by placeing a dot in the intersection."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)
            dst2 = prep.dst2
            labels(2) = prep.labels(2)

            Dim cppData(dst2.Total - 1) As Byte
            Marshal.Copy(dst2.Data, cppData, 0, cppData.Length - 1)
            Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
            Dim imagePtr = RedCart_CPP_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), dst2.Rows, dst2.Cols)
            handleSrc.Free()

            dst3 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
            dst3.SetTo(255, task.noDepthMask)
            dst2.SetTo(0, dst3)
        End Sub
        Protected Overrides Sub Finalize()
            RedCart_CPP_Close(cPtr)
        End Sub
    End Class




    Public Class RedCart_PrepXOld : Inherits TaskParent
        Public redCore As New RedPrep_Core
        Public classCount As Integer
        Public lut As cv.Mat
        Public Sub New()
            task.fOptions.ReductionTargetSlider.Value = 50
            desc = "Prep the vertical regions in the reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redCore.reductionName = "X Reduction"
            redCore.Run(src)
            dst2 = redCore.dst3

            Dim lut = RedCart_Basics.countClasses(redCore.dst2, classCount, 255, labels(2))
            dst2 = redCore.dst2.LUT(lut)
        End Sub
    End Class



    Public Class RedCart_PrepY : Inherits TaskParent
        Public redCore As New RedPrep_Core
        Public classCount As Integer
        Public Sub New()
            task.fOptions.ReductionTargetSlider.Value = 50
            desc = "Prep the horizontal regions in the reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redCore.reductionName = "Y Reduction"
            redCore.Run(src)
            dst2 = redCore.dst3

            Dim lut = RedCart_Basics.countClasses(redCore.dst2, classCount, 255, labels(2))
            dst2 = redCore.dst2.LUT(lut)
        End Sub
    End Class




    Public Class RedCart_PrepXYAlt : Inherits TaskParent
        Dim redX As New RedCart_PrepX
        Dim redY As New RedCart_PrepY
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)

            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Add the output of PrepX and PrepY.  Point camera at a wall for interesting results."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redX.Run(src)
            dst1 = redX.dst2
            dst1.SetTo(0, task.noDepthMask)
            labels(1) = CStr(redX.classCount) + " regions were found"

            redY.Run(src)
            dst3 = redY.dst2
            dst3.SetTo(0, task.noDepthMask)
            labels(3) = CStr(redY.classCount) + " regions were found"

            dst2 = dst1 Or dst3
            labels(2) = CStr(redX.classCount + redY.classCount) + " regions were found"
        End Sub
    End Class





    Public Class RedCart_PrepX : Inherits TaskParent
        Public redCore As New RedPrep_Core
        Public classCount As Integer
        Public Sub New()
            task.fOptions.ReductionTargetSlider.Value = 50
            desc = "Prep the vertical regions in the reduced depth data."
        End Sub

        Public Overrides Sub RunAlg(src As cv.Mat)
            redCore.reductionName = "X Reduction"
            redCore.Run(src)
            dst2 = redCore.dst3

            Dim lut = RedCart_Basics.countClasses(redCore.dst2, classCount, 255, labels(2))
            dst2 = redCore.dst2.LUT(lut)
        End Sub
    End Class
End Namespace
