Imports System.Windows.Forms
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Foreground_Basics : Inherits TaskParent
        Dim simK As New KMeans_Depth
        Public fgDepth As Single
        Public fg As New cv.Mat, bg As New cv.Mat, classCount As Integer
        Public Sub New()
            labels(3) = "Foreground - all the KMeans classes up to and including the first class over 1 meter."
            dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Find the first KMeans class with depth over 1 meter and use it to define foreground"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            simK.Run(src)
            classCount = simK.classCount

            ' Order the KMeans classes from foreground to background using depth data.
            Dim depthMats As New List(Of cv.Mat)
            Dim sortedMats As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
            For i = 0 To classCount - 1
                Dim tmp = simK.dst2.InRange(i, i)
                depthMats.Add(tmp.Clone)
                Dim depth = atask.pcSplit(2).Mean(tmp)(0)
                sortedMats.Add(depth, i)
            Next

            fgDepth = 0
            For Each fgDepth In sortedMats.Keys
                If fgDepth >= 1 Then Exit For ' find all the regions closer than a meter (inclusive)
            Next

            For Each index In sortedMats.Values
                Dim tmp = depthMats(index)
                dst1.SetTo(index + 1, tmp)
            Next
            dst2 = PaletteFull(dst1)
            fg = atask.pcSplit(2).Threshold(fgDepth, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
            dst0 = fg

            fg.SetTo(0, atask.noDepthMask)
            bg = Not fg

            dst3.SetTo(0)
            src.CopyTo(dst3, fg)
            SetTrueText("KMeans classes are in dst1 - ordered by depth" + vbCrLf + "fg = foreground mask", 3)
            labels(2) = "KMeans output defining the " + CStr(classCount) + " classes"
        End Sub
    End Class






    Public Class Foreground_KMeans : Inherits TaskParent
        Dim km As New KMeans_Image
        Public Sub New()
            OptionParent.FindSlider("KMeans k").Value = 2
            labels = {"", "", "Foreground Mask", "Background Mask"}
            dst2 = New cv.Mat(New cv.Size(atask.workRes.Width, atask.workRes.Height), cv.MatType.CV_8U, cv.Scalar.All(0))
            dst3 = New cv.Mat(New cv.Size(atask.workRes.Width, atask.workRes.Height), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Separate foreground and background using Kmeans with k=2."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            atask.optionsChanged = True

            src = atask.pcSplit(2).Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
            km.Run(src)

            Dim minDistance = Single.MaxValue
            Dim minIndex As Integer
            For i = 0 To km.km.colors.Rows - 1
                Dim distance = km.km.colors.Get(Of Single)(i, 0)
                If minDistance > distance And distance > 0 Then
                    minDistance = distance
                    minIndex = i
                End If
            Next
            dst2.SetTo(0)
            dst2.SetTo(255, km.masks(minIndex))
            dst2.SetTo(0, atask.noDepthMask)

            dst3 = Not dst2
            dst3.SetTo(0, atask.noDepthMask)
        End Sub
    End Class







    Public Class Foreground_Hist3D : Inherits TaskParent
        Dim hcloud As New Hist3Dcloud_Basics
        Public Sub New()
            hcloud.maskInput = atask.noDepthMask
            labels = {"", "", "Foreground", "Background"}
            desc = "Use the first class of hist3Dcloud_Basics as the definition of foreground"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            hcloud.Run(src)

            dst2.SetTo(0)
            dst2 = hcloud.dst2.InRange(1, 1) Or atask.noDepthMask
            dst3 = Not dst2
        End Sub
    End Class
End Namespace