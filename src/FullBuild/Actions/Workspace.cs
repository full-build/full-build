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
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using FullBuild.Config;
using FullBuild.Helpers;
using FullBuild.Model;
using FullBuild.SourceControl;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace FullBuild.Actions
{
    internal class Workspace
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public void Update()
        {
            var workspace = WellKnownFolders.GetWorkspaceDirectory();
            var config = ConfigManager.GetConfig(workspace);

            // get all csproj in all repos only
            var anthology = LoadOrCreateAnthology();
            anthology = UpdateAnthologyFromSource(config, workspace, anthology);
            Dump(anthology);

            // Promotion
            anthology = PromoteToProject(anthology);

            // merge with existing
            anthology = MergeNewAnthologyWithExisting(anthology);

            // Generate import files
            GenerateImports(anthology);
        }

        private Anthology PromoteToProject(Anthology anthology)
        {
            var pkgsDir = WellKnownFolders.GetPackageDirectory();

            // 1. remove binary reference imported from packages
            foreach(var project in anthology.Projects)
            {
                // gather all assemblies from packages in this project
                var importedAssemblies = (from pkgRef in project.PackageReferences
                                          let pkgdir = pkgsDir.GetDirectory(pkgRef)
                                          where pkgdir.Exists
                                          select Nuspec.Assemblies(pkgdir)).SelectMany(x => x).Distinct(StringComparer.InvariantCultureIgnoreCase);

                // remove imported assemblies
                var newProject = importedAssemblies.Aggregate(project, (p, a) => p.RemoveBinaryReference(a));
                anthology = anthology.AddOrUpdateProject(newProject);
            }

            // 2. promote package to project
            var pkg2prj = from package in anthology.Packages
                          let pkgdir = pkgsDir.GetDirectory(package.Name)
                          where pkgdir.Exists
                          let assemblies = Nuspec.Assemblies(pkgdir)
                          from project in anthology.Projects
                          where assemblies.Contains(project.AssemblyName, StringComparer.InvariantCultureIgnoreCase)
                          select new {Pkg = package, Prj = project};

            foreach(var p2p in pkg2prj)
            {
                _logger.Info("Migrating package {0} to project {1}", p2p.Pkg.Name, p2p.Prj.ProjectFile);
                foreach(var project in anthology.Projects)
                {
                    if (project.PackageReferences.Contains(p2p.Pkg.Name, StringComparer.InvariantCultureIgnoreCase))
                    {
                        var newProject = project.RemovePackageReference(p2p.Pkg.Name);
                        newProject = newProject.AddProjectReference(p2p.Prj.Guid);
                        anthology = anthology.AddOrUpdateProject(newProject);
                    }
                }
            }

            // 3. remove empty package
            var emptyPackages = from package in anthology.Packages
                                let pkgdir = pkgsDir.GetDirectory(package.Name)
                                where pkgdir.Exists
                                let assemblies = Nuspec.Assemblies(pkgdir)
                                where !assemblies.Any()
                                select package;

            foreach(var project in anthology.Projects)
            {
                var newProject = emptyPackages.Aggregate(project, (p, pa) => p.RemovePackageReference(pa.Name));
                anthology = anthology.AddOrUpdateProject(newProject);
            }

            anthology = emptyPackages.Aggregate(anthology, (a, p) => a.RemovePackage(p));

            // 4. remove unused package
            var usedPackages = anthology.Projects.SelectMany(x => x.PackageReferences).Distinct();

            var packagesToRemove = from package in anthology.Packages
                                   where !usedPackages.Contains(package.Name, StringComparer.InvariantCultureIgnoreCase)
                                   select package;

            anthology = packagesToRemove.Aggregate(anthology, (a, p) => a.RemovePackage(p));

            // 5. remove unused binary
            var usedBinaries = anthology.Projects.SelectMany(x => x.BinaryReferences).Distinct();

            var binariesToRemove = from binary in anthology.Binaries
                                   where !usedBinaries.Contains(binary.AssemblyName, StringComparer.InvariantCultureIgnoreCase)
                                   select binary;

            anthology = binariesToRemove.Aggregate(anthology, (a, b) => a.RemoveBinary(b));

            return anthology;
        }

        public void Init(string path)
        {
            var wsDir = new DirectoryInfo(path);
            wsDir.Create();

            var admDir = wsDir.GetDirectory(".full-build");
            if (admDir.Exists)
            {
                throw new ArgumentException("Workspace is already initialized");
            }

            var config = ConfigManager.GetConfig(wsDir);

            var sourceControl = ServiceActivator<Factory>.Create<ISourceControl>(config.SourceControl);
            sourceControl.Clone("full-build", config.AdminRepo, admDir);

            // force a .gitignore
            //var gitIgnoreFile = admDir.GetFile(".gitignore");
            //var gitIgnore = new StringBuilder().AppendLine("cache").AppendLine("projects").AppendLine("views").ToString();
            //File.WriteAllText(gitIgnoreFile.FullName, gitIgnore);

            config = ConfigManager.GetConfig(wsDir);
            foreach(var repo in config.SourceRepos)
            {
                var repoDir = wsDir.GetDirectory(repo.Name);
                sourceControl.Clone(repo.Name, repo.Url, repoDir);
            }
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
            _logger.Info("Anthology contains {0} projects", anthology.Projects.Count());
            foreach(var repo in config.SourceRepos)
            {
                _logger.Info("Processing repo {0}", repo.Name);
                var repoDir = workspace.GetDirectory(repo.Name);

                // delete all solution files
                var slns = repoDir.EnumerateFiles("*.sln", SearchOption.AllDirectories);
                slns.ForEach(x => x.Delete());

                // process all projects
                var csprojs = repoDir.EnumerateFiles("*.csproj", SearchOption.AllDirectories);
                _logger.Info("Found {0} projects", csprojs.Count());

                anthology = csprojs.Aggregate(anthology, (a, p) => ParseAndAddProject(workspace, p, a));

                _logger.Info("Project count {0}", anthology.Projects.Count());
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

        private static IEnumerable<Model.Package> GetNugetPackages(DirectoryInfo projectDir)
        {
            var packageFile = projectDir.GetFile("packages.config");
            if (!packageFile.Exists)
            {
                return Enumerable.Empty<Model.Package>();
            }

            var docPackage = XDocument.Load(packageFile.FullName);
            var packages = from element in docPackage.Descendants("package")
                           let name = (string) element.Attribute("id")
                           let version = (string) element.Attribute("version")
                           select new Model.Package(name, version);

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
            var allProjectReferences = projectReferences.Concat(fbProjectReferences).ToImmutableList();

            // extract binary references - both nuget and direct reference to assemblies (broken project reference)
            var binaries = from binRef in xdoc.Descendants(XmlHelpers.NsMsBuild + "Reference")
                           let assName = new AssemblyName((string) binRef.Attribute("Include")).Name
                           let maybeHintPath = binRef.Descendants(XmlHelpers.NsMsBuild + "HintPath").SingleOrDefault()
                           select new Binary(assName, null != maybeHintPath ? maybeHintPath.Value.ToUnixSeparator() : null);

            // report spurious binaries reference (System* are mostly OK)
            binaries.Where(x => x.HintPath == null && !x.AssemblyName.InvariantStartsWith("System"))
                    .ForEach(x => _logger.Warn("Spurious assembly reference {0} in project {1}", x.AssemblyName, projectFileName));

            var binaryReferences = binaries.Select(x => x.AssemblyName).ToImmutableList();

            var nugetPackages = GetNugetPackages(projectFile.Directory);
            var fbPackages = from import in xdoc.Descendants(XmlHelpers.NsMsBuild + "Import")
                             let importProject = (string) import.Attribute("Project")
                             where importProject.InvariantStartsWith(WellKnownFolders.MsBuildPackagesDir)
                             let importProjectName = Path.GetFileNameWithoutExtension(importProject)
                             select new Model.Package(importProjectName, "0.0.0");
            var packages = nugetPackages.Concat(fbPackages);
            var packageNames = packages.Select(x => x.Name).ToImmutableList();

            var project = new Project(projectGuid, projectFileName, assemblyName, extension, fxTarget, allProjectReferences, binaryReferences, packageNames);
            anthology = anthology.AddOrUpdateProject(project);

            anthology = binaries.Aggregate(anthology, (a, b) => a.AddOrUpdateBinary(b));
            anthology = packages.Aggregate(anthology, (a, p) => a.AddOrUpdatePackages(p));
            return anthology;
        }
    }
}