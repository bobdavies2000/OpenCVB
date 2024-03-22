#pragma once
#pragma warning (disable : 6387 4005 6011 6031 26451)
#define NOMINMAX
#include <stdio.h>
#include <chrono>
#include <vector>
#include <sstream>
#include <iostream>
#include <algorithm>
#include <winsock2.h>
#include <chrono>
#include <thread>
#include <tchar.h>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <atltypes.h>
#include "../../CPP_Classes/PragmaLibs.h"

using namespace  cv;

#ifndef NOGLFW
#define GLFW_INCLUDE_GLU
#include <GLFW/glfw3.h>

struct float3 {
	float x, y, z;
	float3 operator*(float t)
	{
		return { x * t, y * t, z * t };
	}

	float3 operator-(float t)
	{
		return { x - t, y - t, z - t };
	}

	void operator*=(float t)
	{
		x = x * t;
		y = y * t;
		z = z * t;
	}

	void operator=(float3 other)
	{
		x = other.x;
		y = other.y;
		z = other.z;
	}

	void add(float t1, float t2, float t3)
	{
		x += t1;
		y += t2;
		z += t3;
	}
};
struct float2 { float x, y; };
#endif

class texture_buffer
{
	GLuint texture;
	int last_timestamp;
	std::vector<uint8_t> rgb;

	int frameRate, num_frames, next_time;
public:
	texture_buffer() : texture(), last_timestamp(-1), frameRate(), num_frames(), next_time(1000) {}

	GLuint get_gl_handle() const { return texture; }

	void upload(const void* data, int width, int height)
	{
		// If the frame timestamp has changed since the last time show(...) was called, re-upload the texture
		if (!texture) glGenTextures(1, &texture);
		glBindTexture(GL_TEXTURE_2D, texture);

		glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, width, height, 0, GL_RGB, GL_UNSIGNED_BYTE, data);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP);
		glPixelStorei(GL_UNPACK_ROW_LENGTH, 0);
		glBindTexture(GL_TEXTURE_2D, texture);
	}

	void uploadRGBA(const void* data, int width, int height)
	{
		// If the frame timestamp has changed since the last time show(...) was called, re-upload the texture
		if (!texture) glGenTextures(1, &texture);
		glBindTexture(GL_TEXTURE_2D, texture);

		glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, data);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP);
		glPixelStorei(GL_UNPACK_ROW_LENGTH, 0);
		glBindTexture(GL_TEXTURE_2D, texture);
	}
};

struct state {
	double yaw, pitch, roll, lastX, lastY;
	bool ml;
	int index;
	float offset_x;
	float offset_y;
};

static std::wstring s2ws(const std::string& s)
{
	int len;
	int slength = (int)s.length() + 1;
	len = MultiByteToWideChar(CP_ACP, 0, s.c_str(), slength, 0, 0);
	wchar_t* buf = new wchar_t[len];
	MultiByteToWideChar(CP_ACP, 0, s.c_str(), slength, buf, len);
	std::wstring r(buf);
	delete[] buf;
	return r;
}
 
static HANDLE pipe;
static int MemMapBufferSize;
static GLFWwindow * win;
static double *sharedMem;
static state app_state;

static float3* dataBuffer = 0;
static int dataBufferSize = 0;
static int savedataBufferSize = -1;

static uint8_t *rgbBuffer = 0;
static int rgbBufferSize = 0;
static int saveRGBBufferSize = -1;

static float3 *pointCloudInput;
static int pcBufferSize = 0;
static int savePCBufferSize = -1;

static HANDLE hMapFile;
static int imageWidth;
static int imageHeight;
static double FOV;
static double zNear;
static double zFar;
static int lastFrame = 0;
static texture_buffer rgb;
static texture_buffer tBuffer;
static int pointSize;
static int windowWidth;
static int windowHeight;
static int dataWidth;
static int dataHeight;
static int textureWidth;
static int textureHeight;
static std::ostringstream windowTitle;
static float3 gyro_data;
static float3 accel_data;
static double timestamp;
static float timeConversionUnits = 1000.0f;
static int imageLabelBufferSize = 0;
static int pointcloudWidth;
static int pointcloudHeight;
static char imageLabel[1000];

/* alpha indicates the part that gyro and accelerometer take in computation of theta; higher alpha gives more weight to gyro, but too high
values cause drift; lower alpha gives more weight to accelerometer, which is more sensitive to disturbances */
static float imuAlphaFactor = 0.98f; // Intel IMU mixes the accel and gyro values to get direction while it does not work on K4A
static float3 Eye;
static float3 scaleXYZ;
static float zTrans;
static int oglFunction; // defines the work to be done in the case statement in OpenGL_Functions
static bool showAxes; // options.showXYZaxis
static HWND myHwnd;

static int initializeNamedPipeAndMemMap(int argc, char * argv[])
{
	if (argc != 5)
	{
		MessageBox(0, L"Incorrect number of parameters.  Command should be: <program> <width> <height> <MemMapBufferSize> <windowLeft> <windowTop> <pipeName>\r\n\n\nDon't try to run without OpenCVB.  It won't run correctly.  Use OpenCVB.exe.", L"OpenCVB", MB_OK);
		return -1;
	}
	 
	windowWidth = std::stoi(argv[1]);
	windowHeight = std::stoi(argv[2]);
	MemMapBufferSize = std::stoi(argv[3]);
	printf("MemMapBufferSize = %d\n", MemMapBufferSize);
	printf("width = %d height = %d\n", windowWidth, windowHeight);

	std::string pipeName(argv[4]);

	// setup named pipe interface
	std::string pipePrefix("\\\\.\\pipe\\");
	pipeName = pipePrefix + pipeName;
	std::wstring fullpipeName = s2ws(pipeName);
	pipe = CreateFile(fullpipeName.c_str(), GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
	TCHAR szName[] = TEXT("OpenCVBControl");
	hMapFile = CreateFileMapping(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, 0, MemMapBufferSize, szName);
	 
	sharedMem = (double *)MapViewOfFile(hMapFile, FILE_MAP_ALL_ACCESS, 0, 0, 
		    		   MemMapBufferSize);

	//printf("windowWidth = %d, windowHeight = %d, pipeName = %s\n", windowWidth, windowHeight, pipeName.c_str());
	return 0;
} 

static void readPipeAndMemMap()
{
	int skipCount = 0;
	while (1)
	{
		if ((int)sharedMem[0] != lastFrame) break;
		if (++skipCount > 100) break; // process the current image again to enable getting a closeWindow request (if one comes.)
		std::this_thread::sleep_for(std::chrono::milliseconds(1));
	}

	lastFrame = (int)sharedMem[0];
	imageWidth = (int)sharedMem[1];
	imageHeight = (int)sharedMem[2];

	oglFunction = (int)sharedMem[34];
	rgbBufferSize = (int)sharedMem[3];
	dataBufferSize = (int)sharedMem[4];
	pointcloudWidth = (int)sharedMem[32];
	pointcloudHeight = (int)sharedMem[33];

	pcBufferSize = pointcloudWidth * pointcloudHeight * 12;

	if (rgbBufferSize > 0 && rgbBufferSize != saveRGBBufferSize)
	{
		rgbBuffer = (unsigned char*)malloc(rgbBufferSize);
		saveRGBBufferSize = rgbBufferSize;
	}

	if (dataBufferSize > 0 && dataBufferSize != savedataBufferSize)
	{
		if (dataBuffer != 0) free(dataBuffer);
		dataBuffer = (float3*)malloc(dataBufferSize);
		savedataBufferSize = dataBufferSize;
	}

	if (pcBufferSize > 0 && pcBufferSize != savePCBufferSize)
	{
		pointCloudInput = (float3*)malloc((int)pcBufferSize);
		savePCBufferSize = pcBufferSize;
	}

	printf("\n\n\nRGB = %d bytes, data = %d bytes, pointcloud = %d bytes, w=%d h=%d Function = %d\n", 
			rgbBufferSize, dataBufferSize, pcBufferSize, imageWidth, imageHeight, oglFunction);

	FOV = sharedMem[5];

	zNear = sharedMem[9];
	zFar = sharedMem[10];
	pointSize = (int)sharedMem[11];

	app_state.yaw = sharedMem[6];
	app_state.pitch = sharedMem[7];
	app_state.roll = sharedMem[8];
	
	gyro_data.x = (float)sharedMem[14];
	gyro_data.y = (float)sharedMem[15];
	gyro_data.z = (float)sharedMem[16];

	accel_data.x = (float)sharedMem[17];
	accel_data.y = (float)sharedMem[18];
	accel_data.z = (float)sharedMem[19];

	timestamp = sharedMem[20];

	Eye.x = (float)sharedMem[22];
	Eye.y = (float)sharedMem[23];
	Eye.z = (float)sharedMem[24];
	zTrans = (float)sharedMem[25];

	scaleXYZ.x = (float)sharedMem[26];
	scaleXYZ.y = (float)sharedMem[27];
	scaleXYZ.z = (float)sharedMem[28];

	timeConversionUnits = (float)sharedMem[29];
	imuAlphaFactor = (float)sharedMem[30];
	imageLabelBufferSize = (int)sharedMem[31];

	DWORD dwRead;

	if (rgbBufferSize > 0)
	{
		ReadFile(pipe, rgbBuffer, rgbBufferSize, &dwRead, NULL);
	}

	if (dataBufferSize > 0)
	{
		dataWidth = (int)sharedMem[12];
		dataHeight = (int)sharedMem[13];
		ReadFile(pipe, dataBuffer, dataBufferSize, &dwRead, NULL);
	}

	if (pcBufferSize > 0)
		ReadFile(pipe, pointCloudInput, (int)pcBufferSize, &dwRead, NULL);

	if ((int)sharedMem[35]) // activateTaskRequest
	{
		SetWindowPos(myHwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
	} else {
		SetWindowPos(myHwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
	}

	showAxes = (bool)sharedMem[36];
	ReadFile(pipe, imageLabel, imageLabelBufferSize, &dwRead, NULL);
	imageLabel[imageLabelBufferSize] = 0;
}

static int ackBuffers()
{
	DWORD dwWrite = 0;
	WriteFile(pipe, &lastFrame, 4, &dwWrite, NULL);
	if (dwWrite != 4)
	{
		printf("WriteFile failed\n");
		return -1;
	}
	return 0;
}

static void drawAxes(float axislen, float x, float y, float z)
{
	if (showAxes)
	{
		glLineWidth(5.0);
		glBegin(GL_LINES);
		glColor3f(1, 0, 0); glVertex3f(x, y, z); glVertex3f(axislen, y, z);
		glColor3f(0, 1, 0); glVertex3f(x, y, z); glVertex3f(x, -axislen, z);
		glColor3f(0, 0, 1); glVertex3f(x, y, z); glVertex3f(x, y, -axislen);
		glEnd();
	}
}

static void draw_floor()
{
	glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
	glBegin(GL_POLYGON);

	glColor4f(0.4f, 0.4f, 0.4f, 0.5f);
	GLfloat y = 1.0f;
	glVertex3f(-5.0f, y, 0.0f);
	glVertex3f(5.0f, y, 0.0f);
	glVertex3f(5.0f, y, 20.0f);
	glVertex3f(-5.0f, y, 20.0f);
	glEnd();
	glPolygonMode(GL_FRONT_AND_BACK, GL_POINT);
}

static void drawFlatPlane(GLfloat y)
{
	glEnable(GL_BLEND);
	glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

	glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
	glBegin(GL_POLYGON);

	glVertex3f(-5.0f, y, 0.0f);
	glVertex3f(5.0f, y, 0.0f);
	glVertex3f(5.0f, y, 20.0f);
	glVertex3f(-5.0f, y, 20.0f);
	glEnd();
	glPolygonMode(GL_FRONT_AND_BACK, GL_POINT);
}

static void DrawWireFrame(float x, float y, float z, float dx, float dy, float dz)
{
	glColor3f(0, 0, 0);
	glBegin(GL_LINES);

	glVertex3f(x, y, z);
	glVertex3f(x - dx, y, z);
	glVertex3f(x - dx, y - dy, z);
	glVertex3f(x, y - dy, z);

	glVertex3f(x, y - dy, z - dz);
	glVertex3f(x, y, z - dz);
	glVertex3f(x, y, z);
	glVertex3f(x, y - dy, z);

	glVertex3f(x - dx, y - dy, z);
	glVertex3f(x - dx, y, z);
	glVertex3f(x - dx, y, z - dz);
	glVertex3f(x - dx, y - dy, z - dz);

	glVertex3f(x, y, z);
	glVertex3f(x, y, z - dz);
	glVertex3f(x - dx, y, z - dz);
	glVertex3f(x - dx, y, z);

	glVertex3f(x, y - dy, z - dz);
	glVertex3f(x, y - dy, z);
	glVertex3f(x - dx, y - dy, z);
	glVertex3f(x - dx, y - dy, z - dz);

	glEnd();
}
static void DrawBox(float x, float y, float z, float dx, float dy, float dz)
{
	glBegin(GL_POLYGON);
	glVertex3f(x, y - dy, z);
	glVertex3f(x, y, z);
	glVertex3f(x - dx, y, z);
	glVertex3f(x - dx, y - dy, z);

	glVertex3f(x, y - dy, z - dz);
	glVertex3f(x, y, z - dz);
	glVertex3f(x, y, z);
	glVertex3f(x, y - dy, z);

	glVertex3f(x - dx, y - dy, z);
	glVertex3f(x - dx, y, z);
	glVertex3f(x - dx, y, z - dz);
	glVertex3f(x - dx, y - dy, z - dz);

	glVertex3f(x, y, z);
	glVertex3f(x, y, z - dz);
	glVertex3f(x - dx, y, z - dz);
	glVertex3f(x - dx, y, z);

	glVertex3f(x, y - dy, z - dz);
	glVertex3f(x, y - dy, z);
	glVertex3f(x - dx, y - dy, z);
	glVertex3f(x - dx, y - dy, z - dz);
	glEnd();
}


static void drawPointCloud()
{
	glBegin(GL_POINTS);

	// draw the 3D scene
	int pcIndex = 0; GLfloat* pc = (GLfloat*)pointCloudInput; GLfloat pt[] = { 0, 0 };
	for (int y = 0; y < imageHeight; ++y)
	{
		for (int x = 0; x < imageWidth; ++x)
		{
			glVertex3fv(&pc[pcIndex]);
			pt[0] = GLfloat((x + 0.5f) / imageWidth);
			pt[1] = GLfloat((y + 0.5f) / imageHeight);
			glTexCoord2fv(pt);
			pcIndex += 3;
		}
	}
	glEnd();
}