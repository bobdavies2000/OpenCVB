#include "../CameraDefines.hpp"
#ifdef ORB335L
//#include "libobsensor/hpp/Pipeline.hpp"
//#include "libobsensor/hpp/Error.hpp"
//#include <libobsensor/ObSensor.hpp>
#include <mutex>
#include <thread>

extern "C" {
#include <stdlib.h>
#include <libobsensor/h/Error.h>
#include <libobsensor/h/Frame.h>
#include <libobsensor/h/ObTypes.h>
#include <libobsensor/h/Pipeline.h>
#include <libobsensor/h/StreamProfile.h>
#include <libobsensor/h/Device.h>
#include <libobsensor/h/Sensor.h>
}

#include <opencv2/core.hpp>
#include <opencv2/calib3d.hpp>
#include "opencv2/imgproc.hpp"
#include "opencv2/imgcodecs.hpp"
#include "opencv2/highgui.hpp"
#include <string>
#include <thread>
#include <mutex>
#include <condition_variable>
#include <cmath>
#include "PragmaLibs.h" 

using namespace  cv;
using namespace std;

class CameraOrb335L
{
	void check_error(ob_error* error) {
		if (error) {
			printf("ob_error was raised: \n\tcall: %s(%s)\n", ob_error_function(error), ob_error_args(error));
			printf("\tmessage: %s\n", ob_error_message(error));
			printf("\terror type: %d\n", ob_error_exception_type(error));
			ob_delete_error(error);
			exit(EXIT_FAILURE);
		}
	}
public:
	int width, height;
	Mat leftColor, rightColor, color;
	ob_pipeline* pipeline = nullptr;  // pipeline, used to open the color stream after connecting the device
	ob_device* device = nullptr;  // device, obtained through the pipeline, and the corresponding sensor can be obtained through the device
	~CameraOrb335L() {  }
	CameraOrb335L(int _width, int _height)
	{
		width = _width;
		height = _height;
		leftColor = Mat(Size(width, height), CV_8UC3);
		rightColor = Mat(Size(width, height), CV_8UC3);
		leftColor.setTo(0);
		rightColor.setTo(0);

		//std::shared_ptr<ob::Config> config = std::make_shared<ob::Config>();

		ob_error* error = NULL;     // Used to return SDK interface error information
		pipeline = ob_create_pipeline(&error);
		check_error(error);

		// Create config to configure the resolution, frame rate, and format of the color stream
		ob_config* config = ob_create_config(&error);
		check_error(error);

		// Configure the color stream
		ob_stream_profile* color_profile = nullptr;
		ob_stream_profile_list* profiles = ob_pipeline_get_stream_profile_list(pipeline, OB_SENSOR_COLOR, &error);
		if (error) {
			printf("Current device is not support color sensor!\n");
			exit(EXIT_FAILURE);
		}

		// Find the corresponding Profile according to the specified format, and choose the RGB888 format first
		color_profile = ob_stream_profile_list_get_video_stream_profile(profiles, 1280, 720, OB_FORMAT_BGR, 60, &error);
		// If the specified format is not found, search for the default Profile to open the stream
		if (error) {
			color_profile = ob_stream_profile_list_get_profile(profiles, OB_PROFILE_DEFAULT, &error);
			ob_delete_error(error);
			error = nullptr;
		}

		// enable stream
		ob_config_enable_stream(config, color_profile, &error);
		check_error(error);

		// Get Device through Pipeline
		device = ob_pipeline_get_device(pipeline, &error);
		check_error(error);

		// Start the pipeline with config
		ob_pipeline_start_with_config(pipeline, config, &error);
		check_error(error);

		// Create a rendering display window
		uint32_t width = ob_video_stream_profile_width(color_profile, &error);
		check_error(error);
		uint32_t height = ob_video_stream_profile_height(color_profile, &error);
		check_error(error);

	}

	bool waitForFrame()
	{
		ob_error* error = NULL;     // Used to return SDK interface error information
		ob_frame* frameset = nullptr;
		while (frameset == nullptr) {
			frameset = ob_pipeline_wait_for_frameset(pipeline, 100, &error);
			check_error(error);
			if (frameset != nullptr) {
				// Get the color frame from the frameset
				ob_frame* color_frame = ob_frameset_color_frame(frameset, &error);
				check_error(error);
				if (color_frame == nullptr) {
					ob_delete_frame(frameset, &error);
					check_error(error);
				}

				// Get the index of the frame
				auto index = ob_frame_index(color_frame, &error);
				check_error(error);
			}
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

extern "C" __declspec(dllexport) int* ORBWaitFrame(CameraOrb335L * cPtr, int w, int h) 
{ 
	if (cPtr->waitForFrame()) return 0;

	//// resize(tmp, cPtr->color, Size(w, h));  // can't get it to link with resize - Mynt is setup with 3.43 version of OpenCV.

	//cPtr->rightView = Mat(cPtr->height, cPtr->width, CV_8UC3, (int*) cPtr->right_color);
	//// resize(tmp, cPtr->rightView, Size(w, h));

	//// resize(cPtr->pcFullSize, cPtr->pointCloud, Size(w, h), INTER_NEAREST);
	//cPtr->pointCloud = cPtr->pcFullSize;
	return (int*)cPtr->color.data;
}
extern "C" __declspec(dllexport) int* ORBOpen(int width, int height) { return (int*) new CameraOrb335L(width, height);}
extern "C" __declspec(dllexport) void ORBClose(CameraOrb335L * cPtr) {  delete cPtr; }
//extern "C" __declspec(dllexport) int* ORBIntrinsicsLeft(CameraOrb335L * cPtr) { return (int*)&cPtr->intrinsicsBoth.left; }
//extern "C" __declspec(dllexport) int* ORBRightImage(CameraOrb335L * cPtr) {return (int*)cPtr->rightView.data;}
//extern "C" __declspec(dllexport) int* ORBPointCloud(CameraOrb335L * cPtr) { return (int*)cPtr->pointCloud.data; }
extern "C" __declspec(dllexport) int* ORBAcceleration(CameraOrb335L * cPtr){return (int*)&acceleration;}
extern "C" __declspec(dllexport) int* ORBGyro(CameraOrb335L * cPtr){return (int*)&gyro;}
extern "C" __declspec(dllexport) double ORBIMU_TimeStamp(CameraOrb335L * cPtr){return imuTimeStamp;}
#endif