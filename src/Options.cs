using System;
using System.Drawing;
using System.Windows.Forms;

namespace Jannesrsa.Tools.AssemblyReference
{
    [Serializable]
    public class Options
    {
        public Point Location { get; set; }
        public Size Size { get; set; }
        public FormWindowState WindowState { get; set; }
        public string TfsWorkspaceName { get; set; }
        public string TfsServerUrl { get; set; }
        public string BuildOutputLocalPath { get; set; }
    }
}