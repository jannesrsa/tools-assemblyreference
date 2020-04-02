using System;
using System.Diagnostics;
using System.Windows.Forms;
using Jannesrsa.Tools.AssemblyReference.Helpers;
using Jannesrsa.Tools.AssemblyReference.Properties;

namespace Jannesrsa.Tools.AssemblyReference.Extensions
{
    /// <summary>
    /// Form extention methods
    /// </summary>
    internal static class FormExtensions
    {
        private const int WM_SETREDRAW = 11;

        public static void Busy(this Form form)
        {
            if (form == null)
            {
                return;
            }

            form.Cursor = Cursors.WaitCursor;
        }

        public static void DisplayError(this Form form, string message, string caption)
        {
            form.Stop();
            MessageBoxHelper.DisplayError(message, caption);
        }

        public static void DisplayError(this Form form, Exception ex)
        {
            form.DisplayError(ex.Message, Resources.Error);
        }

        public static void DisplayInfo(this Form form, string message, string caption)
        {
            form.Stop();
            MessageBoxHelper.DisplayInfo(message, caption);
        }

        public static void Draw(this Form form, Action action)
        {
            form.Busy();
            form.StartRedraw();

            try
            {
                action();

                form.Stop();
                form.StopRedraw();
            }
            catch
            {
                form.Stop();
                form.StopRedraw();

                throw;
            }
        }

        /// <summary>
        /// StartStop
        /// </summary>
        /// <param name="form"></param>
        /// <param name="action"></param>
        /// <returns>Returns ElapsedMilliseconds</returns>
        public static long StartStop(this Form form, Action action)
        {
            form.Busy();

            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                action();

                stopwatch.Stop();

                form.Stop();

                return stopwatch.ElapsedMilliseconds;
            }
            catch
            {
                form.Stop();

                throw;
            }
        }

        public static int StartRedraw(this Form form)
        {
            form.SuspendLayout();

            return (int)UnsafeNativeMethods.SendMessage(form.Handle, WM_SETREDRAW, (IntPtr)0, (IntPtr)0);
        }

        public static void Stop(this Form form)
        {
            if (form == null)
            {
                return;
            }

            form.Cursor = Cursors.Default;
        }

        public static int StopRedraw(this Form form)
        {
            var returnVal = UnsafeNativeMethods.SendMessage(form.Handle, WM_SETREDRAW, (IntPtr)1, (IntPtr)0);

            form.ResumeLayout();
            form.Refresh();

            return (int)returnVal;
        }
    }
}