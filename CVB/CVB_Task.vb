Imports cv = OpenCvSharp
Module GlobalVariables
    Public myTask As cvbTask

    Public Const fmt0 = "0"
    Public Const fmt1 = "0.0"
    Public Const fmt2 = "0.00"
    Public Const fmt3 = "0.000"
    Public Const fmt4 = "0.0000"

    Public cameraNames As New List(Of String)({"Intel(R) RealSense(TM) Depth Camera 435i",
                                               "Intel(R) RealSense(TM) Depth Camera 455",
                                               "Oak-D camera",
                                               "Orbbec Gemini 335",
                                               "Orbbec Gemini 335L",
                                               "Orbbec Gemini 336L",
                                               "StereoLabs ZED 2/2i"
                                               })
End Module
Public Class cvbTask
    Public dstList() As cv.Mat

    Public optionsChanged As Boolean
    Public allOptions As OptCVBContainer
    Public gOptions As OptCVBGlobal
    Public featureOptions As OptCVBFeatures
    Public treeView As TreeViewForm

    Public color As New cv.Mat
    Public gray As New cv.Mat
    Public grayStable As New cv.Mat
    Public leftViewStable As New cv.Mat
    Public leftView As New cv.Mat
    Public rightView As New cv.Mat
    Public pointCloud As New cv.Mat
    Public gravityCloud As New cv.Mat
    Public sharpDepth As cv.Mat
    Public sharpRGB As cv.Mat
    Public pcSplit() As cv.Mat

    Public gridRects As List(Of cv.Rect)
    Public firstPass As Boolean = True
    Public algName As String
    Public displayObjectName As String
    Public settings As CVB.Json
    Public cameraName As String
    Public homeDir As String

    Public testAllDuration As Integer
    Public verticalLines As Boolean
    Public edgeMethod As String

    Public workRes As cv.Size

    ' Global Options 
    Public DotSize As Integer
    Public lineWidth As Integer
    Public cvFontThickness As Integer
    Public brickSize As Integer
    Public reductionTarget As Integer
    Public cvFontSize As Single
    Public lineType As cv.LineTypes
    Public histogramBins As Integer
    Public MaxZmeters As Single
    Public highlight As cv.Scalar
    Public closeRequest As Boolean
    Public paletteIndex As Integer
    Public fCorrThreshold As Single
    Public FeatureSampleSize As Integer

    ' TreeView Data.
    Public callTrace As List(Of String)
    Public algorithm_msMain As New List(Of Single)
    Public algorithmNamesMain As New List(Of String)
    Public algorithm_ms As New List(Of Single)
    Public algorithmNames As New List(Of String)
    Public algorithmTimes As New List(Of DateTime)
    Public algorithmStack As New Stack()

    Public desc As String = ""

    Public fpsAlgorithm As Single
    Public fpsCamera As Single
    Public testAllRunning As Boolean

    Public Sub New()
    End Sub

    Public Sub New(camImages As CameraImages.images, _settings As CVB.Json)
        myTask = Me
        settings = _settings

        workRes = settings.workRes
        testAllDuration = settings.testAllDuration
        gravityCloud = New cv.Mat(workRes, cv.MatType.CV_32FC3, 0)

        allOptions = New OptCVBContainer
        allOptions.Show()

        featureOptions = New OptCVBFeatures
        featureOptions.Show()

        gOptions = New OptCVBGlobal
        gOptions.Show()

        treeView = New TreeViewForm
        treeView.Show()
        callTrace = New List(Of String)

        ' process the images and put the results in dstlist.
        dstList = {color, pointCloud, leftView, rightView}

        algName = settings.algorithm
        displayObjectName = algName
        cameraName = settings.cameraName
        homeDir = settings.homeDirPath
        pcSplit = pointCloud.Split()
    End Sub

    Public Enum oCase
        drawPointCloudRGB
        drawLineAndCloud
        drawFloor
        drawPyramid
        drawCube
        quadBasics
        minMaxBlocks
        drawTiles
        drawCell
        drawCells
        floorStudy
        data3D
        sierpinski
        polygonCell
        Histogram3D
        pcPoints
        line3D
        pcPointsAlone
        drawLines
        drawAvgPointCloudRGB
        readPC
        readQuads
        draw3DLines
        draw3DLinesAndCloud
        readLines
        colorTriangles
        imageTriangles
    End Enum
End Class