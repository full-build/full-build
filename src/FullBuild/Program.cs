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
using FullBuild.Commands;
using FullBuild.NatLangParser;

namespace FullBuild
{
    internal class Program
    {
        private static void InitWorkspace(string path)
        {
            var handler = new Workspace();
            handler.Init(path);
        }

        private static void UpdateWorkspace()
        {
            var handler = new Workspace();
            handler.Update();
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
            throw new NotImplementedException();
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

        private static void UpdateView(string viewName)
        {
            var handler = new View();
            handler.UpdateView(viewName);
        }

        private static void Exec(string command)
        {
            var handler = new Exec();
            handler.ForEachRepo(command);
        }

        private static int Main(string[] args)
        {
            var path = Parameter<string>.Create("path");
            var viewname = Parameter<string>.Create("viewname");
            var repos = Parameter<string[]>.Create("repos");
            var command = Parameter<string>.Create("command");

            var parser = new Parser
                         {
                             // ----------------------------------------------------------
                             // porcelain commands
                             // ----------------------------------------------------------

                             // init view <viewname> with <repos> ...
                             MatchBuilder.Describe("init view file <viewname> with provided repositories (<repos>).")
                                         .Command("init")
                                         .Command("view")
                                         .Param(viewname)
                                         .Command("with")
                                         .Param(repos)
                                         .Do(ctx => InitView(ctx.Get(viewname), ctx.Get(repos))),
                                         
                             // init workspace
                             MatchBuilder.Describe("initialize workspace in folder <path>.")
                                         .Command("init")
                                         .Command("workspace")
                                         .Param(path)
                                         .Do(ctx => InitWorkspace(ctx.Get(path))),

                             // update view <viewname>
                             MatchBuilder.Describe("update solution <viewname>.")
                                         .Command("update")
                                         .Command("view")
                                         .Param(viewname)
                                         .Do(ctx => UpdateView(ctx.Get(viewname))),
                                         
                             // refresh workspace
                             MatchBuilder.Describe("refresh workspace from remote.")
                                         .Command("refresh")
                                         .Command("workspace")
                                         .Do(ctx => RefreshWorkspace()),

                             // refresh source
                             MatchBuilder.Describe("refresh sources from source control.")
                                         .Command("refresh")
                                         .Command("sources")
                                         .Do(ctx => RefreshSources()),

                            // exec
                            MatchBuilder.Describe("exec command on each repo")
                                        .Command("exec")
                                        .Param(command)
                                        .Do(ctx => Exec(ctx.Get(command))),

                             // ----------------------------------------------------------
                             // plumbing commands
                             // ----------------------------------------------------------

                             // update workspace
                             MatchBuilder.Describe("update workspace with local changes.")
                                         .Command("update")
                                         .Command("workspace")
                                         .Do(ctx => UpdateWorkspace()),

                              // install package
                             MatchBuilder.Describe("install packages.")
                                         .Command("install")
                                         .Command("packages")
                                         .Do(ctx => InstallPackages()),

                             // fix source
                             MatchBuilder.Describe("convert projects to ensure compatibility with full-build.")
                                         .Command("convert")
                                         .Command("projects")
                                         .Do(ctx => ConvertProjects()),
                         };

            if (! parser.Parse(args))
            {
                Console.WriteLine("Invalid arguments");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine(parser.Usage());
                return 5;
            }

            return 0;
        }

    }
}