Imports cv = OpenCvSharp
Public Class TransformationMatrix_Basics : Inherits VBparent
    Dim topLocations As New List(Of cv.Point3d)
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "TMatrix Top View multiplier", 1, 1000, 500)
        End If
        If task.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2 Then sliders.trackbar(0).Value = 1 ' need a smaller multiplier...

        labels(2) = "View from above the camera"
        labels(3) = "View from side of the camera"
        task.desc = "Show the contents of the transformation matrix"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.transformationMatrix IsNot Nothing Then
            Dim t = task.transformationMatrix
            Dim mul = sliders.trackbar(0).Value
            topLocations.Add(New cv.Point3d(-t(12) * mul + dst2.Width / 2,
                                            -t(13) * mul + dst2.Height / 2,
                                             t(14) * mul + dst2.Height / 2))

            For i = 0 To topLocations.Count - 1
                Dim pt = topLocations.ElementAt(i)
                If pt.X > 0 And pt.X < dst2.Width And pt.Z > 0 And pt.Z < src.Height Then
                    dst2.Circle(New cv.Point(pt.X, pt.Z), task.dotSize + 2, cv.Scalar.Yellow, -1, task.lineType)
                End If

                If pt.Z > 0 And pt.Z < dst2.Width And pt.Y > 0 And pt.Y < src.Height Then
                    dst3.Circle(New cv.Point(pt.Z, pt.Y), task.dotSize + 2, cv.Scalar.Yellow, -1, task.lineType)
                End If
            Next

            If topLocations.Count > 20 Then topLocations.RemoveAt(0) ' just show the last x points
        Else
            setTrueText("The transformation matrix for the current camera has not been set", 10, 125)
        End If
    End Sub
End Class


