using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Threading;

using Accord.MachineLearning.Bayes;
using Accord.Statistics.Distributions.Univariate;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;
using Accord.Neuro;
using Accord.MachineLearning.DecisionTrees.Learning;
using Accord.MachineLearning.DecisionTrees;
using Accord.Neuro.Learning;
using Accord.Statistics.Models.Regression;
using Accord.Statistics.Models.Regression.Fitting;
using Accord.Math;
using Accord.Statistics;
using Accord.Imaging.Filters;
using Accord;
using Accord.Imaging.Textures;
using Accord.Math.Wavelets;
using Accord.Math.Geometry;
using Accord.Imaging;
using Microsoft.Win32;
using System.Windows.Navigation;
using Accord.IO;
using Accord.Controls;
using Accord.MachineLearning;
using Accord.Math.Distances;
using Accord.Statistics.Distributions.DensityKernels;
using Accord.Statistics.Analysis;
using DlibDotNet;

namespace CS_Classes
{
    public class CS_DecisionTrees
    {
        public int[] answers;
        public void RunCS(double[][] inputs, int[] outputs)
        {
            C45Learning teacher = new C45Learning(new[] {
                DecisionVariable.Continuous("X"),
                DecisionVariable.Continuous("Y")
            });

            // Use the learning algorithm to induce the tree
            DecisionTree tree = teacher.Learn(inputs, outputs);

            // Classify the samples using the model
            answers = tree.Decide(inputs);
        }
    }
    public class CS_NaiveBayes
    {
        public int[] answers;
        public void RunCS(double[][] inputs, int[] outputs)
        {
            var teacher = new NaiveBayesLearning<NormalDistribution>();
            var nb = teacher.Learn(inputs, outputs);
            answers = nb.Decide(inputs);
        }
    }
    public class CS_SVMLinear
    {
        public bool[] answers;
        public void RunCS(double[][] inputs, int[] outputs)
        {
            var teacher = new LinearCoordinateDescent();
            var svm = teacher.Learn(inputs, outputs);
            answers = svm.Decide(inputs);
        }
    }
    public class CS_KernelMethod
    {
        public bool[] answers;
        public void RunCS(double[][] inputs, int[] outputs)
        {
            var teacher = new SequentialMinimalOptimization<Gaussian>()
            {
                UseComplexityHeuristic = true,
                UseKernelEstimation = true // estimate the kernel from the data
            };

            var svm = teacher.Learn(inputs, outputs);
            answers = svm.Decide(inputs);
        }
    }
    public class CS_MultiClass
    {
        public int[] answers;
        public void RunCS()
        {
            double[][] inputs = { new double[] { 0 }, new double[] { 3 }, new double[] { 1 }, new double[] { 2 }};
            int[] outputs = {0, 3, 1, 2};

            // Create the Multi-label learning algorithm for the machine
            var teacher = new MulticlassSupportVectorLearning<Linear>()
            {
                Learner = (p) => new SequentialMinimalOptimization<Linear>()
                {
                    Complexity = 10000.0 // Create a hard SVM
                }
            };

            // Learn a multi-label SVM using the teacher
            var svm = teacher.Learn(inputs, outputs);

            // Compute the machine answers for the inputs
            answers = svm.Decide(inputs);
        }
    }
    public class CS_MultiLabel
    {
        public bool[][] answers;
        public void RunCS()
        {
            double[][] inputs = { new double[] { 0 }, new double[] { 3 }, new double[] { 1 }, new double[] { 2 } };
            int[][] outputs = { new[] { -1, 1, -1 }, new[] { -1, -1, 1 }, new[] { 1, 1, -1 }, new[] { -1, -1, -1 } };

            // Create the Multi-label learning algorithm for the machine
            var teacher = new MultilabelSupportVectorLearning<Linear>()
            {
                Learner = (p) => new SequentialMinimalOptimization<Linear>()
                {
                    Complexity = 10000.0 // Create a hard SVM
                }
            };

            // Learn a multi-label SVM using the teacher
            var svm = teacher.Learn(inputs, outputs);
            answers = svm.Decide(inputs);
        }
    }

    public class CS_BipolarSigmoid
    {
        public bool[] answers;
        public void RunCS(double[][] inputs, int[] outputs)
        {
            // Since we would like to learn binary outputs in the form
            // [-1,+1], we can use a bipolar sigmoid activation function
            IActivationFunction function = new BipolarSigmoidFunction();

            // In our problem, we have 2 inputs (x, y pairs), and we will 
            // be creating a network with 5 hidden neurons and 1 output:
            var network = new ActivationNetwork(function, inputsCount: 2, neuronsCount: new[] { 5, 1 });

            // Create a Levenberg-Marquardt algorithm
            var teacher = new LevenbergMarquardtLearning(network)
            {
                UseRegularization = true
            };

            //// Because the network is expecting multiple outputs,
            //// we have to convert our single variable into arrays


            // This algorithm fails and it is clear that Accord no longer supports ToDouble.  Uncomment to see the problem.
            //var y = outputs.ToDouble().ToArray();




            //// Iterate until stop criteria is met
            double error = double.PositiveInfinity;
            double previous;

            do
            {
                previous = error;

                //Compute one learning iteration



                // without y defined above, this will not compile or run...
                //error = teacher.RunEpoch(inputs, y);




            } while (Math.Abs(previous - error) < 1e-10 * previous);
        }
    }
    public class CS_LogisticRegression
    {
        public bool[] answers;
        public void RunCS(double[][] inputs, int[] outputs)
        {
            // Create iterative re-weighted least squares for logistic regressions
            var teacher = new IterativeReweightedLeastSquares<LogisticRegression>()
            {
                MaxIterations = 100,
                Regularization = 1e-6
            };

            // Use the teacher algorithm to learn the regression:
            LogisticRegression lr = teacher.Learn(inputs, outputs);

            // Classify the samples using the model
            answers = lr.Decide(inputs);

            // Convert to Int32 so we can plot:
            int[] zeroOneAnswers = answers.ToZeroOne();
        }
    }
    public class CS_AccordSuite
    {
        public Bitmap RunCS(int index, Bitmap src)
        {
            switch (index)
            {
                case 0:
                    return Grayscale.CommonAlgorithms.BT709.Apply(src);
                case 1:
                    return new Sepia().Apply(src);
                case 2:
                    return new Invert().Apply(src);
                case 3:
                    return new RotateChannels().Apply(src);
                case 4:
                    return new ColorFiltering(new IntRange(25, 230), new IntRange(25, 230), new IntRange(25, 230)).Apply(src);

                case 5:
                    LevelsLinear filterLL = new LevelsLinear();

                    filterLL.InRed = new IntRange(30, 230);
                    filterLL.InGreen = new IntRange(50, 240);
                    filterLL.InBlue = new IntRange(10, 210);
                    return filterLL.Apply(src);

                case 6:
                    return new HueModifier(50).Apply(src);
                case 7:
                    return new SaturationCorrection(0.15f).Apply(src);
                case 8:
                    return new BrightnessCorrection().Apply(src);
                case 9:
                    return new ContrastCorrection().Apply(src);
                case 10:
                    return new HSLFiltering(new IntRange(330, 30), new Range(0, 1), new Range(0, 1)).Apply(src);

                case 11:
                    YCbCrLinear filter = new YCbCrLinear();
                    filter.InCb = new Range(-0.3f, 0.3f);
                    return filter.Apply(src);
                case 12:
                    return new YCbCrFiltering(new Range(0.2f, 0.9f), new Range(-0.3f, 0.3f), new Range(-0.3f, 0.3f)).Apply(src);
                case 13:
                    src = Grayscale.CommonAlgorithms.RMY.Apply(src);
                    return new Threshold().Apply(src);
                case 14:
                    src = Grayscale.CommonAlgorithms.RMY.Apply(src);
                    return new FloydSteinbergDithering().Apply(src);
                case 15:
                    src = Grayscale.CommonAlgorithms.RMY.Apply(src);
                    return new OrderedDithering().Apply(src);
                case 16:
                    return new Convolution(new int[,] {
                                { 1, 2, 3, 2, 1 },
                                { 2, 4, 5, 4, 2 },
                                { 3, 5, 6, 5, 3 },
                                { 2, 4, 5, 4, 2 },
                                { 1, 2, 3, 2, 1 } }).Apply(src);
                case 17:
                    return new Sharpen().Apply(src);
                case 18:
                    return new GaussianBlur(2.0, 7).Apply(src);
                case 19:
                    src = Grayscale.CommonAlgorithms.RMY.Apply(src);
                    return new DifferenceEdgeDetector().Apply(src);
                case 20:
                    src = Grayscale.CommonAlgorithms.RMY.Apply(src);
                    return new HomogenityEdgeDetector().Apply(src);

                case 21:
                    src = Grayscale.CommonAlgorithms.RMY.Apply(src);
                    return new SobelEdgeDetector().Apply(src);
                case 22:
                    return new Jitter().Apply(src);
                case 23:
                    return new OilPainting().Apply(src);
                case 24:
                    return new Texturer(new TextileTexture(), 1.0, 0.8).Apply(src);
                case 25:
                    // Spreading pixels values from 0 to 65535 instead of byte values for less losing data when applying the filter.
                    // Of course, we could use the source image in 8-bit (easiest and fastest way but slightly losing data).
                    //var bmp = Accord.Imaging.Image.Convert8bppTo16bpp(src);
                    var fastGuidedFilter = new FastGuidedFilter
                    {
                        KernelSize = 8,
                        Epsilon = 0.02f,
                        SubSamplingRatio = 0.25f,
                        OverlayImage = (Bitmap)src.Clone()
                    };

                    return fastGuidedFilter.Apply(src);
            }
            return src;
        }
    }
    public class CS_Wavelet
    {
        IWavelet wavelet;
        public Bitmap forwardImage;
        public Bitmap backwardImage;

        public void RunCS(Bitmap src, int iterations, bool useHaar)
        {
            Bitmap image = Accord.Imaging.Image.Convert8bppTo16bpp(src);

            if (useHaar) wavelet = new Haar(iterations); else wavelet = new CDF97(iterations);
            WaveletTransform wt = new WaveletTransform(wavelet);
            forwardImage = wt.Apply(image);

            WaveletTransform wtBack = new WaveletTransform(wavelet, true);
            backwardImage = wtBack.Apply(forwardImage);

            forwardImage = Accord.Imaging.Image.Convert16bppTo8bpp(forwardImage);
            backwardImage = Accord.Imaging.Image.Convert16bppTo8bpp(image);
        }
    }
    public class CS_Kohonen
    {
        public bool firstPass;
        private SOMLearning trainer;
        private DistanceNetwork network;
        private double fixedLearningRate;
        private Random rand = new Random();
        private double driftingLearningRate;
        public void RunCS(int iterations, ref int currentIteration, int radius)
        {
            trainer.LearningRate = driftingLearningRate * (iterations - currentIteration) / iterations + fixedLearningRate;
            trainer.LearningRadius = (double)radius * (iterations - currentIteration) / iterations;

            trainer.Run(new double[] {rand.Next(256), rand.Next(256), rand.Next(256)});
            currentIteration++;
        }
        public void initialize(int square, float learningRate )
        {
            network = new DistanceNetwork(3, square * square);
            // set random generators range
            foreach (var ilayer in network.Layers)
                foreach (var neuron in ilayer.Neurons)
                    neuron.RandGenerator = new UniformContinuousDistribution(new Range(0, 255));
            network.Randomize();

            trainer = new SOMLearning(network);
            fixedLearningRate = learningRate / 10;
            driftingLearningRate = fixedLearningRate * 9;
        }
        public Bitmap showResults(Bitmap mapBitmap, int square)
        {
            BitmapData mapData = mapBitmap.LockBits(ImageLockMode.ReadWrite);

            int stride = mapData.Stride;
            int offset = stride - square * 2 * 3;
            Layer layer = network.Layers[0];

            unsafe
            {
                byte* ptr = (byte*)mapData.Scan0;

                for (int y = 0, i = 0; y < square; y++)
                {
                    for (int x = 0; x < square; x++, i++, ptr += 6)
                    {
                        Neuron neuron = layer.Neurons[i];
                        ptr[2] = ptr[2 + 3] = ptr[2 + stride] = ptr[2 + 3 + stride] = (byte)Math.Max(0, Math.Min(255, neuron.Weights[0]));
                        ptr[1] = ptr[1 + 3] = ptr[1 + stride] = ptr[1 + 3 + stride] = (byte)Math.Max(0, Math.Min(255, neuron.Weights[1]));
                        ptr[0] = ptr[0 + 3] = ptr[0 + stride] = ptr[0 + 3 + stride] = (byte)Math.Max(0, Math.Min(255, neuron.Weights[2]));
                    }

                    ptr += offset;
                    ptr += stride;
                }
            }

            mapBitmap.UnlockBits(mapData);
            return mapBitmap;
        }
    }
    public class CS_HoughLines
    {
        HoughLineTransformation lineTransform = new HoughLineTransformation();
        HoughCircleTransformation circleTransform = new HoughCircleTransformation(35);

        // binarization filtering sequence
        private FiltersSequence filter = new FiltersSequence(
            Grayscale.CommonAlgorithms.BT709,
            new NiblackThreshold(),
            new Invert()
        );
        public Bitmap RunCS(Bitmap src, Bitmap binaryImage, short threshold, float relativeIntensity)
        {
            lineTransform.MinLineIntensity = threshold;

            BitmapData sourceData = binaryImage.LockBits(ImageLockMode.ReadOnly);
            lineTransform.ProcessImage(sourceData);
            binaryImage.UnlockBits(sourceData);

            sourceData = src.LockBits(ImageLockMode.ReadOnly);
            // get lines using relative intensity
            HoughLine[] lines = lineTransform.GetLinesByRelativeIntensity(relativeIntensity);

            foreach (HoughLine line in lines)
            {
                int r = line.Radius;
                double t = line.Theta;

                // check if line is in lower part of the image
                if (r < 0)
                {
                    t += 180;
                    r = -r;
                }

                // convert degrees to radians
                t = (t / 180) * Math.PI;

                // get image centers (all coordinate are measured relative
                // to center)
                int w2 = src.Width / 2;
                int h2 = src.Height / 2;

                double x0 = 0, x1 = 0, y0 = 0, y1 = 0;

                if (line.Theta != 0)
                {
                    // none vertical line
                    x0 = -w2; // most left point
                    x1 = w2;  // most right point

                    // calculate corresponding y values
                    y0 = (-Math.Cos(t) * x0 + r) / Math.Sin(t);
                    y1 = (-Math.Cos(t) * x1 + r) / Math.Sin(t);
                }
                else
                {
                    // vertical line
                    x0 = line.Radius;
                    x1 = line.Radius;

                    y0 = h2;
                    y1 = -h2;
                }

                Drawing.Line(sourceData, new IntPoint((int)x0 + w2, h2 - (int)y0), new IntPoint((int)x1 + w2, h2 - (int)y1), Color.Red);
            }
            src.UnlockBits(sourceData);
            return src;
        }
        public Bitmap FilterBinary(Bitmap image)
        {
            BitmapData sourceData = image.LockBits(ImageLockMode.ReadOnly);

            // binarize the image
            UnmanagedImage binarySource = filter.Apply(new UnmanagedImage(sourceData));

            image.UnlockBits(sourceData);
            return image;
        }
    }
    public class CS_KMeans
    {
        public int[] classifications;
        public void RunCS(Double[][] observations, int k)
        {
            KMeans kmeans = new KMeans(k);
            KMeansClusterCollection clustering = kmeans.Learn(observations);
            classifications = clustering.Decide(observations);
        }
    }
}
