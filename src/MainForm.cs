using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using Jannesrsa.Tools.AssemblyReference.Extensions;
using Jannesrsa.Tools.AssemblyReference.Helpers;
using Jannesrsa.Tools.AssemblyReference.Properties;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Jannesrsa.Tools.AssemblyReference
{
    public partial class MainForm : Form
    {
        private BindingSource _allAssembliesBindingSource;
        private DataGridViewRow[] _allAssembliesRows;
        private ErrorProvider _errorbuildOutputLocalPath = new ErrorProvider();
        private BindingSource _referencedByAssembliesBindingSource;
        private HashSet<string> _referencedByCollection = new HashSet<string>();
        private XDocument _referencesXmlDocument;

        public MainForm()
        {
            InitializeComponent();
            this.mainTabControl.Selecting += MainTabControl_Selecting;

            LoadFormSettings();
        }

        public DataTable AllAssembliesDataTable { get; } = new DataTable();
        public DataTable ReferencedByAssembliesDataTable { get; } = new DataTable();

        private static string GetFileLocalPath(string relativePath)
        {
            return Path.Combine(Settings.Default.Options.BuildOutputLocalPath, "Assemblies", relativePath);
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var about = new AboutBox())
            {
                about.ShowDialog();
            }
        }

        private void AllAssembliesDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            Action action = () =>
            {
                DataRow selectedDataRow;

                // User Click
                if (e.ColumnIndex != -1)
                {
                    InitializeReferencedByAssembliesDataTable();

                    allAssembliesDataGridView.CurrentRow.Tag = 1;
                    selectedDataRow = allAssembliesDataGridView.GetDataRow(allAssembliesDataGridView.CurrentRow.Index);
                }
                // Simulated Click
                else
                {
                    selectedDataRow = allAssembliesDataGridView.GetDataRow(e.RowIndex);
                }

                if (selectedDataRow == null ||
                    _referencesXmlDocument == null)
                {
                    return;
                }

                var selectedAssembly = selectedDataRow[Constants.ColumnName.Assembly] as string;
                var selectedRelativePath = selectedDataRow[Constants.ColumnName.RelativePath] as string;

                Debug.WriteLine($"Selected Assembly\t{selectedAssembly}");

                var selectedXmlElement = _referencesXmlDocument
                       .Element(Constants.XNameValue.SourceCodeBuild)
                       ?.Element(Constants.XNameValue.Projects)
                       ?.Descendants(Constants.XNameValue.Project)
                       ?.FirstOrDefault(i =>
                            i.Attribute(Constants.XmlAttributeName.Name).Value == selectedAssembly &&
                            i.Attribute(Constants.ColumnName.RelativePath).Value == selectedRelativePath)
                        .Element(Constants.XNameValue.ReferencedBy)
                        .Descendants()
                        .Select(i => i.GetAssemblyName())
                        .OrderBy(i => i)
                        .ToArray();

                foreach (var referencedBy in selectedXmlElement)
                {
                    // Is the referenceBy assembly already added?
                    if (referencedBy == selectedAssembly ||
                        _referencedByCollection.Contains(referencedBy))
                    {
                        continue;
                    }

                    Debug.WriteLine($"Referenced By\t{referencedBy}");

                    _referencedByCollection.Add(referencedBy);

                    // Simulate clicking the referencyBy assembly in the AllAssemblies DataGridView to get all its referencedBy assemblies
                    var allAssemblyRows = _allAssembliesRows.Where(i => i.Cells[0]?.Value?.ToString() == referencedBy).ToArray();
                    foreach (var allAssemblyRow in allAssemblyRows)
                    {
                        if (allAssemblyRow.Selected)
                        {
                            continue;
                        }

                        allAssemblyRow.Selected = true;
                        allAssembliesDataGridView.RowCellClick(-1, allAssemblyRow.Index);
                    }
                }
            };

            if (sender == null ||
                e.ColumnIndex == -1)
            {
                action();
            }
            else
            {
                Action startStopAction = () =>
                {
                    action();

                    UpdateReferenceByAssembliesDataTable();
                };

                var elapsedMilliseconds = this.StartStop(startStopAction);
                timingMessageToolStripStatusLabel.Text = $"Time taken: {elapsedMilliseconds}ms";
            }
        }

        private void AllAssembliesDataGridView_SelectionChanged(object sender, EventArgs e)
        {
            UpdateCurrentRowSelectionColors();
        }

        private void EditFrameworkToServerToolStripButton_Click(object sender, EventArgs e)
        {
            Action action = () =>
            {
                IEnumerable<DataGridViewRow> datagridViewRows;
                if (mainTabControl.SelectedTab == AllAssembliesTabPage)
                {
                    var dialogResult = MessageBoxHelper.DisplayQuestion("Do you want to update all the selected projects' TargetFramework to Server.targets?", "Update TargetFramework");
                    if (dialogResult != DialogResult.Yes)
                    {
                        return;
                    }

                    datagridViewRows = allAssembliesDataGridView.SelectedRows.Cast<DataGridViewRow>();
                }
                else
                {
                    var dialogResult = MessageBoxHelper.DisplayQuestion("Do you want to update all the related projects' TargetFramework to Server.targets?", "Update TargetFramework");
                    if (dialogResult != DialogResult.Yes)
                    {
                        return;
                    }

                    datagridViewRows = referencedByAssembliesDataGridView.Rows.Cast<DataGridViewRow>();
                }

                var assemblies = from r in datagridViewRows
                                 select new
                                 {
                                     RelativePath = r.Cells[Constants.ColumnName.RelativePath]?.Value?.ToString(),
                                     FileVersionPath = GetFileLocalPath(r.Cells[Constants.ColumnName.RelativePath]?.Value?.ToString()),
                                     TargetFramework = r.Cells[Constants.ColumnName.TargetFramework].Value?.ToString()
                                 };

                var workspace = ValidateWorkspace();
                workspace.Get(assemblies.Select(i => i.FileVersionPath).ToArray(), VersionSpec.Latest, RecursionType.None, GetOptions.None);

                // Update Import node value, Client.targets to Server.targets
                var clientTargets = assemblies
                    .Where(i => i.TargetFramework != Constants.XmlAttributeValue.ServerTargets);

                if (clientTargets.Select(i => i.FileVersionPath).Any())
                {
                    workspace.PendEdit(clientTargets.Select(i => i.FileVersionPath).ToArray(), RecursionType.None);

                    foreach (var clientTarget in clientTargets)
                    {
                        var projectFileInfo = new FileInfo(clientTarget.FileVersionPath);

                        TargetFrameworkHelper.UpdateTargetFrameworkToServerTarget(
                                projectFileInfo);
                    }
                }
            };

            this.StartStop(action);
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private Dictionary<string, string> GetAllAssembliesRelativePath()
        {
            var relativePaths = _referencesXmlDocument
                .Element(Constants.XNameValue.SourceCodeBuild)
                ?.Element(Constants.XNameValue.Projects)
                ?.Descendants(Constants.XNameValue.Project)
                ?.Select(i => i.Attribute(Constants.ColumnName.RelativePath).Value);

            var paths = new Dictionary<string, string>();
            foreach (var relativePath in relativePaths)
            {
                var fileVersionPath = GetFileLocalPath(relativePath);
                paths[relativePath] = fileVersionPath;
            }

            return paths;
        }

        private void Initialize()
        {
            _errorbuildOutputLocalPath.SetIconAlignment(buildOutputLocalPathComboBox, ErrorIconAlignment.TopRight);

            _allAssembliesBindingSource = new BindingSource()
            {
                DataSource = AllAssembliesDataTable
            };
            allAssembliesBindingNavigator.BindingSource = _allAssembliesBindingSource;
            allAssembliesDataGridView.DataSource = _allAssembliesBindingSource;

            InitializeAllAssembliesDataTable();

            _referencedByAssembliesBindingSource = new BindingSource()
            {
                DataSource = ReferencedByAssembliesDataTable
            };
            referencedByAssembliesBindingNavigator.BindingSource = _referencedByAssembliesBindingSource;
            referencedByAssembliesDataGridView.DataSource = _referencedByAssembliesBindingSource;

            InitializeReferencedByAssembliesDataTable();
        }

        private void InitializeAllAssembliesDataTable()
        {
            AllAssembliesDataTable.Columns.Clear();
            AllAssembliesDataTable.Rows.Clear();
            AllAssembliesDataTable.Clear();

            AllAssembliesDataTable.Columns.Add(Constants.ColumnName.Assembly, typeof(string));
            AllAssembliesDataTable.Columns.Add(Constants.ColumnName.TargetFramework, typeof(string));
            AllAssembliesDataTable.Columns.Add(Constants.ColumnName.RelativePath, typeof(string));

            allAssembliesDataGridView.SetColumnSortMode(DataGridViewColumnSortMode.NotSortable);
        }

        private void InitializeReferencedByAssembliesDataTable()
        {
            _referencedByCollection.Clear();
            ReferencedByAssembliesDataTable.Columns.Clear();
            ReferencedByAssembliesDataTable.Rows.Clear();
            ReferencedByAssembliesDataTable.Clear();

            ReferencedByAssembliesDataTable.Columns.Add(Constants.ColumnName.Assembly, typeof(string));
            ReferencedByAssembliesDataTable.Columns.Add(Constants.ColumnName.TargetFramework, typeof(string));
            ReferencedByAssembliesDataTable.Columns.Add(Constants.ColumnName.RelativePath, typeof(string));

            referencedByAssembliesDataGridView.SetColumnSortMode(DataGridViewColumnSortMode.NotSortable);
        }

        private void LoadFormSettings()
        {
            if (Settings.Default.Options == null)
            {
                Settings.Default.Options = new Options();
            }

            if (!Settings.Default.Options.Location.IsEmpty)
            {
                this.Location = Settings.Default.Options.Location;
            }

            if (!Settings.Default.Options.Size.IsEmpty)
            {
                this.Size = Settings.Default.Options.Size;
                this.WindowState = Settings.Default.Options.WindowState;
            }
        }

        private void LoadTfsSettings()
        {
            if (!string.IsNullOrWhiteSpace(Settings.Default.Options.TfsServerUrl))
            {
                tfsServerUrlTextBox.Text = Settings.Default.Options.TfsServerUrl;
            }
            else
            {
                Settings.Default.Options.TfsServerUrl = tfsServerUrlTextBox.Text;
            }

            if (TryRefreshWorkspaces(false) &&
                !string.IsNullOrWhiteSpace(Settings.Default.Options.TfsWorkspaceName))
            {
                var workspaces = tfsWorkspacesComboBox.DataSource as ToStringWrapper<Workspace>[];
                var selectedWorkspace = workspaces.FirstOrDefault(i => i.ToString() == Settings.Default.Options.TfsWorkspaceName);

                if (selectedWorkspace != null)
                {
                    tfsWorkspacesComboBox.SelectedItem = selectedWorkspace;
                }
            }
            else
            {
                Settings.Default.Options.TfsWorkspaceName = tfsWorkspacesComboBox.SelectedText;
            }

            if (!string.IsNullOrWhiteSpace(Settings.Default.Options.BuildOutputLocalPath))
            {
                buildOutputLocalPathComboBox.Text = Settings.Default.Options.BuildOutputLocalPath;
            }
            else
            {
                Settings.Default.Options.BuildOutputLocalPath = buildOutputLocalPathComboBox.Text;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadTfsSettings();
            Initialize();

            if (string.IsNullOrEmpty(buildOutputLocalPathComboBox.Text))
            {
                mainTabControl.SelectedTab = SettingsTabPage;

                Action action = () => RefreshBuildOutputFolders();
                this.StartStop(action);
            }
        }

        private void MainTabControl_Selecting(object sender, System.Windows.Forms.TabControlCancelEventArgs e)
        {
            UpdateErrors();

            if (!string.IsNullOrEmpty(_errorbuildOutputLocalPath.GetError(buildOutputLocalPathComboBox)) &&
                e.TabPage != SettingsTabPage)
            {
                e.Cancel = true;

                MessageBoxHelper.DisplayError("Clear all Settings errors before selecting another tab.", "Settings Error");
            }
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateAllAssembliesDataTable();
            allAssembliesDataGridView.Focus();

            mainTabControl.SelectedTab = AllAssembliesTabPage;
        }

        private void RefreshBuildOutputFolders()
        {
            Action action = () =>
            {
                var workspace = (tfsWorkspacesComboBox.SelectedItem as ToStringWrapper<Workspace>)?.WrappedObject;
                if (workspace == null)
                {
                    return;
                }

                var buildOutputFolderCollection = new HashSet<string>();

                foreach (var folder in workspace.Folders.Select(i => i.LocalItem))
                {
                    foreach (var buildOutputFolder in Directory.GetDirectories(folder, @"K2", SearchOption.AllDirectories).Where(i => i.Contains(@"Assemblies\x86\Program Files\K2", StringComparison.OrdinalIgnoreCase)))
                    {
                        var serverItem = workspace.TryGetServerItemForLocalItem(buildOutputFolder);
                        if (workspace.VersionControlServer.ServerItemExists(serverItem, ItemType.Folder))
                        {
                            buildOutputFolderCollection.Add(
                                Regex.Replace(
                                    buildOutputFolder,
                                    Regex.Escape(@"Assemblies\x86\Program Files\K2"),
                                    string.Empty,
                                    RegexOptions.IgnoreCase));
                        }
                    }
                }

                buildOutputLocalPathComboBox.Items.Clear();
                buildOutputLocalPathComboBox.Items.AddRange(buildOutputFolderCollection.ToArray());
            };

            this.StartStop(action);
        }

        private void RefreshBuildOutputFoldersButton_Click(object sender, EventArgs e)
        {
            RefreshBuildOutputFolders();
        }

        private void RefreshTfsWorkspaces_Click(object sender, EventArgs e)
        {
            TryRefreshWorkspaces();
        }

        private void SaveSettings()
        {
            if (Settings.Default.Options == null)
            {
                Settings.Default.Options = new Options();
            }

            Settings.Default.Options.Location = this.Location;
            Settings.Default.Options.Size = this.Size;
            Settings.Default.Options.WindowState = this.WindowState;
            Settings.Default.Options.TfsServerUrl = tfsServerUrlTextBox.Text;
            Settings.Default.Options.TfsWorkspaceName = tfsWorkspacesComboBox.Text;
            Settings.Default.Options.BuildOutputLocalPath = buildOutputLocalPathComboBox.Text;

            Settings.Default.Save();
        }

        private void TfsWorkspacesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tfsWorkspacesComboBox.ContainsFocus)
            {
                RefreshBuildOutputFolders();
            }
        }

        private bool TryRefreshWorkspaces(bool throwException = true)
        {
            bool result = false;

            Action action = () =>
            {
                try
                {
                    var tfsTeamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(tfsServerUrlTextBox.Text));
                    var versionControlServer = tfsTeamProjectCollection.GetService<VersionControlServer>();

                    var workspaces = versionControlServer.QueryWorkspaces(null, versionControlServer.AuthorizedUser, Environment.MachineName);
                    var workspacesWrapper = ToStringWrapper.GetEnumerable(workspaces, i => i.Name).ToArray();
                    tfsWorkspacesComboBox.DataSource = workspacesWrapper;

                    result = true;
                }
                catch
                {
                    if (throwException)
                    {
                        throw;
                    }

                    result = false;
                }
            };

            this.StartStop(action);

            return result;
        }

        private void UpdateAllAssembliesDataTable()
        {
            string referencesXmlFilePath;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select References.XML";
                openFileDialog.Filter = "References.XML(References.XML)|References.XML|XML Files(*.XML)|*.XML|All Files(*.*)|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.Cancel ||
                    string.IsNullOrWhiteSpace(openFileDialog.FileName))
                {
                    return;
                }

                referencesXmlFilePath = openFileDialog.FileName;
            }

            Action action = () =>
            {
                InitializeAllAssembliesDataTable();
                InitializeReferencedByAssembliesDataTable();

                _referencesXmlDocument = null;
                _referencesXmlDocument = XDocument.Load(referencesXmlFilePath);

                // Returns a Dictionary<string,string> where the Key=RelativePath and Value=TFS File Path
                var projectPaths = GetAllAssembliesRelativePath();
                var workspace = ValidateWorkspace();
                workspace.Get(projectPaths.Values.ToArray(), VersionSpec.Latest, RecursionType.None, GetOptions.Overwrite);

                foreach (var element in _referencesXmlDocument
                                           .Element(Constants.XNameValue.SourceCodeBuild)
                                           ?.Element(Constants.XNameValue.Projects)
                                           ?.Descendants(Constants.XNameValue.Project)
                                           .OrderBy(i => i.Attribute(Constants.XmlAttributeName.Name).Value))
                {
                    var newRow = AllAssembliesDataTable.NewRow();

                    newRow[Constants.ColumnName.Assembly] = element.Attribute(Constants.XmlAttributeName.Name).Value;

                    var relativePath = element.Attribute(Constants.ColumnName.RelativePath).Value;
                    newRow[Constants.ColumnName.RelativePath] = relativePath;

                    var projectPath = projectPaths[relativePath];
                    if (File.Exists(projectPath))
                    {
                        var xDocument = XDocument.Load(projectPath);

                        var targetFrameworkVersionNode = xDocument
                                .Element(Constants.XNameValue.MsBuildProject)
                                ?.Elements(Constants.XNameValue.MsBuildImport)
                                ?.FirstOrDefault(i => i.Attribute(Constants.XmlAttributeName.Project)?.Value?.IndexOf(Constants.XmlAttributeValue.TargetFrameworkVersion, StringComparison.InvariantCultureIgnoreCase) > 0);

                        string targetFramework = null;

                        if (targetFrameworkVersionNode != null)
                        {
                            var projectVersion = targetFrameworkVersionNode.Attribute(Constants.XmlAttributeName.Project).Value;
                            targetFramework = projectVersion.Substring(projectVersion.LastIndexOf("\\") + 1);
                        }
                        else
                        {
                            targetFramework = xDocument
                                .Element(Constants.XNameValue.MsBuildProject)
                                ?.Element(Constants.XNameValue.MsBuildPropertyGroup)
                                ?.Element(Constants.XNameValue.MsBuildTargetFrameworkVersion)
                                ?.Value;
                        }

                        if (!string.IsNullOrEmpty(targetFramework))
                        {
                            newRow[Constants.ColumnName.TargetFramework] = targetFramework;
                        }
                    }

                    AllAssembliesDataTable.Rows.Add(newRow);
                }

                allAssembliesDataGridView.ClearSelection();
                _allAssembliesRows = allAssembliesDataGridView.Rows.Cast<DataGridViewRow>().ToArray();
            };

            var elapsedMilliseconds = this.StartStop(action);
            timingMessageToolStripStatusLabel.Text = $"Time taken: {elapsedMilliseconds}ms";
        }

        private void UpdateCurrentRowSelectionColors()
        {
            if (allAssembliesDataGridView.CurrentRow == null)
            {
                return;
            }

            allAssembliesDataGridView.CurrentRow.DefaultCellStyle.SelectionBackColor = Color.Yellow;
            allAssembliesDataGridView.CurrentRow.DefaultCellStyle.SelectionForeColor = Color.Black;

            allAssembliesDataGridView.CurrentRow.Tag = 1;

            foreach (DataGridViewRow dataGridViewRow in allAssembliesDataGridView.Rows)
            {
                if (dataGridViewRow == allAssembliesDataGridView.CurrentRow)
                {
                    continue;
                }

                dataGridViewRow.DefaultCellStyle.SelectionBackColor = allAssembliesDataGridView.RowsDefaultCellStyle.SelectionBackColor;
                dataGridViewRow.DefaultCellStyle.SelectionForeColor = allAssembliesDataGridView.RowsDefaultCellStyle.SelectionForeColor;
            }
        }

        private void UpdateErrors()
        {
            if (string.IsNullOrEmpty(buildOutputLocalPathComboBox.Text))
            {
                _errorbuildOutputLocalPath.SetError(buildOutputLocalPathComboBox, "Have to select a Build Output folder");
            }
            else
            {
                _errorbuildOutputLocalPath.SetError(buildOutputLocalPathComboBox, string.Empty);
                SaveSettings();
            }
        }

        private void UpdateReferenceByAssembliesDataTable()
        {
            var referencedAssemblies = from r in _allAssembliesRows
                                       where r.Selected
                                       select new
                                       {
                                           Assembly = r.Cells[Constants.ColumnName.Assembly].Value.ToString(),
                                           RelativePath = r.Cells[Constants.ColumnName.RelativePath].Value.ToString(),
                                           TargetFramework = r.Cells[Constants.ColumnName.TargetFramework].Value.ToString(),
                                       };

            foreach (var referencedBy in referencedAssemblies)
            {
                // Add referencyBy assembly
                var newRow = ReferencedByAssembliesDataTable.NewRow();

                newRow[Constants.ColumnName.Assembly] = referencedBy.Assembly;
                newRow[Constants.ColumnName.TargetFramework] = referencedBy.TargetFramework;
                newRow[Constants.ColumnName.RelativePath] = referencedBy.RelativePath;

                ReferencedByAssembliesDataTable.Rows.Add(newRow);
            }

            referencedByAssembliesDataGridView.ClearSelection();
        }

        private Workspace ValidateWorkspace()
        {
            var workspace = tfsWorkspacesComboBox.SelectedItem as ToStringWrapper<Workspace>;
            if (workspace == null)
            {
                MessageBoxHelper.DisplayError("Select a TFS Workspace in the Settings tab.", "Missing TFS Workspace");
            }

            if (string.IsNullOrEmpty(buildOutputLocalPathComboBox.Text))
            {
                MessageBoxHelper.DisplayError(@"Enter a Build Output Local Path in the Settings tab.", @"Missing Build Output\Assemblies Local Path");
            }

            return workspace.WrappedObject;
        }
    }
}