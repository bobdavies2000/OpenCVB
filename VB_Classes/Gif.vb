Imports cv = OpenCvSharp
Imports System.Drawing
Imports cvext = OpenCvSharp.Extensions
Imports System.IO
Imports System.Windows.Media.Media3D

' https://stackoverflow.com/questions/1196322/how-to-create-an-animated-gif-in-net
' https://stackoverflow.com/questions/18719302/net-creating-a-looping-gif-using-gifbitmapencoder
' https://ezgif.com/optimize
Public Class Gif_Basics : Inherits VB_Algorithm
    Public options As New Options_Gif
    Public Sub New()
        clearTempDir()
        labels = {"", "", "Input to GIF", ""}
        desc = "Create a GIF file by clicking on the checkbox when dst2 is to be used."
    End Sub
    Private Sub clearTempDir()
        Dim imgDir As New DirectoryInfo(task.homeDir + "Temp")
        If imgDir.Exists = False Then imgDir.Create()
        Dim imgList As FileInfo() = imgDir.GetFiles("*.bmp")

        If imgList.Count Then
            For Each imgFile In imgList
                My.Computer.FileSystem.DeleteFile(imgFile.FullName)
            Next
            Dim gifFile As New FileInfo(task.homeDir + "Temp\myGif.gif")
            If gifFile.Exists Then My.Computer.FileSystem.DeleteFile(gifFile.FullName)
        End If
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        dst2 = src

        If options.restartCheck.Checked Then
            task.gifImages.Clear()
            clearTempDir()
        End If

        task.optionsChanged = False ' trying to reduce the impact of options changing on the active algorithm

        labels(2) = "Images captured: " + CStr(task.gifImages.Count)
        setTrueText("Gif_Basics is typically called from VB_Task to create the .gif file." + vbCrLf +
                    "The snapshots that are input to GifBuilder are created in VB_Algorithm.vb (see GifCreator)", 3)
    End Sub
End Class










Public Class Gif_OpenGL : Inherits VB_Algorithm
    Dim input As New Model_RedCloud
    Dim gifC As New Gif_Basics
    Public Sub New()
        desc = "Create a GIF for the Model_RedCloud output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        input.Run(src)

        dst2 = input.dst3
        Dim r = New cv.Rect(0, 0, dst2.Height, dst2.Height)
        gifC.Run(dst2(r))

        setTrueText("Select 'Gif_Basics CheckBox Options' form (see 'OpenCVB Algorithm Options'" + vbCrLf +
                    "Click the check box for each frame to be included" + vbCrLf + "Then click 'Build GIF file...' when done." +
                    vbCrLf + vbCrLf + "To adjust the GIF size, change the working size in the OpenCVB options.", 3)
        labels(2) = gifC.labels(2)
    End Sub
End Class









Public Class Gif_OpenGLwithColor : Inherits VB_Algorithm
    Dim input As New Model_RedCloud
    Dim gifC As New Gif_Basics
    Public Sub New()
        desc = "Create a GIF for the Model_RedCloud output and color image at the same time."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        input.Run(src)

        Dim r = New cv.Rect(0, 0, dst2.Height, dst2.Height)
        dst2 = input.dst3(r)
        Dim tmp As New cv.Mat
        cv.Cv2.HConcat(src, dst2(r), tmp)

        gifC.Run(tmp)

        setTrueText("Select 'Gif_Basics CheckBox Options' form (see 'OpenCVB Algorithm Options'" + vbCrLf +
                    "Click the check box for each frame to be included" + vbCrLf + "Then click 'Build GIF file...' when done.", 3)
        labels(2) = gifC.labels(2)
    End Sub
End Class









Public Class Gif_OpenCVB : Inherits VB_Algorithm
    Dim gifC As New Gif_Basics
    Public Sub New()
        desc = "Create a GIF of the OpenCVB main screen for any algorithm."
    End Sub
    Public Sub createNextGifImage(dst0 As cv.Mat, dst1 As cv.Mat, tmpDst2 As cv.Mat, tmpDst3 As cv.Mat)
        Static snapCheck = findCheckBox("Check this box when Gif_Basics dst2 contains the desired snapshot.")
        If snapCheck.checked Then
            Dim nextBMP As Bitmap
            Select Case task.gifCaptureIndex
                Case 0
                    If tmpDst2.Channels = 1 Then tmpDst2 = tmpDst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                    nextBMP = New Bitmap(tmpDst2.Width, tmpDst2.Height, Imaging.PixelFormat.Format24bppRgb)
                    cvext.BitmapConverter.ToBitmap(tmpDst2, nextBMP)
                Case 1
                    If tmpDst3.Channels = 1 Then tmpDst3 = tmpDst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                    nextBMP = New Bitmap(tmpDst3.Width, tmpDst3.Height, Imaging.PixelFormat.Format24bppRgb)
                    cvext.BitmapConverter.ToBitmap(tmpDst3, nextBMP)
                Case 2 ' OpenCVB window
                    Dim r = New cv.Rect(0, 0, task.mainFormLocation.Width - 20, task.mainFormLocation.Height - 40)
                    nextBMP = New Bitmap(r.Width, r.Height, Imaging.PixelFormat.Format24bppRgb)
                    Dim snapshot As Bitmap = GetWindowImage(task.main_hwnd, r)
                    Dim snap = cvext.BitmapConverter.ToMat(snapshot)
                    snap = snap.CvtColor(cv.ColorConversionCodes.BGRA2BGR)
                    cvext.BitmapConverter.ToBitmap(snap, nextBMP)
                Case 3 ' OpenGL window
                    Dim rect As RECT
                    GetWindowRect(openGL_hwnd, rect)
                    Dim r = New cv.Rect(0, 0, rect.Right - rect.Left + 330, rect.Bottom - rect.Top + 200)
                    nextBMP = New Bitmap(r.Width, r.Height, Imaging.PixelFormat.Format24bppRgb)
                    Dim snapshot As Bitmap = GetWindowImage(openGL_hwnd, r)
                    Dim snap = cvext.BitmapConverter.ToMat(snapshot)
                    snap = snap.CvtColor(cv.ColorConversionCodes.BGRA2BGR)
                    cvext.BitmapConverter.ToBitmap(snap, nextBMP)
                Case 4
                    If dst0.Channels = 1 Then dst0 = dst0.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                    nextBMP = New Bitmap(dst0.Width, dst0.Height, Imaging.PixelFormat.Format24bppRgb)
                    cvext.BitmapConverter.ToBitmap(dst0, nextBMP)
                Case 5
                    If dst1.Channels = 1 Then dst1 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                    nextBMP = New Bitmap(dst1.Width, dst1.Height, Imaging.PixelFormat.Format24bppRgb)
                    cvext.BitmapConverter.ToBitmap(dst1, nextBMP)
            End Select
            task.gifImages.Add(nextBMP)
            snapCheck.checked = False
        End If
    End Sub
    Public Sub RunVB(src As cv.Mat)
        setTrueText("Results are best when the main form is set to an 'auto-sized' setting.", 3)
        Static snapCheck = findCheckBox("Check this box when Gif_Basics dst2 contains the desired snapshot.")

        If snapCheck.checked Or (standalone And task.heartBeat) And task.mainFormLocation.Width > 0 Then
            Dim snapshot As Bitmap = GetWindowImage(task.main_hwnd,
                                     New cv.Rect(0, 0, task.mainFormLocation.Width, task.mainFormLocation.Height))
            Dim r = New cv.Rect(0, 0, snapshot.Width - 16, snapshot.Height - 40)
            dst2 = cvext.BitmapConverter.ToMat(snapshot)(r)
        End If

        gifC.Run(dst2)

        labels(2) = "Images captured: " + CStr(task.gifImages.Count)
        labels(3) = "After 'Build GIF file...' was clicked, resulting gif will be in 'c:/temp/myGIF.gif'"
    End Sub
End Class