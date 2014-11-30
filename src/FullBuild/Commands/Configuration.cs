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
using FullBuild.Config;
using FullBuild.Helpers;
using FullBuild.NatLangParser;

namespace FullBuild.Commands
{
    public partial class Configuration
    {
        public static IEnumerable<Matcher> Commands()
        {
            var path = Parameter<string>.Create("path");
            var key = Parameter<ConfigParameter>.Create("key");
            var value = Parameter<string>.Create("value");

            // set binrepo <path>
            yield return MatchBuilder.Describe("set binary repository")
                                     .Command("set")
                                     .Command("binrepo")
                                     .Param(path)
                                     .Do(ctx => SetBinRepo(ctx.Get(path)));

            // update workspace
            // config <key> <value>
            yield return MatchBuilder.Describe("set configuration {0} to {1}", key, value)
                                     .Command("set")
                                     .Command("config")
                                     .Param(key)
                                     .Param(value)
                                     .Do(ctx => SetConfig(ctx.Get(key), ctx.Get(value)));
        }

        private static void SetConfig(ConfigParameter key, string value)
        {
            ConfigManager.SetBootstrapConfig(key, value);
        }

        private static void SetBinRepo(string path)
        {
            var binRepoDir = new DirectoryInfo(path);
            if (!binRepoDir.Exists)
            {
                throw new ArgumentException("Provided path is unreachable");
            }

            var admDir = WellKnownFolders.GetAdminDirectory();
            var admConfig = ConfigManager.LoadAdminConfig(admDir);
            admConfig.BinRepo = path;
            ConfigManager.SaveAdminConfig(admDir, admConfig);
        }
    }
}
