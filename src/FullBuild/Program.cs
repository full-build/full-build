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
// B
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
using System.Collections.Generic;
using System.IO;
using FullBuild.Commands;
using FullBuild.Config;
using FullBuild.Helpers;
using FullBuild.NatLangParser;
using NLog;

namespace FullBuild
{
    internal class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private static void InitWorkspace(string path)
        {
            var handler = new Workspace();
            handler.Init(path);
        }

        private static void UpdateWorkspace()
        {
            var handler = new Workspace();
            handler.Index();
        }

        private static void InstallPackages()
        {
            var handler = new Packages();
            handler.Install();
        }

        private static void RefreshSources()
        {
            Exec("echo ** running 'git pull --rebase' on %FULLBUILD_REPO% && git pull --rebase");
        }

        private static void RefreshWorkspace()
        {
            var handler = new Exec();

            var admDir = WellKnownFolders.GetAdminDirectory();
            handler.ExecCommand("git pull --rebase", admDir);
        }

        private static void ConvertProjects()
        {
            var handler = new Projects();
            handler.Convert();
        }

        private static void InitView(string viewName, string[] repos)
        {
            var handler = new View();
            handler.InitView(viewName, repos);
        }

        private static void ListView(string viewName)
        {
            var handler = new View();
            handler.List(viewName);
        }

        private static void ListViews()
        {
            var handler = new View();
            handler.ListAll();
        }

        private static void UpdateView(string viewName)
        {
            var handler = new View();
            handler.Generate(viewName);
        }

        private static void Exec(string command)
        {
            var handler = new Exec();
            handler.ForEachRepo(command);
        }

        private static void CloneRepo(string[] repos)
        {
            var handler = new Workspace();
            handler.CloneRepo(repos);
        }

        private static void BuildView(string viewname)
        {
            var handler = new Exec();
            var build = Commands.Exec.IsRunningOnMono() ? "xbuild" : "msbuild";
            var cmd = string.Format("{0} {1}.sln", build, viewname);

            var wsDir = WellKnownFolders.GetWorkspaceDirectory();
            handler.ExecCommand(cmd, wsDir);
        }

        private static void AddRepo(string repoName, VersionControlType vcs, string url)
        {
            var handler = new Workspace();
            handler.AddRepo(repoName, vcs, url);
        }

        private static void SetConfig(ConfigParameter key, string value)
        {
            ConfigManager.SetBootstrapConfig(key, value);
        }

        private static void AvailablePackages()
        {
            var handler = new Packages();
            handler.Check();
        }

        private static void AddNuGet(string url)
        {
            var handler = new Packages();
            handler.AddNuGet(url);
        }

        private static void ListNuGets()
        {
            var handler = new Packages();
            handler.ListNuGet();
        }

        private static void ListPackages()
        {
            var handler = new Packages();
            handler.ListPackages();
        }

        private static void UsePackage(string id, string version)
        {
            var handler = new Packages();
            handler.UsePackage(id, version);
        }

        private static void ListRepos()
        {
            var handler = new Workspace();
            handler.ListRepos();
        }

        private static int Main(string[] args)
        {
            try
            {
                TryMain(args);
                return 0;
            }
            catch(Exception ex)
            {
                _logger.Debug("Uncaught error", ex);
                Console.WriteLine("ERROR: {0}", ex.Message);
            }

            return 5;
        }

        private static void Usage(IEnumerable<string> usages)
        {
            Console.WriteLine("Usage:");
            usages.ForEach(x => Console.WriteLine("\t{0}", x));
        }

        private static void SetBinRepo(string path)
        {
            var binRepoDir = new DirectoryInfo(path);
            if (! binRepoDir.Exists)
            {
                throw new ArgumentException("Provided path is unreachable");
            }

            var admDir = WellKnownFolders.GetAdminDirectory();
            var admConfig = ConfigManager.LoadAdminConfig(admDir);
            admConfig.BinRepo = path;
            ConfigManager.SaveAdminConfig(admDir, admConfig);
        }

        private static void TryMain(string[] args)
        {
            var path = Parameter<string>.Create("path");
            var viewname = Parameter<string>.Create("viewName");
            var repos = Parameter<string[]>.Create("regex");
            var command = Parameter<string>.Create("command");
            var vcs = Parameter<VersionControlType>.Create("vcs");
            var repo = Parameter<string>.Create("repoName");
            var url = Parameter<string>.Create("url");
            var key = Parameter<ConfigParameter>.Create("key");
            var value = Parameter<string>.Create("value");
            var package = Parameter<string>.Create("package");
            var version = Parameter<string>.Create("version");

            var parser = new Parser
                         {
                             MatchBuilder.Describe("Usage")
                                         .Command("/?")
                                         .Do(ctx => Usage(ctx.Usage())),

                             // ============================== WORKSPACE ============================================

                             // init workspace
                             MatchBuilder.Describe("initialize workspace in folder <path>")
                                         .Command("init")
                                         .Command("workspace")
                                         .Param(path)
                                         .Do(ctx => InitWorkspace(ctx.Get(path))),

                                         

                             // refresh workspace
                             MatchBuilder.Describe("refresh workspace from remote")
                                         .Command("refresh")
                                         .Command("workspace")
                                         .Do(ctx => RefreshWorkspace()),

                                         

                             // clone repo
                             MatchBuilder.Describe("clone repositories which names matching {0}", repos)
                                         .Command("clone")
                                         .Command("repo")
                                         .Param(repos)
                                         .Do(ctx => CloneRepo(ctx.Get(repos))),

                             // refresh source
                             MatchBuilder.Describe("refresh sources from source control")
                                         .Command("refresh")
                                         .Command("sources")
                                         .Do(ctx => RefreshSources()),
                             MatchBuilder.Describe("index workspace with local changes")
                                         .Command("index")
                                         .Command("workspace")
                                         .Do(ctx => UpdateWorkspace()),

                             // convert projects
                             MatchBuilder.Describe("convert projects to ensure compatibility with full-build")
                                         .Command("convert")
                                         .Command("projects")
                                         .Do(ctx => ConvertProjects()),

                             // add repo
                             MatchBuilder.Describe("add a new repository to the workspace")
                                         .Command("add")
                                         .Param(vcs)
                                         .Command("repo")
                                         .Param(repo)
                                         .Command("from")
                                         .Param(url)
                                         .Do(ctx => AddRepo(ctx.Get(repo), ctx.Get(vcs), ctx.Get(url))),

                             // list repos
                             MatchBuilder.Describe("list repositories")
                                         .Command("list")
                                         .Command("repos")
                                         .Do(ctx => ListRepos()),

                             // ============================== PACKAGES ============================================
                                         
                             // add nuget feed
                             MatchBuilder.Describe("add nuget feed")
                                         .Command("add")
                                         .Command("nuget")
                                         .Param(url)
                                         .Do(ctx => AddNuGet(ctx.Get(url))),

                             // list nuget feed
                             MatchBuilder.Describe("list nuget feeds")
                                         .Command("list")
                                         .Command("nugets")
                                         .Do(ctx => ListNuGets()),

                             // list packages
                             MatchBuilder.Describe("list packages")
                                         .Command("list")
                                         .Command("packages")
                                         .Do(ctx => ListPackages()),

                             // install package
                             MatchBuilder.Describe("install packages")
                                         .Command("install")
                                         .Command("packages")
                                         .Do(ctx => InstallPackages()),
                                                  
                             // check packages
                             MatchBuilder.Describe("check for new packages versions")
                                         .Command("check")
                                         .Command("packages")
                                         .Do(ctx => AvailablePackages()),

                             // check packages
                             MatchBuilder.Describe("use package with version (* for latest)")
                                         .Command("use")
                                         .Command("package")
                                         .Param(package)
                                         .Command("version")
                                         .Param(version)
                                         .Do(ctx => UsePackage(ctx.Get(package), ctx.Get(version))),

                             // ============================== VIEW ============================================

                             // init view <viewname> with <repos> ...
                             MatchBuilder.Describe("init view file {0} with provided repositories which names matching {1}", viewname, repos)
                                         .Command("init")
                                         .Command("view")
                                         .Param(viewname)
                                         .Command("with")
                                         .Param(repos)
                                         .Do(ctx => InitView(ctx.Get(viewname), ctx.Get(repos))),
                                         
                             // list views>
                             MatchBuilder.Describe("list all available views")
                                         .Command("list")
                                         .Command("views")
                                         .Do(ctx => ListViews()),
                                         
                             // list view <viewname>
                             MatchBuilder.Describe("list view content")
                                         .Command("list")
                                         .Command("view")
                                         .Param(viewname)
                                         .Do(ctx => ListView(ctx.Get(viewname))),

                             // update view <viewname>
                             MatchBuilder.Describe("generate solution {0}", viewname)
                                         .Command("generate")
                                         .Command("view")
                                         .Param(viewname)
                                         .Do(ctx => UpdateView(ctx.Get(viewname))),
                                         
                             // build view
                             MatchBuilder.Describe("build view {0}", viewname)
                                         .Command("build")
                                         .Command("view")
                                         .Param(viewname)
                                         .Do(ctx => BuildView(ctx.Get(viewname))),

                             // ============================== EXEC ============================================

                             // exec
                             MatchBuilder.Describe("exec command on each repo")
                                         .Command("exec")
                                         .Param(command)
                                         .Do(ctx => Exec(ctx.Get(command))),

                             // ============================== CONFIG ============================================

                             // set binrepo <path>
                             MatchBuilder.Describe("set binary repository")
                                         .Command("set")
                                         .Command("binrepo")
                                         .Param(path)
                                         .Do(ctx => SetBinRepo(ctx.Get(path))),

                             // update workspace
                             // config <key> <value>
                             MatchBuilder.Describe("set configuration {0} to {1}", key, value)
                                         .Command("set")
                                         .Command("config")
                                         .Param(key)
                                         .Param(value)
                                         .Do(ctx => SetConfig(ctx.Get(key), ctx.Get(value)))
                         };

            if (! parser.ParseAndInvoke(args))
            {
                throw new ArgumentException("Invalid arguments. Use /? for usage.");
            }
        }
    }
}