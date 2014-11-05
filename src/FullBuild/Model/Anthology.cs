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
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;

namespace FullBuild.Model
{
    internal class Anthology
    {
        public const string AnthologyFileName = "anthology.json";

        [JsonProperty("binaries")]
        private readonly IImmutableList<Binary> _binaries;

        [JsonProperty("packages")]
        private readonly IImmutableList<Package> _packages;

        [JsonProperty("projects")]
        private readonly IImmutableList<Project> _projects;

        public Anthology()
            : this(ImmutableList.Create<Project>(), ImmutableList.Create<Binary>(), ImmutableList.Create<Package>())
        {
        }

        private Anthology(IImmutableList<Project> projects, IImmutableList<Binary> binaries, IImmutableList<Package> packages)
        {
            _projects = projects;
            _binaries = binaries;
            _packages = packages;
        }

        [JsonIgnore]
        public IEnumerable<Binary> Binaries
        {
            get { return _binaries; }
        }

        [JsonIgnore]
        public IEnumerable<Package> Packages
        {
            get { return _packages; }
        }

        [JsonIgnore]
        public IEnumerable<Project> Projects
        {
            get { return _projects; }
        }

        private static IImmutableList<T> AddOrUpdate<T>(T obj, IImmutableList<T> list, Func<T, bool> equals) where T : class
        {
            list = Remove(list, equals).Add(obj);
            return list;
        }

        public Anthology AddOrUpdateProject(Project project)
        {
            return new Anthology(AddOrUpdate(project, _projects, x => x.AssemblyName.InvariantEquals(project.AssemblyName)),
                                 _binaries,
                                 _packages);
        }

        public Anthology AddOrUpdateBinary(Binary binary)
        {
            return new Anthology(_projects,
                                 AddOrUpdate(binary, _binaries, x => x.AssemblyName.InvariantEquals(binary.AssemblyName)),
                                 _packages);
        }

        public Anthology AddOrUpdatePackages(Package package)
        {
            var existing = Packages.FirstOrDefault(x => x.Name.InvariantEquals(package.Name));
            var newPackages = _packages;
            if (null != existing)
            {
                var version = SemVersion.Parse(package.Version);
                var higherVersion = Comparer<SemVersion>.Default.Compare(existing.Version, version) < 0;
                if (higherVersion)
                {
                    newPackages = newPackages.Replace(existing, package);
                }
                else
                {
                    return this;
                }
            }
            else
            {
                newPackages = newPackages.Add(package);
            }
            
            return new Anthology(_projects,
                                 _binaries,
                                 newPackages);
        }

        private static IImmutableList<T> Remove<T>(IImmutableList<T> list, Func<T, bool> equals) where T : class
        {
            var existing = list.FirstOrDefault(@equals);
            if (null != existing)
            {
                list = list.Remove(existing);
            }

            return list;
        }

        public Anthology RemoveProject(Project project)
        {
            return new Anthology(Remove(_projects, x => x.AssemblyName.InvariantEquals(project.AssemblyName)),
                                 _binaries,
                                 _packages);
        }

        public Anthology RemoveBinary(Binary binary)
        {
            return new Anthology(_projects,
                                 Remove(_binaries, x => x.AssemblyName.InvariantEquals(binary.AssemblyName)),
                                 _packages);
        }

        public Anthology RemovePackage(Package package)
        {
            return new Anthology(_projects,
                                 _binaries,
                                 Remove(_packages, x => x.Name.InvariantEquals(package.Name)));
        }
    }
}