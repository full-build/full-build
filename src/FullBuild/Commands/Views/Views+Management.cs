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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FullBuild.Config;
using FullBuild.Helpers;

namespace FullBuild.Commands.Views
{
    internal partial class Views
    {
        public static void Init(string viewName, string[] repos)
        {
            var wsDir = WellKnownFolders.GetWorkspaceDirectory();
            var config = ConfigManager.LoadConfig();

            // validate first that repos are valid and clone them
            var view = new List<string>();
            foreach (var repo in repos)
            {
                var match = "^" + repo + "$";
                var regex = new Regex(match, RegexOptions.IgnoreCase);
                var repoConfigs = config.SourceRepos.Where(x => regex.IsMatch(x.Name));
                if (!repoConfigs.Any())
                {
                    throw new ArgumentException("Invalid repo " + repo);
                }

                foreach (var repoConfig in repoConfigs)
                {
                    var repoDir = wsDir.GetDirectory(repoConfig.Name);
                    if (repoDir.Exists)
                    {
                        view.Add(repoConfig.Name);
                    }
                    else
                    {
                        Console.Error.WriteLine("WARNING | Skipping repository {0} (not cloned)", repoConfig.Name);
                    }
                }
            }

            view = view.Distinct(StringComparer.InvariantCultureIgnoreCase).Where(x => ! String.IsNullOrEmpty(x)).ToList();

            var viewDir = WellKnownFolders.GetViewDirectory();
            var viewFile = viewDir.GetFile(viewName + ".view");

            var sb = view.Aggregate(new StringBuilder(), (s, x) => s.AppendLine(x));
            File.WriteAllText(viewFile.FullName, sb.ToString());

            Generate(viewName);
        }

        public static void Delete(string viewName)
        {
            var viewDir = WellKnownFolders.GetViewDirectory();
            var targetFileName = viewName + ".targets";
            var targetFile = viewDir.GetFile(targetFileName);
            targetFile.Delete();

            var viewFileName = viewName + ".view";
            var viewFile = viewDir.GetFile(viewFileName);
            viewFile.Delete();

            var wsDir = WellKnownFolders.GetWorkspaceDirectory();
            var slnFileName = viewName + ".sln";
            var slnFile = wsDir.GetFile(slnFileName);
            if (slnFile.Exists)
            {
                slnFile.Delete();
            }
        }
    }
}
