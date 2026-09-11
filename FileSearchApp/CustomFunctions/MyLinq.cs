using System;
using System.Collections.Generic;
using System.Linq;
using CSharpNized.Rust.std;

namespace FileSearchApp
{
    public static class MyLinq
    {
        public static IOption<T> Get<T>(this IEnumerable<T> self, int pIndex)
        {
            try
            {
                return new Some<T>(self.ElementAt(pIndex));
            }
            catch
            {
                return new None<T>();
            }
        }
    }
}