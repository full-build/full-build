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
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Serialization;
using FullBuild.Helpers;

namespace FullBuild.Config
{
    internal static class ConfigManager
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool WritePrivateProfileString(
            string lpAppName, string lpKeyName, string lpString, string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetPrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpDefault,
            StringBuilder lpReturnedString,
            uint nSize,
            string lpFileName);

        public static FullBuildConfig LoadConfig(DirectoryInfo adminDir)
        {
            var bootstrapConfig = LoadBootstrapConfig();
            var fbDir = adminDir.GetDirectory(".full-build");
            var adminConfig = LoadAdminConfig(fbDir);
            var config = new FullBuildConfig(bootstrapConfig, adminConfig);
            return config;
        }

        public static BoostrapConfig LoadBootstrapConfig()
        {
            var userProfileDir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            var configFile = userProfileDir.GetFile(".fbconfig");
            if (! configFile.Exists)
            {
                return null;
            }

            var packageGlobalCache = new StringBuilder(255);
            GetPrivateProfileString("FullBuild", "PackageGlobalCache", "", packageGlobalCache, 255, configFile.FullName);

            var adminVcs = new StringBuilder(255);
            GetPrivateProfileString("FullBuild", "AdminVcs", "", adminVcs, 255, configFile.FullName);

            var adminRepo = new StringBuilder(255);
            GetPrivateProfileString("FullBuild", "AdminRepo", "", adminRepo, 255, configFile.FullName);

            var boostrapConfig = new BoostrapConfig
                                 {
                                     PackageGlobalCache = packageGlobalCache.ToString(),
                                     AdminRepo = new RepoConfig
                                                 {
                                                     Name = "admin",
                                                     Vcs = (VersionControlType) Enum.Parse(typeof(VersionControlType), adminVcs.ToString()),
                                                     Url = adminRepo.ToString()
                                                 }
                                 };

            return boostrapConfig;
        }

        public static void SetBootstrapConfig(string key, string value)
        {
            var userProfileDir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            var configFile = userProfileDir.GetFile(".fbconfig");

            WritePrivateProfileString("FullBuild", key, value, configFile.FullName);
        }

        public static void SaveAdminConfig(AdminConfig config, DirectoryInfo adminDir)
        {
            var file = new FileInfo(Path.Combine(adminDir.FullName, "full-build.config"));
            var xmlSer = new XmlSerializer(typeof(AdminConfig));
            using(var writer = new StreamWriter(file.FullName))
            {
                xmlSer.Serialize(writer, config);
            }
        }

        public static AdminConfig LoadAdminConfig(DirectoryInfo adminDir)
        {
            var file = new FileInfo(Path.Combine(adminDir.FullName, "full-build.config"));
            if (file.Exists)
            {
                var xmlSer = new XmlSerializer(typeof(AdminConfig));
                using(var reader = new StreamReader(file.FullName))
                {
                    var bootstrapConfig = (AdminConfig) xmlSer.Deserialize(reader);
                    bootstrapConfig.SourceRepos = bootstrapConfig.SourceRepos ?? new RepoConfig[0];

                    return bootstrapConfig;
                }
            }

            return new AdminConfig {SourceRepos = new RepoConfig[0]};
        }
    }
}