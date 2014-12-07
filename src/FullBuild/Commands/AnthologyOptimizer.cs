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
using FullBuild.Helpers;
using FullBuild.Model;
using NLog;

namespace FullBuild.Commands
{
    internal class AnthologyOptimizer
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static Anthology Optimize(Anthology anthology)
        {
            Console.WriteLine("Optimizing anthology");
            anthology = RemoveBinariesFromReferencedPackages(anthology);
            anthology = UsePackageInsteadOfBinaries(anthology);
            anthology = PromoteBinaryToProject(anthology);
            anthology = PromotePackageToProject(anthology);
            anthology = RemoveUnusedStuff(anthology);
            return anthology;
        }

        private static Anthology RemoveUnusedStuff(Anthology anthology)
        {
            // remove empty packages (no assembly)
            anthology = RemoveEmptyPackages(anthology);

            // remove unused package
            var usedPackages = anthology.Projects.SelectMany(x => x.PackageReferences).Distinct();

            var packagesToRemove = from package in anthology.Packages
                                   where !usedPackages.Contains(package.Name, StringComparer.InvariantCultureIgnoreCase)
                                   select package;

            anthology = packagesToRemove.Aggregate(anthology, (a, p) => a.RemovePackage(p));

            // remove unused binary
            var usedBinaries = anthology.Projects.SelectMany(x => x.BinaryReferences).Distinct();

            var binariesToRemove = from binary in anthology.Binaries
                                   where !usedBinaries.Contains(binary.AssemblyName, StringComparer.InvariantCultureIgnoreCase)
                                   select binary;

            anthology = binariesToRemove.Aggregate(anthology, (a, b) => a.RemoveBinary(b));
            return anthology;
        }

        private static Anthology RemoveEmptyPackages(Anthology anthology)
        {
            var pkgsDir = WellKnownFolders.GetPackageDirectory();

            var emptyPackages = from package in anthology.Packages
                                let pkgdir = pkgsDir.GetDirectory(package.Name)
                                where pkgdir.Exists
                                let assemblies = NuPkg.Assemblies(pkgdir)
                                where !assemblies.Any()
                                select package;

            foreach (var project in anthology.Projects)
            {
                var newProject = emptyPackages.Aggregate(project, (p, pa) => p.RemovePackageReference(pa.Name));
                anthology = anthology.AddOrUpdateProject(newProject);
            }

            anthology = emptyPackages.Aggregate(anthology, (a, p) => a.RemovePackage(p));
            return anthology;
        }

        private static Anthology PromoteBinaryToProject(Anthology anthology)
        {
            var bin2prj = from binary in anthology.Binaries
                          from project in anthology.Projects
                          where binary.AssemblyName.InvariantEquals(project.AssemblyName)
                          let newBin2Prj = new {Bin = binary, Prj = project}
                          group newBin2Prj by newBin2Prj.Bin
                          into g
                          select g;

            foreach (var b2p in bin2prj)
            {
                var bin = b2p.Key;
                if (1 < b2p.Count())
                {
                    Console.Error.WriteLine("WARNING | Too many candidate projects to promote binary {0}:", bin.AssemblyName);
                    b2p.ForEach(x => Console.Error.WriteLine("      | {0} {1:B}", Path.GetFileName(x.Prj.ProjectFile), x.Prj.Guid));
                }
                else
                {
                    var prj = b2p.Single().Prj;
                    _logger.Debug("Converting binary {0} to project {1}", bin.AssemblyName, prj.ProjectFile);
                    foreach (var project in anthology.Projects)
                    {
                        if (project.BinaryReferences.Contains(bin.AssemblyName, StringComparer.InvariantCultureIgnoreCase))
                        {
                            var newProject = project.RemoveBinaryReference(bin.AssemblyName);
                            newProject = newProject.AddProjectReference(prj.Guid);
                            anthology = anthology.AddOrUpdateProject(newProject);
                        }
                    }
                }
            }

            return anthology;
        }

        private static Anthology PromotePackageToProject(Anthology anthology)
        {
            var pkgsDir = WellKnownFolders.GetPackageDirectory();

            var pkg2prj = from package in anthology.Packages
                          let pkgdir = pkgsDir.GetDirectory(package.Name)
                          where pkgdir.Exists
                          let assemblies = NuPkg.Assemblies(pkgdir)
                          from project in anthology.Projects
                          where assemblies.Contains(project.AssemblyName, StringComparer.InvariantCultureIgnoreCase)
                          let newPkg2prj = new {Pkg = package, Prj = project}
                          group newPkg2prj by newPkg2prj.Pkg
                          into g
                          select g;

            foreach (var p2p in pkg2prj)
            {
                var pkg = p2p.Key;
                if (1 < p2p.Count())
                {
                    Console.Error.WriteLine("WARNING | Too many candidate projects to promote package {0}:", pkg.Name);
                    p2p.ForEach(x => Console.Error.WriteLine("      | {0} {1:B}", Path.GetFileName(x.Prj.ProjectFile), x.Prj.Guid));
                }
                else
                {
                    var prj = p2p.Single().Prj;
                    _logger.Debug("Converting package {0} to project {1}", pkg.Name, prj.ProjectFile);
                    foreach (var project in anthology.Projects)
                    {
                        if (project.PackageReferences.Contains(pkg.Name, StringComparer.InvariantCultureIgnoreCase))
                        {
                            var newProject = project.RemovePackageReference(pkg.Name);
                            newProject = newProject.AddProjectReference(prj.Guid);
                            anthology = anthology.AddOrUpdateProject(newProject);
                        }
                    }
                }
            }
            return anthology;
        }

        private static Anthology RemoveBinariesFromReferencedPackages(Anthology anthology)
        {
            var pkgsDir = WellKnownFolders.GetPackageDirectory();
            foreach (var project in anthology.Projects)
            {
                // gather all assemblies from packages in this project
                var allImportedAssemblies = (from pkgRef in project.PackageReferences
                                             let pkgdir = pkgsDir.GetDirectory(pkgRef)
                                             where pkgdir.Exists
                                             select NuPkg.Assemblies(pkgdir));
                var importedAssemblies = allImportedAssemblies.SelectMany(x => x).Distinct(StringComparer.InvariantCultureIgnoreCase);

                // remove imported assemblies
                var newProject = importedAssemblies.Aggregate(project, (p, a) => p.RemoveBinaryReference(a));
                anthology = anthology.AddOrUpdateProject(newProject);
            }

            return anthology;
        }

        private static Anthology UsePackageInsteadOfBinaries(Anthology anthology)
        {
            var pkgsDir = WellKnownFolders.GetPackageDirectory();
            var pkg2assemblies = ((from package in anthology.Packages
                                   let pkgdir = pkgsDir.GetDirectory(package.Name)
                                   where pkgdir.Exists
                                   select new {Name = package.Name, Assemblies = NuPkg.Assemblies(pkgdir)}).ToDictionary(x => x.Name, x => x.Assemblies.ToList()));

            foreach (var project in anthology.Projects)
            {
                foreach (var binary in project.BinaryReferences)
                {
                    var packages = (from pkg2assembly in pkg2assemblies
                                    where pkg2assembly.Value.Contains(binary, StringComparer.InvariantCultureIgnoreCase)
                                    select pkg2assembly).OrderBy(x => x.Value.Count());
                    if (packages.Any())
                    {
                        var newProject = project.RemoveBinaryReference(binary);
                        newProject = newProject.AddPackageReference(packages.First().Key);
                        anthology = anthology.AddOrUpdateProject(newProject);
                    }
                }
            }

            return anthology;
        }
    }
}
