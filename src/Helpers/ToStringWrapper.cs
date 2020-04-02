using System;
using System.Collections.Generic;

namespace Jannesrsa.Tools.AssemblyReference.Helpers
{
    public class ToStringWrapper
    {
        public static IEnumerable<ToStringWrapper<U>> GetEnumerable<U>(IEnumerable<U> values, Func<U, string> toStringFunction)
        {
            foreach (U value in values)
            {
                yield return new ToStringWrapper<U>(value, toStringFunction);
            }
        }
    }
}