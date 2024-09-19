Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices

Public Class Stripes_Basics : Inherits VB_Parent
    Dim options As New Options_Stripes
    Public Sub New()
        desc = "Create stripes throughout the image with reduction"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim depth32f As cvb.Mat = task.pcSplit(2) * 1000
        Dim depth32S As New cvb.Mat
        depth32f.convertto(depth32S, cvb.MatType.CV_32S)


    End Sub
End Class

