#pragma once
using namespace std;
using namespace  cv;
// easiest way to share among all the different camera interfaces.
int* cleanUpDepth(Mat pc, bool closeEdges)
{
	bool closeZ = true; // always close the single pixel gaps.
	if (closeZ)
	{
		for (int y = 1; y < pc.rows - 1; y++)
		{
			for (int x = 1; x < pc.cols - 1; x++)
			{
				Point3f s1 = pc.at<Point3f>(y, x - 1);
				Point3f s2 = pc.at<Point3f>(y, x);
				if (s1.z > 0 && s2.z == 0)
				{
					Point3f s3 = pc.at<Point3f>(y - 1, x);
					Point3f s4 = pc.at<Point3f>(y + 1, x);
					Point3f s5 = pc.at<Point3f>(y, x + 1);
					if ((s3.z > 0 && s4.z > 0) || s5.z > 0) pc.at<Point3f>(y, x) = s1; // duplicate the neighbor next to this pixel missing any depth.
				}
			}
		}
	}
	if (closeEdges)
	{
		int y = 0;
		int maxGap = 50;
		for (int x = 0; x < pc.cols; x++)
		{
			for (int y = 0; y < maxGap; y++)
			{
				Point3f s1 = pc.at<Point3f>(y, x);
				if (s1.z > 0)
				{
					for (int i = 0; i < y; i++)
					{
						pc.at<Point3f>(i, x) = s1;
					}
					break;
				}
			}
			for (int y = 1; y < maxGap; y++)
			{
				Point3f s1 = pc.at<Point3f>(pc.rows - y, x);
				if (s1.z > 0)
				{
					for (int i = 1; i < y; i++)
					{
						pc.at<Point3f>(pc.rows - i, x) = s1;
					}
					break;
				}
			}
		}
		for (int y = 0; y < pc.rows; y++)
		{
			for (int x = 0; x < maxGap; x++)
			{
				Point3f s1 = pc.at<Point3f>(y, x);
				if (s1.z > 0)
				{
					for (int i = 0; i < x; i++)
					{
						pc.at<Point3f>(y, i) = s1;
					}
					break;
				}
			}
			for (int x = 1; x < maxGap; x++)
			{
				Point3f s1 = pc.at<Point3f>(y, pc.cols - x);
				if (s1.z > 0)
				{
					for (int i = 1; i < x; i++)
					{
						pc.at<Point3f>(y, pc.cols - i) = s1;
					}
					break;
				}
			}
		}
	}
	return (int*)pc.data;
}