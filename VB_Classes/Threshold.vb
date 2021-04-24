Imports cv = OpenCvSharp
Public Class Threshold_LaplacianFilter : Inherits VBparent
    Dim edges As New Filter_Laplacian
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "z-Distance", 1, 3000, 200)
        End If
        label1 = "Foreground Input"
        label2 = "Edges of foreground input"
        task.desc = "Threshold the output of a Laplacian derivative, mask with depth foreground.  needs more work"
    End Sub
    Public Sub Run(src as cv.Mat)
        Static distSlider = findSlider("z-Distance")
        edges.Run(src)
        dst2 = edges.dst2
        dst1 = task.depth32f

        Dim mask = dst1.Threshold(distSlider.value, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs(255)
        dst2.SetTo(0, mask)
    End Sub
End Class



