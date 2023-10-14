Imports cv = OpenCvSharp
Imports CS_Classes
'Public Class SOM_Basics : Inherits VB_Algorithm
'    Dim random As New Random_UniformDist
'    Dim options As New Options_SOM
'    Dim koho As New CS_Kohonen
'    Public Sub New()
'        labels(3) = "Original Randomized input"
'        desc = "Accord: Self-Organizing Map (SOM) using Kohonen"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        Dim square = dst2.Height / 2
'        Static rect = New cv.Rect(0, 0, square, square)
'        Options.RunVB()

'        Static currentIteration As Integer
'        If currentIteration >= options.iterations Or firstPass Or task.optionsChanged Then
'            koho.firstPass = True
'            random.Run(src)
'            Dim tmp1 = random.dst2(rect)
'            random.Run(src)
'            Dim tmp2 = random.dst2(rect)
'            random.Run(src)
'            Dim tmp3 = random.dst2(rect)
'            cv.Cv2.Merge({tmp1, tmp2, tmp3}, dst1)
'            dst2 = dst1.Clone
'            currentIteration = 0
'            koho.initialize(square, options.learningRate)
'        End If

'        Dim bitmap = cv.Extensions.BitmapConverter.ToBitmap(dst2)
'        koho.RunCS(options.iterations, currentIteration, options.radius)
'        If currentIteration Mod 10 = 9 Then
'            bitmap = koho.showResults(bitmap, square)
'            dst2 = cv.Extensions.BitmapConverter.ToMat(bitmap)
'        End If
'        labels(2) = "Self-Organizing Map (SOM) output after " + CStr(currentIteration) + " iterations"
'    End Sub
'End Class
