#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/ximgproc.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/core/utility.hpp>
using namespace std;
using namespace  cv;
class RedCC_FindCells
{
private:
public:
    vector <int> cellList;
    RedCC_FindCells(){}
    void RunCPP(Mat src) {
        cellList.clear();
        for (int y = 0; y < src.rows; y++)
            for (int x = 0; x < src.cols; x++)
            {
                auto val = src.at<unsigned char>(y, x);
                if (val > 0)
                {
                    if (count(cellList.begin(), cellList.end(), val) == 0) 
                        cellList.push_back(val);
                }
            }
    }
};
extern "C" __declspec(dllexport)
RedCC_FindCells *RedCC_FindCells_Open() {
    RedCC_FindCells *cPtr = new RedCC_FindCells();
    return cPtr;
}
extern "C" __declspec(dllexport)
void RedCC_FindCells_Close(RedCC_FindCells *cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport) int RedCC_FindCells_TotalCount(RedCC_FindCells * cPtr) { return int(cPtr->cellList.size()); }
extern "C" __declspec(dllexport)
int *RedCC_FindCells_RunCPP(RedCC_FindCells *cPtr, int *dataPtr, int rows, int cols)
{
		cPtr->RunCPP(Mat(rows, cols, CV_8UC1, dataPtr));
		return (int *) &cPtr->cellList[0]; 
}
