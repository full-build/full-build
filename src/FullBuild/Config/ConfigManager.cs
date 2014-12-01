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

using System.IO;
using System.Xml.Serialization;
using FullBuild.Helpers;

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
            var file = adminDir.GetFile("full-build.config");
            if (file.Exists)
            {
                var xmlSer = new XmlSerializer(typeof(FullBuildConfig));
                using (var reader = new StreamReader(file.FullName))
                {
                    var bootstrapConfig = (FullBuildConfig)xmlSer.Deserialize(reader);
                    bootstrapConfig.SourceRepos = bootstrapConfig.SourceRepos ?? new RepoConfig[0];
                    bootstrapConfig.NuGets = bootstrapConfig.NuGets ?? new string[0];

                    return bootstrapConfig;
                }
            }

            return new FullBuildConfig {NuGets = new string[0], SourceRepos = new RepoConfig[0]};
        }

        public static FullBuildConfig LoadConfig()
        {
            var adminDir = WellKnownFolders.GetAdminDirectory();
            return LoadConfig(adminDir);
        }
    }
}
