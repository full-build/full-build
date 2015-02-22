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
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using FullBuild.Helpers;
using FullBuild.Model;
using FullBuild.NuGet;

namespace FullBuild.Commands.Projects
{
    internal class Projects
    {
        public static void CheckProjects()
        {
            var admDir = WellKnownFolders.GetAdminDirectory();
            var anthology = Anthology.Load(admDir);
            var prjDir = WellKnownFolders.GetProjectDirectory();

            // validate first that repos are valid and clone them
            foreach (var project in anthology.Projects)
            {
                var what = string.Format("Checking project {0}", project.ProjectFile);

                foreach (var binRef in project.BinaryReferences)
                {
                    var bin = anthology.Binaries.SingleOrDefault(x => x.AssemblyName.InvariantEquals(binRef));
                    if (null == bin)
                    {
                        if (null != what)
                        {
                            Console.WriteLine(what);
                            what = null;
                        }

                        Console.WriteLine("  Missing binary {0}", binRef);
                    }
                }

                foreach (var pkgRef in project.PackageReferences)
                {
                    var pkg = anthology.Packages.SingleOrDefault(x => x.Name.InvariantEquals(pkgRef));
                    if (null == pkg)
                    {
                        if (null != what)
                        {
                            Console.WriteLine(what);
                            what = null;
                        }

                        Console.WriteLine("  Missing package {0}", pkgRef);
                    }
                }

                foreach (var prjRef in project.ProjectReferences)
                {
                    var prj = anthology.Projects.SingleOrDefault(x => x.Guid == prjRef);
                    if (null == prj)
                    {
                        if (null != what)
                        {
                            Console.WriteLine(what);
                            what = null;
                        }

                        var prjFileName = prjRef + ".targets";
                        var prjFile = prjDir.GetFile(prjFileName);
                        if (prjFile.Exists)
                        {
                            var xdoc = XDocument.Load(prjFile.FullName);
                            var xref = xdoc.Descendants(XmlHelpers.NsMsBuild + "Reference").Single();
                            var prjName = xref.Attribute("Include").Value;
                            Console.WriteLine("  Missing project {0} ({1})", prjRef, prjName);
                        }
                        else
                        {
                            Console.WriteLine("  Missing project {0}", prjRef);
                        }
                    }
                }
            }
        }

        public static void OptimizeProjects()
        {
            var admDir = WellKnownFolders.GetAdminDirectory();
            var anthology = Anthology.Load(admDir);
            var wsDir = WellKnownFolders.GetWorkspaceDirectory();
            var binDir = wsDir.GetDirectory("bin");
            var pkgsDir = WellKnownFolders.GetPackageDirectory();

            // validate first that repos are valid and clone them
            foreach (var project in anthology.Projects)
            {
                var projectAssemblyFile = binDir.GetFile(project.AssemblyName + project.Extension);

                if (! projectAssemblyFile.Exists)
                {
                    continue;
                }

                Console.WriteLine("Processing project {0}", project.ProjectFile);

                var projectAssembly = Assembly.ReflectionOnlyLoadFrom(projectAssemblyFile.FullName);
                var referencedAssemblies = projectAssembly.GetReferencedAssemblies();

                var newProject = project;

                // remove unused packages
                var allImportedAssemblies = from pkgRef in project.PackageReferences
                                            let pkgdir = pkgsDir.GetDirectory(pkgRef)
                                            where pkgdir.Exists
                                            let assemblies = NuPkg.Assemblies(pkgdir)
                                            select new {Package = pkgRef, Assemblies = assemblies};
                var unusedPackages = allImportedAssemblies.Where(x => !referencedAssemblies.Any(y => x.Assemblies.Contains(y.Name, StringComparer.InvariantCultureIgnoreCase)));
                newProject = unusedPackages.Aggregate(newProject, (p, pr) => p.RemovePackageReference(pr.Package));

                // remove unused projects
                var unusedProjects = from refProject in project.ProjectReferences
                                     from otherProject in anthology.Projects
                                     where refProject == otherProject.Guid
                                     where ! referencedAssemblies.Any(x => x.Name.InvariantEquals(otherProject.AssemblyName))
                                     select new {otherProject.Guid, otherProject.AssemblyName};
                newProject = unusedProjects.Aggregate(newProject, (p, a) => p.RemoveProjectReference(a.Guid));

                anthology = anthology.AddOrUpdateProject(newProject);
            }

            anthology.Save(admDir);
        }
    }
}
