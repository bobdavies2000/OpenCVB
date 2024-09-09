Imports OpenCvSharp
Imports OpenCvSharp.Extensions
Imports System.Windows.Forms

Public Class Form1
    Private captureLeft As VideoCapture
    Private captureRight As VideoCapture
    Private captureDepth As VideoCapture
    Private timer As Timer
    Dim bitmapLeft As Bitmap
    Dim bitmapRight As Bitmap
    Dim bitmapDepth As Bitmap
    Dim frameLeft As Mat = New Mat()
    Dim frameRight As Mat = New Mat()
    Dim frameDepth As Mat = New Mat()

    Public Sub New()
        InitializeComponent()

        ' Initialize the VideoCapture objects
        captureLeft = New VideoCapture(0) ' ID for the left camera
        captureRight = New VideoCapture(2) ' ID for the right camera
        captureDepth = New VideoCapture(1) ' ID for the depth camera (adjust as needed)

        ' Initialize the Timer
        timer = New Timer()
        AddHandler timer.Tick, AddressOf Timer_Tick
        timer.Interval = 30 ' Set the interval to 30 ms (approx. 33 FPS)
        timer.Start()
    End Sub

    Private Sub Timer_Tick(sender As Object, e As EventArgs)

        captureLeft.Read(frameLeft)
        captureRight.Read(frameRight)
        captureDepth.Read(frameDepth)

        If Not frameLeft.Empty() Then
            ' Convert the Mat to a Bitmap
            bitmapLeft = BitmapConverter.ToBitmap(frameLeft)
            ' Display the Bitmap in the PictureBox for the left camera
            PictureBox1.Image = bitmapLeft
        End If

        If Not frameRight.Empty() Then
            ' Convert the Mat to a Bitmap
            bitmapRight = BitmapConverter.ToBitmap(frameRight)
            ' Display the Bitmap in the PictureBox for the right camera
            PictureBox2.Image = bitmapRight
        End If

        If Not frameDepth.Empty() Then
            ' Convert the Mat to a Bitmap
            bitmapDepth = BitmapConverter.ToBitmap(frameDepth)
            ' Display the Bitmap in the PictureBox for the depth camera
            PictureBox3.Image = bitmapDepth
        End If

        Static frameCount As Integer
        frameCount += 1
        If frameCount Mod 30 = 0 Then
            GC.Collect()
            GC.WaitForPendingFinalizers()
            GC.Collect()
        End If
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        MyBase.OnFormClosing(e)

        ' Release the VideoCapture objects
        captureLeft.Release()
        captureRight.Release()
        captureDepth.Release()
    End Sub
End Class