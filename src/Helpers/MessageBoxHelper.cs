using System;
using System.Text;
using System.Windows.Forms;

namespace Jannesrsa.Tools.AssemblyReference.Helpers
{
    internal static class MessageBoxHelper
    {
        internal static void DisplayError(Exception ex)
        {
            StringBuilder message = new StringBuilder();

            message.AppendLine(ex.Message);

            if (ex.InnerException != null)
            {
                message.AppendLine();
                message.AppendLine("Inner Exception : " + ex.InnerException.Message);

                if (!string.IsNullOrEmpty(ex.InnerException.Source) &&
                    ex.InnerException.Source != "SmartObject Tester")

                    message.AppendLine("Source : " + ex.InnerException.Source);
            }

            DisplayError(message.ToString());
        }

        internal static void DisplayError(string message, string caption)
        {
            MessageBox.Show(message, caption, MessageBoxButtons.OK,
                 MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
        }

        internal static void DisplayInfo(string message, string caption)
        {
            MessageBox.Show(message, caption, MessageBoxButtons.OK,
                 MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
        }

        internal static DialogResult DisplayQuestion(string question, string caption)
        {
            return MessageBox.Show(question, caption, MessageBoxButtons.YesNoCancel,
                 MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
        }

        private static void DisplayError(string message)
        {
            DisplayError(message, "Error");
        }
    }
}