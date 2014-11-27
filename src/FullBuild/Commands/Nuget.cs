using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using FullBuild.Helpers;
using FullBuild.Model;

namespace FullBuild.Commands
{
    class NuGet
    {
        private readonly IEnumerable<string> nugets;
        private readonly IWebClient webClient;

        public NuGet(params string[] nugets)
            : this(new WebClientAdapter(), nugets)
        {

        }

        internal NuGet(IWebClient webClient, IEnumerable<string> nugets)
        {
            this.webClient = webClient;
            this.nugets = nugets;
        }

        public HostedPackage GetLatestVersion(Package package)
        {
            var nuGetResults = Query(string.Format("Packages()?$filter=tolower(Id) eq '{0}'", package.Name)).Where(nr => nr.IsLatestVersion).ToList();
            var lastVersion = nuGetResults.Max(nr => nr.Published);

            var latestPackage = nuGetResults.Single(nr => nr.Published == lastVersion);

            return new HostedPackage(package, latestPackage);
        }

        public IEnumerable<HostedPackage> GetHostedPackages(Package package)
        {
            return Query(string.Format("Packages(Id='{0}',Version='{1}')", package.Name, package.Version)).Select(nuGetResult => new HostedPackage(package, nuGetResult));
        }

        private IEnumerable<NuGetResult> Query(string query)
        {
            foreach (var nugetQuery in nugets.Select(nuget => new Uri(new Uri(nuget), query)))
            {
                string result;

                if (webClient.TryDownloadString(nugetQuery, out result))
                {
                    foreach (var entry in XDocument.Parse(result).Descendants(XmlHelpers.Atom + "entry"))
                    {
                        yield return new NuGetResult(entry);
                    }
                }
            }
        }

        public void Install(HostedPackage hostedPackage, DirectoryInfo cacheDirectory, DirectoryInfo packageRoot)
        {
            var cacheFileName = new FileInfo(Path.Combine(cacheDirectory.FullName, string.Format("{0}.{1}.nupkg", hostedPackage.Name, hostedPackage.Version)));

            UpdatePackage(hostedPackage, cacheFileName);

            var packageDirectory = SetupPackageDirectory(hostedPackage, packageRoot);

            ZipFile.ExtractToDirectory(cacheFileName.FullName, packageDirectory.FullName);
        }

        private void UpdatePackage(HostedPackage hostedPackage, FileInfo cacheFileName)
        {
            if (IsMissed(hostedPackage, cacheFileName))
            {
                cacheFileName.Delete();
                webClient.DownloadFile(hostedPackage.Content, cacheFileName.FullName);
            }
        }

        private static DirectoryInfo SetupPackageDirectory(HostedPackage hostedPackage, DirectoryInfo packageRoot)
        {
            var packageDirectory = packageRoot.GetDirectory(hostedPackage.Name);

            if (packageDirectory.Exists)
            {
                packageDirectory.Delete(true);
            }
            return packageDirectory;
        }

        private static bool IsMissed(HostedPackage hostedPackage, FileInfo cacheFileName)
        {
            return !cacheFileName.Exists || cacheFileName.Length != hostedPackage.PackageSize;
        }

        public string RetrieveFeedTitle(Uri nuget)
        {
            var content = webClient.DownloadString(nuget);
            var xdoc = XDocument.Parse(content);
            var title = xdoc.Descendants(XmlHelpers.NuGet + "collection")
                .Single(x => x.Attribute("href").Value == "Packages")
                .Descendants(XmlHelpers.Atom + "title").Single().Value;
            return title;
        }
    }
}