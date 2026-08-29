Imports OpenCvSharp
Imports OpenCvSharp.Cv2
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Neighbor_Basics : Inherits TaskParent
        Dim redC As New RedC_Basics
        Public nabs As New List(Of Integer)
        Public Sub New()
            desc = "Find all the neighbors with CalcHist and the neighborMask"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            If task.rcD IsNot Nothing Then
                Dim rc = task.rcD
                Dim histogram As New Mat
                Dim bins = redC.rcList.Count
                Dim ranges = {New Rangef(0, bins + 1)}
                CalcHist({redC.IndexMap(rc.rect)}, {0}, rc.neighborMask, histogram, 1, {bins}, ranges)

                Dim histArray(bins) As Single
                histogram.GetArray(Of Single)(histArray)
                For i = 1 To bins - 1
                    If histArray(i) > 0 Then nabs.Add(i)
                Next

                strOut = ""
                For Each index In nabs
                    strOut += "cell " + CStr(index) + " is a neighbor"
                Next
                SetTrueText(strOut, 3)
            End If
        End Sub
    End Class





End Namespace