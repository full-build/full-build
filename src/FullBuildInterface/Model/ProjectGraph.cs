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
using System.Reflection;
using System.Xml.Linq;
using NLog;

namespace FullBuildInterface.Model
{
    internal class ProjectGraph
    {
        private readonly Dictionary<string, Binary> _binaries = new Dictionary<string, Binary>();

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, Package> _packages = new Dictionary<string, Package>();

        private readonly Dictionary<Guid, Project> _projects = new Dictionary<Guid, Project>();

        private void AddProject(Project project)
        {
            _projects.Add(project.Guid, project);
        }

        public Anthology ToAnthology()
        {
            CheckConsistency();
            return new Anthology(_projects.Values, _binaries.Values, _packages.Values);
        }

        private void AddBinary(Binary binary)
        {
            var key = binary.AssemblyName.ToLowerInvariant();
            if (! _binaries.ContainsKey(key))
            {
                _binaries.Add(key, binary);
            }
        }

        private IEnumerable<string> GetDependencies(string id, string version)
        {
            _logger.Debug("Getting dependencies for package {0} {1}", id, version);

            var ctx = new NugetService.V2FeedContext(new Uri("https://www.nuget.org/api/v2"));
            var nugetPkg = (from p in ctx.Packages
                            where p.Id == id && p.Version == version
                            select p).SingleOrDefault();

            var dependenciesInfo = nugetPkg.Dependencies;
            if (string.IsNullOrEmpty(dependenciesInfo))
            {
                return Enumerable.Empty<string>();
            }

            var dependencies = dependenciesInfo.Split('|');
            return dependencies;
        }

        private void AddPackage(Package package)
        {
            var key = package.Name.ToLowerInvariant();
            if (_packages.ContainsKey(key))
            {
                var prevPackage = _packages[key];
                var prevVersion = SemVersion.Parse(prevPackage.Version);
                var version = SemVersion.Parse(package.Version);
                if (Comparer<SemVersion>.Default.Compare(prevVersion, version) <= 0)
                {
                    return;
                }
            }

            _packages[key] = package;

            var dependencies = GetDependencies(package.Name, package.Version);
            foreach(var dependency in dependencies)
            {
                var dependencyComponents = dependency.Split(':');
                var depPackage = new Package(dependencyComponents[0], dependencyComponents[1]);
                AddPackage(depPackage);
            }
        }

        private void CheckConsistency()
        {
            foreach(var project in _projects.Values)
            {
                _logger.Debug("Consistency for project {0}", project.GetName());

                foreach(var projectDependency in project.ProjectReferences)
                {
                    Project depProject;
                    if (! _projects.TryGetValue(projectDependency, out depProject))
                    {
                        var msg = string.Format("Project {0} references unknown project {1}", project.GetName(), projectDependency);
                        throw new ArgumentException(msg);
                    }

                    _logger.Debug(" --> {0}", depProject.GetName());
                }

                foreach(var binaryDependency in project.BinaryReferences)
                {
                    _logger.Debug(" ++ {0}", binaryDependency);
                }
            }
        }

        public IEnumerable<Package> GetPackages(FileInfo fileInfo)
        {
            var dirInfo = fileInfo.Directory;
            var package = dirInfo.GetFile("packages.config");
            if (! package.Exists)
            {
                return Enumerable.Empty<Package>();
            }

            var docPackage = XDocument.Load(package.FullName);
            var packages = from element in docPackage.Descendants("package")
                           let name = (string) element.Attribute("id")
                           let version = (string) element.Attribute("version")
                           select new Package(name, version);
            return packages;
        }

        public void Parse(DirectoryInfo workspace, FileInfo fileInfo)
        {
            var xdoc = XDocument.Load(fileInfo.FullName);

            var projectFileName = fileInfo.FullName.Substring(workspace.FullName.Length + 1);

            var projectGuid = Guid.ParseExact((string) xdoc.Descendants(XmlHelpers.NsMsBuild + "ProjectGuid").Single(), "B");
            var assemblyName = (string) xdoc.Descendants(XmlHelpers.NsMsBuild + "AssemblyName").Single();
            var fxTarget = (string) xdoc.Descendants(XmlHelpers.NsMsBuild + "TargetFrameworkVersion").Single();

            var extension = ((string) xdoc.Descendants(XmlHelpers.NsMsBuild + "OutputType").Single()).InvariantEquals("Library") ? ".dll" : ".exe";

            var projectReferences = from prjRef in xdoc.Descendants(XmlHelpers.NsMsBuild + "ProjectReference").Descendants(XmlHelpers.NsMsBuild + "Project")
                                    select Guid.ParseExact(prjRef.Value, "B");

            // extract binary references - both nuget and direct reference to assemblies (broken project reference)
            var binaries = from binRef in xdoc.Descendants(XmlHelpers.NsMsBuild + "Reference")
                           let assName = new AssemblyName((string) binRef.Attribute("Include")).Name
                           let maybeHintPath = binRef.Descendants(XmlHelpers.NsMsBuild + "HintPath").SingleOrDefault()
                           select new Binary(assName, null != maybeHintPath ? maybeHintPath.Value.ToUnixSeparator() : null);

            // report spurious binaries reference (System* are mostly OK)
            binaries.Where(x => x.HintPath == null && ! x.AssemblyName.InvariantStartsWith("System"))
                    .ForEach(x => _logger.Warn("Spurious assembly reference {0} in project {1}", x.AssemblyName, projectFileName));

            var binaryReferences = binaries.Select(x => x.AssemblyName);

            var packages = GetPackages(fileInfo);
            var packageNames = packages.Select(x => x.Name);

            var project = new Project(projectGuid, projectFileName, assemblyName, extension, fxTarget, projectReferences, binaryReferences, packageNames);
            AddProject(project);

            binaries.ForEach(AddBinary);
            packages.ForEach(AddPackage);
        }
    }
}