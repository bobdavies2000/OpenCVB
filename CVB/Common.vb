Imports cv = OpenCvSharp

Public Class vbTask
    Public optionsChanged As Boolean
    Public allOptions As New OptionsContainer
    Public gOptions As OptionsGlobal
    Public featureOptions As OptionsFeatures

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