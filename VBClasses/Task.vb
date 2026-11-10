Imports jsonShared : Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class AlgorithmTask : Implements IDisposable
        Public Shared Sub Initialize(settings As jsonShared.Settings)
            Static AlgorithmTestAllCount As Integer = 1
            task.Settings = settings
            Dim paintFreq = task.Settings.paintFrequency
            task.gridRects = New List(Of cv.Rect)
            task.optionsChanged = True
            task.firstPass = True
            task.useXYRange = True ' Most projections of pointcloud data can use the xRange and yRange to improve task.results..

            task.rows = settings.workRes.Height
            task.cols = settings.workRes.Width
            task.workRes = settings.workRes
            task.captureRes = settings.captureRes

            task.allOptions = New OptionsContainer
            task.allOptions.Show()
            task.allOptions.Location = New System.Drawing.Point(task.Settings.allOptionsLeft, task.Settings.allOptionsTop)
            task.allOptions.Size = New System.Drawing.Size(task.Settings.allOptionsWidth, task.Settings.allOptionsHeight)
            task.allOptions.positionedFromSettings = True

            If (settings.algorithm.StartsWith("GL_") Or settings.algorithm.StartsWith("XR_GL_")) And
                        settings.algorithm <> "GL_MainForm" And task.optionsChanged Then
                If task.sharpGL IsNot Nothing Then task.sharpGL.Dispose()
                task.sharpGL = New SharpGLForm
                task.sharpGL.Show()
            End If

            task.gOptions = New OptionsGlobal
            task.fOptions = New OptionsFeatures
            task.treeView = New TreeViewForm

            task.cpu.callTrace = New List(Of String)
            task.gravityCloud = New Mat(task.workRes, MatType.CV_32FC3, 0)
            task.noDepthMask = New Mat(task.workRes, MatType.CV_8U, 0)
            task.depthmask = New Mat(task.workRes, MatType.CV_8U, 0)

            task.Algorithms = New TaskAlgorithms()

            task.fOptions.Show()
            task.gOptions.Show()
            task.treeView.Show()

            task.mouseMovePoint = New cv.Point(task.workRes.Width \ 2, task.workRes.Height \ 2)
            task.mainFormLocation = New cv.Rect(task.Settings.MainFormLeft, task.Settings.MainFormTop,
                                                task.Settings.MainFormWidth, task.Settings.MainFormHeight)
            task.myStopWatch = Stopwatch.StartNew()
            task.optionsChanged = True
            task.readyForCameraInput = True
            task.clickPoint = New cv.Point(CInt(task.workRes.Width / 2), CInt(task.workRes.Height / 2))
            task.mouseClickFlag = True

            task.gOptions.PaintFreqSlider.Value = paintFreq
            Options_PointCloud.setupCalcHist()
            Debug.WriteLine(vbCrLf + vbCrLf + vbCrLf + "Starting algorithm " + settings.algorithm + " at " + CStr(Now))
            Debug.WriteLine(vbTab + CStr(AlgorithmTestAllCount) + " algorithms tested")
            task.cpu.displayObjectName = settings.algorithm
            AlgorithmTestAllCount += 1
            task.minCellSize = task.workRes.Width * task.workRes.Height * 0.001
            Select Case task.Settings.cameraName
                Case "StereoLabs ZED 2/2i"
                    task.fOptions.ColorDiffSlider.Value = 10
                Case "Orbbec Gemini 335L", "Orbbec Gemini 336L", "Orbbec Gemini 335"
                    task.fOptions.ColorDiffSlider.Value = 30
                Case "Intel(R) RealSense(TM) Depth Camera 435i", "Intel(R) RealSense(TM) Depth Camera 455"
                Case "Oak-3D camera", "Oak-4D camera"
            End Select

            Dim w As Integer = task.workRes.Width / 2, h As Integer = task.workRes.Height / 2
            task.quads(0) = New cv.Rect(0, 0, w, h)
            task.quads(1) = New cv.Rect(w, 0, w, h)
            task.quads(2) = New cv.Rect(0, h, w, h)
            task.quads(3) = New cv.Rect(w, h, w, h)

            ' if not a task algorithm, then we need to run the algorithm.  
            task.needtoRunAlgorithm = Not task.Algorithms.algTasks.Contains(task.Settings.algorithm)
        End Sub
        Public Sub RunAlgorithm()
            Dim displayObject = task.MainUI_Algorithm
            If task.allOptions.titlesAdded Then
                task.allOptions.titlesAdded = False
                task.allOptions.layoutOptions(normalRequest:=True)
            End If

            If task.firstPass Then task.cpu.initialize(Settings.algorithm)

            Dim src = task.color
            If src.Width = 0 Or task.pointCloud.Width = 0 Then Exit Sub ' camera data is not ready.

            task.bins2D = {task.workRes.Height, task.workRes.Width}

            task.IMU_FrameTime = task.IMU_AlphaFilter = 0.5
            task.lineMaxCount = task.fOptions.FeatureSizeSlider.Value
            TaskAlgorithms.RunTaskAlgorithms()





            If task.needtoRunAlgorithm Then
                task.MainUI_Algorithm.Run(src.Clone) ' <<<<<<<< This is where the VB algorithm runs...
            Else
                If task.cpu.displayObjectName = task.Settings.algorithm Then
                    Dim compoundName = task.Settings.algorithm + "\TaskAlgorithms\" + task.Settings.algorithm + "\"
                    task.cpu.displayObjectIndex = task.cpu.callTrace.IndexOf(compoundName)
                    task.cpu.displayObjectName = compoundName
                End If
            End If





            If task.cpu.displayObjectName <> displayObject.traceName Then
                displayObject = cpu.activeObjects(task.cpu.displayObjectIndex)
            End If

            Dim nextTrueData As List(Of TrueText) = displayObject.trueData
            task.trueData = New List(Of TrueText)(nextTrueData)

            task.firstPass = False
            task.heartBeatLT = False

            pixelViewerOrGIFProcessing(src, displayObject.dst1, displayObject.dst2, displayObject.dst3)

            task.dstList(0) = If(task.gOptions.showMyDst0.Checked, Mat_Convert.Mat_Check8UC3(displayObject.dst0), task.color.Clone)
            task.dstList(1) = If(task.gOptions.showMyDst1.Checked, Mat_Convert.Mat_Check8UC3(displayObject.dst1), task.depthRGB.Clone)
            task.dstList(2) = Mat_Convert.Mat_Check8UC3(displayObject.dst2)
            task.dstList(3) = Mat_Convert.Mat_Check8UC3(displayObject.dst3)

            For Each m In task.dstList
                If m.Size <> task.workRes Then Resize(m, m, task.workRes, 0, 0, InterpolationFlags.Nearest)
            Next
            Dim pt = task.mouseMovePoint
            Dim tag = task.mousePicTag
            Try
                task.mousePixelValue = task.dstList(tag).Get(Of Vec3b)(pt.Y, pt.X)
            Catch ex As Exception
            End Try

            If task.gOptions.ShowGrid.Checked Then task.dstList(2).SetTo(Scalar.White, task.gridMask)
            If task.gOptions.showMotionMask.Checked Then
                ' motion cloud contains all the RGB motion as well.
                For Each mIndex In task.motion.motionSort
                    Rectangle(task.dstList(0), task.gridRects(mIndex), Scalar.White, task.lineWidth)
                Next
            End If

            If task.gOptions.CrossHairs.Checked Then Gravity_Basics.showVectors(task.dstList(0))

            task.trueData.Clear()
            task.trueData.Add(New TrueText(task.depthAndDepthRange,
                              New cv.Point(task.mouseMovePoint.X, task.mouseMovePoint.Y - 24), 1))
            For Each tt In displayObject.trueData
                task.trueData.Add(tt)
            Next

            displayObject.trueData.Clear()
            task.labels = displayObject.labels
            If task.gOptions.showMyDst0.Checked = False Then task.labels(0) = task.resolutionDetails
            If task.gOptions.showMyDst1.Checked = False Then task.labels(1) = task.depthAndDepthRange.Replace(vbCrLf, "")
        End Sub
        Private Sub pixelViewerOrGIFProcessing(src As Mat, dst1 As Mat, dst2 As Mat, dst3 As Mat)
            If task.pixelViewerOn Then
                If task.PixelViewer IsNot Nothing Then
                    task.PixelViewer.viewerForm.Visible = True
                    task.PixelViewer.viewerForm.Show()
                    task.PixelViewer.dst0Input = src
                    task.PixelViewer.dst1Input = dst1
                    task.PixelViewer.dst2Input = dst2
                    task.PixelViewer.dst3Input = dst3
                    task.PixelViewer.Run(src)
                End If
            End If
            If task.gifCreator IsNot Nothing Then task.gifCreator.createNextGifImage()

            task.optionsChanged = False
        End Sub
        Public Sub New()
            Randomize() ' just in case anyone uses VB.Net's Rnd
        End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
            GC.SuppressFinalize(Me)
            If task.allOptions IsNot Nothing Then task.allOptions.Dispose()

            task.fOptions.Close()
            task.treeView.Close()
            If task.sharpGL IsNot Nothing Then task.sharpGL.Close()

            GC.Collect()
        End Sub
    End Class
End Namespace