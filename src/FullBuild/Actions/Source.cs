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

namespace FullBuild.Actions
{
    internal class Source
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private static Project FindProjectFromGuid(Anthology anthology, Guid guid)
        {
            var project = anthology.Projects.SingleOrDefault(x => x.Guid == guid);
            return project;
        }

        private static Model.Package FindPackageFromAssemblyName(Anthology anthology, string pkgName)
        {
            var pkg = anthology.Packages.SingleOrDefault(x => x.Name.InvariantEquals(pkgName));
            return pkg;
        }

        private static Binary FindBinaryFromAssemblyName(Anthology anthology, string assName)
        {
            var bin = anthology.Binaries.SingleOrDefault(x => x.AssemblyName.InvariantEquals(assName));
            return bin;
        }

        public void Fix()
        {
            var wsDir = WellKnownFolders.GetWorkspaceDirectory();
            var admDir = WellKnownFolders.GetAdminDirectory();
            var anthologyFile = admDir.GetFile(Anthology.AnthologyFileName);
            var oldJson = File.ReadAllText(anthologyFile.FullName);
            var anthology = JsonConvert.DeserializeObject<Anthology>(oldJson);

            foreach(var projectDef in anthology.Projects)
            {
                var projectFile = wsDir.GetFile(projectDef.ProjectFile);
                var xdoc = XDocument.Load(projectFile.FullName);

                _logger.Debug("Fixing project {0}", projectDef.GetName());

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

                var firstItemGroup = xdoc.Descendants(XmlHelpers.NsMsBuild + "ItemGroup").First();

                // generate binary references
                if (projectDef.BinaryReferences.Any(x => ! x.InvariantStartsWith("System")))
                {
                    _logger.Warn("Project has spurious binary reference");
                }

                foreach(var refBin in projectDef.BinaryReferences)
                {
                    var bin = FindBinaryFromAssemblyName(anthology, refBin);
                    if (null == bin)
                    {
                        Console.WriteLine("Failed to find binary from assembly name {0}", refBin);
                    }

                    var hintPath = null != bin.HintPath ? new XElement(XmlHelpers.NsMsBuild + "HintPath", bin.HintPath) : null;
                    var binReference = new XElement(XmlHelpers.NsMsBuild + "Reference",
                                                    new XAttribute("Include", bin.AssemblyName),
                                                    hintPath);
                    firstItemGroup.Add(binReference);
                }

                // add imports to project reference
                foreach(var refGuid in projectDef.ProjectReferences)
                {
                    var refProject = FindProjectFromGuid(anthology, refGuid);
                    var targetFileName = refProject.Guid + ".targets";
                    var import = Path.Combine(WellKnownFolders.MsBuildProjectDir, targetFileName);
                    var newProjectRef = new XElement(XmlHelpers.NsMsBuild + "Import", new XAttribute("Project", import));
                    firstItemGroup.AddAfterSelf(newProjectRef);
                }

                // add packages
                foreach(var refPkg in projectDef.PackageReferences)
                {
                    var refPackage = FindPackageFromAssemblyName(anthology, refPkg);
                    var packageFileName = refPackage.Name + ".targets";
                    var import = Path.Combine(WellKnownFolders.MsBuildPackagesDir, packageFileName);
                    var condition = string.Format("'$({0}_Pkg)' == ''", refPackage.Name.Replace('-', '_'));
                    var newProjectRef = new XElement(XmlHelpers.NsMsBuild + "Import", new XAttribute("Project", import), new XAttribute("Condition", condition));
                    firstItemGroup.AddAfterSelf(newProjectRef);
                }

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