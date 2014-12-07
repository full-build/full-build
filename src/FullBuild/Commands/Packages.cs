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
using FullBuild.NatLangParser;
using NLog;

namespace FullBuild.Commands
{
    internal partial class Packages
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static IEnumerable<Matcher> Commands()
        {
            var url = Parameter<string>.Create("url");
            var package = Parameter<string>.Create("package");
            var version = Parameter<string>.Create("version");

            // add nuget feed
            yield return MatchBuilder.Describe("add nuget feed")
                                     .Command("add")
                                     .Command("nuget")
                                     .Param(url)
                                     .Do(ctx => AddNuGet(ctx.Get(url)));

            // list nuget feed
            yield return MatchBuilder.Describe("list nuget feeds")
                                     .Command("list")
                                     .Command("nugets")
                                     .Do(ctx => ListNuGets());

            // list packages
            yield return MatchBuilder.Describe("list packages")
                                     .Command("list")
                                     .Command("packages")
                                     .Do(ctx => ListPackages());

            // install package
            yield return MatchBuilder.Describe("install packages")
                                     .Command("install")
                                     .Command("packages")
                                     .Do(ctx => InstallPackages());

            // check packages
            yield return MatchBuilder.Describe("check for new packages versions")
                                     .Command("check")
                                     .Command("packages")
                                     .Do(ctx => CheckPackages());

            // check packages
            yield return MatchBuilder.Describe("use package with version (* for latest)")
                                     .Command("use")
                                     .Command("package")
                                     .Param(package)
                                     .Command("version")
                                     .Param(version)
                                     .Do(ctx => UsePackage(ctx.Get(package), ctx.Get(version)));
        }
    }
}
