Imports cv = OpenCvSharp
Public Class TransformationMatrix_Basics
    Inherits VBparent
    Dim topLocations As New List(Of cv.Point3d)
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "TMatrix Top View multiplier", 1, 1000, 500)
        End If
        If task.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2 Then sliders.trackbar(0).Value = 1 ' need a smaller multiplier...

        label1 = "View from above the camera"
        label2 = "View from side of the camera"
        task.desc = "Show the contents of the transformation matrix"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If task.transformationMatrix IsNot Nothing Then
            Dim t = task.transformationMatrix
            Dim mul = sliders.trackbar(0).Value
            topLocations.Add(New cv.Point3d(-t(12) * mul + dst1.Width / 2,
                                            -t(13) * mul + dst1.Height / 2,
                                             t(14) * mul + dst1.Height / 2))

            For i = 0 To topLocations.Count - 1
                Dim pt = topLocations.ElementAt(i)
                If pt.X > 0 And pt.X < dst1.Width And pt.Z > 0 And pt.Z < src.Height Then
                    dst1.Circle(New cv.Point(pt.X, pt.Z), 3, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
                End If

                If pt.Z > 0 And pt.Z < dst1.Width And pt.Y > 0 And pt.Y < src.Height Then
                    dst2.Circle(New cv.Point(pt.Z, pt.Y), 3, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
                End If
            Next

            If topLocations.Count > 20 Then topLocations.RemoveAt(0) ' just show the last x points
        Else
            ocvb.trueText("The transformation matrix for the current camera has not been set", 10, 125)
        End If
    End Sub
End Class

