using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Jannesrsa.Tools.AssemblyReference.Helpers
{
    [SuppressUnmanagedCodeSecurityAttribute]
    internal static class UnsafeNativeMethods
    {
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
    }
}