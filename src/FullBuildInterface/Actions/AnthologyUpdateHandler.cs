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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using FullBuildInterface.Config;
using FullBuildInterface.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace FullBuildInterface.Actions
{
    internal class AnthologyUpdateHandler
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public void Execute()
        {
            var workspace = WellKnownFolders.GetWorkspaceDirectory();
            var config = ConfigManager.GetConfig(workspace);

            // get all csproj in all repos only
            var anthology = LoadOrCreateAnthology();
            anthology = UpdateAnthologyFromSource(config, workspace, anthology);
            Dump(anthology);

            // first merge with existing one
            anthology = MergeNewAnthologyWithExisting(anthology);

            // Generate import files
            GenerateImports(anthology);
        }

        private void GenerateImports(Anthology anthology)
        {
            var targetDir = WellKnownFolders.GetProjectDirectory();
            targetDir.Create();

            foreach(var project in anthology.Projects)
            {
                var projectFile = Path.Combine(WellKnownFolders.MsBuildSolutionDir, project.ProjectFile);
                var binFile = Path.Combine(WellKnownFolders.MsBuildBinDir, project.AssemblyName + project.Extension);
                var srcCondition = string.Format("'$({0}_Src)' != ''", project.AssemblyName.Replace('.', '_'));
                var binCondition = string.Format("'$({0}_Src)' == ''", project.AssemblyName.Replace('.', '_'));

                var xdoc = new XElement(XmlHelpers.NsMsBuild + "Project",
                                        new XElement(XmlHelpers.NsMsBuild + "Import",
                                                     new XAttribute("Project", Path.Combine(WellKnownFolders.MsBuildViewDir, "$(SolutionName).targets")),
                                                     new XAttribute("Condition", "'$(BinSrcConfig)' == ''")),
                                        new XElement(XmlHelpers.NsMsBuild + "ItemGroup",
                                                     new XElement(XmlHelpers.NsMsBuild + "ProjectReference",
                                                                  new XAttribute("Include", projectFile),
                                                                  new XAttribute("Condition", srcCondition),
                                                                  new XElement(XmlHelpers.NsMsBuild + "Project", project.Guid.ToString("B")),
                                                                  new XElement(XmlHelpers.NsMsBuild + "Name", project.AssemblyName)),
                                                     new XElement(XmlHelpers.NsMsBuild + "Reference",
                                                                  new XAttribute("Include", project.AssemblyName),
                                                                  new XAttribute("Condition", binCondition),
                                                                  new XElement(XmlHelpers.NsMsBuild + "HintPath", binFile))));

                var targetFileName = project.Guid + ".targets";
                var prjImport = targetDir.GetFile(targetFileName);
                xdoc.Save(prjImport.FullName);
            }
        }

        private static Anthology UpdateAnthologyFromSource(FullBuildConfig config, DirectoryInfo workspace, Anthology anthology)
        {
            foreach(var repo in config.SourceRepos)
            {
                var repoDir = workspace.GetDirectory(repo.Name);

                // delete all solution files
                var slns = repoDir.EnumerateFiles("*.sln", SearchOption.AllDirectories);
                slns.ForEach(x => x.Delete());

                // process all projects
                var csprojs = repoDir.EnumerateFiles("*.csproj", SearchOption.AllDirectories);

                anthology = csprojs.Aggregate(anthology, (a, p) => ParseAndAddProject(workspace, p, anthology));
            }

            return anthology;
        }

        private static Anthology LoadOrCreateAnthology()
        {
            var admDir = WellKnownFolders.GetAdminDirectory();
            var anthologyFile = admDir.GetFile(Anthology.AnthologyFileName);
            if (anthologyFile.Exists)
            {
                var oldJson = File.ReadAllText(anthologyFile.FullName);
                var prevAnthology = JsonConvert.DeserializeObject<Anthology>(oldJson);
                var oldJ = JObject.FromObject(prevAnthology);
                var anthology = oldJ.ToObject<Anthology>();
                return anthology;
            }

            return new Anthology();
        }

        private static Anthology MergeNewAnthologyWithExisting(Anthology anthology)
        {
            // merge anthology files
            var admDir = WellKnownFolders.GetAdminDirectory();
            var anthologyFile = admDir.GetFile(Anthology.AnthologyFileName);
            if (anthologyFile.Exists)
            {
                var oldJson = File.ReadAllText(anthologyFile.FullName);
                var prevAnthology = JsonConvert.DeserializeObject<Anthology>(oldJson);
                var newJ = JObject.FromObject(anthology);
                var oldJ = JObject.FromObject(prevAnthology);

                var mergeSettings = new JsonMergeSettings {MergeArrayHandling = MergeArrayHandling.Replace};
                oldJ.Merge(newJ, mergeSettings);
                anthology = oldJ.ToObject<Anthology>();
            }

            var json = JsonConvert.SerializeObject(anthology, Formatting.Indented);
            File.WriteAllText(anthologyFile.FullName, json);

            return anthology;
        }

        private static IEnumerable<Package> GetNugetPackages(DirectoryInfo projectDir)
        {
            var packageFile = projectDir.GetFile("packages.config");
            if (!packageFile.Exists)
            {
                return Enumerable.Empty<Package>();
            }

            var docPackage = XDocument.Load(packageFile.FullName);
            var packages = from element in docPackage.Descendants("package")
                           let name = (string) element.Attribute("id")
                           let version = (string) element.Attribute("version")
                           select new Package(name, version);

            return packages;
        }

        private static void Dump(Anthology anthology)
        {
            foreach(var project in anthology.Projects)
            {
                _logger.Debug("Consistency for project {0}", project.GetName());

                foreach(var projectDependency in project.ProjectReferences)
                {
                    var target = anthology.Projects.Single(x => x.Guid == projectDependency);
                    _logger.Debug(" --> {0}", target.GetName());
                }

                foreach(var binaryDependency in project.BinaryReferences)
                {
                    _logger.Debug(" ++ {0}", binaryDependency);
                }
            }
        }

        private static Anthology ParseAndAddProject(DirectoryInfo workspace, FileInfo projectFile, Anthology anthology)
        {
            var xdoc = XDocument.Load(projectFile.FullName);

            var projectFileName = projectFile.FullName.Substring(workspace.FullName.Length + 1);

            var projectGuid = Guid.ParseExact((string) xdoc.Descendants(XmlHelpers.NsMsBuild + "ProjectGuid").Single(), "B");
            var assemblyName = (string) xdoc.Descendants(XmlHelpers.NsMsBuild + "AssemblyName").Single();
            var fxTarget = (string) xdoc.Descendants(XmlHelpers.NsMsBuild + "TargetFrameworkVersion").Single();

            var extension = ((string) xdoc.Descendants(XmlHelpers.NsMsBuild + "OutputType").Single()).InvariantEquals("Library") ? ".dll" : ".exe";

            var projectReferences = from prjRef in xdoc.Descendants(XmlHelpers.NsMsBuild + "ProjectReference").Descendants(XmlHelpers.NsMsBuild + "Project")
                                    select Guid.ParseExact(prjRef.Value, "B");
            var fbProjectReferences = from import in xdoc.Descendants(XmlHelpers.NsMsBuild + "Import")
                                      let importProject = (string) import.Attribute("Project")
                                      where importProject.InvariantStartsWith(WellKnownFolders.MsBuildProjectDir)
                                      let importProjectName = Path.GetFileNameWithoutExtension(importProject)
                                      select Guid.Parse(importProjectName);
            var allProjectReferences = projectReferences.Concat(fbProjectReferences);

            // extract binary references - both nuget and direct reference to assemblies (broken project reference)
            var binaries = from binRef in xdoc.Descendants(XmlHelpers.NsMsBuild + "Reference")
                           let assName = new AssemblyName((string) binRef.Attribute("Include")).Name
                           let maybeHintPath = binRef.Descendants(XmlHelpers.NsMsBuild + "HintPath").SingleOrDefault()
                           select new Binary(assName, null != maybeHintPath ? maybeHintPath.Value.ToUnixSeparator() : null);

            // report spurious binaries reference (System* are mostly OK)
            binaries.Where(x => x.HintPath == null && !x.AssemblyName.InvariantStartsWith("System"))
                    .ForEach(x => _logger.Warn("Spurious assembly reference {0} in project {1}", x.AssemblyName, projectFileName));

            var binaryReferences = binaries.Select(x => x.AssemblyName);

            var nugetPackages = GetNugetPackages(projectFile.Directory);
            var fbPackages = from import in xdoc.Descendants(XmlHelpers.NsMsBuild + "Import")
                             let importProject = (string) import.Attribute("Project")
                             where importProject.InvariantStartsWith(WellKnownFolders.MsBuildPackagesDir)
                             let importProjectName = Path.GetFileNameWithoutExtension(importProject)
                             select new Package(importProjectName, "0.0.0");
            var packages = nugetPackages.Concat(fbPackages);
            var packageNames = packages.Select(x => x.Name);

            var project = new Project(projectGuid, projectFileName, assemblyName, extension, fxTarget, allProjectReferences, binaryReferences, packageNames);
            anthology = anthology.AddOrUpdateProject(project);

            anthology = binaries.Aggregate(anthology, (a, b) => a.AddOrUpdateBinary(b));
            anthology = packages.Aggregate(anthology, (a, p) => a.AddOrUpdatePackages(p));
            return anthology;
        }
    }
}