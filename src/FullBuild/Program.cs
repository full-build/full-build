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
using System.Linq;
using CommandLine;
using FullBuildInterface.Actions;
using FullBuildInterface.NatLangParser;
using Parser = FullBuildInterface.NatLangParser.Parser;

namespace FullBuildInterface
{
    internal class Program
    {
        private static void InitWorkspace(string path)
        {
            var handler = new InitHandler();
            handler.Execute(path);
        }

        private static void UpdateWorkspace()
        {
            var handler = new AnthologyUpdateHandler();
            handler.Execute();
        }

        private static void UpdatePackage()
        {
            var handler = new PkgUpdateHandler();
            handler.Execute();
        }

        private static void UpdateSource()
        {
            throw new NotImplementedException();
        }

        private static void FixSource()
        {
            var handler = new FixHandler();
            handler.Execute();
        }

        private static void InitView(string viewName, string[] repos)
        {
            var handler = new GenSlnHandler();
            handler.Execute(viewName, repos);
        }

        private static int Main(string[] args)
        {
            var parser = new Parser
                         {
                             MatchBuilder.Describe("initialize workspace.").Text("init").Text("workspace").Match<string>("path")
                                         .Do((string path) => InitWorkspace(path)),
                             MatchBuilder.Describe("update workspace with projects or packages changes.").Text("update").Text("workspace")
                                         .Do(UpdateWorkspace),
                             MatchBuilder.Describe("update packages.").Text("update").Text("package")
                                         .Do(UpdatePackage),
                             MatchBuilder.Describe("update sources from source control").Text("update").Text("source")
                                         .Do(UpdateSource),
                             MatchBuilder.Describe("fix sources to ensure compatibility with full-build.").Text("fix").Text("source")
                                         .Do(FixSource),
                             MatchBuilder.Describe("create solution file with provided repositories.").Text("init").Text("view").Match<string>("viewname").Text("with").MatchAggregate<string>("repos")
                                         .Do((string viewname, string[] repos) => InitView(viewname, repos))
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