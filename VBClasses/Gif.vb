Imports System.Drawing.Imaging
Imports System.IO
Imports System.Runtime.Intrinsics
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions

' https://stackoverflow.com/questions/1196322/how-to-create-an-animated-gif-in-net
' https://stackoverflow.com/questions/18719302/net-creating-a-looping-gif-using-gifbitmapencoder
' https://ezgif.com/optimize
Namespace VBClasses
    Public Class Gif_Basics : Inherits TaskParent
        Public options As New Options_Gif
        Public Sub New()
            clearTempDir()
            labels = {"", "", "Input to GIF", ""}
            desc = "Create a GIF file by clicking on the checkbox when dst2 is to be used."
        End Sub
        Private Sub clearTempDir()
            Dim imgDir As New DirectoryInfo(taskA.homeDir + "Temp")
            If imgDir.Exists = False Then imgDir.Create()
            Dim imgList As FileInfo() = imgDir.GetFiles("*.bmp")

            If imgList.Count Then
                For Each imgFile In imgList
                    My.Computer.FileSystem.DeleteFile(imgFile.FullName)
                Next
                Dim gifFile As New FileInfo(taskA.homeDir + "Temp\myGif.gif")
                If gifFile.Exists Then My.Computer.FileSystem.DeleteFile(gifFile.FullName)
            End If
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst2 = src

            If options.restartRequest Then
                taskA.gifImages.Clear()
                clearTempDir()
            End If

            taskA.optionsChanged = False ' trying to reduce the impact of options changing on the active algorithm

            labels(2) = "Images captured: " + CStr(taskA.gifImages.Count)
            SetTrueText("Gif_Basics is typically called from VB_Task to create the .gif file." + vbCrLf +
                        "The snapshots that are input to GifBuilder are created in TaskParent.vb (see GifCreator)", 3)
        End Sub
    End Class










    Public Class Gif_OpenCVB : Inherits TaskParent
        Public gifC As New Gif_Basics
        Public Sub New()
            desc = "Create a GIF of the OpenCVB main screen for any algorithm."
        End Sub
        Public Function CaptureScreen() As Bitmap
            Dim screenBounds As Rectangle = Screen.PrimaryScreen.Bounds
            Dim screenshot As New Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb)
            Using g As Graphics = Graphics.FromImage(screenshot)
                g.CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, screenBounds.Size, CopyPixelOperation.SourceCopy)
            End Using
            Return screenshot
        End Function
        Public Function GetWindowImage(ByVal WindowHandle As IntPtr, ByVal rect As cv.Rect) As Bitmap
            Dim b As New Bitmap(rect.Width, rect.Height, Imaging.PixelFormat.Format24bppRgb)

            Using img As Graphics = Graphics.FromImage(b)
                Dim ImageHDC As IntPtr = img.GetHdc
                Using window As Graphics = Graphics.FromHwnd(WindowHandle)
                    Dim WindowHDC As IntPtr = window.GetHdc
                    BitBlt(ImageHDC, 0, 0, rect.Width, rect.Height, WindowHDC, rect.X, rect.Y, CopyPixelOperation.SourceCopy)
                    window.ReleaseHdc()
                End Using
                img.ReleaseHdc()
            End Using

            Return b
        End Function
        Public Sub createNextGifImage()
            Static snapCheck = OptionParent.FindCheckBox("Step 1: Check this box when ready to capture the desired snapshot.")
            If snapCheck.checked Then
                Dim nextBMP As Bitmap = Nothing
                Select Case taskA.gifCaptureIndex
                    Case gifTypes.gifdst0
                        If taskA.gOptions.CrossHairs.Checked Then Gravity_Basics.showVectors(taskA.color)
                        Dim dst = If(taskA.gOptions.displayDst0.Checked, dst0, taskA.color)
                        If dst.Channels() = 1 Then
                            dst = dst.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                        End If
                        nextBMP = New Bitmap(dst.Width, dst.Height, Imaging.PixelFormat.Format24bppRgb)
                        cvext.BitmapConverter.ToBitmap(dst, nextBMP)
                    Case gifTypes.gifdst1
                        Dim dst = If(taskA.gOptions.displayDst1.Checked, dst1, taskA.depthRGB)
                        If dst.Channels() = 1 Then
                            dst = dst.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                        End If
                        nextBMP = New Bitmap(dst.Width, dst.Height, Imaging.PixelFormat.Format24bppRgb)
                        cvext.BitmapConverter.ToBitmap(dst, nextBMP)
                    Case gifTypes.gifdst2
                        If taskA.gOptions.ShowGrid.Checked Then taskA.dstList(2).SetTo(cv.Scalar.White, taskA.gridMask)
                        If taskA.dstList(2).Channels() = 1 Then
                            taskA.dstList(2) = taskA.dstList(2).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                        End If
                        nextBMP = New Bitmap(taskA.workRes.Width, taskA.workRes.Height, Imaging.PixelFormat.Format24bppRgb)
                        cvext.BitmapConverter.ToBitmap(taskA.dstList(2), nextBMP)
                    Case gifTypes.gifdst3
                        If taskA.dstList(3).Channels() = 1 Then
                            taskA.dstList(3) = taskA.dstList(3).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                        End If
                        nextBMP = New Bitmap(taskA.workRes.Width, taskA.workRes.Height, Imaging.PixelFormat.Format24bppRgb)
                        cvext.BitmapConverter.ToBitmap(taskA.dstList(3), nextBMP)
                    Case gifTypes.openCVBwindow
                        Dim r = New cv.Rect(0, 0, taskA.Settings.MainFormWidth - 20, taskA.Settings.MainFormHeight - 40)
                        nextBMP = New Bitmap(r.Width, r.Height, Imaging.PixelFormat.Format24bppRgb)
                        Dim snapshot As Bitmap = GetWindowImage(taskA.main_hwnd, r)
                        Dim snap = cvext.BitmapConverter.ToMat(snapshot)
                        snap = snap.CvtColor(cv.ColorConversionCodes.BGRA2BGR)
                        cvext.BitmapConverter.ToBitmap(snap, nextBMP)
                    Case gifTypes.openGLwindow
                        Dim r = New Rectangle(0, 0, taskA.sharpGL.Width, taskA.sharpGL.Height)
                        nextBMP = New Bitmap(r.Width, r.Height, Imaging.PixelFormat.Format32bppArgb)
                        Dim snapshot As Bitmap = New Bitmap(r.Width, r.Height, Imaging.PixelFormat.Format32bppArgb)
                        taskA.sharpGL.DrawToBitmap(snapshot, r)
                        Dim snap = cvext.BitmapConverter.ToMat(snapshot)
                        cvext.BitmapConverter.ToBitmap(snap, nextBMP)
                    Case gifTypes.EntireScreen
                        nextBMP = CaptureScreen()
                        Dim snap = cvext.BitmapConverter.ToMat(nextBMP)
                        ' snap = snap.CvtColor(cv.ColorConversionCodes.BGRA2BGR)
                        ' snap = snap.Resize(New cv.Size(snap.Width / 3, snap.Height / 3))
                        cvext.BitmapConverter.ToBitmap(snap, nextBMP)
                End Select
                taskA.gifImages.Add(nextBMP)
                snapCheck.checked = False
            End If
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            SetTrueText("Results are best when the main form is set to an 'auto-sized' setting.", 3)
            Static snapCheck = OptionParent.FindCheckBox("Step 1: Check this box when ready to capture the desired snapshot.")

            gifC.Run(dst2)

            labels(2) = "Images captured: " + CStr(taskA.gifImages.Count)
            labels(3) = "After 'Build GIF file...' was clicked, resulting gif will be in '" +
                    taskA.homeDir + "/temp/myGIF.gif'"
        End Sub
    End Class
End Namespace