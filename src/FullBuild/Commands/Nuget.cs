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
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using FullBuild.Helpers;
using FullBuild.Model;
using NLog;

namespace FullBuild.Commands
{
    internal class NuGet
    {
        private readonly IEnumerable<string> _nugets;
        private readonly IWebClient _webClient;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        internal NuGet(IWebClient webClient, IEnumerable<string> nugets)
        {
            _webClient = webClient;
            _nugets = nugets;
        }

        public static NuGet Default(params string[] nugets)
        {
            return new NuGet(new WebClientAdapter(), nugets);
        }

        public NuSpec GetLatestVersion(string name)
        {
            _logger.Debug("Getting latest version for package {0}", name);
            var query = string.Format("Packages()?$filter=tolower(Id) eq '{0}'", name);
            var nuGetResults = Query(query).Where(nr => nr.IsLatestVersion).ToList();
            var lastVersion = nuGetResults.Max(nr => nr.Published);
            var latestNuspec = nuGetResults.Single(nr => nr.Published == lastVersion);
            _logger.Debug("Latest version of package {0} is {1}", name, latestNuspec.Version);
            return latestNuspec;
        }

        public IEnumerable<NuSpec> GetNuSpecs(Package package)
        {
            string query = string.Format("Packages(Id='{0}',Version='{1}')", package.Name, package.Version);
            return Query(query);
        }

        private IEnumerable<NuSpec> Query(string query)
        {
            foreach(var nugetQuery in _nugets.Select(nuget => new Uri(new Uri(nuget), query)))
            {
                _logger.Debug("Trying to download nuspec from {0}", nugetQuery);

                string result;
                if (_webClient.TryDownloadString(nugetQuery, out result))
                {
                    _logger.Debug("Download successful", result);
                    foreach(var entry in XDocument.Parse(result).Descendants(XmlHelpers.Atom + "entry"))
                    {
                        yield return NuSpec.CreateFromNugetApiV1(entry);
                    }
                }
            }
        }

        public void Install(Package pkg, NuSpec nuSpec, DirectoryInfo cacheDirectory, DirectoryInfo packageRoot)
        {
            var cacheFileName = new FileInfo(Path.Combine(cacheDirectory.FullName, string.Format("{0}.{1}.nupkg", pkg.Name, nuSpec.Version)));
            var packageDirectory = SetupPackageDirectory(pkg, packageRoot);

            UpdatePackage(pkg, nuSpec, cacheFileName);
            try
            {
                ZipFile.ExtractToDirectory(cacheFileName.FullName, packageDirectory.FullName);
            }
            catch(Exception ex)
            {
                _logger.Debug("Failed to unzip. Considering file as corrupt", ex);

                cacheFileName.Delete();
                cacheFileName.Refresh();
                UpdatePackage(pkg, nuSpec, cacheFileName);
                ZipFile.ExtractToDirectory(cacheFileName.FullName, packageDirectory.FullName);
            }
          }

        private void UpdatePackage(Package pkg, NuSpec nuSpec, FileInfo cacheFileName)
        {
            if (IsMissingOrInvalid(nuSpec, cacheFileName))
            {
                _logger.Debug("Downloading package {0} (package is missing or corrupt)", pkg.Name);

                _webClient.DownloadFile(nuSpec.Content, cacheFileName.FullName);
            }
        }

        private static DirectoryInfo SetupPackageDirectory(Package pkg, DirectoryInfo packageRoot)
        {
            var packageDirectory = packageRoot.GetDirectory(pkg.Name);
            if (packageDirectory.Exists)
            {
                packageDirectory.Delete(true);
            }

            return packageDirectory;
        }

        private static bool IsMissingOrInvalid(NuSpec nuSpec, FileInfo cacheFileName)
        {
            _logger.Debug("Sanity check for NuPkg {0} {1}", nuSpec.Title, nuSpec.Version);

            if (! cacheFileName.Exists)
            {
                _logger.Debug("NuPkg {0} is missing", cacheFileName);
                return true;
            }

            _logger.Debug("NuPkg {0} is sane", cacheFileName);
            return false;
        }

        public string RetrieveFeedTitle(Uri nuget)
        {
            var content = _webClient.DownloadString(nuget);
            var xdoc = XDocument.Parse(content);
            var title = xdoc.Descendants(XmlHelpers.NuGet + "collection")
                            .Single(x => x.Attribute("href").Value == "Packages")
                            .Descendants(XmlHelpers.Atom + "title").Single().Value;
            return title;
        }
    }
}