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
using System.Xml.Serialization;
using FullBuild.Helpers;
using Mini;

namespace FullBuild.Config
{
    internal static class ConfigManager
    {
        public static void SaveConfig(DirectoryInfo adminDir, FullBuildConfig config)
        {
            var file = adminDir.GetFile("full-build.config");
            var xmlSer = new XmlSerializer(typeof(FullBuildConfig));
            using (var writer = new StreamWriter(file.FullName))
            {
                xmlSer.Serialize(writer, config);
            }
        }

        public static void SaveConfig(FullBuildConfig config)
        {
            var adminDir = WellKnownFolders.GetAdminDirectory();
            SaveConfig(adminDir, config);
        }

        public static FullBuildConfig LoadConfig(DirectoryInfo adminDir)
        {
            var config = new FullBuildConfig {NuGets = new string[0], SourceRepos = new RepoConfig[0]};

            // try to load config from admin repo if any
            var file = adminDir.GetFile("full-build.config");
            if (file.Exists)
            {
                var xmlSer = new XmlSerializer(typeof(FullBuildConfig));
                using (var reader = new StreamReader(file.FullName))
                {
                    config = (FullBuildConfig)xmlSer.Deserialize(reader);
                    config.SourceRepos = config.SourceRepos ?? new RepoConfig[0];
                    config.NuGets = config.NuGets ?? new string[0];
                }
            }

            // load bootstrap config and merge it
            var userProfileDir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            var configFile = userProfileDir.GetFile(".full-build-config");
            var ini = new IniDocument(configFile.FullName);
            var fbSection = ini["FullBuild"];

            if (null == config.PackageGlobalCache && fbSection.HasSetting(ConfigParameter.PackageGlobalCache.ToString()))
            {
                var packageGlobalCacheConfig = fbSection[ConfigParameter.PackageGlobalCache.ToString()].Value;
                config.PackageGlobalCache = packageGlobalCacheConfig;
            }

            if (null == config.AdminRepo && fbSection.HasSetting(ConfigParameter.RepoType.ToString()) && fbSection.HasSetting(ConfigParameter.RepoUrl.ToString()))
            {
                var adminVcsConfig = fbSection[ConfigParameter.RepoType.ToString()].Value;
                var adminRepoConfig = fbSection[ConfigParameter.RepoUrl.ToString()].Value;

                var adminRepo = new RepoConfig
                                {
                                    Name = ".full-build",
                                    Vcs = (VersionControlType)Enum.Parse(typeof(VersionControlType), adminVcsConfig, true),
                                    Url = adminRepoConfig
                                };

                config.AdminRepo = adminRepo;
            }

            if (null == config.BinRepo && fbSection.HasSetting(ConfigParameter.BinRepo.ToString()))
            {
                var binRepo = fbSection[ConfigParameter.BinRepo.ToString()].Value;
                config.BinRepo = binRepo;
            }

            return config;
        }

        public static FullBuildConfig LoadConfig()
        {
            var adminDir = WellKnownFolders.GetAdminDirectory();
            return LoadConfig(adminDir);
        }

        public static void SetBootstrapConfig(ConfigParameter key, string value)
        {
            var userProfileDir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            var configFile = userProfileDir.GetFile(".full-build-config");
            var keyName = key.ToString();

            var ini = new IniDocument(configFile.FullName);
            ini["FullBuild"][keyName].Value = value;
            ini.Write();
        }
    }
}
