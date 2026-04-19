Imports System.IO
Imports cv = OpenCvSharp
Imports jsonShared
Public Class AlgorithmTask : Implements IDisposable
    Public Sub Initialize(settings As jsonShared.Settings)
        task.Settings = settings
        Dim paintFreq = task.Settings.paintFrequency

        rows = settings.workRes.Height
        cols = settings.workRes.Width
        workRes = settings.workRes
        captureRes = settings.captureRes

        ' StereoLabs does not need the left and right images to be adjusted for brightness.
        If settings.cameraName.Contains("StereoLabs") Then task.leftRightBrightnessAdjust = False

        allOptions = New OptionsContainer
        allOptions.Show()
        allOptions.Location = New System.Drawing.Point(task.Settings.allOptionsLeft, task.Settings.allOptionsTop)
        allOptions.Size = New System.Drawing.Size(task.Settings.allOptionsWidth, task.Settings.allOptionsHeight)
        allOptions.positionedFromSettings = True

        If (settings.algorithm.StartsWith("GL_") Or settings.algorithm.StartsWith("NR_GL_")) And
                    settings.algorithm <> "GL_MainForm" And optionsChanged Then
            If sharpGL IsNot Nothing Then sharpGL.Dispose()
            sharpGL = New SharpGLForm
            sharpGL.Show()
        End If

        gOptions = New OptionsGlobal
        fOptions = New OptionsFeatures
        treeView = New TreeViewForm

        cpu.callTrace = New List(Of String)
        gravityCloud = New cv.Mat(workRes, cv.MatType.CV_32FC3, 0)
        noDepthMask = New cv.Mat(workRes, cv.MatType.CV_8U, 0)
        depthmask = New cv.Mat(workRes, cv.MatType.CV_8U, 0)

        colorizer = New DepthColorizer_Basics_TA
        gravityMatrix = New IMU_GMatrix_TA
        gravityBasics = New Gravity_Basics_TA
        imuBasics = New IMU_Basics_TA
        motion = New Motion_Basics_TA
        motionStable = New StableGray_Measure_TA

        stabilizeDepth = New StableDepth_Basics
        stabilizeDepth.TA_Active = True
        stabilizeGray = New StableGray_Basics_TA

        cloudGravity = New Cloud_Gravity_TA
        grid = New Grid_Basics_TA
        lines = New Line_Basics_TA
        filterBasics = New Filter_Basics_TA
        foreground = New Foreground_Basics_TA
        If task.leftRightBrightnessAdjust Then leftRightBrightness = New LeftRight_Brightness_TA

        ' all the algorithms in the list are task algorithms that are children of the algorithm.
        For i = 1 To cpu.callTrace.Count - 1
            cpu.callTrace(i) = settings.algorithm + "\" + cpu.callTrace(i)
        Next

        taskUpdate()
        fOptions.Show()
        gOptions.Show()
        treeView.Show()
        centerRect = New cv.Rect(workRes.Width / 4, workRes.Height / 4, workRes.Width / 2, workRes.Height / 2)
        fpList.Clear()

        task.mouseMovePoint = New cv.Point(task.workRes.Width \ 2, task.workRes.Height \ 2)
        task.mainFormLocation = New cv.Rect(task.Settings.MainFormLeft, task.Settings.MainFormTop,
                                                    task.Settings.MainFormWidth, task.Settings.MainFormHeight)

        myStopWatch = Stopwatch.StartNew()
        optionsChanged = True
        readyForCameraInput = True
        task.clickPoint = New cv.Point(CInt(workRes.Width / 2), CInt(workRes.Height / 2))

        task.gOptions.PaintFreqSlider.Value = paintFreq
        Options_PointCloud.setupCalcHist()
        Debug.WriteLine(vbCrLf + vbCrLf + vbCrLf + "Starting algorithm " + settings.algorithm)
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
        If allOptions.titlesAdded Then
            allOptions.titlesAdded = False
            allOptions.layoutOptions(normalRequest:=True)
        End If

        taskUpdate()

        If task.firstPass Then task.cpu.initialize(Settings.algorithm)

        Dim src = task.color
        If src.Width = 0 Or task.pointCloud.Width = 0 Then Exit Sub ' camera data is not ready.

        bins2D = {task.workRes.Height, task.workRes.Width}

        ' run any universal algorithms here
        IMU_Acceleration = IMU_Acceleration
        IMU_AngularVelocity = IMU_AngularVelocity
        IMU_FrameTime =
                IMU_AlphaFilter = 0.5 '  gOptions.imu_Alpha

        grid.Run(task.color)
        imuBasics.Run(emptyMat)
        gravityMatrix.Run(emptyMat)

        If gOptions.CreateGif.Checked Then
            optionsChanged = False
        Else
            heartBeat = heartBeat Or optionsChanged Or mouseClickFlag
        End If

        frameHistoryCount = 3 ' default value.  Use Options_History to update this value.

        filterBasics.Run(color.Clone)
        task.gray = filterBasics.dst3
        task.grayOriginal = task.gray.Clone
        If task.leftRightBrightnessAdjust Then
            leftRightBrightness.Run(emptyMat)
            leftView = leftRightBrightness.dst2
            rightView = leftRightBrightness.dst3
        End If

        If gOptions.stabilizeDepthRGB.Checked Then
            motionStable.Run(task.gray)

            motion.Run(gray)
            stabilizeGray.Run(task.gray)
        Else
            motion.motionMask.SetTo(255)
            motion.motionSort.Clear()
            motion.Run(gray)
        End If

        If feat IsNot Nothing Then feat.Run(src)

        If vbc.task.pixelViewerOn Then
            If vbc.task.PixelViewer Is Nothing Then
                vbc.task.PixelViewer = New PixelViewer.Pixel_Viewer
            End If
        End If

        If gOptions.CreateGif.Checked Then
            If gifCreator Is Nothing Then gifCreator = New Gif_OpenCVB
            gifCreator.Run(src.Clone)
            If gifCreator.gifC.options.buildCheck.Checked Then
                gifCreator.gifC.options.buildCheck.Checked = False
                For i = 0 To gifImages.Count - 1
                    Dim fileName As New FileInfo(task.homeDir + "Temp/image" + Format(i, "000") + ".bmp")
                    gifImages(i).Save(fileName.FullName)
                Next

                gifImages.Clear()
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

        cloudGravity.Run(emptyMat) '******* this may rotate for gravity if gravity is selected *******

        If gOptions.stabilizeDepthRGB.Checked Then stabilizeDepth.Run(emptyMat)

        colorizer.Run(src)

        gravityBasics.Run(src.Clone)
        lines.motionMask = motion.motionMask
        lines.Run(gray)
        histBinList = {histogramBins, histogramBins, histogramBins}

        foreground.Run(emptyMat)

        Dim saveOptionsChanged = optionsChanged
        If activateTaskForms Then
            If sharpGL IsNot Nothing Then sharpGL.Activate()
            treeView.Activate()
            allOptions.Activate()
            If PixelViewer IsNot Nothing Then PixelViewer.viewerForm.Activate()
            activateTaskForms = False
        End If





        MainUI_Algorithm.Run(src.Clone) ' <<<<<<<< This is where the VB algorithm runs...




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
                displayObject = task.cpu.activeObjects(index)
            End If
        End If
        Dim nextTrueData As List(Of TrueText) = displayObject.trueData
        trueData = New List(Of TrueText)(nextTrueData)

        firstPass = False
        heartBeatLT = False

        pixelViewerOrGIFProcessing(src, displayObject.dst1, displayObject.dst2, displayObject.dst3)

        dstList(0) = If(gOptions.displayDst0.Checked, Mat_Convert.Mat_Check8UC3(displayObject.dst0), color.Clone)
        dstList(1) = If(gOptions.displayDst1.Checked, Mat_Convert.Mat_Check8UC3(displayObject.dst1), depthRGB.Clone)
        dstList(2) = Mat_Convert.Mat_Check8UC3(displayObject.dst2)
        dstList(3) = Mat_Convert.Mat_Check8UC3(displayObject.dst3)

        If gOptions.ShowGrid.Checked Then dstList(2).SetTo(cv.Scalar.White, gridMask)
        If gOptions.showMotionMask.Checked Then
            ' motion cloud contains all the RGB motion as well.
            For Each mIndex In motion.motionSort
                dstList(0).Rectangle(gridRects(mIndex), cv.Scalar.White, lineWidth)
            Next
        End If


        If gOptions.CrossHairs.Checked Then
            Gravity_Basics_TA.showVectors(dstList(0))
            Dim lp = If(lpGravity IsNot Nothing, lpGravity, lines.lpList(0))
            Dim pt = New cv.Point2f((lp.pE1.X + lp.pE2.X) / 2 + 5, (lp.pE1.Y + lp.pE2.Y) / 2)
        End If

        trueData.Clear()
        trueData.Add(New TrueText(task.depthAndDepthRange, New cv.Point(task.mouseMovePoint.X, task.mouseMovePoint.Y - 24), 1))
        For Each tt In displayObject.trueData
            trueData.Add(tt)
        Next

        displayObject.trueData.Clear()
        labels = displayObject.labels
        If task.gOptions.displayDst0.Checked = False Then labels(0) = task.resolutionDetails
        If task.gOptions.displayDst1.Checked = False Then labels(1) = task.depthAndDepthRange.Replace(vbCrLf, "")
    End Sub
    Private Sub pixelViewerOrGIFProcessing(src As cv.Mat, dst1 As cv.Mat, dst2 As cv.Mat, dst3 As cv.Mat)
        If vbc.task.pixelViewerOn Then
            If PixelViewer IsNot Nothing Then
                PixelViewer.viewerForm.Visible = True
                PixelViewer.viewerForm.Show()
                PixelViewer.dst0Input = src
                PixelViewer.dst1Input = dst1
                PixelViewer.dst2Input = dst2
                PixelViewer.dst3Input = dst3
                PixelViewer.Run(src)
            End If
        End If
        If gifCreator IsNot Nothing Then gifCreator.createNextGifImage()

        optionsChanged = False
    End Sub
    Public Sub New()
        Randomize() ' just in case anyone uses VB.Net's Rnd
        gridRects = New List(Of cv.Rect)
        optionsChanged = True
        firstPass = True
        useXYRange = True ' Most projections of pointcloud data can use the xRange and yRange to improve task.results..
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If allOptions IsNot Nothing Then allOptions.Dispose()

        task.fOptions.Close()
        task.treeView.Close()
        If task.sharpGL IsNot Nothing Then task.sharpGL.Close()

        GC.Collect()
    End Sub
End Class
