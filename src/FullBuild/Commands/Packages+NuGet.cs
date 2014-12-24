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
using FullBuild.NuGet;

namespace FullBuild.Commands
{
    internal partial class Packages
    {
        private static void AddNuGet(string url)
        {
            var config = ConfigManager.LoadConfig();
            int nuGetVersion = NuGetFactory.GetNuGetVersion(url);
            var nuGetConfig = new NuGetConfig {Url = url, Version = nuGetVersion};

            config.NuGets = config.NuGets.Append(nuGetConfig).Distinct().ToArray();
            ConfigManager.SaveConfig(config);
            
            Console.WriteLine("Added NuGet feed {0}", url);
        }

        private static void ListNuGets()
        {
            var config = ConfigManager.LoadConfig();
            config.NuGets.ForEach(Console.WriteLine);
        }

        private static void ListPackages()
        {
            var admDir = WellKnownFolders.GetAdminDirectory();
            var anthology = Anthology.Load(admDir);

            anthology.Packages.ForEach(x => Console.WriteLine("{0} {1}", x.Name, x.Version));
        }

        private static void UsePackage(string name, string version)
        {
            var admDir = WellKnownFolders.GetAdminDirectory();
            var anthology = Anthology.Load(admDir);
            var config = ConfigManager.LoadConfig();

            if (version == "*")
            {
                version = NuGetFactory.CreateAll(config.NuGets).GetLatestVersion(name).Version;
            }

            var pkg = new Package(name, version);

            Console.WriteLine("Using package {0} version {1}", name, version);

            anthology = anthology.AddOrUpdatePackages(pkg);

            anthology.Save(admDir);

            // force package installation
            InstallPackage(pkg);
        }
    }
}
