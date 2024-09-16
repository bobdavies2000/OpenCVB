#include "../CameraDefines.hpp"

#ifdef ORBBEC_GEMINI335
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
#pragma comment(lib, "C:/Program Files/OrbbecSDK 1.10.5/SDK/lib/OrbbecSDK.lib") 

using namespace  cv;
using namespace std;

class CameraOrb335L
{
public:
    ob::Pipeline pipe;
    std::shared_ptr<ob::Config> imuConfig;
    int width, height;
    int* leftData = 0, * rightData = 0, * colorData = 0, * pcData = 0;
    OBCalibrationParam param;
    OBCameraParam cameraParam;
    std::shared_ptr<ob::Sensor> gyroSensor = nullptr;
    const std::shared_ptr<ob::StreamProfile> gyroProfile;
    std::shared_ptr<ob::Sensor> accelSensor = nullptr;
    float acceleration[3] = { 0, 0, 0 };
    float gyro[3] = { 0, 0, 0 };
    ob::Context ctx;
    double imuTimeStamp;
    bool FirstPass = true;
    std::mutex frameMutex;
    std::map<OBFrameType, std::shared_ptr<ob::Frame>> imuFrameMap;

    ~CameraOrb335L() {  }
    CameraOrb335L(int _width, int _height)
    {
        width = _width;
        height = _height;
        int fps = 5;
        auto devList = ctx.queryDeviceList();
        auto dev = devList->getDevice(0);

        // Configure which streams to enable or disable for the Pipeline by creating a Config
        std::shared_ptr<ob::Config> config = std::make_shared<ob::Config>();
        auto profiles = pipe.getStreamProfileList(OB_SENSOR_COLOR);
        auto colorProfile = profiles->getVideoStreamProfile(width, height, OB_FORMAT_BGR, fps);
        config->enableStream(colorProfile);

        auto depthProfiles = pipe.getStreamProfileList(OB_SENSOR_DEPTH);
        auto depthProfile = depthProfiles->getVideoStreamProfile(width, height, OB_FORMAT_Y16, fps);
        config->enableStream(depthProfile);

        auto irLeftProfiles = pipe.getStreamProfileList(OB_SENSOR_IR_LEFT);
        auto irLeftProfile = irLeftProfiles->getVideoStreamProfile(width, height, OB_FORMAT_Y8, fps);
        config->enableStream(irLeftProfile->as<ob::VideoStreamProfile>());

        auto irRightProfiles = pipe.getStreamProfileList(OB_SENSOR_IR_RIGHT);
        auto irRightProfile = irRightProfiles->getVideoStreamProfile(width, height, OB_FORMAT_Y8, fps);
        config->enableStream(irRightProfile->as<ob::VideoStreamProfile>());

        gyroSensor = dev->getSensorList()->getSensor(OB_SENSOR_GYRO);
        accelSensor = dev->getSensorList()->getSensor(OB_SENSOR_ACCEL);

        // just hit "Continue" if a break occurs here.  It doesn't happen with the Orbbec examples but running under VB.Net seems to be a problem.
        // This will happen whenever OpenCVB is compiled with "Native Code debugging" enabled. (See Properties/Debug for OpenCVB project) 
        // It does NOT happen when native code debugging is disabled whether debug or release.
        // Since the default is to turn off native code debugging, it should normally work.
        pipe.start(config);
        cameraParam = pipe.getCameraParam();
    }

    bool waitForFrame()
    {
        static OBStreamType align_to_stream = OB_STREAM_COLOR;
        static ob::Align align(align_to_stream);
        static ob::PointCloudFilter pointCloud;
        static OBCameraParam cameraParam = pipe.getCameraParam();
        pointCloud.setCameraParam(cameraParam);

        static std::vector<std::shared_ptr<ob::Frame>> gyroFrames;
        static std::mutex gyroMutex;
        static std::vector<int64_t> imuTimeStamps;
        static std::vector<std::shared_ptr<ob::Frame>> imuFrames;
        static std::mutex imuMutex;
        if (FirstPass)
        {
            FirstPass = false;
            auto profiles = gyroSensor->getStreamProfileList();
            auto profile = profiles->getProfile(OB_PROFILE_DEFAULT);
            gyroSensor->start(profile, [&](std::shared_ptr<ob::Frame> frame) {
                auto gyroFrame = frame->as<ob::GyroFrame>();
                if (gyroFrame != nullptr) {
                    std::unique_lock<std::mutex> lk(gyroMutex);
                    gyroFrames.push_back(frame);
                }
                });

            auto imuProfiles = accelSensor->getStreamProfileList();
            auto imuProfile = imuProfiles->getProfile(OB_PROFILE_DEFAULT);
            accelSensor->start(imuProfile, [&](std::shared_ptr<ob::Frame> frame) {
                auto imuFrame = frame->as<ob::AccelFrame>();
                if (imuFrame != nullptr) {
                    auto timeStamp = frame->timeStamp();
                    std::unique_lock<std::mutex> lk(imuMutex);
                    imuFrames.push_back(frame);
                    imuTimeStamps.push_back(timeStamp);
                }
                });
        }
        pcData = colorData = leftData = rightData = 0;
        while (1)
        {
            auto frameSet = pipe.waitForFrames(100);
            if (frameSet == nullptr) continue;

            auto cFrame = frameSet->colorFrame();
            auto dFrame = frameSet->depthFrame();
            auto lFrame = frameSet->getFrame(OB_FRAME_IR_LEFT);
            auto rFrame = frameSet->getFrame(OB_FRAME_IR_RIGHT);

            if (lFrame != nullptr) leftData = (int*)lFrame->data();
            if (rFrame != nullptr) rightData = (int*)rFrame->data();
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
            {
                std::unique_lock<std::mutex> lock(gyroMutex);
                auto val = gyroFrames.back()->as<ob::GyroFrame>()->value();
                gyro[0] = val.x;
                gyro[1] = val.y;
                gyro[2] = val.z;
                gyroFrames.clear();
            }
            {
                std::unique_lock<std::mutex> lock(imuMutex);
                auto val = imuFrames.back()->as<ob::AccelFrame>()->value();
                acceleration[0] = val.x;
                acceleration[1] = val.y;
                acceleration[2] = val.z;
                imuFrames.clear();
                imuTimeStamp = imuTimeStamps.back();
                imuTimeStamps.clear();
            }
            break;
        }

        return true;
    }
};

extern "C" __declspec(dllexport) int* ORBWaitForFrame(CameraOrb335L * cPtr)
{ 
	if (cPtr->waitForFrame() == false) return 0;

	return cPtr->colorData;
}
extern "C" __declspec(dllexport) int* ORBOpen(int width, int height) { return (int*) new CameraOrb335L(width, height);}
extern "C" __declspec(dllexport) void ORBClose(CameraOrb335L * cPtr) 
{ 
    cPtr->pipe.stop(); 
    delete cPtr; 
}
extern "C" __declspec(dllexport) int* ORBIntrinsics(CameraOrb335L * cPtr) 
{ 
    return (int*)&cPtr->cameraParam;
}
extern "C" __declspec(dllexport) int* ORBLeftImage(CameraOrb335L * cPtr) { return cPtr->leftData; }
extern "C" __declspec(dllexport) int* ORBRightImage(CameraOrb335L * cPtr) { return cPtr->rightData; }
extern "C" __declspec(dllexport) int* ORBPointCloud(CameraOrb335L * cPtr) { return cPtr->pcData; }
extern "C" __declspec(dllexport) int* ORBAccel(CameraOrb335L * cPtr){return (int*)&cPtr->acceleration;}
extern "C" __declspec(dllexport) int* ORBGyro(CameraOrb335L * cPtr){return (int*)&cPtr->gyro;}
extern "C" __declspec(dllexport) double ORBIMUTimeStamp(CameraOrb335L * cPtr){return cPtr->imuTimeStamp;}
#endif