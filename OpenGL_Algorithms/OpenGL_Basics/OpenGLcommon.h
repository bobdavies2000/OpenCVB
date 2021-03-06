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
#ifdef _DEBUG
#include "../Data/PragmaLibsD.h"
#else
#include "../Data/PragmaLibs.h"
#endif

using namespace cv;

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

	int fps, num_frames, next_time;
public:
	texture_buffer() : texture(), last_timestamp(-1), fps(), num_frames(), next_time(1000) {}

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

static float3* dataBuffer;
static int dataBufferSize = 0;

static uint8_t *textureBuffer;
static int textureBufferSize = 0;

static uint8_t *rgbBuffer = 0;
static int rgbBufferSize;

static float3 *pointCloudBuffer;
static int pcBufferSize = 0;

static double fx, fy, ppx, ppy;
static HANDLE hMapFile;
static int imageWidth;
static int imageHeight;
static double FOV;
static double zNear;
static double zFar;
static int lastFrame = -1;
static int totalMem = 0;
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
static int IMU_Present = false;
static int imageLabelBufferSize = 0;
static int pointcloudWidth;
static int pointcloudHeight;
static char imageLabel[1000];

/* alpha indicates the part that gyro and accelerometer take in computation of theta; higher alpha gives more weight to gyro, but too high
values cause drift; lower alpha gives more weight to accelerometer, which is more sensitive to disturbances */
static float imuAlphaFactor = 0.98f; // Intel IMU mixes the accel and gyro values to get direction while it does not work on Kinect
static float3 Eye;
static float3 scaleXYZ;
static float zTrans;

static int initializeNamedPipeAndMemMap(int argc, char * argv[])
{
	if (argc != 5)
	{
		MessageBox(0, L"Incorrect number of parameters.  Command should be: <program> <width> <height> <MemMapBufferSize> <pipeName>", L"OpenCVB", MB_OK);
		MessageBox(0, L"Use OpenCVB as the startup project in Visual Studio", L"OpenCVB", MB_OK);
		return -1;
	}

	windowWidth = std::stoi(argv[1]);
	windowHeight = std::stoi(argv[2]);
	MemMapBufferSize = std::stoi(argv[3]);

	std::string pipeName(argv[4]);

	// setup named pipe interface
	std::string pipePrefix("\\\\.\\pipe\\");
	pipeName = pipePrefix + pipeName;
	std::wstring fullpipeName = s2ws(pipeName);
	pipe = CreateFile(fullpipeName.c_str(), GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
	TCHAR szName[] = TEXT("OpenCVBControl");
	hMapFile = CreateFileMapping(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, 0, MemMapBufferSize, szName);

	sharedMem = (double *)MapViewOfFile(hMapFile, FILE_MAP_ALL_ACCESS, 0, 0, MemMapBufferSize);
	fx = sharedMem[1];
	fy = sharedMem[2];
	ppx = sharedMem[3];
	ppy = sharedMem[4];

	printf("pipeName = %s\n", pipeName.c_str());
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
	imageWidth = (int)sharedMem[5];
	imageHeight = (int)sharedMem[6];

	if (totalMem == 0)
	{
		rgbBufferSize = (int)sharedMem[7];
		dataBufferSize = (int)sharedMem[8];
		pointcloudWidth = (int)sharedMem[36];
		pointcloudHeight = (int)sharedMem[37];
		textureBufferSize = (int)sharedMem[38];

		pcBufferSize = pointcloudWidth * pointcloudHeight * 12;
		totalMem = rgbBufferSize + dataBufferSize + textureBufferSize + pcBufferSize;

		if (rgbBufferSize > 0) rgbBuffer = (unsigned char*)malloc(rgbBufferSize);
		if (dataBufferSize > 0) dataBuffer = (float3*)malloc(dataBufferSize);
		if (textureBufferSize > 0) textureBuffer = (uint8_t*)malloc(textureBufferSize);
		if (pcBufferSize > 0) pointCloudBuffer = (float3*)malloc((int)pcBufferSize);

		printf("RGB size = %d data size = %d pointcloud size = %d texture size = %d w=%d h=%d\n", 
			   rgbBufferSize, dataBufferSize, pcBufferSize, textureBufferSize, imageWidth, imageHeight);
	}
	FOV = sharedMem[9];

	zNear = sharedMem[13];
	zFar = sharedMem[14];
	pointSize = (int)sharedMem[15];
	app_state.yaw = sharedMem[10];
	app_state.pitch = sharedMem[11];
	app_state.roll = sharedMem[12];
	
	gyro_data.x = (float)sharedMem[18];
	gyro_data.y = (float)sharedMem[19];
	gyro_data.z = (float)sharedMem[20];

	accel_data.x = (float)sharedMem[21];
	accel_data.y = (float)sharedMem[22];
	accel_data.z = (float)sharedMem[23];

	timestamp = sharedMem[24];
	IMU_Present = true; // always present

	Eye.x = (float)sharedMem[26];
	Eye.y = (float)sharedMem[27];
	Eye.z = (float)sharedMem[28];
	zTrans = (float)sharedMem[29];

	scaleXYZ.x = (float)sharedMem[30];
	scaleXYZ.y = (float)sharedMem[31];
	scaleXYZ.z = (float)sharedMem[32];

	timeConversionUnits = (float)sharedMem[33];
	imuAlphaFactor = (float)sharedMem[34];
	imageLabelBufferSize = (int)sharedMem[35];

	DWORD dwRead;

	if (rgbBufferSize > 0)
	{
		ReadFile(pipe, rgbBuffer, rgbBufferSize, &dwRead, NULL);
	}

	if (dataBufferSize > 0)
	{
		dataWidth = (int)sharedMem[16];
		dataHeight = (int)sharedMem[17];
		ReadFile(pipe, dataBuffer, dataBufferSize, &dwRead, NULL);
	}

	if (textureBufferSize > 0)
	{
		textureWidth = 256;
		textureHeight = 256;
		ReadFile(pipe, textureBuffer, textureBufferSize, &dwRead, NULL);
	}

	if (pcBufferSize > 0)
		ReadFile(pipe, pointCloudBuffer, (int)pcBufferSize, &dwRead, NULL);

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
	glLineWidth(5.0);
	glBegin(GL_LINES);
	glColor3f(1, 0, 0); glVertex3f(x, y, z); glVertex3f(axislen, y, z);
	glColor3f(0, 1, 0); glVertex3f(x, y, z); glVertex3f(x, -axislen, z);
	glColor3f(0, 0, 1); glVertex3f(x, y, z); glVertex3f(x, y, -axislen);
	glEnd();
}

static void draw_floor(int tileCount, GLint y, GLint z)
{
	glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
	glLineWidth(1.0);
	glBegin(GL_POLYGON);
	glColor4f(0.4f, 0.4f, 0.4f, 1.f);
	int tileHalf = (int)(tileCount / 2);
	// Render "floor" grid
	for (int i = 0; i <= tileCount; i++)
	{
		glVertex3i(i - tileHalf, y, z);
		glVertex3i(i - tileHalf, y, tileCount);
		glVertex3i(-tileHalf, y, i);
		glVertex3i(tileHalf, y, i);
	}
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
