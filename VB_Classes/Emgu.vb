Imports cv = OpenCvSharp



' be sure to add Emgu support with 'Tools/NuGet/NuGet Package Manager' before enabling the code below.




'Public Class Emgu_Basics
'    Inherits ocvbClass
'    Public Sub New()
'        initParent()
'        task.desc = "Test a sample EMGU usage."
'    End Sub
'    Public Sub Run()
'		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
'        Dim data(src.Rows * src.Cols * src.ElemSize) As Byte
'        If task.parms.testAllRunning  Then
'            ocvb.trueText("During 'Test All', EMGU will occasionally fail with a missing cvextern.dll." + vbCrLf +
'                                                  "The algorithm is working fine so it is turned off during testing.", 10, 125))
'        Else
'            Emgu_Classes.DrawSubdivision.Draw(src.Rows, src.Cols, 20, data)
'            ' why not just have Draw return a Mat from Emgu?  Because an Emgu Mat is not an OpenCVSharp Mat!  But this works...
'            dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, data)
'        End If
'    End Sub
'End Class




'Public Class Emgu_Facedetection
'    Inherits ocvbClass
'    Public Sub New()
'        initParent()
'        task.desc = "Use the simplest possible face detector in Emgu examples."
'    End Sub
'    Public Sub Run()
'		 If task.intermediateReview = caller Then ocvb.intermediateObject = Me
'        If task.parms.testAllRunning  Then
'            ocvb.trueText("During 'Test All', EMGU will occasionally fail with a missing cvextern.dll." + vbCrLf +
'                                                  "The algorithm is working fine so it is turned off during testing.", 10, 125))
'        Else
'            Dim lena = New cv.Mat(task.parms.homeDir + "Data/Lena.jpg", cv.ImreadModes.Color)
'            Dim data(lena.Rows * lena.Cols * lena.ElemSize) As Byte
'            Emgu_Classes.FaceDetection.Detect(task.parms.homeDir + "Data\\Lena.jpg",
'                                              task.parms.homeDir + "Data\\haarcascade_frontalface_alt.xml", data)
'            Dim tmp = New cv.Mat(lena.Rows, lena.Cols, cv.MatType.CV_8UC3, data)
'            tmp = tmp.Resize(New cv.Size(dst1.Rows, dst1.Rows))
'            dst1(New cv.Rect(0, 0, tmp.Rows, tmp.Cols)) = tmp
'        End If
'    End Sub
'End Class


