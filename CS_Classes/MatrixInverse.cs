//https://visualstudiomagazine.com/articles/2020/04/06/invert-matrix.aspx
using System;
using cv = OpenCvSharp;
namespace CS_Classes
{
    public class MatrixInverse
    {
        public double[] bVector;
        public double[] solution;

        cv.Mat inverse;
        public cv.Mat RunCS(cv::Mat m)
        {
            bool showIntermediate = false; // turn this on if further detail is needed.
            double d = MatDeterminant(m);
            if (Math.Abs(d) < 1.0e-5)
                if (showIntermediate) Console.WriteLine("\nMatrix has no inverse");
                else
                if (showIntermediate) Console.WriteLine("\nDet(m) = " + d.ToString("F4"));

            inverse = MatInverse(m);

            cv.Mat prod = MatProduct(m, inverse);
            if (showIntermediate)
            {
                Console.WriteLine("\nThe product of m * inv is ");
                MatShow(prod, 1, 6);
            }

            cv.Mat lum;
            int[] perm;
            int toggle = MatDecompose(m, out lum, out perm);
            if (showIntermediate)
            {
                Console.WriteLine("\nThe combined lower-upper decomposition of m is");
                MatShow(lum, 4, 8);
            }

            cv.Mat lower = ExtractLower(lum);
            cv.Mat upper = ExtractUpper(lum);

            if (showIntermediate)
            {
                solution = MatVecProd(inverse, bVector);  // (1, 0, 2, 1)
                Console.WriteLine("\nThe lower part of LUM is");
                MatShow(lower, 4, 8);

                Console.WriteLine("\nThe upper part of LUM is");
                MatShow(upper, 4, 8);

                Console.WriteLine("\nThe perm[] array is");
                VecShow(perm, 4);

                cv.Mat lowUp = MatProduct(lower, upper);
                Console.WriteLine("\nThe product of lower * upper is ");
                MatShow(lowUp, 4, 8);

                Console.WriteLine("\nVector b = ");
                VecShow(bVector, 1, 8);

                Console.WriteLine("\nSolving m*x = b");

                Console.WriteLine("\nSolution x = ");
                VecShow(solution, 1, 8);
            }
            return inverse;
        }

        static cv.Mat MatInverse(cv.Mat m)
        {
            // assumes determinant is not 0
            // that is, the matrix does have an inverse
            int n = m.Rows;
            cv.Mat result = m.Clone();

            cv.Mat lum; // combined lower & upper
            int[] perm;  // out parameter
            MatDecompose(m, out lum, out perm);  // ignore return

            double[] b = new double[n];
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                    if (i == perm[j])
                        b[j] = 1.0;
                    else
                        b[j] = 0.0;

                double[] x = Reduce(lum, b); // 
                for (int j = 0; j < n; ++j)
                    result.Set<double>(j, i, x[j]);
            }
            return result;
        }

        static int MatDecompose(cv.Mat m, out cv.Mat lum, out int[] perm)
        {
            // Crout's LU decomposition for matrix determinant and inverse
            // stores combined lower & upper in lum[][]
            // stores row permuations into perm[]
            // returns +1 or -1 according to even or odd number of row permutations
            // lower gets dummy 1.0s on diagonal (0.0s above)
            // upper gets lum values on diagonal (0.0s below)

            int toggle = +1; // even (+1) or odd (-1) row permutatuions
            int n = m.Rows;

            // make a copy of m[][] into result lum[][]
            lum = m.Clone();

            // make perm[]
            perm = new int[n];
            for (int i = 0; i < n; ++i)
                perm[i] = i;

            for (int j = 0; j < n - 1; ++j) // process by column. note n-1 
            {
                double max = Math.Abs(lum.At<double>(j, j));
                int piv = j;

                for (int i = j + 1; i < n; ++i) // find pivot index
                {
                    double xij = Math.Abs(lum.At<double>(i, j));
                    if (xij > max)
                    {
                        max = xij;
                        piv = i;
                    }
                } // i

                if (piv != j)
                {
                    cv.Mat tmp = lum.Row(piv).Clone(); // swap rows j, piv
                    lum.Row(j).CopyTo(lum.Row(piv));
                    tmp.CopyTo(lum.Row(j));

                    int t = perm[piv]; // swap perm elements
                    perm[piv] = perm[j];
                    perm[j] = t;

                    toggle = -toggle;
                }

                double xjj = lum.At<double>(j, j);
                if (xjj != 0.0)
                {
                    for (int i = j + 1; i < n; ++i)
                    {
                        double xij = lum.At<double>(i, j) / xjj;
                        lum.Set<double>(i, j, xij);
                        for (int k = j + 1; k < n; ++k)
                            lum.Set<double>(i, k, lum.At<double>(i, k) - xij * lum.At<double>(j, k));
                    }
                }
            }

            return toggle;  // for determinant
        }

        static double[] Reduce(cv.Mat luMatrix, double[] b) // helper
        {
            int n = luMatrix.Rows;
            double[] x = new double[n];
            for (int i = 0; i < n; ++i)
                x[i] = b[i];

            for (int i = 1; i < n; ++i)
            {
                double sum = x[i];
                for (int j = 0; j < i; ++j)
                    sum -= luMatrix.At<double>(i, j) * x[j];
                x[i] = sum;
            }

            x[n - 1] /= luMatrix.At<double>(n - 1, n - 1);
            for (int i = n - 2; i >= 0; --i)
            {
                double sum = x[i];
                for (int j = i + 1; j < n; ++j)
                    sum -= luMatrix.At<double>(i, j) * x[j];
                x[i] = sum / luMatrix.At<double>(i, i);
            }

            return x;
        }

        static double MatDeterminant(cv.Mat m)
        {
            cv.Mat lum;
            int[] perm;

            double result = MatDecompose(m, out lum, out perm);  // impl. cast
            for (int i = 0; i < lum.Rows; ++i)
                result *= lum.At<double>(i, i);
            return result;
        }

        static cv.Mat MatProduct(cv.Mat matA, cv.Mat matB)
        {
            int aRows = matA.Rows;
            int aCols = matA.Cols;
            int bRows = matB.Rows;
            int bCols = matB.Cols;
            if (aCols != bRows)
                throw new Exception("Non-conformable matrices");

            cv.Mat result = new cv.Mat(aRows, bCols, cv.MatType.CV_64F, 0);

            for (int i = 0; i < aRows; ++i) // each row of A
                for (int j = 0; j < bCols; ++j) // each col of B
                    for (int k = 0; k < aCols; ++k) // could use bRows
                        result.Set<double>(i, j, result.At<double>(i, j) + matA.At<double>(i, k) * matB.At<double>(k, j));

            return result;
        }

        static double[] MatVecProd(cv.Mat m, double[] v)
        {
            int n = v.Length;
            if (m.Cols != n)
                throw new Exception("non-comform in MatVecProd");

            double[] result = new double[n];

            for (int i = 0; i < m.Rows; ++i)
            {
                for (int j = 0; j < m.Cols; ++j)
                {
                    result[i] += m.At<double>(i, j) * v[j];
                }
            }
            return result;
        }

        static cv.Mat ExtractLower(cv.Mat lum)
        {
            // lower part of an LU Crout's decomposition
            // (dummy 1.0s on diagonal, 0.0s above)
            int n = lum.Rows;
            cv.Mat result = lum.Clone().SetTo(0);
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    if (i == j)
                        result.Set<double>(i, j, 1.0);
                    else if (i > j)
                        result.Set<double>(i, j, lum.At<double>(i, j));
                }
            }
            return result;
        }

        static cv.Mat ExtractUpper(cv.Mat lum)
        {
            // upper part of an LU (lu values on diagional and above, 0.0s below)
            int n = lum.Rows;
            cv.Mat result = lum.Clone().SetTo(0);
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    if (i <= j)
                        result.Set<double>(i, j, lum.At<double>(i, j));
                }
            }
            return result;
        }

        static void MatShow(cv.Mat m, int dec, int wid)
        {
            for (int i = 0; i < m.Rows; ++i)
            {
                for (int j = 0; j < m.Cols; ++j)
                {
                    double v = m.At<double>(i, j);
                    if (Math.Abs(v) < 1.0e-5) v = 0.0;  // avoid "-0.00"
                    Console.Write(v.ToString("F" + dec).PadLeft(wid));
                }
                Console.WriteLine("");
            }
        }

        static void VecShow(int[] vec, int wid)
        {
            for (int i = 0; i < vec.Length; ++i)
                Console.Write(vec[i].ToString().PadLeft(wid));
            Console.WriteLine("");
        }

        static void VecShow(double[] vec, int dec, int wid)
        {
            for (int i = 0; i < vec.Length; ++i)
            {
                double x = vec[i];
                if (Math.Abs(x) < 1.0e-5) x = 0.0;  // avoid "-0.00"
                Console.Write(x.ToString("F" + dec).PadLeft(wid));
            }
            Console.WriteLine("");
        }
    }

}
