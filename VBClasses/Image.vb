Imports System.IO
Imports System.Drawing
Imports cv = OpenCvSharp
' https://www.kaggle.com/datasets/balraj98/berkeley-segmentation-dataset-500-bsds500
Public Class Image_Basics : Inherits TaskParent
    Public inputFileName As String
    Public options As New Options_Images
    Public Sub New()
        desc = "Load an image into OpenCVB"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        src = options.fullsizeImage

        If src.Width <> dst2.Width Or src.Height <> dst2.Height Then
            Dim newSize = New cv.Size(dst2.Height * src.Width / src.Height, dst2.Height)
            If newSize.Width > dst2.Width Then
                newSize = New cv.Size(dst2.Width, dst2.Width * src.Height / src.Width)
            End If
            dst2.SetTo(0)
            dst2(New cv.Rect(0, 0, newSize.Width, newSize.Height)) = src.Resize(newSize)
        Else
            dst2 = src
        End If
    End Sub
End Class










Public Class Image_Series : Inherits TaskParent
    Public images As New Image_Basics
    Public Sub New()
        images.options.imageSeries = True
        desc = "Display a new image from the directory every heartbeat"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        ' to work on a specific file, specify it here.
        ' options.fileInputName = new fileinfo(task.HomeDir + "Images/train/103041.jpg")
        images.Run(images.options.fullsizeImage)
        dst2 = images.dst2
    End Sub
End Class










Public Class Image_RedCloudColor : Inherits TaskParent
    Public images As New Image_Series
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Use RedCloud on a photo instead of the video stream."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        images.Run(src)
        dst0 = images.dst2.Clone
        dst1 = images.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        dst2 = runRedC(src, labels(2))

        Dim mask = task.redC.rcMap.InRange(0, 0)
        dst2.SetTo(cv.Scalar.Black, mask)
    End Sub
End Class






Public Class Image_CellStats : Inherits TaskParent
    Dim images As New Image_RedCloudColor
    Dim stats As New RedCell_Basics
    Public Sub New()
        images.images.images.options.imageSeries = False
        If standalone Then task.gOptions.displaydst0.checked = true
        If standalone Then task.gOptions.displaydst1.checked = true
        desc = "Display the statistics for the selected cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.pointCloud.SetTo(0)
        task.pcSplit = task.pointCloud.Split()

        images.Run(src)
        dst0 = images.dst0
        dst1 = images.dst1
        dst2 = images.dst2

        stats.statsString()

        SetTrueText(stats.strOut, 3)
    End Sub
End Class








Public Class Image_MSER : Inherits TaskParent
    Public images As New Image_Series
    Dim core As New MSER_Detect
    Dim options As New Options_Images
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true
        OptionParent.FindSlider("MSER Min Area").Value = 15
        OptionParent.FindSlider("MSER Max Area").Value = 200000
        desc = "Find the MSER (Maximally Stable Extermal Regions) in the still image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        images.Run(options.fullsizeImage)
        dst1 = images.dst2
        core.Run(dst1)
        dst2 = core.dst2
    End Sub
End Class








Public Class Image_Icon : Inherits TaskParent
    Dim inputImage As Bitmap
    Public Sub New()
        inputImage = New Bitmap(task.HomeDir + "/Main/Data/OpenCVB.png")
        desc = "Create an icon from an image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If inputImage Is Nothing Then Exit Sub
        Dim iconHandle As IntPtr = inputImage.GetHicon()
        Dim icon As Icon = Icon.FromHandle(iconHandle)

        ' Save the icon to a file
        Using fs As New FileStream(task.HomeDir + "/Main/Data/test.ico", FileMode.OpenOrCreate)
            icon.Save(fs)
        End Using
        inputImage = Nothing
    End Sub
End Class
