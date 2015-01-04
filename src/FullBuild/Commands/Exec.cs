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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FullBuild.Config;
using FullBuild.Helpers;
using FullBuild.NatLangParser;

namespace FullBuild.Commands
{
    internal class Exec
    {
        public static IEnumerable<Matcher> Commands()
        {
            var command = Parameter<string>.Create("command");

            // exec
            yield return MatcherBuilder.Describe("exec command on each repo")
                                       .Command("exec")
                                       .Param(command)
                                       .Do(ctx => ForEachRepo(ctx.Get(command)));
        }

        public static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        public static void ForEachRepo(string command)
        {
            var workspace = WellKnownFolders.GetWorkspaceDirectory();
            var config = ConfigManager.LoadConfig();

            foreach (var repo in config.SourceRepos)
            {
                var repoDir = workspace.GetDirectory(repo.Name);
                if (! repoDir.Exists)
                {
                    continue;
                }

                ExecCommand(command, repoDir, repo);
            }
        }

        public static void ExecCommand(string command, DirectoryInfo dir, RepoConfig repoConfig = null)
        {
            const string filename = "cmd";
            var arguments = string.Format("/c \"{0}\"", command);

            var psi = new ProcessStartInfo
                      {
                          FileName = filename,
                          Arguments = arguments,
                          UseShellExecute = false,
                          WorkingDirectory = dir.FullName,
                      };

            if (null != repoConfig)
            {
                psi.EnvironmentVariables.Add("FULLBUILD_REPO", repoConfig.Name);
                psi.EnvironmentVariables.Add("FULLBUILD_REPO_PATH", dir.FullName);
                psi.EnvironmentVariables.Add("FULLBUILD_REPO_URL", repoConfig.Url);
            }

            using (var process = Process.Start(psi))
            {
                if (null != process)
                {
                    process.WaitForExit();
                    if (0 != process.ExitCode)
                    {
                        var msg = string.Format("ERROR | Process exited with error code " + process.ExitCode);
                        if (!GlobalOptions.Force)
                        {
                            throw new ApplicationException(msg);
                        }

                        Console.WriteLine(msg);
                    }
                }
            }
        }
    }
}
