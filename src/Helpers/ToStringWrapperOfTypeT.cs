using System;

namespace Jannesrsa.Tools.AssemblyReference.Helpers
{
    public class ToStringWrapper<T>
    {
        private readonly T _wrappedObject;
        private readonly Func<T, string> _toStringFunction;

        public T WrappedObject
        {
            get { return _wrappedObject; }
        }

        public ToStringWrapper(T wrappedObject, Func<T, string> toStringFunction)
        {
            this._wrappedObject = wrappedObject;
            this._toStringFunction = toStringFunction;
        }

        public override string ToString()
        {
            return _toStringFunction(_wrappedObject);
        }
    }
}