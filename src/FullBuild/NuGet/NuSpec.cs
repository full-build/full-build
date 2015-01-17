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
using System.Linq;
using System.Xml.Linq;
using FullBuild.Helpers;

namespace FullBuild.NuGet
{
    internal class NuSpec
    {
        private NuSpec(PackageId packageId, IEnumerable<PackageId> dependencies, Uri content, long packageSize, string packageHash, bool isLatestVersion, DateTime published)
        {
            PackageId = packageId;
            Dependencies = dependencies.ToArray();
            Published = published;
            IsLatestVersion = isLatestVersion;
            Content = content;
            PackageSize = packageSize;
            PackageHash = packageHash;
        }

        public PackageId PackageId { get; private set; }

        public IEnumerable<PackageId> Dependencies { get; private set; }

        public Uri Content { get; private set; }

        public long PackageSize { get; private set; }

        public string PackageHash { get; private set; }

        public bool IsLatestVersion { get; private set; }

        public DateTime Published { get; private set; }

        private static IEnumerable<PackageId> CreateDependencies(string dependencies)
        {
            // Zlib.Portable:1.10.0:portable-net40+sl50+wp80+win80|::net40
            // cassandra-sharp-interfaces:3.3.1:|cassandra-sharp-core:3.3.2:
            // autofixture:3.22.0:|RhinoMocks:3.6:
            var items = dependencies.Split(new[] {":|"}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in items)
            {
                var nameVersion = item.Split(':');
                var packageId = new PackageId(nameVersion[0], nameVersion[1]);
                yield return packageId;
            }
        }

        public static NuSpec CreateFromEntry(XElement entry)
        {
            var title = entry.Descendants(XmlHelpers.Atom + "title").Single().Value;
            var content = entry.Descendants(XmlHelpers.Atom + "content").Single().Attribute("src").Value;
            var packageSize = long.Parse(entry.Descendants(XmlHelpers.DataServices + "PackageSize").Single().Value);
            var packageHash = entry.Descendants(XmlHelpers.DataServices + "PackageHash").Single().Value;
            var isLatestVersion = bool.Parse(entry.Descendants(XmlHelpers.DataServices + "IsAbsoluteLatestVersion").Single().Value);
            var published = DateTime.Parse(entry.Descendants(XmlHelpers.DataServices + "Published").Single().Value);
            var version = entry.Descendants(XmlHelpers.DataServices + "Version").Single().Value;
            var dependencies = entry.Descendants(XmlHelpers.DataServices + "Dependencies").Single().Value;

            var packageId = new PackageId(title, version);
            var packageDependencies = CreateDependencies(dependencies);

            return new NuSpec(packageId, packageDependencies, new Uri(content), packageSize, packageHash, isLatestVersion, published);
        }
    }
}
