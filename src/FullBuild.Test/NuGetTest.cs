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

            var hostedPackage = new NuGet("http://www.nuget.org/api/v2/").GetHostedPackages(package).First();

            Assert.Equal("http://www.nuget.org/api/v2/package/Castle.Core/3.3.3", hostedPackage.Content.ToString());
            Assert.Equal(864855, hostedPackage.PackageSize);
            Assert.Equal("tvVrLbHhtAGubUUDbEkH9JlivdlJOTI25u3hlyHpPxuJdTVi/yVQKWCYvGpafGPm7G1ibdWj4brqhtSQkAmN+g==", hostedPackage.PackageHash);
            Assert.Equal(true, hostedPackage.IsLatestVersion);
            Assert.Equal(new DateTime(2014, 11, 6, 2, 19, 10, 157), hostedPackage.Published);
        }

        [Fact]
        public void Find_not_available_package_from_multiple_repos()
        {
            var package = new Package("Castle.Core", "5.0.0");

            Assert.Null(new NuGet("http://siriona-proget/nuget/default/", "http://www.nuget.org/api/v2/").GetHostedPackages(package).FirstOrDefault());
        }

        [Fact]
        public void Find_available_package_from_multiple_last_repo()
        {
            var package = new Package("Castle.Core", "3.3.3");

            Assert.NotNull(new NuGet("http://siriona-proget/nuget/default/", "http://www.nuget.org/api/v2/").GetHostedPackages(package).FirstOrDefault());
        }

        [Fact]
        public void Find_latest_version_of_package_accross_nugets()
        {
            var expected = new Package("Castle.Core", "3.3.3");

            var latestVersion = new NuGet("http://siriona-proget/nuget/default/", "http://www.nuget.org/api/v2/").GetLatestVersion(new Package(expected.Name, "3.2.0"));

            Assert.Equal(expected.Version, latestVersion.Version);
        }

        class TemporaryDirectory : IDisposable
        {
            public DirectoryInfo Directory { get; private set; }

            public TemporaryDirectory()
            {
                Directory = new DirectoryInfo(Path.GetRandomFileName());
                Directory.Create();
            }

            public void Dispose()
            {
                if (Directory.Exists)
                {
                    Directory.Delete(true);
                }
            }
        }

        [Fact]
        public void Get_feed_title_from_repo()
        {
            Assert.Equal("Packages", new NuGet().RetrieveFeedTitle(new Uri("https://nuget.org/api/v2/")));
        }

        [Fact]
        public void Install_package_never_downloaded()
        {
            using (var cacheDir = new TemporaryDirectory())
            using (var pkgDir = new TemporaryDirectory())
            {
                var package = new Package("Castle.Core", "3.3.3");
                var nuGet = new NuGet("http://www.nuget.org/api/v2/");
                var hostedPackage = nuGet.GetHostedPackages(package).First();

                Assert.Empty(Directory.EnumerateFiles(cacheDir.Directory.FullName, "*.*"));
                Assert.Empty(Directory.EnumerateFiles(pkgDir.Directory.FullName, "*.*"));

                nuGet.Install(hostedPackage, cacheDir.Directory, pkgDir.Directory);

                Assert.NotEmpty(Directory.EnumerateFiles(cacheDir.Directory.FullName, "*Castle.Core.3.3.3.nupkg"));
                Assert.NotEmpty(Directory.EnumerateFiles(pkgDir.Directory.FullName, "*Castle*.dll", SearchOption.AllDirectories));
            }
        }

        [ExcludeFromCodeCoverage]
        class DisconnectedWebClient : IWebClient
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

        [Fact]
        public void Install_package_already_installed()
        {
            using (var cacheDir = new TemporaryDirectory())
            using (var pkgDir = new TemporaryDirectory())
            {
                var package = new Package("Castle.Core", "3.3.3");
                var nuGet = new NuGet("http://www.nuget.org/api/v2/");
                var hostedPackage = nuGet.GetHostedPackages(package).First();

                Assert.Empty(Directory.EnumerateFiles(cacheDir.Directory.FullName, "*.*"));
                Assert.Empty(Directory.EnumerateFiles(pkgDir.Directory.FullName, "*.*"));

                nuGet.Install(hostedPackage, cacheDir.Directory, pkgDir.Directory);

                Assert.NotEmpty(Directory.EnumerateFiles(cacheDir.Directory.FullName, "*Castle.Core.3.3.3.nupkg"));
                Assert.NotEmpty(Directory.EnumerateFiles(pkgDir.Directory.FullName, "*Castle*.dll", SearchOption.AllDirectories));

                var disconnectedNuget = new NuGet(new DisconnectedWebClient(), new[] { "http://www.nuget.org/api/v2/" });

                disconnectedNuget.Install(hostedPackage, cacheDir.Directory, pkgDir.Directory);

                Assert.NotEmpty(Directory.EnumerateFiles(cacheDir.Directory.FullName, "*Castle.Core.3.3.3.nupkg"));
                Assert.NotEmpty(Directory.EnumerateFiles(pkgDir.Directory.FullName, "*Castle*.dll", SearchOption.AllDirectories));
            }
        }

        [Fact]
        public void Force_install_if_package_is_corrupt()
        {
            using (var cacheDir = new TemporaryDirectory())
            using (var pkgDir = new TemporaryDirectory())
            {
                var package = new Package("Castle.Core", "3.3.3");
                var nuGet = new NuGet("http://www.nuget.org/api/v2/");
                var hostedPackage = nuGet.GetHostedPackages(package).First();

                Assert.Empty(Directory.EnumerateFiles(cacheDir.Directory.FullName, "*.*", SearchOption.AllDirectories));
                Assert.Empty(Directory.EnumerateFiles(pkgDir.Directory.FullName, "*.*", SearchOption.AllDirectories));

                var castlePkg = new FileInfo(Path.Combine(cacheDir.Directory.FullName, "Castle.Core.3.3.3.nupkg"));
                using (File.Open(castlePkg.FullName, FileMode.Create, FileAccess.Write)) { }

                Assert.Equal(0, new FileInfo(castlePkg.FullName).Length);

                var nuget = new NuGet("http://www.nuget.org/api/v2/");

                nuget.Install(hostedPackage, cacheDir.Directory, pkgDir.Directory);

                Assert.Equal(hostedPackage.PackageSize, new FileInfo(castlePkg.FullName).Length);
            }
        }
    }
}
