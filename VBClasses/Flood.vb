Imports System.Windows.Documents
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Flood_Basics : Inherits TaskParent
        Public redC As New RedColor_Basics
        Public Sub New()
            desc = "Build the RedCloud cells with the grayscale input."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            SetTrueText(redC.strOut, 3)
        End Sub
    End Class







    Public Class NR_Flood_Tiers : Inherits TaskParent
        Dim flood As New Flood_BasicsMask
        Dim tiers As New Depth_Tiers
        Dim color8U As New Color8U_Basics
        Public Sub New()
            task.gOptions.displayDst1.Checked = True
            desc = "Subdivide the Flood_Basics cells using depth tiers."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim tier = task.gOptions.DebugSlider.Value

            tiers.Run(src)
            If tier >= tiers.classCount Then tier = 0

            If tier = 0 Then
                dst0 = Not tiers.dst2.InRange(0, 1)
            Else
                dst0 = Not tiers.dst2.InRange(tier, tier)
            End If

            labels(2) = tiers.labels(2) + " in tier " + CStr(tier) + ".  Use the global options 'DebugSlider' to select different tiers."

            color8U.Run(src)

            flood.inputRemoved = dst0
            flood.Run(color8U.dst2)

            dst2 = flood.dst2
            dst3 = flood.dst3

            SetTrueText(flood.redC.strOut, 1)
        End Sub
    End Class





    Public Class NR_Flood_Minimal : Inherits TaskParent
        Dim prep As New RedPrep_Basics
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            labels(2) = "Output is from RedPrep_Core. Click any region to floodfill it."
            labels(3) = "Mask resulting region selected by the click."
            desc = "Floodfill the selected segment of the RedPrep image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)
            dst2 = prep.dst1

            If task.mouseClickFlag Then
                Dim rect As New cv.Rect
                Dim pt = task.clickPoint
                Dim mask = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8U, 0)
                Dim flags = cv.FloodFillFlags.FixedRange Or (255 << 8) Or cv.FloodFillFlags.MaskOnly
                Dim count = cv.Cv2.FloodFill(dst2, mask, pt, 255, rect, 0, 0, flags)
                dst1.SetTo(0)
                dst3 = mask(New cv.Rect(1, 1, dst2.Width, dst2.Height)).Clone
                dst1.Rectangle(rect, 255, task.lineWidth)
            End If
        End Sub
    End Class






    Public Class Flood_BasicsMask : Inherits TaskParent
        Public inputRemoved As cv.Mat
        Public showSelected As Boolean = True
        Public redC As New RedColor_Basics
        Public Sub New()
            labels(3) = "The inputRemoved mask is used to limit how much of the image is processed."
            desc = "Floodfill by color as usual but this is run repeatedly with the different tiers."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                Static color8U As New Color8U_Basics
                color8U.Run(src)
                inputRemoved = task.pcSplit(2).InRange(task.MaxZmeters, task.MaxZmeters).ConvertScaleAbs()
                src = color8U.dst2
            End If

            dst3 = inputRemoved
            If inputRemoved IsNot Nothing Then src.SetTo(0, inputRemoved)

            redC.Run(src)
            labels(2) = redC.labels(2)
            dst2 = redC.dst2.SetTo(0, inputRemoved)

            labels(2) = $"{redC.rcList.Count} cells identified"

            If showSelected Then SetTrueText(redC.strOut, 1)
        End Sub
    End Class
End Namespace