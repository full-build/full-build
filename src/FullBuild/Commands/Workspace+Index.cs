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

namespace FullBuild.Commands
{
    internal partial class Workspace
    {
        private static void IndexWorkspace()
        {
            var workspace = WellKnownFolders.GetWorkspaceDirectory();
            var config = ConfigManager.LoadConfig();

            var admDir = WellKnownFolders.GetAdminDirectory();

            EnsureProjectGuidsAreUnique(config, workspace);

            // get all csproj in all repos only
            var anthology = Anthology.Load(admDir);
            anthology = UpdateAnthologyFromSource(config, workspace, anthology);
            anthology.Save(admDir);

            // get packages
            Packages.InstallPackages();

            // Promotion
            anthology = AnthologyOptimizer.Optimize(anthology);

            anthology.Save(admDir);

            // Generate import files
            GenerateImports(anthology);
        }

        private static void EnsureProjectGuidsAreUnique(FullBuildConfig config, DirectoryInfo workspace)
        {
            Console.WriteLine("Ensuring projets GUID are unique");

            var existingGuids = new HashSet<Guid>();
            foreach (var repo in config.SourceRepos)
            {
                var repoDir = workspace.GetDirectory(repo.Name);
                if (!repoDir.Exists)
                {
                    continue;
                }

                // process all projects
                var allprojs = repoDir.EnumerateSupportedProjectFiles();

                // scan projects and import
                foreach (var proj in allprojs)
                {
                    var xdoc = XDocument.Load(proj.FullName);

                    // extract infos from project
                    var projectGuid = ExtractProjectGuid(xdoc);
                    if (existingGuids.Contains(projectGuid))
                    {
                        Console.Error.WriteLine("WARNING | Project '{0}' GUID conflicts with other projects", proj.FullName);

                        projectGuid = Guid.NewGuid();
                        xdoc.Descendants(XmlHelpers.NsMsBuild + "ProjectGuid").Single().Value = projectGuid.ToString("B");
                        xdoc.Save(proj.FullName);
                    }

                    existingGuids.Add(projectGuid);
                }
            }
        }

        private static void GenerateImports(Anthology anthology)
        {
            var targetDir = WellKnownFolders.GetProjectDirectory();
            targetDir.Create();

            foreach (var project in anthology.Projects)
            {
                var projectFile = Path.Combine(WellKnownFolders.MsBuildSolutionDir, project.ProjectFile);
                var binFile = Path.Combine(WellKnownFolders.MsBuildBinDir, project.AssemblyName + project.Extension);
                var projectProperty = project.GetProjectPropertyGroupName();
                var srcCondition = string.Format("'$({0})' != ''", projectProperty);
                var binCondition = string.Format("'$({0})' == ''", projectProperty);

                var xdoc = new XElement(XmlHelpers.NsMsBuild + "Project",
                                        new XElement(XmlHelpers.NsMsBuild + "Import",
                                                     new XAttribute("Project", Path.Combine(WellKnownFolders.MsBuildViewDir, "$(SolutionName).targets")),
                                                     new XAttribute("Condition", "'$(FullBuild_Config)' == ''")),
                                        new XElement(XmlHelpers.NsMsBuild + "ItemGroup",
                                                     new XElement(XmlHelpers.NsMsBuild + "ProjectReference",
                                                                  new XAttribute("Include", projectFile),
                                                                  new XAttribute("Condition", srcCondition),
                                                                  new XElement(XmlHelpers.NsMsBuild + "Project", project.Guid.ToString("B")),
                                                                  new XElement(XmlHelpers.NsMsBuild + "Name", project.AssemblyName)),
                                                     new XElement(XmlHelpers.NsMsBuild + "Reference",
                                                                  new XAttribute("Include", project.AssemblyName),
                                                                  new XAttribute("Condition", binCondition),
                                                                  new XElement(XmlHelpers.NsMsBuild + "HintPath", binFile),
                                                                  new XElement(XmlHelpers.NsMsBuild + "Private", "true"))));

                var targetFileName = project.Guid + ".targets";
                var prjImport = targetDir.GetFile(targetFileName);
                xdoc.Save(prjImport.FullName);
            }
        }

        private static Anthology AddPaketDependencies(FileInfo paketDependencies, Anthology anthology)
        {
            var nugetFound = false;
            foreach (var line in File.ReadAllLines(paketDependencies.FullName))
            {
                if (!nugetFound)
                {
                    nugetFound = line.Trim() == "NUGET";
                    continue;
                }

                if (0 == line.Length || (line[0] != ' ' && line[0] != '\t'))
                {
                    break;
                }

                if (!line.Contains('('))
                {
                    continue;
                }

                var items = line.Split(new[] {" ", "\t", "(", ")", ">="}, StringSplitOptions.RemoveEmptyEntries);
                var name = items[0];
                var version = items[1];

                var package = new Package(name, version);
                anthology = anthology.AddOrUpdatePackages(package);
            }

            return anthology;
        }

        private static Anthology UpdateAnthologyFromSource(FullBuildConfig config, DirectoryInfo workspace, Anthology anthology)
        {
            Console.WriteLine("Processing repositories");
            foreach (var repo in config.SourceRepos)
            {
                var repoDir = workspace.GetDirectory(repo.Name);
                if (! repoDir.Exists)
                {
                    continue;
                }

                Console.WriteLine("  {0}", repo.Name);

                // add paket dependencies first
                var paketDependencies = repoDir.EnumeratePaketDependencies();
                anthology = paketDependencies.Aggregate(anthology, (a, f) => AddPaketDependencies(f, a));

                // delete all solution files
                repoDir.EnumerateSolutionFiles().ForEach(x => x.Delete());

                // process all projects
                var allprojs = repoDir.EnumerateSupportedProjectFiles();

                // scan projects and import
                var projectAnthology = allprojs.Aggregate(anthology, (a, p) => ParseAndAddProject(workspace, p, a));

                anthology = projectAnthology.Binaries.Aggregate(anthology, (a, b) => a.AddOrUpdateBinary(b));
                anthology = projectAnthology.Packages.Aggregate(anthology, (a, p) => a.AddOrUpdatePackages(p));
                anthology = projectAnthology.Projects.Aggregate(anthology, (a, p) => a.AddOrUpdateProject(p));
            }

            return anthology;
        }

        private static IEnumerable<Package> GetNuGetPackages(DirectoryInfo projectDir)
        {
            var packageFile = projectDir.GetFile("packages.config");
            if (!packageFile.Exists)
            {
                return Enumerable.Empty<Package>();
            }

            var docPackage = XDocument.Load(packageFile.FullName);
            var packages = from element in docPackage.Descendants("package")
                           let name = (string)element.Attribute("id")
                           let version = (string)element.Attribute("version")
                           select new Package(name, version);

            return packages;
        }

        private static Guid ExtractProjectGuid(XDocument xdoc)
        {
            Guid projectGuid;
            try
            {
                projectGuid = Guid.ParseExact((string)xdoc.Descendants(XmlHelpers.NsMsBuild + "ProjectGuid").Single(), "B");
            }
            catch (Exception)
            {
                // F# project GUID are badly formatted
                projectGuid = Guid.ParseExact((string)xdoc.Descendants(XmlHelpers.NsMsBuild + "ProjectGuid").Single(), "D");
            }

            return projectGuid;
        }

        private static Anthology ParseAndAddProject(DirectoryInfo workspace, FileInfo projectFile, Anthology anthology)
        {
            var xdoc = XDocument.Load(projectFile.FullName);

            // extract infos from project
            var projectFileName = projectFile.FullName.Substring(workspace.FullName.Length + 1).ToUnixSeparator();
            var projectGuid = ExtractProjectGuid(xdoc);

            var assemblyName = (string)xdoc.Descendants(XmlHelpers.NsMsBuild + "AssemblyName").Single();
            var fxTarget = (string)xdoc.Descendants(XmlHelpers.NsMsBuild + "TargetFrameworkVersion").SingleOrDefault() ?? "v4.5";
            var extension = ((string)xdoc.Descendants(XmlHelpers.NsMsBuild + "OutputType").Single()).InvariantEquals("Library")
                ? ".dll"
                : ".exe";

            // check first that project does not exist with same GUID and different project file (duplicated project)
            EnsureGuidIsUnique(anthology, projectGuid, assemblyName, projectFileName);

            // extract project reference along project location to help detecting invalid project references
            var allProjectReferences = GetProjectReferences(xdoc);

            // extract binary references - both nuget and direct reference to assemblies (broken project reference)
            var binaries = GetBinaryReferences(xdoc).ToList();

            // extract all packages (nuget, fullbuild paket and guessed based on binaries)
            var packages = GetPackages(projectFile, xdoc, binaries).ToList();

            // update anthology with this new project
            var binaryReferences = binaries.Select(x => x.AssemblyName).Distinct().ToImmutableList();
            var packageNames = packages.Select(x => x.Name).Distinct().ToImmutableList();
            var project = new Project(projectGuid, projectFileName, assemblyName, extension, fxTarget, allProjectReferences, binaryReferences, packageNames);

            anthology = anthology.AddOrUpdateProject(project);
            anthology = binaries.Aggregate(anthology, (a, b) => a.AddOrUpdateBinary(b));
            anthology = packages.Aggregate(anthology, (a, p) => a.AddOrUpdatePackages(p));

            return anthology;
        }

        private static void EnsureGuidIsUnique(Anthology anthology, Guid projectGuid, string assemblyName, string projectFileName)
        {
            var similarProjects = anthology.Projects.Where(x => x.Guid == projectGuid && (!x.AssemblyName.InvariantEquals(assemblyName) || !x.ProjectFile.InvariantEquals(projectFileName))).ToList();
            if (similarProjects.Any())
            {
                var errMsg = string.Format("ERROR | Project '{0}' conflicts with other projects (same GUID but different location)", projectFileName);
                throw new ProcessingException(errMsg, () => similarProjects.Select(x => x.ProjectFile));
            }
        }

        private static IEnumerable<Package> GetPackages(FileInfo projectFile, XDocument xdoc, IEnumerable<Binary> binaries)
        {
            var nugetPackages = GetNuGetPackages(projectFile.Directory);
            var fullbuildPackages = GetFullBuildPackages(xdoc).ToList();
            var paketPackages = GetPaketPackages(projectFile).ToList();
            var importedPackages = nugetPackages.Concat(fullbuildPackages).Concat(paketPackages).Distinct().ToList();

            // try to guess packages as they could came from missing nuget packages
            var guessedPackages = GuessNuGetPackagesNotCorrectlyDeclared(projectFile, binaries, importedPackages);

            var packages = importedPackages.Concat(guessedPackages);
            return packages;
        }

        private static IEnumerable<Package> GetPaketPackages(FileInfo projectFile)
        {
            var paketFile = projectFile.Directory.GetFile("paket.references");
            var paketPackages = Enumerable.Empty<Package>();
            if (paketFile.Exists)
            {
                paketPackages = from line in File.ReadAllLines(paketFile.FullName)
                                let depPackageName = line.Trim()
                                where !string.IsNullOrEmpty(depPackageName) && !depPackageName.InvariantContains("File:")
                                select new Package(depPackageName, null);
            }
            return paketPackages;
        }

        private static IEnumerable<Package> GuessNuGetPackagesNotCorrectlyDeclared(FileInfo projectFile, IEnumerable<Binary> binaries, IEnumerable<Package> importedPackaged)
        {
            var guessPackages = (from binary in binaries
                                 where null != binary.HintPath && binary.HintPath.InvariantContains("/packages/")
                                 let startOfPackageId = binary.HintPath.InvariantIndexOf("/packages/") + "/packages/".Length
                                 let endOfPackageId = binary.HintPath.InvariantFirstIndexOf(new[] {".0", ".1", ".2", ".3", ".4", ".5", ".6", ".7", ".8", ".9"}, startOfPackageId)
                                 let packageId = binary.HintPath.Substring(startOfPackageId, endOfPackageId - startOfPackageId)
                                 let endOfPackageVersion = binary.HintPath.IndexOf('/', endOfPackageId)
                                 let packageVersion = binary.HintPath.Substring(endOfPackageId + 1, endOfPackageVersion - (endOfPackageId + 1))
                                 select new Package(packageId, packageVersion)).Distinct().ToList();

            var remainingGuessPackages = guessPackages.Where(x => !importedPackaged.Contains(x)).ToList();
            if (remainingGuessPackages.Any())
            {
                Console.WriteLine("WARNING | Project {0} contains package references not declared in packages.config", projectFile.FullName);
                remainingGuessPackages.ForEach(x => Console.Error.WriteLine("        | {0} {1}", x.Name, x.Version));
            }

            return remainingGuessPackages;
        }

        private static IEnumerable<Package> GetFullBuildPackages(XDocument xdoc)
        {
            var fbPackages = from import in xdoc.Descendants(XmlHelpers.NsMsBuild + "Import")
                             let importProject = (string)import.Attribute("Project")
                             where importProject.InvariantStartsWith(WellKnownFolders.MsBuildPackagesDir)
                             let importProjectName = Path.GetFileNameWithoutExtension(importProject)
                             select new Package(importProjectName, null);
            return fbPackages;
        }

        private static IEnumerable<Binary> GetBinaryReferences(XDocument xdoc)
        {
            var binaries = from binRef in xdoc.Descendants(XmlHelpers.NsMsBuild + "Reference")
                           let include = ((string)binRef.Attribute("Include")).Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)[0]
                           where ! string.IsNullOrEmpty(include)
                           let assName = new AssemblyName(include).Name
                           let maybeHintPath = binRef.Descendants(XmlHelpers.NsMsBuild + "HintPath").SingleOrDefault()
                           select new Binary(assName, null != maybeHintPath
                               ? maybeHintPath.Value.ToUnixSeparator()
                               : null);
            return binaries;
        }

        private static ImmutableList<Guid> GetProjectReferences(XDocument xdoc)
        {
            var projectRefWithLocation = from prjRef in xdoc.Descendants(XmlHelpers.NsMsBuild + "ProjectReference")
                                         let guid = Guid.ParseExact(prjRef.Element(XmlHelpers.NsMsBuild + "Project").Value, "B")
                                         let include = prjRef.Attribute("Include").Value
                                         select new {Guid = guid, Include = include};

            // extract project references
            var projectReferences = projectRefWithLocation.Select(x => x.Guid);
            var fbProjectReferences = from import in xdoc.Descendants(XmlHelpers.NsMsBuild + "Import")
                                      let importProject = (string)import.Attribute("Project")
                                      where importProject.InvariantStartsWith(WellKnownFolders.MsBuildProjectDir)
                                      let importProjectName = Path.GetFileNameWithoutExtension(importProject)
                                      select Guid.Parse(importProjectName);
            var allProjectReferences = projectReferences.Union(fbProjectReferences).ToImmutableList();
            return allProjectReferences;
        }
    }
}
