Imports System.IO
Imports VBClasses
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Namespace CVB
    Partial Public Class MainForm
        Public task As vbTask
        Private Sub processImages(camImages As CameraImages.images)
            task = New vbTask()
            task.allOptions.settings = settings
            task.allOptions.Show()
            task.gOptions = New OptionsGlobal
            task.featureOptions = New OptionsFeatures

            task.color = camImages.color
            task.pointCloud = camImages.pointCloud
            task.leftView = camImages.left
            task.rightView = camImages.right

            gridRects = New List(Of cv.Rect)
            firstPass = True
            algName = parms.algName
            displayObjectName = algName
            cameraName = parms.cameraName
            testAllRunning = parms.testAllRunning
            showBatchConsole = parms.showBatchConsole
            fpsAlgorithm = parms.fpsRate
            fpsCamera = parms.fpsHostCamera
            CalibData = parms.calibData
            HomeDir = parms.HomeDir

            main_hwnd = parms.main_hwnd
            task.pcSplit = task.pointCloud.Split()
        End Sub
    End Class
End Namespace