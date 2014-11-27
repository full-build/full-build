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
using FullBuild.Helpers;
using FullBuild.Model;

namespace FullBuild.Commands
{
    internal partial class Packages
    {
        public void AddNuGet(string url)
        {
            var admDir = WellKnownFolders.GetAdminDirectory();
            var config = ConfigManager.LoadAdminConfig(admDir);

            config.NuGets = config.NuGets.Concat(new[] {url}).Distinct().ToArray();
            ConfigManager.SaveAdminConfig(admDir, config);

            var title = new NuGet().RetrieveFeedTitle(new Uri(url));
            Console.WriteLine("Added feed {0} with title {1}", url, title);
        }

        public void ListNuGet()
        {
            var admDir = WellKnownFolders.GetAdminDirectory();
            var config = ConfigManager.LoadAdminConfig(admDir);
            config.NuGets.ForEach(Console.WriteLine);
            ConfigManager.SaveAdminConfig(admDir, config);
        }

        public void ListPackages()
        {
            var admDir = WellKnownFolders.GetAdminDirectory();
            var anthology = Anthology.Load(admDir);

            anthology.Packages.ForEach(x => Console.WriteLine("{0} {1}", x.Name, x.Version));
        }

        public void UsePackage(string id, string version)
        {
            var admDir = WellKnownFolders.GetAdminDirectory();
            var anthology = Anthology.Load(admDir);

            var wsDir = WellKnownFolders.GetWorkspaceDirectory();
            var config = ConfigManager.LoadConfig(wsDir);

            var pkg = new Package(id, version);
            if (version == "*")
            {
                version = new NuGet(config.Nugets).GetLatestVersion(pkg).Version;
            }

            Console.WriteLine("Using package {0} version {1}", id, version);

            anthology = anthology.AddOrUpdatePackages(pkg);

            anthology.Save(admDir);

            // force package installation
            InstallPackage(pkg);
        }
    }
}