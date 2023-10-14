Imports cv = OpenCvSharp
Public Class Related_Basics : Inherits VB_Algorithm
    Public test(4 - 1) As Object
    Public Sub New()
        gOptions.displayDst0.Checked = True
        gOptions.displayDst1.Checked = True
        If standalone Then test = {New RedCloud_Basics, New Line_KNNCenters, New Convex_RedCloud, New Line_ID}
        desc = "A general-purpose algorithm to run multiple algorithms that are related but not in a variety of locations."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If test(0) IsNot Nothing Then test(0).Run(src.Clone)
        If test(1) IsNot Nothing Then test(1).Run(src.Clone)
        If test(2) IsNot Nothing Then test(2).Run(src.Clone)
        If test(3) IsNot Nothing Then test(3).Run(src.Clone)
        If standalone Then
            dst0 = test(0).dst2
            dst1 = test(1).dst2
            dst2 = test(2).dst2
            dst3 = test(3).dst2
            labels = {test(0).labels(2), test(1).labels(2), test(2).labels(2), test(3).labels(3)}
        End If
    End Sub
End Class







Public Class Related_Match : Inherits VB_Algorithm
    Dim main As New Related_Basics
    Public Sub New()
        main.test = {New Feature_GoodFeatureTrace, New Line_ID, New Match_GoodFeatureKNN, New RedCloud_Hulls}
        labels = {main.test(0).traceName, main.test(1).traceName, main.test(2).traceName, main.test(3).traceName}
        desc = "Some algorithms related to tracing features and matching but found in different files."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        main.Run(src)
        dst0 = main.test(0).dst2
        dst1 = main.test(1).dst2
        dst2 = main.test(2).dst2
        dst3 = main.test(3).dst2
        labels = {main.test(0).tracename + ": " + main.test(0).labels(2), main.test(1).tracename + ": " + main.test(1).labels(3),
                  main.test(2).tracename + ": " + main.test(2).labels(2), main.test(3).tracename + ": " + main.test(3).labels(2)}
    End Sub
End Class








Public Class Related_RedCloud2 : Inherits VB_Algorithm
    Dim main As New Related_Basics
    Public Sub New()
        main.test = {New RedCloud_Basics, New CComp_RedCloud, New RedCC_ColorCloud, New RedColor_FeatureLess}
        desc = "Some alternative uses for RedCloud tracking."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        main.Run(src)
        dst0 = main.test(0).dst2
        dst1 = main.test(1).dst2
        dst2 = main.test(2).dst2
        dst3 = main.test(3).dst2
        labels = {main.test(0).tracename + ": " + main.test(0).labels(2), main.test(1).tracename + ": " + main.test(1).labels(2),
                  main.test(2).tracename + ": " + main.test(2).labels(2), main.test(3).tracename + ": " + main.test(3).labels(2)}
    End Sub
End Class






Public Class Related_MouseCheck : Inherits VB_Algorithm
    Dim main As New Related_Basics
    Public Sub New()
        main.test = {New BackProject_Basics, New PointCloud_SurfaceH, New Structured_SliceH, New BackProject2D_Basics}
        desc = "Some algorithms related to tracing features and matching but found in different files."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        main.Run(src)
        dst0 = main.test(0).dst2
        dst1 = main.test(1).dst2
        dst2 = main.test(2).dst2
        dst3 = main.test(3).dst2
        labels = {main.test(0).tracename + ": " + main.test(0).labels(2), main.test(1).tracename + ": " + main.test(1).labels(2),
                  main.test(2).tracename + ": " + main.test(2).labels(2), main.test(3).tracename + ": " + main.test(3).labels(2)}
    End Sub
End Class








Public Class Related_Hull : Inherits VB_Algorithm
    Dim main As New Related_Basics
    Public Sub New()
        main.test = {New MSER_Contours, New Convex_RedCloud, New RedCloud_Hulls, New Draw_Polygon}
        desc = "Some algorithms related to hulls but found in different files."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        main.Run(src)
        dst0 = main.test(0).dst2
        dst1 = main.test(1).dst3
        dst2 = main.test(2).dst2
        dst3 = main.test(3).dst2
        labels = {main.test(0).tracename + ": " + main.test(0).labels(2), main.test(1).tracename + ": " + main.test(1).labels(3),
                  main.test(2).tracename + ": " + main.test(2).labels(2), main.test(3).tracename + ": " + main.test(3).labels(2)}
    End Sub
End Class







Public Class Related_Palette : Inherits VB_Algorithm
    Dim main As New Related_Basics
    Public Sub New()
        main.test = {New EMax_Basics, New Palette_Basics, New LUT_CustomColor, New MFI_FloodFill}
        desc = "Some algorithms that use palettes in different ways"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        main.Run(src)
        dst0 = main.test(0).dst2
        dst1 = main.test(1).dst2
        dst2 = main.test(2).dst2
        dst3 = main.test(3).dst2
        labels = {main.test(0).tracename + ": " + main.test(0).labels(2), main.test(1).tracename + ": " + main.test(1).labels(2),
                  main.test(2).tracename + ": " + main.test(2).labels(2), main.test(3).tracename + ": " + main.test(3).labels(2)}
    End Sub
End Class






Public Class Related_Random : Inherits VB_Algorithm
    Dim main As New Related_Basics
    Public Sub New()
        main.test = {New Random_KalmanPoints, New Random_CheckNormalDistSmoothed, New Random_CustomDistribution, New Random_Clusters}
        desc = "Using random numbers can be hard.  Lots of examples here."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        main.Run(src)
        dst0 = main.test(0).dst2
        dst1 = main.test(1).dst2
        dst2 = main.test(2).dst2
        dst3 = main.test(3).dst2
        labels = {main.test(0).tracename + ": " + main.test(0).labels(2), main.test(1).tracename + ": " + main.test(1).labels(2),
                  main.test(2).tracename + ": " + main.test(2).labels(2), main.test(3).tracename + ": " + main.test(3).labels(2)}
    End Sub
End Class






Public Class Related_Points : Inherits VB_Algorithm
    Dim main As New Related_Basics
    Public Sub New()
        main.test = {New Corners_FASTStablePoints, New Feature_ShiTomasi, New Feature_Basics, New RedCloud_Hulls}
        desc = "Identify points using different techniques."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        main.Run(src)
        dst0 = main.test(0).dst2
        dst1 = main.test(1).dst2
        dst2 = main.test(2).dst2
        dst3 = main.test(3).dst2
        labels = {main.test(0).tracename + ": " + main.test(0).labels(2), main.test(1).tracename + ": " + main.test(1).labels(2),
                  main.test(2).tracename + ": " + main.test(2).labels(2), main.test(3).tracename + ": " + main.test(3).labels(2)}
    End Sub
End Class





Public Class Related_Regions : Inherits VB_Algorithm
    Dim main As New Related_Basics
    Public Sub New()
        main.test = {New Feature_PointsDelaunay, New EMax_RandomClusters, New EMax_RandomClusters, New EMax_RandomClusters}
        desc = "Using delaunay and EMax to isolate regions in the image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        main.Run(src)
        dst0 = main.test(0).dst3
        dst1 = main.test(0).dst2
        dst2 = main.test(1).dst3
        dst3 = main.test(1).dst2
        labels = {main.test(0).tracename + ": " + main.test(0).labels(3), main.test(1).tracename + ": " + main.test(0).labels(2),
                  main.test(1).tracename + ": " + main.test(2).labels(2), main.test(3).tracename + ": " + main.test(1).labels(3)}
    End Sub
End Class







Public Class Related_Trace1 : Inherits VB_Algorithm
    Dim main As New Related_Basics
    Public Sub New()
        main.test = {New Feature_Points, New Feature_GoodFeatureTrace, New Feature_TrackerRedC, New RedColor_Basics}
        desc = "Identify traces of points using several different techniques."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        main.Run(src)
        dst0 = main.test(0).dst2
        dst1 = main.test(1).dst2
        dst2 = main.test(2).dst2
        dst3 = main.test(3).dst2
        labels = {main.test(0).tracename + ": " + main.test(0).labels(2), main.test(1).tracename + ": " + main.test(1).labels(2),
                  main.test(2).tracename + ": " + main.test(2).labels(2), main.test(3).tracename + ": " + main.test(3).labels(2)}
    End Sub
End Class








Public Class Related_Trace2 : Inherits VB_Algorithm
    Dim main As New Related_Basics
    Public Sub New()
        main.test = {New Match_GoodFeatureKNN, New Feature_Tracer, New Match_TraceRedC, New RedColor_Basics}
        desc = "Identify traces of points using several different techniques."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        main.Run(src)
        dst0 = main.test(0).dst2
        dst1 = main.test(1).dst2
        dst2 = main.test(2).dst2
        dst3 = main.test(3).dst2
        labels = {main.test(0).tracename + ": " + main.test(0).labels(2), main.test(1).tracename + ": " + main.test(1).labels(2),
                  main.test(2).tracename + ": " + main.test(2).labels(2), main.test(3).tracename + ": " + main.test(3).labels(2)}
    End Sub
End Class







Public Class Related_RGBFilters : Inherits VB_Algorithm
    Dim main As New Related_Basics
    Public Sub New()
        main.test = {New Contrast_Basics, New PhotoShop_SharpenDetail, New PhotoShop_WhiteBalance, New Filter_Laplacian}
        desc = "Identify image regions using several different techniques."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        main.Run(src)
        dst0 = main.test(0).dst2
        dst1 = main.test(1).dst2
        dst2 = main.test(2).dst2
        dst3 = main.test(3).dst2
        labels = {main.test(0).tracename + ": " + main.test(0).labels(2), main.test(1).tracename + ": " + main.test(1).labels(2),
                  main.test(2).tracename + ": " + main.test(2).labels(2), main.test(3).tracename + ": " + main.test(3).labels(2)}
    End Sub
End Class








Public Class Related_KMeans : Inherits VB_Algorithm
    Dim main As New Related_Basics
    Public Sub New()
        main.test = {New KMeans_Basics, New BackProject_Full, New Reduction_Basics, New RedCC_Basics}
        desc = "Reduce an image to basic regions."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        main.Run(src)
        dst0 = main.test(0).dst2
        dst1 = main.test(1).dst2
        dst2 = main.test(2).dst2
        dst3 = main.test(3).dst2
        labels = {main.test(0).tracename + ": " + main.test(0).labels(2), main.test(1).tracename + ": " + main.test(1).labels(2),
                  main.test(2).tracename + ": " + main.test(2).labels(2), main.test(3).tracename + ": " + main.test(3).labels(2)}
    End Sub
End Class







Public Class Related_Valleys : Inherits VB_Algorithm
    Dim main As New Related_Basics
    Public Sub New()
        main.test = {New HistValley_Basics, New HistValley_Clusters, New Blob_DepthRanges, New Histogram_PeakFinder}
        desc = "Find the best way to segment the depth data with ranges."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        main.Run(src)
        dst0 = main.test(0).dst3
        dst1 = main.test(1).dst3
        dst2 = main.test(2).dst3
        dst3 = main.test(3).dst2
        labels = {main.test(0).tracename + ": " + main.test(0).labels(2), main.test(1).tracename + ": " + main.test(1).labels(2),
                  main.test(2).tracename + ": " + main.test(2).labels(2), main.test(3).tracename + ": " + main.test(3).labels(2)}
    End Sub
End Class
