class CPP_Options_Blur {
public:
	int kernelSize = 3;
	float sigma = 1.5f;
	CPP_Options_Blur() {
		//	traceName = "CPP_Options_Blur";
		//        if (sliders.Setup(traceName)) {
		//            sliders.setupTrackBar("Blur Kernel Size", 0, 32, 3);
		//            sliders.setupTrackBar("Blur Sigma", 1, 10, 3);
		//        }
	}
	void Run() {
		//        static Trackbar* kernelSlider = findSlider("Blur Kernel Size");
		//        static Trackbar* sigmaSlider = findSlider("Blur Sigma");
		//        kernelSize = kernelSlider->value | 1;  
		//        sigma = sigmaSlider->value * 0.5f;
	}
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
	int kernelSize;
	int bgThreshold;
	int contrastMin;
	CPP_Options_Bernson() {
		//	traceName = "CPP_Options_Bernson";
		//        if (sliders.Setup(traceName)) {
		//            sliders.setupTrackBar("Kernel Size", 3, 500, kernelSize);
		//            sliders.setupTrackBar("Contrast min", 0, 255, contrastMin);
		//            sliders.setupTrackBar("bg Threshold", 0, 255, bgThreshold);
		//        }
	}
	void Run() {
		//        static Slider* kernelSlider = findSlider("Kernel Size");
		//        static Slider* contrastSlider = findSlider("Contrast min");
		//        static Slider* bgSlider = findSlider("bg Threshold");
		//        kernelSize = kernelSlider->value | 1;  
		//        bgThreshold = bgSlider->value;
		//        contrastMin = contrastSlider->value;
	}
};







class CPP_Options_BGSubtract {
public:
	float MOGlearnRate;
	CPP_Options_BGSubtract() {
		//	traceName = "CPP_Options_BGSubtract";
		//        if (sliders.Setup(traceName)) {
		//            sliders.setupTrackBar("MOG Learn Rate X1000", 1, 1000, 1);
		//        }
	}
	void Run() {
		//        static Slider* learnRateSlider = findSlider("MOG Learn Rate X1000");
		//        MOGlearnRate = learnRateSlider->value / 1000.0f;
	}
};







class CPP_Options_BGSubtractSynthetic {
public:
	double amplitude;
	double magnitude;
	double waveSpeed;
	double objectSpeed;
	CPP_Options_BGSubtractSynthetic() {
		//	traceName = "CPP_Options_BGSubtractSynthetic";
		//        if (sliders.Setup(traceName)) {
		//            sliders.setupTrackBar("Synthetic Amplitude x100", 1, 400, amplitude);
		//            sliders.setupTrackBar("Synthetic Magnitude", 1, 40, magnitude);
		//            sliders.setupTrackBar("Synthetic Wavespeed x100", 1, 400, waveSpeed);
		//            sliders.setupTrackBar("Synthetic ObjectSpeed", 1, 20, objectSpeed);
		//        }
	}
	void Run() {
		//        static Slider* amplitudeSlider = findSlider("Synthetic Amplitude x100");
		//        static Slider* magnitudeSlider = findSlider("Synthetic Magnitude");
		//        static Slider* waveSpeedSlider = findSlider("Synthetic Wavespeed x100");
		//        static Slider* objectSpeedSlider = findSlider("Synthetic ObjectSpeed");
		//        amplitude = amplitudeSlider->value;
		//        magnitude = magnitudeSlider->value;
		//        waveSpeed = waveSpeedSlider->value;
		//        objectSpeed = objectSpeedSlider->value;
	}
};
