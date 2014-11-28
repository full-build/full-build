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
using FullBuild.Config;
using FullBuild.Helpers;
using FullBuild.Model;

namespace FullBuild.Commands
{
    internal partial class Packages
    {
        public void Check()
        {
            // read anthology.json
            var admDir = WellKnownFolders.GetAdminDirectory();
            var anthology = Anthology.Load(admDir);

            var wsDir = WellKnownFolders.GetWorkspaceDirectory();
            var config = ConfigManager.LoadConfig(wsDir);

            var nuget = NuGet.Default(config.Nugets);

            foreach(var pkg in anthology.Packages)
            {
                var slatestVersion = nuget.GetLatestVersion(pkg);
                var latestVersion = slatestVersion.Version.ParseSemVersion();
                var currentVersion = pkg.Version.ParseSemVersion();

                if (currentVersion < latestVersion)
                {
                    Console.WriteLine("{0} version {1} is available (current is {2})", pkg.Name, slatestVersion, pkg.Version);
                }
            }
        }
    }
}