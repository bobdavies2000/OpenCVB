Imports cvb = OpenCvSharp
Imports System.IO
Public Class SLR_Basics : Inherits VB_Parent
    Public slrCore As New SLR_Core
    Public plot As New Plot_Points
    Public Sub New()
        desc = "Segmented Linear Regression example"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        slrCore.Run(src)

        If task.FirstPass And standalone Then
            Static slrInput As New SLR_PlotTest()
            slrInput.getData(slrCore.inputX, slrCore.inputY)
        End If

        labels(2) = "Tolerance = " & slrCore.options.tolerance.ToString() &
                    " and moving average window = " & slrCore.options.halfLength.ToString()
        If slrCore.inputX.Count > 0 Then
            plot.input = slrCore.input
            plot.Run(src)
            dst2 = plot.dst2.Clone()

            plot.input = slrCore.output
            plot.Run(src)
            dst3 = plot.dst2
        End If
    End Sub
End Class




Public Class SLR_Core : Inherits VB_Parent
    Dim slr As New SLR()
    Public inputX As New List(Of Double)
    Public inputY As New List(Of Double)
    Public output As New List(Of cvb.Point2d)
    Public input As New List(Of cvb.Point2d)
    Public options As New Options_SLR()
    Public Sub New()
        desc = "The core algorithm for Segmented Linear Regression"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If inputX.Count = 0 Then
            SetTrueText("No input provided.  Update inputX and inputY and test again." + vbCrLf +
                        traceName + " when run standalone has no output.")
            Exit Sub
        End If

        Dim outputX As New List(Of Double)
        Dim outputY As New List(Of Double)
        slr.SegmentedRegressionFast(inputX, inputY, options.tolerance, options.halfLength,
                                    outputX, outputY)

        output.Clear()
        input.Clear()
        For i = 0 To outputX.Count - 1
            output.Add(New cvb.Point2d(outputX(i), outputY(i)))
            input.Add(New cvb.Point2d(inputX(i), inputY(i)))
        Next

        labels(2) = "Tolerance = " & options.tolerance.ToString() & " and moving average window = " & options.halfLength.ToString()
    End Sub
End Class





Public Class SLR_Plot : Inherits VB_Parent
    Dim plot As New Plot_Basics_CPP_VB()
    Dim slr As New SLR()
    Dim options As New Options_SLR()
    Public dataX As New List(Of Double)
    Public dataY As New List(Of Double)
    Public Sub New()
        desc = "Segmented Linear Regression example"
    End Sub

    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        If task.FirstPass And standalone Then
            Static slrInput As New SLR_PlotTest()
            slrInput.getData(dataX, dataY)
        End If

        Dim resultX As New List(Of Double)()
        Dim resultY As New List(Of Double)()

        slr.SegmentedRegressionFast(dataX, dataY, options.tolerance, options.halfLength, resultX, resultY)

        labels(2) = "Tolerance = " & options.tolerance.ToString() & " and moving average window = " & options.halfLength.ToString()
        If resultX.Count > 0 Then
            plot.srcX = dataX
            plot.srcY = dataY
            plot.Run(src)
            dst2 = plot.dst2.Clone()

            plot.srcX = resultX
            plot.srcY = resultY
            plot.Run(src)
            dst3 = plot.dst2
        Else
            dst2.SetTo(0)
            dst3.SetTo(0)
            SetTrueText(labels(2) & " yielded no results...")
        End If
        If Not standaloneTest() Then
            dataX.Clear()
            dataY.Clear()
        End If
    End Sub
End Class






' https://www.codeproject.com/Articles/5282014/Segmented-Linear-Regression
Public Class SLR_PlotTest : Inherits VB_Parent
    Dim plot As New Plot_Basics_CPP_VB
    Public dataX As New List(Of Double)
    Public dataY As New List(Of Double)
    Public Sub New()
        getData(dataX, dataY)
        desc = "Plot the data used in SLR_Basics"
    End Sub
    Public Sub getData(ByRef x As List(Of Double), ByRef y As List(Of Double))
        Dim sr = New StreamReader(task.HomeDir + "/Data/real_data.txt")
        Dim code As String = sr.ReadToEnd
        sr.Close()

        Dim lines = code.Split(vbLf)
        For Each line In lines
            Dim split = line.Split(" ")
            If split.Length > 1 Then
                x.Add(split(0))
                y.Add(split(1))
            End If
        Next
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        plot.srcX = dataX
        plot.srcY = dataY
        plot.Run(src)
        dst2 = plot.dst2
    End Sub
End Class






Public Class SLR_TrendImages : Inherits VB_Parent
    Dim trends As New SLR_Trends
    Dim options As New Options_SLRImages
    Public Sub New()
        desc = "Find trends by filling in short histogram gaps for depth or 1-channel images"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim split = src.Split()
        trends.hist.plot.maxRange = 255
        trends.hist.plot.removeZeroEntry = False ' default is to look at element 0....

        Dim splitIndex = 0
        Select Case options.radioText
            Case "pcSplit(2) input"
                trends.hist.plot.maxRange = task.MaxZmeters
                trends.hist.plot.removeZeroEntry = True ' not interested in the undefined depth areas...
                trends.Run(task.pcSplit(2))
                labels(2) = "SLR_TrendImages - pcSplit(2)"
            Case "Grayscale input"
                trends.Run(src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY))
                labels(2) = "SLR_TrendImages - grayscale"
            Case "Blue input"
                labels(2) = "SLR_TrendImages - Blue channel"
                splitIndex = 0
            Case "Green input"
                labels(2) = "SLR_TrendImages - Green channel"
                splitIndex = 1
            Case "Red input"
                labels(2) = "SLR_TrendImages - Red channel"
                splitIndex = 2
        End Select
        trends.Run(split(splitIndex))
        dst2 = trends.dst2
    End Sub
End Class










Public Class SLR_SurfaceH : Inherits VB_Parent
    Dim surface As New PointCloud_SurfaceH
    Public Sub New()
        desc = "Use the PointCloud_SurfaceH data to indicate valleys and peaks."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        surface.Run(src)
        dst2 = surface.dst3
    End Sub
End Class









Public Class SLR_Trends : Inherits VB_Parent
    Public hist As New Hist_KalmanAuto
    Dim valList As New List(Of Single)
    Dim barMidPoint As Single
    Dim lastPoint As cvb.Point2f
    Public resultingPoints As New List(Of cvb.Point2f)
    Public resultingValues As New List(Of Single)
    Public Sub New()
        desc = "Find trends by filling in short histogram gaps in the given image's histogram."
    End Sub
    Public Sub connectLine(i As Integer, dst As cvb.Mat)
        Dim x = barMidPoint + dst.Width * i / valList.Count
        Dim y = dst.Height - dst.Height * valList(i) / hist.plot.maxRange
        Dim p1 = New cvb.Point2f(x, y)
        resultingPoints.Add(p1)
        resultingValues.Add(p1.Y)
        dst.Line(lastPoint, p1, cvb.Scalar.Yellow, task.lineWidth + 1, task.lineType)
        lastPoint = p1
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        labels(2) = "Grayscale histogram - yellow line shows trend"
        hist.plot.backColor = cvb.Scalar.Red
        hist.Run(src)
        dst2 = hist.dst2

        Dim indexer = hist.histogram.GetGenericIndexer(Of Single)()
        valList = New List(Of Single)
        For i = 0 To hist.histogram.Rows - 1
            valList.Add(indexer(i))
        Next
        barMidPoint = dst2.Width / valList.Count / 2

        If valList.Count < 2 Then Exit Sub
        hist.plot.maxRange = valList.Max
        lastPoint = New cvb.Point2f(barMidPoint, dst2.Height - dst2.Height * valList(0) / hist.plot.maxRange)
        resultingPoints.Clear()
        resultingValues.Clear()
        resultingPoints.Add(lastPoint)
        resultingValues.Add(lastPoint.Y)
        For i = 1 To valList.Count - 2
            If valList(i - 1) > valList(i) And valList(i + 1) > valList(i) Then
                valList(i) = (valList(i - 1) + valList(i + 1)) / 2
            End If
            connectLine(i, dst2)
        Next
        connectLine(valList.Count - 1, dst2)
    End Sub
End Class




'////+////////////////////////////////////////////////////////////
'                                                               
'          Copyright Vadim Stadnik 2020.                        
' Distributed under the Code Project Open License (CPOL) 1.02.  
' (See or copy at http://www.codeproject.com/info/cpol10.aspx)  
'                                                               
'/////////////////////////////////////////////////////////////////

'                                                                      
'  This file contains the demonstration VB.NET code for the article        
'  by V. Stadnik "Segmented Linear Regression";                        
'                                                                      
'  Note that the algorithms for segmented linear regression (SLR)      
'  were originally written in C++. The VB.NET variants of these algorithms 
'  preserve the structure of the original C++ implementation.          
'                                                                      
'  The accuracy of the approximation is basically the same.            
'  The differences are observed in the least significant digits of     
'  type <Double>.                                                      
'                                                                      
'  The VB.NET implementation of the algorithms has comparable performance  
'  with C++ implementation in terms of measured running time.          
'  The asymptotical performance is identical.                          
'                                                                      
'  The implementation assumes that the attached sample datasets are    
'  stored in the folder "C:\SampleData\" ;                           
'    
' https://www.codeproject.com/Articles/5282014/Segmented-Linear-Regression

'  implemenation of algorithms          
'  SLR == Segmented Linear Regression ;
Public Class SLR
    ' Type to store coefficients A and B of linear regression 
    Public Class LinearRegressionParams
        Public Property CoefA As Double
        Public Property CoefB As Double

        Public Sub New(ByVal a As Double, ByVal b As Double)
            CoefA = a
            CoefB = b
        End Sub
    End Class

    ' struct RangeIndex represents a semi-open range of indices [ a, b )
    Public Class RangeIndex
        Public idx_a As Integer
        Public idx_b As Integer

        Public Sub New(ByVal _a As Integer, ByVal _b As Integer)
            ' empty and reversed ranges are NOT allowed in this type
            If _b <= _a Then
                Debug.WriteLine("invalid range")
            End If

            ' a negative index will crash application
            If _a < 0 Then
                Debug.WriteLine("invalid index")
            End If

            idx_a = _a
            idx_b = _b
        End Sub

        Public Function Length() As Integer
            Return (idx_b - idx_a)
        End Function
    End Class

    ' RangeLengthMin() is the limit for minimum allowed length of a range
    ' a range is indivisible if its length is less than 2*RANGE_LENGTH_MIN
    Public Shared Function RangeLengthMin() As Integer
        Return 2
    End Function

    ' The function to measure the accuracy of an approximation 
    Public Function ApproximationErrorY(data_y_orig As List(Of Double), data_y_approx As List(Of Double)) As Double
        If data_y_orig.Count <> data_y_approx.Count Then
            Debug.WriteLine("SLR: data size error")
            Return Double.MaxValue
        End If

        ' The result is max value of abs differences between two matching y values 
        Dim diff_max As Double = 0.0
        Dim n_values As Integer = data_y_orig.Count

        If n_values < 1 Then
            Return diff_max
        End If

        For i As Integer = 0 To n_values - 1
            Dim y_orig_i As Double = data_y_orig(i)
            Dim y_aprox_i As Double = data_y_approx(i)
            Dim diff_i As Double = Math.Abs(y_orig_i - y_aprox_i)

            If diff_i > diff_max Then
                diff_max = diff_i
            End If
        Next

        Return diff_max
    End Function


    'Public Structure LinearRegressionParams
    '    Public coef_a As Double
    '    Public coef_b As Double
    'End Structure

    ''' <summary>
    ''' Computes parameters of linear regression using values of given sums.
    ''' Returns False for special cases or invalid input that should be processed in client code.
    ''' </summary>
    Public Function LinearRegressionParameters(
                n_values As Double,
                sum_x As Double,
                sum_y As Double,
                sum_xx As Double,
                sum_xy As Double,
                ByRef lin_regn_out As LinearRegressionParams) As Boolean

        ' Result for special cases or invalid input parameters
        lin_regn_out.CoefA = 0.0
        lin_regn_out.CoefB = 0.0

        Const TOLER As Double = 0.0000000001
        ' Invalid input n_values:
        ' 0 is UN-defined case;
        ' 1 causes division by zero (denom == 0.0);
        If n_values < 1.0 + TOLER Then
            Return False
        End If

        Dim denom As Double = n_values * sum_xx - sum_x * sum_x

        If Math.Abs(denom) < TOLER Then
            ' The following special cases should be processed in client code:
            ' 1. User data represent a single point;
            ' 2. Regression line is vertical: coef_a == INFINITY, coeff_b is UN-defined;
            Return False
        End If

        ' Coefficients for the approximation line: y = a + b*x;
        lin_regn_out.CoefA = (sum_y * sum_xx - sum_x * sum_xy) / denom
        lin_regn_out.CoefB = (n_values * sum_xy - sum_x * sum_y) / denom
        Return True
    End Function


    ' The function ComputeLinearRegression() computes parameters of 
    ' linear regression and approximation error                     
    ' for a given range of a given dataset                          
    Public Sub ComputeLinearRegression(
                    data_x As List(Of Double),
                    data_y As List(Of Double),
                    idx_range As RangeIndex,
                    lin_regr_out As LinearRegressionParams,
                    ByRef err_appr_out As Double)

        If idx_range.Length() < RangeLengthMin() Then
            Debug.WriteLine("SLR error: input range is too small")
            Return
        End If

        Dim idx_a As Integer = idx_range.idx_a
        Dim idx_b As Integer = idx_range.idx_b
        Dim n_vals As Double = idx_range.Length()
        Dim sum_x As Double = 0.0
        Dim sum_y As Double = 0.0
        Dim sum_xx As Double = 0.0
        Dim sum_xy As Double = 0.0

        ' compute the required sums: 
        For it As Integer = idx_a To idx_b - 1
            Dim xi As Double = data_x(it)
            Dim yi As Double = data_y(it)
            sum_x += xi
            sum_y += yi
            sum_xx += xi * xi
            sum_xy += xi * yi
        Next

        ' compute parameters of linear regression in the given range 
        If Not LinearRegressionParameters(n_vals, sum_x, sum_y, sum_xx, sum_xy, lin_regr_out) Then
            ' this is a very unusual case for real data  
            'Debug.WriteLine("SLR: special case error")
            Return
        End If

        Dim coef_a As Double = lin_regr_out.CoefA
        Dim coef_b As Double = lin_regr_out.CoefB

        ' use linear regression obtained to measure approximation error in the given range,          
        ' the error is the maximum of absolute differences between original and approximation values 
        Dim diff_max As Double = 0.0
        For it As Integer = idx_a To idx_b - 1
            Dim xi As Double = data_x(it)
            Dim yi_orig As Double = data_y(it)
            Dim yi_appr As Double = coef_a + coef_b * xi

            Dim diff_i As Double = Math.Abs(yi_orig - yi_appr)
            If diff_i > diff_max Then
                diff_max = diff_i
            End If
        Next

        err_appr_out = diff_max
    End Sub




    ' Implementation specific function-helper for better code re-use,
    ' it enables us to measure approximations errors in results
    Public Shared Sub InterpolateSegments(
                ByVal vec_ranges As List(Of RangeIndex),
                ByVal vec_LR_params As List(Of LinearRegressionParams),
                ByVal data_x As List(Of Double),
                ByVal data_x_interpol As List(Of Double),
                ByVal data_y_interpol As List(Of Double)
            )
        data_x_interpol.Clear()
        data_y_interpol.Clear()

        Dim n_ranges As Integer = vec_ranges.Count
        For i_rng As Integer = 0 To n_ranges - 1
            ' In the current range we only need to interpolate y-data
            ' using corresponding linear regression
            Dim range_i As RangeIndex = vec_ranges(i_rng)
            Dim lr_params_i As LinearRegressionParams = vec_LR_params(i_rng)

            Dim coef_a As Double = lr_params_i.CoefA
            Dim coef_b As Double = lr_params_i.CoefB
            Dim i_start As Integer = range_i.idx_a
            Dim i_end As Integer = range_i.idx_b
            For i As Integer = i_start To i_end - 1
                Dim x_i As Double = data_x(i)
                Dim y_i As Double = coef_a + coef_b * x_i

                data_x_interpol.Add(x_i)
                data_y_interpol.Add(y_i)
            Next
        Next
    End Sub





    ' Placeholder for ComputeLinearRegression function
    'Private Sub ComputeLinearRegression(data_x As List(Of Double), data_y As List(Of Double),
    '                                    range As RangeIndex, params As LinearRegressionParams,
    '                                    ByRef errorVal As Double)
    '    ' Implementation needed
    'End Sub

    ' The function CanSplitRangeThorough()                          
    ' makes decision whether a given range should be split or not ; 
    '                                                                
    ' a given range is not subdivided if the specified accuracy of  
    ' linear regression has been achieved, otherwise, the function  
    ' searches for the best split point in the range ;              
    '                                                                
    Public Function CanSplitRangeThorough(
                data_x As List(Of Double),
                data_y As List(Of Double),
                devn_max_user As Double,
                idx_range_in As RangeIndex,
                ByRef idx_split_out As Integer,
                lr_params_out As LinearRegressionParams
             ) As Boolean

        ' compute linear regression and approximation error for input range 
        Dim error_range_in As Double = Double.MaxValue
        ComputeLinearRegression(data_x, data_y, idx_range_in, lr_params_out, error_range_in)

        ' if the approximation is acceptable, input range is not subdivided 
        If error_range_in < devn_max_user Then
            Return False
        End If

        ' approximation error for a current split 
        Dim err_split As Double = Double.MaxValue
        ' the position (index) of a current split 
        Dim idx_split As Integer = -1
        Dim idx_a As Integer = idx_range_in.idx_a
        Dim idx_b As Integer = idx_range_in.idx_b
        Dim end_offset As Integer = RangeLengthMin()

        ' sequential search for the best split point in the input range 
        For idx As Integer = idx_a + end_offset To idx_b - end_offset - 1
            ' sub-divided ranges 
            Dim range_left As New RangeIndex(idx_a, idx)
            Dim range_right As New RangeIndex(idx, idx_b)

            ' parameters of linear regression in sub-divided ranges 
            Dim lin_regr_left As New LinearRegressionParams(0.0, 0.0)
            Dim lin_regr_right As New LinearRegressionParams(0.0, 0.0)

            ' corresponding approximation errors 
            Dim err_left As Double = Double.MaxValue
            Dim err_right As Double = Double.MaxValue

            ' compute linear regression and approximation error in each range 
            ComputeLinearRegression(data_x, data_y, range_left, lin_regr_left, err_left)
            ComputeLinearRegression(data_x, data_y, range_right, lin_regr_right, err_right)

            ' we use the worst approximation error 
            Dim err_idx As Double = Math.Max(err_left, err_right)
            ' the smaller error the better split   
            If err_idx < err_split Then
                err_split = err_idx
                idx_split = idx
            End If
        Next

        ' check that sub-division is valid,                             
        ' the case of short segment: 2 or 3 data points ;               
        ' if (n==3) required approximation accuracy cannot be reached ; 
        If idx_split < 0 Then
            Return False
        End If

        idx_split_out = idx_split
        Return True
    End Function




    ''' <summary>
    ''' This function implements the smoothing method,
    ''' which is known as simple moving average.
    ''' 
    ''' The implementation uses symmetric window.
    ''' The window length is variable in front and tail ranges.
    ''' The first and last values are fixed.
    ''' </summary>
    ''' <param name="data_io">List of double values to be smoothed</param>
    ''' <param name="half_len">Half length of the smoothing window</param>
    Public Sub SimpleMovingAverage(ByRef data_io As List(Of Double), ByVal half_len As Integer)
        Dim n_values As Integer = data_io.Count

        ' No processing is required
        If half_len <= 0 OrElse n_values < 3 Then
            Return
        End If

        ' Smoothing window is too large
        If (2 * half_len + 1) > n_values Then
            Return
        End If

        Dim ix As Integer = 0
        Dim sum_y As Double = 0.0
        Dim data_copy As New List(Of Double)(data_io)

        ' For better readability, where relevant the code below shows
        ' the symmetry of processing at a current data point,
        ' for example: we use (ix + 1 + ix) instead of (2 * ix + 1)

        ' The first point is fixed
        sum_y = data_copy(0)
        data_io(0) = sum_y / 1.0

        ' The front range:
        ' Processing accumulates sum_y using gradually increasing length window
        For ix = 1 To half_len
            sum_y = sum_y + data_copy(2 * ix - 1) + data_copy(2 * ix)
            data_io(ix) = sum_y / CDbl(ix + 1 + ix)
        Next

        ' In the middle range window length is constant
        For ix = (half_len + 1) To ((n_values - 1) - half_len)
            ' Add to window new data point and remove from window the oldest data point
            sum_y = sum_y + data_copy(ix + half_len) - data_copy(ix - half_len - 1)
            data_io(ix) = sum_y / CDbl(half_len + 1 + half_len)
        Next

        ' The tail range:
        ' Processing uses gradually decreasing length window
        For ix = (n_values - half_len) To (n_values - 2)
            sum_y = sum_y - data_copy(n_values - 1 - 2 * half_len + 2 * (ix - (n_values - 1 - half_len)) - 2) _
                                  - data_copy(n_values - 1 - 2 * half_len + 2 * (ix - (n_values - 1 - half_len)) - 1)

            data_io(ix) = sum_y / CDbl(n_values - 1 - ix + 1 + n_values - 1 - ix)
        Next

        ' The last point is fixed
        data_io(n_values - 1) = data_copy(n_values - 1)
    End Sub




    ''' <summary>
    ''' This function detects positions (indices) of local maxima
    ''' in a given dataset of values of type Double.
    ''' 
    ''' Limitations:
    ''' The implementation is potentially sensitive to numerical error,
    ''' thus, it is not the best choice for processing perfect (no noise) data.
    ''' It does not support finding maximum value in a plateau.
    ''' </summary>
    ''' <param name="vecDataIn">Input list of double values</param>
    ''' <param name="vecMaxIndicesRes">Resulting list of indices of local maxima</param>
    Public Sub FindLocalMaxima(vecDataIn As List(Of Double), vecMaxIndicesRes As List(Of Integer))
        vecMaxIndicesRes.Clear()

        Dim nValues As Integer = vecDataIn.Count

        If nValues < 3 Then
            Return
        End If

        ' The last and first values are excluded from processing
        For ix As Integer = 1 To nValues - 2
            Dim yPrev As Double = vecDataIn(ix - 1)
            Dim yCurr As Double = vecDataIn(ix)
            Dim yNext As Double = vecDataIn(ix + 1)

            Dim lessPrev As Boolean = (yPrev < yCurr)
            Dim lessNext As Boolean = (yNext < yCurr)

            If lessPrev AndAlso lessNext Then
                vecMaxIndicesRes.Add(ix)
                ix += 1
            End If
        Next
    End Sub


    ' the function CanSplitRangeFast()                              
    ' makes decision whether a given range should be split or not ; 
    '                                                                
    ' a given range is not subdivided if the specified accuracy of  
    ' linear regression has been achieved, otherwise,               
    ' the function selects for the best split the position of       
    ' the greatest local maximum of absolute differences            
    ' between original and smoothed values in a given range ;       
    '                                                                
    Public Function CanSplitRangeFast(
                data_x As List(Of Double),
                data_y As List(Of Double),
                vec_devns_in As List(Of Double),
                vec_max_ind_in As List(Of Integer),
                devn_max_user As Double,
                idx_range_in As RangeIndex,
                ByRef idx_split_out As Integer,
                lr_params_out As LinearRegressionParams
            ) As Boolean

        idx_split_out = -1

        If vec_devns_in.Count <> data_x.Count Then
            Debug.WriteLine("SLR: size error")
            Return False
        End If

        Dim end_offset As Integer = RangeLengthMin()
        Dim range_len As Integer = idx_range_in.Length()
        If range_len < end_offset Then
            Debug.WriteLine("SLR: input range is too small")
            Return False
        End If

        ' compute linear regression and approximation error for input range 
        Dim err_range_in As Double = Double.MaxValue
        ComputeLinearRegression(data_x, data_y, idx_range_in, lr_params_out, err_range_in)

        ' if the approximation is acceptable, input range is not subdivided 
        If err_range_in < devn_max_user Then
            Return False
        End If

        ' check for indivisible range 
        If range_len < 2 * RangeLengthMin() Then
            Return False
        End If

        If vec_devns_in.Count = 0 Then
            Return False
        End If

        ' for the main criterion of splitting here we use                 
        ' the greatest local maximum of deviations inside the given range 
        Dim idx_split_local_max As Integer = -1
        Dim devn_max As Double = 0.0
        Dim devn_cur As Double = 0.0
        Dim sloc_max As Integer = vec_max_ind_in.Count

        ' find inside given range local maximum with the largest deviation 
        For k_max As Integer = 0 To sloc_max - 1
            Dim idx_max_cur As Integer = vec_max_ind_in(k_max)

            ' check if the current index is inside the given range and that  
            ' potential split will not create segment with 1 data point only 
            If (idx_max_cur < idx_range_in.idx_a + end_offset) OrElse
                       (idx_max_cur >= idx_range_in.idx_b - end_offset) Then
                Continue For
            End If

            devn_cur = vec_devns_in(idx_max_cur)
            If devn_cur > devn_max Then
                devn_max = devn_cur
                idx_split_local_max = idx_max_cur
            End If
        Next

        ' the case of no one local maximum inside the given range 
        If idx_split_local_max < 0 Then
            Return False
        End If

        ' the case (idx_split_local_max==0) is not possible here due to (end_offset==RANGE_LENGTH_MIN), 
        ' this is a valid result ( idx_split_local_max > 0 )                                            
        idx_split_out = idx_split_local_max

        Return True
    End Function
    Public Function SegmentedRegressionFast(
    data_x As List(Of Double),
    data_y As List(Of Double),
    devn_max As Double,
    sm_half_len As Integer,
    ByRef data_x_res As List(Of Double),
    ByRef data_y_res As List(Of Double)
) As Boolean

        data_x_res.Clear()
        data_y_res.Clear()

        Dim size_x As Integer = data_x.Count
        Dim size_y As Integer = data_y.Count

        If size_x <> size_y Then
            Return False
        End If

        If size_x < 2 * RangeLengthMin() Then
            Return False
        End If

        Dim data_y_smooth As New List(Of Double)(data_y)
        SimpleMovingAverage(data_y_smooth, sm_half_len)

        Dim vec_deviations As New List(Of Double)
        For i As Integer = 0 To size_y - 1
            vec_deviations.Add(Math.Abs(data_y_smooth(i) - data_y(i)))
        Next

        Dim vec_max_indices As New List(Of Integer)
        FindLocalMaxima(vec_deviations, vec_max_indices)

        Dim vec_ranges As New List(Of RangeIndex)
        Dim vec_LR_params As New List(Of LinearRegressionParams)

        Dim range_top As New RangeIndex(0, size_x)
        Dim idx_split As Integer = -1
        Dim lr_params As New LinearRegressionParams(0.0, 0.0)

        Dim stack_ranges As New Stack(Of RangeIndex)
        stack_ranges.Push(range_top)

        While stack_ranges.Count > 0
            range_top = stack_ranges.Pop()

            If CanSplitRangeFast(data_x, data_y, vec_deviations, vec_max_indices,
                                 devn_max, range_top, idx_split, lr_params) Then
                stack_ranges.Push(New RangeIndex(idx_split, range_top.idx_b))
                stack_ranges.Push(New RangeIndex(range_top.idx_a, idx_split))
            Else
                vec_ranges.Add(New RangeIndex(range_top.idx_a, range_top.idx_b))
                vec_LR_params.Add(New LinearRegressionParams(lr_params.CoefA, lr_params.CoefB))
            End If
        End While

        Dim data_x_interpol As New List(Of Double)
        Dim data_y_interpol As New List(Of Double)

        InterpolateSegments(vec_ranges, vec_LR_params, data_x,
                            data_x_interpol, data_y_interpol)

        Dim appr_error As Double = ApproximationErrorY(data_y, data_y_interpol)
        'If appr_error > devn_max Then
        '    Return False
        'End If

        data_x_res.AddRange(data_x_interpol)
        data_y_res.AddRange(data_y_interpol)

        Return True
    End Function
End Class
