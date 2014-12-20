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
using System.Xml.Linq;
using FullBuild.Config;
using FullBuild.Helpers;
using FullBuild.Model;

namespace FullBuild.Commands
{
    internal partial class Workspace
    {
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

        private static void ConvertProjects()
        {
            var wsDir = WellKnownFolders.GetWorkspaceDirectory();
            var admDir = WellKnownFolders.GetAdminDirectory();

            var anthology = Anthology.Load(admDir);
            foreach (var projectDef in anthology.Projects)
            {
                _logger.Debug("Converting project {0}", projectDef.GetName());

                var projectFile = wsDir.GetFile(projectDef.ProjectFile);
                if (! projectFile.Exists)
                {
                    continue;
                }

                // get template according to project type
                var templateFileName = "Template" + projectFile.Extension;
                var templateFile = admDir.GetFile(templateFileName);

                // extract info from real project
                var realProjectDoc = XDocument.Load(projectFile.FullName);
                var rootNamespace = realProjectDoc.Descendants(XmlHelpers.NsMsBuild + "RootNamespace").Single().Value;
                var outputType = realProjectDoc.Descendants(XmlHelpers.NsMsBuild + "OutputType").Single().Value;
                var signAssembly = realProjectDoc.Descendants(XmlHelpers.NsMsBuild + "SignAssembly").SingleOrDefault();
                var originatorKeyFile = realProjectDoc.Descendants(XmlHelpers.NsMsBuild + "AssemblyOriginatorKeyFile").SingleOrDefault();
                var childrenOfItemGroup = from ig in realProjectDoc.Descendants(XmlHelpers.NsMsBuild + "ItemGroup").Elements()
                                          where ig.Name.LocalName != "Reference"
                                                && ig.Name.LocalName != "ProjectReference"
                                                && ig.Name.LocalName != "BootstrapperPackage"
                                          select ig;
                var applicationIcon = realProjectDoc.Descendants(XmlHelpers.NsMsBuild + "ApplicationIcon").SingleOrDefault();

                // prepare project structure either from template or original project file
                var templateForProjectFile = templateFile.Exists
                    ? templateFile
                    : projectFile;
                var xdoc = XDocument.Load(templateForProjectFile.FullName);

                // remove all import from .full-build and project reference
                CleanUpProject(xdoc);

                // setup project guid
                SetProjectOutputSettings(xdoc, projectDef, outputType, rootNamespace, signAssembly, originatorKeyFile, applicationIcon);

                var propertyGroup = xdoc.Root.Elements(XmlHelpers.NsMsBuild + "PropertyGroup").Last();
                var itemGroupReference = new XElement(XmlHelpers.NsMsBuild + "ItemGroup");
                var itemGroupFile = new XElement(XmlHelpers.NsMsBuild + "ItemGroup");

                propertyGroup.AddAfterSelf(itemGroupReference);
                itemGroupReference.AddAfterSelf(itemGroupFile);

                // generate binary references
                AddBinaryReferences(projectDef, anthology, itemGroupReference);

                // add imports to project reference
                AddProjectReferences(projectDef, anthology, itemGroupFile);

                // add packages
                AddPackageReferences(projectDef, anthology, itemGroupFile);

                // add files
                if (templateFile.Exists)
                {
                    itemGroupFile.Add(childrenOfItemGroup);
                }

                // remove nuget packages file
                RemoveNuGetStuff(xdoc, projectFile);

                xdoc.Save(projectFile.FullName);
            }

            GenerateImports(anthology);

            // remove nuget folders
            var config = ConfigManager.LoadConfig();
            foreach (var repo in config.SourceRepos)
            {
                var repoDir = wsDir.GetDirectory(repo.Name);
                if (repoDir.Exists)
                {
                    RecursiveDeleteDirectoryButVcs(repoDir);
                }
            }
        }

        private static void RecursiveDeleteDirectoryButVcs(DirectoryInfo dir)
        {
            if (dir.Name.InvariantEquals(".hg") || dir.Name.InvariantEquals(".git"))
            {
                return;
            }

            if (dir.Name.InvariantEquals(".nuget"))
            {
                Reliability.Do(() => dir.Delete(true));
                return;
            }

            dir.GetDirectories().ForEach(RecursiveDeleteDirectoryButVcs);
        }

        private static void SetProjectOutputSettings(XDocument xdoc, Project projectDef, string outputType, string rootNamespace, XElement signAssembly, XElement originatorKeyFile,
                                                     XElement applicationIcon)
        {
            xdoc.Descendants(XmlHelpers.NsMsBuild + "ProjectGuid").Single().Value = projectDef.Guid.ToString("B");
            xdoc.Descendants(XmlHelpers.NsMsBuild + "OutputType").Single().Value = outputType;
            xdoc.Descendants(XmlHelpers.NsMsBuild + "RootNamespace").Single().Value = rootNamespace;
            xdoc.Descendants(XmlHelpers.NsMsBuild + "AssemblyName").Single().Value = projectDef.AssemblyName;
            xdoc.Descendants(XmlHelpers.NsMsBuild + "TargetFrameworkVersion").Single().Value = projectDef.FxTarget;

            if (null != signAssembly)
            {
                var targetSignAssembly = xdoc.Descendants(XmlHelpers.NsMsBuild + "SignAssembly").SingleOrDefault();
                if (null != targetSignAssembly)
                {
                    targetSignAssembly.Value = signAssembly.Value;
                }
            }

            if (null != originatorKeyFile)
            {
                var targetAssemblyOriginatorKeyFile = xdoc.Descendants(XmlHelpers.NsMsBuild + "AssemblyOriginatorKeyFile").SingleOrDefault();
                if (null != targetAssemblyOriginatorKeyFile)
                {
                    targetAssemblyOriginatorKeyFile.Value = originatorKeyFile.Value;
                }
            }

            if (null != applicationIcon)
            {
                var targetApplicationIcon = xdoc.Descendants(XmlHelpers.NsMsBuild + "ApplicationIcon").SingleOrDefault();
                if (null != targetApplicationIcon)
                {
                    targetApplicationIcon.Value = applicationIcon.Value;
                }
            }
        }

        private static void RemoveNuGetStuff(XDocument xdoc, FileInfo projectFile)
        {
            xdoc.Descendants(XmlHelpers.NsMsBuild + "None").Where(x => null != x.Attribute("Include") && x.Attribute("Include").Value.InvariantEquals("packages.config")).Remove();
            projectFile.Directory.GetFile("packages.config").Delete();
        }

        private static void AddPackageReferences(Project projectDef, Anthology anthology, XElement itemGroupFile)
        {
            foreach (var refPkg in projectDef.PackageReferences)
            {
                var pkg = anthology.Packages.SingleOrDefault(x => x.Name.InvariantEquals(refPkg));
                var refPackage = pkg;
                var packageFileName = refPackage.Name + ".targets";
                var import = Path.Combine(WellKnownFolders.MsBuildPackagesDir, packageFileName).ToUnixSeparator();
                var condition = string.Format("'$(FullBuild_{0}_Pkg)' == ''", refPackage.Name.ToMsBuild());
                var newProjectRef = new XElement(XmlHelpers.NsMsBuild + "Import", new XAttribute("Project", import), new XAttribute("Condition", condition));
                itemGroupFile.AddBeforeSelf(newProjectRef);
            }
        }

        private static void AddProjectReferences(Project projectDef, Anthology anthology, XElement itemGroupFile)
        {
            foreach (var refGuid in projectDef.ProjectReferences)
            {
                var project = anthology.Projects.SingleOrDefault(x => x.Guid == refGuid);
                if (null == project)
                {
                    var errMsg = string.Format("Project {0:B} references unknown project {1:B}", projectDef.Guid, refGuid);
                    throw new ProcessingException(errMsg, Enumerable.Empty<string>);
                }

                var refProject = project;
                var targetFileName = refProject.Guid + ".targets";
                var import = Path.Combine(WellKnownFolders.MsBuildProjectDir, targetFileName).ToUnixSeparator();
                var newProjectRef = new XElement(XmlHelpers.NsMsBuild + "Import", new XAttribute("Project", import));
                itemGroupFile.AddBeforeSelf(newProjectRef);
            }
        }

        private static void AddBinaryReferences(Project projectDef, Anthology anthology, XElement itemGroupReference)
        {
            var spuriousReferences = new List<string>();
            foreach (var refBin in projectDef.BinaryReferences)
            {
                var bin = anthology.Binaries.SingleOrDefault(x => x.AssemblyName.InvariantEquals(refBin));
                var hintPath = null != bin.HintPath
                    ? new XElement(XmlHelpers.NsMsBuild + "HintPath", bin.HintPath)
                    : null;
                var binReference = new XElement(XmlHelpers.NsMsBuild + "Reference",
                                                new XAttribute("Include", bin.AssemblyName),
                                                hintPath);
                itemGroupReference.Add(binReference);

                if (null != hintPath)
                {
                    spuriousReferences.Add(bin.AssemblyName);
                }
            }

            if (0 < spuriousReferences.Count)
            {
                Console.Error.WriteLine("WARNING | Project {0} has spurious binary references", projectDef.ProjectFile);
                foreach (var spuriousRef in spuriousReferences)
                {
                    Console.Error.WriteLine("        | {0}", spuriousRef);
                }
            }
        }

        private static void CleanUpProject(XDocument xdoc)
        {
            xdoc.Descendants(XmlHelpers.NsMsBuild + "ProjectReference").Remove();
            xdoc.Descendants(XmlHelpers.NsMsBuild + "Import").Where(x => x.Attribute("Project").Value.InvariantStartsWith(WellKnownFolders.MsBuildProjectDir)).Remove();
            xdoc.Descendants(XmlHelpers.NsMsBuild + "Reference").Remove();
            xdoc.Descendants(XmlHelpers.NsMsBuild + "Import").Where(x => x.Attribute("Project").Value.InvariantContains("NuGet.targets")).Remove();
            xdoc.Descendants(XmlHelpers.NsMsBuild + "Import").Where(x => x.Attribute("Project").Value.InvariantContains("paket.targets")).Remove();
            xdoc.Descendants(XmlHelpers.NsMsBuild + "Import").Where(x => x.Attribute("Project").Value.InvariantStartsWith(WellKnownFolders.RelativeAdminDirectory)).Remove();
            xdoc.Descendants(XmlHelpers.NsMsBuild + "Import").Where(x => x.Attribute("Project").Value.InvariantStartsWith(WellKnownFolders.MsBuildPackagesDir)).Remove();
            xdoc.Descendants(XmlHelpers.NsMsBuild + "Target").Where(x => x.Attribute("Name").Value.InvariantEquals("EnsureNuGetPackageBuildImports")).Remove();
            xdoc.Descendants(XmlHelpers.NsMsBuild + "RestorePackages").Remove();
            xdoc.Descendants(XmlHelpers.NsMsBuild + "ItemGroup").Where(x => !x.DescendantNodes().Any()).Remove();
        }
    }
}
