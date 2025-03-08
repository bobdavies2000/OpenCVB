Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Drawing

Module TaskExterns
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SemiGlobalMatching_Open(rows As Integer, cols As Integer, disparityRange As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SemiGlobalMatching_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SemiGlobalMatching_Run(SemiGlobalMatchingPtr As IntPtr, leftPtr As IntPtr, rightPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function







    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Annealing_Basics_Open(cityPositions As IntPtr, numberOfCities As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Annealing_Basics_Close(saPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Annealing_Basics_Run(saPtr As IntPtr, cityOrder As IntPtr, numberOfCities As Integer) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub MinTriangle_Run(inputPtr As IntPtr, numberOfPoints As Integer, outputTriangle As IntPtr)
    End Sub




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function BGSubtract_BGFG_Open(currMethod As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function BGSubtract_BGFG_Close(bgfs As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function BGSubtract_BGFG_Run(bgfs As IntPtr, bgrPtr As IntPtr, rows As Integer, cols As Integer, channels As Integer,
                                        learnRate As Double) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function BGSubtract_Synthetic_Open(bgrPtr As IntPtr, rows As Integer, cols As Integer, fgFilename As String, amplitude As Double,
                                          magnitude As Double, wavespeed As Double, objectspeed As Double) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function BGSubtract_Synthetic_Close(synthPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function BGSubtract_Synthetic_Run(synthPtr As IntPtr) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Corners_ShiTomasi(grayPtr As IntPtr, rows As Integer, cols As Integer, blocksize As Integer, aperture As Integer) As IntPtr
    End Function



    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Harris_Features_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Harris_Features_Close(Harris_FeaturesPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Harris_Features_Run(Harris_FeaturesPtr As IntPtr, inputPtr As IntPtr, rows As Integer, cols As Integer, threshold As Single,
                                        neighborhood As Int16, aperture As Int16, HarrisParm As Single) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function Harris_Detector_Count(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Harris_Detector_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Harris_Detector_Close(Harris_FeaturesPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Harris_Detector_Run(Harris_FeaturesPtr As IntPtr, inputPtr As IntPtr, rows As Integer, cols As Integer, qualityLevel As Double) As IntPtr
    End Function



    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function cppTask_Open(cppFunction As Integer, rows As Integer, cols As Integer,
                             heartBeat As Boolean, addWeighted As Single, lineWidth As Integer,
                             lineType As Integer, DotSize As Integer, cellSize As Integer,
                             histogramBins As Integer, ocvheartBeat As Boolean, gravityPointCloud As Boolean,
                             pixelDiffThreshold As Integer, UseKalman As Boolean, paletteIndex As Integer,
                             frameHistory As Integer, displayDst0 As Boolean,
                             displayDst1 As Boolean) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function cppTask_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function cppTask_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, channels As Integer, frameCount As Integer,
                                   rows As Integer, cols As Integer, x As Single, y As Single, z As Single,
                                   optionsChanged As Boolean, heartBeat As Boolean, displayDst0 As Boolean,
                                   displayDst1 As Boolean, debugCheckBox As Boolean) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function cppTask_PointCloud(cPtr As IntPtr, dataPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function

    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function cppTask_DepthLeftRight(cPtr As IntPtr, dataPtr As IntPtr, leftPtr As IntPtr,
                                           rightPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function

    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function cppTask_GetDst(cPtr As IntPtr, index As Integer, ByRef type As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub cppTask_OptionsCPPtoVB(cPtr As IntPtr, ByRef cellSize As Integer,
                                      ByRef histogramBins As Integer,
                                      ByRef pixelDiffThreshold As Integer,
                                      ByRef UseKalman As Boolean, ByRef frameHistory As Integer,
                                      ByRef rectX As Integer, ByRef rectY As Integer, ByRef rectWidth As Integer,
                                      ByRef rectHeight As Integer,
                                      <MarshalAs(UnmanagedType.LPStr)> ByVal labels As StringBuilder,
                                      <MarshalAs(UnmanagedType.LPStr)> ByVal descBuffer As StringBuilder,
                                      <MarshalAs(UnmanagedType.LPStr)> ByVal adviceBuffer As StringBuilder
                                      )
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub cppTask_OptionsVBtoCPP(cPtr As IntPtr, cellSize As Integer,
                                      histogramBins As Integer,
                                      pixelDiffThreshold As Integer,
                                      UseKalman As Boolean, frameHistory As Integer,
                                      rectX As Integer, rectY As Integer, rectWidth As Integer,
                                      rectHeight As Integer, lineWidth As Integer,
                                      lineType As Integer, DotSize As Integer, lowResWidth As Integer,
                                      lowResHeight As Integer, MaxZmeters As Single,
                                      PointCloudReduction As Integer, fontSize As Single,
                                      fontThickness As Integer, clickX As Integer,
                                      clickY As Integer, clickFlag As Boolean, picTag As Integer,
                                      moveX As Integer, moveY As Integer, paletteIndex As Integer,
                                      desircList As Integer, midHeartBeat As Boolean,
                                      quarterBeat As Boolean, colorInputIndex As Integer, depthInputIndex As Integer,
                                      xRangeDefault As Single, yRangeDefault As Single)
    End Sub






    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Denoise_Basics_Open(frameCount As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Denoise_Basics_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Denoise_Basics_Run(cPtr As IntPtr, bgrPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Denoise_SinglePixels_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Denoise_SinglePixels_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Denoise_SinglePixels_Run(cPtr As IntPtr, bgrPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Denoise_Pixels_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Denoise_Pixels_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Denoise_Pixels_EdgeCountBefore(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Denoise_Pixels_EdgeCountAfter(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Denoise_Pixels_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function


    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Density_2D_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Density_2D_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Density_2D_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32, distance As Double) As IntPtr
    End Function

    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Density_Count_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Density_Count_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Density_Count_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32, zCount As Integer) As IntPtr
    End Function




    Public minLengthContour = 4 ' use any contour with enough points to make a contour!

    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer_Close(Depth_ColorizerPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer_Run(Depth_ColorizerPtr As IntPtr, bgrPtr As IntPtr, rows As Integer, cols As Integer,
                                        maxDepth As Single) As IntPtr
    End Function






    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayer16(pixels As IntPtr, width As Integer, height As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayer8(pixels As IntPtr, width As Integer, height As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayer4(pixels As IntPtr, width As Integer, height As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayer3(pixels As IntPtr, width As Integer, height As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayer2(pixels As IntPtr, width As Integer, height As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayerRgbNbpp(pixels As IntPtr, width As Integer, height As Integer, nColors As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayerRgb3bpp(pixels As IntPtr, width As Integer, height As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayerRgb6bpp(pixels As IntPtr, width As Integer, height As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayerRgb9bpp(pixels As IntPtr, width As Integer, height As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayerRgb12bpp(pixels As IntPtr, width As Integer, height As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayerRgb15bpp(pixels As IntPtr, width As Integer, height As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayerRgb18bpp(pixels As IntPtr, width As Integer, height As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherFSRgbNbpp(pixels As IntPtr, width As Integer, height As Integer, nColors As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherFS(pixels As IntPtr, width As Integer, height As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherFSRgb3bpp(pixels As IntPtr, width As Integer, height As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherFSRgb6bpp(pixels As IntPtr, width As Integer, height As Integer)
    End Sub

    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherFSRgb9bpp(pixels As IntPtr, width As Integer, height As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherFSRgb12bpp(pixels As IntPtr, width As Integer, height As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherFSRgb15bpp(pixels As IntPtr, width As Integer, height As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherFSRgb18bpp(pixels As IntPtr, width As Integer, height As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherSierraLiteRgbNbpp(pixels As IntPtr, width As Integer, height As Integer, nColors As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherSierraLite(pixels As IntPtr, width As Integer, height As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherSierraRgbNbpp(pixels As IntPtr, width As Integer, height As Integer, nColors As Integer)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherSierra(pixels As IntPtr, width As Integer, height As Integer)
    End Sub





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edge_RandomForest_Open(modelFileName As String) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edge_RandomForest_Close(Edges_RandomForestPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edge_RandomForest_Run(Edges_RandomForestPtr As IntPtr, inputPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function

    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edge_Deriche_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edge_Deriche_Close(Edges_DerichePtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edge_Deriche_Run(Edges_DerichePtr As IntPtr, bgrPtr As IntPtr, rows As Integer, cols As Integer, alpha As Single, omega As Single) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edge_ColorGap_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edge_ColorGap_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edge_ColorGap_Run(cPtr As IntPtr, bgrPtr As IntPtr, rows As Int32, cols As Int32, distance As Int32, diff As Int32) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edge_DepthGap_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edge_DepthGap_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edge_DepthGap_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32, minDiff As Single) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EdgeLines_Image_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EdgeLines_Image_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EdgeLines_Image_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32, lineWidth As Integer) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EdgeLines_Edges_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EdgeLines_Edges_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EdgeLines_Edges_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32, lineWidth As Integer) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EdgeLines_Lines_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EdgeLines_Lines_Count(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EdgeLines_Lines_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EdgeLines_Lines_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32, lineWidth As Integer) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EdgeLines_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EdgeLines_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EdgeLines_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32, lineWidth As Integer) As IntPtr
    End Function

    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EdgeLines_NextLength(cPtr As IntPtr) As Integer
    End Function

    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EdgeLines_GetLength(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EdgeLines_NextSegment(cPtr As IntPtr) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EdgeLine_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EdgeLine_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function EdgeLine_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, maskPtr As IntPtr, rows As Int32, cols As Int32, lineWidth As Integer) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EMax_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function EMax_Close(EMax_RawPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function EMax_Run(EMax_RawPtr As IntPtr, samplesPtr As IntPtr, labelsPtr As IntPtr, inputCount As Integer, dimension As Integer, rows As Integer, cols As Integer,
                                 clusters As Integer, stepSize As Integer, covarianceMatrixType As Integer) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Agast_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function Agast_Count(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Agast_Close(Harris_FeaturesPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Agast_Run(Harris_FeaturesPtr As IntPtr, inputPtr As IntPtr, rows As Integer, cols As Integer,
                              threshold As Integer) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function FitEllipse_AMS(inputPtr As IntPtr, numberOfPoints As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function FitEllipse_Direct(inputPtr As IntPtr, numberOfPoints As Integer) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Fuzzy_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Fuzzy_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Fuzzy_Run(cPtr As IntPtr, bgrPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Guess_Depth_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Guess_Depth_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Guess_Depth_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Guess_ImageEdges_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Guess_ImageEdges_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Guess_ImageEdges_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32, maxDistanceToEdge As Int32) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Hist_1D_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Hist_1D_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Hist_1D_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32,
                                        bins As Integer) As IntPtr
    End Function

    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Hist_1D_Sum(cPtr As IntPtr) As Single
    End Function

    Public histDelta = 0.00001




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Hist3Dcolor_Run(bgrPtr As IntPtr, rows As Integer, cols As Integer, bins As Integer) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function BackProjectBGR_Run(bgrPtr As IntPtr, rows As Integer, cols As Integer, bins As Integer,
                                       threshold As Single) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Hist3Dcloud_Run(pcPtr As IntPtr, rows As Integer, cols As Integer, bins As Integer,
                                    minX As Single, minY As Single, minZ As Single,
                                    maxX As Single, maxY As Single, maxZ As Single) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function BackProjectCloud_Run(pcPtr As IntPtr, rows As Integer, cols As Integer, bins As Integer, threshold As Single,
                                         minX As Single, minY As Single, minZ As Single,
                                         maxX As Single, maxY As Single, maxZ As Single) As IntPtr
    End Function







    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function HMM_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function HMM_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function HMM_Run(HMMPtr As IntPtr, bgrPtr As IntPtr, rows As Integer, cols As Integer, channels As Integer) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KMeans_MultiGaussian_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KMeans_MultiGaussian_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KMeans_MultiGaussian_RunCPP(cPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Kmeans_Simple_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Kmeans_Simple_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Kmeans_Simple_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32, minVal As Single, maxVal As Single) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function ML_RemoveDups_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function ML_RemoveDups_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function ML_RemoveDups_GetCount(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function ML_RemoveDups_Run(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32, type As Integer) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MSER_Open(delta As Integer, minArea As Integer, maxArea As Integer, maxVariation As Single, minDiversity As Single,
                          maxEvolution As Integer, areaThreshold As Single, minMargin As Single, edgeBlurSize As Integer,
                          pass2Setting As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub MSER_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MSER_Rects(cPtr As IntPtr) As IntPtr
    End Function

    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MSER_FloodPoints(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MSER_MaskCounts(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MSER_Count(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MSER_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Integer, cols As Integer, channels As Integer) As IntPtr
    End Function





    Public removeZeroNeighbors As Boolean = True

    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbors1_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Neighbors1_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbors1_CellData(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbors1_Points(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbors1_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Integer, cols As Integer) As Integer
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbor2_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Neighbor2_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbor2_Points(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbor2_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Integer, cols As Integer) As Integer
    End Function






    ' https://docs.opencv.org/3.4/db/d7f/tutorial_js_lucas_kanade.html
    Public Function opticalFlow_Dense(oldGray As cv.Mat, gray As cv.Mat, pyrScale As Single, levels As Integer, winSize As Integer, iterations As Integer,
                                polyN As Single, polySigma As Single, OpticalFlowFlags As cv.OpticalFlowFlags) As cv.Mat
        Dim flow As New cv.Mat
        If pyrScale >= 1 Then pyrScale = 0.99

        ' When running "Test All", the OpenGL code requires full resolution which switches to low resolution (if active) after completion.
        ' The first frame after switching will mean oldgray is full resolution and gray is low resolution.  This "If" avoids the problem.
        ' if another algorithm lexically follows the OpenGL algorithms, this may change (or be deleted!)
        If oldGray.Size() <> gray.Size() Then oldGray = gray.Clone()

        cv.Cv2.CalcOpticalFlowFarneback(oldGray, gray, flow, pyrScale, levels, winSize, iterations, polyN, polySigma, OpticalFlowFlags)
        Dim flowVec(1) As cv.Mat
        flowVec = flow.Split()

        Dim hsv As New cv.Mat
        Dim hsv0 As New cv.Mat
        Dim hsv1 As New cv.Mat(gray.Rows, gray.Cols, cv.MatType.CV_8UC1, cv.Scalar.All(255))
        Dim hsv2 As New cv.Mat

        Dim magnitude As New cv.Mat
        Dim angle As New cv.Mat
        cv.Cv2.CartToPolar(flowVec(0), flowVec(1), magnitude, angle)
        angle.ConvertTo(hsv0, cv.MatType.CV_8UC1, 180 / Math.PI / 2)
        cv.Cv2.Normalize(magnitude, hsv2, 0, 255, cv.NormTypes.MinMax, cv.MatType.CV_8UC1)

        Dim hsvVec() As cv.Mat = {hsv0, hsv1, hsv2}
        cv.Cv2.Merge(hsvVec, hsv)
        Return hsv
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OpticalFlow_CPP_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OpticalFlow_CPP_Close(sPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OpticalFlow_CPP_Run(sPtr As IntPtr, bgrPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function
    Public Sub calcOpticalFlowPyrLK_Native(gray1 As cv.Mat, gray2 As cv.Mat, features1 As cv.Mat, features2 As cv.Mat)
        Dim hGray1 As GCHandle
        Dim hGray2 As GCHandle
        Dim hF1 As GCHandle
        Dim hF2 As GCHandle

        Dim grayData1(gray1.Total - 1)
        Dim grayData2(gray2.Total - 1)
        Dim fData1(features1.Total * features1.ElemSize - 1)
        Dim fData2(features2.Total * features2.ElemSize - 1)
        hGray1 = GCHandle.Alloc(grayData1, GCHandleType.Pinned)
        hGray2 = GCHandle.Alloc(grayData2, GCHandleType.Pinned)
        hF1 = GCHandle.Alloc(fData1, GCHandleType.Pinned)
        hF2 = GCHandle.Alloc(fData2, GCHandleType.Pinned)
    End Sub






    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function ParticleFilterTest_Open(matlabFileName As String, rows As Integer, cols As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function ParticleFilterTest_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function ParticleFilterTest_Run(pfPtr As IntPtr) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function PCA_Prep_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Sub PCA_Prep_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function PCA_Prep_GetCount(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function PCA_Prep_Run(cPtr As IntPtr, dataPtr As IntPtr,
                                                                                                              rows As Integer, cols As Integer) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function WhiteBalance_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function WhiteBalance_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function WhiteBalance_Run(wPtr As IntPtr, rgb As IntPtr, rows As Integer, cols As Integer, thresholdVal As Single) As IntPtr
    End Function




    ' for performance we are putting this in an optimized C++ interface to the K4A camera for convenience...
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SimpleProjectionRun(cPtr As IntPtr, depth As IntPtr, desiredMin As Single, desiredMax As Single, rows As Integer, cols As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SimpleProjectionSide(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SimpleProjectionOpen() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub SimpleProjectionClose(cPtr As IntPtr)
    End Sub
    Public Class compareAllowIdenticalDoubleInverted : Implements IComparer(Of Double)
        Public Function Compare(ByVal a As Double, ByVal b As Double) As Integer Implements IComparer(Of Double).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Public Class compareAllowIdenticalDouble : Implements IComparer(Of Double)
        Public Function Compare(ByVal a As Double, ByVal b As Double) As Integer Implements IComparer(Of Double).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a >= b Then Return 1
            Return -1
        End Function
    End Class
    Public Class compareAllowIdenticalSingleInverted : Implements IComparer(Of Single)
        Public Function Compare(ByVal a As Single, ByVal b As Single) As Integer Implements IComparer(Of Single).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Public Class compareAllowIdenticalSingle : Implements IComparer(Of Single)
        Public Function Compare(ByVal a As Single, ByVal b As Single) As Integer Implements IComparer(Of Single).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a >= b Then Return 1
            Return -1
        End Function
    End Class
    Public Class compareAllowIdenticalIntegerInverted : Implements IComparer(Of Integer)
        Public Function Compare(ByVal a As Integer, ByVal b As Integer) As Integer Implements IComparer(Of Integer).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Public Class compareByte : Implements IComparer(Of Byte)
        Public Function Compare(ByVal a As Byte, ByVal b As Byte) As Integer Implements IComparer(Of Byte).Compare
            If a <= b Then Return -1
            Return 1
        End Function
    End Class
    Public Class compareAllowIdenticalInteger : Implements IComparer(Of Integer)
        Public Function Compare(ByVal a As Integer, ByVal b As Integer) As Integer Implements IComparer(Of Integer).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a >= b Then Return 1
            Return -1
        End Function
    End Class
    Public Class CompareMaskSize : Implements IComparer(Of Integer)
        Public Function Compare(ByVal a As Integer, ByVal b As Integer) As Integer Implements IComparer(Of Integer).Compare
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Public Function findNearestCentroid(detailPoint As cv.Point, centroids As List(Of cv.Point)) As Integer
        Dim minIndex As Integer
        Dim minDistance As Single = Single.MaxValue
        For i = 0 To centroids.Count - 1
            Dim pt = centroids.ElementAt(i)
            Dim distance = Math.Sqrt((detailPoint.X - pt.X) * (detailPoint.X - pt.X) + (detailPoint.Y - pt.Y) * (detailPoint.Y - pt.Y))
            If distance < minDistance Then
                minIndex = i
                minDistance = distance
            End If
        Next
        Return minIndex
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Random_PatternGenerator_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Random_PatternGenerator_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Random_PatternGenerator_Run(Random_PatternGeneratorPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function


    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Random_DiscreteDistribution_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Random_DiscreteDistribution_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Random_DiscreteDistribution_Run(rPtr As IntPtr, rows As Integer, cols As Integer, channels As Integer) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RecursiveBilateralFilter_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RecursiveBilateralFilter_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RecursiveBilateralFilter_Run(cPtr As IntPtr, inputPtr As IntPtr, rows As Integer, cols As Integer, recursions As Integer) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbors_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Neighbors_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbors_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Integer, cols As Integer) As Integer
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbors_NabList(cPtr As IntPtr) As IntPtr
    End Function






    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedMask_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedMask_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedMask_Rects(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedMask_FloodPoints(cPtr As IntPtr) As IntPtr
    End Function

    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedMask_Sizes(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedMask_Count(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedMask_Run(cPtr As IntPtr, dataPtr As IntPtr, maskPtr As IntPtr,
                                 rows As Integer, cols As Integer, minSize As Integer) As IntPtr
    End Function






    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RedCloudMaxDist_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RedCloudMaxDist_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RedCloudMaxDist_Rects(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RedCloudMaxDist_Sizes(cPtr As IntPtr) As IntPtr
    End Function

    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub RedCloudMaxDist_SetPoints(cPtr As IntPtr, count As Integer, maxList As IntPtr)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RedCloudMaxDist_Count(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RedCloudMaxDist_Run(
                cPtr As IntPtr, dataPtr As IntPtr, maskPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function







    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function PlotOpenCV_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function PlotOpenCV_Close(cPtr As IntPtr) As IntPtr
    End Function

    Public backColor = cv.Scalar.DarkGray

    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function PlotOpenCV_Run(cPtr As IntPtr, inX As IntPtr, inY As IntPtr, inLen As Integer,
                                     rows As Integer, cols As Integer) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedColor_FindCells_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub RedColor_FindCells_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedColor_FindCells_TotalCount(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedColor_FindCells_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function






    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Pixels_Vector_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Pixels_Vector_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Pixels_Vector_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Integer, cols As Integer) As Integer
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Pixels_Vector_Pixels(cPtr As IntPtr) As IntPtr
    End Function





    Public Function shapeCorrelation(points As List(Of cv.Point)) As Single
        Dim pts As cv.Mat = cv.Mat.FromPixelData(points.Count, 1, cv.MatType.CV_32SC2, points.ToArray)
        Dim pts32f As New cv.Mat
        pts.ConvertTo(pts32f, cv.MatType.CV_32FC2)
        Dim split = pts32f.Split()
        Dim correlationMat As New cv.Mat
        cv.Cv2.MatchTemplate(split(0), split(1), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
        Return correlationMat.Get(Of Single)(0, 0)
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Retina_Basics_Open(rows As Integer, cols As Integer, useLogSampling As Boolean, samplingFactor As Single) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Retina_Basics_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Retina_Basics_Run(RetinaPtr As IntPtr, bgrPtr As IntPtr, rows As Integer, cols As Integer, magno As IntPtr, useLogSampling As Integer) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Salience_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Salience_Run(classPtr As IntPtr, numScales As Integer, grayInput As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Salience_Close(cPtr As IntPtr) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Sort_MLPrepTest_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Sort_MLPrepTest_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Sort_MLPrepTest_Run(cPtr As IntPtr, bgrPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function



    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function FLess_Range_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function FLess_Range_Count(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function FLess_Range_Close(cPtr As IntPtr) As IntPtr
    End Function

    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function FLess_Range_Run(cPtr As IntPtr, bgrPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SuperPixel_Open(width As Integer, height As Integer, num_superpixels As Integer, num_levels As Integer, prior As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SuperPixel_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SuperPixel_GetLabels(spPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SuperPixel_Run(spPtr As IntPtr, bgrPtr As IntPtr) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function Tracker_Basics_Open(trackType As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function Tracker_Basics_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Tracker_Basics_Run(cPtr As IntPtr, bgrPtr As IntPtr, rows As Integer, cols As Integer,
                                       x As Integer, y As Integer, w As Integer, h As Integer) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Vignetting_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Vignetting_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Vignetting_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32, radius As Double,
                                      centerX As Double, centerY As Double, removeal As Boolean) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function VoronoiDemo_Open(matlabFileName As String, rows As Integer, cols As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function VoronoiDemo_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function VoronoiDemo_Run(pfPtr As IntPtr, Input As IntPtr, pointCount As Integer, width As Integer, height As Integer) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function WarpModel_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function WarpModel_Close(WarpModelPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function WarpModel_Run(WarpModelPtr As IntPtr, src1Ptr As IntPtr, src2Ptr As IntPtr, rows As Integer, cols As Integer, channels As Integer, warpMode As Integer) As IntPtr
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function xPhoto_OilPaint_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function xPhoto_OilPaint_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function xPhoto_OilPaint_Run(xPhoto_OilPaint_Ptr As IntPtr, bgrPtr As IntPtr, rows As Integer, cols As Integer,
                                       size As Integer, dynRatio As Integer, colorCode As Integer) As IntPtr
    End Function


    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function xPhoto_Inpaint_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function xPhoto_Inpaint_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function xPhoto_Inpaint_Run(xPhoto_Inpaint_Ptr As IntPtr, bgrPtr As IntPtr, maskPtr As IntPtr, rows As Integer, cols As Integer, iType As Integer) As IntPtr
    End Function


    <DllImport("gdi32.dll")>
    Public Function BitBlt(ByVal hdc As IntPtr, ByVal nXDest As Integer, ByVal nYDest As Integer, ByVal nWidth As Integer, ByVal nHeight As Integer,
                        ByVal hdcSrc As IntPtr, ByVal nXSrc As Integer, ByVal nYSrc As Integer, ByVal dwRop As CopyPixelOperation) As Boolean
    End Function

    <DllImport("user32.dll", SetLastError:=True, CharSet:=CharSet.Auto)>
    Public Function FindWindow(ByVal lpClassName As String, ByVal lpWindowName As String) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Public Function SetForegroundWindow(ByVal hWnd As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    Public Declare Auto Function MoveWindow Lib "user32.dll" (ByVal hWnd As IntPtr, ByVal X As Int32, ByVal Y As Int32, ByVal nWidth As Int32,
                                                              ByVal nHeight As Int32, ByVal bRepaint As Boolean) As Boolean

    Public Declare Function GetWindowRect Lib "user32" (ByVal HWND As Integer, ByRef lpRect As RECT) As Integer
    <StructLayout(LayoutKind.Sequential)> Public Structure RECT
        Dim Left As Integer
        Dim Top As Integer
        Dim Right As Integer
        Dim Bottom As Integer
    End Structure
    <DllImport("user32.dll", SetLastError:=True)>
    Public Function SetWindowPos(ByVal hWnd As IntPtr, ByVal hWndInsertAfter As IntPtr, ByVal X As Integer, ByVal Y As Integer,
                                  ByVal cx As Integer, ByVal cy As Integer, ByVal uFlags As UInteger) As Boolean
    End Function




    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edge_DiffX_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Edge_DiffX_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edge_DiffX_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Integer, cols As Integer, channels As Integer) As IntPtr
    End Function





    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edge_DiffY_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Edge_DiffY_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edge_DiffY_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Integer, cols As Integer, channels As Integer) As IntPtr
    End Function
End Module
