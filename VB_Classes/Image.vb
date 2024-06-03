Imports System.IO
Imports cv = OpenCvSharp
' https://www.kaggle.com/datasets/balraj98/berkeley-segmentation-dataset-500-bsds500
Public Class Image_Basics : Inherits VB_Parent
    Public fileNameForm As OptionsFileName
    Public inputFileName As String
    Public Sub New()
        fileNameForm = New OptionsFileName
        fileNameForm.OpenFileDialog1.InitialDirectory = task.homeDir + "Images/train"
        fileNameForm.OpenFileDialog1.FileName = "*.*"
        fileNameForm.OpenFileDialog1.CheckFileExists = False
        fileNameForm.OpenFileDialog1.Filter = "jpg (*.jpg)|*.jpg|png (*.png)|*.png|bmp (*.bmp)|*.bmp|All files (*.*)|*.*"
        fileNameForm.OpenFileDialog1.FilterIndex = 1
        fileNameForm.filename.Text = GetSetting("OpenCVB", "Image_Basics_Name", "Image_Basics_Name", task.homeDir + "Images/train/2092.jpg")
        fileNameForm.Text = "Select an image file for use in OpenCVB"
        fileNameForm.FileNameLabel.Text = "Select a file."
        fileNameForm.PlayButton.Hide()
        fileNameForm.TrackBar1.Hide()
        fileNameForm.Setup(traceName)
        fileNameForm.Show()

        desc = "Load an image into OpenCVB"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static fileInputName As FileInfo
        fileInputName = New FileInfo(fileNameForm.filename.Text)
        If inputFileName <> fileInputName.FullName Or task.optionsChanged Then
            inputFileName = fileInputName.FullName
            If fileInputName.Exists = False Then
                labels(2) = "No input file specified or file not found."
                Exit Sub
            End If

            Dim fullsizeImage = cv.Cv2.ImRead(fileInputName.FullName)
            If fullsizeImage.Width <> dst2.Width Or fullsizeImage.Height <> dst2.Height Then
                Dim newSize = New cv.Size(dst2.Height * fullsizeImage.Width / fullsizeImage.Height, dst2.Height)
                If newSize.Width > dst2.Width Then
                    newSize = New cv.Size(dst2.Width, dst2.Width * fullsizeImage.Height / fullsizeImage.Width)
                End If
                dst2.SetTo(0)
                dst2(New cv.Rect(0, 0, newSize.Width, newSize.Height)) = fullsizeImage.Resize(newSize)
            Else
                dst2 = fullsizeImage
            End If

            ' SaveSetting("OpenCVB", "Image_Basics_Name", "Image_Basics_Name", fileInputName.FullName)
        End If
    End Sub
End Class










Public Class Image_Series : Inherits VB_Parent
    Dim images As New Image_Basics
    Dim fileIndex As Integer
    Public fileInputName As FileInfo
    Dim fileNameList As New List(Of String)
    Public Sub New()
        fileInputName = New FileInfo(images.fileNameForm.filename.Text)

        Dim dirName = fileInputName.Directory
        Dim fileList As IO.FileInfo() = dirName.GetFiles("*.jpg")
        For Each file In fileList
            fileNameList.Add(file.FullName)
        Next

        desc = "Display a new image from the directory every heartbeat"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If task.optionsChanged Or loadNextImage Then
            If loadNextImage Then fileIndex += 1
            loadNextImage = False
            If fileIndex >= fileNameList.Count Then fileIndex = 0

            images.fileNameForm.filename.Text = fileNameList(fileIndex)

            ' to work on a specific file, specify it here.
            ' images.fileNameForm.filename.Text = task.homeDir + "Images/train/103041.jpg"

            images.Run(empty)
            dst2 = images.dst2
        End If
    End Sub
End Class










Public Class Image_RedCloudColor : Inherits VB_Parent
    Public images As New Image_Series
    Public redC As New RedCloud_Cells
    Public Sub New()
        gOptions.displayDst0.Checked = True
        desc = "Use RedCloud on a photo instead of the video stream."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        images.Run(empty)
        dst0 = images.dst2.Clone
        dst1 = images.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        redC.Run(dst0)
        dst2 = redC.dst2

        Dim mask = task.cellMap.InRange(0, 0)
        dst2.SetTo(cv.Scalar.Black, mask)

        labels(2) = redC.labels(2)
    End Sub
End Class











Public Class Image_RedCloudColorSeries : Inherits VB_Parent
    Dim images As New Image_RedCloudColor
    Public Sub New()
        desc = "Use RedCloud on a series of photos instead of the video stream."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then loadNextImage = True
        images.Run(empty)
        dst0 = images.dst0
        dst1 = images.dst1
        dst2 = images.dst2
        dst3 = images.dst3
        labels(2) = images.images.fileInputName.Name
    End Sub
End Class







Public Class Image_CellStats : Inherits VB_Parent
    Dim images As New Image_RedCloudColor
    Dim stats As New Cell_Basics
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst0.Checked = True
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        redOptions.UseColorOnly.Checked = True
        desc = "Display the statistics for the selected cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.pointCloud.SetTo(0)
        task.pcSplit = task.pointCloud.Split()

        images.Run(empty)
        dst0 = images.dst0
        dst1 = images.dst1
        dst2 = images.dst2

        stats.statsString()

        setTrueText(stats.strOut, 3)
    End Sub
End Class






Module Image_Variables
    Public loadNextImage As Boolean
End Module






Public Class Image_MSER : Inherits VB_Parent
    Public images As New Image_Series
    Dim core As New MSER_Detect
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst0.Checked = True
        If findfrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Load the next image")
        End If

        findSlider("MSER Min Area").Value = 15
        findSlider("MSER Max Area").Value = 200000
        desc = "Find the MSER (Maximally Stable Extermal Regions) in the still image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static nextCheck = findCheckBox("Load the next image")
        loadNextImage = nextCheck.checked
        nextCheck.checked = False

        images.Run(empty)
        dst0 = images.dst2

        core.Run(dst0)
        dst2 = core.dst2
    End Sub
End Class
