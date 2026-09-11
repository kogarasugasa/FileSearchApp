using System;

namespace CSharpNized.Rust.std
{
    public interface IOption<T>
    {
        void Match(Action<string> none, Action<T> some);
        T Match(Func<string, T> none, Func<T, T> some);
        U Match<U>(Func<string, U> none, Func<T, U> some);
        T GetOrDefault(T other);
        void IfNone(Action action);
        void IfSome(Action<T> action);
        bool IsNone();
        bool IsSome();
    }
}