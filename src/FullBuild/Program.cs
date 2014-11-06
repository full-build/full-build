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
using FullBuild.Actions;
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

        private static void UpdatePackage()
        {
            var handler = new Package();
            handler.Update();
        }

        private static void UpdateSource()
        {
            throw new NotImplementedException();
        }

        private static void FixSource()
        {
            var handler = new Source();
            handler.Fix();
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

        private static int Main(string[] args)
        {
            var path = Parameter<string>.Create("path");
            var viewname = Parameter<string>.Create("viewname");
            var repos = Parameter<string[]>.Create("repos");

            var parser = new Parser
                         {
                             // init workspace
                             MatchBuilder.Describe("initialize workspace in folder <path>.")
                                         .Command("init")
                                         .Command("workspace")
                                         .Param(path)
                                         .Do(ctx => InitWorkspace(ctx.Get(path))),

                             // update workspace
                             MatchBuilder.Describe("update workspace with projects or packages changes.")
                                         .Command("update")
                                         .Command("workspace")
                                         .Do(ctx => UpdateWorkspace()),

                             // update package
                             MatchBuilder.Describe("update packages.")
                                         .Command("update")
                                         .Command("package")
                                         .Do(ctx => UpdatePackage()),

                             // update source
                             MatchBuilder.Describe("update sources from source control.")
                                         .Command("update")
                                         .Command("source")
                                         .Do(ctx => UpdateSource()),

                             // update view <viewname>
                             MatchBuilder.Describe("update view <viewname>.")
                                         .Command("update")
                                         .Command("view")
                                         .Param(viewname)
                                         .Do(ctx => UpdateView(ctx.Get(viewname))),

                             // fix source
                             MatchBuilder.Describe("fix sources to ensure compatibility with full-build.")
                                         .Command("fix")
                                         .Command("source")
                                         .Do(ctx => FixSource()),

                             // init view <viewname> with <repos> ...
                             MatchBuilder.Describe("create solution file <viewname> with provided repositories (<repos>).")
                                         .Command("init")
                                         .Command("view")
                                         .Param(viewname)
                                         .Command("with")
                                         .Params<string>("repos")
                                         .Do(ctx => InitView(ctx.Get(viewname), ctx.Get(repos)))
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