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
using FullBuild.Config;
using FullBuild.Helpers;
using FullBuild.NatLangParser;
using NLog;

namespace FullBuild.Commands
{
    internal partial class Workspace
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static IEnumerable<Matcher> Commands()
        {
            var path = Parameter<string>.Create("path");
            var repos = Parameter<string[]>.Create("regex");
            var vcs = Parameter<VersionControlType>.Create("vcs");
            var repo = Parameter<string>.Create("repoName");
            var url = Parameter<string>.Create("url");

            // init workspace
            yield return MatchBuilder.Describe("initialize workspace in folder <path>")
                                     .Command("init")
                                     .Command("workspace")
                                     .Param(path)
                                     .Do(ctx => InitWorkspace(ctx.Get(path)));

            // refresh workspace
            yield return MatchBuilder.Describe("refresh workspace from remote")
                                     .Command("refresh")
                                     .Command("workspace")
                                     .Do(ctx => RefreshWorkspace());

            yield return MatchBuilder.Describe("index workspace with local changes")
                                     .Command("index")
                                     .Command("workspace")
                                     .Do(ctx => IndexWorkspace());

            // convert projects
            yield return MatchBuilder.Describe("convert projects to ensure compatibility with full-build")
                                     .Command("convert")
                                     .Command("projects")
                                     .Do(ctx => ConvertProjects());

            // clone repo
            yield return MatchBuilder.Describe("clone repositories which names matching {0}", repos)
                                     .Command("clone")
                                     .Command("repo")
                                     .Param(repos)
                                     .Do(ctx => CloneRepo(ctx.Get(repos)));

            // refresh source
            yield return MatchBuilder.Describe("refresh sources from source control")
                                     .Command("refresh")
                                     .Command("sources")
                                     .Do(ctx => RefreshSources());

            // add repo
            yield return MatchBuilder.Describe("add a new repository to the workspace")
                                     .Command("add")
                                     .Param(vcs)
                                     .Command("repo")
                                     .Param(repo)
                                     .Command("from")
                                     .Param(url)
                                     .Do(ctx => AddRepo(ctx.Get(repo), ctx.Get(vcs), ctx.Get(url)));

            // list repos
            yield return MatchBuilder.Describe("list repositories")
                                     .Command("list")
                                     .Command("repos")
                                     .Do(ctx => ListRepos());
        }

        private static void RefreshSources()
        {
            Exec.ForEachRepo("echo ** running 'git pull --rebase' on %FULLBUILD_REPO% && git pull --rebase");
        }

        private static void RefreshWorkspace()
        {
            var admDir = WellKnownFolders.GetAdminDirectory();
            Exec.ExecCommand("git pull --rebase", admDir);
        }
    }
}
