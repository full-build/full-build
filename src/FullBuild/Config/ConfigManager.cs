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
        public static FullBuildConfig LoadConfig(DirectoryInfo wsDir)
        {
            var bootstrapConfig = LoadBootstrapConfig();
            var fbDir = wsDir.GetDirectory(".full-build");
            var adminConfig = LoadAdminConfig(fbDir);
            var config = new FullBuildConfig(bootstrapConfig, adminConfig);
            return config;
        }

        public static BoostrapConfig LoadBootstrapConfig()
        {
            var userProfileDir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            var configFile = userProfileDir.GetFile(".full-build-config");
            if (! configFile.Exists)
            {
                throw new ArgumentException("Configure full-build before proceeding.");
            }

            var ini = new IniDocument(configFile.FullName);
            var packageGlobalCacheConfig = ini["FullBuild"]["PackageGlobalCache"].Value;
            var adminVcsConfig = ini["FullBuild"]["RepoType"].Value;
            var adminRepoConfig = ini["FullBuild"]["RepoUrl"].Value;

            var adminRepo = new RepoConfig
                            {
                                Name = "admin",
                                Vcs = (VersionControlType) Enum.Parse(typeof(VersionControlType), adminVcsConfig, true),
                                Url = adminRepoConfig
                            };
            var boostrapConfig = new BoostrapConfig(packageGlobalCacheConfig, adminRepo);

            return boostrapConfig;
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

        public static void SaveAdminConfig(DirectoryInfo adminDir, AdminConfig config)
        {
            var file = adminDir.GetFile("full-build.config");
            var xmlSer = new XmlSerializer(typeof(AdminConfig));
            using(var writer = new StreamWriter(file.FullName))
            {
                xmlSer.Serialize(writer, config);
            }
        }

        public static AdminConfig LoadAdminConfig(DirectoryInfo adminDir)
        {
            var file = adminDir.GetFile("full-build.config");
            if (file.Exists)
            {
                var xmlSer = new XmlSerializer(typeof(AdminConfig));
                using(var reader = new StreamReader(file.FullName))
                {
                    var bootstrapConfig = (AdminConfig) xmlSer.Deserialize(reader);
                    bootstrapConfig.SourceRepos = bootstrapConfig.SourceRepos ?? new RepoConfig[0];
                    bootstrapConfig.NuGets = bootstrapConfig.NuGets ?? new string[0];

                    return bootstrapConfig;
                }
            }

            return new AdminConfig {NuGets = new string[0], SourceRepos = new RepoConfig[0]};
        }
    }
}