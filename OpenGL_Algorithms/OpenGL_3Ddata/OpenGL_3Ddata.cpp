#include "../OpenGL_Basics/OpenGLcommon.h"
#define GLFW_INCLUDE_GLU
#include <GLFW/glfw3.h>

int main(int argc, char * argv[])
{ 
	windowTitle << "OpenCVB Data Cloud"; // this will create the window title.
	if (initializeNamedPipeAndMemMap(argc, argv) != 0) return -1;

	glfwInit();
	win = glfwCreateWindow(windowWidth, windowHeight, windowTitle.str().c_str(), 0, 0);

	app_state = { 0, 0, 0, 0, false, 0 };
	glfwSetWindowUserPointer(win, &app_state);
	glfwMakeContextCurrent(win);

	while (!glfwWindowShouldClose(win))
	{
		glfwPollEvents();

		readPipeAndMemMap();

		glPointSize((float)pointSize);
		glPushAttrib(GL_ALL_ATTRIB_BITS);

		glfwGetFramebufferSize(win, &windowWidth, &windowHeight);
		glViewport(0, 0, windowWidth, windowHeight);
		glClearColor(1, 1, 1, 1);
		glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

		glMatrixMode(GL_PROJECTION);
		glPushMatrix();
		gluPerspective(FOV, (float)windowWidth / windowHeight, zNear + 0.01, zFar);

		glMatrixMode(GL_MODELVIEW);
		glPushMatrix();
		gluLookAt(Eye.x, Eye.y, Eye.z, 0, 0, 10, 0, -1, 0);

		float zTrans = 1.5;
		glTranslatef(0, 0, zTrans);
		glRotated(app_state.pitch, 1, 0, 0);
		glRotated(app_state.yaw, 0, 1, 0);
		glRotated(app_state.roll, 0, 0, 1);
		glTranslatef(0, 0, -zTrans);

		glEnable(GL_DEPTH_TEST);
		glEnable(GL_TEXTURE_2D);
		glBindTexture(GL_TEXTURE_2D, rgb.get_gl_handle());

		glColor3f(1, 1, 1);
		glBegin(GL_POINTS);

		int bins = (int) cbrt(dataBufferSize / 4); // 3D histograms must have 3 equal bins...
		float *data = (float *)dataBuffer;
		float3 xyz;
		for (int z = 0; z < bins; ++z)
		{
			for (int y = 0; y < bins; ++y)
			{
				for (int x = 0; x < bins; ++x)
				{
					float val = (float)data[x + y * bins + z * bins * bins];
					int index = (int)val;
					if (index != 0)
					{
						glColor3ub(rgbBuffer[index], rgbBuffer[index + 1], rgbBuffer[index + 2]);
						xyz.x = (float) x; xyz.y = (float) -y; xyz.z = (float) z;
						glVertex3fv((const GLfloat *)&xyz);
					}
				}
			}
		}

		glEnd();
		glDisable(GL_TEXTURE_2D);

		float zDistance = 10.0f;
		drawAxes(100.0f, 0, 0, zDistance);
		draw_floor(20, 1, 0);

		glPopMatrix();
		glMatrixMode(GL_PROJECTION);
		glPopMatrix();
		glPopAttrib();

		glfwGetWindowSize(win, &windowWidth, &windowHeight);
		glPushAttrib(GL_ALL_ATTRIB_BITS);
		glPushMatrix();
		glOrtho(0, windowWidth, windowHeight, 0, zNear, zFar);

		glPopMatrix();

		glfwSwapBuffers(win);

		if (ackBuffers()) break;
	}

	CloseHandle(hMapFile);
	CloseHandle(pipe);
	glfwDestroyWindow(win);
	glfwTerminate();
	return EXIT_SUCCESS;
}
