class CPP_Options_Blur {
public:
	int kernelSize = 3;
	float sigma = 1.5;
	CPP_Options_Blur() {
	}
	void RunVB() {}
};




class CPP_Options_Annealing {
public:
	CPP_Random_Basics* random;
	int cityCount = 25;
	bool copyBestFlag = false;
	bool circularFlag = true;
	int successCount = 8;
	CPP_Options_Annealing() {
		//	traceName = "CPP_Options_Annealing";
		random = new CPP_Random_Basics();
		//        random->Run(empty);  
		//        if (sliders.Setup(traceName)) {
		//            sliders.setupTrackBar("Anneal Number of Cities", 5, 500, cityCount);  
		//            sliders.setupTrackBar("Success = top X threads agree on energy level.", 2, thread::hardware_concurrency(), successCount);  
		//        }
		//        if (check.Setup(traceName)) {
		//            check.addCheckBox("Restart Traveling Salesman");
		//            check.addCheckBox("Copy Best Intermediate solutions (top half) to Bottom Half");
		//            check.addCheckBox("Circular pattern of cities (allows you to visually check if successful.)");
		//            check.Box(0).Checked = true;
		//            check.Box(2).Checked = true;
		//        }
	}
	void RunVB() {
		//        static Trackbar* citySlider = findSlider("Anneal Number of Cities");
		//        static Trackbar* successSlider = findSlider("Success = top X threads agree on energy level.");
		//        static CheckBox* travelCheck = findCheckBox("Restart Traveling Salesman");
		//        static CheckBox* copyBestCheck = findCheckBox("Copy Best Intermediate solutions (top half) to Bottom Half");
		//        static CheckBox* circularCheck = findCheckBox("Circular pattern of cities (allows you to visually check if successful.)");
		//        copyBestFlag = copyBestCheck->checked;
		//        circularFlag = circularCheck->checked;
		//        cityCount = citySlider->value;  
		//        successCount = successSlider->value;  
		//        travelCheck->checked = false;
	}
};




class CPP_Options_Extrinsics {
public:
	int leftCorner;
	int rightCorner;
	int topCorner;
	CPP_Options_Extrinsics() {
		int leftVal = 15, rightVal = 15, topBotVal = 15;
		leftCorner = task->workingRes.width * leftVal;
		rightCorner = task->workingRes.width * rightVal;
		topCorner = task->workingRes.width * topBotVal;
	}
	void RunVB() {
	}
};





class CPP_Options_FeatureMatch {
public:
	int matchOption;
	string matchText;
	int featurePoints;
	float correlationThreshold;
	int matchCellSize;
	CPP_Options_FeatureMatch() {
	}
	void RunVB() {
	}
};





class CPP_Options_Features {
public:
	bool useBRISK;
	double quality;
	double minDistance;
	cv::Rect roi;
	int distanceThreshold;
	int matchOption;
	string matchText;
	CPP_Options_FeatureMatch fOptions;
	CPP_Options_Features() {
	}
	void RunVB() {}
};







class CPP_Options_Bernson {
public:
	int kernelSize = 51;
	int bgThreshold = 100;
	int contrastMin = 50;
	CPP_Options_Bernson() {
	}
	void RunVB() {}
};




class CPP_Options_BGSubtractSynthetic {
public:
	double amplitude = 200;
	double magnitude = 20;
	double waveSpeed = 20;
	double objectSpeed = 15;
	CPP_Options_BGSubtractSynthetic() {
	}
	void RunVB() {}
};

class CPP_Options_BGSubtract {
public:
	float MOGlearnRate = 1 / 1000;
	CPP_Options_BGSubtract() {
	}
	void RunVB() {}
};




class CPP_Options_BinarizeNiBlack {
public:
	int kernelSize = 51;
	float niBlackK = -200 / 1000;
	float nickK = 100 / 1000;
	float sauvolaK = 100 / 1000;
	float sauvolaR = 64;
	CPP_Options_BinarizeNiBlack() {
	}
	void RunVB() {}
};




class CPP_Options_Blob {
public:
	cv::SimpleBlobDetector::Params blobParams;
	CPP_Options_Blob() {
	}
	void RunVB() {}
};



class CPP_Options_BlockMatching {
public:
	int numDisparity = 2 * 16;
	int blockSize = 15;
	int distance = 20;
	CPP_Options_BlockMatching() {
	}
	void RunVB() {}
};



class CPP_Options_Boundary {
public:
	int desiredBoundaries = 15;
	int peakDistance = task->workingRes.width / 20;
	CPP_Options_Boundary() {
	}
	void RunVB() {}
};




class CPP_Options_CamShift {
public:
	int camMax = 255;
	Scalar camSBins = Scalar(0, 40, 32);
	CPP_Options_CamShift() {
	}
	void RunVB() {}
};




class CPP_Options_Canny {
public:
	int threshold1 = 100;
	int threshold2 = 150;
	int aperture = 3;
	CPP_Options_Canny() {
	}
	void RunVB() {}
};





class CPP_Options_Cartoonify {
public:
	int medianBlur = 7;
	int medianBlur2 = 3;
	int kernelSize = 5;
	int threshold = 80;
	CPP_Options_Cartoonify() {
	}
	void RunVB() {}
};




class CPP_Options_CComp {
public:
	int light = 127;
	int dark = 50;
	CPP_Options_CComp() {
	}
	void RunVB() {}
};




class CPP_Options_Classifier {
public:
	String classifierName;
	CPP_Options_Classifier() {
	}
	void RunVB() {}
};




class CPP_Options_ColorFormat {
public:
	String colorFormat;
	CPP_Options_ColorFormat() {
	}
	void RunVB() {}
};




class CPP_Options_ColorMatch {
public:
	bool maxDistanceCheck;
	CPP_Options_ColorMatch() {
	}
	void RunVB() {}
};




class CPP_Options_Colors {
public:
	int red;
	int green;
	int blue;
	CPP_Options_Colors() {
	}
	void RunVB() {}
};




class CPP_Options_Complexity {
public:
	Scalar plotColor = Scalar(0, 255, 255);
	CPP_Options_Complexity() {
	}
	void RunVB() {}
};




class CPP_Options_Contours {
public:
	cv::RetrievalModes retrievalMode = cv::RetrievalModes::RETR_EXTERNAL;
	cv::ContourApproximationModes ApproximationMode = cv::ContourApproximationModes::CHAIN_APPROX_TC89_KCOS;
	float epsilon = 3 / 100;
	CPP_Options_Contours() {
	}
	void RunVB() {}
};





class CPP_Options_Contours2 {
public:
	cv::ContourApproximationModes ApproximationMode = cv::ContourApproximationModes::CHAIN_APPROX_TC89_KCOS;
	CPP_Options_Contours2() {
	}
	void RunVB() {}
};





class CPP_Options_DCT {
public:
	int runLengthMin = 15;
	int removeFrequency = 1;
	CPP_Options_DCT() {
	}
	void RunVB() {}
};



class CPP_Options_Denoise {
public:
	bool removeSinglePixels;
	CPP_Options_Denoise() {
	}
	void RunVB() {}
};



class CPP_Options_DFT {
public:
	int radius = task->workingRes.width;
	int order = 2;
	CPP_Options_DFT() {
	}
	void RunVB() {}
};





class CPP_Options_Dilate {
public:
	int kernelSize = 3;
	int iterations = 1;
	cv::MorphShapes morphShape;
	cv::Mat element;
	bool noshape;
	CPP_Options_Dilate() {
	}
	void RunVB() {}
};





class CPP_Options_Distance {
public:
	cv::DistanceTypes distanceType = cv::DistanceTypes::DIST_L1;
	CPP_Options_Distance() {
	}
	void RunVB() {}
};





class CPP_Options_Dither {
public:
	int radioIndex;
	int bppIndex = 1;
	CPP_Options_Dither() {
	}
	void RunVB() {}
};




class CPP_Options_DNN {
public:
	String superResModelFileName;
	String shortModelName;
	int superResMultiplier;
	CPP_Options_DNN() {
	}
	void RunVB() {}
};






class CPP_Options_Draw {
public:
	int drawCount = 3;
	int drawFilled = 2;
	bool drawRotated = false;
	CPP_Options_Draw() {
	}
	void RunVB() {}
};



class CPP_Options_DrawArc {
public:
	int saveMargin = task->workingRes.width / 16;
	bool drawFull;
	bool drawFill;
	CPP_Options_DrawArc() {
	}
	void RunVB() {}
};





class CPP_Options_Eigen {
public:
	bool highlight;
	bool recompute;
	int randomCount = 100;
	int linePointCount = 20;
	int noiseOffset = 10;
	CPP_Options_Eigen() {
	}
	void RunVB() {}
};






class CPP_Options_Encode {
public:
	int qualityLevel = 1;
	int scalingLevel = 85;
	cv::ImwriteFlags encodeOption = cv::ImwriteFlags::IMWRITE_JPEG_PROGRESSIVE;
	CPP_Options_Encode() {
	}
	void RunVB() {}
};





class CPP_Options_Erode {
public:
	int kernelSize = 3;
	int iterations = 1;
	cv::MorphShapes morphShape = cv::MorphShapes::MORPH_CROSS;
	cv::Mat element;
	bool noshape;
	CPP_Options_Erode() {
	}
	void RunVB() {}
};





class CPP_Options_Filter {
public:
	int kernelSize = 3;
	CPP_Options_Filter() {
	}
	void RunVB() {}
};




class CPP_Options_FilterNorm {
public:
	cv::Mat kernel;
	CPP_Options_FilterNorm() {
	}
	void RunVB() {
		float defaults[21] = { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
		kernel = Mat(1, 21, CV_32FC1, defaults);
	}
};





class CPP_Options_FitLine {
public:
	int radiusAccuracy = 10;
	int angleAccuracy = 10;
	CPP_Options_FitLine() {
	}
	void RunVB() {}
};





class CPP_Options_Flood {
public:
	cv::FloodFillFlags floodFlag;
	int stepSize = 30;
	CPP_Options_Flood() {
		floodFlag = cv::FloodFillFlags::FLOODFILL_FIXED_RANGE;
	}
	void RunVB() {}
};





class CPP_Options_ForeGround {
public:
	float maxForegroundDepthInMeters = 1500 / 1000;
	int minSizeContour = 100;
	float depthPerRegion;
	int numberOfRegions = 5;
	CPP_Options_ForeGround() {
	}
	void RunVB() {}
};





class CPP_Options_FPoly {
public:
	int removeThreshold = 4;
	int autoResyncAfterX = 500;
	CPP_Options_FPoly() {
	}
	void RunVB() {}
};





class CPP_Options_Fractal {
public:
	int iterations = 34;
	bool resetCheck = false;
	CPP_Options_Fractal() {
	}
	void RunVB() {}
};





class CPP_Options_GeneticDrawing {
public:
	int stageTotal = 100;
	float brushPercent = 1.0;
	int strokeCount = 10;
	bool snapCheck = false;
	int generations = 20;
	CPP_Options_GeneticDrawing() {
	}
	void RunVB() {}
};




class CPP_Options_Harris {
public:
	float threshold = 1 / 10000;
	int neighborhood = 21;
	int aperture = 21;
	float harrisParm = 1;
	CPP_Options_Harris() {
	}
	void RunVB() {}
};





class CPP_Options_HeatMap {
public:
	int redThreshold = 20;
	String viewName = "vertical";
	bool showHistory;
	bool topView = true;
	bool sideView;
	CPP_Options_HeatMap() {
	}
	void RunVB() {}
};




class CPP_Options_HistCompare {
public:
	cv::HistCompMethods compareMethod;
	String compareName;
	CPP_Options_HistCompare() {
		compareMethod = cv::HistCompMethods::HISTCMP_CORREL;
	}
	void RunVB() {}
};






class CPP_Options_HistXD {
public:
	int sideThreshold = 5;
	int topThreshold = 15;
	int threshold3D = 40;
	int selectedBin = 0;
	CPP_Options_HistXD() {
	}
	void RunVB() {}
};






class CPP_Options_Hough {
public:
	int rho = 1;
	float theta = float(1000 * CV_PI / 180);
	int threshold = 3;
	int lineCount = 25;
	float relativeIntensity = 90 / 1000;
	CPP_Options_Hough() {
	}
	void RunVB() {}
};





class CPP_Options_IMUFrameTime {
public:
	int minDelayIMU = 4;
	int minDelayHost = 4;
	int plotLastX = 20;
	CPP_Options_IMUFrameTime() {
	}
	void RunVB() {}
};





class CPP_Options_InPaint {
public:
	bool telea = false;
	CPP_Options_InPaint() {
	}
	void RunVB() {}
};





class CPP_Options_Intercepts {
public:
	int interceptRange = 10;
	int mouseMovePoint;
	int selectedIntercept;
	CPP_Options_Intercepts() {
	}
	void RunVB() {}
};





class CPP_Options_Interpolate {
public:
	int resizePercent = 2;
	int interpolationThreshold = 4;
	int pixelCountThreshold = 0;
	int saveDefaultThreshold = resizePercent;
	CPP_Options_Interpolate() {
	}
	void RunVB() {}
};





class CPP_Options_KLT {
public:
	cv::Point2f* inputPoints;
	int maxCorners = 100;
	float qualityLevel = 0.01f;
	int minDistance = 7;
	int blockSize = 7;
	bool nightMode;
	CPP_Options_KLT() {
	}
	void RunVB() {}
};






class CPP_Options_KMeans {
public:
	cv::KmeansFlags kMeansFlag;
	int kMeansK = 5;
	CPP_Options_KMeans() {
	}
	void RunVB() {}
};





class CPP_Options_Laplacian {
public:
	float scale = 1;
	float delta = 0;
	bool gaussianBlur;
	bool boxFilterBlur;
	int threshold = 15;
	CPP_Options_Laplacian() {
	}
	void RunVB() {}
};






class CPP_Options_LeftRight {
public:
	float alpha = 2000;
	int beta = -100;
	CPP_Options_LeftRight() {
	}
	void RunVB() {}
};





class CPP_Options_Line {
public:
	int lineLengthThreshold = 20;
	CPP_Options_Line() {
	}
	void RunVB() {}
};






class CPP_Options_MatchCell {
public:
	float overlapPercent = 0.5f;
	CPP_Options_MatchCell() {
	}
	void RunVB() {}
};






class CPP_Options_MatchShapes {
public:
	cv::ShapeMatchModes matchOption;
	float matchThreshold = 0.8f;
	float maxYdelta = 0.05f;
	float minSize = (task->workingRes.width * task->workingRes.height) / 100;
	CPP_Options_MatchShapes() {
		matchOption = cv::ShapeMatchModes::CONTOURS_MATCH_I1;
	}
	void RunVB() {}
};





class CPP_Options_MinArea {
public:
	int squareWidth = 100;
	int numPoints = 5;
	CPP_Options_MinArea() {
	}
	void RunVB() {}
};





class CPP_Options_MinMaxNone {
public:
	bool useMax;
	bool useMin;
	bool useNone;
	CPP_Options_MinMaxNone() {
	}
	void RunVB() {}
};





class CPP_Options_Motion {
public:
	int motionThreshold;
	float cumulativePercentThreshold = 0.1f;
	CPP_Options_Motion() {
	}
	void RunVB() {}
};





class CPP_Options_MotionBlur {
public:
	bool showDirection = true;
	int kernelSize = 51;
	float theta;
	int restoreLen = 10;
	int SNR = 700;
	int gamma = 5;
	CPP_Options_MotionBlur() {
	}
	void RunVB() {}
};





class CPP_Options_MSER {
public:
	int delta = 9;
	int minArea = 0;
	int maxArea = 0;
	float maxVariation = 0.25f;
	float minDiversity = 0.2f;
	int maxEvolution = 200;
	float areaThreshold = 1.01f;
	float minMargin = 0.003f;
	int edgeBlurSize = 5;
	bool pass2Setting = false;
	bool graySetting = false;
	CPP_Options_MSER() {
	}
	void RunVB() {}
};





class CPP_Options_Neighbors {
public:
	float threshold = 0.005f;
	int pixels = 6;
	bool patchZ;
	CPP_Options_Neighbors() {
	}
	void RunVB() {}
};





class CPP_Options_OilPaint {
public:
	int kernelSize = 4;
	int intensity = 20;
	int threshold = 25;
	int filterSize = 3;
	CPP_Options_OilPaint() {
	}
	void RunVB() {}
};





class CPP_Options_OpenGL {
public:
	float FOV = 75;
	float yaw = -3;
	float pitch = 3;
	float roll = 0;
	float zNear = 0;
	float zFar = 20;
	int pointSize = 2;
	float zTrans = 0.5f;
	CPP_Options_OpenGL() {
	}
	void RunVB() {}
};







class CPP_Options_OpenGL_Contours {
public:
	int depthPointStyle;
	float filterThreshold = 0.3f;
	CPP_Options_OpenGL_Contours() {
	}
	void RunVB() {}
};





class CPP_Options_OpenGLFunctions {
public:
	cv::Point3f moveAmount;
	float FOV = 75;
	float yaw = -3;
	float pitch = 3;
	float roll = 0;
	float zNear = 0;
	float zFar = 20.0f;
	float zTrans = 0.5f;
	CPP_Options_OpenGLFunctions() {
	}
	void RunVB() {}
};





class CPP_Options_Photoshop {
public:
	int switchVal = 0;
	CPP_Options_Photoshop() {
	}
	void RunVB() {}
};





class CPP_Options_Plane {
public:
	float rmsThreshold = 0.1f;
	bool useMaskPoints = false;
	bool useContourPoints = false;
	bool use3Points = false;
	bool reuseRawDepthData = false;
	CPP_Options_Plane() {
	}
	void RunVB() {}
};




class CPP_Options_PlaneEstimation {
public:
	bool useDiagonalLines = false;
	bool useContour_SidePoints = false;
	CPP_Options_PlaneEstimation() {
	}
	void RunVB() {}
};





class CPP_Options_Pointilism {
public:
	int smoothingRadius = 32 * 2 + 1;
	int strokeSize = 3;
	bool useElliptical = false;
	CPP_Options_Pointilism() {
	}
	void RunVB() {}
};




class CPP_Options_ProCon {
public:
    int buffer[10];
	int pduration = 1;
	int cduration = 1;
	int bufferSize = 10;
	CPP_Options_ProCon() {
	}
	void RunVB() {}
};






class CPP_Options_Resize {
public:
	cv::InterpolationFlags warpFlag = cv::InterpolationFlags::INTER_NEAREST;
	int radioIndex;
	float resizePercent = 0.03f;
	int topLeftOffset = 10;
	CPP_Options_Resize() {
	}
	void RunVB() {}
};




class CPP_Options_SepFilter2D {
public:
	int xDim = 5;
	int yDim = 11;
	float sigma = 17;
	bool diffCheck = false;
	CPP_Options_SepFilter2D() {
	}
	void RunVB() {}
};





class CPP_Options_Sift {
public:
	bool useBFMatcher = false;
	int pointCount = 200;
	CPP_Options_Sift() {
	}
	void RunVB() {}
};





class CPP_Options_Smoothing {
public:
	int iterations = 8;
	float interiorTension = 0.5f;
	int stepSize = 30;
	CPP_Options_Smoothing() {
	}
	void RunVB() {}
};





class CPP_Options_Sobel {
public:
	int kernelSize = 3;
	int threshold = 50;
	bool horizontalDerivative = true;
	bool verticalDerivative = true;
	CPP_Options_Sobel() {
	}
	void RunVB() {}
};




class CPP_Options_SOM {
public:
	int iterations = 3000;
	float learningRate = 0.1f;
	int radius = 15;
	CPP_Options_SOM() {
	}
	void RunVB() {}
};





class CPP_Options_Spectrum {
public:
	int gapDepth = 1;
	int gapGray = 1;
	int sampleThreshold = 10;
	CPP_Options_Spectrum() {
	}
	void RunVB() {}
};





class CPP_Options_Structured {
public:
	int sliceSize = 1;
	int stepSize = 20;
	CPP_Options_Structured() {
	}
	void RunVB() {}
};





class CPP_Options_SuperRes {
public:
	String method = "farneback";
	int iterations = 10;
	bool restartWithNewOptions = false;
	CPP_Options_SuperRes() {
	}
	void RunVB() {}
	};





class CPP_Options_SVM {
public:
	cv::ml::SVM::KernelTypes kernelType;
	int granularity = 5;
	float svmDegree = 1;
	int gamma = 1;
	float svmCoef0 = 1;
	float svmC = 1;
	float svmNu = 0.5f;
	float svmP = 0;
	int sampleCount = 500;
	CPP_Options_SVM() {
		kernelType = cv::ml::SVM::KernelTypes::POLY;
	}
	void RunVB() {}
};





class CPP_Options_SVM2 {
public:
	cv::ml::SVM::KernelTypes SVMType;
	CPP_Options_SVM2() {
		SVMType = cv::ml::SVM::KernelTypes::CHI2;
	}
	void RunVB() {}
};





class CPP_Options_SymmetricalShapes {
public:
	float rotateAngle = 0;
	cv::Scalar fillColor = cv::Scalar(0, 0, 255);
	int numPoints = 500;
	int nGenPer = 100;
	int radius1 = task->workingRes.height / 4;
	int radius2 = task->workingRes.height / 8;
	float dTheta = 2 * CV_PI / numPoints;
	bool symmetricRipple = true;
	bool reverseInOut = false;
	bool fillRequest = true;
	CPP_Options_SymmetricalShapes() {
	}
	void RunVB() {}
};





class CPP_Options_Threshold {
public:
	cv::ThresholdTypes thresholdOption;
	int maxVal = 255;
	int threshold = 100;
	bool inputGray = false;
	bool otsuOption = true;
	CPP_Options_Threshold() {
	}
	void RunVB() {}
};





class CPP_Options_Threshold_Adaptive {
public:
	cv::AdaptiveThresholdTypes method;
	int blockSize = 5;
	int constantVal = 0;
	CPP_Options_Threshold_Adaptive() {
	}
	void RunVB() {}
};




class CPP_Options_Warp {
public:
	double alpha = 0.9;
	double beta = 0.9;
	double gamma = 0.9;
	double f = 600;
	double distance = 400;
	CPP_Options_Warp() {
	}
	void RunVB() {}
};





class CPP_Options_Wavelet {
public:
	bool useHaar = false;
	int iterations = 3;
	CPP_Options_Wavelet() {
	}
	void RunVB() {}
};





class CPP_Options_XPhoto {
public:
	cv::ColorConversionCodes colorCode;
	int dynamicRatio;
	int blockSize;
	CPP_Options_XPhoto() {
		colorCode = cv::ColorConversionCodes::COLOR_BGR2GRAY;
	}
	void RunVB() {}
};