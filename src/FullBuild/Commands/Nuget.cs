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
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly IEnumerable<string> _nugets;

        private readonly IWebClient _webClient;

        internal NuGet(IWebClient webClient, IEnumerable<string> nugets)
        {
            _webClient = webClient;
            _nugets = nugets;
        }

        public static NuGet Default(params string[] nugets)
        {
            return new NuGet(new WebClientAdapter(), nugets);
        }

        private NuSpec GetLatestVersion(string name, bool includePreRelease)
        {
            var query = string.Format("Packages?filter=Id eq '{0}'&includePrerelease={1}", name, includePreRelease
                ? "true"
                : "false");
            var latestNuSpec = Query(query).LastOrDefault();
            return latestNuSpec;
        }

        public NuSpec GetLatestVersion(string name)
        {
            _logger.Debug("Getting latest version for package {0}", name);

            var latestNuSpec = GetLatestVersion(name, false);
            if (null == latestNuSpec)
            {
                latestNuSpec = GetLatestVersion(name, true);
                if (null == latestNuSpec)
                {
                    return null;
                }
            }

            _logger.Debug("Latest version of package {0} is {1}", name, latestNuSpec.Version);
            return latestNuSpec;
        }

        public IEnumerable<NuSpec> GetNuSpecs(Package package)
        {
            var query = string.Format("Packages(Id='{0}',Version='{1}')", package.Name, package.Version);
            return Query(query);
        }

        private IEnumerable<NuSpec> Query(string query)
        {
            foreach (var nugetQuery in _nugets.Select(nuget => new Uri(new Uri(nuget), query)))
            {
                _logger.Debug("Trying to download nuspec from {0}", nugetQuery);

                string result;
                if (_webClient.TryDownloadString(nugetQuery, out result))
                {
                    _logger.Debug("Download successful", result);
                    XDocument xdoc;
                    try
                    {
                        xdoc = XDocument.Parse(result);
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug("Failed to parse response {0}", result, ex);
                        var msg = string.Format("Invalid response for query {0}", nugetQuery);
                        throw new ApplicationException(msg);
                    }

                    foreach (var entry in xdoc.Descendants(XmlHelpers.Atom + "entry"))
                    {
                        yield return NuSpec.CreateFromNugetApiV1(entry);
                    }
                }
            }
        }

        private static FileInfo GetCachedPackageName(Package pkg, DirectoryInfo cacheDirectory)
        {
            var cacheFileName = string.Format("{0}.{1}.nupkg", pkg.Name, pkg.Version);
            var cacheFile = cacheDirectory.GetFile(cacheFileName);
            return cacheFile;
        }

        public bool IsPackageInCache(Package pkg, DirectoryInfo cacheDirectory)
        {
            var cacheFile = GetCachedPackageName(pkg, cacheDirectory);
            return cacheFile.Exists;
        }

        public void DownloadNuSpecToCache(Package pkg, NuSpec nuSpec, DirectoryInfo cacheDirectory)
        {
            if (!IsPackageInCache(pkg, cacheDirectory))
            {
                _logger.Debug("Downloading package {0} (package is missing or corrupt)", nuSpec.Title);

                var cacheFile = GetCachedPackageName(pkg, cacheDirectory);
                _webClient.DownloadFile(nuSpec.Content, cacheFile.FullName);
            }
        }

        public void InstallPackageFromCache(Package pkg, DirectoryInfo cacheDirectory, DirectoryInfo packageRoot)
        {
            var packageDirectory = SetupPackageDirectory(pkg, packageRoot);
            var cacheFileName = string.Format("{0}.{1}.nupkg", pkg.Name, pkg.Version);
            var cacheFile = cacheDirectory.GetFile(cacheFileName);
            try
            {
                ZipFile.ExtractToDirectory(cacheFile.FullName, packageDirectory.FullName);

                var libDir = packageDirectory.GetDirectory("lib");
                if (libDir.Exists)
                {
                    RenameFolderWithPlus(libDir);
                }
            }
            catch (Exception ex)
            {
                cacheFile.Delete();
                throw new ApplicationException("Failed to unzip package, please retry.", ex);
            }
        }

        private static void RenameFolderWithPlus(DirectoryInfo libDir)
        {
            var newLibDirName = libDir.Name;
            if (newLibDirName.Contains("%2B"))
            {
                newLibDirName = newLibDirName.Replace("%2B", "+");
                var newLibDir = libDir.Parent.GetDirectory(newLibDirName);
                libDir.MoveTo(newLibDir.FullName);

                libDir = newLibDir;
            }

            foreach (var subDir in libDir.GetDirectories())
            {
                RenameFolderWithPlus(subDir);
            }
        }

        private static DirectoryInfo SetupPackageDirectory(Package pkg, DirectoryInfo packageRoot)
        {
            var packageDirectory = packageRoot.GetDirectory(pkg.Name);
            Reliability.Do(() =>
                           {
                               packageDirectory.Refresh();
                               if (packageDirectory.Exists)
                               {
                                   packageDirectory.Delete(true);
                               }
                           });
            return packageDirectory;
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
