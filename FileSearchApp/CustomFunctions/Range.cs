using System;
using System.Threading;

namespace CustomFunctions
{
    public struct Range
    {
        readonly int From;
        readonly int To;
        public Range(int from, int to)
        {
            if (from > to){
                throw new Exception("from は to より小さくしてください");
            }
            this.From = from;
            this.To = to;
        }
        public bool IsInRange(int value)
        {
            return this.From <= value && this.To >= value;
        }
        public float GetPercentile(int value)
        {
            var range = this.To - this.From;
            return (float)(value - this.From) / range;
        }
    }
}