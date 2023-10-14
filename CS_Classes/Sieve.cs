using System.Collections.Generic;
using System.Linq;

// https://github.com/TheAlgorithms/C-Sharp/blob/master/Algorithms/Other/SieveOfEratosthenes.cs'
namespace CS_Classes
{
    public class Sieve
    {
        public List<int> GetPrimeNumbers(int count)
        {
            var output = new List<int>();
            for (int n = 2; output.Count < count; n++)
            {
                if (output.All(x => n % x != 0)) output.Add(n);
            }
            return output;
        }
    }
}
