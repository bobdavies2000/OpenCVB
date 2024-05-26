/////////////////////////////////////////////////////////////////
//                                                               
//          Copyright Vadim Stadnik 2020.                        
// Distributed under the Code Project Open License (CPOL) 1.02.  
// (See or copy at http://www.codeproject.com/info/cpol10.aspx)  
//                                                               
//  this file contains the demonstration code for the article    
//  by V. Stadnik "Simple approach to Voronoi diagrams";         
//                                                               
/////////////////////////////////////////////////////////////////

using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using cv = OpenCvSharp;

//                                                                      
//  the namespace VoronoiDemo provides a demonstration variant of       
//  the nearest neighbor search in an ordered dataset of                
//  two dimensional points;                                             
//  the algorithm has square root computational complexity, on average; 
//                                                                      
//  the performance test emulates the computation of                    
//  the distance transform; the performance of the developed            
//  algorithm can be compared with the performance of                   
//  the brute force algorithm;                                          
//                                                                      
//  the code of the namespace VoronoiDemo has been written              
//  to avoid complications associated with numeric errors of            
//  floating data types in comparison operations;                       
//  the code uses integer type for X and Y coordinates of               
//  two dimensional points; in addition to this, instead of             
//  distance between two points, it calculates squared distance,        
//  which also takes advantage of the exact integer value;              
//                                                                      
namespace CS_Classes
{
    public class VoronoiDemo
    {

        //  class Point represents a two dimensional point 
        public class Point
        {
            protected int x;
            protected int y;

            public Point(int _x, int _y)
            {
                x = _x;
                y = _y;
            }

            public int X() { return x; }
            public int Y() { return y; }

            public bool IsEqual(Point that)
            {
                return (this.X() == that.X() &&
                         this.Y() == that.Y());
            }

            public bool NotEqual(Point that)
            {
                return (!this.IsEqual(that));
            }

            static public uint DistanceSquared(Point pnt_a, Point pnt_b)
            {
                int x = pnt_b.X() - pnt_a.X();
                int y = pnt_b.Y() - pnt_a.Y();
                uint d = (uint)(x * x + y * y);
                return d;
            }
        }


        //  the comparison operation to order a given list or an array for search, 
        //  which is more efficient than straightforward sequential search         
        public class PointComparer : IComparer<Point>
        {
            //  compare coordinates in X then Y order 
            public int Compare(Point pnt_a, Point pnt_b)
            {
                if (pnt_a.X().CompareTo(pnt_b.X()) != 0)
                {
                    return pnt_a.X().CompareTo(pnt_b.X());
                }
                else if (pnt_a.Y().CompareTo(pnt_b.Y()) != 0)
                {
                    return pnt_a.Y().CompareTo(pnt_b.Y());
                }
                else
                    return 0;
            }
        }

        //  the comparison operation to remove duplicates from a given dataset 
        class PointEquality : IEqualityComparer<Point>
        {
            public bool Equals(Point pnt_a, Point pnt_b)
            {
                if (pnt_a.IsEqual(pnt_b))
                    return true;
                else
                    return false;
            }

            public int GetHashCode(Point pnt)
            {
                int hCode = pnt.X() ^ pnt.Y();
                return hCode.GetHashCode();
            }
        }


        //  implementation of the algorithm using sequential search 
        class AlgoBruteForce
        {
            //  the function MinDistanceBruteForce() implements 
            //  the brute force sequential search algorithm;    
            //  a dataset can be either ordered or unordered;   
            //                                                  
            //  computational complexity - O(N),                
            //  where N is the number of points in a container; 
            static public uint MinDistanceBruteForce
                (
                    Point point_in,
                    List<Point> points
                )
            {
                uint dist_min = uint.MaxValue;
                uint dist_cur = dist_min;
                int n_points = points.Count;

                for (int i = 0; i < n_points; ++i)
                {
                    dist_cur = Point.DistanceSquared(point_in, points[i]);

                    if (dist_cur < dist_min)
                        dist_min = dist_cur;
                }

                return dist_min;
            }

            //  the function TestPerformance() measures the running time of  
            //  the nearest neighbor search in an ordered dataset of points; 
            //                                                               
            //  the test emulates the computation of the distance transform; 
            //  it calculates the minimum distance from each point in        
            //  the given rectangle to a point in the input dataset;         
            static public cv.Mat TestPerformance
                (
                    int rect_width,
                    int rect_height,
                    List<Point> test_points
                )
            {
                cv.Mat dist = new cv.Mat(rect_height, rect_width, cv.MatType.CV_32F);
                PointComparer pnt_comparer = new PointComparer();
                test_points.Sort(pnt_comparer);

                Stopwatch watch = new Stopwatch();
                watch.Start();

                for (int x = 0; x < rect_width; ++x)
                {
                    for (int y = 0; y < rect_height; ++y)
                    {
                        float nextVal = MinDistanceBruteForce(new Point(x, y), test_points);
                        dist.Set<float>(y, x, nextVal);
                    }
                }

                watch.Stop();
                Console.WriteLine("execution time of AlgoBruteForce algorithm = {0} ms ;", watch.ElapsedMilliseconds);

                return dist;
            }

        }   //  class AlgoBruteForce ; 


        //  implementation of the algorithm using efficient search 
        //  on ordered dataset                                     
        class AlgoOrderedList
        {
            //  the function LowerBound() implements a binary search,   
            //  which in terms of operator < returns the first position 
            //  that satisfies the following condition:                 
            //      ! ( points_ordered[pos] < point_in ) == true ;      
            //                                                          
            //  the computational complexity is O(log N),               
            //  where N is the number of points in a dataset;           
            static protected int LowerBound
                (
                    List<Point> points_ordered,
                    PointComparer pnt_comparer_in,
                    Point point_in
                )
            {
                int i_low = 0;
                int i_high = points_ordered.Count;
                int i_mid = 0;

                while (i_low < i_high)
                {
                    i_mid = (i_low + i_high) / 2;

                    if (pnt_comparer_in.Compare(points_ordered[i_mid], point_in) < 0)
                    {
                        i_low = i_mid + 1;
                    }
                    else
                    {
                        i_high = i_mid;
                    }
                }

                return i_low;
            }

            //  the function FindForward() is a helper   
            //  for the function MinDistanceOrderedSet() 
            static protected void FindForward
                (
                    Point point_in,
                    int i_low_bound,
                    int i_end,
                    List<Point> points_ordered,
                    ref uint dist_min_io
                )
            {
                uint dist_cur = 0;
                uint dist_x = 0;

                for (int i = i_low_bound; i < i_end; ++i)
                {
                    dist_cur = Point.DistanceSquared(points_ordered[i], point_in);
                    dist_x = (uint)(points_ordered[i].X() - point_in.X()) *
                               (uint)(points_ordered[i].X() - point_in.X());

                    if (dist_cur < dist_min_io)
                        dist_min_io = dist_cur;

                    if (dist_x > dist_min_io)
                        break;
                }
            }

            //  the function FindBackward() is a helper  
            //  for the function MinDistanceOrderedSet() 
            static protected void FindBackward
                (
                    Point point_in,
                    int i_low_bound,
                    int i_start,
                    List<Point> points_ordered,
                    ref uint dist_min_io
                )
            {
                uint dist_cur = 0;
                uint dist_x = 0;

                for (int i = i_low_bound - 1; i >= 0; --i)
                {
                    dist_cur = Point.DistanceSquared(points_ordered[i], point_in);
                    dist_x = (uint)(points_ordered[i].X() - point_in.X()) *
                               (uint)(points_ordered[i].X() - point_in.X());

                    if (dist_cur < dist_min_io)
                        dist_min_io = dist_cur;

                    if (dist_x > dist_min_io)
                        break;
                }
            }


            //  the function MinDistanceOrderedSet() implements          
            //  the nearest neighbor search in an ordered set of points; 
            //  its average computational complexity - O ( sqrt(N) ) ,   
            //  where N is the number of points in the set;              
            static protected uint MinDistanceOrderedSet
                (
                    Point point_in,
                    PointComparer pnt_comparer_in,
                    List<Point> points_ordered
                )
            {
                uint dist_min = uint.MaxValue;
                int i_start = 0;
                int i_end = points_ordered.Count;
                int i_low_bound = 0;

                i_low_bound = LowerBound(points_ordered, pnt_comparer_in, point_in);

                FindForward(point_in, i_low_bound, i_end, points_ordered, ref dist_min);
                FindBackward(point_in, i_low_bound, i_start, points_ordered, ref dist_min);

                return dist_min;
            }


            //  the function TestPerformance() measures the running time of  
            //  the nearest neighbor search in an ordered dataset of points; 
            //                                                               
            //  the test emulates the computation of the distance transform; 
            //  it calculates the minimum distance from each point in        
            //  the given rectangle to a point in the input dataset;         
            static public cv.Mat TestPerformance
                (
                    int rect_width,
                    int rect_height,
                    List<Point> test_points
                )
            {
                cv.Mat dist = new cv.Mat(rect_height, rect_width, cv.MatType.CV_32F);
                PointComparer pnt_comparer = new PointComparer();
                test_points.Sort(pnt_comparer);

                Stopwatch watch = new Stopwatch();
                watch.Start();

                for (int x = 0; x < rect_width; ++x)
                {
                    for (int y = 0; y < rect_height; ++y)
                    {
                        float nextVal = (float)MinDistanceOrderedSet(new Point(x, y), pnt_comparer, test_points);
                        dist.Set<float>(y, x, nextVal);
                    }
                }

                watch.Stop();
                Console.WriteLine("execution time of ordered dataset algorithm = {0} ms ;", watch.ElapsedMilliseconds);

                return dist;
            }

        }   //  class AlgoOrderedList ; 


        //  class to generate test datasets of random points 
        public class TestPoints
        {
            //  this function generates random points inside the    
            //  specified rectangle: [ 0, width ) x [ 0, height ) ; 
            //                                                      
            //  the result is sorted and contains no duplicates;    
            //  note also that  points_res.size() <= n_points  ;    
            static public void Generate
                (
                    //  rectangle area to fill in 
                    int width,
                    int height,
                    int n_points,
                    List<Point> points_out
                )
            {
                points_out.Clear();

                Random rand_x = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
                Thread.Sleep(20);
                Random rand_y = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);

                HashSet<Point> hash_set = new HashSet<Point>(new PointEquality());
                int n_duplicates = 0;

                for (int i = 0; i < n_points; ++i)
                {
                    int xi = rand_x.Next(width);
                    int yi = rand_y.Next(height);
                    if (hash_set.Add(new Point(xi, yi)))
                    {
                        points_out.Add(new Point(xi, yi));
                    }
                    else
                        ++n_duplicates;
                }

                //  if ( n_duplicates > 0 ) 
                //      Console . WriteLine ( "test points: {0} duplicates removed;", n_duplicates ) ;

                points_out.Sort(new PointComparer());
            }
        }   //  class TestPoints ; 

        public void New() { }

        public void RunCS(ref cv.Mat src, List<cv.Point2f> points, bool bruteForce)
        {
            List<Point> test_points = new List<Point>();
            foreach (cv.Point pt in points)
            {
                test_points.Add(new Point(pt.X, pt.Y));
            }

            if (bruteForce == false)
                src = AlgoOrderedList.TestPerformance(src.Width, src.Height, test_points);
            else
                src = AlgoBruteForce.TestPerformance(src.Width, src.Height, test_points);
        }
        public void RunCS(ref cv.Mat src, List<cv.Point2f> points)
        {
            List<Point> test_points = new List<Point>();
            foreach (cv.Point pt in points)
            {
                test_points.Add(new Point(pt.X, pt.Y));
            }

            src = AlgoOrderedList.TestPerformance(src.Width, src.Height, test_points);
        }
    }
}

