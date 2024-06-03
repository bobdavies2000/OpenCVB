Imports cv = OpenCvSharp
Imports  System.IO
' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_Basics : Inherits VB_Parent
    Public srcVideo As String
    Public captureVideo As New cv.VideoCapture
    Public fileNameForm As OptionsFileName
    Dim fileInfo As FileInfo
    Public Sub New()
        fileNameForm = New OptionsFileName
        fileNameForm.OpenFileDialog1.InitialDirectory = task.homeDir + "Data\"
        fileNameForm.OpenFileDialog1.FileName = "*.*"
        fileNameForm.OpenFileDialog1.CheckFileExists = False
        fileNameForm.OpenFileDialog1.Filter = "video files (*.mp4)|*.mp4|All files (*.*)|*.*"
        fileNameForm.OpenFileDialog1.FilterIndex = 1
        fileNameForm.filename.Text = GetSetting("OpenCVB", "VideoFileName", "VideoFileName", task.homeDir + "Data\CarsDrivingUnderBridge.mp4")
        fileNameForm.Text = "Select a video file for input"
        fileNameForm.FileNameLabel.Text = "Select a video file for input"
        fileNameForm.PlayButton.Hide()
        fileNameForm.Setup(traceName)
        fileNameForm.Show()

        fileInfo = New FileInfo(fileNameForm.filename.Text)
        srcVideo = fileInfo.FullName

        captureVideo = New cv.VideoCapture(fileInfo.FullName)
        labels(2) = fileInfo.Name
        desc = "Show a video file"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If srcVideo <> fileNameForm.filename.Text Then
            If fileInfo.Exists = False Then
                setTrueText("File not found: " + fileInfo.FullName, New cv.Point(10, 125))
                Exit Sub
            End If
            srcVideo = fileNameForm.filename.Text
            captureVideo = New cv.VideoCapture(fileNameForm.filename.Text)
        End If
        captureVideo.Read(dst1)
        If dst1.Empty() Then
            captureVideo.Dispose()
            captureVideo = New cv.VideoCapture(fileNameForm.filename.Text)
            captureVideo.Read(dst1)
        End If

        fileNameForm.TrackBar1.Maximum = captureVideo.FrameCount
        fileNameForm.TrackBar1.Value = captureVideo.PosFrames
        dst2 = dst1.Resize(dst1.Size())
    End Sub
End Class






' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_CarCounting : Inherits VB_Parent
    Dim flow As New Font_FlowText
    Dim video As New Video_Basics
    Dim bgSub As New BGSubtract_MOG
    Public Sub New()
        desc = "Count cars in a video file"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        video.Run(src)
        dst2.SetTo(0)
        bgSub.Run(video.dst1) ' use the original size of the video input - not the dst2 size...
        Dim videoImage = bgSub.dst2
        dst3 = video.dst2

        ' there are 5 lanes of traffic so setup 5 regions
        ' NOTE: if long shadows are present this approach will not work without provision for the width of a car.  Needs more sample data.
        Dim activeHeight = 30
        Dim finishLine = bgSub.dst2.Height - activeHeight * 8
        Static activeState(5) As Boolean
        Static carCount As Integer
        For i = 1 To activeState.Length - 1
            Dim lane = New cv.Rect(Choose(i, 230, 460, 680, 900, 1110), finishLine, 40, activeHeight)
            Dim cellCount = videoImage(lane).CountNonZero
            If cellCount Then
                activeState(i) = True
                videoImage.Rectangle(lane, cv.Scalar.Red, -1)
                dst3.Rectangle(lane, cv.Scalar.Red, -1)
            End If
            If cellCount = 0 And activeState(i) = True Then
                activeState(i) = False
                carCount += 1
            End If
            dst3.Rectangle(lane, cv.Scalar.White, 2)
        Next

        Dim tmp = videoImage.Resize(src.Size())
        If tmp.Channels <> dst2.Channels Then tmp = tmp.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        flow.msgs.Add("  Cars " + CStr(carCount))
        flow.Run(empty)
        dst2 = dst2 Or tmp
    End Sub
End Class




' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_CarCComp : Inherits VB_Parent
    Dim cc As New CComp_Both
    Dim video As New Video_Basics
    Dim bgSub As New BGSubtract_MOG
    Public Sub New()
        desc = "Outline cars with a rectangle"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        video.Run(src)
        If video.dst2.Empty() = False Then
            bgSub.Run(video.dst2)
            cc.Run(bgSub.dst2)
            dst2 = cc.dst3
            dst3 = cc.dst2
        End If
    End Sub
End Class







' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_MinRect : Inherits VB_Parent
    Public video As New Video_Basics
    Public bgSub As New BGSubtract_MOG
    Public contours As cv.Point()()
    Public Sub New()
        video.srcVideo = task.homeDir + "Data/CarsDrivingUnderBridge.mp4"
        video.Run(dst2)
        desc = "Find area of car outline - example of using minAreaRect"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        video.Run(src)
        If video.dst2.Empty() = False Then
            bgSub.Run(video.dst2)

            contours = cv.Cv2.FindContoursAsArray(bgSub.dst2, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
            dst2 = bgSub.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            If standaloneTest() Then
                For i = 0 To contours.Length - 1
                    Dim minRect = cv.Cv2.MinAreaRect(contours(i))
                    drawRotatedRectangle(minRect, dst2, cv.Scalar.Red)
                Next
            End If
            dst3 = video.dst2
        End If
    End Sub
End Class





Public Class Video_MinCircle : Inherits VB_Parent
    Dim video As New Video_MinRect
    Public Sub New()
        desc = "Find area of car outline - example of using MinEnclosingCircle"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        video.Run(src)
        dst2 = video.dst2
        dst3 = video.dst3

        Dim center As New cv.Point2f
        Dim radius As Single
        If video.contours IsNot Nothing Then
            For i = 0 To video.contours.Length - 1
                cv.Cv2.MinEnclosingCircle(video.contours(i), center, radius)
                dst2.Circle(center, radius, cv.Scalar.White, task.lineWidth, task.lineType)
            Next
        End If
    End Sub
End Class


