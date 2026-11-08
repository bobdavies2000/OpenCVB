Imports System.IO
Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Public Class TaskAlgorithms
    Public algTaskList() As String = {"DepthColorizer_Basics", "IMU_GMatrix", "Gravity_Basics",
                                      "IMU_Basics", "Motion_Basics", "HeartBeat_Basics",
                                      "Edge_Basics", "Delaunay_FeatureLess", "FeatureLess_Basics",
                                      "StableDepth_Basics", "StableGray_Basics", "Grid_Basics",
                                      "Filter_Basics", "LeftRight_Brightness", "SteadyCam_Basics"}
    Public Sub New()
        task.colorizer = New DepthColorizer_Basics
        task.gravityMatrix = New IMU_GMatrix
        task.gravityBasics = New Gravity_Basics
        task.imuBasics = New IMU_Basics
        task.motion = New Motion_Basics With {.standalone = False}
        task.heartBeats = New HeartBeat_Basics
        task.edges = New Edge_Basics
        task.delaunayFLess = New Delaunay_FeatureLess
        task.fless = New FeatureLess_Basics

        If task.gOptions.stableDepthRGB.Checked Then
            task.stableDepth = New StableDepth_Basics
            task.stableGray = New StableGray_Basics
        End If

        task.grid = New Grid_Basics

        task.filterBasics = New Filter_Basics
        task.leftRightBrightness = New LeftRight_Brightness
        task.steadyCam = New SteadyCam_Basics

        HeartBeat_Basics.setHeartBeat()
    End Sub
    Public Shared Sub RunTasks()
        ' run any task algorithms here
        task.grid.Run(task.color)
        task.imuBasics.Run(New cv.Mat)
        task.gravityMatrix.Run(New cv.Mat)

        If task.gOptions.CreateGif.Checked Then
            task.optionsChanged = False
        Else
            task.heartBeat = task.heartBeat Or task.optionsChanged Or task.mouseClickFlag
        End If

        task.filterBasics.Run(task.color.Clone)
        task.gray = task.filterBasics.dst3.Clone
        task.grayOriginal = task.gray.Clone
        task.originalPointCloud = task.pointCloud.Clone
        task.leftRightBrightness.Run(New cv.Mat)
        task.steadyCam.Run(task.gray)
        task.leftView = task.leftRightBrightness.dst2
        task.rightView = task.leftRightBrightness.dst3

        If task.stableGray Is Nothing Then
            task.motion.motionMask.SetTo(255)
            task.motion.motionSort.Clear()
            task.motion.Run(task.gray)
        Else
            task.motion.Run(task.gray)
            task.stableGray.Run(task.gray)
        End If

        If task.pixelViewerOn Then
            If task.PixelViewer Is Nothing Then
                task.PixelViewer = New PixelViewer.Pixel_Viewer
            End If
        End If

        If task.gOptions.CreateGif.Checked Then
            If task.gifCreator Is Nothing Then task.gifCreator = New Gif_OpenCVB
            task.gifCreator.Run(task.color.Clone)
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
            task.stableDepth.Run(New cv.Mat)
            task.depthRGB = task.stableDepth.dst2
        End If

        task.colorizer.Run(task.color)

        task.gravityBasics.Run(task.color.Clone)
        If task.lines IsNot Nothing Then task.lines.Run(task.gray)

        task.histBinList = {task.histogramBins, task.histogramBins, task.histogramBins}
        ' task.foreground.Run(New cv.Mat)
        If task.firstPass Then task.delaunayFLess.Run(task.gray)
        task.edges.Run(task.gray)
        task.fless.Run(task.gray)
        task.delaunayFLess.Run(task.gray)
        If task.redColor IsNot Nothing Then task.redColor.Run(task.gray)
    End Sub
End Class
