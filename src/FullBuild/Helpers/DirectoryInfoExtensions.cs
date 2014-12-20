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

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FullBuild.Helpers
{
    internal static class DirectoryInfoExtensions
    {
        public static DirectoryInfo GetDirectory(this DirectoryInfo @this, string path)
        {
            var newPath = Path.Combine(@this.FullName, path);
            return new DirectoryInfo(newPath);
        }

        public static FileInfo GetFile(this DirectoryInfo @this, string fileFormat, params object[] args)
        {
            var fileName = string.Format(fileFormat, args);
            var filePath = Path.Combine(@this.FullName, fileName);
            var file = new FileInfo(filePath);
            return file;
        }

        public static IEnumerable<FileInfo> EnumerateSupportedProjectFiles(this DirectoryInfo dir)
        {
            var csproj = dir.GetFiles("*.csproj", SearchOption.AllDirectories);
            var vbproj = dir.GetFiles("*.vbproj", SearchOption.AllDirectories);
            var fsproj = dir.GetFiles("*.fsproj", SearchOption.AllDirectories);
            var sqlproj = dir.GetFiles("*.sqlproj", SearchOption.AllDirectories);
            return csproj.Concat(vbproj).Concat(fsproj).Concat(sqlproj);
        }

        public static IEnumerable<FileInfo> EnumerateSolutionFiles(this DirectoryInfo dir)
        {
            var sln = dir.GetFiles("*.sln", SearchOption.AllDirectories);
            return sln;
        }

        public static IEnumerable<FileInfo> EnumeratePaketDependencies(this DirectoryInfo dir)
        {
            var paket = dir.GetFiles("paket.lock", SearchOption.AllDirectories);
            return paket;
        }
    }
}
