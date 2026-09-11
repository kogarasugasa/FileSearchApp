using System;
using System.Diagnostics.Contracts;

namespace CSharpNized.Rust.std
{
    public sealed class None<T> : IOption<T>
    {
        const string NONE_DESC = "None";
        public void Match(Action<string> none, Action<T> some)
        {
            none(NONE_DESC);
        }
        public T Match(Func<string, T> none, Func<T, T> some)
        {
            return none(NONE_DESC);
        }
        public U Match<U>(Func<string, U> none, Func<T, U> some)
        {
            return none(NONE_DESC);
        }
        public T GetOrDefault(T other)
        {
            return other;
        }
        public void IfNone(Action action)
        {
            action();
        }
        public void IfSome(Action<T> action)
        {
            return;
        }
        public bool IsNone()
        {
            return true;
        }
        public bool IsSome()
        {
            return false;
        }
    }
}