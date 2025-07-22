// License: Apache 2.0. See LICENSE file in root directory.
// Copyright(c) 2015 Intel Corporation. All Rights Reserved.
#include "OpenGLcommon.h"

int main(int argc, char * argv[])
{ 
	windowTitle << "OpenGL_Basics"; // this will create the window title.
	initializeNamedPipeAndMemMap(argc, argv);

	glfwInit();
	win = glfwCreateWindow(windowWidth, windowHeight, windowTitle.str().c_str(), 0, 0);

	state app_state = { 0, 0, 0, 0, false, 0 };
	glfwSetWindowUserPointer(win, &app_state);
	glfwMakeContextCurrent(win);

	Sleep(10);
	myHwnd = FindWindow(NULL, L"OpenGL_Basics");

	while (!glfwWindowShouldClose(win))
	{
		glfwPollEvents();
		readPipeAndMemMap();

		glPushAttrib(GL_ALL_ATTRIB_BITS);

		rgb.upload(rgbBuffer, imageWidth, imageHeight);

		glfwGetFramebufferSize(win, &windowWidth, &windowHeight);
		glViewport(0, 0, windowWidth, windowHeight);

		glPointSize((float)pointSize);
		glClearColor(1, 1, 1, 1);
		glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

		glMatrixMode(GL_PROJECTION);
		glPushMatrix();
		gluPerspective(FOV, (float)windowWidth / windowHeight, zNear + 0.01f, zFar);

		glMatrixMode(GL_MODELVIEW);
		glPushMatrix();
		gluLookAt(Eye.x, Eye.y, Eye.z, 0, 0, 10, 0, -1, 0);
		glScalef(scaleXYZ.x, scaleXYZ.y, scaleXYZ.z);

		glTranslatef(0, 0, zTrans);
		glRotated(app_state.pitch, 1, 0, 0);
		glRotated(app_state.yaw,   0, 1, 0);
		glRotated(app_state.roll,  0, 0, 1);
		glTranslatef(0, 0, -zTrans);

		glEnable(GL_DEPTH_TEST);
		glEnable(GL_TEXTURE_2D);
		glBindTexture(GL_TEXTURE_2D, rgb.get_gl_handle());

		glColor3f(1, 1, 1);

		drawPointCloudRGB();
		glDisable(GL_TEXTURE_2D);

		glEnable(GL_BLEND);
		glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
		drawAxes(10, 0, 0, 1);
		draw_floor();

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
