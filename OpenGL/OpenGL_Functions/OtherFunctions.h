#include "example.hpp"
// A simple two-dimensional point class to make life easy.  It allows you to
// reference points with x and y coordinates instead of array indices) and
// encapsulates a midpoint function.
struct sPoint {
	GLfloat x, y;
	sPoint(GLfloat x = 0, GLfloat y = 0) : x(x), y(y) {}
	sPoint midpoint(sPoint p) { return sPoint(GLfloat((x + p.x) / 2.0f), GLfloat((y + p.y) / 2.0f)); }
};

// Draws a Sierpinski triangle with a fixed number of points. (Note that the
// number of points is kept fairly small because a display callback should
// NEVER run for too long.
// https://cs.lmu.edu/~ray/notes/openglexamples/
void drawSierpinski() {
	glClear(GL_COLOR_BUFFER_BIT);

	static sPoint vertices[] = { sPoint(0, 0), sPoint(200, 500), sPoint(500, 0) };

	// Compute and plot 100000 new points, starting (arbitrarily) with one of
	// the vertices. Each point is halfway between the previous point and a
	// randomly chosen vertex.
	static sPoint p = vertices[0];
	glBegin(GL_POINTS);
	float pt[3];
	pt[2] = 5.0f;
	float offset = 1.5; float f = 0.01f;
	for (int i = 0; i < 100000; i++) {
		p = p.midpoint(vertices[rand() % 3]);
		pt[0] = f * p.x - offset; pt[1] = f * p.y - offset;
		glVertex3fv(pt);
	}
	glEnd();
	glFlush();
}