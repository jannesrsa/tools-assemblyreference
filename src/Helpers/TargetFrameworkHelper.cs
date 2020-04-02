using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Jannesrsa.Tools.AssemblyReference.Helpers
{
    internal static class TargetFrameworkHelper
    {
        public static void UpdateTargetFrameworkToServerTarget(FileInfo projectFileInfo)
        {
            if (!File.Exists(projectFileInfo.FullName))
            {
                Debug.WriteLine($"File does not exist: {projectFileInfo.FullName}");
                return;
            }

            var xDocument = XDocument.Load(projectFileInfo.FullName);

            var importTargetFrameworkVersionNode = xDocument
                                .Element(Constants.XNameValue.MsBuildProject)
                                ?.Elements(Constants.XNameValue.MsBuildImport)
                                ?.FirstOrDefault(i => i.Attribute(Constants.XmlAttributeName.Project)?.Value?.IndexOf(Constants.XmlAttributeValue.TargetFrameworkVersion, StringComparison.InvariantCultureIgnoreCase) > 0);

            if (importTargetFrameworkVersionNode == null)
            {
                var targetFrameworkVersionNode = xDocument
                                .Element(Constants.XNameValue.MsBuildProject)
                                ?.Element(Constants.XNameValue.MsBuildPropertyGroup)
                                ?.Element(Constants.XNameValue.MsBuildTargetFrameworkVersion);

                if (targetFrameworkVersionNode != null)
                {
                    targetFrameworkVersionNode.Remove();
                }

                // Get the branch location from the known Build Output location
                var branchLocation = Properties.Settings.Default.Options.BuildOutputLocalPath
                    .Replace(@"Build Output", string.Empty)?.Trim('\\');

                // Get the project's relative directory in respect to the branch location.
                var projectRelativeDirectoryLocation = projectFileInfo.FullName
                   .Replace(branchLocation, string.Empty)?.Trim('\\');

                var serverTargetsRelativePath = new System.Text.StringBuilder()
                    .Insert(0, @"..\", projectRelativeDirectoryLocation.Split('\\').Count() - 1)
                    .Append(@"Common\TargetFrameworkVersion\Server.targets")
                    .ToString();

                var projectNode = xDocument.Element(Constants.XNameValue.MsBuildProject);
                var existingImportNode = projectNode.Elements(Constants.XNameValue.MsBuildImport).LastOrDefault();

                var importTargetsElement = new XElement(Constants.XNameValue.MsBuildImport,
                    new XAttribute(Constants.XmlAttributeName.Project, serverTargetsRelativePath));

                if (existingImportNode != null)
                {
                    existingImportNode.AddBeforeSelf(importTargetsElement);
                }
                else
                {
                    projectNode.Add(importTargetsElement);
                }
            }
            else
            {
                var projectNode = importTargetFrameworkVersionNode.Attribute(Constants.XNameValue.Project);

                var newValue = projectNode.Value
                    .Replace(Constants.XmlAttributeValue.ClientTargets, Constants.XmlAttributeValue.ServerTargets);

                // Already set to Server.Targets
                if (newValue == projectNode.Value)
                {
                    return;
                }

                projectNode.SetValue(newValue);
            }

            var xmlWriterSettings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true
            };

            using (var xmlWriter = XmlWriter.Create(projectFileInfo.FullName, xmlWriterSettings))
            {
                xDocument.Save(xmlWriter);
            }
        }
    }
}