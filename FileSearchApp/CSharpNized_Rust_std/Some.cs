using System;

namespace CSharpNized.Rust.std
{
    public sealed class Some<T> : IOption<T>
    {
        T _value;
        public Some(T value)
        {
            _value = value;
        }
        public void Match(Action<string> none, Action<T> some)
        {
            some(_value);
        }
        public T Match(Func<string, T> none, Func<T, T> some)
        {
            return some(_value);
        }
        public U Match<U>(Func<string, U> none, Func<T, U> some)
        {
            return some(_value);
        }
        public T GetOrDefault(T other)
        {
            return _value;
        }
        public void IfNone(Action action)
        {
            return;
        }
        public void IfSome(Action<T> action)
        {
            action(_value);
        }
        public bool IsNone()
        {
            return false;
        }
        public bool IsSome()
        {
            return true;
        }
        public T Get()
        {
            return _value;
        }
    }
    public static class Some
    {
        public static Some<T> New<T>(T value)
        {
            return new Some<T>(value);
        }
    }
}