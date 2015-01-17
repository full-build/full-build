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
using System.IO.Compression;
using System.Net;
using FullBuild.Helpers;
using FullBuild.Model;
using NLog;

namespace FullBuild.NuGet
{
    internal class GlobalCache
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private static FileInfo GetCachedPackageName(Package pkg, DirectoryInfo cacheDirectory)
        {
            var cacheFileName = string.Format("{0}.{1}.nupkg", pkg.Name, pkg.Version);
            var cacheFile = cacheDirectory.GetFile(cacheFileName);
            return cacheFile;
        }

        public static bool IsPackageInCache(Package pkg, DirectoryInfo cacheDirectory)
        {
            var cacheFile = GetCachedPackageName(pkg, cacheDirectory);
            return cacheFile.Exists;
        }

        public static void DownloadPackageToCache(Package pkg, NuSpec nuSpec, DirectoryInfo cacheDirectory)
        {
            if (!IsPackageInCache(pkg, cacheDirectory))
            {
                _logger.Debug("Downloading package {0} (package is missing or corrupt)", nuSpec.PackageId.Name);

                var cacheFile = GetCachedPackageName(pkg, cacheDirectory);
                using (var webClient = new WebClient())
                {
                    webClient.DownloadFile(nuSpec.Content, cacheFile.FullName);
                }
            }
        }

        public static void InstallPackageFromCache(Package pkg, DirectoryInfo cacheDirectory, DirectoryInfo packageRoot)
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
    }
}
