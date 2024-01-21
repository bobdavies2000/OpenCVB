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





