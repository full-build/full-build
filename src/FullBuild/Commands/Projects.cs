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
using System.Xml.Linq;
using FullBuild.Helpers;
using FullBuild.Model;
using Newtonsoft.Json;
using NLog;

namespace FullBuild.Commands
{
    internal class Projects
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public void Convert()
        {
            var wsDir = WellKnownFolders.GetWorkspaceDirectory();
            var admDir = WellKnownFolders.GetAdminDirectory();
            var anthologyFile = admDir.GetFile(Anthology.AnthologyFileName);
            var oldJson = File.ReadAllText(anthologyFile.FullName);
            var anthology = JsonConvert.DeserializeObject<Anthology>(oldJson);
            var templateFile = admDir.GetFile("Template.csproj");
            foreach(var projectDef in anthology.Projects)
            {
                _logger.Debug("Fixing project {0}", projectDef.GetName());
                
                FileInfo projectFile = wsDir.GetFile(projectDef.ProjectFile);
                var realProjectDoc = XDocument.Load(projectFile.FullName);

                // extract info from real project
                var rootNamespace = realProjectDoc.Descendants(XmlHelpers.NsMsBuild + "RootNamespace").Single().Value;
                var outputType = realProjectDoc.Descendants(XmlHelpers.NsMsBuild + "OutputType").Single().Value;
                var signAssembly = realProjectDoc.Descendants(XmlHelpers.NsMsBuild + "SignAssembly").SingleOrDefault();
                var originatorKeyFile = realProjectDoc.Descendants(XmlHelpers.NsMsBuild + "AssemblyOriginatorKeyFile").SingleOrDefault();
                var targetFramework = realProjectDoc.Descendants(XmlHelpers.NsMsBuild + "TargetFrameworkVersion").Single().Value;
                var childrenOfItemGroup = from ig in realProjectDoc.Descendants(XmlHelpers.NsMsBuild + "ItemGroup").Elements()
                                          where ig.Name.LocalName != "Reference" 
                                                && ig.Name.LocalName != "ProjectReference"
                                                && ig.Name.LocalName != "BootstrapperPackage"
                                          select ig;
                var applicationIcon = realProjectDoc.Descendants(XmlHelpers.NsMsBuild + "ApplicationIcon").SingleOrDefault();

                // prepare project structure either from template or original project file
                var templateForProjectFile = templateFile.Exists ? templateFile : projectFile;
                var xdoc = XDocument.Load(templateForProjectFile.FullName);
                
                // remove all import from .full-build and project reference
                xdoc.Descendants(XmlHelpers.NsMsBuild + "ProjectReference").Remove();
                xdoc.Descendants(XmlHelpers.NsMsBuild + "Import").Where(x => x.Attribute("Project").Value.InvariantContains(WellKnownFolders.RelativeAdminDirectory)).Remove();
                xdoc.Descendants(XmlHelpers.NsMsBuild + "Reference").Remove();
                xdoc.Descendants(XmlHelpers.NsMsBuild + "Import").Where(x => x.Attribute("Project").Value.InvariantContains("NuGet.targets")).Remove();
                xdoc.Descendants(XmlHelpers.NsMsBuild + "Import").Where(x => x.Attribute("Project").Value.InvariantStartsWith(WellKnownFolders.RelativeAdminDirectory)).Remove();
                xdoc.Descendants(XmlHelpers.NsMsBuild + "Import").Where(x => x.Attribute("Project").Value.InvariantStartsWith(WellKnownFolders.MsBuildPackagesDir)).Remove();
                xdoc.Descendants(XmlHelpers.NsMsBuild + "Target").Where(x => x.Attribute("Name").Value.InvariantEquals("EnsureNuGetPackageBuildImports")).Remove();
                xdoc.Descendants(XmlHelpers.NsMsBuild + "RestorePackages").Remove();
                xdoc.Descendants(XmlHelpers.NsMsBuild + "ItemGroup").Where(x => !x.DescendantNodes().Any()).Remove();

                // setup project guid
                xdoc.Descendants(XmlHelpers.NsMsBuild + "ProjectGuid").Single().Value = projectDef.Guid.ToString("B");
                xdoc.Descendants(XmlHelpers.NsMsBuild + "OutputType").Single().Value = outputType;
                xdoc.Descendants(XmlHelpers.NsMsBuild + "RootNamespace").Single().Value = rootNamespace;
                xdoc.Descendants(XmlHelpers.NsMsBuild + "AssemblyName").Single().Value = projectDef.AssemblyName;
                xdoc.Descendants(XmlHelpers.NsMsBuild + "TargetFrameworkVersion").Single().Value = targetFramework;
                if (null != signAssembly)
                {
                    xdoc.Descendants(XmlHelpers.NsMsBuild + "SignAssembly").Single().Value = signAssembly.Value;
                    xdoc.Descendants(XmlHelpers.NsMsBuild + "AssemblyOriginatorKeyFile").Single().Value = originatorKeyFile.Value;
                }
                if (null != applicationIcon)
                {
                    xdoc.Descendants(XmlHelpers.NsMsBuild + "ApplicationIcon").Single().Value = applicationIcon.Value;
                }

                var propertyGroup = xdoc.Descendants(XmlHelpers.NsMsBuild + "PropertyGroup").Last();
                var itemGroupReference = new XElement(XmlHelpers.NsMsBuild + "ItemGroup");
                var itemGroupProject = new XElement(XmlHelpers.NsMsBuild + "ItemGroup");
                var itemGroupPackage = new XElement(XmlHelpers.NsMsBuild + "ItemGroup");
                var itemGroupFile = new XElement(XmlHelpers.NsMsBuild + "ItemGroup");
                
                propertyGroup.AddAfterSelf(itemGroupReference);
                itemGroupReference.AddAfterSelf(itemGroupProject);
                itemGroupProject.AddAfterSelf(itemGroupPackage);
                itemGroupPackage.AddAfterSelf(itemGroupFile);
                
                // generate binary references
                var spuriousReferences = projectDef.BinaryReferences.Where(x => !x.InvariantStartsWith("System"));
                if (spuriousReferences.Any())
                {
                    Console.WriteLine("WARNING: Project {0} has spurious binary references", projectDef.ProjectFile);
                    foreach (var spuriousRef in spuriousReferences)
                    {
                        Console.WriteLine("\t{0}", spuriousRef);
                    }
                }

                foreach (var refBin in projectDef.BinaryReferences)
                {
                    var bin = anthology.Binaries.SingleOrDefault(x => x.AssemblyName.InvariantEquals(refBin));
                    var hintPath = null != bin.HintPath ? new XElement(XmlHelpers.NsMsBuild + "HintPath", bin.HintPath) : null;
                    var binReference = new XElement(XmlHelpers.NsMsBuild + "Reference",
                                                    new XAttribute("Include", bin.AssemblyName),
                                                    hintPath);
                    itemGroupReference.Add(binReference);
                }

                // add imports to project reference
                foreach (var refGuid in projectDef.ProjectReferences)
                {
                    var project = anthology.Projects.SingleOrDefault(x => x.Guid == refGuid);
                    var refProject = project;
                    var targetFileName = refProject.Guid + ".targets";
                    var import = Path.Combine(WellKnownFolders.MsBuildProjectDir, targetFileName).ToUnixSeparator();
                    var newProjectRef = new XElement(XmlHelpers.NsMsBuild + "Import", new XAttribute("Project", import));
                    itemGroupProject.AddAfterSelf(newProjectRef);
                }

                // add packages
                foreach (var refPkg in projectDef.PackageReferences)
                {
                    var pkg = anthology.Packages.SingleOrDefault(x => x.Name.InvariantEquals(refPkg));
                    var refPackage = pkg;
                    var packageFileName = refPackage.Name + ".targets";
                    var import = Path.Combine(WellKnownFolders.MsBuildPackagesDir, packageFileName).ToUnixSeparator();
                    var condition = string.Format("'$({0}_Pkg)' == ''", refPackage.Name.Replace('-', '_'));
                    var newProjectRef = new XElement(XmlHelpers.NsMsBuild + "Import", new XAttribute("Project", import), new XAttribute("Condition", condition));
                    itemGroupPackage.AddAfterSelf(newProjectRef);
                }

                // add files
                itemGroupFile.Add(childrenOfItemGroup);

                xdoc.Save(projectFile.FullName);

                // remove nuget packages file
                var projectDir = projectFile.Directory;
                var packagesConfig = projectDir.GetFile("packages.config");
                if (packagesConfig.Exists)
                {
                    packagesConfig.Delete();
                }
            }
        }
    }
}