using System;
using System.Xml.Linq;

namespace Jannesrsa.Tools.AssemblyReference.Extensions
{
    internal static class XElementExtensions
    {
        public static string GetAssemblyName(this XElement referencedBy)
        {
            if (referencedBy == null)
            {
                throw new System.ArgumentNullException(nameof(referencedBy));
            }

            string assemblyName;

            var indexOfComma = new Lazy<int>(() => referencedBy.Value.IndexOf(","));
            var indexOfDll = new Lazy<int>(() => referencedBy.Value.IndexOf(".dll", StringComparison.OrdinalIgnoreCase));
            var indexOfLastBackslash = new Lazy<int>(() => referencedBy.Value.LastIndexOf(@"\"));

            if (indexOfComma.Value > -1)
            {
                assemblyName = referencedBy.Value.Substring(0, indexOfComma.Value);
            }
            //else if (indexOfDll.Value > -1 &&
            //    indexOfLastBackslash.Value > -1)
            //{
            //    assemblyName = referencedBy.Value.Substring(indexOfLastBackslash.Value, indexOfDll.Value);
            //}
            else
            {
                assemblyName = referencedBy.Value;
            }

            return assemblyName;
        }
    }
}