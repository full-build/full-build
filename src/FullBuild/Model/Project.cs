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
using System.Collections.Immutable;
using System.Linq;
using FullBuild.Helpers;

namespace FullBuild.Model
{
    internal class Project
    {
        public Project(Guid guid, string projectFile, string assemblyName, string extension, string fxTarget, ImmutableList<Guid> projectReferences,
                       ImmutableList<string> binaryReferences, ImmutableList<string> packageReferences)
        {
            AssemblyName = assemblyName;
            Extension = extension;
            Guid = guid;
            ProjectFile = projectFile.ToUnixSeparator();
            FxTarget = fxTarget;
            BinaryReferences = binaryReferences;
            ProjectReferences = projectReferences;
            PackageReferences = packageReferences;
        }

        public string AssemblyName { get; private set; }

        public string Extension { get; private set; }

        public Guid Guid { get; private set; }

        public string ProjectFile { get; private set; }

        public string FxTarget { get; set; }

        public ImmutableList<Guid> ProjectReferences { get; private set; }

        public ImmutableList<string> BinaryReferences { get; private set; }

        public ImmutableList<string> PackageReferences { get; private set; }

        public Project AddProjectReference(Guid project)
        {
            var newProjects = ProjectReferences.Add(project).Distinct().ToImmutableList();
            return new Project(Guid, ProjectFile, AssemblyName, Extension, FxTarget, newProjects, BinaryReferences, PackageReferences);
        }

        public Project RemoveBinaryReference(string binary)
        {
            var newBinaries = BinaryReferences.Remove(binary, StringComparer.InvariantCultureIgnoreCase);
            return new Project(Guid, ProjectFile, AssemblyName, Extension, FxTarget, ProjectReferences, newBinaries, PackageReferences);
        }

        public Project RemovePackageReference(string package)
        {
            var newPackages = PackageReferences.Remove(package, StringComparer.InvariantCultureIgnoreCase);
            return new Project(Guid, ProjectFile, AssemblyName, Extension, FxTarget, ProjectReferences, BinaryReferences, newPackages);
        }

        public Project AddPackageReference(string package)
        {
            var newPackages = PackageReferences.Add(package).Distinct(StringComparer.InvariantCultureIgnoreCase).ToImmutableList();
            return new Project(Guid, ProjectFile, AssemblyName, Extension, FxTarget, ProjectReferences, BinaryReferences, newPackages);
                 
        }
    }
}
