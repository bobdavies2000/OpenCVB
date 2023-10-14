using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CS_Classes
{
    public class KNN
    {
        public List<float[]> trainingSetValues = new List<float[]>();
        public List<float[]> testSetValues = new List<float[]>();

        private int K;

        public void Classify(int neighborsNumber)
        {
            this.K = neighborsNumber;

            // create an array where we store the distance from our test data and the training data -> [0]
            // plus the index of the training data element -> [1]
            float[][] distances = new float[trainingSetValues.Count][];

            for (int i = 0; i < trainingSetValues.Count; i++)
                distances[i] = new float[2];

            Console.WriteLine("[i] classifying...");

            // start computing
            for (var test = 0; test < this.testSetValues.Count; test++)
            {
                Parallel.For(0, trainingSetValues.Count, index =>
                {
                    var dist = EuclideanDistance(this.testSetValues[test], this.trainingSetValues[index]);
                    distances[index][0] = dist;
                    distances[index][1] = index;
                }
                );

                // sort and select first K of them
                var sortedDistances = distances.AsParallel().OrderBy(t => t[0]).Take(this.K);
            }
        }

        private static float EuclideanDistance(float[] sampleOne, float[] sampleTwo)
        {
            float d = 0.0f;

            for (int i = 0; i < sampleOne.Length; i++)
            {
                float temp = sampleOne[i] - sampleTwo[i];
                d += temp * temp;
            }
            return (float)Math.Sqrt(d);
        }
    }
}
