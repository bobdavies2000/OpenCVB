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

