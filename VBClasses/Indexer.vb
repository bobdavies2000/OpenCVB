Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Indexer_Basics : Inherits TaskParent
        Dim prep As New RedPrep_Core
        Public Sub New()
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Find edges in the RedPrep output using indexers"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then
                prep.Run(src)
                dst2 = prep.dst2
                labels(2) = prep.labels(2)
            Else
                dst2 = src
            End If

            dst0 = dst2.Clone
            Dim indexer1 As cv.MatIndexer(Of Byte) = dst0.GetGenericIndexer(Of Byte)()
            For y = 0 To dst0.Rows - 1
                For x = 1 To dst0.Cols - 1
                    If indexer1(y, x) = indexer1(y, x - 1) Then indexer1(y, x - 1) = 0
                Next
            Next

            dst1 = dst2.Clone
            Dim indexer2 As cv.MatIndexer(Of Byte) = dst1.GetGenericIndexer(Of Byte)()
            For x = 0 To dst1.Cols - 1
                For y = 1 To dst1.Rows - 1
                    If indexer2(y, x) = indexer2(y - 1, x) Then indexer2(y - 1, x) = 0
                Next
            Next

            dst3.SetTo(0)
            dst3.SetTo(255, dst0)
            dst3.SetTo(255, dst1)
            dst3.SetTo(255, task.noDepthMask)
        End Sub
    End Class
End Namespace