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
using System.Security.Cryptography;
using System.Xml.Linq;
using FullBuild.Config;
using FullBuild.Helpers;
using FullBuild.Model;
using FullBuild.NuGet;

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


        private static IEnumerable<Package> GetDependencies(XElement xpackage)
        {
            var packages = from x in xpackage.Descendants().Where(x => x.Name.LocalName == "dependency")
                          let id = (string)x.Attribute("id")
                          let version = (string)x.Attribute("version")
                          select new Package(id, version);
            return packages;
        }

        public static void InstallPackage(Package pkg)
        {
            var config = ConfigManager.LoadConfig();
            var nuget = NuGetFactory.CreateAll(config.NuGets);
            var cacheDir = WellKnownFolders.GetCacheDirectory();
            var pkgsDir = WellKnownFolders.GetPackageDirectory();

            if (! GlobalCache.IsPackageInCache(pkg, cacheDir))
            {
                _logger.Debug("Package {0} version {1} was not found in cache {2}", pkg.Name, pkg.Version, cacheDir.FullName);
                var nuSpec = nuget.GetVersion(pkg);
                GlobalCache.DownloadPackageToCache(pkg, nuSpec, cacheDir);
            }

            GlobalCache.InstallPackageFromCache(pkg, cacheDir, pkgsDir);

            // install dependencies
            var pkgDir = pkgsDir.GetDirectory(pkg.Name);
            var nuspecFile = pkgDir.GetFile(pkg.Name + ".nuspec");
            var xnuspec = XDocument.Load(nuspecFile.FullName);
            var dependencies = GetDependencies(xnuspec.Root);
            dependencies.ForEach(InstallPackage);

            // create target file for project
            GenerateTargetsForProject(pkg);
        }

        private static XElement GenerateWhen(IEnumerable<string> fxFolderNamesToTry, string fxVersion, DirectoryInfo libDir)
        {
            foreach (var folderToTry in fxFolderNamesToTry)
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

        private static XElement GeneratePortableWhen(IEnumerable<string> fxFolderNamesToTry, string fxVersion, DirectoryInfo libDir)
        {
            const string portablePrefix = "portable-";
            foreach (var folderToTry in fxFolderNamesToTry)
            {
                var portableDirs = libDir.GetDirectories(portablePrefix + "*");
                foreach (var portableDir in portableDirs)
                {
                    var portableDirName = portableDir.Name;
                    var supportedFrameworks = portableDirName.Substring(portablePrefix.Length).Split('+');
                    if (supportedFrameworks.Any(x => x.InvariantEquals(folderToTry)))
                    {
                        var condition = String.Format("'$(TargetFrameworkVersion)' == '{0}'", fxVersion);
                        var when = new XElement(XmlHelpers.NsMsBuild + "When",
                                                new XAttribute("Condition", condition),
                                                GenerateItemGroup(portableDir));
                        return when;
                    }
                }
            }

            return null;
        }

        private static void GenerateTargetsForProject(Package package)
        {
            var pkgsDir = WellKnownFolders.GetPackageDirectory();
            var pkgDir = pkgsDir.GetDirectory(package.Name);
            var libDir = pkgDir.GetDirectory("lib");

            // Algorith is as follow to choose the right binary while compiling:
            // For each supported framework version we have to find the binaries with the most up-to-date framework version.
            // This mean that for a given framework version, we start with this version exactly and then go back in the framework version history to find sometjing compatible.
            // If nothing is found, use th default package (if it exists_. If really no default package exists, there is nothing we can do but skip the request framework version.
            var whens = new List<XElement>();
            if (libDir.Exists)
            {
                for (var i = 0; i < FrameworkVersion.CompatibilityOrder.Length; ++i)
                {
                    var fxVersion = FrameworkVersion.CompatibilityOrder[i];

                    XElement when = null;

                    // first try to find a standard framework version
                    for (var j = i; j >= 0; --j)
                    {
                        var substituteVersion = FrameworkVersion.CompatibilityOrder[j];
                        var fxFolderNamesToTry = FrameworkVersion.FxVersion2Folder[substituteVersion];

                        when = GenerateWhen(fxFolderNamesToTry, fxVersion, libDir);
                        if (null != when)
                        {
                            break;
                        }
                    }

                    // if nothing found then try to find a portable version
                    if (null == when)
                    {
                        for (var j = i; j >= 0; --j)
                        {
                            var substituteVersion = FrameworkVersion.CompatibilityOrder[j];
                            var fxFolderNamesToTry = FrameworkVersion.FxVersion2Folder[substituteVersion];

                            when = GeneratePortableWhen(fxFolderNamesToTry, fxVersion, libDir);
                            if (null != when)
                            {
                                break;
                            }
                        }
                    }

                    // nothing found then use default version
                    if (null == when)
                    {
                        when = GenerateWhen(new[] {""}, fxVersion, libDir);
                    }

                    whens.Add(when);
                }
            }

            var nuspecFileName = String.Format("{0}.nuspec", package.Name);
            var nuspecFile = pkgDir.GetFile(nuspecFileName);
            var xdocNuspec = XDocument.Load(nuspecFile.FullName);
            var dependencies = GetDependencies(xdocNuspec.Root);

            var imports = from dependency in dependencies
                          let dependencyPackageFileName = dependency.Name + ".targets"
                          let dependencyTargets = Path.Combine(WellKnownFolders.MsBuildPackagesDir, dependencyPackageFileName).ToUnixSeparator()
                          let condition = String.Format("'$(FullBuild_{0}_Pkg)' == ''", dependency.Name.ToMsBuild())
                          select new XElement(XmlHelpers.NsMsBuild + "Import",
                                              new XAttribute("Project", dependencyTargets),
                                              new XAttribute("Condition", condition));

            var choose = whens.Any()
                ? new XElement(XmlHelpers.NsMsBuild + "Choose", whens)
                : GenerateItemGroup(libDir);

            var defineName = String.Format("FullBuild_{0}_Pkg", package.Name.ToMsBuild());
            var define = new XElement(XmlHelpers.NsMsBuild + "PropertyGroup",
                                      new XElement(XmlHelpers.NsMsBuild + defineName, "Y"));

            var propCondition = string.Format("'$(FullBuild_{0}_Pkg)' == ''", package.Name.ToMsBuild());
            var projectCondition = new XAttribute("Condition", propCondition);

            var project = new XElement(XmlHelpers.NsMsBuild + "Project", projectCondition, define, imports, choose);
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
                          let hintPath = Path.Combine(WellKnownFolders.MsBuildSolutionDir, relativePath).ToUnixSeparator()
                          select new XElement(XmlHelpers.NsMsBuild + "Reference",
                                              new XAttribute("Include", assemblyName),
                                              new XElement(XmlHelpers.NsMsBuild + "HintPath", hintPath),
                                              new XElement(XmlHelpers.NsMsBuild + "Private", "true"));

            return new XElement(XmlHelpers.NsMsBuild + "ItemGroup", imports);
        }
    }
}
