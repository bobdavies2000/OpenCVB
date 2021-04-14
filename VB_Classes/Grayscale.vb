Imports cv = OpenCvSharp
Public Class Grayscale_Basics
    Inherits VBparent
    Public Sub New()
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Use OpenCV to create grayscale image"
            check.Box(0).Checked = True
        End If
        task.desc = "Manually create a grayscale image.  The only reason for this example is to show how slow it can be to do the work manually in VB.Net"
		' task.rank = 1
        label1 = "Grayscale_Basics"
        label2 = ""
    End Sub
    Public Sub Run(src as cv.Mat)
        If check.Box(0).Checked Then
            dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8U)
            Parallel.For(0, src.Rows,
                Sub(y)
                    For x = 0 To src.Cols - 1
                        Dim cc = src.Get(Of cv.Vec3b)(y, x)
                        dst1.Set(Of Byte)(y, x, CByte((cc.Item0 * 1140 + cc.Item1 * 5870 + cc.Item2 * 2989) / 10000))
                    Next
                End Sub)
        End If
    End Sub
End Class


