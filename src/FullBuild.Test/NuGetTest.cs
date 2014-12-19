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
using FullBuild.Helpers;
using FullBuild.Model;
using NFluent;
using NUnit.Framework;

namespace FullBuild.Test
{
    [TestFixture]
    public class NuGetTest
    {
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

        [Test]
        public void Ensure_failure_if_package_is_corrupt()
        {
            using (var cacheDir = new TemporaryDirectory())
            {
                using (var pkgDir = new TemporaryDirectory())
                {
                    var package = new Package("Castle.Core", "3.3.3");

                    Check.That(Directory.EnumerateFiles(cacheDir.Directory.FullName, "*.*", SearchOption.AllDirectories)).IsEmpty();
                    Check.That(Directory.EnumerateFiles(pkgDir.Directory.FullName, "*.*", SearchOption.AllDirectories)).IsEmpty();

                    var castlePkg = cacheDir.Directory.GetFile("Castle.Core.3.3.3.nupkg");
                    using (File.Open(castlePkg.FullName, FileMode.Create, FileAccess.Write))
                    {
                    }

                    Check.That(new FileInfo(castlePkg.FullName).Length).IsEqualTo(0);

                    var nuget = NuGet.Default("http://www.nuget.org/api/v2/");

                    Check.That(nuget.IsPackageInCache(package, cacheDir.Directory)).IsTrue();

                    Check.ThatCode(() => nuget.InstallPackageFromCache(package, cacheDir.Directory, pkgDir.Directory)).Throws<Exception>();

                    Check.That(nuget.IsPackageInCache(package, cacheDir.Directory)).IsFalse();
                }
            }
        }

        [Test]
        public void Find_available_package_from_multiple_last_repo()
        {
            var package = new Package("Castle.Core", "3.3.3");

            Assert.NotNull(NuGet.Default("http://test-proget/nuget/default/", "http://www.nuget.org/api/v2/").GetNuSpecs(package).FirstOrDefault());
        }

        [Test]
        public void Find_available_package_from_nugets_for_one_repo()
        {
            var package = new Package("Castle.Core", "3.3.3");

            var nuspec = NuGet.Default("http://www.nuget.org/api/v2/").GetNuSpecs(package).First();

            Check.That(nuspec.Content.ToString()).IsEqualTo("http://www.nuget.org/api/v2/package/Castle.Core/3.3.3");
            Check.That(nuspec.PackageSize).IsEqualTo(864855);
            Check.That(nuspec.PackageHash).IsEqualTo("tvVrLbHhtAGubUUDbEkH9JlivdlJOTI25u3hlyHpPxuJdTVi/yVQKWCYvGpafGPm7G1ibdWj4brqhtSQkAmN+g==");
            Check.That(nuspec.IsLatestVersion).IsTrue();
            Check.That(nuspec.Published).IsEqualTo(new DateTime(2014, 11, 6, 2, 19, 10, 157));
        }

        [Test]
        public void Find_latest_version_of_package_accross_nugets()
        {
            var expected = new Package("Castle.Core", "3.3.3");

            var latestVersion = NuGet.Default("http://test-proget/nuget/default/", "http://www.nuget.org/api/v2/").GetLatestVersion(expected.Name);

            Check.That(latestVersion.Version).IsEqualTo(expected.Version);
        }

        [Test]
        public void Find_not_available_package_from_multiple_repos()
        {
            var package = new Package("Castle.Core", "5.0.0");

            Assert.Null(NuGet.Default("http://test-proget/nuget/default/", "http://www.nuget.org/api/v2/").GetNuSpecs(package).FirstOrDefault());
        }

        [Test]
        public void Get_feed_title_from_repo()
        {
            Check.That(NuGet.Default().RetrieveFeedTitle(new Uri("https://nuget.org/api/v2/"))).IsEqualTo("Packages");
        }
    }
}
