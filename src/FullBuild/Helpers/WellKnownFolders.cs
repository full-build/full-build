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

namespace FullBuild.Helpers
{
    internal class WellKnownFolders
    {
        public const string RelativeAdminDirectory = ".full-build";

        public const string RelativeProjectDirectory = ".full-build/projects";

        public const string RelativeCacheDirectory = ".full-build/cache";

        public const string RelativeViewDirectory = ".full-build/views";

        public const string RelativeBinDirectory = "bin";

        public const string RelativePackageDirectory = "packages";

        public const string AnthologyFileName = "anthology.json";

        public static readonly string MsBuildSolutionDir = "$(SolutionDir)";

        public static readonly string RelativeProjectAdminRepo = ".full-build-repo";

        public static readonly string MsBuildAdminDir = Path.Combine("$(SolutionDir)", RelativeAdminDirectory).ToUnixSeparator();

        public static readonly string MsBuildBinDir = Path.Combine("$(SolutionDir)", RelativeBinDirectory).ToUnixSeparator();

        public static readonly string MsBuildProjectDir = Path.Combine("$(SolutionDir)", RelativeProjectDirectory).ToUnixSeparator();

        public static readonly string MsBuildViewDir = Path.Combine("$(SolutionDir)", RelativeViewDirectory).ToUnixSeparator();

        public static readonly string MsBuildPackagesDir = Path.Combine("$(SolutionDir)", RelativePackageDirectory).ToUnixSeparator();

        public static DirectoryInfo GetWorkspaceDirectory()
        {
            var dirInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (null != dirInfo && dirInfo.Exists)
            {
                var fbiDir = dirInfo.GetDirectory(".full-build");
                if (fbiDir.Exists)
                {
                    return dirInfo;
                }

                dirInfo = dirInfo.Parent;
            }

            throw new ArgumentException("Can't find workspace root directory. Check you are in a workspace.");
        }

        public static DirectoryInfo GetAdminDirectory()
        {
            var workspace = GetWorkspaceDirectory();
            var target = workspace.GetDirectory(RelativeAdminDirectory);
            return target;
        }

        public static DirectoryInfo GetProjectDirectory()
        {
            var fbDir = GetWorkspaceDirectory();
            var target = fbDir.GetDirectory(RelativeProjectDirectory);
            target.Create();
            return target;
        }

        public static DirectoryInfo GetCacheDirectory()
        {
            var wsDir = GetWorkspaceDirectory();
            var target = wsDir.GetDirectory(RelativeCacheDirectory);
            target.Create();
            return target;
        }

        public static DirectoryInfo GetViewDirectory()
        {
            var wsDir = GetWorkspaceDirectory();
            var target = wsDir.GetDirectory(RelativeViewDirectory);
            target.Create();
            return target;
        }

        public static DirectoryInfo GetPackageDirectory()
        {
            var wsDir = GetWorkspaceDirectory();
            var target = wsDir.GetDirectory(RelativePackageDirectory);
            target.Create();
            return target;
        }
    }
}
