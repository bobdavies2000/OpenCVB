#include "../VTK.h"
#ifdef WITH_VTK

#ifdef _DEBUG
#pragma comment(lib, "..\\..\\opencv\\build\\lib\\debug\\opencv_viz451d.lib")
#else
#pragma comment(lib, "..\\..\\opencv\\build\\lib\\debug\\opencv_viz451.lib")
#endif

#include <iostream>
#ifdef _DEBUG
#include "../Data/PragmaLibsD.h"
#else
#include "../Data/PragmaLibs.h"
#endif

using namespace std;

#include <opencv2/viz.hpp>

class vtkHist
{
private:
public:
	cv::Mat histogram;
	double threshold;
	cv::Ptr<cv::viz::Viz3d> fen3D;
	int nbWidget;
	double maxH;
	int code;
	cv::Mat dst;
	int bins;

	void DrawHistogram3D()
	{
		int planeSize = (int)histogram.step1(0);
		int cols = (int)histogram.step1(1);
		int rows = (int)planeSize / cols;

		int planes = (int)histogram.total() / planeSize;
		fen3D->removeAllWidgets();
		fen3D->showWidget("Axis", cv::viz::WCoordinateSystem(10));
		for (int k = 0; k < planes; k++)
		{
			for (int i = 0; i < rows; i++)
			{
				for (int j = 0; j < cols; j++)
				{
					double x = histogram.at<float>(k, i, j);
					if (x >= threshold)
					{
						double r = std::max(x / maxH, 0.1);

						cv::Point3d p1 = cv::Point3d(k - r / 2, i - r / 2, j - r / 2);
						cv::Point3d p2 = cv::Point3d(k + r / 2, i + r / 2, j + r / 2);
						cv::viz::Color c = cv::viz::Color(j / double(planes) * 255, i / double(rows) * 255, k / double(cols) * 255);

						cv::viz::WCube s(p1, p2, false, c);
						fen3D->showWidget(cv::format("I3d%d", nbWidget++), s);
					}
				}
			}
		}
	}

	vtkHist()
	{
		fen3D = new cv::viz::Viz3d("3D Histogram");
		fen3D = cv::makePtr<cv::viz::Viz3d>("3D Histogram");
	}

	void Run(cv::Mat input)
	{
		float hRange[] = { 0, 256 };
		const float* range[] = { hRange, hRange,hRange };
		int hBins[] = { bins, bins, bins };
		int channel[] = { 2, 1, 0 };
		cv::calcHist(&input, 1, channel, cv::Mat(), histogram, 3, hBins, range, true, false);
		cv::normalize(histogram, histogram, 100.0f / input.total(), 0, cv::NORM_MINMAX, -1, cv::Mat());
		cv::minMaxIdx(histogram, NULL, &maxH, NULL, NULL);

		fen3D = cv::makePtr<cv::viz::Viz3d>("3D Histogram");
		nbWidget = 0;
		DrawHistogram3D();
	}
};
#endif

int main(int argc, char **argv)
{
#ifdef WITH_VTK
	windowTitle << "OpenCVB VTK_Data Cloud"; // this will create the window title.
	if (initializeNamedPipeAndMemMap(argc, argv) != 0) return -1;

	vtkHist *v = new vtkHist();
	while (1)
	{
		readPipeAndMemMap();
		if (UserData[0] != v->bins || UserData[1] != v->threshold || UserData[2] != 0)
		{
			v->bins = (int)UserData[0];
			v->threshold = UserData[1];
		}

		if (rgbWidth) v->Run(src); else  v->Run(data32f);

		v->fen3D->spinOnce(1);
		v->DrawHistogram3D();
		if (ackBuffers()) break;
	}
#endif
	return 0;
}
