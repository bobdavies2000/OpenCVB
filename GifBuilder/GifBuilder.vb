Imports System.IO
Imports System.Windows
Imports System.Windows.Interop.Imaging
Imports System.Windows.Media.Imaging
Imports System.ComponentModel
Public Class GifBuilder
    Private Sub GifBuilder_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim strFileSize As String = ""
        Dim imgDir As New DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory + "/../../../../Temp/")
        Dim imgList As FileInfo() = imgDir.GetFiles("*.bmp")

        If imgList.Count = 0 Then
            MessageBox.Show("Use the Global Option 'Create GIF of current algorithm' to create input images to GifBuilder.")
            End
        End If

        Dim images As New List(Of System.Drawing.Bitmap)
        For Each imgFile In imgList
            Dim bmp = System.Drawing.Bitmap.FromFile(imgFile.FullName)
            images.Add(bmp)
            Console.WriteLine("File Name: {0}", imgFile.Name)
            Console.WriteLine("File Full Name: {0}", imgFile.FullName)
            Console.WriteLine("File Extension: {0}", imgFile.Extension)
            Console.WriteLine("Last Accessed: {0}", imgFile.LastAccessTime)
        Next
        Dim encoder = New GifBitmapEncoder()
        For Each bmp In images
            Dim hand = bmp.GetHbitmap()
            Dim endSrc = CreateBitmapSourceFromHBitmap(hand, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
            encoder.Frames.Add(BitmapFrame.Create(endSrc))
            bmp.Dispose()
        Next

        Dim mstream = New MemoryStream()
        encoder.Save(mstream)
        Dim filebytes = mstream.ToArray
        ' NETSCAPE2.0 application extension
        Dim appExtention As Byte() = {33, 255, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 0, 0, 0}
        Dim newBytes = New List(Of Byte)
        newBytes.AddRange(filebytes.Take(13))
        newBytes.AddRange(appExtention)
        newBytes.AddRange(filebytes.Skip(13))
        File.WriteAllBytes(imgDir.FullName + "\myGIF.gif", newBytes.ToArray)
        Dim displayProcess As New Process
        displayProcess.StartInfo.FileName = "cmd.exe"
        displayProcess.StartInfo.Arguments = "/c myGif.gif"
        displayProcess.StartInfo.WorkingDirectory = imgDir.FullName
        displayProcess.Start()
        End
    End Sub
End Class
