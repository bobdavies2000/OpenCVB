﻿Imports cv = OpenCvSharp
Imports System.Drawing
Imports cvext = OpenCvSharp.Extensions
Imports System.IO

' https://stackoverflow.com/questions/1196322/how-to-create-an-animated-gif-in-net
' https://stackoverflow.com/questions/18719302/net-creating-a-looping-gif-using-gifbitmapencoder
' https://ezgif.com/optimize
Public Class Gif_Basics : Inherits TaskParent
    Public options As New Options_Gif
    Public Sub New()
        clearTempDir()
        labels = {"", "", "Input to GIF", ""}
        desc = "Create a GIF file by clicking on the checkbox when dst2 is to be used."
    End Sub
    Private Sub clearTempDir()
        Dim imgDir As New DirectoryInfo(task.HomeDir + "Temp")
        If imgDir.Exists = False Then imgDir.Create()
        Dim imgList As FileInfo() = imgDir.GetFiles("*.bmp")

        If imgList.Count Then
            For Each imgFile In imgList
                My.Computer.FileSystem.DeleteFile(imgFile.FullName)
            Next
            Dim gifFile As New FileInfo(task.HomeDir + "Temp\myGif.gif")
            If gifFile.Exists Then My.Computer.FileSystem.DeleteFile(gifFile.FullName)
        End If
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = src

        If options.restartCheck.Checked Then
            task.gifImages.Clear()
            clearTempDir()
        End If

        task.optionsChanged = False ' trying to reduce the impact of options changing on the active algorithm

        labels(2) = "Images captured: " + CStr(task.gifImages.Count)
        SetTrueText("Gif_Basics is typically called from VB_Task to create the .gif file." + vbCrLf +
                    "The snapshots that are input to GifBuilder are created in TaskParent.vb (see GifCreator)", 3)
    End Sub
End Class










Public Class Gif_OpenGL : Inherits TaskParent
    Dim input As New Model_RedCloud
    Dim gifC As New Gif_Basics
    Public Sub New()
        desc = "Create a GIF for the Model_RedCloud output"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        input.Run(src)

        dst2 = input.dst3
        Dim r = New cv.Rect(0, 0, dst2.Height, dst2.Height)
        gifC.Run(dst2(r))

        SetTrueText("Select 'Gif_Basics CheckBox Options' form (see 'OpenCVB Algorithm Options')" + vbCrLf +
                    "Click the check box for each frame to be included" + vbCrLf + "Then click 'Build GIF file...' when done." +
                    vbCrLf + vbCrLf + "To adjust the GIF size, change the working size in the OpenCVB options.", 3)
        labels(2) = gifC.labels(2)
    End Sub
End Class









Public Class Gif_OpenGLwithColor : Inherits TaskParent
    Dim input As New Model_RedCloud
    Dim gifC As New Gif_Basics
    Public Sub New()
        desc = "Create a GIF for the Model_RedCloud output and color image at the same time."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        input.Run(src)

        Dim r = New cv.Rect(0, 0, dst2.Height, dst2.Height)
        dst2 = input.dst3(r)
        Dim tmp As New cv.Mat
        cv.Cv2.HConcat(src, dst2(r), tmp)

        gifC.Run(tmp)

        SetTrueText("Select 'Gif_Basics CheckBox Options' form (see 'OpenCVB Algorithm Options')" + vbCrLf +
                    "Click the check box for each frame to be included" + vbCrLf + "Then click 'Build GIF file...' when done.", 3)
        labels(2) = gifC.labels(2)
    End Sub
End Class









Public Class Gif_OpenCVB : Inherits TaskParent
    Dim gifC As New Gif_Basics
    Public Sub New()
        desc = "Create a GIF of the OpenCVB main screen for any algorithm."
    End Sub
    Public Sub createNextGifImage()
        Static snapCheck = OptionParent.findCheckBox("Step 1: Check this box when ready to capture the desired snapshot.")
        If snapCheck.checked Then
            Dim nextBMP As Bitmap = Nothing
            Dim rect As RECT
            Select Case task.gifCaptureIndex
                Case gifTypes.gifdst0
                    If task.dst0.Channels() = 1 Then
                        task.dst0 = task.dst0.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                    End If
                    nextBMP = New Bitmap(task.dst0.Width, task.dst0.Height,
                                         Imaging.PixelFormat.Format24bppRgb)
                    cvext.BitmapConverter.ToBitmap(task.dst0, nextBMP)
                Case gifTypes.gifdst1
                    If task.dst1.Channels() = 1 Then
                        task.dst1 = task.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                    End If
                    nextBMP = New Bitmap(task.dst1.Width, task.dst1.Height,
                                         Imaging.PixelFormat.Format24bppRgb)
                    cvext.BitmapConverter.ToBitmap(task.dst1, nextBMP)
                Case gifTypes.gifdst2
                    If task.dst2.Channels() = 1 Then
                        task.dst2 = task.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                    End If
                    nextBMP = New Bitmap(task.dst2.Width, task.dst2.Height,
                                         Imaging.PixelFormat.Format24bppRgb)
                    cvext.BitmapConverter.ToBitmap(task.dst2, nextBMP)
                Case gifTypes.gifdst3
                    If task.dst3.Channels() = 1 Then
                        task.dst3 = task.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                    End If
                    nextBMP = New Bitmap(task.dst3.Width, task.dst3.Height,
                                         Imaging.PixelFormat.Format24bppRgb)
                    cvext.BitmapConverter.ToBitmap(task.dst3, nextBMP)
                Case gifTypes.openCVBwindow
                    Dim r = New cv.Rect(0, 0, task.mainFormLocation.Width - 20,
                                              task.mainFormLocation.Height - 40)
                    nextBMP = New Bitmap(r.Width, r.Height, Imaging.PixelFormat.Format24bppRgb)
                    Dim snapshot As Bitmap = GetWindowImage(task.main_hwnd, r)
                    Dim snap = cvext.BitmapConverter.ToMat(snapshot)
                    snap = snap.CvtColor(cv.ColorConversionCodes.BGRA2BGR)
                    cvext.BitmapConverter.ToBitmap(snap, nextBMP)
                Case gifTypes.openGLwindow
                    GetWindowRect(task.openGL_hwnd, rect)
                    Dim r = New cv.Rect(0, 0, rect.Right - rect.Left + 330,
                                              rect.Bottom - rect.Top + 200)
                    nextBMP = New Bitmap(r.Width, r.Height, Imaging.PixelFormat.Format24bppRgb)
                    Dim snapshot As Bitmap = GetWindowImage(task.openGL_hwnd, r)
                    Dim snap = cvext.BitmapConverter.ToMat(snapshot)
                    snap = snap.CvtColor(cv.ColorConversionCodes.BGRA2BGR)
                    cvext.BitmapConverter.ToBitmap(snap, nextBMP)
                Case gifTypes.EntireScreen
                    nextBMP = CaptureScreen()
                    Dim snap = cvext.BitmapConverter.ToMat(nextBMP)
                    ' snap = snap.CvtColor(cv.ColorConversionCodes.BGRA2BGR)
                    ' snap = snap.Resize(New cv.Size(snap.Width / 3, snap.Height / 3))
                    cvext.BitmapConverter.ToBitmap(snap, nextBMP)
            End Select
            task.gifImages.Add(nextBMP)
            snapCheck.checked = False
        End If
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        SetTrueText("Results are best when the main form is set to an 'auto-sized' setting.", 3)
        Static snapCheck = OptionParent.findCheckBox("Step 1: Check this box when ready to capture the desired snapshot.")

        gifC.Run(dst2)

        labels(2) = "Images captured: " + CStr(task.gifImages.Count)
        labels(3) = "After 'Build GIF file...' was clicked, resulting gif will be in '" +
                    task.HomeDir + "/temp/myGIF.gif'"
    End Sub
End Class