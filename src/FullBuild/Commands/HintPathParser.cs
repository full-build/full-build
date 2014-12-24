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
using FullBuild.Helpers;
using FullBuild.Model;

namespace FullBuild.Commands
{
    internal class HintPathParser
    {
        public static Package ExtractPackageNameFromPackagePlusVersion(string packageNamePlusVersion)
        {
            if (null == packageNamePlusVersion)
            {
                return null;
            }

            var numbers = new[] {".0", ".1", ".2", ".3", ".4", ".5", ".6", ".7", ".8", ".9"};

            //   v         v                          v
            // ../packages/Connectivity.Services.Model.1.1.24/lib/net45/Connectivity.Services.Model.dll
            const string packagesFolder = "/packages/";
            var indexOfPackages = packageNamePlusVersion.InvariantIndexOf(packagesFolder);
            if (-1 == indexOfPackages)
            {
                return null;
            }

            var indexOfLib = packageNamePlusVersion.InvariantIndexOf("/lib/", indexOfPackages);
            if (-1 == indexOfLib)
            {
                return null;
            }

            var indexOfPackageName = indexOfPackages + packagesFolder.Length;
            var indexOfFirstNumber = packageNamePlusVersion.InvariantFirstIndexOf(numbers, indexOfPackageName);
            if (-1 == indexOfFirstNumber)
            {
                var packageNameWithoutVersion = packageNamePlusVersion.Substring(indexOfPackageName, indexOfLib - indexOfPackageName);
                var package = new Package(packageNameWithoutVersion, null);
                return package;
            }

            var packageName = packageNamePlusVersion.Substring(indexOfPackageName, indexOfFirstNumber - indexOfPackageName);
            var packageVersion = packageNamePlusVersion.Substring(indexOfFirstNumber + 1, indexOfLib - (indexOfFirstNumber + 1));

            try
            {
                var version = packageVersion.ParseSemVersion();
                if (null == version)
                {
                    return null;
                }

                var package = new Package(packageName, packageVersion);
                return package;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
