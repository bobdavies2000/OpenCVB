class Options_Blur {
public:
	int kernelSize = 3;
	float sigma = 1.5;
	Options_Blur() {
	}
	void RunVB() {}
};




class Options_Annealing {
public:
	Random_Basics_CPP* random;
	int cityCount = 25;
	bool copyBestFlag = false;
	bool circularFlag = true;
	int successCount = 8;
	Options_Annealing() {
		random = new Random_Basics_CPP();
	}
	void RunVB() {
	}
};




class Options_Extrinsics {
public:
	int leftCorner;
	int rightCorner;
	int topCorner;
	Options_Extrinsics() {
		int leftVal = 15, rightVal = 15, topBotVal = 15;
		leftCorner = task->WorkingRes.width * leftVal;
		rightCorner = task->WorkingRes.width * rightVal;
		topCorner = task->WorkingRes.width * topBotVal;
	}
	void RunVB() {
	}
};





class Options_FeatureMatch {
public:
	cv::TemplateMatchModes matchOption = cv::TemplateMatchModes::TM_CCOEFF_NORMED;
	string matchText = "";
	int featurePoints = 200;
	float correlationThreshold = 0.9f;
	int matchCellSize = 10;
	Options_FeatureMatch() {
	}
	void RunVB() {
	}
};





class Options_Features {
public:
	bool useBRISK = false;
	double quality = 0.01;
	double minDistance = 10;
	cv::Rect roi;
	int distanceThreshold = 16;
	cv::TemplateMatchModes matchOption = cv::TemplateMatchModes::TM_CCOEFF_NORMED;
	string matchText = "";
	Options_FeatureMatch fOptions;
	Options_Features() {
	}
	void RunVB() {}
};







class Options_Bernson {
public:
	int kernelSize = 51;
	int bgThreshold = 100;
	int contrastMin = 50;
	Options_Bernson() {
	}
	void RunVB() {}
};




class Options_BGSubtractSynthetic {
public:
	double amplitude = 200;
	double magnitude = 20;
	double waveSpeed = 20;
	double objectSpeed = 15;
	Options_BGSubtractSynthetic() {
	}
	void RunVB() {}
};




class Options_BGSubtract {
public:
	String methodDesc = "MOG2";
	int currMethod = 4;
	float MOGlearnRate = 1 / 1000;
	Options_BGSubtract() {
	}
	void RunVB() {}
};




class Options_BinarizeNiBlack {
public:
	int kernelSize = 51;
	float niBlackK = -200 / 1000;
	float nickK = 100 / 1000;
	float sauvolaK = 100 / 1000;
	float sauvolaR = 64;
	Options_BinarizeNiBlack() {
	}
	void RunVB() {}
};




class Options_Blob {
public:
	cv::SimpleBlobDetector::Params blobParams;
	Options_Blob() {
	}
	void RunVB() {}
};



class Options_BlockMatching {
public:
	int numDisparity = 2 * 16;
	int blockSize = 15;
	int distance = 20;
	Options_BlockMatching() {
	}
	void RunVB() {}
};



class Options_Boundary {
public:
	int desiredBoundaries = 15;
	int peakDistance = task->WorkingRes.width / 20;
	Options_Boundary() {
	}
	void RunVB() {}
};




class Options_CamShift {
public:
	int camMax = 255;
	Scalar camSBins = Scalar(0, 40, 32);
	Options_CamShift() {
	}
	void RunVB() {}
};




class Options_Canny {
public:
	int threshold1 = 100;
	int threshold2 = 150;
	int aperture = 3;
	Options_Canny() {
	}
	void RunVB() {}
};





class Options_Cartoonify {
public:
	int medianBlur = 7;
	int medianBlur2 = 3;
	int kernelSize = 5;
	int threshold = 80;
	Options_Cartoonify() {
	}
	void RunVB() {}
};




class Options_CComp {
public:
	int light = 127;
	int dark = 50;
	Options_CComp() {
	}
	void RunVB() {}
};




class Options_Classifier {
public:
	String classifierName;
	Options_Classifier() {
	}
	void RunVB() {}
};




class Options_ColorFormat {
public:
	String colorFormat;
	Options_ColorFormat() {
	}
	void RunVB() {}
};




class Options_ColorMatch {
public:
	bool maxDistanceCheck;
	Options_ColorMatch() {
	}
	void RunVB() {}
};




class Options_Colors {
public:
	int redS;
	int greenS;
	int blueS;
	Options_Colors() {
	}
	void RunVB() {}
};




class Options_Complexity {
public:
	Scalar plotColor = Scalar(0, 255, 255);
	Options_Complexity() {
	}
	void RunVB() {}
};




class Options_Contours {
public:
	cv::RetrievalModes retrievalMode = cv::RetrievalModes::RETR_EXTERNAL;
	cv::ContourApproximationModes ApproximationMode = cv::ContourApproximationModes::CHAIN_APPROX_TC89_KCOS;
	float epsilon = 3 / 100;
	Options_Contours() {
	}
	void RunVB() {}
};





class Options_Contours2 {
public:
	cv::ContourApproximationModes ApproximationMode = cv::ContourApproximationModes::CHAIN_APPROX_TC89_KCOS;
	Options_Contours2() {
	}
	void RunVB() {}
};





class Options_DCT {
public:
	int runLengthMin = 15;
	int removeFrequency = 1;
	Options_DCT() {
	}
	void RunVB() {}
};



class Options_Denoise {
public:
	bool removeSinglePixels;
	Options_Denoise() {
	}
	void RunVB() {}
};



class Options_DFT {
public:
	int radius = task->WorkingRes.width;
	int order = 2;
	Options_DFT() {
	}
	void RunVB() {}
};





class Options_Dilate {
public:
	int kernelSize = 3;
	int iterations = 1;
	cv::MorphShapes morphShape;
	cv::Mat element;
	bool noshape;
	Options_Dilate() {
	}
	void RunVB() {}
};





class Options_Distance {
public:
	cv::DistanceTypes distanceType = cv::DistanceTypes::DIST_L1;
	Options_Distance() {
	}
	void RunVB() {}
};





class Options_Dither {
public:
	int radioIndex;
	int bppIndex = 1;
	Options_Dither() {
	}
	void RunVB() {}
};




class Options_DNN {
public:
	String superResModelFileName;
	String shortModelName;
	int superResMultiplier;
	Options_DNN() {
	}
	void RunVB() {}
};






class Options_Draw {
public:
	int drawCount = 3;
	int drawFilled = 2;
	bool drawRotated = false;
	Options_Draw() {
	}
	void RunVB() {}
};



class Options_DrawArc {
public:
	int saveMargin = task->WorkingRes.width / 16;
	bool drawFull;
	bool drawFill;
	Options_DrawArc() {
	}
	void RunVB() {}
};





class Options_Eigen {
public:
	bool highlight;
	bool recompute;
	int randomCount = 100;
	int linePairCount = 20;
	int noiseOffset = 10;
	Options_Eigen() {
	}
	void RunVB() {}
};






class Options_Encode {
public:
	int qualityLevel = 1;
	int scalingLevel = 85;
	cv::ImwriteFlags encodeOption = cv::ImwriteFlags::IMWRITE_JPEG_PROGRESSIVE;
	Options_Encode() {
	}
	void RunVB() {}
};





class Options_Erode {
public:
	int kernelSize = 3;
	int iterations = 1;
	cv::MorphShapes morphShape = cv::MorphShapes::MORPH_CROSS;
	cv::Mat element;
	bool noshape;
	Options_Erode() {
	}
	void RunVB() {}
};





class Options_Filter {
public:
	int kernelSize = 3;
	Options_Filter() {
	}
	void RunVB() {}
};




class Options_FilterNorm {
public:
	cv::Mat kernel;
	Options_FilterNorm() {
	}
	void RunVB() {
		float defaults[21] = { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
		kernel = Mat(1, 21, CV_32FC1, defaults);
	}
};





class Options_FitLine {
public:
	int radiusAccuracy = 10;
	int angleAccuracy = 10;
	Options_FitLine() {
	}
	void RunVB() {}
};





class Options_Flood {
public:
	cv::FloodFillFlags floodFlag;
	int stepSize = 30;
	Options_Flood() {
		floodFlag = cv::FloodFillFlags::FLOODFILL_FIXED_RANGE;
	}
	void RunVB() {}
};





class Options_ForeGround {
public:
	float maxForegroundDepthInMeters = 1500 / 1000;
	int minSizeContour = 100;
	float depthPerRegion;
	int numberOfRegions = 5;
	Options_ForeGround() {
	}
	void RunVB() {}
};





class Options_FPoly {
public:
	int removeThreshold = 4;
	int autoResyncAfterX = 500;
	Options_FPoly() {
	}
	void RunVB() {}
};





class Options_Fractal {
public:
	int iterations = 34;
	bool resetCheck = false;
	Options_Fractal() {
	}
	void RunVB() {}
};





class Options_GeneticDrawing {
public:
	int stageTotal = 100;
	float brushPercent = 1.0;
	int strokeCount = 10;
	bool snapCheck = false;
	int generations = 20;
	Options_GeneticDrawing() {
	}
	void RunVB() {}
};




class Options_Harris {
public:
	float threshold = 1 / 10000;
	int neighborhood = 21;
	int aperture = 21;
	float harrisParm = 1;
	Options_Harris() {
	}
	void RunVB() {}
};





class Options_HeatMap {
public:
	int redThreshold = 20;
	String viewName = "vertical";
	bool showHistory;
	bool topView = true;
	bool sideView;
	Options_HeatMap() {
	}
	void RunVB() {}
};




class Options_HistCompare {
public:
	cv::HistCompMethods compareMethod;
	String compareName;
	Options_HistCompare() {
		compareMethod = cv::HistCompMethods::HISTCMP_CORREL;
	}
	void RunVB() {}
};






class Options_HistXD {
public:
	int sideThreshold = 5;
	int topThreshold = 15;
	int threshold3D = 40;
	int selectedBin = 0;
	Options_HistXD() {
	}
	void RunVB() {}
};






class Options_Hough {
public:
	int rho = 1;
	float theta = float(1000 * CV_PI / 180);
	int threshold = 3;
	int lineCount = 25;
	float relativeIntensity = 90 / 1000;
	Options_Hough() {
	}
	void RunVB() {}
};





class Options_IMUFrameTime {
public:
	int minDelayIMU = 4;
	int minDelayHost = 4;
	int plotLastX = 20;
	Options_IMUFrameTime() {
	}
	void RunVB() {}
};





class Options_InPaint {
public:
	bool telea = false;
	Options_InPaint() {
	}
	void RunVB() {}
};





class Options_Intercepts {
public:
	int interceptRange = 10;
	int mouseMovePoint;
	int selectedIntercept;
	Options_Intercepts() {
	}
	void RunVB() {}
};





class Options_Interpolate {
public:
	int resizePercent = 2;
	int interpolationThreshold = 4;
	int pixelCountThreshold = 0;
	int saveDefaultThreshold = resizePercent;
	Options_Interpolate() {
	}
	void RunVB() {}
};





class Options_KLT {
public:
	cv::Point2f* inputPoints;
	int maxCorners = 100;
	float qualityLevel = 0.01f;
	int minDistance = 7;
	int blockSize = 7;
	bool nightMode;
	Options_KLT() {
	}
	void RunVB() {}
};






class Options_KMeans {
public:
	cv::KmeansFlags kMeansFlag;
	int kMeansK = 5;
	Options_KMeans() {
	}
	void RunVB() {}
};





class Options_Laplacian {
public:
	float scale = 1;
	float delta = 0;
	bool gaussianBlur;
	bool boxFilterBlur;
	int threshold = 15;
	Options_Laplacian() {
	}
	void RunVB() {}
};






class Options_BrightnessContrast {
public:
	float alpha = 2000;
	int beta = -100;
	Options_BrightnessContrast() {
	}
	void RunVB() {}
};





class Options_Line {
public:
	int lineLengthThreshold = 20;
	Options_Line() {
	}
	void RunVB() {}
};






class Options_MatchCell {
public:
	float overlapPercent = 0.5f;
	Options_MatchCell() {
	}
	void RunVB() {}
};






class Options_MatchShapes {
public:
	cv::ShapeMatchModes matchOption;
	float matchThreshold = 0.8f;
	float maxYdelta = 0.05f;
	float minSize = (task->WorkingRes.width * task->WorkingRes.height) / 100;
	Options_MatchShapes() {
		matchOption = cv::ShapeMatchModes::CONTOURS_MATCH_I1;
	}
	void RunVB() {}
};





class Options_MinArea {
public:
	int squareWidth = 100;
	int numPoints = 5;
	Options_MinArea() {
	}
	void RunVB() {}
};





class Options_MinMaxNone {
public:
	bool useMax;
	bool useMin;
	bool useNone;
	Options_MinMaxNone() {
	}
	void RunVB() {}
};





class Options_Motion {
public:
	int motionThreshold;
	float cumulativePercentThreshold = 0.1f;
	Options_Motion() {
	}
	void RunVB() {}
};





class Options_MotionBlur {
public:
	bool showDirection = true;
	int kernelSize = 51;
	float theta;
	int restoreLen = 10;
	int SNR = 700;
	int gamma = 5;
	Options_MotionBlur() {
	}
	void RunVB() {}
};





class Options_MSER {
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
	Options_MSER() {
	}
	void RunVB() {}
};





class Options_Neighbors {
public:
	float threshold = 0.005f;
	int pixels = 6;
	bool patchZ;
	Options_Neighbors() {
	}
	void RunVB() {}
};





class Options_OilPaint {
public:
	int kernelSize = 4;
	int intensity = 20;
	int threshold = 25;
	int filterSize = 3;
	Options_OilPaint() {
	}
	void RunVB() {}
};





class Options_OpenGL {
public:
	float FOV = 75;
	float yaw = -3;
	float pitch = 3;
	float roll = 0;
	float zNear = 0;
	float zFar = 20;
	int pointSize = 2;
	float zTrans = 0.5f;
	Options_OpenGL() {
	}
	void RunVB() {}
};







class Options_OpenGL_Contours {
public:
	int depthPointStyle;
	float filterThreshold = 0.3f;
	Options_OpenGL_Contours() {
	}
	void RunVB() {}
};





class Options_OpenGLFunctions {
public:
	cv::Point3f moveAmount;
	float FOV = 75;
	float yaw = -3;
	float pitch = 3;
	float roll = 0;
	float zNear = 0;
	float zFar = 20.0f;
	float zTrans = 0.5f;
	Options_OpenGLFunctions() {
	}
	void RunVB() {}
};





class Options_Photoshop {
public:
	int switchVal = 0;
	Options_Photoshop() {
	}
	void RunVB() {}
};





class Options_Plane {
public:
	float rmsThreshold = 0.1f;
	bool useMaskPoints = false;
	bool useContourPoints = false;
	bool use3Points = false;
	bool reuseRawDepthData = false;
	Options_Plane() {
	}
	void RunVB() {}
};




class Options_PlaneEstimation {
public:
	bool useDiagonalLines = false;
	bool useContour_SidePoints = false;
	Options_PlaneEstimation() {
	}
	void RunVB() {}
};





class Options_Pointilism {
public:
	int smoothingRadius = 32 * 2 + 1;
	int strokeSize = 3;
	bool useElliptical = false;
	Options_Pointilism() {
	}
	void RunVB() {}
};




class Options_ProCon {
public:
    int buffer[10];
	int pduration = 1;
	int cduration = 1;
	int bufferSize = 10;
	Options_ProCon() {
	}
	void RunVB() {}
};






class Options_Resize {
public:
	cv::InterpolationFlags warpFlag = cv::InterpolationFlags::INTER_NEAREST;
	int radioIndex;
	float resizePercent = 0.03f;
	int topLeftOffset = 10;
	Options_Resize() {
	}
	void RunVB() {}
};




class Options_SepFilter2D {
public:
	int xDim = 5;
	int yDim = 11;
	float sigma = 17;
	bool diffCheck = false;
	Options_SepFilter2D() {
	}
	void RunVB() {}
};





class Options_Sift {
public:
	bool useBFMatcher = false;
	int pointCount = 200;
	Options_Sift() {
	}
	void RunVB() {}
};





class Options_Smoothing {
public:
	int iterations = 8;
	float interiorTension = 0.5f;
	int stepSize = 30;
	Options_Smoothing() {
	}
	void RunVB() {}
};





class Options_Sobel {
public:
	int kernelSize = 3;
	int threshold = 50;
	bool horizontalDerivative = true;
	bool verticalDerivative = true;
	Options_Sobel() {
	}
	void RunVB() {}
};




class Options_SOM {
public:
	int iterations = 3000;
	float learningRate = 0.1f;
	int radius = 15;
	Options_SOM() {
	}
	void RunVB() {}
};





class Options_Spectrum {
public:
	int gapDepth = 1;
	int gapGray = 1;
	int sampleThreshold = 10;
	Options_Spectrum() {
	}
	void RunVB() {}
};





class Options_Structured {
public:
	int sliceSize = 1;
	int stepSize = 20;
	Options_Structured() {
	}
	void RunVB() {}
};





class Options_SuperRes {
public:
	String method = "farneback";
	int iterations = 10;
	bool restartWithNewOptions = false;
	Options_SuperRes() {
	}
	void RunVB() {}
	};





class Options_SVM {
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
	Options_SVM() {
		kernelType = cv::ml::SVM::KernelTypes::POLY;
	}
	void RunVB() {}
};





class Options_SVM2 {
public:
	cv::ml::SVM::KernelTypes SVMType;
	Options_SVM2() {
		SVMType = cv::ml::SVM::KernelTypes::CHI2;
	}
	void RunVB() {}
};





class Options_SymmetricalShapes {
public:
	float rotateAngle = 0;
	cv::Scalar fillColor = cv::Scalar(0, 0, 255);
	int numPoints = 500;
	int nGenPer = 100;
	int radius1 = task->WorkingRes.height / 4;
	int radius2 = task->WorkingRes.height / 8;
	float dTheta = 2 * CV_PI / numPoints;
	bool symmetricRipple = true;
	bool reverseInOut = false;
	bool fillRequest = true;
	Options_SymmetricalShapes() {
	}
	void RunVB() {}
};





class Options_Threshold {
public:
	cv::ThresholdTypes thresholdOption;
	int maxVal = 255;
	int threshold = 100;
	bool inputGray = false;
	bool otsuOption = true;
	Options_Threshold() {
	}
	void RunVB() {}
};





class Options_Threshold_Adaptive {
public:
	cv::AdaptiveThresholdTypes method;
	int blockSize = 5;
	int constantVal = 0;
	Options_Threshold_Adaptive() {
	}
	void RunVB() {}
};




class Options_Warp {
public:
	double alpha = 0.9;
	double beta = 0.9;
	double gamma = 0.9;
	double f = 600;
	double distance = 400;
	Options_Warp() {
	}
	void RunVB() {}
};





class Options_Wavelet {
public:
	bool useHaar = false;
	int iterations = 3;
	Options_Wavelet() {
	}
	void RunVB() {}
};





class Options_XPhoto {
public:
	cv::ColorConversionCodes colorCode;
	int dynamicRatio;
	int blockSize;
	Options_XPhoto() {
		colorCode = cv::ColorConversionCodes::COLOR_BGR2GRAY;
	}
	void RunVB() {}
};