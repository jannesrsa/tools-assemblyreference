using System;

namespace Jannesrsa.Tools.AssemblyReference.Extensions
{
    /// <summary>
    /// String Extensions
    /// </summary>
    internal static class StringExtensions
    {
        public static bool Contains(this string str, string value, StringComparison comparisonType)
        {
            return str?.IndexOf(value, comparisonType) >= 0;
        }
    }
}