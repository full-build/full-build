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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using FullBuild.Commands;
using FullBuild.Model;
using Xunit;

namespace FullBuild.Test
{
    public class NuGetTest
    {
        [Fact]
        public void Find_available_package_from_nugets_for_one_repo()
        {
            var package = new Package("Castle.Core", "3.3.3");

            var nuspec = NuGet.Default("http://www.nuget.org/api/v2/").GetNuSpecs(package).First();

            Assert.Equal("http://www.nuget.org/api/v2/package/Castle.Core/3.3.3", nuspec.Content.ToString());
            Assert.Equal(864855, nuspec.PackageSize);
            Assert.Equal("tvVrLbHhtAGubUUDbEkH9JlivdlJOTI25u3hlyHpPxuJdTVi/yVQKWCYvGpafGPm7G1ibdWj4brqhtSQkAmN+g==", nuspec.PackageHash);
            Assert.Equal(true, nuspec.IsLatestVersion);
            Assert.Equal(new DateTime(2014, 11, 6, 2, 19, 10, 157), nuspec.Published);
        }

        [Fact]
        public void Find_not_available_package_from_multiple_repos()
        {
            var package = new Package("Castle.Core", "5.0.0");

            Assert.Null(NuGet.Default("http://test-proget/nuget/default/", "http://www.nuget.org/api/v2/").GetNuSpecs(package).FirstOrDefault());
        }

        [Fact]
        public void Find_available_package_from_multiple_last_repo()
        {
            var package = new Package("Castle.Core", "3.3.3");

            Assert.NotNull(NuGet.Default("http://test-proget/nuget/default/", "http://www.nuget.org/api/v2/").GetNuSpecs(package).FirstOrDefault());
        }

        [Fact]
        public void Find_latest_version_of_package_accross_nugets()
        {
            var expected = new Package("Castle.Core", "3.3.3");

            var latestVersion = NuGet.Default("http://test-proget/nuget/default/", "http://www.nuget.org/api/v2/").GetLatestVersion(new Package(expected.Name, "3.2.0"));

            Assert.Equal(expected.Version, latestVersion.Version);
        }

        [Fact]
        public void Get_feed_title_from_repo()
        {
            Assert.Equal("Packages", NuGet.Default().RetrieveFeedTitle(new Uri("https://nuget.org/api/v2/")));
        }

        [Fact]
        public void Install_package_never_downloaded()
        {
            using(var cacheDir = new TemporaryDirectory())
            {
                using(var pkgDir = new TemporaryDirectory())
                {
                    var package = new Package("Castle.Core", "3.3.3");
                    var nuget = NuGet.Default("http://www.nuget.org/api/v2/");
                    var nuspec = nuget.GetNuSpecs(package).First();

                    Assert.Empty(Directory.EnumerateFiles(cacheDir.Directory.FullName, "*.*"));
                    Assert.Empty(Directory.EnumerateFiles(pkgDir.Directory.FullName, "*.*"));

                    nuget.Install(package, nuspec, cacheDir.Directory, cacheDir.Directory);

                    Assert.NotEmpty(Directory.EnumerateFiles(cacheDir.Directory.FullName, "*Castle.Core.3.3.3.nupkg"));
                    Assert.NotEmpty(Directory.EnumerateFiles(pkgDir.Directory.FullName, "*Castle*.dll", SearchOption.AllDirectories));
                }
            }
        }

        [Fact]
        public void Install_package_already_installed()
        {
            using(var cacheDir = new TemporaryDirectory())
            {
                using(var pkgDir = new TemporaryDirectory())
                {
                    var package = new Package("Castle.Core", "3.3.3");
                    var nuget = NuGet.Default("http://www.nuget.org/api/v2/");
                    var nuspec = nuget.GetNuSpecs(package).First();

                    Assert.Empty(Directory.EnumerateFiles(cacheDir.Directory.FullName, "*.*"));
                    Assert.Empty(Directory.EnumerateFiles(pkgDir.Directory.FullName, "*.*"));

                    nuget.Install(package, nuspec, cacheDir.Directory, cacheDir.Directory);

                    Assert.NotEmpty(Directory.EnumerateFiles(cacheDir.Directory.FullName, "*Castle.Core.3.3.3.nupkg"));
                    Assert.NotEmpty(Directory.EnumerateFiles(pkgDir.Directory.FullName, "*Castle*.dll", SearchOption.AllDirectories));

                    var disconnectedNuget = new NuGet(new DisconnectedWebClient(), new[] {"http://www.nuget.org/api/v2/"});

                    disconnectedNuget.Install(package, nuspec, cacheDir.Directory, cacheDir.Directory);

                    Assert.NotEmpty(Directory.EnumerateFiles(cacheDir.Directory.FullName, "*Castle.Core.3.3.3.nupkg"));
                    Assert.NotEmpty(Directory.EnumerateFiles(pkgDir.Directory.FullName, "*Castle*.dll", SearchOption.AllDirectories));
                }
            }
        }

        [Fact]
        public void Force_install_if_package_is_corrupt()
        {
            using(var cacheDir = new TemporaryDirectory())
            {
                using(var pkgDir = new TemporaryDirectory())
                {
                    var package = new Package("Castle.Core", "3.3.3");
                    var nuGet = NuGet.Default("http://www.nuget.org/api/v2/");
                    var nuspec = nuGet.GetNuSpecs(package).First();

                    Assert.Empty(Directory.EnumerateFiles(cacheDir.Directory.FullName, "*.*", SearchOption.AllDirectories));
                    Assert.Empty(Directory.EnumerateFiles(pkgDir.Directory.FullName, "*.*", SearchOption.AllDirectories));

                    var castlePkg = new FileInfo(Path.Combine(cacheDir.Directory.FullName, "Castle.Core.3.3.3.nupkg"));
                    using(File.Open(castlePkg.FullName, FileMode.Create, FileAccess.Write))
                    {
                    }

                    Assert.Equal(0, new FileInfo(castlePkg.FullName).Length);

                    var nuget = NuGet.Default("http://www.nuget.org/api/v2/");

                    nuget.Install(package, nuspec, cacheDir.Directory, cacheDir.Directory);

                    Assert.Equal(nuspec.PackageSize, new FileInfo(castlePkg.FullName).Length);
                }
            }
        }

        [ExcludeFromCodeCoverage]
        private class DisconnectedWebClient : IWebClient
        {
            public bool TryDownloadString(Uri uri, out string result)
            {
                throw new Exception("failed");
            }

            public string DownloadString(Uri uri)
            {
                throw new Exception("failed");
            }

            public void DownloadFile(Uri address, string fileName)
            {
                throw new Exception("failed");
            }
        }

        private class TemporaryDirectory : IDisposable
        {
            public TemporaryDirectory()
            {
                Directory = new DirectoryInfo(Path.GetRandomFileName());
                Directory.Create();
            }

            public DirectoryInfo Directory { get; private set; }

            public void Dispose()
            {
                if (Directory.Exists)
                {
                    Directory.Delete(true);
                }
            }
        }
    }
}