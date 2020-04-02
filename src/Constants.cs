using System.Xml.Linq;

namespace Jannesrsa.Tools.AssemblyReference
{
    internal static class Constants
    {
        public static class ColumnName
        {
            public const string Assembly = "Assembly";
            public const string RelativePath = "RelativePath";
            public const string TargetFramework = "TargetFramework";
        }

        public static class XmlAttributeName
        {
            public const string Name = "Name";
            public const string Project = "Project";
        }

        public static class XmlAttributeValue
        {
            public const string ClientTargets = @"Client.targets";
            public const string ServerTargets = @"Server.targets";
            public const string TargetFrameworkVersion = "TargetFrameworkVersion";
        }

        public static class XmlNamespaceName
        {
            public const string MsBuild = "http://schemas.microsoft.com/developer/msbuild/2003";
        }

        public static class XmlNodeName
        {
            public const string Import = "Import";
            public const string Project = "Project";
            public const string Projects = "Projects";
            public const string PropertyGroup = "PropertyGroup";
            public const string ReferencedBy = "ReferencedBy";
            public const string SourceCodeBuild = "SourceCode.Build";
            public const string TargetFrameworkVersion = "TargetFrameworkVersion";
        }

        public static class XNameValue
        {
            public static XName MsBuildImport = XName.Get(Constants.XmlNodeName.Import, Constants.XmlNamespaceName.MsBuild);
            public static XName MsBuildProject = XName.Get(Constants.XmlNodeName.Project, Constants.XmlNamespaceName.MsBuild);
            public static XName MsBuildPropertyGroup = XName.Get(Constants.XmlNodeName.PropertyGroup, Constants.XmlNamespaceName.MsBuild);
            public static XName MsBuildTargetFrameworkVersion = XName.Get(Constants.XmlNodeName.TargetFrameworkVersion, Constants.XmlNamespaceName.MsBuild);
            public static XName Project = XName.Get(Constants.XmlNodeName.Project);
            public static XName Projects = XName.Get(Constants.XmlNodeName.Projects);
            public static XName ReferencedBy = XName.Get(Constants.XmlNodeName.ReferencedBy);
            public static XName SourceCodeBuild = XName.Get(Constants.XmlNodeName.SourceCodeBuild);
        }
    }
}