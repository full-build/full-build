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
using FullBuild.Config;
using FullBuild.Model;
using FullBuild.NuGet;
using NFluent;
using NUnit.Framework;

namespace FullBuild.Test.NuGet
{
    [TestFixture]
    public class NuGetTests
    {
        [Test]
        [Category("integration")]
        public void Find_available_package_from_multiple_last_repo()
        {
            var package = new Package("Castle.Core", "3.3.3");

            var nugetConfig1 = new NuGetConfig {Url = "http://siriona-proget/nuget/default/", Version = 1};
            var nugetConfig2 = new NuGetConfig {Url = "http://www.nuget.org/api/v2/", Version = 2};
            var nugetConfig = new[] {nugetConfig1, nugetConfig2};

            Assert.NotNull(NuGetFactory.CreateAll(nugetConfig).GetVersion(package));
        }

        [Test]
        [Category("integration")]
        public void Find_available_package()
        {
            var package = new Package("Siriona.Availpro.Common", "3.3.3");

            var nugetConfig1 = new NuGetConfig { Url = "http://siriona-proget/nuget/default/", Version = 1 };
            var nugetConfig2 = new NuGetConfig { Url = "http://www.nuget.org/api/v2/", Version = 2 };
            var nugetConfig = new[] { nugetConfig1, nugetConfig2 };

            Assert.NotNull(NuGetFactory.CreateAll(nugetConfig).GetLatestVersion(package.Name));
        }


        [Test]
        [Category("integration")]
        public void Check_package_download()
        {
            var package = new Package("Connectivity.Services.Model", "1.1.26");

            var nugetConfig1 = new NuGetConfig { Url = "http://siriona-proget/nuget/default/", Version = 1 };
            var nugetConfig2 = new NuGetConfig { Url = "http://www.nuget.org/api/v2/", Version = 2 };
            var nugetConfig = new[] { nugetConfig1, nugetConfig2 };

            NuSpec anObject = NuGetFactory.CreateAll(nugetConfig).GetVersion(package);
            Assert.NotNull(anObject);
        }

        [Test]
        [Category("integration")]
        public void Find_available_package_from_nugets_for_one_repo()
        {
            var package = new Package("Castle.Core", "3.3.3");

            var nugetConfig2 = new NuGetConfig {Url = "http://www.nuget.org/api/v2/", Version = 2};
            var nuspec = NuGetFactory.Create(nugetConfig2).GetVersion(package);

            Check.That(nuspec.Content.ToString()).IsEqualTo("http://www.nuget.org/api/v2/package/Castle.Core/3.3.3");
            Check.That(nuspec.PackageSize).IsEqualTo(864855);
            Check.That(nuspec.PackageHash).IsEqualTo("tvVrLbHhtAGubUUDbEkH9JlivdlJOTI25u3hlyHpPxuJdTVi/yVQKWCYvGpafGPm7G1ibdWj4brqhtSQkAmN+g==");
            Check.That(nuspec.IsLatestVersion).IsTrue();
            Check.That(nuspec.Published).IsEqualTo(new DateTime(2014, 11, 6, 2, 19, 10, 157));
        }

        [Test]
        [Category("integration")]
        public void Find_available_package_with_dependencies()
        {
            var package = new Package("cassandra-sharp", "3.3.2");

            var nugetConfig2 = new NuGetConfig {Url = "http://www.nuget.org/api/v2/", Version = 2};
            var nuspec = NuGetFactory.Create(nugetConfig2).GetVersion(package);

            Check.That(nuspec.Content.ToString()).IsEqualTo("http://www.nuget.org/api/v2/package/cassandra-sharp/3.3.2");
            Check.That(nuspec.PackageId.Name).IsEqualTo("cassandra-sharp");
            Check.That(nuspec.PackageId.Version).IsEqualTo("3.3.2");
            Check.That(nuspec.Dependencies).HasSize(2);
            Check.That(nuspec.Dependencies.ElementAt(0)).IsEqualTo(new PackageId("cassandra-sharp-interfaces", "3.3.1"));
            Check.That(nuspec.Dependencies.ElementAt(1)).IsEqualTo(new PackageId("cassandra-sharp-core", "3.3.2"));
        }

        [Test]
        [Category("integration")]
        public void Find_available_package_with_dependencies_portable()
        {
            var package = new Package("FSharp.Data", "2.1.1");

            var nugetConfig2 = new NuGetConfig { Url = "http://www.nuget.org/api/v2/", Version = 2 };
            var nuspec = NuGetFactory.Create(nugetConfig2).GetVersion(package);

            Check.That(nuspec.Content.ToString()).IsEqualTo("http://www.nuget.org/api/v2/package/FSharp.Data/2.1.1");
            Check.That(nuspec.PackageId.Name).IsEqualTo("FSharp.Data");
            Check.That(nuspec.PackageId.Version).IsEqualTo("2.1.1");
            Check.That(nuspec.Dependencies).HasSize(1);
            Check.That(nuspec.Dependencies.ElementAt(0)).IsEqualTo(new PackageId("Zlib.Portable", "1.10.0"));
        }        

        [Test]
        [Category("integration")]
        public void Find_latest_version_of_package_accross_nugets()
        {
            var expected = new Package("Castle.Core", "3.3.3");

            var nugetConfig1 = new NuGetConfig {Url = "http://siriona-proget/nuget/default/", Version = 1};
            var nugetConfig2 = new NuGetConfig {Url = "http://www.nuget.org/api/v2/", Version = 2};
            var nugetConfig = new[] {nugetConfig1, nugetConfig2};

            var latestVersion = NuGetFactory.CreateAll(nugetConfig).GetLatestVersion(expected.Name);

            Check.That(latestVersion.PackageId.Version).IsEqualTo(expected.Version);
        }

        [Test]
        [Category("integration")]
        public void Find_not_available_package_from_multiple_repos()
        {
            var package = new Package("Castle.Core", "5.0.0");
            var nugetConfig1 = new NuGetConfig {Url = "http://siriona-proget/nuget/default/", Version = 1};
            var nugetConfig2 = new NuGetConfig {Url = "http://www.nuget.org/api/v2/", Version = 2};
            var nugetConfig = new[] {nugetConfig1, nugetConfig2};

            Assert.Null(NuGetFactory.CreateAll(nugetConfig).GetVersion(package));
        }

        [Test]
        [Category("integration")]
        public void Get_feed_version_from_repo()
        {
            var version = NuGetFactory.GetNuGetVersion("http://www.nuget.org/api/v2/");
            Check.That(version).IsEqualTo(2);
        }
    }
}
