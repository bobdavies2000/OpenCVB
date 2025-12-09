Imports cv = OpenCvSharp
Namespace MainForm
    Public Class cvbTask
        Public dst() As cv.Mat

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
        Public depthmask As cv.Mat
        Public noDepthMask As cv.Mat
        Public depthRGB As cv.Mat

        Public gridRects As List(Of cv.Rect)
        Public firstPass As Boolean = True
        Public algName As String
        Public cameraName As String

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
        Public clickPoint As New cv.Point ' last place where mouse was clicked.

        ' TreeView and trace Data.
        Public callTrace As List(Of String)
        Public algorithm_msMain As New List(Of Single)
        Public algorithmNamesMain As New List(Of String)
        Public algorithm_ms As New List(Of Single)
        Public algorithmNames As New List(Of String)
        Public algorithmTimes As New List(Of DateTime)
        Public algorithmStack As New Stack()
        Public displayObjectName As String
        Public activeObjects As New List(Of Object)
        Public calibData As cameraInfo

        Public desc As String = ""

        Public fpsAlgorithm As Single
        Public fpsCamera As Single
        Public testAllRunning As Boolean

        ' color maps
        Public scalarColors(255) As cv.Scalar
        Public vecColors(255) As cv.Vec3b
        Public depthColorMap As cv.Mat
        Public colorMap As cv.Mat
        Public colorMapZeroIsBlack As cv.Mat
        Public correlationColorMap As cv.Mat

        ' task algorithms - operate on every frame regardless of which algorithm is being run.
        Public colorizer As DepthColorizer_Basics

        Public Sub New()
        End Sub

        Public Sub New(camImages As CameraImages.images, _settings As Json)
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

            algName = settings.algorithm
            displayObjectName = algName
            cameraName = settings.cameraName
        End Sub

        Public Sub RunAlgorithm()
            If pcSplit.Count > 0 Then
                colorizer.Run(pcSplit(2))
                dst(1) = colorizer.dst2
            End If
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
End Namespace