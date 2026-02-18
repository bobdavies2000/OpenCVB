Imports System.Runtime.InteropServices
Imports OpenCvSharp
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedCart_Basics : Inherits TaskParent
        Public prepData As New RedPrep_Core
        Dim redC As New RedCloud_Basics
        Public Sub New()
            labels(3) = "Use debug slider to select region to display."
            OptionParent.findRadio("X Reduction").Checked = True
            desc = "Run RedCloud on the output of RedPrep_Core"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prepData.Run(emptyMat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
        End Sub
    End Class





    Public Class RedCart_Basics1 : Inherits TaskParent
        Public prepData As New RedPrep_Core
        Public lut As New cv.Mat
        Public lutList As New List(Of Byte)
        Public Sub New()
            task.gOptions.DebugSlider.Value = 1
            labels(3) = "Use debug slider to select region to display."
            OptionParent.findRadio("X Reduction").Checked = True
            desc = "Prepare the grid of point cloud data."
        End Sub
        Public Shared Function countClasses(input As cv.Mat, ByRef label As String) As cv.Mat
            Dim histogram As New cv.Mat
            Dim mm = GetMinMax(input)
            Dim ranges = {New cv.Rangef(mm.minVal, mm.maxVal)}
            cv.Cv2.CalcHist({input}, {0}, task.depthmask, histogram, 1, {255}, ranges)
            Dim histArray(255) As Single
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

            Dim sizeThreshold = input.Total * 0.001 ' ignore regions less than 0.1% - 1/10th of 1%
            Dim lutArray(255) As Byte
            Dim regionList As New List(Of Integer)
            For i = 1 To histArray.Count - 1
                If histArray(i) > sizeThreshold Then
                    regionList.Add(i)
                    lutArray(i) = regionList.Count
                End If
            Next

            Dim lut As New cv.Mat(1, 256, cv.MatType.CV_8U)
            lut.SetArray(Of Byte)(lutArray)

            label = CStr(regionList.Count) + " non-zero regions."
            Return lut
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            prepData.Run(emptyMat)
            labels(2) = prepData.labels(2)

            lut = countClasses(prepData.dst2.Clone, labels(2))
            lutList.Clear()
            For i = 0 To lut.Cols - 1
                Dim val = lut.Get(Of Byte)(0, i)
                If val > 0 Then lutList.Add(val)
            Next
            dst1 = prepData.dst2.LUT(lut)
            dst2 = PaletteBlackZero(dst1)

            If standalone Then
                Dim index = Math.Abs(task.gOptions.DebugSlider.Value)
                If index < lutList.Count Then
                    dst3 = dst1.InRange(index, index)
                End If
            End If
        End Sub
    End Class





    Public Class RedCart_Debug : Inherits TaskParent
        Dim redCart As New RedCart_Basics1
        Public classCount As Integer
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Identify each region using the debug slider."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redCart.Run(emptyMat)
            dst3 = redCart.dst2
            labels(3) = redCart.labels(2)
            strOut = ""

            For i = 1 To redCart.lutList.Count - 1
                dst2 = redCart.dst1.InRange(i, i)
                Dim mean = redCart.prepData.reduced32s.Mean(dst2)
                strOut += "Mean of selected region " + CStr(i) + " = " + Format(mean(0), fmt0) + "  "
                If i Mod 2 = 0 Then strOut += vbCrLf
            Next

            dst2 = (redCart.prepData.reduced32s - -1000) * 255 / (2000)
            dst2.ConvertTo(dst2, cv.MatType.CV_8U)
            dst2.SetTo(0, task.noDepthMask)


            labels(1) = CStr(redCart.lutList.Count) + " non-zero regions."
            SetTrueText(strOut, 1)
        End Sub
    End Class




    Public Class RedCart_PrepXY : Inherits TaskParent
        Public redCart As New RedCart_Basics
        Public Sub New()
            OptionParent.findRadio("XY Reduction").Checked = True
            desc = "Prep the XY regions in the reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redCart.Run(src)
            dst2 = PaletteBlackZero(redCart.dst1)
            labels(2) = redCart.labels(2)
        End Sub
    End Class




    Public Class RedCart_PrepData : Inherits TaskParent
        Dim prepData As New RedPrep_Core
        Public Sub New()
            desc = "Prepare the grid of point cloud data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prepData.Run(emptyMat)
            dst2 = PaletteBlackZero(prepData.dst2)
            labels(2) = prepData.labels(2)

            Dim val = prepData.reduced32f.Get(Of Single)(task.clickPoint.Y, task.clickPoint.X)
            SetTrueText("Depth = " + Format(val, fmt3), 3)
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

            Dim mm = GetMinMax(redCart.prepData.reduced32f)
            Dim ranges = {New cv.Rangef(mm.minVal, mm.maxVal)}
            Dim histogram As New cv.Mat
            Dim histBins As Integer = 500
            cv.Cv2.CalcHist({redCart.prepData.reduced32f}, {0}, task.depthmask, histogram, 1, {histBins}, ranges)
            Dim histArray(histogram.Rows - 1) As Single
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)
            Dim incr = mm.range / histBins

            dst1.SetTo(0)
            For i = 0 To histArray.Count - 1
                Dim tmp = redCart.prepData.reduced32f.InRange(mm.minVal + incr * i, mm.minVal + incr * (i + 1))
                dst1.SetTo(i + 1, tmp)
            Next
            dst1.SetTo(0, task.noDepthMask)

            dst3 = PaletteBlackZero(dst1)
        End Sub
    End Class




    Public Class RedCart_CheckerBoardWall : Inherits TaskParent
        Public prepData As New RedPrep_Core
        Public classCount As Integer
        Dim edges As New Edge_Basics
        Public Sub New()
            desc = "Use this algorithm to build a checkerboard when pointing at a wall."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            ' prepData.reductionName = "XY Reduction" ' default
            prepData.Run(src)

            Dim lut = RedCart_Basics1.countClasses(prepData.dst2, labels(2))
            dst2 = prepData.dst2.LUT(lut)

            edges.Run(dst2)
            dst3 = edges.dst2
        End Sub
    End Class




    Public Class RedCart_TriangleDots : Inherits TaskParent
        Dim checkers As New RedPrep_Core
        Public Sub New()
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
        Public prepData As New RedPrep_Core
        Public classCount As Integer
        Public lut As cv.Mat
        Public Sub New()
            OptionParent.findRadio("X Reduction").Checked = True
            desc = "Prep the vertical regions in the reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prepData.Run(src)
            dst2 = prepData.dst3

            Dim lut = RedCart_Basics1.countClasses(prepData.dst2, labels(2))
            dst2 = prepData.dst2.LUT(lut)
        End Sub
    End Class



    Public Class RedCart_PrepY : Inherits TaskParent
        Public prepData As New RedPrep_Core
        Public classCount As Integer
        Public Sub New()
            OptionParent.findRadio("Y Reduction").Checked = True
            desc = "Prep the horizontal regions in the reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prepData.Run(src)
            dst2 = prepData.dst3

            Dim lut = RedCart_Basics1.countClasses(prepData.dst2, labels(2))
            dst2 = prepData.dst2.LUT(lut)
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
        Public prepData As New RedPrep_Core
        Public classCount As Integer
        Public Sub New()
            OptionParent.findRadio("X Reduction").Checked = True
            desc = "Prep the vertical regions in the reduced depth data."
        End Sub

        Public Overrides Sub RunAlg(src As cv.Mat)
            prepData.Run(src)
            dst2 = prepData.dst3

            Dim lut = RedCart_Basics1.countClasses(prepData.dst2, labels(2))
            dst2 = prepData.dst2.LUT(lut)
        End Sub
    End Class





    Public Class RedCart_PrepWC : Inherits TaskParent
        Dim prepData As New RedPrep_Core
        Public Sub New()
            OptionParent.findRadio("X Reduction").Checked = True
            desc = "Prepare the absolute coordinates of the World Coordinates."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static radioX = OptionParent.findRadio("X Reduction")
            Static radioY = OptionParent.findRadio("Y Reduction")

            radioX.checked = True
            prepData.Run(src)
            dst2 = prepData.dst2
            labels(2) = prepData.labels(2)

            radioY.checked = True
            prepData.Run(src)
            dst3 = prepData.dst2
            labels(3) = prepData.labels(2)
        End Sub
    End Class
End Namespace
