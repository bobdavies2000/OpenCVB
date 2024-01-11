#include <math.h>
#include <stdio.h>
#include <stdlib.h>
#include "OpenCVB_Extern.h"
// https://machinelearningmastery.com/a-gentle-introduction-to-the-jacobian/#:~:text=The%20Jacobian%20matrix%20collects%20all,one%20coordinate%20space%20and%20another.
// https://github.com/oneapi-src/oneAPI-samples/blob/master/DirectProgramming/DPC%2B%2B/DenseLinearAlgebra/jacobi_iterative/sycl_dpct_migrated/src/jacobi.h
// https://en.wikipedia.org/wiki/Jacobian_matrix_and_determinant
#define N_ROWS 512

// creates N_ROWS x N_ROWS matrix A with N_ROWS+1 on the diagonal and 1
// elsewhere. The elements of the right hand side b all equal 2*n, hence the
// exact solution x to A*x = b is a vector of ones.
void createLinearSystem(float* A, double* b) {
    int i, j;
    for (i = 0; i < N_ROWS; i++) {
        b[i] = 2.0 * N_ROWS;
        for (j = 0; j < N_ROWS; j++) A[i * N_ROWS + j] = 1.0;
        A[i * N_ROWS + i] = N_ROWS + 1.0;
    }
}

// Run the Jacobi method for A*x = b on CPU.
void JacobiMethodCPU(float* A, double* b, float conv_threshold, int max_iter,
    int* num_iter, double* x) {
    double* x_new = new double[N_ROWS];
    for (int i = 0; i < N_ROWS; i++) x_new[i] = 0;
    int k;

    for (k = 0; k < max_iter; k++) {
        double sum = 0.0;
        for (int i = 0; i < N_ROWS; i++) {
            double temp_dx = b[i];
            for (int j = 0; j < N_ROWS; j++) temp_dx -= A[i * N_ROWS + j] * x[j];
            temp_dx /= A[i * N_ROWS + i];
            x_new[i] += temp_dx;
            sum += fabs(temp_dx);
        }

        for (int i = 0; i < N_ROWS; i++) x[i] = x_new[i];

        if (sum <= conv_threshold) break;
    }
    *num_iter = k + 1;
}

int test(int argc, char** argv) {
    // Host variable declaration and allocation
    double* b = NULL;
    float* A = NULL;

    createLinearSystem(A, b);
    double* x = NULL;

    // start with array of all zeroes
    x = (double*)calloc(N_ROWS, sizeof(double));

    float conv_threshold = 1.0e-2f;
    int max_iter = 4 * N_ROWS * N_ROWS;
    int cnt = 0;

    JacobiMethodCPU(A, b, conv_threshold, max_iter, &cnt, x);
    return 0;
}
