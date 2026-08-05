Imports System.IO
Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Imports jsonShared
Namespace VBClasses
    Public Class AlgorithmTask : Implements IDisposable
        Public Shared Sub Initialize(settings As jsonShared.Settings)
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
            task.foregroundMask = New Mat(task.workRes, MatType.CV_8U, 0)

            task.colorizer = New DepthColorizer_Basics_TA
            task.gravityMatrix = New IMU_GMatrix_TA
            task.gravityBasics = New Gravity_Basics_TA
            task.imuBasics = New IMU_Basics_TA
            task.motion = New Motion_Basics_TA With {.standalone = False}
            task.heartBeats = New HeartBeat_Basics_TA
            task.edges = New Edge_Basics_TA

            task.stableDepth = New StableDepth_Basics_TA
            task.stableGray = New StableGray_Basics_TA

            task.grid = New Grid_Basics_TA
            task.lines = New Line_Basics_TA

            task.filterBasics = New Filter_Basics_TA
            task.foreground = New Foreground_Basics_TA
            task.leftRightBrightness = New LeftRight_Brightness_TA

            ' all the algorithms in the list are task algorithms that are children of the algorithm.
            For i = 1 To task.cpu.callTrace.Count - 1
                task.cpu.callTrace(i) = settings.algorithm + "\" + task.cpu.callTrace(i)
            Next

            HeartBeat_Basics_TA.setHeartBeat()
            task.fOptions.Show()
            task.gOptions.Show()
            task.treeView.Show()
            task.centerRect = New cv.Rect(task.workRes.Width / 4, task.workRes.Height / 4, task.workRes.Width / 2, task.workRes.Height / 2)

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
            AlgorithmTestAllCount += 1

            Select Case task.Settings.cameraName
                Case "StereoLabs ZED 2/2i"
                    task.fOptions.ColorDiffSlider.Value = 10
                Case "Orbbec Gemini 335L", "Orbbec Gemini 336L", "Orbbec Gemini 335"
                    task.fOptions.ColorDiffSlider.Value = 30
                Case "Intel(R) RealSense(TM) Depth Camera 435i", "Intel(R) RealSense(TM) Depth Camera 455"
                Case "Oak-3D camera", "Oak-4D camera"

            End Select
        End Sub
        Public Sub RunAlgorithm()
            If task.allOptions.titlesAdded Then
                task.allOptions.titlesAdded = False
                task.allOptions.layoutOptions(normalRequest:=True)
            End If

            task.heartBeats.Run(Nothing)

            If task.firstPass Then task.cpu.initialize(Settings.algorithm)

            Dim src = task.color
            If src.Width = 0 Or task.pointCloud.Width = 0 Then Exit Sub ' camera data is not ready.

            task.bins2D = {task.workRes.Height, task.workRes.Width}

            task.IMU_FrameTime = task.IMU_AlphaFilter = 0.5

            ' run any task algorithms here
            task.grid.Run(task.color)
            task.imuBasics.Run(emptyMat)
            task.gravityMatrix.Run(emptyMat)

            If task.gOptions.CreateGif.Checked Then
                task.optionsChanged = False
            Else
                task.heartBeat = task.heartBeat Or task.optionsChanged Or task.mouseClickFlag
            End If

            task.filterBasics.Run(Color.Clone)
            task.gray = task.filterBasics.dst3
            task.grayOriginal = task.gray.Clone
            task.originalPointCloud = task.pointCloud.clone
            task.leftRightBrightness.Run(emptyMat)
            task.leftView = task.leftRightBrightness.dst2
            task.rightView = task.leftRightBrightness.dst3

            If task.gOptions.stableDepthRGB.Checked Then
                ' motionStable.Run(task.gray)

                task.motion.Run(task.gray)
                task.stableGray.Run(task.gray)
            Else
                task.motion.motionMask.SetTo(255)
                task.motion.motionSort.Clear()
                task.motion.Run(gray)
            End If

            If vbc.task.pixelViewerOn Then
                If vbc.task.PixelViewer Is Nothing Then
                    vbc.task.PixelViewer = New PixelViewer.Pixel_Viewer
                End If
            End If

            If task.gOptions.CreateGif.Checked Then
                If task.gifCreator Is Nothing Then task.gifCreator = New Gif_OpenCVB
                task.gifCreator.Run(src.Clone)
                If task.gifCreator.gifC.options.buildCheck.Checked Then
                    task.gifCreator.gifC.options.buildCheck.Checked = False
                    For i = 0 To task.gifImages.Count - 1
                        Dim fileName As New FileInfo(task.homeDir + "Temp/image" + i.ToString("000") + ".bmp")
                        task.gifImages(i).Save(fileName.FullName)
                    Next

                    task.gifImages.Clear()
                    Dim dirInfo As New DirectoryInfo(task.homeDir + "GifBuilder\bin\x64\Debug\net8.0\")
                    Dim dirData = dirInfo.GetDirectories()
                    Dim gifExe As New FileInfo(dirInfo.FullName + "GifBuilder.exe")
                    If gifExe.Exists = False Then
                        MessageBox.Show("GifBuilder.exe was not found!")
                    Else
                        Try
                            Dim gifProcess As New Process
                            gifProcess.StartInfo.FileName = gifExe.FullName
                            gifProcess.StartInfo.UseShellExecute = False
                            gifProcess.StartInfo.CreateNoWindow = False
                            gifProcess.Start()
                        Catch ex As System.ComponentModel.Win32Exception When ex.Message?.Contains("The operation completed successfully") OrElse ex.NativeErrorCode = 0
                            ' Process started; Windows sometimes reports success as this exception.
                        End Try
                    End If
                End If
            End If

            '******* rotate for gravity if gravityPointCloud is selected *******
            If task.gOptions.gravityPointCloud.Checked Then Cloud_Gravity.rotatePointCloud()
            Cloud_Gravity.preparePointCloud()

            If task.gOptions.stableDepthRGB.Checked Then
                task.stableDepth.Run(emptyMat)
                task.depthRGB = task.stableDepth.dst2
            End If

            task.colorizer.Run(src)

            task.gravityBasics.Run(src.Clone)
            task.lines.Run(gray)
            task.histBinList = {task.histogramBins, task.histogramBins, task.histogramBins}

            task.foreground.Run(emptyMat)
            task.edges.Run(task.gray)




            task.MainUI_Algorithm.Run(src.Clone) ' <<<<<<<< This is where the VB algorithm runs...



            Dim displayObject = task.MainUI_Algorithm
            Dim index As Integer = 0
            If task.cpu.displayObjectName IsNot Nothing Then
                If task.cpu.displayObjectName <> displayObject.traceName Then
                    For Each td In task.cpu.activeObjects
                        If td.traceName.endswith(task.cpu.displayObjectName) Then
                            index = task.cpu.activeObjects.IndexOf(td)
                            Exit For
                        End If
                    Next
                    displayObject = cpu.activeObjects(index)
                End If
            End If

            Dim nextTrueData As List(Of TrueText) = displayObject.trueData
            task.trueData = New List(Of TrueText)(nextTrueData)

            task.firstPass = False
            task.heartBeatLT = False

            pixelViewerOrGIFProcessing(src, displayObject.dst1, displayObject.dst2, displayObject.dst3)

            task.dstList(0) = If(task.gOptions.displayDst0.Checked, Mat_Convert.Mat_Check8UC3(displayObject.dst0), task.color.Clone)
            task.dstList(1) = If(task.gOptions.displayDst1.Checked, Mat_Convert.Mat_Check8UC3(displayObject.dst1), task.depthRGB.Clone)
            task.dstList(2) = Mat_Convert.Mat_Check8UC3(displayObject.dst2)
            task.dstList(3) = Mat_Convert.Mat_Check8UC3(displayObject.dst3)

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

            If task.gOptions.CrossHairs.Checked Then Gravity_Basics_TA.showVectors(task.dstList(0))

            task.trueData.Clear()
            task.trueData.Add(New TrueText(task.depthAndDepthRange,
                              New cv.Point(task.mouseMovePoint.X, task.mouseMovePoint.Y - 24), 1))
            For Each tt In displayObject.trueData
                task.trueData.Add(tt)
            Next

            displayObject.trueData.Clear()
            task.labels = displayObject.labels
            If task.gOptions.displayDst0.Checked = False Then task.labels(0) = task.resolutionDetails
            If task.gOptions.displayDst1.Checked = False Then task.labels(1) = task.depthAndDepthRange.Replace(vbCrLf, "")
        End Sub
        Private Sub pixelViewerOrGIFProcessing(src As Mat, dst1 As Mat, dst2 As Mat, dst3 As Mat)
            If vbc.task.pixelViewerOn Then
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