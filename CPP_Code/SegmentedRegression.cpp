/////////////////////////////////////////////////////////////////
// 
//          Copyright Vadim Stadnik 2020.
// Distributed under the Code Project Open License (CPOL) 1.02. 
// (See or copy at http://www.codeproject.com/info/cpol10.aspx) 
//
/////////////////////////////////////////////////////////////////

//
//  This file contains the demonstration code for the article 
//  by V. Stadnik "Segmented Linear Regression"; 
//

#include <float.h> 
#include <math.h> 
#include <vector> 
#include <stack> 


//  type of containers to store datasets 
typedef std::vector<double>         VectorDbls ; 
typedef std::vector<int>            VectorInts ;

//  type to store coefficients A and B of linear regression 
typedef std::pair<double, double>   LinearRegrnParams ;

//  RANGE_LENGTH_MIN is the limit for minimum allowed length of a range ; 
//  a range is indivisible if its length is less than 2*RANGE_LENGTH_MIN ; 
const int   RANGE_LENGTH_MIN = 2 ;


//  struct RangeIndex represents a semi-open range of indices [ a, b ) 
struct RangeIndex
{
    explicit 
    RangeIndex ( int _a=0, int _b=0 ) : idx_a(_a), idx_b(_b) 
    {  
        //  empty and reversed ranges are NOT allowed in this type
        if ( _b <= _a )     
            throw std::range_error("invalid range") ;

        //  a negative index will crash application
        if ( _a < 0 )       
            throw std::out_of_range("invalid index") ; 
    } 

    int  Length ( ) const { return ( idx_b - idx_a ) ; } 

    int     idx_a ; 
    int     idx_b ; 
} ;


//  the function to measure the accuracy of an approximation 
double  ApproximationErrorY 
    ( 
        VectorDbls const &  data_y_orig  , 
        VectorDbls const &  data_y_approx 
    ) 
{
    if ( data_y_orig.size() != data_y_approx.size() ) 
        throw std::runtime_error("linear regression: size error") ; 

    //  the result is max value of abs differences between two matching y values 
    double      diff_max = 0.0 ;
    const int   n_values = static_cast<int>( data_y_orig.size() ) ; 

    if ( n_values < 1 ) 
        return diff_max ; 

    for ( int   i = 0 ; i < n_values ; ++i )
    {
        double  y_orig_i  = data_y_orig  [i] ; 
        double  y_aprox_i = data_y_approx[i] ; 
        double  diff_i    = fabs ( y_orig_i - y_aprox_i ) ; 

        if ( diff_i > diff_max ) 
            diff_max = diff_i ; 
    }

    return diff_max ; 
} 


//  the function LinearRegressionParameters() computes parameters of 
//  linear regression using values of given sums ; 
// 
//  this function returns <false> for special cases or invalid input 
//  that should be processed in client code ; 
bool LinearRegressionParameters 
    ( 
        const double    n_values , 
        const double    sum_x    ,
        const double    sum_y    ,
        const double    sum_xx   ,
        const double    sum_xy   ,
        //  the results are 
        //  coefficients a and b of linear function: y = a + b*x ; 
        //
        //  they are solution of the two equations:  a * N     + b * sum_x  = sum_y  ;
        //                                           a * sum_x + b * sum_xx = sum_xy ; 
        //  
        double &        coef_a_out , 
        double &        coef_b_out 
    )
{
    //  result for special cases or invalid input parameters 
    coef_a_out = 0.0 ; 
    coef_b_out = 0.0 ; 

    //  invalid input n_values: 
    //      0 is UN-defined case; 
    //      1 causes division by zero (denom ==0.0) ;
    if ( n_values < 2.0 )
        return false ; 

    const double    toler = 1.0e-10 ;
    const double    denom = n_values * sum_xx - sum_x * sum_x ;  

    if ( fabs ( denom ) < toler ) 
    {
        //  the following special cases should be processed in client code: 
        //    1. user data represent a single point ; 
        //    2. regression line is vertical: coef_a==INFINITY , coeff_b is UN-defined ; 
        return false ; 
    }

    //  coefficients for the approximation line: y = a + b*x ; 
    coef_a_out = (    sum_y * sum_xx - sum_x * sum_xy ) / denom ; 
    coef_b_out = ( n_values * sum_xy - sum_x * sum_y  ) / denom ; 
    return true ; 
} 


//  the function ComputeLinearRegression() computes parameters of 
//  linear regression and approximation error 
//  for a given range of a given dataset ;
void ComputeLinearRegression 
    (
        //  original dataset 
        VectorDbls const &      data_x       ,
        VectorDbls const &      data_y       ,
        //  semi-open range [ a , b ) 
        RangeIndex const &      idx_range    ,  
        //  coefficients of linear regression in the given range 
        LinearRegrnParams &     lin_regr_out , 
        //  approximation error 
        double &                err_appr_out
    )
{
    if ( idx_range.Length() < RANGE_LENGTH_MIN ) 
        throw std::range_error("input range is too small") ;

    int         idx_a  = idx_range . idx_a ; 
    int         idx_b  = idx_range . idx_b ; 
    double      n_vals = idx_range . Length() ; 
    double      sum_x  = 0.0 ;
    double      sum_y  = 0.0 ;
    double      sum_xx = 0.0 ;
    double      sum_xy = 0.0 ;

    //  compute the required sums: 
    for ( int   it = idx_a ; it < idx_b ; ++it ) 
    {
        double  xi = data_x [ it ] ; 
        double  yi = data_y [ it ] ; 
        sum_x  += xi ; 
        sum_y  += yi ; 
        sum_xx += xi * xi ; 
        sum_xy += xi * yi ; 
    }

    //  compute parameters of linear regression in the given range 
    double      coef_a = 0.0 ;  
    double      coef_b = 0.0 ;

    if ( ! LinearRegressionParameters ( n_vals, sum_x, sum_y, sum_xx, sum_xy, coef_a, coef_b ) ) 
    {
        //  this is a very unusual case for real data  
        throw std::runtime_error("linear regression: special case error") ; 
    } 

    //  first result 
    lin_regr_out.first  = coef_a ; 
    lin_regr_out.second = coef_b ; 

    //  use linear regression obtained to measure approximation error in the given range,
    //  the error is the maximum of absolute differences between original and approximation values 
    double      diff_max = 0.0 ;
    for ( int   it = idx_a ; it < idx_b ; ++it ) 
    {
        double  xi      = data_x [ it ] ; 
        double  yi_orig = data_y [ it ] ; 
        double  yi_appr = coef_a + coef_b*xi ; 

        double  diff_i  = fabs ( yi_orig - yi_appr ) ; 
        if ( diff_i > diff_max ) 
        {
            diff_max = diff_i ; 
        }
    }

    //  second result 
    err_appr_out = diff_max ; 
} 


//  implementation specific function-helper for better code re-use, 
//  it enables us to measure approximations errors in results 
void InterpolateSegments 
    (
        std::vector<RangeIndex> const &         vec_ranges      , 
        std::vector<LinearRegrnParams> const &  vec_LR_params   , 
        VectorDbls const &                      data_x          , 
        VectorDbls &                            data_x_interpol , 
        VectorDbls &                            data_y_interpol 
    )
{
    if ( data_x.size() != data_x_interpol.size() ||
         data_x.size() != data_y_interpol.size()    )
        throw std::runtime_error("linear regression: size error") ; 

    const int   n_ranges = static_cast<int>( vec_ranges.size() ) ; 
    for ( int   i_rng    = 0 ; i_rng < n_ranges ; ++i_rng ) 
    {
        //  in the current range we only need to interpolate y-data
        //  using corresponding linear regression 
        RangeIndex const &          range_i     = vec_ranges . at(i_rng) ;
        LinearRegrnParams const &   lr_params_i = vec_LR_params.at(i_rng) ;

        double      coef_a  = lr_params_i . first  ; 
        double      coef_b  = lr_params_i . second ;
        int         i_start = range_i . idx_a ; 
        int         i_end   = range_i . idx_b ; 
        for ( int   i = i_start ; i < i_end ; ++i ) 
        {
            double  x_i = data_x [ i ] ;   
            double  y_i = coef_a + coef_b*x_i ; 

            data_x_interpol[i] = x_i ; 
            data_y_interpol[i] = y_i ; 
        } 
    } 
} 


//
/////////////////////////////////////////////////////////////////////////////
//


//  the function CanSplitRangeThorough() 
//  makes decision whether a given range should be split or not ; 
//
//  a given range is not subdivided if the specified accuracy of 
//  linear regression has been achieved, otherwise, the function 
//  searches for the best split point in the range ; 
//
bool CanSplitRangeThorough 
    ( 
        //  original dataset 
        VectorDbls const &      data_x       ,
        VectorDbls const &      data_y       ,
        //  the limit for maximum allowed approximation error (tolerance)  
        const double            devn_max_user,     
        //  input range to be split if linear regression is not acceptable
        RangeIndex const &      idx_range_in , 
        //  the position of a split point, when the function returns <true> 
        int &                   idx_split_out,
        //  the parameters of linear regression for the given range, 
        //  when the function returns <false> 
        LinearRegrnParams &     lr_params_out
    ) 
{
    //  compute linear regression and approximation error for input range 
    double      error_range_in = DBL_MAX ;
    ComputeLinearRegression( data_x, data_y, idx_range_in, lr_params_out, error_range_in ) ; 

    //  if the approximation is acceptable, input range is not subdivided 
    if ( error_range_in < devn_max_user ) 
        return false ; 

    //  approximation error for a current split
    double      err_split = DBL_MAX ; 
    //  the position (index) of a current split
    int         idx_split = -1 ;
    int         idx_a     = idx_range_in.idx_a ;
    int         idx_b     = idx_range_in.idx_b ;
    const int   end_offset= RANGE_LENGTH_MIN ;

    //  sequential search for the best split point in the input range
    for ( int   idx = idx_a+end_offset; idx < idx_b-end_offset ; ++idx ) 
    {
        //  sub-divided ranges
        RangeIndex      range_left  ( idx_a , idx   ) ; 
        RangeIndex      range_right ( idx   , idx_b ) ; 

        //  parameters of linear regression in sub-divided ranges
        LinearRegrnParams   lin_regr_left  ; 
        LinearRegrnParams   lin_regr_right ; 
        //  corresponding approximation errors 
        double              err_left  = DBL_MAX ;
        double              err_right = DBL_MAX ;

        //  compute linear regression and approximation error in each range 
        ComputeLinearRegression( data_x, data_y, range_left , lin_regr_left , err_left  ) ;
        ComputeLinearRegression( data_x, data_y, range_right, lin_regr_right, err_right ) ;

        //  we use the worst approximation error 
        double          err_idx = (std::max) ( err_left, err_right ) ; 
        //  the smaller error the better split
        if ( err_idx < err_split ) 
        {
            err_split = err_idx ; 
            idx_split = idx ; 
        } 
    } 

    //  check that sub-division is valid, 
    //  the case of short segment: 2 or 3 data points ; 
    //  if (n==3) required approximation accuracy cannot be reached ; 
    if ( idx_split < 0 ) 
        return false ;      

    idx_split_out = idx_split ; 
    return true ; 
}


//  the function SegmentedRegressionThorough() implements 
//  algorithm for segmented linear (piecewise) regression, which is 
//  based on exhaustive sequential search for the best split point ;
//  
//  the performance of this algorithm is quadratic on average 
//  and cubic in the worst case ; 
//
//  return value <false> shows that the required approximation accuracy 
//  has not been achieved ;
//
bool SegmentedRegressionThorough 
    (   
        //  input dataset: 
        //  this function assumes that input x-data are equally spaced
        VectorDbls const &      data_x     ,
        VectorDbls const &      data_y     ,
        //  user specified approximation accuracy (tolerance) ; 
        //  this parameter allows to control the total number 
        //  and lengths of segments detected ; 
        const double            devn_max   ,
        //  the resulting segmented linear regression 
        //  is interpolated to match and compare against input values 
        VectorDbls &            data_x_res ,
        VectorDbls &            data_y_res 
    )
{
    data_x_res . clear ( ) ; 
    data_y_res . clear ( ) ;

    const int   size_x = static_cast<int>( data_x.size() ) ; 
    const int   size_y = static_cast<int>( data_y.size() ) ; 

    if ( size_x != size_y ) 
        return false ; 

    //  check for indivisible range 
    if ( size_x < 2*RANGE_LENGTH_MIN )
        return false ; 
    
    //  ranges (segments) of linear regression 
    std::vector<RangeIndex>         vec_ranges ; 
    //  parameters of linear regression in each matching range 
    std::vector<LinearRegrnParams>  vec_LR_params ; 

    //  the stage of recursive top-down subvision: 
    //  this processing starts from the entire range of given dataset 
    RangeIndex          range_top ( 0 , size_x ) ; 
    //  the position (index) of a current split point 
    int                 idx_split = -1 ;
    //  parameters of linear regression in a current range (segment) 
    LinearRegrnParams   lr_params ; 

    std::stack<RangeIndex>  stack_ranges ; 
    stack_ranges . push ( range_top ) ; 

    while ( ! stack_ranges . empty ( ) ) 
    {
        range_top = stack_ranges . top ( ) ; 
        stack_ranges . pop ( ) ; 

        if ( CanSplitRangeThorough( data_x, data_y, devn_max, range_top, idx_split, lr_params) ) 
        {
            //  reverse order of pushing onto stack eliminates re-ordering vec_ranges 
            //  after this function is completed
            stack_ranges . push ( RangeIndex ( idx_split       , range_top.idx_b ) ) ; 
            stack_ranges . push ( RangeIndex ( range_top.idx_a , idx_split       ) ) ; 
        }
        else
        {
            //  the range is indivisible, we add it to the result 
            vec_ranges    . push_back ( range_top ) ; 
            vec_LR_params . push_back ( lr_params ) ; 
        }
    } 

    //  interpolate the resulting segmented linear regression 
    //  and verify the accuracy of the approximation 
    VectorDbls  data_x_interpol ( size_x, 0.0 ) ; 
    VectorDbls  data_y_interpol ( size_x, 0.0 ) ; 

    InterpolateSegments ( vec_ranges, vec_LR_params, data_x, 
                          data_x_interpol , data_y_interpol ) ; 

    double      appr_error = ApproximationErrorY ( data_y, data_y_interpol ) ;  
    if ( appr_error > devn_max ) 
        return false ; 

    //  the result of this function when the required accuracy has been achieved 
    data_x_res = data_x_interpol ; 
    data_y_res = data_y_interpol ; 

    return true ; 
}


//
/////////////////////////////////////////////////////////////////////////////////
//


//  this function implements the smoothing method, 
//  which is known as simple moving average ; 
//
//  the implementation uses symmetric window ;
//  the window length is variable in front and tail ranges ;
//  the first and last values are fixed ;
void SimpleMovingAverage
    ( 
        VectorDbls &    data_io  , 
        const int       half_len 
    )
{
    const int   n_values = static_cast<int>( data_io.size() ) ;

    //  no processing is required 
    if ( half_len<=0 || n_values<3 ) 
        return ; 

    //  smoothing window is too large
    if ( ( 2*half_len + 1 ) > n_values ) 
        return ; 

    int         ix    = 0   ; 
    double      sum_y = 0.0 ; 
    VectorDbls  data_copy ( data_io ) ;

    //  for better readability, where relevant the code below shows 
    //  the symmetry of processing at a current data point, 
    //  for example: we use ( ix + 1 + ix ) instead of ( 2*ix + 1 ) ;

    //  the first point is fixed  
    sum_y      = data_copy [ 0 ] ;
    data_io[0] = sum_y / 1.0  ;

    //  the front range:
    //  processing accumulates sum_y using gradually increasing length window
    for ( ix = 1 ; ix <= half_len ; ++ix ) 
    {
        sum_y       = sum_y + data_copy [ 2*ix - 1 ] + data_copy [ 2*ix ] ;
        data_io[ix] = sum_y / double ( ix + 1 + ix ) ;
    }      

    //  in the middle range window length is constant 
    for ( ix = ( half_len + 1 ) ; ix <= ( ( n_values - 1 ) - half_len ) ; ++ix ) 
    {
        //  add to window new data point and remove from window the oldest data point
        sum_y       = sum_y + data_copy [ ix + half_len ] - data_copy [ ix - half_len - 1 ] ; 
        data_io[ix] = sum_y / double ( half_len + 1 + half_len ) ;
    } 

    //  the tail range:
    //  processing uses gradually decreasing length window 
    for ( ix = ( n_values - half_len ) ; ix < ( n_values - 1 ) ; ++ix ) 
    {
        sum_y = sum_y - data_copy [ n_values - 1 - 2*half_len + 2*( ix - (n_values - 1 - half_len) ) - 2 ]
                      - data_copy [ n_values - 1 - 2*half_len + 2*( ix - (n_values - 1 - half_len) ) - 1 ] ; 

        data_io[ix] = sum_y / double ( n_values - 1 - ix + 1 + n_values - 1 - ix ) ;
    } 

    //  the last point is fixed 
    data_io[n_values - 1] = data_copy[ n_values - 1 ] ;
} 


//  
//  this function detects positions (indices) of local maxima 
//  in a given dataset of values of type <double> ; 
//
//  limitations: 
//  the implementation is potentially sensitive to numerical error, 
//  thus, it is not the best choice for processing perfect (no noise) data ;
//  it does not support finding maximum value in a plato ; 
//
void FindLocalMaxima 
    ( 
        VectorDbls const &  vec_data_in         , 
        VectorInts &        vec_max_indices_res 
    ) 
{
    vec_max_indices_res . clear ( ) ; 

    const int   n_values = static_cast<int>( vec_data_in.size() ) ;

    if ( n_values < 3 )
        return ; 

    //  the last and first values are excluded from processing 
    for ( int   ix = 1 ; ix <= n_values - 2 ; ++ix )
    {
        double  y_prev = vec_data_in [ ix - 1 ] ; 
        double  y_curr = vec_data_in [ ix ]     ; 
        double  y_next = vec_data_in [ ix + 1 ] ; 

        bool    less_prev = ( y_prev < y_curr ) ; 
        bool    less_next = ( y_next < y_curr ) ; 

        if ( less_prev && less_next ) 
        {
            vec_max_indices_res . push_back ( ix ) ; 
            ++ix ; 
        }
    }
}


//  the function CanSplitRangeFast() 
//  makes decision whether a given range should be split or not ; 
//
//  a given range is not subdivided if the specified accuracy of 
//  linear regression has been achieved, otherwise, 
//  the function selects for the best split the position of 
//  the greatest local maximum of absolute differences 
//  between original and smoothed values in a given range ;
//  
bool CanSplitRangeFast
    (
        //  original dataset 
        VectorDbls const &      data_x        ,
        VectorDbls const &      data_y        , 
        //  absolute differences between original and smoothed values 
        VectorDbls const &      vec_devns_in  ,
        //  positions (indices) of local maxima in vec_devns_in 
        VectorInts const &      vec_max_ind_in, 
        //  the limit for maximum allowed approximation error (tolerance)  
        const double            devn_max_user , 
        //  input range to be split if linear regression is not acceptable
        RangeIndex const &      idx_range_in  , 
        //  the position of a split point, when the function returns <true> 
        int &                   idx_split_out ,
        //  the parameters of linear regression for the given range, 
        //  when the function returns <false> 
        LinearRegrnParams &     lr_params_out
    )
{
    idx_split_out = -1 ; 

    if ( vec_devns_in.size() != data_x.size() ) 
        throw std::runtime_error("linear regression: size error") ; 

    const int   end_offset = RANGE_LENGTH_MIN ; 
    const int   range_len  = idx_range_in.Length() ; 

    if ( range_len < end_offset )
        throw std::range_error("input range is too small") ;

    //  compute linear regression and approximation error for input range 
    double      err_range_in = DBL_MAX ;
    ComputeLinearRegression( data_x, data_y, idx_range_in, lr_params_out, err_range_in ) ; 

    //  if the approximation is acceptable, input range is not subdivided 
    if ( err_range_in < devn_max_user ) 
        return false ; 

    //  check for indivisible range 
    if ( range_len < 2*RANGE_LENGTH_MIN )
        return false ;      

    if ( vec_devns_in.empty() )
        return false ; 


    //  for the main criterion of splitting here we use 
    //  the greatest local maximum of deviations inside the given range 
    int         idx_split_local_max = -1 ; 
    double      devn_max = 0.0 ; 
    double      devn_cur = 0.0 ; 
    const int   sz_loc_max = static_cast<int>( vec_max_ind_in.size() ) ;

    //  find inside given range local maximum with the largest deviation 
    for ( int   k_max = 0 ; k_max < sz_loc_max  ; ++k_max  ) 
    {
        int     idx_max_cur = vec_max_ind_in [ k_max ] ; 

        //  check if the current index is inside the given range and that 
        //  potential split will not create segment with 1 data point only 
        if ( ( idx_max_cur <  idx_range_in.idx_a + end_offset) ||
             ( idx_max_cur >= idx_range_in.idx_b - end_offset)    ) 
            continue ; 

        devn_cur = vec_devns_in [ idx_max_cur ] ; 
        if ( devn_cur > devn_max ) 
        {
            devn_max = devn_cur ; 
            idx_split_local_max = idx_max_cur ; 
        }
    }

    //  the case of no one local maximum inside the given range 
    if ( idx_split_local_max < 0 ) 
        return false ;    

    //  the case (idx_split_local_max==0) is not possible here due to (end_offset==RANGE_LENGTH_MIN), 
    //  this is a valid result ( idx_split_local_max > 0 ) 
    idx_split_out = idx_split_local_max ;   

    return true ; 
}


//  the function SegmentedRegressionFast() implements 
//  algorithm for segmented linear (piecewise) regression, 
//  which uses for range splitting local maxima of 
//  absolute differences between original and smoothed values ; 
//  the method of smoothing is simple moving average; 
//  
//  the average performance of this algorithm is O(N logM), where 
//      N is the number of given values and 
//      M is the number of resulting line segments ;
//  in the worst case the performace is quadratic ; 
//
//  return value <false> shows that the required approximation accuracy 
//  has not been achieved ;
//
bool SegmentedRegressionFast 
    (   
        //  input dataset: 
        //  this function assumes that input x-data are equally spaced
        VectorDbls const &      data_x     ,
        VectorDbls const &      data_y     ,
        //  user specified approximation accuracy (tolerance) ; 
        //  this parameter allows to control the total number 
        //  and lengths of segments detected ; 
        const double            devn_max   ,
        //  this parameter represents half length of window ( h_len+1+h_len ),  
        //  which is used by simple moving average to create smoothed dataset 
        const int               sm_half_len,
        //  the resulting segmented linear regression 
        //  is interpolated to match and compare against input values 
        VectorDbls &            data_x_res ,
        VectorDbls &            data_y_res 
    )
{
    data_x_res . clear ( ) ; 
    data_y_res . clear ( ) ;

    const int   size_x = static_cast<int>( data_x.size() ) ; 
    const int   size_y = static_cast<int>( data_y.size() ) ; 

    if ( size_x != size_y ) 
        return false ; 

    //  check for indivisible range 
    if ( size_x < 2*RANGE_LENGTH_MIN )
        return false ; 

    //  vector of smoothed values 
    VectorDbls      data_y_smooth ( data_y ) ;
    SimpleMovingAverage( data_y_smooth, sm_half_len) ; 

    //  vector of deviations (as absolute differences) between original and smoothed values 
    VectorDbls  vec_deviations ( size_y , 0.0 ) ;  
    for ( int   i = 0 ; i < size_y ; ++i ) 
    {
        vec_deviations[i] = fabs ( data_y_smooth[i] - data_y[i] ) ;
    }

    //  find positions of local maxima in the vector of deviations 
	VectorInts        vec_max_indices ; 
    FindLocalMaxima ( vec_deviations, vec_max_indices ) ; 

    //  ranges (segments) of linear regression 
    std::vector<RangeIndex>         vec_ranges ; 
    //  parameters of linear regression in each matching range 
    std::vector<LinearRegrnParams>  vec_LR_params ; 

    //  the stage of recursive top-down subvision: 
    //  this processing starts from the entire range of given dataset 
    RangeIndex          range_top ( 0 , size_x ) ; 
    //  the position (index) of a current split point 
    int                 idx_split = -1 ;
    //  parameters of linear regression in a current range (segment) 
    LinearRegrnParams   lr_params ; 

    std::stack<RangeIndex>  stack_ranges ; 
    stack_ranges . push ( range_top ) ; 

    while ( ! stack_ranges . empty ( ) ) 
    {
        range_top = stack_ranges . top ( ) ; 
        stack_ranges . pop ( ) ; 

        if ( CanSplitRangeFast( data_x, data_y, vec_deviations, vec_max_indices, 
                                devn_max, range_top, idx_split, lr_params ) )
        {
            //  reverse order of pushing onto stack eliminates re-ordering vec_ranges 
            //  after this function is completed
            stack_ranges . push ( RangeIndex ( idx_split       , range_top.idx_b ) ) ; 
            stack_ranges . push ( RangeIndex ( range_top.idx_a , idx_split       ) ) ; 
        }
        else
        {
            //  the range is indivisible, we add it to the result 
            vec_ranges    . push_back ( range_top ) ; 
            vec_LR_params . push_back ( lr_params ) ; 
        }
    }

    //  interpolate the resulting segmented linear regression 
    //  and verify the accuracy of the approximation 
    VectorDbls  data_x_interpol ( size_x, 0.0 ) ; 
    VectorDbls  data_y_interpol ( size_x, 0.0 ) ; 

    InterpolateSegments ( vec_ranges, vec_LR_params, data_x, 
                          data_x_interpol , data_y_interpol ) ; 

    double      appr_error = ApproximationErrorY ( data_y, data_y_interpol ) ;  
    if ( appr_error > devn_max ) 
        return false ; 

    //  the result of this function when the required accuracy has been achieved 
    data_x_res = data_x_interpol ; 
    data_y_res = data_y_interpol ; 

    return true ; 
}


//
/////////////////////////////////////////////////////////////////////////////////
//


//  the function to test compilation of 
//  slow algorithm for segmented linear regression 
bool Test_SLR_Thorough ( ) 
{
    //  input dataset 
    VectorDbls      data_x_user ; 
    VectorDbls      data_y_user ; 
    //  user specified approximation accuracy 
    const double    devn_max = 0.5 ; 
    //  result of algorithm
    VectorDbls      data_x_slr  ; 
    VectorDbls      data_y_slr  ; 

    if ( ! SegmentedRegressionThorough 
            ( data_x_user , 
              data_y_user , 
              devn_max    ,
              data_x_slr  ,
              data_y_slr    ) 
       )
        return false ; 

    return true ; 
}


//  the function to test compilation of 
//  fast algorithm for segmented linear regression 
bool Test_SLR_Fast ( ) 
{
    //  input dataset 
    VectorDbls      data_x_user ; 
    VectorDbls      data_y_user ; 
    //  user specified approximation accuracy 
    const double    devn_max = 0.5 ; 
    //  half length of smoothing window ( h_len+1+h_len ) 
    //  for simple moving average 
    const int       half_len = 10 ;
    //  result of algorithm
    VectorDbls      data_x_slr  ; 
    VectorDbls      data_y_slr  ; 

    if ( ! SegmentedRegressionFast 
            ( data_x_user , 
              data_y_user , 
              devn_max    ,
              half_len    ,
              data_x_slr  ,
              data_y_slr    ) 
       )
        return false ; 

    return true ;
}




