Imports cvb = OpenCvSharp
Public Class TransformationMatrix_Basics : Inherits VB_Parent
    Dim topLocations As New List(Of cvb.Point3d)
    Dim options As New Options_TransformationMatrix
    Public Sub New()
        If task.cameraName = "StereoLabs ZED 2/2i" Then
            FindSlider("TMatrix Top View multiplier").Value = 1 ' need a smaller multiplier for this camera...
        End If
        labels = {"", "", "View from above the camera", "View from side of the camera"}
        desc = "Show the contents of the transformation matrix"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If task.transformationMatrix IsNot Nothing Then
            Dim t = task.transformationMatrix

            topLocations.Add(New cvb.Point3d(-t(12) * options.mul + dst2.Width / 2,
                                            -t(13) * options.mul + dst2.Height / 2,
                                             t(14) * options.mul + dst2.Height / 2))

            For i = 0 To topLocations.Count - 1
                Dim pt = topLocations.ElementAt(i)
                If pt.X > 0 And pt.X < dst2.Width And pt.Z > 0 And pt.Z < src.Height Then
                    DrawCircle(dst2, New cvb.Point(pt.X, pt.Z), task.DotSize + 2, cvb.Scalar.Yellow)
                End If

                If pt.Z > 0 And pt.Z < dst2.Width And pt.Y > 0 And pt.Y < src.Height Then
                    DrawCircle(dst3, New cvb.Point(pt.Z, pt.Y), task.DotSize + 2, cvb.Scalar.Yellow)
                End If
            Next

            If topLocations.Count > 20 Then topLocations.RemoveAt(0) ' just show the last x points
        Else
            SetTrueText("The transformation matrix for the current camera has not been set", New cvb.Point(10, 125))
        End If
    End Sub
End Class


