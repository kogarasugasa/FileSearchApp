using System.Collections.Generic;

namespace CustomFunctions
{
    public static class Prime
    {
        public static List<long> GetPrimes(long pTake)
        {
            long cnt = 0;
            long num = 0;
            var primes = new List<long>();
            while (cnt < pTake)
            {
                if (IsPrime(num)){
                    primes.Add(num);
                    cnt++;
                }
                num++;
            }
            return primes;
        }
        /// <summary>
        /// 素数であるか判定する
        /// </summary>
        public static bool IsPrime(this long n)
        {
            if (n == 1 || n == 0){
                return false;
            }
            for (long i = 2; i * i <= n; i++)
            {
                if (n % i == 0){
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 素数であるか判定する
        /// </summary>
        public static bool IsPrime(this int n)
        {
            return IsPrime((long)n);
        }
    }
}