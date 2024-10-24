﻿Imports System.IO
Imports System.Drawing
Imports cvb = OpenCvSharp
' https://www.kaggle.com/datasets/balraj98/berkeley-segmentation-dataset-500-bsds500
Public Class Image_Basics : Inherits TaskParent
    Public inputFileName As String
    Public options As New Options_Images
    Public Sub New()
        desc = "Load an image into OpenCVB"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        src = options.fullsizeImage

        If src.Width <> dst2.Width Or src.Height <> dst2.Height Then
            Dim newSize = New cvb.Size(dst2.Height * src.Width / src.Height, dst2.Height)
            If newSize.Width > dst2.Width Then
                newSize = New cvb.Size(dst2.Width, dst2.Width * src.Height / src.Width)
            End If
            dst2.SetTo(0)
            dst2(New cvb.Rect(0, 0, newSize.Width, newSize.Height)) = src.Resize(newSize)
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
    Public Sub RunAlg(src As cvb.Mat)
        ' to work on a specific file, specify it here.
        ' options.fileInputName = new fileinfo(task.HomeDir + "Images/train/103041.jpg")
        images.Run(images.options.fullsizeImage)
        dst2 = images.dst2
    End Sub
End Class










Public Class Image_RedCloudColor : Inherits TaskParent
    Public images As New Image_Series
    Public redC As New RedCloud_Cells
    Public Sub New()
        task.gOptions.setDisplay1()
        desc = "Use RedCloud on a photo instead of the video stream."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        images.Run(empty)
        dst0 = images.dst2.Clone
        dst1 = images.dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        redC.Run(dst0)
        dst2 = redC.dst2

        Dim mask = task.cellMap.InRange(0, 0)
        dst2.SetTo(cvb.Scalar.Black, mask)

        labels(2) = redC.labels(2)
    End Sub
End Class






Public Class Image_CellStats : Inherits TaskParent
    Dim images As New Image_RedCloudColor
    Dim stats As New Cell_Basics
    Public Sub New()
        images.images.images.options.imageSeries = False
        If standaloneTest() Then task.gOptions.setDisplay0()
        If standaloneTest() Then task.gOptions.setDisplay1()
        task.redOptions.setUseColorOnly(True)
        desc = "Display the statistics for the selected cell"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        task.pointCloud.SetTo(0)
        task.pcSplit = task.pointCloud.Split()

        images.Run(empty)
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
        If standaloneTest() Then task.gOptions.setDisplay1()
        FindSlider("MSER Min Area").Value = 15
        FindSlider("MSER Max Area").Value = 200000
        desc = "Find the MSER (Maximally Stable Extermal Regions) in the still image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        images.Run(options.fullsizeImage)
        dst1 = images.dst2
        core.Run(dst1)
        dst2 = core.dst2
    End Sub
End Class








Public Class Image_Icon : Inherits TaskParent
    Dim inputImage As bitmap
    Public Sub New()
        inputImage = New Bitmap(task.HomeDir + "/Main_UI/Data/OpenCVB.bmp")
        desc = "Create an icon from an image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If inputImage Is Nothing Then Exit Sub
        Dim iconHandle As IntPtr = inputImage.GetHicon()
        Dim icon As Icon = Icon.FromHandle(iconHandle)

        ' Save the icon to a file
        Using fs As New FileStream(task.HomeDir + "/Main_UI/Data/OpenCVB.ico", FileMode.OpenOrCreate)
            icon.Save(fs)
        End Using
        inputImage = Nothing
    End Sub
End Class
