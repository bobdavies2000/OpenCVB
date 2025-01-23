Imports System.IO
Imports AnimatedGif
Module GifBuilder
    Sub Main()
        Dim strFileSize As String = ""
        Dim imgDir As New DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory + "../../../../Temp/")
        Dim imgList As FileInfo() = imgDir.GetFiles("*.bmp")
        Dim imageFiles As New List(Of String)
        For Each img In imgList
            imageFiles.Add(img.FullName)
        Next

        Dim outputGifPath As String = "../../../../Temp/myGif.gif"

        If imgList.Count = 0 Then
            MsgBox("Use the Global Option 'Create GIF of current algorithm' to create input images to GifBuilder.")
            End
        End If

        Try
            CreateAnimatedGif(imageFiles, outputGifPath, 1000) ' 1 seconds delay
        Catch ex As Exception
            Console.WriteLine("Error creating GIF: " & ex.Message)
        End Try
    End Sub

    Public Sub CreateAnimatedGif(imagePaths As List(Of String), outputPath As String, delay As Integer)
        If imagePaths Is Nothing OrElse imagePaths.Count = 0 Then
            Throw New ArgumentException("At least one image path is required.")
        End If

        If String.IsNullOrEmpty(outputPath) Then
            Throw New ArgumentException("Output path cannot be null or empty.")
        End If

        ' Create a new Animated GIF encoder
        Dim encoder As New AnimatedGifCreator(outputPath, delay) ' Delay is in milliseconds
        'Alternative NGif.NET
        'Dim encoder As New GifEncoder(outputPath, delay)
        'Alternative AnimatedGif
        'Using encoder As New GifEncoder(outputPath, delay:=delay, repeat:=0)

        Try
            ' Loop through each image path
            For Each imagePath As String In imagePaths
                ' Load the image using System.Drawing.Image
                Using img As System.Drawing.Image = System.Drawing.Image.FromFile(imagePath)
                    ' Add the frame to the GIF
                    encoder.AddFrame(img)
                End Using
            Next

        Finally
            ' Finalize the GIF encoding (important to close the stream properly)
            encoder.Dispose()
        End Try
    End Sub
End Module