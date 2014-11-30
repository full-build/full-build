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
using FullBuild.Helpers;
using FullBuild.NatLangParser;

namespace FullBuild.Commands
{
    internal partial class Views
    {
        public static IEnumerable<Matcher> Commands()
        {
            var viewname = Parameter<string>.Create("viewName");
            var repos = Parameter<string[]>.Create("regex");

            // init view <viewname> with <repos> ...
            yield return MatchBuilder.Describe("init view file {0} with provided repositories which names matching {1}", viewname, repos)
                                     .Command("init")
                                     .Command("view")
                                     .Param(viewname)
                                     .Command("with")
                                     .Param(repos)
                                     .Do(ctx => InitView(ctx.Get(viewname), ctx.Get(repos)));

            yield return MatchBuilder.Describe("drop view {0}", viewname)
                                     .Command("drop")
                                     .Command("view")
                                     .Param(viewname)
                                     .Do(ctx => DeleteView(ctx.Get(viewname)));

            // list views>
            yield return MatchBuilder.Describe("list all available views")
                                     .Command("list")
                                     .Command("views")
                                     .Do(ctx => ListViews());

            // list view <viewname>
            yield return MatchBuilder.Describe("describe view content")
                                     .Command("describe")
                                     .Command("view")
                                     .Param(viewname)
                                     .Do(ctx => DescribeView(ctx.Get(viewname)));

            // update view <viewname>
            yield return MatchBuilder.Describe("generate solution {0}", viewname)
                                     .Command("generate")
                                     .Command("view")
                                     .Param(viewname)
                                     .Do(ctx => GenerateView(ctx.Get(viewname)));

            // build view
            yield return MatchBuilder.Describe("build view {0}", viewname)
                                     .Command("build")
                                     .Command("view")
                                     .Param(viewname)
                                     .Do(ctx => BuildView(ctx.Get(viewname)));
        }

        private static void BuildView(string viewname)
        {
            var build = Exec.IsRunningOnMono()
                ? "xbuild"
                : "msbuild";
            var cmd = string.Format("{0} {1}.sln", build, viewname);

            var wsDir = WellKnownFolders.GetWorkspaceDirectory();
            Exec.ExecCommand(cmd, wsDir);
        }
    }
}
