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
    internal partial class Packages
    {
        public static void InstallPackages()
        {
            // read anthology.json
            var admDir = WellKnownFolders.GetAdminDirectory();
            var anthology = Anthology.Load(admDir);

            Console.WriteLine("Installing packages");
            foreach (var pkg in anthology.Packages)
            {
                Console.WriteLine("  {0} {1}", pkg.Name, pkg.Version);
                InstallPackage(pkg);
            }
        }

        public static void InstallPackage(Package pkg)
        {
            var config = ConfigManager.LoadConfig();
            var nuget = NuGet.Default(config.NuGets);
            var cacheDir = WellKnownFolders.GetCacheDirectory();
            var pkgDir = WellKnownFolders.GetPackageDirectory();

            if (! nuget.IsPackageInCache(pkg, cacheDir))
            {
                var nuSpec = nuget.GetNuSpecs(pkg).First(x => x.Version == pkg.Version);
                nuget.DownloadNuSpecToCache(pkg, nuSpec, cacheDir);
            }

            nuget.InstallPackageFromCache(pkg, cacheDir, pkgDir);
            GenerateTargetsForProject(pkg);
        }

        private static XElement Generatewhen(IEnumerable<string> foldersToTry, string fxVersion, DirectoryInfo libDir)
        {
            foreach (var folderToTry in foldersToTry)
            {
                var fxLibs = libDir.GetDirectory(folderToTry);
                if (fxLibs.Exists)
                {
                    var condition = String.Format("'$(TargetFrameworkVersion)' == '{0}'", fxVersion);
                    var when = new XElement(XmlHelpers.NsMsBuild + "When",
                                            new XAttribute("Condition", condition),
                                            GenerateItemGroup(fxLibs));
                    return when;
                }
            }

            return null;
        }

        private static void GenerateTargetsForProject(Package package)
        {
            var pkgsDir = WellKnownFolders.GetPackageDirectory();
            var pkgDir = pkgsDir.GetDirectory(package.Name);
            var libDir = pkgDir.GetDirectory("lib");

            var whens = new List<XElement>();
            if (libDir.Exists)
            {
                for (var i = 0; i < FrameworkVersion.CompatibilityOrder.Length; ++i)
                {
                    var fxVersion = FrameworkVersion.CompatibilityOrder[i];

                    XElement when = null;
                    for (var j = i; j >= 0; --j)
                    {
                        var substituteVersion = FrameworkVersion.CompatibilityOrder[j];
                        var foldersToTry = FrameworkVersion.FxVersion2Folder[substituteVersion];

                        when = Generatewhen(foldersToTry, fxVersion, libDir);
                        if (null != when)
                        {
                            break;
                        }
                    }

                    when = when ?? Generatewhen(new[] {""}, fxVersion, libDir);
                    whens.Add(when);
                }
            }

            var nuspecFileName = String.Format("{0}.nuspec", package.Name);
            var nuspecFile = pkgDir.GetFile(nuspecFileName);
            var xdocNuspec = XDocument.Load(nuspecFile.FullName);
            var dependencies = from d in xdocNuspec.Descendants(XmlHelpers.NsNuget + "dependency")
                               select (string)d.Attribute("id");

            var imports = from dependency in dependencies
                          let dependencyPackageFileName = dependency + ".targets"
                          let dependencyTargets = Path.Combine(WellKnownFolders.MsBuildPackagesDir, dependencyPackageFileName)
                          let condition = String.Format("'$(FullBuild_{0}_Pkg)' == ''", dependency.ToMsBuild())
                          select new XElement(XmlHelpers.NsMsBuild + "Import",
                                              new XAttribute("Project", dependencyTargets),
                                              new XAttribute("Condition", condition));

            var choose = whens.Any()
                ? new XElement(XmlHelpers.NsMsBuild + "Choose", whens)
                : GenerateItemGroup(libDir);

            var defineName = String.Format("FullBuild_{0}_Pkg", package.Name.ToMsBuild());
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
                                              new XElement(XmlHelpers.NsMsBuild + "HintPath", hintPath),
                                              new XElement(XmlHelpers.NsMsBuild + "Private", "true"));

            return new XElement(XmlHelpers.NsMsBuild + "ItemGroup", imports);
        }
    }
}
