﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;

namespace GrzegorzKozub.VisualStudioExtensions.ConEmuLauncher
{
    internal class Launcher
    {
        private DTE _dte;
        private Options _options;

        internal Launcher(DTE dte, Options options)
        {
            _dte = dte;
            _options = options;
        }

        internal void Launch()
        {
            var process = System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = GetFileName(),
                Arguments = GetArguments()
            });

            System.Threading.Thread.Sleep(250);

            if (process != null && !process.HasExited)
                NativeMethods.SetForegroundWindow(process.MainWindowHandle);
        }

        private string GetFileName()
        {
            return _options.Path;
        }

        private string GetArguments()
        {
            var arguments = new StringBuilder();

            arguments.AppendFormat(" /dir \"{0}\"", Path.GetDirectoryName(GetActiveItemPath()));

            if (!string.IsNullOrEmpty(_options.CommandLineOptions))
                arguments.AppendFormat(" {0}", _options.CommandLineOptions);

            if (!_options.CommandLineOptions.ContainsParameter("/single") && _options.ReuseExistingInstance)
                arguments.Append(" /single");

            if (!_options.CommandLineOptions.ContainsParameter("/cmd") && !string.IsNullOrEmpty(_options.TaskToExecute))
                arguments.AppendFormat(" /cmd {0}", _options.TaskToExecute);

            return arguments.ToString();
        }

        private string GetActiveItemPath()
        {
            string path;
            var selectedItem = _dte.SelectedItems.Item(1);

            if (selectedItem.Project != null &&
                selectedItem.Project.Kind == "{E24C65DC-7377-472b-9ABA-BC803B73C61A}")
            {
                // Web Site.
                path = selectedItem.Project.Properties.Item("FullPath").Value.ToString() + "\\";
            }
            else if (selectedItem.Project != null &&
                selectedItem.Project.Kind != "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}" && // Excludes Solution Folders.
                !string.IsNullOrEmpty(selectedItem.Project.FullName))
            {
                // Project.
                path = selectedItem.Project.FullName;
            }
            else if (selectedItem.ProjectItem != null &&
                (
                    Guid.Parse(selectedItem.ProjectItem.Kind) == VSConstants.GUID_ItemType_PhysicalFile ||
                    Guid.Parse(selectedItem.ProjectItem.Kind) == VSConstants.GUID_ItemType_PhysicalFolder
                ) &&
                selectedItem.ProjectItem.Properties != null &&
                selectedItem.ProjectItem.Properties.Item("FullPath") != null)
            {
                // Project Folder (also Project Properties) or Project Item.
                path = selectedItem.ProjectItem.Properties.Item("FullPath").Value.ToString();
            }
            else
            {
                // Solution, Solution Folder, Solution Folder contents or Project References.
                path = _dte.Solution.FullName;
            }

            if (string.IsNullOrEmpty(path))
                return GetDefaultWorkingDirectory();

            return path;
        }

        private string GetDefaultWorkingDirectory()
        {
            var defaultWorkingDirectory = string.IsNullOrEmpty(_options.DefaultWorkingDirectory) ? "%HOMEDRIVE%%HOMEPATH%" : _options.DefaultWorkingDirectory;
            return Environment.ExpandEnvironmentVariables(defaultWorkingDirectory);
        }
    }
}
