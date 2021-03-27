Imports cv = OpenCvSharp
Imports System.IO
' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_Basics
    Inherits VBparent
    Public srcVideo As String
    Public image As New cv.Mat
    Public captureVideo As New cv.VideoCapture
    Public Sub New()
        initParent()

        task.openFileDialogRequested = True
        task.openFileInitialDirectory = task.parms.homeDir + "/Data/"
        task.openFileDialogName = GetSetting("OpenCVB", "VideoFileName", "VideoFileName", task.parms.homeDir + "Data\CarsDrivingUnderBridge.mp4")
        task.openFileFilter = "video files (*.mp4)|*.mp4|All files (*.*)|*.*"
        task.openFileFilterIndex = 1
        task.openFileDialogTitle = "Select a video file for input"
        task.initialStartSetting = False

        Dim fileInfo = New FileInfo(task.openFileDialogName)
        srcVideo = fileInfo.FullName

        captureVideo = New cv.VideoCapture(fileInfo.FullName)
        label1 = fileInfo.Name
        task.desc = "Show a video file"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim fileInfo = New FileInfo(task.openFileDialogName)
        If srcVideo <> task.openFileDialogName Then
            If fileInfo.Exists = False Then
                ocvb.trueText("File not found: " + fileInfo.FullName, 10, 125)
                Exit Sub
            End If
            srcVideo = task.openFileDialogName
            captureVideo = New cv.VideoCapture(task.openFileDialogName)
        End If
        captureVideo.Read(image)
        If image.Empty() Then
            captureVideo.Dispose()
            captureVideo = New cv.VideoCapture(fileInfo.FullName)
            captureVideo.Read(image)
        End If

        task.openFileSliderPercent = captureVideo.PosFrames / captureVideo.FrameCount
        If image.Empty() = False Then dst1 = image.Resize(src.Size())
    End Sub
End Class






' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_CarCounting
    Inherits VBparent
    Dim flow As Font_FlowText
    Dim video As Video_Basics
    Dim bgSub As BGSubtract_MOG
    Public Sub New()
        initParent()
        bgSub = New BGSubtract_MOG()

        video = New Video_Basics()

        flow = New Font_FlowText()

        task.desc = "Count cars in a video file"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        video.Run()
        If video.dst1.Empty() = False And video.image.Empty() = False Then
            dst1.SetTo(0)
            bgSub.src = video.image
            bgSub.Run()
            Dim videoImage = bgSub.dst1
            dst2 = video.dst1

            ' there are 5 lanes of traffic so setup 5 regions
            ' NOTE: if long shadows are present this approach will not work without provision for the width of a car.  Needs more sample data.
            Dim activeHeight = 30
            Dim finishLine = bgSub.dst1.Height - activeHeight * 8
            Static activeState(5) As Boolean
            Static carCount As Integer
            For i = 1 To activeState.Length - 1
                Dim lane = New cv.Rect(Choose(i, 230, 460, 680, 900, 1110), finishLine, 40, activeHeight)
                Dim cellCount = videoImage(lane).CountNonZero()
                If cellCount Then
                    activeState(i) = True
                    videoImage.Rectangle(lane, cv.Scalar.Red, -1)
                    dst2.Rectangle(lane, cv.Scalar.Red, -1)
                End If
                If cellCount = 0 And activeState(i) = True Then
                    activeState(i) = False
                    carCount += 1
                End If
                dst2.Rectangle(lane, cv.Scalar.White, 2)
            Next

            Dim tmp = videoImage.Resize(src.Size())
            flow.msgs.Add("  Cars " + CStr(carCount))
            flow.Run()
            cv.Cv2.BitwiseOr(dst1, tmp.CvtColor(cv.ColorConversionCodes.GRAY2BGR), dst1)
        End If
    End Sub
End Class




' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_CarCComp
    Inherits VBparent
    Dim cc As CComp_Basics
    Dim video As Video_Basics
    Dim bgSub As BGSubtract_MOG
    Public Sub New()
        initParent()

        bgSub = New BGSubtract_MOG()
        cc = New CComp_Basics()
        video = New Video_Basics()

        task.desc = "Outline cars with a rectangle"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        video.Run()
        If video.dst1.Empty() = False Then
            bgSub.src = video.dst1
            bgSub.Run()
            cc.src = bgSub.dst1
            cc.Run()
            dst1 = cc.dst1
            dst2 = cc.dst2
        End If
    End Sub
End Class




' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_MinRect
    Inherits VBparent
    Public video As Video_Basics
    Public bgSub As BGSubtract_MOG
    Public contours As cv.Point()()
    Public Sub New()
        initParent()
        video = New Video_Basics()
        video.srcVideo = task.parms.homeDir + "Data/CarsDrivingUnderBridge.mp4"
        video.Run()

        bgSub = New BGSubtract_MOG()
        task.desc = "Find area of car outline - example of using minAreaRect"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        video.Run()
        If video.dst1.Empty() = False Then
            bgSub.src = video.dst1
            bgSub.Run()

            contours = cv.Cv2.FindContoursAsArray(bgSub.dst1, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
            dst1 = bgSub.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            If standalone or task.intermediateReview = caller Then
                For i = 0 To contours.Length - 1
                    Dim minRect = cv.Cv2.MinAreaRect(contours(i))
                    drawRotatedRectangle(minRect, dst1, cv.Scalar.Red)
                Next
            End If
            dst2 = video.dst1
        End If
    End Sub
End Class





Public Class Video_MinCircle
    Inherits VBparent
    Dim video As Video_MinRect
    Public Sub New()
        initParent()
        video = New Video_MinRect()
        task.desc = "Find area of car outline - example of using MinEnclosingCircle"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        video.Run()
        dst1 = video.dst1
        dst2 = video.dst2

        Dim center As New cv.Point2f
        Dim radius As Single
        If video.contours IsNot Nothing Then
            For i = 0 To video.contours.Length - 1
                cv.Cv2.MinEnclosingCircle(video.contours(i), center, radius)
                dst1.Circle(center, radius, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            Next
        End If
    End Sub
End Class

