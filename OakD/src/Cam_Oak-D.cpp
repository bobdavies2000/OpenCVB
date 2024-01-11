#include "depthai/depthai.hpp"
 
using namespace std;
using namespace cv;

// The disparity is computed at this resolution, then upscaled to BGR resolution
static constexpr auto monoRes = dai::MonoCameraProperties::SensorResolution::THE_400_P;

class OakDCamera
{
private:
public:
	dai::Pipeline pipeline;
	std::vector<std::string> queueNames;
	std::unordered_map<std::string, Mat> frame;
	std::unordered_map<std::string, std::shared_ptr<dai::ImgFrame>> latestPacket;
	float intrinsics[9];
	Mat depth16u;
	Mat rgb;
	Mat leftView;
	Mat rightView;
	dai::CalibrationHandler deviceCalib;
	double imuTimeStamp;
	bool firstTs = false;
	int rows, cols;
	bool firstPass;

	dai::IMUReportAccelerometer acceleroValues;
	dai::IMUReportGyroscope gyroValues;
	
	~OakDCamera(){}
	OakDCamera(int _cols, int _rows) 
	{
		firstPass = true;
		rows = _rows;
		cols = _cols;
	}

	void waitForFrame(bool close)
	{
		if (firstPass)
		{
			rgb = Mat(rows, cols, CV_8UC3);
			depth16u = Mat(rows, cols, CV_16UC1);
			leftView = Mat(rows, cols, CV_8UC1);
			rightView = Mat(rows, cols, CV_8UC1);

			static auto camRgb = pipeline.create<dai::node::ColorCamera>();
			static auto xoutRgb = pipeline.create<dai::node::XLinkOut>();
			static auto monoLeft = pipeline.create<dai::node::MonoCamera>();
			static auto monoRight = pipeline.create<dai::node::MonoCamera>();
			static auto stereo = pipeline.create<dai::node::StereoDepth>();
			static auto xoutLeft = pipeline.create<dai::node::XLinkOut>();
			static auto xoutRight = pipeline.create<dai::node::XLinkOut>();
			static auto xoutDepth = pipeline.create<dai::node::XLinkOut>();
			static auto xoutRectifL = pipeline.create<dai::node::XLinkOut>();
			static auto xoutRectifR = pipeline.create<dai::node::XLinkOut>();
			static auto imu = pipeline.create<dai::node::IMU>();
			static auto xlinkOut = pipeline.create<dai::node::XLinkOut>();

			xlinkOut->setStreamName("imu");
			// enable ACCELEROMETER_RAW at 500 hz rate
			imu->enableIMUSensor(dai::IMUSensor::ACCELEROMETER_RAW, 500);
			// enable GYROSCOPE_RAW at 400 hz rate
			imu->enableIMUSensor(dai::IMUSensor::GYROSCOPE_RAW, 400);
			// it's recommended to set both setBatchReportThreshold and setMaxBatchReports to 20 when integrating in a pipeline with a lot of input/output connections
			// above this threshold packets will be sent in batch of X, if the host is not blocked and USB bandwidth is available
			imu->setBatchReportThreshold(1);
			// maximum number of IMU packets in a batch, if it's reached device will block sending until host can receive it
			// if lower or equal to batchReportThreshold then the sending is always blocking on device
			// useful to reduce device's CPU load  and number of lost packets, if CPU load is high on device side due to multiple nodes
			imu->setMaxBatchReports(1);
			// Link plugins IMU -> XLINK
			imu->out.link(xlinkOut->input);

			camRgb->setInterleaved(false);
			camRgb->setColorOrder(dai::ColorCameraProperties::ColorOrder::RGB);
			camRgb->setResolution(dai::ColorCameraProperties::SensorResolution::THE_1080_P);
			camRgb->setBoardSocket(dai::CameraBoardSocket::RGB);
			camRgb->isp.link(xoutRgb->input);
			// If the Oak-D camera is stopped and then restarted, it will fail here.  Need to find out why.  
			camRgb->setPreviewSize(cols, rows);
			//camRgb->initialControl.setManualFocus(135);

			// added these 
			camRgb->setFps(60);
			camRgb->setIspScale(2, 3);

			// XLinkOut
			xoutRgb->setStreamName("rgb");
			xoutLeft->setStreamName("left");
			xoutRight->setStreamName("right");
			xoutDepth->setStreamName("depth");
			xoutRectifL->setStreamName("rectified_left");
			xoutRectifR->setStreamName("rectified_right");
			//xlinkImu->setStreamName("imu");

			queueNames.push_back("rgb");
			queueNames.push_back("depth");
			queueNames.push_back("left");
			queueNames.push_back("right");
			queueNames.push_back("rectified_left");
			queueNames.push_back("rectified_right");
			//queueNames.push_back("imu");

			// Properties
			monoLeft->setResolution(dai::MonoCameraProperties::SensorResolution::THE_720_P);
			monoLeft->setBoardSocket(dai::CameraBoardSocket::LEFT);
			monoRight->setResolution(dai::MonoCameraProperties::SensorResolution::THE_720_P);
			monoRight->setBoardSocket(dai::CameraBoardSocket::RIGHT);

			stereo->setRectifyEdgeFillColor(0);  // black, to better see the cutout
			stereo->initialConfig.setMedianFilter(dai::MedianFilter::KERNEL_7x7);
			stereo->setDefaultProfilePreset(dai::node::StereoDepth::PresetMode::HIGH_DENSITY);
			stereo->setLeftRightCheck(true); // LR-check is required for depth alignment
			stereo->setSubpixel(false);

			// added these
			stereo->initialConfig.setConfidenceThreshold(230);
			stereo->setDepthAlign(dai::CameraBoardSocket::RGB);

			// Linking
			monoLeft->out.link(stereo->left);
			monoRight->out.link(stereo->right);

			stereo->rectifiedLeft.link(xoutRectifL->input);
			stereo->rectifiedRight.link(xoutRectifR->input);
			stereo->depth.link(xoutDepth->input);
			stereo->syncedLeft.link(xoutLeft->input);
			stereo->syncedRight.link(xoutRight->input);
		}

		// Connect to device and start pipeline
		static dai::Device device(pipeline);

		if (firstPass)
		{
			firstPass = false;
			deviceCalib = device.readCalibration();
		}
		std::unordered_map<std::string, std::shared_ptr<dai::ImgFrame>> latestPacket;

		using namespace std::chrono;

		static auto qRgb = device.getOutputQueue("rgb", 4, false);
		auto inRGB = qRgb->get<dai::ImgFrame>();
		static auto leftQueue = device.getOutputQueue("left", 8, false);
		static auto rightQueue = device.getOutputQueue("right", 8, false);
		static auto depthQueue = device.getOutputQueue("depth", 8, false);
		static auto rectifLeftQueue = device.getOutputQueue("rectified_left", 8, false);
		static auto rectifRightQueue = device.getOutputQueue("rectified_right", 8, false);
		auto inLeft = rectifLeftQueue->get<dai::ImgFrame>();
		auto inRight = rectifRightQueue->get<dai::ImgFrame>();
		static auto imuQueue = device.getOutputQueue("imu", 50, false);
		static auto baseTs = std::chrono::time_point<std::chrono::steady_clock, std::chrono::steady_clock::duration>();

		// this is for the shutdown process only...
		if (close) { device.close();  return; }

		auto queueEvents = device.getQueueEvents(queueNames);
		for (const auto& name : queueEvents) {
			auto packets = device.getOutputQueue(name)->tryGetAll<dai::ImgFrame>();
			auto count = packets.size();
			if (count > 0) latestPacket[name] = packets[count - 1];
		}

		auto beforeTime = std::chrono::time_point_cast<std::chrono::milliseconds>(std::chrono::system_clock::now());
		auto depth = depthQueue->get<dai::ImgFrame>();
		depth16u = depth->getCvFrame();

		rgb = inRGB->getCvFrame();
		resize(inLeft->getCvFrame(), leftView, rgb.size());
		resize(inRight->getCvFrame(), rightView, rgb.size());

		auto imuData = imuQueue->get<dai::IMUData>();

		auto imuPackets = imuData->packets;
		for (auto& imuPacket : imuPackets) {
			acceleroValues = imuPacket.acceleroMeter;
			gyroValues = imuPacket.gyroscope; 

			auto acceleroTs1 = acceleroValues.getTimestampDevice();
			auto gyroTs1 = gyroValues.getTimestampDevice();
			if (!firstTs) {
				baseTs = std::min(acceleroTs1, gyroTs1);
				firstTs = true;
			}

			auto acceleroTs = acceleroTs1 - baseTs;
			auto gyroTs = gyroTs1 - baseTs;
		}

		// this is for the shutdown process only...
		if (close) { device.close();  return; }
	}
};


extern "C" __declspec(dllexport)
int *OakDOpen(int w, int h)
{
	OakDCamera* cPtr = new OakDCamera(w, h);
	return (int *)cPtr;
}

extern "C" __declspec(dllexport)
int* OakDintrinsics(OakDCamera * cPtr)
{
	std::vector<std::vector<float>> intrin = cPtr->deviceCalib.getCameraIntrinsics(dai::CameraBoardSocket::RGB, 1280, 720);
	int i = 0;
	for (auto row : intrin)  for (auto val : row) cPtr->intrinsics[i++] = val;
	return (int *)&cPtr->intrinsics;
}



extern "C" __declspec(dllexport) int* OakDRawDepth(OakDCamera * cPtr) 
{
	return (int*)cPtr->depth16u.data; 
}



//http://graphics.cs.cmu.edu/courses/15-463/2017_fall/lectures/lecture19.pdf
extern "C" __declspec(dllexport) int* OakDPointCloud(OakDCamera * cPtr) { return 0; }
extern "C" __declspec(dllexport) double OakDIMUTimeStamp(OakDCamera * cPtr) { return cPtr->imuTimeStamp;}
extern "C" __declspec(dllexport) int* OakDGyro(OakDCamera * cPtr) { return (int *)&cPtr->gyroValues.x; }
extern "C" __declspec(dllexport) int* OakDAccel(OakDCamera * cPtr) { return (int*)&cPtr->acceleroValues.x;}
extern "C" __declspec(dllexport) int* OakDColor(OakDCamera * cPtr) { return (int*)cPtr->rgb.data; }
extern "C" __declspec(dllexport) void OakDWaitForFrame(OakDCamera * cPtr) { cPtr->waitForFrame(false);}
extern "C" __declspec(dllexport) void OakDStop(OakDCamera * cPtr) 
{ 
	cPtr->waitForFrame(true); 
	if (cPtr != 0) delete cPtr; 
}
extern "C" __declspec(dllexport) int* OakDLeftImage(OakDCamera * cPtr) { return (int*)cPtr->leftView.data; }
extern "C" __declspec(dllexport) int* OakDRightImage(OakDCamera * cPtr) { return (int*)cPtr->rightView.data; }