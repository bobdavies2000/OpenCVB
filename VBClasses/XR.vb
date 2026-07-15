Imports System.IO
Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
' This class is a collection of algorithms that just don't justify having their own class.vb.

Public Class XR_ImShow_Basics : Inherits TaskParent
    Implements IDisposable
    Public Sub New()
        desc = "This is just a reminder that all HighGUI methods are available in OpenCVB"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.testAllRunning Then Exit Sub ' when testing, this can occasionally fail - mysterious.
        If src.Width > 0 Then ImShow("color", src)
    End Sub
    Protected Overrides Sub Finalize()
        If task.testAllRunning = False Then DestroyWindow("color")
    End Sub
End Class





Public Class XR_ImShow_CV32FC3 : Inherits TaskParent
    Implements IDisposable
    Public Sub New()
        desc = "Experimenting with how to show an 32fc3 Mat file."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.testAllRunning Then Exit Sub ' when testing, this can occasionally fail - mysterious.
        ImShow("Point cloud", task.pointCloud)
        dst2 = task.pointCloud.Clone
    End Sub
    Protected Overrides Sub Finalize()
        DestroyWindow("Point cloud")
    End Sub
End Class




Public Class XR_ImageOffset_Basics : Inherits TaskParent
    Public options As New Options_ImageOffset
    Dim options1 As New Options_Diff
    Public masks(2) As Mat
    Public dst(2) As Mat
    Public pcFiltered(2) As Mat
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst1 = New Mat(dst1.Size, MatType.CV_32FC1, New Scalar(0))
        dst2 = New Mat(dst2.Size, MatType.CV_32FC1, New Scalar(0))
        dst3 = New Mat(dst3.Size, MatType.CV_32FC1, New Scalar(0))
        desc = "Compute various differences between neighboring pixels"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        options1.Run()

        Dim r1 = New cv.Rect(1, 1, task.cols - 2, task.rows - 2)
        Dim r2 As cv.Rect
        Select Case options.offsetDirection
            Case "Upper Left"
                r2 = New cv.Rect(0, 0, r1.Width, r1.Height)
            Case "Above"
                r2 = New cv.Rect(1, 0, r1.Width, r1.Height)
            Case "Upper Right"
                r2 = New cv.Rect(2, 0, r1.Width, r1.Height)
            Case "Left"
                r2 = New cv.Rect(0, 1, r1.Width, r1.Height)
            Case "Right"
                r2 = New cv.Rect(2, 1, r1.Width, r1.Height)
            Case "Lower Left"
                r2 = New cv.Rect(0, 2, r1.Width, r1.Height)
            Case "Below"
                r2 = New cv.Rect(1, 2, r1.Width, r1.Height)
            Case "Below Right"
                r2 = New cv.Rect(2, 2, r1.Width, r1.Height)
        End Select

        Dim r3 = New cv.Rect(1, 1, r1.Width, r1.Height)

        Absdiff(task.pcSplit(0)(r1), task.pcSplit(0)(r2), dst1(r3))
        Absdiff(task.pcSplit(1)(r1), task.pcSplit(1)(r2), dst2(r3))
        Absdiff(task.pcSplit(2)(r1), task.pcSplit(2)(r2), dst3(r3))

        dst = {dst1, dst2, dst3}
        For i = 0 To dst.Count - 1
            If masks(i) Is Nothing Then masks(i) = New Mat
            Threshold(dst(i), masks(i), options1.pixelDiffThreshold, 255, ThresholdTypes.BinaryInv)
            ConvertScaleAbs(masks(i), masks(i))
            pcFiltered(i) = New Mat(src.Size, MatType.CV_32FC1, New Scalar(0))
            task.pcSplit(i).CopyTo(pcFiltered(i), masks(i))
        Next
    End Sub
End Class






Public Class XR_ImageOffset_SliceH : Inherits TaskParent
    Dim iOff As New XR_ImageOffset_Basics
    Dim plot As New PlotOpenCV_Points
    Dim options As New Options_SLR
    Dim slr As New SLR
    Dim mats As New Mat_4to1
    Public Sub New()
        labels(2) = "Upper left is pointcloud X, upper right pointcloud Y, bottom left pointcloud Z"
        desc = "Visualize a slice through the ImageOffsets_Basics images"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        iOff.Run(src)

        Dim pt = task.mouseMovePoint
        If standalone And task.mouseMovePoint.X = 0 And task.mouseMovePoint.Y = 0 Then
            pt = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        End If

        Dim slice As Mat
        For i = 0 To 2
            slice = iOff.pcFiltered(i).Row(pt.Y)
            Dim inputX As New List(Of Double)
            Dim inputY As New List(Of Double)
            For j = 0 To dst2.Width - 1
                inputX.Add(j)
                inputY.Add(slice.Get(Of Single)(0, j))
            Next

            Dim outputX As New List(Of Double)
            Dim outputY As New List(Of Double)
            slr.SegmentedRegressionFast(inputX, inputY, options.tolerance, options.halfLength,
                                            outputX, outputY)

            plot.input.Clear()
            For j = 0 To outputX.Count - 1
                plot.input.Add(New Point2d(CDbl(outputX(j)), CDbl(outputY(j))))
            Next

            plot.minY = Choose(i + 1, -task.xRange, -task.yRange, 0)
            plot.maxY = Choose(i + 1, task.xRange, task.yRange, task.MaxZmeters)
            plot.Run(src)

            mats.mat(i) = plot.dst2.Clone
        Next

        mats.Run(emptyMat)
        dst2 = mats.dst2

        Dim p1 = New cv.Point(0, pt.Y), p2 = New cv.Point(dst2.Width, pt.Y)
        Line(task.color, p1, p2, task.highlight, task.lineWidth)
        Line(task.depthRGB, p1, p2, task.highlight, task.lineWidth)
    End Sub
End Class







Public Class XR_ImageOffset_SliceV : Inherits TaskParent
    Dim iOff As New XR_ImageOffset_Basics
    Dim plot As New PlotOpenCV_Points
    Dim options As New Options_SLR
    Dim slr As New SLR
    Dim mats As New Mat_4to1
    Public Sub New()
        labels(2) = "Upper left is pointcloud X, upper right pointcloud Y, bottom left pointcloud Z"
        desc = "Visualize a slice through the ImageOffsets_Basics images"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        iOff.Run(src)

        Dim pt = task.mouseMovePoint
        If standalone And task.mouseMovePoint.X = 0 And task.mouseMovePoint.Y = 0 Then
            pt = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        End If

        Dim slice As Mat
        For i = 0 To 2
            slice = iOff.pcFiltered(i).Col(pt.X)
            Dim inputX As New List(Of Double)
            Dim inputY As New List(Of Double)
            For j = 0 To dst2.Height - 1
                inputX.Add(CDbl(j))
                inputY.Add(CDbl(slice.Get(Of Single)(j, 0)))
            Next

            Dim outputX As New List(Of Double)
            Dim outputY As New List(Of Double)
            slr.SegmentedRegressionFast(inputX, inputY, options.tolerance, options.halfLength,
                                            outputX, outputY)

            plot.input.Clear()
            For j = 0 To outputX.Count - 1
                plot.input.Add(New Point2d(CDbl(outputX(j)), CDbl(outputY(j))))
            Next

            plot.minY = Choose(i + 1, -task.xRange, -task.yRange, 0)
            plot.maxY = Choose(i + 1, task.xRange, task.yRange, task.MaxZmeters)
            plot.Run(src)
            mats.mat(i) = plot.dst2.Clone
        Next

        mats.Run(emptyMat)
        dst2 = mats.dst2

        Dim p1 = New cv.Point(pt.X, 0), p2 = New cv.Point(pt.X, dst2.Height)
        Line(task.color, p1, p2, task.highlight, task.lineWidth)
        Line(task.depthRGB, p1, p2, task.highlight, task.lineWidth)
    End Sub
End Class





Public Class XR_ImageOffset_Cloud : Inherits TaskParent
    Public Sub New()
        desc = "Create a pointcloud with the results of the imageOffset slices"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
    End Sub
End Class




' https://www.kaggle.com/datasets/balraj98/berkeley-segmentation-dataset-500-bsds500
Public Class XR_Image_Basics : Inherits TaskParent
    Public inputFileName As String
    Public options As New Options_Images
    Public Sub New()
        desc = "Load an image into OpenCVB"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        src = options.fullsizeImage

        If src.Width <> dst2.Width Or src.Height <> dst2.Height Then
            Dim newSize = New Size(dst2.Height * src.Width / src.Height, dst2.Height)
            If newSize.Width > dst2.Width Then
                newSize = New Size(dst2.Width, dst2.Width * src.Height / src.Width)
            End If
            dst2.SetTo(0)
            Resize(src, dst2(New cv.Rect(0, 0, newSize.Width, newSize.Height)), newSize)
        Else
            dst2 = src
        End If
    End Sub
End Class










Public Class XR_Image_Series : Inherits TaskParent
    Public images As New XR_Image_Basics
    Public Sub New()
        images.options.imageSeries = True
        desc = "Display a new image from the directory every heartbeat"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        ' to work on a specific file, specify it here.
        ' options.fileInputName = new fileinfo(task.homeDir + "Images/train/103041.jpg")
        images.Run(images.options.fullsizeImage)
        dst2 = images.dst2
    End Sub
End Class










Public Class XR_Image_RedCloudColor : Inherits TaskParent
    Public images As New XR_Image_Series
    Dim redC As New RedColor_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        task.fOptions.ReductionColor.Value = 50
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Use RedCloud on a photo instead of the video stream."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        images.Run(src)
        dst0 = images.dst2.Clone
        CvtColor(images.dst2, dst1, ColorConversionCodes.BGR2GRAY)

        reduction.Run(dst1)

        redC.Run(reduction.dst2)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        InRange(dst1, 0, 0, dst0)
        dst2.SetTo(0, dst0)
    End Sub
End Class








Public Class XR_Image_MSER : Inherits TaskParent
    Public images As New XR_Image_Series
    Dim core As New MSER_Detect
    Dim options As New Options_Images
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
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








Public Class XR_Image_Icon : Inherits TaskParent
    Dim inputImage As Bitmap
    Public Sub New()
        Dim filePath As String = task.homeDir + "/MainUI/Data/Magnify.png"
        inputImage = New Bitmap(filePath)
        desc = "Create an icon from an image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If inputImage Is Nothing Then Exit Sub
        Dim iconHandle As IntPtr = inputImage.GetHicon()
        Dim icon As Icon = Icon.FromHandle(iconHandle)

        ' Save the icon to a file
        Using fs As New FileStream(task.homeDir + "/MainUI/Data/test.ico", FileMode.OpenOrCreate)
            icon.Save(fs)
        End Using
        inputImage = Nothing
    End Sub
End Class
