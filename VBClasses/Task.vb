Imports System.IO
Imports PixelViewer
Imports cv = OpenCvSharp
Imports jsonShared
Namespace VBClasses
    Public Class AlgorithmTask : Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
            If allOptions IsNot Nothing Then allOptions.Close()
            If cpu.activeObjects IsNot Nothing Then
                For Each algorithm In cpu.activeObjects
                    If algorithm.GetType().GetMethod("Close") IsNot Nothing Then algorithm.Close()  ' Close any unmanaged classes...
                Next
            End If
            For Each mat In taskAlg.dstList
                If mat IsNot Nothing Then mat.Dispose()
            Next
        End Sub
        Public Sub Initialize(settings As jsonShared.Settings)
            taskAlg.Settings = settings
            rgbLeftAligned = True
            If settings.cameraName.Contains("RealSense") Then rgbLeftAligned = False

            rows = settings.workRes.Height
            cols = settings.workRes.Width
            workRes = settings.workRes
            captureRes = settings.captureRes

            allOptions = New OptionsContainer
            allOptions.Show()
            allOptions.Location = New Point(taskAlg.Settings.allOptionsLeft, taskAlg.Settings.allOptionsTop)
            allOptions.Size = New Size(taskAlg.Settings.allOptionsWidth, taskAlg.Settings.allOptionsHeight)
            allOptions.positionedFromSettings = True

            If settings.algorithm.StartsWith("GL_") And settings.algorithm <> "GL_MainForm" And optionsChanged Then
                If sharpGL IsNot Nothing Then sharpGL.Dispose()
                sharpGL = New SharpGLForm
                sharpGL.Show()
            End If

            Dim fps = taskAlg.Settings.FPSPaintTarget
            gOptions = New OptionsGlobal
            gOptions.DisplayFPSSlider.Value = fps
            featureOptions = New OptionsFeatures
            treeView = New TreeViewForm

            cpu.callTrace = New List(Of String)
            gravityCloud = New cv.Mat(workRes, cv.MatType.CV_32FC3, 0)
            motionMask = New cv.Mat(workRes, cv.MatType.CV_8U, 255)
            noDepthMask = New cv.Mat(workRes, cv.MatType.CV_8U, 0)
            depthmask = New cv.Mat(workRes, cv.MatType.CV_8U, 0)

            colorizer = New DepthColorizer_Basics
            gmat = New IMU_GMatrix
            gravityBasics = New Gravity_Basics
            imuBasics = New IMU_Basics
            motionBasics = New Motion_Basics
            pcMotion = New Motion_PointCloud
            grid = New Grid_Basics
            lines = New Line_Basics
            rgbFilter = New Filter_Basics

            ' all the algorithms in the list are taskAlg algorithms that are children of the algorithm.
            For i = 1 To cpu.callTrace.Count - 1
                cpu.callTrace(i) = settings.algorithm + "\" + cpu.callTrace(i)
            Next

            taskUpdate()
            featureOptions.Show()
            gOptions.Show()
            Options_HistPointCloud.setupCalcHist()
            treeView.Show()
            centerRect = New cv.Rect(workRes.Width / 4, workRes.Height / 4,
                                 workRes.Width / 2, workRes.Height / 2)

            fpList.Clear()

            myStopWatch = Stopwatch.StartNew()
            optionsChanged = True
            readyForCameraInput = True

            Debug.WriteLine("")
            Debug.WriteLine("")
            Debug.WriteLine("Starting algorithm " + settings.algorithm)
            Debug.WriteLine(vbTab + CStr(AlgorithmTestAllCount) + " algorithms tested")
            AlgorithmTestAllCount += 1
        End Sub
        Public Sub RunAlgorithm()
            If allOptions.titlesAdded Then
                allOptions.titlesAdded = False
                allOptions.layoutOptions(normalRequest:=True)
            End If

            taskUpdate()

            If taskAlg.cpu.algorithm_ms.Count = 0 Then taskAlg.cpu.initialize(Settings.algorithm)

            Dim src = taskAlg.color
            If src.Width = 0 Or taskAlg.pointCloud.Width = 0 Then Exit Sub ' camera data is not ready.

            bins2D = {taskAlg.workRes.Height, taskAlg.workRes.Width}

            ' run any universal algorithms here
            IMU_RawAcceleration = IMU_Acceleration
            IMU_RawAngularVelocity = IMU_AngularVelocity
            IMU_AlphaFilter = 0.5 '  gOptions.imu_Alpha

            grid.Run(taskAlg.color)
            imuBasics.Run(emptyMat)
            gmat.Run(emptyMat)

            If gOptions.CreateGif.Checked Then
                heartBeat = False
                optionsChanged = False
            Else
                heartBeat = heartBeat Or optionsChanged Or mouseClickFlag
            End If

            frameHistoryCount = 3 ' default value.  Use Options_History to update this value.

            If optionsChanged Then motionMask.SetTo(255)

            rgbFilter.Run(color)
            If gOptions.UseMotionMask.Checked Then
                motionBasics.Run(gray)
                If optionsChanged Or taskAlg.frameCount < 5 Then
                    motionRect = New cv.Rect(0, 0, workRes.Width, workRes.Height)
                    grayStable = gray.Clone
                    leftViewStable = leftView.Clone
                Else
                    If motionRect.Width > 0 Then
                        gray.CopyTo(grayStable, motionMask)
                        leftView.CopyTo(leftViewStable, motionMask)
                    End If
                End If
            Else
                motionMask.SetTo(255)
                motionBasics.motionList.Clear()
                grayStable = gray
                leftViewStable = leftView
                motionRect = New cv.Rect(0, 0, gray.Width, gray.Height)
            End If

            If pcMotion IsNot Nothing Then
                pcMotion.Run(emptyMat) '******* this is the gravity rotation *******
            Else
                taskAlg.pcSplit = taskAlg.pointCloud.Split
            End If

            colorizer.Run(src)

            If feat IsNot Nothing Then feat.Run(src)
            If bricks IsNot Nothing Then bricks.Run(src)

            If pixelViewerOn And PixelViewer Is Nothing Then
                PixelViewer = New PixelViewer.Pixel_Viewer
            Else
                If pixelViewerOn = False Then PixelViewer = Nothing
            End If

            If gOptions.CreateGif.Checked Then
                If gifCreator Is Nothing Then gifCreator = New Gif_OpenCVB
                gifCreator.Run(src.Clone)
                If gifCreator.gifC.options.buildCheck.Checked Then
                    gifCreator.gifC.options.buildCheck.Checked = False
                    For i = 0 To gifImages.Count - 1
                        Dim fileName As New FileInfo(taskAlg.homeDir + "Temp/image" + Format(i, "000") + ".bmp")
                        gifImages(i).Save(fileName.FullName)
                    Next

                    gifImages.Clear()
                    Dim dirInfo As New DirectoryInfo(taskAlg.homeDir + "GifBuilder\bin\Debug\net8.0\")
                    Dim dirData = dirInfo.GetDirectories()
                    Dim gifExe As New FileInfo(dirInfo.FullName + "GifBuilder.exe")
                    If gifExe.Exists = False Then
                        MessageBox.Show("GifBuilder.exe was not found!")
                    Else
                        Dim gifProcess As New Process
                        gifProcess.StartInfo.FileName = gifExe.FullName
                        gifProcess.Start()
                    End If
                End If
            End If

            gravityBasics.Run(src.Clone)
            lines.Run(grayStable)
            histBinList = {histogramBins, histogramBins, histogramBins}

            Dim saveOptionsChanged = optionsChanged
            If activateTaskForms Then
                If sharpGL IsNot Nothing Then sharpGL.Activate()
                treeView.Activate()
                allOptions.Activate()
                If PixelViewer IsNot Nothing Then PixelViewer.viewerForm.Activate()
                activateTaskForms = False
            End If
            If paused = False Then




                algorithmPrep = False
                MainUI_Algorithm.Run(src.Clone) ' <<<<<<<< This is where the VB algorithm runs...
                algorithmPrep = True




                Dim displayObject = taskAlg.MainUI_Algorithm

                Dim nextTrueData As List(Of TrueText) = displayObject.trueData
                trueData = New List(Of TrueText)(nextTrueData)

                firstPass = False
                heartBeatLT = False

                ' they could have asked to display one of the algorithms in the TreeView.
                For Each obj In taskAlg.cpu.activeObjects
                    If obj.tracename = taskAlg.cpu.displayObjectName Then
                        displayObject = obj
                        Exit For
                    End If
                Next

                postProcess(src, displayObject.dst1, displayObject.dst2, displayObject.dst3)

                dstList(0) = If(gOptions.displayDst0.Checked, Mat_Convert.Mat_Check8uc3(displayObject.dst0), color).Clone
                dstList(1) = If(gOptions.displayDst1.Checked, Mat_Convert.Mat_Check8uc3(displayObject.dst1), depthRGB).Clone
                dstList(2) = Mat_Convert.Mat_Check8uc3(displayObject.dst2)
                dstList(3) = Mat_Convert.Mat_Check8uc3(displayObject.dst3)

                If gOptions.ShowGrid.Checked Then dstList(2).SetTo(cv.Scalar.White, gridMask)
                If gOptions.showMotionMask.Checked Then
                    For Each mIndex In motionBasics.motionList
                        dstList(0).Rectangle(gridRects(mIndex), cv.Scalar.White, lineWidth)
                    Next
                    dstList(0).Rectangle(motionRect, white, lineWidth)
                End If

                If gOptions.CrossHairs.Checked Then
                    Gravity_Basics.showVectors(dstList(0))
                    Dim lp = lineLongest
                    Dim pt = New cv.Point2f((lp.pE1.X + lp.pE2.X) / 2 + 5, (lp.pE1.Y + lp.pE2.Y) / 2)
                    displayObject.trueData.Add(New TrueText("Longest", pt, 0))
                End If

                If taskAlg.drawRect.Width > 0 And taskAlg.drawRect.Height > 0 Then
                    For Each dst In dstList
                        dst.Rectangle(taskAlg.drawRect, cv.Scalar.White, 1)
                    Next
                End If

                ' if there were no cycles spent on this routine, then it was inactive.
                ' if any active algorithm has an index = -1, it has not been run.
                Dim index = taskAlg.cpu.algorithmNames.IndexOf(displayObject.traceName)
                If index = -1 Then
                    displayObject.trueData.Add(New TrueText("This taskAlg is not active at this time.",
                                           New cv.Point(workRes.Width / 3, workRes.Height / 2), 2))
                End If

                trueData.Clear()
                trueData.Add(New TrueText(taskAlg.depthAndDepthRange,
                                  New cv.Point(taskAlg.mouseMovePoint.X, taskAlg.mouseMovePoint.Y - 24), 1))
                For Each tt In displayObject.trueData
                    trueData.Add(tt)
                Next

                displayObject.trueData.Clear()
                labels = displayObject.labels
                If taskAlg.gOptions.displayDst0.Checked = False Then labels(0) = taskAlg.resolutionDetails
                If taskAlg.gOptions.displayDst1.Checked = False Then labels(1) = taskAlg.depthAndDepthRange.Replace(vbCrLf, "")
            End If
        End Sub
        Private Sub postProcess(src As cv.Mat, dst1 As cv.Mat, dst2 As cv.Mat, dst3 As cv.Mat)
            If PixelViewer IsNot Nothing Then
                If pixelViewerOn Then
                    PixelViewer.viewerForm.Visible = True
                    PixelViewer.viewerForm.Show()
                    PixelViewer.dst0Input = src
                    PixelViewer.dst1Input = dst1
                    PixelViewer.dst2Input = dst2
                    PixelViewer.dst3Input = dst3
                    PixelViewer.Run(src)
                Else
                    PixelViewer.viewerForm.Visible = False
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
            useXYRange = True ' Most projections of pointcloud data can use the xRange and yRange to improve taskAlg.results..
        End Sub
    End Class
End Namespace