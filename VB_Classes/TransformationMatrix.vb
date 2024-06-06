Imports cv = OpenCvSharp
Public Class TransformationMatrix_Basics : Inherits VB_Parent
    Dim topLocations As New List(Of cv.Point3d)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("TMatrix Top View multiplier", 1, 1000, 500)
        If task.cameraName = "StereoLabs ZED 2/2i" Then
            FindSlider("TMatrix Top View multiplier").Value = 1 ' need a smaller multiplier for this camera...
        End If
        labels = {"", "", "View from above the camera", "View from side of the camera"}
        desc = "Show the contents of the transformation matrix"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static multSlider = FindSlider("TMatrix Top View multiplier")
        If task.transformationMatrix IsNot Nothing Then
            Dim t = task.transformationMatrix
            Dim mul = multSlider.Value
            topLocations.Add(New cv.Point3d(-t(12) * mul + dst2.Width / 2,
                                            -t(13) * mul + dst2.Height / 2,
                                             t(14) * mul + dst2.Height / 2))

            For i = 0 To topLocations.Count - 1
                Dim pt = topLocations.ElementAt(i)
                If pt.X > 0 And pt.X < dst2.Width And pt.Z > 0 And pt.Z < src.Height Then
                    drawCircle(dst2,New cv.Point(pt.X, pt.Z), task.dotSize + 2, cv.Scalar.Yellow)
                End If

                If pt.Z > 0 And pt.Z < dst2.Width And pt.Y > 0 And pt.Y < src.Height Then
                    drawCircle(dst3,New cv.Point(pt.Z, pt.Y), task.dotSize + 2, cv.Scalar.Yellow)
                End If
            Next

            If topLocations.Count > 20 Then topLocations.RemoveAt(0) ' just show the last x points
        Else
            setTrueText("The transformation matrix for the current camera has not been set", New cv.Point(10, 125))
        End If
    End Sub
End Class


