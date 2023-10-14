/////+////////////////////////////////////////////////////////////
//                                                               
//          Copyright Vadim Stadnik 2020.                        
// Distributed under the Code Project Open License (CPOL) 1.02.  
// (See or copy at http://www.codeproject.com/info/cpol10.aspx)  
//                                                               
/////////////////////////////////////////////////////////////////

//                                                                      
//  This file contains the demonstration C# code for the article        
//  by V. Stadnik "Segmented Linear Regression";                        
//                                                                      
//  Note that the algorithms for segmented linear regression (SLR)      
//  were originally written in C++. The C# variants of these algorithms 
//  preserve the structure of the original C++ implementation.          
//                                                                      
//  The accuracy of the approximation is basically the same.            
//  The differences are observed in the least significant digits of     
//  type <double>.                                                      
//                                                                      
//  The C# implementation of the algorithms has comparable performance  
//  with C++ implementation in terms of measured running time.          
//  The asymptotical performance is identical.                          
//                                                                      
//  The implementation assumes that the attached sample datasets are    
//  stored in the folder "C:\\SampleData\\" ;                           
//    
// https://www.codeproject.com/Articles/5282014/Segmented-Linear-Regression
using System;
using System.Collections.Generic;

namespace CS_Classes
{
    //  implemenation of algorithms          
    //  SLR == Segmented Linear Regression ; 
    public class SLR
    {
        //  type to store coefficients A and B of linear regression 
        public class LinearRegressionParams
        {
            public double coef_a;
            public double coef_b;

            public LinearRegressionParams(double _a, double _b)
            {
                coef_a = _a;
                coef_b = _b;
            }
        }


        //  struct RangeIndex represents a semi-open range of indices [ a, b ) 
        public class RangeIndex
        {
            public int idx_a;
            public int idx_b;

            public RangeIndex(int _a, int _b)
            {
                //  empty and reversed ranges are NOT allowed in this type 
                if (_b <= _a)
                    Console.WriteLine("invalid range");

                //  a negative index will crash application 
                if (_a < 0)
                    Console.WriteLine("invalide index");

                idx_a = _a;
                idx_b = _b;
            }

            public int Length()
            {
                return (idx_b - idx_a);
            }
        }

        //  RangeLengthMin() is the limit for minimum allowed length of a range ;  
        //  a range is indivisible if its length is less than 2*RANGE_LENGTH_MIN ; 
        static public int RangeLengthMin()
        {
            return 2;
        }

        //  the function to measure the accuracy of an approximation 
        static public double ApproximationErrorY
            (
                List<double> data_y_orig,
                List<double> data_y_approx
            )
        {
            if (data_y_orig.Count != data_y_approx.Count)
            {
                Console.WriteLine("SLR: data size error");
                return double.MaxValue;
            }

            //  the result is max value of abs differences between two matching y values 
            double diff_max = 0.0;
            int n_values = data_y_orig.Count;

            if (n_values < 1)
                return diff_max;

            for (int i = 0; i < n_values; ++i)
            {
                double y_orig_i = data_y_orig[i];
                double y_aprox_i = data_y_approx[i];
                double diff_i = Math.Abs(y_orig_i - y_aprox_i);

                if (diff_i > diff_max)
                    diff_max = diff_i;
            }

            return diff_max;
        }


        //  the function LinearRegressionParameters() computes parameters of 
        //  linear regression using values of given sums ;                   
        //                                                                   
        //  this function returns <false> for special cases or invalid input 
        //  that should be processed in client code ;                        
        static public bool LinearRegressionParameters
            (
                double n_values,
                double sum_x,
                double sum_y,
                double sum_xx,
                double sum_xy,
                //  the results are                                                            
                //  coefficients a and b of linear function: y = a + b*x ;                     
                //                                                                             
                //  they are solution of the two equations:  a * N     + b * sum_x  = sum_y  ; 
                //                                           a * sum_x + b * sum_xx = sum_xy ; 
                //                                                                             
                LinearRegressionParams lin_regn_out
            )
        {
            //  result for special cases or invalid input parameters 
            lin_regn_out.coef_a = 0.0;
            lin_regn_out.coef_b = 0.0;

            const double TOLER = 1.0e-10;
            //  invalid input n_values:                       
            //      0 is UN-defined case;                     
            //      1 causes division by zero (denom ==0.0) ; 
            if (n_values < 1.0 + TOLER)
                return false;

            double denom = n_values * sum_xx - sum_x * sum_x;

            if (Math.Abs(denom) < TOLER)
            {
                //  the following special cases should be processed in client code:              
                //    1. user data represent a single point ;                                    
                //    2. regression line is vertical: coef_a==INFINITY , coeff_b is UN-defined ; 
                return false;
            }

            //  coefficients for the approximation line: y = a + b*x ;            
            lin_regn_out.coef_a = (sum_y * sum_xx - sum_x * sum_xy) / denom;
            lin_regn_out.coef_b = (n_values * sum_xy - sum_x * sum_y) / denom;
            return true;
        }

        //  the function ComputeLinearRegression() computes parameters of 
        //  linear regression and approximation error                     
        //  for a given range of a given dataset ;                        
        static public void ComputeLinearRegression
            (
                //  original dataset                                     
                List<double> data_x,
                List<double> data_y,
                //  semi-open range [ a , b )                            
                RangeIndex idx_range,
                //  coefficients of linear regression in the given range 
                LinearRegressionParams lin_regr_out,
                //  approximation error                                  
                ref double err_appr_out
            )
        {
            if (idx_range.Length() < RangeLengthMin())
            {
                Console.WriteLine("SLR error: input range is too small");
                return;
            }

            int idx_a = idx_range.idx_a;
            int idx_b = idx_range.idx_b;
            double n_vals = idx_range.Length();
            double sum_x = 0.0;
            double sum_y = 0.0;
            double sum_xx = 0.0;
            double sum_xy = 0.0;

            //  compute the required sums: 
            for (int it = idx_a; it < idx_b; ++it)
            {
                double xi = data_x[it];
                double yi = data_y[it];
                sum_x += xi;
                sum_y += yi;
                sum_xx += xi * xi;
                sum_xy += xi * yi;
            }

            //  compute parameters of linear regression in the given range 
            if (!LinearRegressionParameters(n_vals, sum_x, sum_y, sum_xx, sum_xy, lin_regr_out))
            {
                //  this is a very unusual case for real data  
                //Console.WriteLine("SLR: special case error");
                return;
            }

            double coef_a = lin_regr_out.coef_a;
            double coef_b = lin_regr_out.coef_b;

            //  use linear regression obtained to measure approximation error in the given range,          
            //  the error is the maximum of absolute differences between original and approximation values 
            double diff_max = 0.0;
            for (int it = idx_a; it < idx_b; ++it)
            {
                double xi = data_x[it];
                double yi_orig = data_y[it];
                double yi_appr = coef_a + coef_b * xi;

                double diff_i = Math.Abs(yi_orig - yi_appr);
                if (diff_i > diff_max)
                {
                    diff_max = diff_i;
                }
            }

            err_appr_out = diff_max;
        }

        //  implementation specific function-helper for better code re-use, 
        //  it enables us to measure approximations errors in results       
        static public void InterpolateSegments
            (
                List<RangeIndex> vec_ranges,
                List<LinearRegressionParams> vec_LR_params,
                List<double> data_x,
                //  results 
                List<double> data_x_interpol,
                List<double> data_y_interpol
            )
        {
            data_x_interpol.Clear();
            data_y_interpol.Clear();

            int n_ranges = vec_ranges.Count;
            for (int i_rng = 0; i_rng < n_ranges; ++i_rng)
            {
                //  in the current range we only need to interpolate y-data 
                //  using corresponding linear regression                   
                RangeIndex range_i = vec_ranges[i_rng];
                LinearRegressionParams lr_params_i = vec_LR_params[i_rng];

                double coef_a = lr_params_i.coef_a;
                double coef_b = lr_params_i.coef_b;
                int i_start = range_i.idx_a;
                int i_end = range_i.idx_b;
                for (int i = i_start; i < i_end; ++i)
                {
                    double x_i = data_x[i];
                    double y_i = coef_a + coef_b * x_i;

                    data_x_interpol.Add(x_i);
                    data_y_interpol.Add(y_i);
                }
            }
        }


        //  the function CanSplitRangeThorough()                          
        //  makes decision whether a given range should be split or not ; 
        //                                                                
        //  a given range is not subdivided if the specified accuracy of  
        //  linear regression has been achieved, otherwise, the function  
        //  searches for the best split point in the range ;              
        //                                                                
        static public bool CanSplitRangeThorough
            (
                //  original dataset                                                
                List<double> data_x,
                List<double> data_y,
                //  the limit for maximum allowed approximation error (tolerance)   
                double devn_max_user,
                //  input range to be split if linear regression is not acceptable  
                RangeIndex idx_range_in,
                //  the position of a split point, when the function returns <true> 
                ref int idx_split_out,
                //  the parameters of linear regression for the given range,        
                //  when the function returns <false>                               
                LinearRegressionParams lr_params_out
            )
        {
            //  compute linear regression and approximation error for input range 
            double error_range_in = double.MaxValue;
            ComputeLinearRegression(data_x, data_y, idx_range_in, lr_params_out, ref error_range_in);

            //  if the approximation is acceptable, input range is not subdivided 
            if (error_range_in < devn_max_user)
                return false;

            //  approximation error for a current split 
            double err_split = double.MaxValue;
            //  the position (index) of a current split 
            int idx_split = -1;
            int idx_a = idx_range_in.idx_a;
            int idx_b = idx_range_in.idx_b;
            int end_offset = RangeLengthMin();

            //  sequential search for the best split point in the input range 
            for (int idx = idx_a + end_offset; idx < idx_b - end_offset; ++idx)
            {
                //  sub-divided ranges 
                RangeIndex range_left = new RangeIndex(idx_a, idx);
                RangeIndex range_right = new RangeIndex(idx, idx_b);

                //  parameters of linear regression in sub-divided ranges 
                LinearRegressionParams lin_regr_left = new LinearRegressionParams(0.0, 0.0);
                LinearRegressionParams lin_regr_right = new LinearRegressionParams(0.0, 0.0);

                //  corresponding approximation errors 
                double err_left = double.MaxValue;
                double err_right = double.MaxValue;

                //  compute linear regression and approximation error in each range 
                ComputeLinearRegression(data_x, data_y, range_left, lin_regr_left, ref err_left);
                ComputeLinearRegression(data_x, data_y, range_right, lin_regr_right, ref err_right);

                //  we use the worst approximation error 
                double err_idx = Math.Max(err_left, err_right);
                //  the smaller error the better split   
                if (err_idx < err_split)
                {
                    err_split = err_idx;
                    idx_split = idx;
                }
            }

            //  check that sub-division is valid,                             
            //  the case of short segment: 2 or 3 data points ;               
            //  if (n==3) required approximation accuracy cannot be reached ; 
            if (idx_split < 0)
                return false;

            idx_split_out = idx_split;
            return true;
        }


        //  this function implements the smoothing method,           
        //  which is known as simple moving average ;                
        //                                                           
        //  the implementation uses symmetric window ;               
        //  the window length is variable in front and tail ranges ; 
        //  the first and last values are fixed ;                    
        static public void SimpleMovingAverage
            (
                List<double> data_io,
                int half_len
            )
        {
            int n_values = data_io.Count;

            //  no processing is required 
            if (half_len <= 0 || n_values < 3)
                return;

            //  smoothing window is too large 
            if ((2 * half_len + 1) > n_values)
                return;

            int ix = 0;
            double sum_y = 0.0;
            List<double> data_copy = new List<double>();
            data_copy.AddRange(data_io);

            //  for better readability, where relevant the code below shows   
            //  the symmetry of processing at a current data point,           
            //  for example: we use ( ix + 1 + ix ) instead of ( 2*ix + 1 ) ; 

            //  the first point is fixed 
            sum_y = data_copy[0];
            data_io[0] = sum_y / 1.0;

            //  the front range:                                                      
            //  processing accumulates sum_y using gradually increasing length window 
            for (ix = 1; ix <= half_len; ++ix)
            {
                sum_y = sum_y + data_copy[2 * ix - 1] + data_copy[2 * ix];
                data_io[ix] = sum_y / (double)(ix + 1 + ix);
            }

            //  in the middle range window length is constant 
            for (ix = (half_len + 1); ix <= ((n_values - 1) - half_len); ++ix)
            {
                //  add to window new data point and remove from window the oldest data point
                sum_y = sum_y + data_copy[ix + half_len] - data_copy[ix - half_len - 1];
                data_io[ix] = sum_y / (double)(half_len + 1 + half_len);
            }

            //  the tail range:                                    
            //  processing uses gradually decreasing length window 
            for (ix = (n_values - half_len); ix < (n_values - 1); ++ix)
            {
                sum_y = sum_y - data_copy[n_values - 1 - 2 * half_len + 2 * (ix - (n_values - 1 - half_len)) - 2]
                                - data_copy[n_values - 1 - 2 * half_len + 2 * (ix - (n_values - 1 - half_len)) - 1];

                data_io[ix] = sum_y / (double)(n_values - 1 - ix + 1 + n_values - 1 - ix);
            }

            //  the last point is fixed 
            data_io[n_values - 1] = data_copy[n_values - 1];
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
        static public void FindLocalMaxima
            (
                List<double> vec_data_in,
                List<int> vec_max_indices_res
            )
        {
            vec_max_indices_res.Clear();

            int n_values = vec_data_in.Count;

            if (n_values < 3)
                return;

            //  the last and first values are excluded from processing 
            for (int ix = 1; ix <= n_values - 2; ++ix)
            {
                double y_prev = vec_data_in[ix - 1];
                double y_curr = vec_data_in[ix];
                double y_next = vec_data_in[ix + 1];

                bool less_prev = (y_prev < y_curr);
                bool less_next = (y_next < y_curr);

                if (less_prev && less_next)
                {
                    vec_max_indices_res.Add(ix);
                    ++ix;
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
        static public bool CanSplitRangeFast
            (
                //  original dataset                                                
                List<double> data_x,
                List<double> data_y,
                //  absolute differences between original and smoothed values       
                List<double> vec_devns_in,
                //  positions (indices) of local maxima in vec_devns_in             
                List<int> vec_max_ind_in,
                //  the limit for maximum allowed approximation error (tolerance)   
                double devn_max_user,
                //  input range to be split if linear regression is not acceptable  
                RangeIndex idx_range_in,
                //  the position of a split point, when the function returns <true> 
                ref int idx_split_out,
                //  the parameters of linear regression for the given range,        
                //  when the function returns <false>                               
                LinearRegressionParams lr_params_out
            )
        {
            idx_split_out = -1;

            if (vec_devns_in.Count != data_x.Count)
            {
                Console.WriteLine("SLR: size error");
                return false;
            }

            int end_offset = RangeLengthMin();
            int range_len = idx_range_in.Length();
            if (range_len < end_offset)
            {
                Console.WriteLine("SLR: input range is too small");
                return false;
            }

            //  compute linear regression and approximation error for input range 
            double err_range_in = double.MaxValue;
            ComputeLinearRegression(data_x, data_y, idx_range_in, lr_params_out, ref err_range_in);

            //  if the approximation is acceptable, input range is not subdivided 
            if (err_range_in < devn_max_user)
                return false;

            //  check for indivisible range 
            if (range_len < 2 * RangeLengthMin())
                return false;

            if (vec_devns_in.Count == 0)
                return false;

            //  for the main criterion of splitting here we use                 
            //  the greatest local maximum of deviations inside the given range 
            int idx_split_local_max = -1;
            double devn_max = 0.0;
            double devn_cur = 0.0;
            int sz_loc_max = vec_max_ind_in.Count;

            //  find inside given range local maximum with the largest deviation 
            for (int k_max = 0; k_max < sz_loc_max; ++k_max)
            {
                int idx_max_cur = vec_max_ind_in[k_max];

                //  check if the current index is inside the given range and that  
                //  potential split will not create segment with 1 data point only 
                if ((idx_max_cur < idx_range_in.idx_a + end_offset) ||
                        (idx_max_cur >= idx_range_in.idx_b - end_offset))
                    continue;

                devn_cur = vec_devns_in[idx_max_cur];
                if (devn_cur > devn_max)
                {
                    devn_max = devn_cur;
                    idx_split_local_max = idx_max_cur;
                }
            }

            //  the case of no one local maximum inside the given range 
            if (idx_split_local_max < 0)
                return false;

            //  the case (idx_split_local_max==0) is not possible here due to (end_offset==RANGE_LENGTH_MIN), 
            //  this is a valid result ( idx_split_local_max > 0 )                                            
            idx_split_out = idx_split_local_max;

            return true;
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
        public bool SegmentedRegressionFast
            (
                //  input dataset:                                                     
                //  this function assumes that input x-data are equally spaced         
                List<double> data_x,
                List<double> data_y,
                //  user specified approximation accuracy (tolerance) ;                
                //  this parameter allows to control the total number                  
                //  and lengths of segments detected ;                                 
                double devn_max,
                //  this parameter represents half length of window ( h_len+1+h_len ), 
                //  which is used by simple moving average to create smoothed dataset  
                int sm_half_len,
                //  the resulting segmented linear regression                          
                //  is interpolated to match and compare against input values          
                List<double> data_x_res,
                List<double> data_y_res
            )
        {
            data_x_res.Clear();
            data_y_res.Clear();

            int size_x = data_x.Count;
            int size_y = data_y.Count;

            if (size_x != size_y)
                return false;

            //  check for indivisible range 
            if (size_x < 2 * RangeLengthMin())
                return false;

            //  vector of smoothed values 
            List<double> data_y_smooth = new List<double>();
            data_y_smooth.AddRange(data_y);
            SimpleMovingAverage(data_y_smooth, sm_half_len);

            //  vector of deviations (as absolute differences) between original and smoothed values 
            List<double> vec_deviations = new List<double>();
            for (int i = 0; i < size_y; ++i)
            {
                vec_deviations.Add(Math.Abs(data_y_smooth[i] - data_y[i]));
            }

            //  find positions of local maxima in the vector of deviations 
            List<int> vec_max_indices = new List<int>();
            FindLocalMaxima(vec_deviations, vec_max_indices);

            //  ranges (segments) of linear regression 
            List<RangeIndex> vec_ranges = new List<RangeIndex>();
            //  parameters of linear regression in each matching range 
            List<LinearRegressionParams> vec_LR_params = new List<LinearRegressionParams>();

            //  the stage of recursive top-down subvision:                    
            //  this processing starts from the entire range of given dataset 
            RangeIndex range_top = new RangeIndex(0, size_x);
            //  the position (index) of a current split point                 
            int idx_split = -1;
            //  parameters of linear regression in a current range (segment)  
            LinearRegressionParams lr_params = new LinearRegressionParams(0.0, 0.0);

            Stack<RangeIndex> stack_ranges = new Stack<RangeIndex>();
            stack_ranges.Push(range_top);

            while (stack_ranges.Count > 0)
            {
                range_top = stack_ranges.Pop();

                if (CanSplitRangeFast(data_x, data_y, vec_deviations, vec_max_indices,
                                        devn_max, range_top, ref idx_split, lr_params))
                {
                    //  reverse order of pushing onto stack eliminates re-ordering vec_ranges 
                    //  after this function is completed                                      
                    stack_ranges.Push(new RangeIndex(idx_split, range_top.idx_b));
                    stack_ranges.Push(new RangeIndex(range_top.idx_a, idx_split));
                }
                else
                {
                    //  the range is indivisible, we add it to the result 
                    vec_ranges.Add(new RangeIndex(range_top.idx_a, range_top.idx_b));
                    vec_LR_params.Add(new LinearRegressionParams(lr_params.coef_a, lr_params.coef_b));
                }
            }


            //  interpolate the resulting segmented linear regression 
            //  and verify the accuracy of the approximation          
            List<double> data_x_interpol = new List<double>();
            List<double> data_y_interpol = new List<double>();

            InterpolateSegments(vec_ranges, vec_LR_params, data_x,
                                    data_x_interpol, data_y_interpol);

            double appr_error = ApproximationErrorY(data_y, data_y_interpol);
            //if (appr_error > devn_max)
            //    return false;

            //  the result of this function when the required accuracy has been achieved 
            data_x_res.AddRange(data_x_interpol);
            data_y_res.AddRange(data_y_interpol);

            return true;
        }
    }
}
