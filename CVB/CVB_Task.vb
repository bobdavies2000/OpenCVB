Imports cv = OpenCvSharp
Module GlobalVariables
    Public myTask As cvbTask

    Public Const fmt0 = "0"
    Public Const fmt1 = "0.0"
    Public Const fmt2 = "0.00"
    Public Const fmt3 = "0.000"
    Public Const fmt4 = "0.0000"
End Module
Public Class cvbTask
    Public optionsChanged As Boolean
    Public allOptions As New OptCVBsContainer
    Public gOptions As OptCVBGlobal
    Public featureOptions As OptCVBFeatures

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
    Public Sub New()
    End Sub
    Public Sub New(camImages As CameraImages.images, _settings As CVB.Json)
        myTask = Me
        settings = _settings
        allOptions.Show()
        gOptions = New OptCVBGlobal
        featureOptions = New OptCVBFeatures

        color = camImages.color
        pointCloud = camImages.pointCloud
        leftView = camImages.left
        rightView = camImages.right

        algName = settings.algorithm
        displayObjectName = algName
        cameraName = settings.cameraName
        homeDir = settings.homeDirPath
        pcSplit = pointCloud.Split()

        workRes = settings.workRes
        testAllDuration = settings.testAllDuration
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