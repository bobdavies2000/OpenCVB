#include "example.hpp"
#define NOGLFW
#include  "../OpenGL_Basics/OpenGLcommon.h"
#include <iostream>
#include <cmath>
#include <cfloat>
#include  "otherFunctions.h"
#include <opencv2/highgui.hpp>

int main(int argc, char* argv[])
{
	GLuint gl_handle = 0;
	windowTitle << "OpenGL_Functions";
	initializeNamedPipeAndMemMap(argc, argv);

	window app(windowWidth, windowHeight, windowTitle.str().c_str());
	glfw_state MyState;
	register_glfw_callbacks(app, MyState);

	Sleep(10);
	myHwnd = FindWindow(NULL, L"OpenGL_Functions");
	LoadWindowPosition(myHwnd);

	while (app)
	{
		glfwPollEvents();
		readPipeAndMemMap();

		glPointSize((float)pointSize);
		glPushAttrib(GL_ALL_ATTRIB_BITS);

		if (rgbBufferSize != 0)
			rgb.upload(rgbBuffer, imageWidth, imageHeight);

		glLoadIdentity();

		glClearColor(0.7f, 0.7f, 0.7f, 1);
		glClear(GL_DEPTH_BUFFER_BIT);

		glMatrixMode(GL_PROJECTION);
		glPushMatrix();
		gluPerspective(FOV, imageWidth / imageHeight, zNear + 0.01, zFar);
		glMatrixMode(GL_MODELVIEW);
		glPushMatrix();
		gluLookAt(Eye.x, Eye.y, Eye.z, 0, 0, 10, 0, -1, 0);

		glTranslatef(0, 0, +1.5f + MyState.offset_y * 0.05f);
		glRotated(MyState.pitch, 1, 0, 0);
		glRotated(MyState.yaw, 0, 1, 0);
		glTranslatef(0, 0, -0.5f);

		glEnable(GL_DEPTH_TEST);
		glEnable(GL_TEXTURE_2D);
		glBindTexture(GL_TEXTURE_2D, rgb.get_gl_handle());
		float tex_border_color[] = { 0.8f, 0.8f, 0.8f, 0.8f };
		glTexParameterfv(GL_TEXTURE_2D, GL_TEXTURE_BORDER_COLOR, tex_border_color);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, 0x812F); // GL_CLAMP_TO_EDGE
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, 0x812F); // GL_CLAMP_TO_EDGE

		float xx, yy, zz;
		float* data = (float*)dataBuffer;
		int pairCount = (int)(dataBufferSize / 6);
		int dataCount = (int)(dataBufferSize / 12); // 12 bytes per float3 entries = number of float3 entries.
		float3* dataBuff = (float3*)dataBuffer; int index;
		bool drawFloorNeeded = true;
		switch (oglFunction) 
		{
			case 0: // oCase.drawPointCloudRGB - default case draws the pointcloud with the RGB providing texture.
			{
				drawPointCloudRGB();
				glDisable(GL_TEXTURE_2D);
				break;
			}
			case 1: // oCase.drawLineAndCloud - draw vertical lines oCase.drawLineAndCloud
			{
				drawPointCloudRGB();
				glDisable(GL_TEXTURE_2D);

				// draw lines provided in the data buffer
				glLineWidth(50.0);
				glBegin(GL_LINES);
				for (int i = 0; i < pairCount; i += 6)
				{
					glColor3f(1, 1, 1); glVertex3fv(&data[i]); glVertex3fv(&data[i + 3]);
				}
				glEnd();
				break;
			}
			case 2: // oCase.drawFloor - draw floor plane oCase.drawFloor
			{
				drawPointCloudRGB();
				glDisable(GL_TEXTURE_2D);

				// draw and texture the floor --------------------------------------------------------------------------------------------------------
				glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
				glEnable(GL_BLEND);
				glMatrixMode(GL_TEXTURE);
				glEnable(GL_TEXTURE_2D);

				glColor4f(1, 1, 1, 1);
				glBindTexture(GL_TEXTURE_2D, tBuffer.get_gl_handle());
				glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
				glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);

				glBegin(GL_POLYGON);
				xx = 10;
				yy = data[3];
				zz = 10;
				glTexCoord2f(0.0f, 10.0f); glVertex3f(-xx, yy, zz);
				glTexCoord2f(10.0f, 0.0f); glVertex3f(-xx, yy, 0);
				glTexCoord2f(0.0f, 0.0f);  glVertex3f(xx, yy, 0);
				glTexCoord2f(10.0f, 10.0f); glVertex3f(xx, yy, zz);
				glEnd();
				glDisable(GL_TEXTURE_2D);
				glDisable(GL_BLEND);
				break;
			}
			case 3: // oCase.trianglesAndColor - oCase.trianglesAndColor
			{
				GLfloat* glData = (GLfloat*)dataBuffer;
				int glCount = (int)(dataBufferSize / 4);
				
				glDisable(GL_TEXTURE_2D);
				glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
				glBegin(GL_TRIANGLES);
				for (int i = 0; i < glCount; i += 12)
				{
					glColor3f(glData[i + 2] / 255, glData[i + 1] / 255, glData[i] / 255); // BGR to RGB tossed in...
					glVertex3f(glData[i + 3], glData[i + 4], glData[i + 5]);
					glVertex3f(glData[i + 6], glData[i + 7], glData[i + 8]);
					glVertex3f(glData[i + 9], glData[i + 10], glData[i + 11]);
				}

				glEnd();
				break;
			}

			case 4: // oCase.drawPyramid - OpenGL_Pyramid
			{
				glBegin(GL_TRIANGLES);
				glColor3f(1.0f, 0.0f, 0.0f); glVertex3f(0.0f, -1.f, 1.0f); // Red triangle
				glColor3f(1.0f, 0.0f, 0.0f); glVertex3f(-1.0f, 0.0f, 2.0f);
				glColor3f(1.0f, 0.0f, 0.0f); glVertex3f(1.0f, 0.0f, 2.0f);

				glColor3f(1.0f, 1.0f, 0.0f); glVertex3f(0.0f, -1.0f, 1.0f); // Yellow triangle
				glColor3f(1.0f, 1.0f, 0.0f); glVertex3f(-1.0f, 0.0f, 2.0f);
				glColor3f(1.0f, 1.0f, 0.0f); glVertex3f(0.0f, 0.0f, 0.0f);

				glColor3f(0.0f, 0.0f, 1.0f); glVertex3f(0.0f, -1.0f, 1.0f); // Blue triangle
				glColor3f(0.0f, 0.0f, 1.0f); glVertex3f(1.0f, 0.0f, 2.0f);
				glColor3f(0.0f, 0.0f, 1.0f); glVertex3f(0.0f, 0.0f, 0.0f);

				glColor3f(0.0f, 1.0f, 0.0f); glVertex3f(-1.0f, 0.0f, 2.0f); // Green triangle
				glColor3f(0.0f, 1.0f, 0.0f); glVertex3f(1.0f, 0.0f, 2.0f);
				glColor3f(0.0f, 1.0f, 0.0f); glVertex3f(0.0f, 0.0f, 0.0f);
				glEnd();
				break;
			}
			case 5: // oCase.drawCube - OpenGL_DrawCube
			{
				glBegin(GL_QUADS);
				// Begin drawing the color cube with 6 quads
				//  Top face (y = 1.0f)
				// Define vertices in counter-clockwise (CCW) order with normal pointing out
				glColor3f(0.0f, 1.0f, 0.0f);     // Green
				glVertex3f(0.25f, 0.25f, 0.25f);
				glVertex3f(-0.25f, 0.25f, 0.25f);
				glVertex3f(-0.25f, 0.25f, 0.75f);
				glVertex3f(0.25f, 0.25f, 0.75f);

				// Bottom face (y = -1.0f - 1.0f)
				glColor3f(1.0f, 0.5f, 0.0f);     // Orange
				glVertex3f(0.25f, -0.25f, 0.75f);
				glVertex3f(-0.25f, -0.25f, 0.75f);
				glVertex3f(-0.25f, -0.25f, 0.25f);
				glVertex3f(0.25f, -0.25f, 0.25f);

				// Front face  (z = 1.0f - 1.0f)
				glColor3f(1.0f, 0.0f, 0.0f);     // Red
				glVertex3f(0.25f, 0.25f, 0.75f);
				glVertex3f(-0.25f, 0.25f, 0.75f);
				glVertex3f(-0.25f, -0.25f, 0.75f);
				glVertex3f(0.25f, -0.25f, 0.75f);

				// Back face (z = -1.0f - 1.0f)
				glColor3f(1.0f, 1.0f, 0.0f);     // Yellow
				glVertex3f(0.25f, -0.25f, 0.25f);
				glVertex3f(-0.25f, -0.25f, 0.25f);
				glVertex3f(-0.25f, 0.25f, 0.25f);
				glVertex3f(0.25f, 0.25f, 0.25f);

				// Left face (x = -1.0f - 1.0f)
				glColor3f(0.0f, 0.0f, 1.0f);     // Blue
				glVertex3f(-0.25f, 0.25f, 0.75f);
				glVertex3f(-0.25f, 0.25f, 0.25f);
				glVertex3f(-0.25f, -0.25f, 0.25f);
				glVertex3f(-0.25f, -0.25f, 0.75f);

				// Right face (x = 1.0f - 1.0f)
				glColor3f(1.0f, 0.0f, 1.0f);     // Magenta
				glVertex3f(0.25f, 0.25f, 0.25f);
				glVertex3f(0.25f, 0.25f, 0.75f);
				glVertex3f(0.25f, -0.25f, 0.75f);
				glVertex3f(0.25f, -0.25f, 0.25f);
				glEnd();
				break;
			}
			case 6: // oCase.simplePlane - OpenGL_QuadSimple
			//{
			//	glDisable(GL_TEXTURE_2D);
			//	glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
			//	glBegin(GL_QUADS);
			//	for (int i = 0; i < dataCount; i++)
			//	{
			//		if (i % 5 == 0)
			//		{
			//			index = i;
			//		}
			//		else
			//		{
			//			glColor3fv((GLfloat*)&dataBuff[index]);
			//			glVertex3fv((GLfloat*)&dataBuff[i]);
			//		}
			//	}

			//	glEnd();
			//	break;
			//}
			{
				GLfloat* quadData = (GLfloat*)dataBuffer;
				int quadCount = (int)(dataBufferSize / 4);
				glDisable(GL_TEXTURE_2D);
				glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
				glBegin(GL_QUADS);
				for (int i = 0; i < quadCount; i += 15)
				{
					glColor3f(quadData[i + 2] / 255, quadData[i + 1] / 255, quadData[i] / 255); // BGR to RGB tossed in...
					glVertex3f(quadData[i + 3], quadData[i + 4], quadData[i + 5]);
					glVertex3f(quadData[i + 6], quadData[i + 7], quadData[i + 8]);
					glVertex3f(quadData[i + 9], quadData[i + 10], quadData[i + 11]);
					glVertex3f(quadData[i + 12], quadData[i + 13], quadData[i + 14]);
				}

				glEnd();
				break;
			}
			
			case 7: // oCase.minMaxBlocks 
			{
				glDisable(GL_TEXTURE_2D);
				glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
				glBegin(GL_QUADS);
				for (int i = 0; i < dataCount; i++)
				{
					if (i % 25 == 0)
					{
						index = i;
					}
					else
					{
						glColor3fv((GLfloat*)&dataBuff[index]);
						glVertex3fv((GLfloat*)&dataBuff[i]);
					}
				}
				glEnd();
				break;
			}
			case 8: // oglFunction = 8 - oCase.drawTiles
			{
				glDisable(GL_TEXTURE_2D);
				glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
				glBegin(GL_POINTS);
				for (int i = 0; i < dataCount; i++)
				{
					if (i % 2 == 0)
					{
						index = i;
					}
					else
					{
						glColor3fv((GLfloat*)&dataBuff[index]);
						glVertex3fv((GLfloat*)&dataBuff[i]);
					}
				}
				glEnd();
				break;
			}

			case 10: // oglFunction = 10  oCase.drawCells
			{
				glDisable(GL_TEXTURE_2D);
				glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
				glEnable(GL_BLEND);
				glMatrixMode(GL_TEXTURE);
				glEnable(GL_TEXTURE_2D);

				glBindTexture(GL_TEXTURE_2D, tBuffer.get_gl_handle());
				glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
				glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);

				int index = 0;
				float3 count = dataBuff[index++];
				int polygonCount = (int)count.x;
				int cellCount;

				for (int p = 0; p < polygonCount; p++)
				{
					count = dataBuff[index++];
					cellCount = (int)count.x;
					int colorIndex = index++;
					glBegin(GL_POLYGON);

					glColor4fv((GLfloat*)&dataBuff[colorIndex]);
					for (int i = 0; i < cellCount; i++)
					{
						glVertex3fv((GLfloat*)&dataBuff[index + i]);
						glVertex3fv((GLfloat*)&dataBuff[index + (i + 1) % cellCount]);
						glVertex3fv((GLfloat*)&dataBuff[index + (i + 2) % cellCount]);
					}
					glEnd();
					index += cellCount;
				}
				glDisable(GL_TEXTURE_2D);
				glDisable(GL_BLEND);
				break;
			}
			case 11: // oglFunction = 11  oCase.floorStudy
			{
				drawFloorNeeded = false;
				drawPointCloudRGB();
				glDisable(GL_TEXTURE_2D);
				glColor4f(0.0f, 0.0f, 1.0f, 0.7f); // floor
				drawFlatPlane(data[0]);
				if (dataBufferSize > 4)
				{
					glColor4f(1.0f, 0.0f, 0.0f, 0.9f); // ceiling
					drawFlatPlane(data[1]);
				}
				break;
			}
			case 12: // oglFunction = 12 oCase.data3D
			{
				glClear(GL_COLOR_BUFFER_BIT);
				glBegin(GL_POINTS);

				int points = int(dataBufferSize / sizeof(float));
				float* data = (float*)dataBuffer;
				for (int i = 0; i < points; i += 3)
				{
					float ptData[3] = { 1, float(i / points), float(i / points) };
					glColor3fv(ptData);
					glVertex3fv((GLfloat*) &data[i]);
				}
				glEnd();
				break;
			}
			case 13: // oglFunction = oCase.sierpinski
			{
				glDisable(GL_TEXTURE_2D);
				drawSierpinski();
				break;
			}
		case 14: // oglFunction = oCase.polygonCell
			{
				glDisable(GL_TEXTURE_2D);
				glBegin(GL_POLYGON);

				for (int i = 1; i < dataCount; i++) // first element is the color so start at 1
				{
					if (dataBuff[i].z > 0)
					{
						glColor3fv((GLfloat*)&dataBuff[0]);
						glVertex3fv((GLfloat*)&dataBuff[i]);
					}
				}

				glEnd();
				glFlush();
				break;
			}
		case 15: // oglFunction = oCase.Histogram3D
			{
				glClear(GL_COLOR_BUFFER_BIT);
				glBegin(GL_POINTS);

				int points = int(dataBufferSize / sizeof(float));
				float histogramBins = float(cbrt(points)); // length=bins*bins*bins so find cube root of bins first.
				float* data = (float*)dataBuffer;
				float scale = 1;
				float shiftY = -1;
				for (int x = 0; x < histogramBins; x++)
				{
					for (int y = 0; y < histogramBins; y++)
					{
						for (int z = 0; z < histogramBins; z++)
						{
							float val = data[int(x * histogramBins * histogramBins) + int(y * histogramBins) + z];
							if (val > 0)
							{
								glColor3f(GLfloat(x / histogramBins), GLfloat(y / histogramBins), GLfloat(z / histogramBins));
								glVertex3f(GLfloat( scale * x / histogramBins), GLfloat(shiftY + scale * y / histogramBins), GLfloat(scale * z / histogramBins));
							}
						}
					}
				}
				glEnd();
				break;
			}
		case 16: // oglFunction = oCase.pcPoints
			{
				glBegin(GL_POINTS);

				float* pc = (float*)dataBuffer;
				int floatCount = int(dataBufferSize / sizeof(float));
				for (int i = 0; i < floatCount; i+=6)
				{
					glColor3fv((GLfloat*)(&pc[i]));
					glVertex3fv((GLfloat *)(&pc[i + 3]));
					// printf("color = %0.0f, %0.0f, %0.0f, pt = %f, %f, %f\n", pc[i], pc[i + 1], pc[i + 2], pc[i + 3], pc[i + 4], pc[i + 5]);
				}
				glEnd();
				glDisable(GL_TEXTURE_2D);
				break;
			}
		case 17: // oglFunction = oCase.pcLines
			{
				float* pc = (float*)dataBuffer;
				int floatCount = int(dataBufferSize / sizeof(float));
				glLineWidth((GLfloat) 5); 
				for (int i = 0; i < floatCount; i += 9)
				{
					GLfloat color[3] = { GLfloat(pc[i]), GLfloat(pc[i + 1]), GLfloat(pc[i + 2]) };
					if (color[0] != 0 || color[1] != 0 || color[2] != 0)
					{
						glBegin(GL_LINES);
						glColor3fv(color);
						glVertex3fv((GLfloat*)(&pc[i + 3]));
						glVertex3fv((GLfloat*)(&pc[i + 6]));
						glEnd();
					}
				}
				glDisable(GL_TEXTURE_2D);
				break;
			}
		case 18: // oglFunction = oCase.pcPointsAlone
			{
				glBegin(GL_POINTS);
				float* pc = (float*)dataBuffer;
				int floatCount = int(dataBufferSize / sizeof(float));
				GLfloat color[3] = { GLfloat(1), GLfloat(1), GLfloat(1) };
				for (int i = 0; i < floatCount; i += 3)
				{
					if (pc[i + 2] != 0)
					{
						glColor3fv(color);
						glVertex3fv((GLfloat*)(&pc[i]));
					}
				}
				glEnd();
				glDisable(GL_TEXTURE_2D);
				break;
			}

		case 19: // drawLines - draw vertical lines oCase.drawLines
			{
				glDisable(GL_TEXTURE_2D);

				// draw lines provided in the data buffer
				glLineWidth(50.0);
				glBegin(GL_LINES);
				int lineCount = (int)(data[0]);
				for (int i = 1; i < lineCount; i += 9)
				{
					glColor3f(data[i], data[i+1], data[i+2]); 
					glVertex3fv(&data[i+3]); 
					glVertex3fv(&data[i+6]);
				}
				glEnd();
				break;
			}
		case 20: // oCase.drawPointCloudRGB - can also try oCase.drawAvgPointCloudRGB
			{
				drawAvgPointCloud();
				glDisable(GL_TEXTURE_2D);
				break;
			}

		} // end of the switch statement

		glEnable(GL_BLEND);
		glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

		drawAxes(10, 0, 0, 1);

		if (drawFloorNeeded) draw_floor();

		glPopMatrix();
		glMatrixMode(GL_PROJECTION);
		glPopMatrix();
		glPopAttrib();

		glfwSetWindowTitle(app, imageLabel);
		if (ackBuffers()) break;
	} // end of the while loop...

	SaveWindowPosition(myHwnd);
	CloseHandle(hMapFile);
	CloseHandle(pipe);
	return EXIT_SUCCESS;
}
