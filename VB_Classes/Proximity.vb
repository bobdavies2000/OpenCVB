Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Proximity_BasicsDepth : Inherits VBparent
    Dim km As New KMeans_Image
    Public Sub New()
        findSlider("Resize Factor (used only with KMeans_BasicsFast)").Enabled = True
        task.desc = "Cluster just depth using kMeans but hopefully faster than Proximity_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static resizeSlider = findSlider("Resize Factor (used only with KMeans_BasicsFast)")
        Dim resizeFactor = resizeSlider.value

        Dim w = CInt(task.depth32f.Width / resizeFactor)
        Dim h = CInt(task.depth32f.Height / resizeFactor)
        Dim depth32f = task.depth32f.Resize(New cv.Size(w, h), 0, 0, cv.InterpolationFlags.Nearest)
        depth32f.SetTo(0, task.noDepthMask.Resize(depth32f.Size))
        km.Run(depth32f)
        dst2 = km.dst2
    End Sub
End Class







Public Class Proximity_Reduction : Inherits VBparent
    Dim reduction As New Reduction_Basics
    Public counts As New List(Of Integer)
    Public Sub New()
        reduction.radio.check(0).Checked = True
        findSlider("Reduction factor").Value = 800
        labels(3) = "Reduced depth data before normalizing (32-bit)"
        task.desc = "Use reduction to cluster depth data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.depth32f.ConvertTo(src, cv.MatType.CV_32S)
        reduction.Run(src)
        reduction.dst2.ConvertTo(dst3, cv.MatType.CV_32F)

        dst2 = dst3.Normalize(0, 255, cv.NormTypes.MinMax)
        dst2.ConvertTo(dst2, cv.MatType.CV_8UC1)

        If task.heartBeat Then
            counts.Clear()
            For y = 0 To dst2.Rows - 1 Step dst2.Rows / 10
                For x = 0 To dst2.Cols - 1 Step dst2.Cols / 10
                    Dim val = CInt(dst2.Get(Of Byte)(y, x))
                    If counts.Contains(val) = False Then counts.Add(val)
                Next
            Next
        End If

        task.palette.Run(dst2)
        dst2 = task.palette.dst2
        labels(2) = reduction.labels(2) + " with " + CStr(counts.Count) + " levels"
    End Sub
End Class
