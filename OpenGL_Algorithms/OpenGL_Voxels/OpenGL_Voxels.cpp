// Create a new project with this as the main module.  Simplest way: copy the OpenGL_Callbacks project to another directory.
// Rename the directory and project and add it to the OpenCVB solution.  Don't forget to update VB_Classes dependencies.
// When VB_Classes depends on your new project, your new OpenGL will recompile (if changed) with every restart of OpenCVB.
#include "example.hpp"          // Include short list of convenience functions for rendering
#define NOGLFW
#include "../OpenGL_Basics/OpenGLcommon.h"
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>

using namespace cv;

int main(int argc, char* argv[])
{
	GLuint gl_handle = 0;
	windowTitle << "OpenGL_Voxels";
	initializeNamedPipeAndMemMap(argc, argv);

	window app(windowWidth, windowHeight, windowTitle.str().c_str());
	glfw_state MyState;
	MyState.offset_x = 0;
	MyState.offset_y = -35.0f;
	MyState.pitch = 5.0f;
	MyState.yaw = 0;
	register_glfw_callbacks(app, MyState);

	while (app)
	{
		readPipeAndMemMap(); 
		glLoadIdentity();
		glPushAttrib(GL_ALL_ATTRIB_BITS);

		glClearColor(153.f / 255, 153.f / 255, 153.f / 255, 1);
		glClear(GL_DEPTH_BUFFER_BIT);

		glMatrixMode(GL_PROJECTION);
		glPushMatrix();
		gluPerspective(160, imageWidth / imageHeight, 0.01f, 30.0f);

		glMatrixMode(GL_MODELVIEW);
		glPushMatrix();
		gluLookAt(0, 0, -2, 0, 0, 10, 0, -1, 0);
		 
		glTranslatef(0, 0, +0.5f + MyState.offset_y * 0.05f);
		glRotated(MyState.pitch, 1, 0, 0);
		glRotated(MyState.yaw, 0, 1, 0);
		glTranslatef(0, 0, -0.5f);

		glEnable(GL_DEPTH_TEST);
		glColor3f(1.0f, 0, 0);

		Mat voxels(dataHeight, dataWidth, CV_32F, dataBuffer);

		float x = 0, y = -5, z = 0;
		float dx = 0.3f, dy = dx, dz = dx;
		Scalar nearColor(1.0f, 1.0f, 0.0f);
		Scalar farColor (0.0f, 0.0f, 1.0f);
		int half = int(dataWidth / 2);
		int testCount = 0;
		glLineWidth(3.0f);
		double min, max;
		cv::minMaxLoc(voxels, &min, &max);
		min /= 1000;
		max /= 1000;
		for (int i = -half; i < half; i++)
		{
			for (int j = 0; j < dataHeight; j++)
			{
				float d = voxels.at<float>(dataHeight - j - 1, i + half) / 1000;
				float v = float((d - min) / (max - min));
				if (v > 0 && v < 1.0f)
				{
					glBegin(GL_POLYGON);
					glColor3f(float((1.0f - v) * nearColor(0) + v * farColor(0)),
							  float((1.0f - v) * nearColor(1) + v * farColor(1)),
							  float((1.0f - v) * nearColor(2) + v * farColor(2)));
					z = float(d * 5.0f);
					glVertex3f(x + i * dx, -y - j * dy - dy, z - dz);
					glVertex3f(x + i * dx, -y - j * dy, z - dz);      
					glVertex3f(x + i * dx - dx, -y - j * dy, z - dz);      
					glVertex3f(x + i * dx - dx, -y - j * dy - dy, z - dz);      
					glEnd();
					DrawBox(x + i * dx, -y - j * dy, z, dx, dy, dz);
					DrawWireFrame(x + i * dx, -y - j * dy, z, dx, dy, dz);
				}
			}
		}

		drawAxes(50, 0, 0, 5);
		draw_floor(20, 10, 0);
		
		glPopMatrix();
		glMatrixMode(GL_PROJECTION);
		glPopMatrix();
		glPopAttrib();
		
		if (ackBuffers()) break;
	}

	CloseHandle(hMapFile);
	CloseHandle(pipe);
	return EXIT_SUCCESS;
}
