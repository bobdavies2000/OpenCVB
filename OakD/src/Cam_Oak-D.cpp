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
	Mat rgb, leftView, rightView, depth16u;
	dai::CalibrationHandler deviceCalib;
	double imuTimeStamp;
	bool firstTs = false;
	int rows, cols;
	bool firstPass;
	std::shared_ptr<dai::Device> device;
	std::shared_ptr<dai::node::ColorCamera> camRgb;
	std::shared_ptr<dai::node::XLinkOut> xoutRgb;
	std::shared_ptr<dai::node::MonoCamera> monoLeft;
	std::shared_ptr<dai::node::MonoCamera> monoRight;
	std::shared_ptr<dai::node::StereoDepth> stereo;
	std::shared_ptr<dai::node::XLinkOut> xoutLeft;
	std::shared_ptr<dai::node::XLinkOut> xoutRight;
	std::shared_ptr<dai::node::XLinkOut> xoutDepth;
	std::shared_ptr<dai::node::XLinkOut> xoutRectifL;
	std::shared_ptr<dai::node::XLinkOut> xoutRectifR;
	std::shared_ptr<dai::node::IMU> imu;
	std::shared_ptr<dai::node::XLinkOut> xlinkOut;

	std::shared_ptr <dai::DataOutputQueue> qRgb;
	std::shared_ptr <dai::DataOutputQueue> leftQueue;
	std::shared_ptr <dai::DataOutputQueue> rightQueue;
	std::shared_ptr <dai::DataOutputQueue> depthQueue;
	std::shared_ptr <dai::DataOutputQueue> rectifLeftQueue;
	std::shared_ptr <dai::DataOutputQueue> rectifRightQueue;
	std::shared_ptr <dai::DataOutputQueue> imuQueue;
	std::chrono::time_point<std::chrono::steady_clock, std::chrono::steady_clock::duration> baseTs;

	dai::IMUReportAccelerometer acceleroValues;
	dai::IMUReportGyroscope gyroValues;

	~OakDCamera() {}
	OakDCamera(int _cols, int _rows)
	{
		firstPass = true;
		rows = _rows;
		cols = _cols;

		camRgb = pipeline.create<dai::node::ColorCamera>();
		xoutRgb = pipeline.create<dai::node::XLinkOut>();
		monoLeft = pipeline.create<dai::node::MonoCamera>();
		monoRight = pipeline.create<dai::node::MonoCamera>();
		stereo = pipeline.create<dai::node::StereoDepth>();
		xoutLeft = pipeline.create<dai::node::XLinkOut>();
		xoutRight = pipeline.create<dai::node::XLinkOut>();
		xoutDepth = pipeline.create<dai::node::XLinkOut>();
		xoutRectifL = pipeline.create<dai::node::XLinkOut>();
		xoutRectifR = pipeline.create<dai::node::XLinkOut>();
		imu = pipeline.create<dai::node::IMU>();
		xlinkOut = pipeline.create<dai::node::XLinkOut>();

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

		queueNames.push_back("rgb");
		queueNames.push_back("depth");
		queueNames.push_back("left");
		queueNames.push_back("right");
		queueNames.push_back("rectified_left");
		queueNames.push_back("rectified_right");

		//auto connectedCameras = device.getConnectedCameraFeatures();
		//for (const auto& cam : connectedCameras) {
		//	if (cam.sensorName == "COLOR") {
		//		camRgb->setBoardSocket(cam.socket);
		//		break;
		//	}
		//}
		camRgb->setBoardSocket(dai::CameraBoardSocket::CAM_A); 
		monoLeft->setResolution(dai::MonoCameraProperties::SensorResolution::THE_720_P);
		monoLeft->setBoardSocket(dai::CameraBoardSocket::CAM_B);
		monoRight->setResolution(dai::MonoCameraProperties::SensorResolution::THE_720_P);
		monoRight->setBoardSocket(dai::CameraBoardSocket::CAM_C);

		stereo->setRectifyEdgeFillColor(0);  // black, to better see the cutout
		stereo->initialConfig.setMedianFilter(dai::MedianFilter::KERNEL_7x7);
		stereo->setDefaultProfilePreset(dai::node::StereoDepth::PresetMode::HIGH_DENSITY);
		stereo->setLeftRightCheck(true); // LR-check is required for depth alignment
		stereo->setSubpixel(false);

		// added these
		stereo->initialConfig.setConfidenceThreshold(230);
		stereo->setDepthAlign(dai::CameraBoardSocket::CAM_A);

		// Linking
		monoLeft->out.link(stereo->left);
		monoRight->out.link(stereo->right);

		stereo->rectifiedLeft.link(xoutRectifL->input);
		stereo->rectifiedRight.link(xoutRectifR->input);
		stereo->depth.link(xoutDepth->input);
		stereo->syncedLeft.link(xoutLeft->input);
		stereo->syncedRight.link(xoutRight->input);

		device = std::make_shared<dai::Device>(pipeline);
		deviceCalib = device->readCalibration();

		imuQueue = device->getOutputQueue("imu", 50, false);
		baseTs = std::chrono::time_point<std::chrono::steady_clock, std::chrono::steady_clock::duration>();

		rgb = Mat(rows, cols, CV_8UC3);
		depth16u = Mat(rows, cols, CV_16UC1);
		leftView = Mat(rows, cols, CV_8UC1);
		rightView = Mat(rows, cols, CV_8UC1);
	}

	void waitForFrame()
	{
		std::unordered_map<std::string, std::shared_ptr<dai::ImgFrame>> latestPacket;

		using namespace std::chrono;

		qRgb = device->getOutputQueue("rgb", 4, false);
		leftQueue = device->getOutputQueue("left", 8, false);
		rightQueue = device->getOutputQueue("right", 8, false);
		depthQueue = device->getOutputQueue("depth", 8, false);
		rectifLeftQueue = device->getOutputQueue("rectified_left", 8, false);
		rectifRightQueue = device->getOutputQueue("rectified_right", 8, false);

		auto inRGB = qRgb->get<dai::ImgFrame>();
		auto inLeft = rectifLeftQueue->get<dai::ImgFrame>();
		auto inRight = rectifRightQueue->get<dai::ImgFrame>();

		auto queueEvents = device->getQueueEvents(queueNames);
		for (const auto& name : queueEvents) {
			auto packets = device->getOutputQueue(name)->tryGetAll<dai::ImgFrame>();
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
	}
};


extern "C" __declspec(dllexport)
int* OakDOpen(int w, int h)
{
	OakDCamera* cPtr = new OakDCamera(w, h);
	return (int*)cPtr;
}

extern "C" __declspec(dllexport)
int* OakDintrinsics(OakDCamera* cPtr, int camera)
{
	std::vector<std::vector<float>> intrin;
	if (camera == 1)
	{
		intrin = cPtr->deviceCalib.getCameraIntrinsics(dai::CameraBoardSocket::CAM_A, 1280, 720);
	}
	else {
		if (camera == 2)
			intrin = cPtr->deviceCalib.getCameraIntrinsics(dai::CameraBoardSocket::CAM_B, 1280, 720);
		else
			intrin = cPtr->deviceCalib.getCameraIntrinsics(dai::CameraBoardSocket::CAM_C, 1280, 720);
	}
	
	int i = 0;
	for (auto row : intrin)  for (auto val : row) cPtr->intrinsics[i++] = val;
	return (int*)&cPtr->intrinsics;
}



extern "C" __declspec(dllexport) int* OakDRawDepth(OakDCamera* cPtr)
{
	return (int*)cPtr->depth16u.data;
}



//http://graphics.cs.cmu.edu/courses/15-463/2017_fall/lectures/lecture19.pdf
extern "C" __declspec(dllexport) int* OakDPointCloud(OakDCamera* cPtr) { return 0; }
extern "C" __declspec(dllexport) double OakDIMUTimeStamp(OakDCamera* cPtr) { return cPtr->imuTimeStamp; }
extern "C" __declspec(dllexport) int* OakDGyro(OakDCamera* cPtr) { return (int*)&cPtr->gyroValues.x; }
extern "C" __declspec(dllexport) int* OakDAccel(OakDCamera* cPtr) { return (int*)&cPtr->acceleroValues.x; }
extern "C" __declspec(dllexport) int* OakDColor(OakDCamera* cPtr) { return (int*)cPtr->rgb.data; }
extern "C" __declspec(dllexport) void OakDWaitForFrame(OakDCamera* cPtr) { cPtr->waitForFrame(); }
extern "C" __declspec(dllexport) void OakDStop(OakDCamera* cPtr)
{
	cPtr->device->close();
	if (cPtr != 0) delete cPtr;
}
extern "C" __declspec(dllexport) int* OakDLeftImage(OakDCamera* cPtr) { return (int*)cPtr->leftView.data; }
extern "C" __declspec(dllexport) int* OakDRightImage(OakDCamera* cPtr) { return (int*)cPtr->rightView.data; }
