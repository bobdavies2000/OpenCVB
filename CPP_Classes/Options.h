class CPP_Options_Blur {
public:
	int kernelSize = 3;
	float sigma = 1.5;
	CPP_Options_Blur() {
	}
	void Run() {}
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
	void Run() {
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
		//	traceName = "CPP_Options_Extrinsics";
		        int leftVal = 15, rightVal = 15, topBotVal = 15;
		        //if (task->cameraName == "Intel(R) RealSense(TM) Depth Camera 435i") {
		        //    leftVal = 14;
		        //    rightVal = 13;
		        //    topBotVal = 14;
		        //} else if (task->cameraName == "Oak-D camera") {
		        //    leftVal = 10;
		        //    rightVal = 10;
		        //    topBotVal = 10;
		        //}
				leftCorner = task->workingRes.width * leftVal;
				rightCorner = task->workingRes.width * rightVal;
				topCorner = task->workingRes.width * topBotVal;
				//        if (sliders.Setup(traceName)) {
		//            sliders.setupTrackBar("Left image percent", 0, 50, leftVal);
		//            sliders.setupTrackBar("Right image percent", 0, 50, rightVal);
		//            sliders.setupTrackBar("Height percent", 0, 50, topBotVal);
		//        }
	}
	void Run() {
		//        static Slider* leftSlider = findSlider("Left image percent");
		//        static Slider* rightSlider = findSlider("Right image percent");
		//        static Slider* heightSlider = findSlider("Height percent");
		//        leftCorner = dst2.cols * leftSlider->value / 100;
		//        rightCorner = dst2.cols * rightSlider->value / 100;
		//        topCorner = dst2.rows * heightSlider->value / 100;
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
		//	traceName = "CPP_Options_FeatureMatch";
		//        if (radio.Setup(traceName)) {
		//            radio.addRadio("CCoeff");
		//            radio.addRadio("CCoeffNormed");
		//            radio.addRadio("CCorr");
		//            radio.addRadio("CCorrNormed");
		//            radio.addRadio("SqDiff");
		//            radio.addRadio("SqDiffNormed");
		//            radio.check(1).Checked = true;
		//        }
		//        if (sliders.Setup(traceName)) {
		//            sliders.setupTrackBar("Feature Sample Size", 1, 1000, featurePoints);
		//            sliders.setupTrackBar("Feature Correlation Threshold", 1, 100, correlationThreshold * 100);
		//            sliders.setupTrackBar("MatchTemplate Cell Size", 2, 60, task->workingRes.Height >= 480 ? 20 : matchCellSize * 2);
		//        }
	}
	void Run() {
		//        static Form* frm = findfrm(traceName + " Radio Buttons");
		//        for (int i = 0; i < frm->check.Count; i++) {
		//            if (frm->check[i]->Checked) {
		//                matchOption = i + 1;  
		//                matchText = string({"CCoeff", "CCoeffNormed", "CCorr", "CCorrNormed", "SqDiff", "SqDiffNormed"})[i];
		//                break;
		//            }
		//        }
		//        static Slider* featureSlider = findSlider("Feature Sample Size");
		//        featurePoints = featureSlider->value;
		//        static Slider* corrSlider = findSlider("Feature Correlation Threshold");
		//        correlationThreshold = corrSlider->value / 100.0f;
		//        static Slider* cellSlider = findSlider("MatchTemplate Cell Size");
		//        matchCellSize = cellSlider->value / 2;
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
		//	traceName = "CPP_Options_Features";
		//        if (sliders.Setup(traceName)) {
		//            sliders.setupTrackBar("Distance threshold (pixels)", 1, 30, distanceThreshold);
		//            sliders.setupTrackBar("Min Distance to next", 1, 100, minDistance);
		//            sliders.setupTrackBar("Quality Level", 1, 100, quality * 100);
		//        }
		//        if (findfrm(traceName + " Radio Options") == nullptr) {
		//            radio.Setup(traceName);
		//            radio.addRadio("Use GoodFeatures");
		//            radio.addRadio("Use BRISK");
		//            radio.check(0).Checked = true;
		//        }
	}
	void Run() {
		//        static Slider* distanceSlider = findSlider("Distance threshold (pixels)");
		//        static Slider* qualitySlider = findSlider("Quality Level");
		//        static Slider* distSlider = findSlider("Min Distance to next");
		//        static Radio* briskRadio = findRadio("Use BRISK");
		//        useBRISK = briskRadio->checked;
		//        fOptions.Run();
		//        matchOption = fOptions.matchOption;
		//        matchText = fOptions.matchText;
		//        if (task->optionsChanged) {
		//            distanceThreshold = distanceSlider->value;
		//            quality = qualitySlider->Value / 100.0;
		//            minDistance = distSlider->Value;
		//            roi = cv::Rect(0, 0, fOptions.matchCellSize * 2, fOptions.matchCellSize * 2);
		//        }
	}
};







class CPP_Options_Bernson {
public:
	int kernelSize = 51;
	int bgThreshold = 100;
	int contrastMin = 50;
	CPP_Options_Bernson() {
	}
};




class CPP_Options_BGSubtractSynthetic {
public:
	double amplitude = 200;
	double magnitude = 20;
	double waveSpeed = 20;
	double objectSpeed = 15;
	CPP_Options_BGSubtractSynthetic() {
	}
};

class CPP_Options_BGSubtract {
public:
	float MOGlearnRate = 1 / 1000;
	CPP_Options_BGSubtract() {
	}
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
};




class CPP_Options_Blob {
public:
	cv::SimpleBlobDetector::Params blobParams;
	CPP_Options_Blob() {
	}
};



class CPP_Options_BlockMatching {
public:
	int numDisparity = 2 * 16;
	int blockSize = 15;
	int distance = 20;
	CPP_Options_BlockMatching() {
	}
};



class CPP_Options_Boundary {
public:
	int desiredBoundaries = 15;
	int peakDistance = task->workingRes.width / 20;
	CPP_Options_Boundary() {
	}
};