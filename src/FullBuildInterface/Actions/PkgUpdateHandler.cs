﻿// Copyright (c) 2014, Pierre Chalamet
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using FullBuildInterface.Model;
using Newtonsoft.Json;
using NLog;

namespace FullBuildInterface.Actions
{
    internal class PkgUpdateHandler : Handler<PkgUpdateOptions>
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        protected override void ExecuteWithOptions(PkgUpdateOptions initAnthologyUpdateOptions)
        {
            // read anthology.json
            var admDir = WellKnownFolders.GetAdminDirectory();
            var anthologyFile = admDir.GetFile(Anthology.AnthologyFileName);
            var json = File.ReadAllText(anthologyFile.FullName);
            var anthology = JsonConvert.DeserializeObject<Anthology>(json);

            foreach(var pkg in anthology.Packages)
            {
                Nuget.InstallPackage(pkg);
            }

            var pkgsDir = WellKnownFolders.GetPackageDirectory();
            var packageToRemove = new List<Package>();
            foreach(var pkg in anthology.Packages)
            {
                var pkgDir = pkgsDir.GetDirectory(pkg.Name);
                var pkgAssemblies = Nuspec.Assemblies(pkgDir);
                var forceRemove = ! pkgAssemblies.Any();

                // remove binaries declared in packages
                var binAssembliesToRemove = (from pkgAssembly in pkgAssemblies
                                             from binAssembly in anthology.Binaries
                                             where pkgAssembly.InvariantEquals(binAssembly.AssemblyName)
                                             select binAssembly).ToList();

                anthology = binAssembliesToRemove.Aggregate(anthology, (a, b) => a.RemoveBinary(b));

                binAssembliesToRemove.ForEach(x => anthology.Projects.ForEach(y => y.BinaryReferences.Remove(x.AssemblyName)));

                // remove packages identified as project
                var projectForPackage = (from pkgAssembly in pkgAssemblies
                                         from project in anthology.Projects
                                         where pkgAssembly.InvariantEquals(project.AssemblyName)
                                         select project);
                var distinctProjectName = (from p in projectForPackage
                                           select p.AssemblyName).Distinct();

                if (forceRemove)
                {
                    packageToRemove.Add(pkg);
                    anthology.Projects.ForEach(x => x.PackageReferences.Remove(pkg.Name));
                }

                // several projects with same output can contribute to the package
                if (1 == distinctProjectName.Count() && 1 < projectForPackage.Count())
                {
                    projectForPackage.ForEach(x => _logger.Warn("Nugets generated probably using more than one project {0}", x.ProjectFile));
                }

                // single project found ==> we can safely migrate the package to project
                if (1 == projectForPackage.Count())
                {
                    packageToRemove.Add(pkg);
                    anthology.Projects.Where(x => x.PackageReferences.Contains(pkg.Name)).ForEach(x => x.ProjectReferences.Add(projectForPackage.Single().Guid));
                    anthology.Projects.ForEach(x => x.PackageReferences.Remove(pkg.Name));
                }

                GenerateTargetsForProject(pkg);
            }

            // remove unused packages
            anthology = packageToRemove.Aggregate(anthology, (a, p) => a.RemovePackage(p));

            var newJson = JsonConvert.SerializeObject(anthology, Formatting.Indented);
            File.WriteAllText(anthologyFile.FullName, newJson);
        }

        private static XElement Generatewhen(IEnumerable<string> foldersToTry, string fxVersion, DirectoryInfo libDir)
        {
            foreach(var folderToTry in foldersToTry)
            {
                var fxLibs = libDir.GetDirectory(folderToTry);
                if (fxLibs.Exists)
                {
                    var condition = string.Format("'$(TargetFrameworkVersion)' == '{0}'", fxVersion);
                    var when = new XElement(XmlHelpers.NsMsBuild + "When",
                                            new XAttribute("Condition", condition),
                                            GenerateItemGroup(fxLibs));
                    return when;
                }
            }

            return null;
        }

        private void GenerateTargetsForProject(Package package)
        {
            var pkgsDir = WellKnownFolders.GetPackageDirectory();
            var pkgDir = pkgsDir.GetDirectory(package.Name);
            var libDir = pkgDir.GetDirectory("lib");

            var whens = new List<XElement>();
            if (libDir.Exists)
            {
                for(var i = 0; i < FrameworkVersion.CompatibilityOrder.Length; ++i)
                {
                    var fxVersion = FrameworkVersion.CompatibilityOrder[i];

                    XElement when = null;
                    for(var j = i; j >= 0; --j)
                    {
                        var substituteVersion = FrameworkVersion.CompatibilityOrder[j];
                        var foldersToTry = FrameworkVersion.FxVersion2Folder[substituteVersion];

                        when = Generatewhen(foldersToTry, fxVersion, libDir);
                        if (null != when)
                        {
                            break;
                        }
                    }

                    if (null == when)
                    {
                        when = Generatewhen(new[] {""}, fxVersion, libDir);
                    }

                    whens.Add(when);
                }
            }

            var nuspecFileName = string.Format("{0}.nuspec", package.Name);
            var nuspecFile = new FileInfo(Path.Combine(pkgDir.FullName, nuspecFileName));
            var xdocNuspec = XDocument.Load(nuspecFile.FullName);
            var dependencies = from d in xdocNuspec.Descendants(XmlHelpers.NsNuget + "dependency")
                               select (string) d.Attribute("id");

            var imports = from dependency in dependencies
                          let dependencyPackageFileName = dependency + ".targets"
                          let dependencyTargets = Path.Combine(WellKnownFolders.MsBuildPackagesDir, dependencyPackageFileName)
                          let condition = string.Format("'$({0}_Pkg)' == ''", dependency.Replace('-', '_'))
                          select new XElement(XmlHelpers.NsMsBuild + "Import",
                                              new XAttribute("Project", dependencyTargets),
                                              new XAttribute("Condition", condition));

            var choose = whens.Any()
                ? new XElement(XmlHelpers.NsMsBuild + "Choose", whens)
                : GenerateItemGroup(libDir);

            var defineName = string.Format("{0}_Pkg", package.Name.Replace('-', '_'));
            var define = new XElement(XmlHelpers.NsMsBuild + "PropertyGroup",
                                      new XElement(XmlHelpers.NsMsBuild + defineName, "Y"));

            var project = new XElement(XmlHelpers.NsMsBuild + "Project", define, imports, choose);
            var xdoc = new XDocument(project);

            var packageFileName = package.Name + ".targets";
            var targetsFile = pkgsDir.GetFile(packageFileName);
            xdoc.Save(targetsFile.FullName);
        }

        private static XElement GenerateItemGroup(DirectoryInfo dir)
        {
            if (! dir.Exists)
            {
                return null;
            }

            var len = WellKnownFolders.GetWorkspaceDirectory().FullName.Length + 1;
            var files = dir.EnumerateFiles("*.dll").Concat(dir.EnumerateFiles("*.exe"));
            var imports = from file in files
                          let assemblyName = Path.GetFileNameWithoutExtension(file.FullName)
                          let relativePath = file.FullName.Substring(len)
                          let hintPath = Path.Combine(WellKnownFolders.MsBuildSolutionDir, relativePath)
                          select new XElement(XmlHelpers.NsMsBuild + "Reference",
                                              new XAttribute("Include", assemblyName),
                                              new XElement(XmlHelpers.NsMsBuild + "HintPath", hintPath));

            return new XElement(XmlHelpers.NsMsBuild + "ItemGroup", imports);
        }
    }
}