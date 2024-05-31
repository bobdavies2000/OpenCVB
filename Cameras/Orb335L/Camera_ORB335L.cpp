#include "../CameraDefines.hpp"
#ifdef ORB335L
#include "libobsensor/hpp/Pipeline.hpp"
#include "libobsensor/hpp/Error.hpp"
#include <mutex>
#include <thread>
#include <libobsensor/ObSensor.hpp>
//#include <opencv2/opencv.hpp>
#include <string>
#include <condition_variable>
#include <cmath>

#include <opencv2/core.hpp>
#include <opencv2/calib3d.hpp>
#include "opencv2/imgproc.hpp"
#include "opencv2/imgcodecs.hpp"
#include "opencv2/highgui.hpp"
#include "PragmaLibs.h" 

using namespace  cv;
using namespace std;

class CameraOrb335L
{
public:
	ob::Pipeline pipe;
	int width, height;
	int *leftData, *rightData, *colorData, *pcData;
	~CameraOrb335L() {  }
	CameraOrb335L(int _width, int _height)
	{
		width = _width;
		height = _height;

        // Configure which streams to enable or disable for the Pipeline by creating a Config
        std::shared_ptr<ob::Config> config = std::make_shared<ob::Config>();

        std::shared_ptr<ob::VideoStreamProfile> colorProfile = nullptr;
        try {
            // Get all stream profiles of the color camera, including stream resolution, frame rate, and frame format
            auto profiles = pipe.getStreamProfileList(OB_SENSOR_COLOR);
            try {
                // Find the corresponding Profile according to the specified format, and choose the RGB888 format first
                colorProfile = profiles->getVideoStreamProfile(width, height, OB_FORMAT_BGR, 60);
            }
            catch (ob::Error& e) {
                // If the specified format is not found, select the first one (default stream profile)
                colorProfile = std::const_pointer_cast<ob::StreamProfile>(profiles->getProfile(OB_PROFILE_DEFAULT))->as<ob::VideoStreamProfile>();
            }
            config->enableStream(colorProfile);
        }
        catch (ob::Error& e) {
            std::cerr << "Current device is not support color sensor!" << std::endl;
            exit(EXIT_FAILURE);
        }

        // Start the pipeline with config
        pipe.start(config);

	}

	bool waitForFrame()
	{
        while (1)
        {
            auto frameSet = pipe.waitForFrames(100);
            if (frameSet == nullptr) continue;
            colorData = (int *) frameSet->colorFrame()->data();
            auto _depthFrame = frameSet->depthFrame();
            auto _pcFrame = frameSet->pointsFrame();
            auto _keftFrame = frameSet->pointsFrame();
            auto _rightFrame = frameSet->pointsFrame();
            break;
        }

		return true;
	}
}; 

float acceleration[3];
float gyro[3];
double imuTimeStamp;
extern "C" __declspec(dllexport) void ORBtaskIMU(CameraOrb335L * cPtr)
{

}

extern "C" __declspec(dllexport) int* ORBWaitForFrame(CameraOrb335L * cPtr, int w, int h)
{ 
	if (cPtr->waitForFrame() == false) return 0;

	return cPtr->colorData;
}
extern "C" __declspec(dllexport) int* ORBOpen(int width, int height) { return (int*) new CameraOrb335L(width, height);}
extern "C" __declspec(dllexport) void ORBClose(CameraOrb335L * cPtr) { cPtr->pipe.stop(); delete cPtr; }
//extern "C" __declspec(dllexport) int* ORBIntrinsicsLeft(CameraOrb335L * cPtr) { return (int*)&cPtr->intrinsicsBoth.left; }
extern "C" __declspec(dllexport) int* ORBRightImage(CameraOrb335L * cPtr) {return (int*)cPtr->rightData;}
extern "C" __declspec(dllexport) int* ORBPointCloud(CameraOrb335L * cPtr) { return (int*)cPtr->pcData; }
extern "C" __declspec(dllexport) int* ORBAcceleration(CameraOrb335L * cPtr){return (int*)&acceleration;}
extern "C" __declspec(dllexport) int* ORBGyro(CameraOrb335L * cPtr){return (int*)&gyro;}
extern "C" __declspec(dllexport) double ORBIMU_TimeStamp(CameraOrb335L * cPtr){return imuTimeStamp;}
#endif