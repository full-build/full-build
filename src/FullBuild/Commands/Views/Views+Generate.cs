// Copyright (c) 2014, Pierre Chalamet
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Pierre Chalamet nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL PIERRE CHALAMET BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using FullBuild.Helpers;
using FullBuild.Model;

namespace FullBuild.Commands.Views
{
    public partial class Views
    {
        private static void CopySlnFile(FileInfo fileInfo, string solutionName, DirectoryInfo target)
        {
            const string template = "template";
            var targetFileName = solutionName + fileInfo.Name.Substring(template.Length);
            var targetFile = target.GetFile(targetFileName).FullName;

            fileInfo.CopyTo(targetFile);
        }

        public static void Generate(string viewName)
        {
            // read anthology.json
            var admDir = WellKnownFolders.GetAdminDirectory();
            var anthology = Anthology.Load(admDir);

            // get current view
            var viewDir = WellKnownFolders.GetViewDirectory();
            viewDir.Create();

            var viewFileName = viewDir.GetFile(viewName + ".view");
            if (! viewFileName.Exists)
            {
                throw new ArgumentException("Initialize first solution with a list of repositories to include.");
            }

            var view = File.ReadAllLines(viewFileName.FullName)
                           .Distinct(StringComparer.InvariantCultureIgnoreCase)
                           .Where(x => ! string.IsNullOrEmpty(x)).ToList();

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
            sb.AppendLine("# Visual Studio 2013");

            var projects = from repo in view
                           from prj in anthology.Projects
                           where prj.ProjectFile.InvariantStartsWith(repo + "/")
                           select prj;

            foreach (var prj in projects)
            {
                sb.AppendFormat(@"Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{0}"", ""{1}"", ""{2:B}""",
                                prj.GetProjectName(), prj.ProjectFile, prj.Guid).AppendLine();
                sb.AppendFormat("\tProjectSection(ProjectDependencies) = postProject").AppendLine();
                foreach (var dependency in prj.ProjectReferences)
                {
                    // add a build order if dependency is included in solution
                    if (projects.Any(x => x.Guid == dependency))
                    {
                        sb.AppendFormat("\t\t{0:B} = {0:B}", dependency).AppendLine();
                    }
                }
                sb.AppendFormat("\tEndProjectSection").AppendLine();
                sb.AppendLine("EndProject");
            }

            sb.AppendLine("Global");

            sb.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
            sb.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
            sb.AppendLine("\t\tRelease|Any CPU = Release|Any CPU");
            sb.AppendLine("\tEndGlobalSection");
            sb.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");

            foreach (var prj in projects)
            {
                sb.AppendFormat("\t\t{0:B}.Debug|Any CPU.ActiveCfg = Debug|Any CPU", prj.Guid).AppendLine();
                sb.AppendFormat("\t\t{0:B}.Debug|Any CPU.Build.0 = Debug|Any CPU", prj.Guid).AppendLine();
                sb.AppendFormat("\t\t{0:B}.Release|Any CPU.ActiveCfg = Release|Any CPU", prj.Guid).AppendLine();
                sb.AppendFormat("\t\t{0:B}.Release|Any CPU.Build.0 = Release|Any CPU", prj.Guid).AppendLine();
            }

            sb.AppendLine("\tEndGlobalSection");
            sb.AppendLine("EndGlobal");

            var wsDir = WellKnownFolders.GetWorkspaceDirectory();
            var slnFileName = viewName + ".sln";
            var slnFile = wsDir.GetFile(slnFileName);
            File.WriteAllText(slnFile.FullName, sb.ToString());

            // copy templates for solution - this could be either resharper settings or ncrunch
            var slnTemplates = admDir.EnumerateFiles("template.sln.*");
            slnTemplates.ForEach(x => CopySlnFile(x, viewName, wsDir));

            // generate target for solution
            var xdoc = new XElement(XmlHelpers.NsMsBuild + "Project",
                                    new XElement(XmlHelpers.NsMsBuild + "PropertyGroup",
                                                 new XElement(XmlHelpers.NsMsBuild + "FullBuild_Config", "Y"),
                                                 from prj in projects
                                                 let projectProperty = prj.GetProjectPropertyGroupName()
                                                 select new XElement(XmlHelpers.NsMsBuild + projectProperty, "Y")));
            var targetFileName = viewName + ".targets";
            var targetFile = Path.Combine(viewDir.FullName, targetFileName);
            xdoc.Save(targetFile);
        }
    }
}
