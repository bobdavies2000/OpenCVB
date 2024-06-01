#include "../CameraDefines.hpp"
#ifdef ORB335L
#include "libobsensor/hpp/Pipeline.hpp"
#include "libobsensor/hpp/Error.hpp"
#include <mutex>
#include <thread>
#include <libobsensor/ObSensor.hpp>
#include <libobsensor/hpp/Utils.hpp>
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
    OBCalibrationParam param;
    ~CameraOrb335L() {  }
	CameraOrb335L(int _width, int _height)
	{
		width = _width;
		height = _height;
        int fps = 5;

        // Configure which streams to enable or disable for the Pipeline by creating a Config
        std::shared_ptr<ob::Config> config = std::make_shared<ob::Config>();
        auto profiles = pipe.getStreamProfileList(OB_SENSOR_COLOR);
        auto colorProfile = profiles->getVideoStreamProfile(width, height, OB_FORMAT_BGR, fps);
        config->enableStream(colorProfile);


        auto depthProfiles = pipe.getStreamProfileList(OB_SENSOR_DEPTH);
        auto depthProfile = depthProfiles->getVideoStreamProfile(width, height, OB_FORMAT_Y16, fps);
        config->enableStream(depthProfile);

        // It does not appear that orbbec allows collecting left/right and color.  This is a mistake if true.
         
        auto irLeftProfiles = pipe.getStreamProfileList(OB_SENSOR_IR_LEFT);
        try {
            auto irLeftProfile = irLeftProfiles->getVideoStreamProfile(width, height, OB_FORMAT_Y8, fps);
            config->enableStream(irLeftProfile->as<ob::VideoStreamProfile>());
        }
        catch (...) {
            std::cout << "IR(Left) stream not found!" << std::endl;
        }

        auto irRightProfiles = pipe.getStreamProfileList(OB_SENSOR_IR_RIGHT);
        try {
            auto irRightProfile = irRightProfiles->getVideoStreamProfile(width, height, OB_FORMAT_Y8, fps);
            config->enableStream(irRightProfile->as<ob::VideoStreamProfile>());
        }
        catch (...) {
            std::cout << "IR(Right) stream not found!" << std::endl;
        }




        // just hit "Continue" if a break occurs here.  It doesn't happen with the Orbbec examples but running under VB.Net seems to be a problem.
        // This will happen whenever OpenCVB.exe is compiled with "Native Code debugging" enabled. (Properties/Debug) 
        // It does NOT happen when native code debugging is disabled whether debug or release.
        // Since the default is to turn off native code debugging, it should normally work.
        try {
            pipe.start(config);
        }
        catch (...) {}  param = pipe.getCalibrationParam(config);
	}

	bool waitForFrame()
	{
        static OBStreamType align_to_stream = OB_STREAM_COLOR; 
        static ob::Align align(align_to_stream);
        static ob::PointCloudFilter pointCloud;
        // get camera intrinsic and extrinsic parameters form pipeline and set to point cloud filter
        static OBCameraParam cameraParam = pipe.getCameraParam();
        pointCloud.setCameraParam(cameraParam);

        pcData = colorData = leftData = rightData = 0;
        while (1)
        {
            auto frameSet = pipe.waitForFrames(100);
            if (frameSet == nullptr) continue;

            auto lFrame = frameSet->getFrame(OB_FRAME_IR_LEFT);
            auto rFrame = frameSet->getFrame(OB_FRAME_IR_RIGHT);
            if (lFrame != nullptr) 
                leftData = (int*)lFrame->data();
            if (rFrame != nullptr) 
                rightData = (int*)rFrame->data();

            auto cFrame = frameSet->colorFrame();
            auto dFrame = frameSet->depthFrame();
            if (cFrame != nullptr) colorData = (int*)frameSet->colorFrame()->data();
            if (dFrame != nullptr)
            {
                auto newFrame = align.process(frameSet);
                auto newFrameSet = newFrame->as<ob::FrameSet>();
                cFrame = newFrameSet->colorFrame();
                dFrame = newFrameSet->depthFrame();

                static float depthValueScale = dFrame->getValueScale();
                pointCloud.setPositionDataScaled(depthValueScale);
                pointCloud.setCreatePointFormat(OB_FORMAT_POINT);
                pcData = (int*)pointCloud.process(newFrameSet)->data();
            }
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

extern "C" __declspec(dllexport) int* ORBWaitForFrame(CameraOrb335L * cPtr)
{ 
	if (cPtr->waitForFrame() == false) return 0;

	return cPtr->colorData;
}
extern "C" __declspec(dllexport) int* ORBOpen(int width, int height) { return (int*) new CameraOrb335L(width, height);}
extern "C" __declspec(dllexport) void ORBClose(CameraOrb335L * cPtr) 
{ 
    cPtr->pipe.stop(); delete cPtr; 
}
//extern "C" __declspec(dllexport) int* ORBIntrinsicsLeft(CameraOrb335L * cPtr) { return (int*)&cPtr->intrinsicsBoth.left; }
extern "C" __declspec(dllexport) int* ORBLeftImage(CameraOrb335L * cPtr) { return cPtr->leftData; }
extern "C" __declspec(dllexport) int* ORBRightImage(CameraOrb335L * cPtr) { return cPtr->rightData; }
extern "C" __declspec(dllexport) int* ORBPointCloud(CameraOrb335L * cPtr) { return cPtr->pcData; }
extern "C" __declspec(dllexport) int* ORBAcceleration(CameraOrb335L * cPtr){return (int*)&acceleration;}
extern "C" __declspec(dllexport) int* ORBGyro(CameraOrb335L * cPtr){return (int*)&gyro;}
extern "C" __declspec(dllexport) double ORBIMU_TimeStamp(CameraOrb335L * cPtr){return imuTimeStamp;}
#endif