Imports System.IO
Imports cv = OpenCvSharp
' https://www.kaggle.com/datasets/balraj98/berkeley-segmentation-dataset-500-bsds500
Public Class Image_Basics : Inherits VB_Algorithm
    Public fileNameForm As OptionsFileName
    Public inputFileName As String
    Public Sub New()
        fileNameForm = New OptionsFileName
        fileNameForm.OpenFileDialog1.InitialDirectory = task.homeDir + "Images/train"
        fileNameForm.OpenFileDialog1.FileName = "*.*"
        fileNameForm.OpenFileDialog1.CheckFileExists = False
        fileNameForm.OpenFileDialog1.Filter = "jpg (*.jpg)|*.jpg|png (*.png)|*.png|bmp (*.bmp)|*.bmp|All files (*.*)|*.*"
        fileNameForm.OpenFileDialog1.FilterIndex = 1
        fileNameForm.filename.Text = GetSetting("OpenCVB1", "Image_Basics_Name", "Image_Basics_Name", task.homeDir + "Images/train/2092.jpg")
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

            ' SaveSetting("OpenCVB1", "Image_Basics_Name", "Image_Basics_Name", fileInputName.FullName)
        End If
    End Sub
End Class










Public Class Image_Series : Inherits VB_Algorithm
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
        If task.optionsChanged Then
            If gOptions.DebugCheckBox.Checked Then fileIndex += 1
            gOptions.DebugCheckBox.Checked = False
            If fileIndex >= fileNameList.Count Then fileIndex = 0

            images.fileNameForm.filename.Text = fileNameList(fileIndex)

            ' to work on a specific file, specify it here.
            ' images.fileNameForm.filename.Text = task.homeDir + "Images/train/103041.jpg"

            images.Run(Nothing)
            dst2 = images.dst2
        End If
    End Sub
End Class










Public Class Image_RedCloudColor : Inherits VB_Algorithm
    Public images As New Image_Series
    Public redc As New RedCloud_ColorOnly
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Use RedCloud on a photo instead of the video stream."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        images.Run(Nothing)
        dst0 = images.dst2.Clone
        dst1 = images.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        redc.Run(dst0)
        dst2 = redc.dst2

        Dim mask = task.cellMap.InRange(task.redOther, task.redOther)
        dst2.SetTo(cv.Scalar.Black, mask)

        redc.colorC.redSelect(dst0, dst1, dst2)

        labels(2) = redc.labels(2)
    End Sub
End Class











Public Class Image_RedCloudColorSeries : Inherits VB_Algorithm
    Dim images As New Image_RedCloudColor
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Use RedCloud on a series of photos instead of the video stream."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If heartBeat() Then gOptions.DebugCheckBox.Checked = True
        images.Run(Nothing)
        dst0 = images.dst0
        dst1 = images.dst1
        dst2 = images.dst2
        dst3 = images.dst3
        labels(2) = images.images.fileInputName.Name
    End Sub
End Class







Public Class Image_CellStats : Inherits VB_Algorithm
    Dim images As New Image_RedCloudColor
    Dim stats As New RedCloud_CellStats
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        If standalone Then gOptions.displayDst1.Checked = True
        stats.redC = New RedCloud_ColorOnly
        desc = "Display the statistics for the selected cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.pointCloud.SetTo(0)
        task.pcSplit = task.pointCloud.Split()

        images.Run(Nothing)
        dst0 = images.dst0
        dst1 = images.dst1
        dst2 = images.dst2

        stats.statsString(src)

        setTrueText(stats.strOut, 3)
    End Sub
End Class










Public Class ReductionCloud_KMeans : Inherits VB_Algorithm
    Dim km As New KMeans_MultiChannel
    Dim colorC As New ReductionCloud_Basics
    Public Sub New()
        labels = {"", "", "KMeans_MultiChannel output", "RedColor_Basics output"}
        desc = "Use RedCloud to identify the regions created by kMeans"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        km.Run(src)
        dst2 = km.dst2

        colorC.Run(km.dst3)
        dst3 = colorC.dst2
    End Sub
End Class









Public Class ReductionCloud_LineID : Inherits VB_Algorithm
    Public lines As New Line_Basics
    Public rCells As New List(Of rcData)
    Dim p1list As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalInteger)
    Dim p2list As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalInteger)
    Dim rectList As New List(Of cv.Point)
    Dim maxDistance As Integer
    Public colorC As New ReductionCloud_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Width of line detected in the image", 1, 10, 2)
            sliders.setupTrackBar("Width of Isolating line", 2, 10, 5)
            sliders.setupTrackBar("Max distance between point and rect", 1, 20, 10)
        End If

        gOptions.useMotion.Checked = False
        labels(3) = "Input to RedCloud"
        desc = "Identify and isolate each line in the current image"
    End Sub
    Private Function connectDistance(rpt As cv.Point) As Integer
        For i = 0 To p1list.Count - 1
            Dim dist = p1list.ElementAt(i).Value.DistanceTo(rpt)
            If dist < maxDistance Then Return i
        Next
        Return -1
    End Function
    Public Sub RunVB(src As cv.Mat)
        Static lineSlider = findSlider("Width of line detected in the image")
        Static isoSlider = findSlider("Width of Isolating line")
        Static distSlider = findSlider("Max distance between point and rect")
        Dim lineWidth = lineSlider.Value
        Dim isolineWidth = isoSlider.Value
        maxDistance = distSlider.Value

        lines.Run(src)
        If lines.sortLength.Count = 0 Then Exit Sub

        Static rInput = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
        If heartBeat() Then rInput.setto(0)
        p1list.Clear()
        For i = lines.sortLength.Count - 1 To 0 Step -1
            Dim mps = lines.mpList(lines.sortLength.ElementAt(i).Value)
            rInput.Line(mps.p1, mps.p2, 0, isolineWidth, cv.LineTypes.Link4)
            rInput.Line(mps.p1, mps.p2, 255, lineWidth, cv.LineTypes.Link4)
            p1list.Add(mps.p1.Y, mps.p1)
        Next

        If heartBeat() Then
            If rInput.Type = cv.MatType.CV_32SC1 Then rInput.convertto(rInput, cv.MatType.CV_8U)

            colorC.Run(rInput)
            dst2.SetTo(0)
            For Each rc In colorC.redCells
                If rc.rect.Width = 0 Or rc.rect.Height = 0 Then Continue For
                If rc.rect.Width < dst2.Width / 2 Or rc.rect.Height < dst2.Height / 2 Then dst2(rc.rect).SetTo(rc.color, rc.mask)
            Next

            If colorC.redCells.Count < 3 Then Exit Sub ' dark room - no cells.

            Dim rcLargest As New rcData
            For Each rc In colorC.redCells
                If rc.rect.Width > dst2.Width / 2 And rc.rect.Height > dst2.Height / 2 Then Continue For
                If rc.pixels > rcLargest.pixels Then rcLargest = rc
            Next

            dst2.Rectangle(rcLargest.rect, cv.Scalar.Yellow, task.lineWidth + 1, task.lineType)
            labels(2) = CStr(colorC.redCells.Count) + " lines were identified.  Largest line detected is highlighted in yellow"
        End If
    End Sub
End Class










Public Class ReductionCloud_KNNCenters : Inherits VB_Algorithm
    Dim lines As New ReductionCloud_LineID
    Dim knn As New KNN_Lossy
    Dim ptTrace As New List(Of List(Of cv.Point))
    Public Sub New()
        labels = {"", "", "Line_ID output", "KNN_Basics output"}
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "use the mid-points in each line with KNN and identify the movement in each line"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2

        knn.queries.Clear()
        For Each rc In lines.colorC.redCells
            knn.queries.Add(rc.maxDist)
        Next

        knn.Run(Nothing)

        Dim trace As New List(Of cv.Point2f)
        Static regularPt As New List(Of cv.Point2f)
        If heartBeat() Then
            dst3.SetTo(0)
            regularPt.Clear()
        End If
        Dim preciseCount As Integer

        For i = 0 To knn.matches.Count - 1
            Dim mps = knn.matches(i)
            Dim distance = mps.p1.DistanceTo(mps.p2)
            If distance <= 2 Then
                regularPt.Add(mps.p1)
                dst3.Set(Of Byte)(mps.p2.Y, mps.p2.X, 255)
                preciseCount += 1
            End If
        Next
        labels(3) = CStr(preciseCount) + " of " + CStr(knn.matches.Count) + " KNN_One_To_One matches"
    End Sub
End Class







Public Class ReductionCloud_ProjectCell : Inherits VB_Algorithm
    Dim topView As New Histogram_ShapeTop
    Dim sideView As New Histogram_ShapeSide
    Dim mats As New Mat_4Click
    Dim colorC As New ReductionCloud_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Top: XZ values and mask, Bottom: ZY values and mask"
        desc = "Visualize the top and side projection of a RedCloud cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        colorC.Run(src)
        dst1 = colorC.dst2

        labels(2) = colorC.labels(2)

        Dim rc = task.rcSelect

        Dim pc = New cv.Mat(rc.rect.Height, rc.rect.Width, cv.MatType.CV_32FC3, 0)
        task.pointCloud(rc.rect).CopyTo(pc, rc.mask)

        topView.rc = rc
        topView.Run(pc)

        sideView.rc = rc
        sideView.Run(pc)

        mats.mat(0) = topView.dst2
        mats.mat(1) = topView.dst3
        mats.mat(2) = sideView.dst2
        mats.mat(3) = sideView.dst3
        mats.Run(Nothing)
        dst2 = mats.dst2
        dst3 = mats.dst3

        Dim padX = dst2.Width / 15
        Dim padY = dst2.Height / 20
        strOut = "Top" + vbTab + "Top Mask" + vbCrLf + vbCrLf + "Side" + vbTab + "Side Mask"
        setTrueText(strOut, New cv.Point(dst2.Width / 2 - padX, dst2.Height / 2 - padY), 2)
        setTrueText("Select a RedCloud cell above to project it into the top and side views at left.", 3)
    End Sub
End Class











Public Class ReductionCloud_ContourCorners : Inherits VB_Algorithm
    Public corners(4 - 1) As cv.Point
    Public rc As New rcData
    Public Sub New()
        labels(2) = "The RedCloud Output with the highlighted contour to smooth"
        desc = "Find the point farthest from the center in each cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            Static colorC As New ReductionCloud_Basics
            colorC.Run(src)
            dst2 = colorC.dst2
            labels(2) = colorC.labels(2)
            rc = task.rcSelect
        End If

        dst3.SetTo(0)
        dst3.Circle(rc.maxDist, task.dotSize, cv.Scalar.White, task.lineWidth)
        Dim center As New cv.Point(rc.maxDist.X - rc.rect.X, rc.maxDist.Y - rc.rect.Y)
        Dim maxDistance(4 - 1) As Single
        For i = 0 To corners.Length - 1
            corners(i) = center ' default is the center - a triangle shape can omit a corner
        Next
        If rc.contour Is Nothing Then Exit Sub
        For Each pt In rc.contour
            Dim quad As Integer
            If pt.X - center.X >= 0 And pt.Y - center.Y <= 0 Then quad = 0 ' upper right quadrant
            If pt.X - center.X >= 0 And pt.Y - center.Y >= 0 Then quad = 1 ' lower right quadrant
            If pt.X - center.X <= 0 And pt.Y - center.Y >= 0 Then quad = 2 ' lower left quadrant
            If pt.X - center.X <= 0 And pt.Y - center.Y <= 0 Then quad = 3 ' upper left quadrant
            Dim dist = center.DistanceTo(pt)
            If dist > maxDistance(quad) Then
                maxDistance(quad) = dist
                corners(quad) = pt
            End If
        Next

        vbDrawContour(dst3(rc.rect), rc.contour, cv.Scalar.White)
        For i = 0 To corners.Count - 1
            dst3(rc.rect).Line(center, corners(i), cv.Scalar.White, task.lineWidth, task.lineType)
        Next
    End Sub
End Class
