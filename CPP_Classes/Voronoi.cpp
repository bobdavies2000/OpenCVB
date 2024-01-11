/////////////////////////////////////////////////////////////////
// 
//          Copyright Vadim Stadnik 2015.
// Distributed under the Code Project Open License (CPOL) 1.02. 
// (See or copy at http://www.codeproject.com/info/cpol10.aspx) 
//
/////////////////////////////////////////////////////////////////


#include <vector> 
#include <list> 
#include <deque> 
#include <set> 
#include <algorithm> 
#include <sstream>

#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "OpenCVB_Extern.h"

    //  class Point represents a two dimensional point 
class Point
{
public:
    explicit
        Point(int _x = 0, int _y = 0) : x(_x), y(_y) {  }

    int	 X() const { return x; }
    int	 Y() const { return y; }

    bool operator <  (const Point& that) const
    {
        if (x < that.x)
            return true;
        else if (that.x < x)
            return false;
        else
            return (y < that.y);
    }

    bool operator == (const Point& that) const
    {
        return (x == that.x) && (y == that.y);
    }

    bool operator != (const Point& that) const
    {
        return !(*this == that);
    }

protected:
    int     x;
    int     y;
};

//  several types of containers to store two dimensional points
typedef std::vector<Point>      VectorPoints;
typedef std::deque<Point>       DequePoints;
typedef std::list<Point>        ListPoints;
typedef std::set<Point>         SetPoints;

//  
//  the namespace VoronoiDemo provides a demonstration variant of 
//  the nearest neighbor search in an ordered set of two dimensional points; 
//  the algorithm has square root computational complexity, on average; 
//  
//  the algorithm is parameterized on types of containers, 
//  iterators and supporting algorithms; a user algorithm 
//  can take advantage of the interchangeable C++ standard 
//  containers and algorithms;
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
class VoronoiDemo 
{
    //  type for non-negative values of DistanceSquared()  
    typedef unsigned int    uint ; 
public:
    cv::Mat outImage;
    std::vector<Point> test_points;
    VoronoiDemo(void) {}

    uint DistanceSquared 
        ( 
            Point const &   pnt_a , 
            Point const &   pnt_b 
        )
    {
        int     x = pnt_b.X() - pnt_a.X() ; 
        int     y = pnt_b.Y() - pnt_a.Y() ; 
        uint    d = static_cast<uint>( x*x + y*y ) ; 

        return d ; 
    } 


    //  struct LowerBoundMemberFunction is a 
    //  function object for STL containers, such as std::set<T>, 
    //  that support member functions lower_bound(); 
    template < typename _OrderedSet > 
    struct LowerBoundMemberFunction
    {
        typedef typename _OrderedSet::const_iterator  CtIterator ; 

        LowerBoundMemberFunction ( ) { } ; 

        CtIterator
        operator ( ) 
            ( 
                _OrderedSet const &     set_points , 
                Point const &           pnt          
            ) const 
        {
            CtIterator  iter_res = set_points . lower_bound ( pnt ) ; 
            return iter_res ; 
        } 
    } ; 

 
    //  struct LowerBoundSTDAlgorithm is a 
    //  function object for STL sequence containers 
    //  that store ordered elements; 
    //  for the best performance requires a container 
    //  with random access iterators;
    template < typename _OrderedContainer > 
    struct LowerBoundSTDAlgorithm
    {
        typedef typename _OrderedContainer::const_iterator  CtIterator ; 

        LowerBoundSTDAlgorithm ( ) { } ; 

        CtIterator
        operator ( ) 
            ( 
                _OrderedContainer const &   ordered_points , 
                Point const &               pnt          
            ) const 
        {
            CtIterator  iter_begin = ordered_points . begin ( ) ; 
            CtIterator  iter_end   = ordered_points . end ( )   ;
            CtIterator  iter_res   = std::lower_bound ( iter_begin , iter_end , pnt ) ; 
            return iter_res ; 
        } 
    } ; 


    //  the function FindForward() is a helper 
    //  for the function MinDistanceOrderedSet() 
    template < typename _CtIterator > 
    void FindForward 
        ( 
            Point const &   pnt      , 
            _CtIterator     it_cur   , 
            _CtIterator     it_end   , 
            uint &          dist_min 
        )
    {
        uint        dist_cur = 0 ; 
        uint        dist_x   = 0 ; 

        while ( it_cur != it_end ) 
        {
            dist_cur = DistanceSquared ( *it_cur , pnt ) ; 
            dist_x   = (it_cur->X() - pnt.X())*(it_cur->X() - pnt.X()) ; 

            if ( dist_cur < dist_min ) 
                dist_min = dist_cur ; 

            if ( dist_x > dist_min ) 
                break ; 

            ++it_cur ; 
        } 
    } 


    //  the function FindBackward() is a helper 
    //  for the function MinDistanceOrderedSet(), 
    //  generally, it is NOT safe if container is empty;
    template < typename _CtIterator > 
    void FindBackward 
        ( 
            Point const &   pnt      , 
            _CtIterator     it_cur   , 
            _CtIterator     it_begin , 
            uint &          dist_min
        )
    {
        uint        dist_cur = 0 ; 
        uint        dist_x   = 0 ; 

        do 
        {
            //  it is safe if input ( it_cur == container.end() )  
            //  and container is NOT empty 
            --it_cur ; 

            dist_cur = DistanceSquared ( *it_cur , pnt ) ; 
            dist_x   = (it_cur->X() - pnt.X())*(it_cur->X() - pnt.X()) ; 

            if ( dist_cur < dist_min ) 
                dist_min = dist_cur ; 

            if ( dist_x > dist_min ) 
                break ; 
        }
        while ( it_cur != it_begin ) ;
    } 


    //  the function MinDistanceOrderedSet() implements  
    //  the nearest neighbor search in an ordered set of points, 
    //  its average computational complexity - O ( sqrt(N) ) ,
    //  where N is the number of points in the set; 
    //  
    //  the template parameter <_OrderedSet> represents either 
    //  an ordered set or an ordered sequence of two-dimensional points; 
    //  both std::vector<T> and std::set<T> can be used as template arguments; 
    //  
    //  the template parameter <_FuncLowBound> represents an algorithm 
    //  that finds for an input point its lower bound in 
    //  either an ordered set or an ordered sequence of two dimensional points; 
    //  the namespace VoronoiDemo provides two function objects of this algorithm: 
    //  LowerBoundMemberFunction and LowerBoundSTDAlgorithm; 
    //  to achieve the specified computational complexity 
    //  an object of LowerBoundSTDAlgorithm should be used 
    //  with a container that supports random access iterators; 
    //
    //  for examples how to use the function MinDistanceOrderedSet(), 
    //  see the code below in this file; 
    //  
    template < typename _OrderedSet , typename _FuncLowBound > 
    uint  MinDistanceOrderedSet 
        ( 
            _OrderedSet const &     set_points , 
            _FuncLowBound           find_LB    , 
            Point const &           pnt         
        )
    {
        typedef typename _OrderedSet::const_iterator  CtIterator ; 

        uint        dist_min   = UINT_MAX ; 
        CtIterator  iter_begin = set_points . begin ( ) ; 
        CtIterator  iter_end   = set_points . end ( )   ;
        //  call lower boundary algorithm through a function object
        CtIterator  iter_forw  = find_LB ( set_points , pnt ) ; 
        CtIterator  iter_back  = iter_forw ; 

        bool        move_forward  = ( iter_forw != iter_end   ) ; 
        bool        move_backward = ( iter_back != iter_begin ) ; 

        if ( move_forward ) 
            FindForward  ( pnt , iter_forw , iter_end   , dist_min ) ; 
        if ( move_backward ) 
            FindBackward ( pnt , iter_back , iter_begin , dist_min ) ; 

        return dist_min ; 
    } 


    //  the function TestOrderedSet() tests the efficiency of 
    //  the nearest neighbor search in an ordered set of points;
    //
    //  this test emulates the computation of the distance transform; 
    //  it calculates the minimum distance from each point in 
    //  the given rectangle to a point in the input set; 
    //
    template < typename _OrderedSet , typename _FuncLowBound > 
    cv::Mat TestOrderedSet(const int rect_width, const int rect_height) 
    {
        _OrderedSet     set_points ( test_points.begin() , test_points.end() ) ; 
        _FuncLowBound   func_lower_bound ;    

        cv::Mat dist = cv::Mat(rect_height, rect_width, CV_32F);
        for     ( int  x = 0 ; x < rect_width  ; ++x )
        {
            for ( int  y = 0 ; y < rect_height ; ++y )
            {
                Point p(x, y); 
                float nextVal = (float)MinDistanceOrderedSet(test_points, func_lower_bound, p);
                dist.at<float>(y, x) = nextVal;
            }
        }

        
        return dist; 
    } 


    //  the function Run: 
    //  the result is sorted and contains no duplicates,
    //  note that  test_points.size() <= n_points  ;
    void Run(int width, int height)
    {
        std::sort(test_points.begin(), test_points.end());

        //  remove duplicates 
        std::vector<Point>::iterator  it_new_end;
        it_new_end = std::unique(test_points.begin(), test_points.end());
        test_points.erase(it_new_end, test_points.end());

        outImage = TestOrderedSet< VectorPoints, LowerBoundSTDAlgorithm<VectorPoints>>(width, height);
    }
};


VB_EXTERN
VoronoiDemo * VoronoiDemo_Open()
{
    VoronoiDemo* cPtr = new VoronoiDemo();
    return cPtr;
}

VB_EXTERN
int * VoronoiDemo_Close(VoronoiDemo *cPtr)
{
    delete cPtr;
    return (int*)0;
}

VB_EXTERN
int* VoronoiDemo_Run(VoronoiDemo *cPtr, cv::Point *input, int pointCount, int width, int height)
{
    cPtr->test_points.clear();
    for (int i = 0; i < pointCount; ++i)
    {
        cv::Point pt = input[i];
        cPtr->test_points.push_back(Point(pt.x, pt.y));
    }

    cPtr->Run(width, height);
    return (int*)cPtr->outImage.data;
}
