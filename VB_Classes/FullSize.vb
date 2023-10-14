Imports  cv = OpenCvSharp
Public Class FullSize_Test : Inherits VBparent
    Dim tracker As New Feature_Tracker
    Public Sub New()
        task.desc = "Run the Features_Tracker with the full resolution image regardless of the current workingSize." + vbCrLf +
                    "For algorithms whose output is a list of points, this can improve the accuracy even at low resolution."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.dotSize = If(task.dotSize < 3, 3, task.dotSize)
        tracker.Run(task.fullSize.color)
        labels = tracker.labels
        dst0 = tracker.dst0
        dst1 = tracker.dst1
        dst2 = tracker.dst2
        dst3 = tracker.dst3
    End Sub
End Class

