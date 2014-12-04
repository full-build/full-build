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
                var targetFramework = realProjectDoc.Descendants(XmlHelpers.NsMsBuild + "TargetFrameworkVersion").SingleOrDefault();
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
                xdoc.Descendants(XmlHelpers.NsMsBuild + "Choose").Where(x => !x.Descendants(XmlHelpers.NsMsBuild + "FSharpTargetsPath").Any()).Remove();

                // setup project guid
                xdoc.Descendants(XmlHelpers.NsMsBuild + "ProjectGuid").Single().Value = projectDef.Guid.ToString("B");
                xdoc.Descendants(XmlHelpers.NsMsBuild + "OutputType").Single().Value = outputType;
                xdoc.Descendants(XmlHelpers.NsMsBuild + "RootNamespace").Single().Value = rootNamespace;
                xdoc.Descendants(XmlHelpers.NsMsBuild + "AssemblyName").Single().Value = projectDef.AssemblyName;

                if (null != targetFramework)
                {
                    xdoc.Descendants(XmlHelpers.NsMsBuild + "TargetFrameworkVersion").Single().Value = targetFramework.Value;
                }

                if (null != signAssembly)
                {
                    xdoc.Descendants(XmlHelpers.NsMsBuild + "SignAssembly").Single().Value = signAssembly.Value;
                }

                if (null != originatorKeyFile)
                {
                    xdoc.Descendants(XmlHelpers.NsMsBuild + "AssemblyOriginatorKeyFile").Single().Value = originatorKeyFile.Value;
                }

                if (null != applicationIcon)
                {
                    xdoc.Descendants(XmlHelpers.NsMsBuild + "ApplicationIcon").Single().Value = applicationIcon.Value;
                }

                var propertyGroup = xdoc.Root.Elements(XmlHelpers.NsMsBuild + "PropertyGroup").Last();
                var itemGroupReference = new XElement(XmlHelpers.NsMsBuild + "ItemGroup");
                var itemGroupFile = new XElement(XmlHelpers.NsMsBuild + "ItemGroup");

                propertyGroup.AddAfterSelf(itemGroupReference);
                itemGroupReference.AddAfterSelf(itemGroupFile);

                // generate binary references
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

                // add imports to project reference
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

                // add packages
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

                // add files
                itemGroupFile.Add(childrenOfItemGroup);

                xdoc.Save(projectFile.FullName);

                // remove nuget packages file
                projectFile.Directory.GetFile("packages.config").Delete();
            }

            // remove nuget folders
            var config = ConfigManager.LoadConfig();
            foreach (var repo in config.SourceRepos)
            {
                var repoDir = wsDir.GetDirectory(repo.Name);
                if (repoDir.Exists)
                {
                    repoDir.EnumerateNugetDirectories().ForEach(x =>
                                                                {
                                                                    try
                                                                    {
                                                                        x.Refresh();
                                                                        x.Delete(true);
                                                                    }
                                                                    catch
                                                                    {
                                                                    }
                                                                });
                }
            }
        }
    }
}
