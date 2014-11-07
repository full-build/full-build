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
using System.IO;
using System.Net;
using FullBuild.Helpers;
using FullBuild.Model;
using NLog;

namespace FullBuild
{
    internal static class Nuget
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static void InstallPackage(Package pkg)
        {
            var cacheDir = WellKnownFolders.GetCacheDirectory();

            // avoid downloading again the package (they are immutable)
            var pkgFile = new FileInfo(Path.Combine(cacheDir.FullName, string.Format("{0}.{1}.zip", pkg.Name, pkg.Version)));
            if (!pkgFile.Exists)
            {
                DownloadNugetPackage(pkg, pkgFile);
            }

            var pkgsDir = WellKnownFolders.GetPackageDirectory();
            var pkgDir = pkgsDir.GetDirectory(pkg.Name);
            if (pkgDir.Exists)
            {
                pkgDir.Delete(true);
            }

            pkgDir.Create();

            _logger.Debug("Unzipping package {0}:{1}", pkg.Name, pkg.Version);
            System.IO.Compression.ZipFile.ExtractToDirectory(pkgFile.FullName, pkgDir.FullName);
        }

        private static void DownloadNugetPackage(Package pkg, FileInfo pkgFile)
        {
            var packageUrl = string.Format("https://www.nuget.org/api/v2/package/{0}/{1}", pkg.Name, pkg.Version);
            _logger.Debug("Downloading package {0}:{1} from {2}", pkg.Name, pkg.Version, packageUrl);
            var webClient = new WebClient();
            webClient.DownloadFile(packageUrl, pkgFile.FullName);
        }
    }
}