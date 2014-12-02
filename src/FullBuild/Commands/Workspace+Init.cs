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
using System.IO;
using FullBuild.Config;
using FullBuild.Helpers;
using FullBuild.SourceControl;

namespace FullBuild.Commands
{
    internal partial class Workspace
    {
        private static void InitWorkspace(string path)
        {
            var wsDir = new DirectoryInfo(path);
            wsDir.Create();

            var admDir = wsDir.GetDirectory(".full-build");
            if (admDir.Exists)
            {
                throw new ArgumentException("Workspace is already initialized");
            }

            // get bootstrap config
            var config = ConfigManager.LoadConfig(admDir);
            
            // get bootstrap config
            var sourceControl = ServiceActivator<Factory>.Create<ISourceControl>(config.AdminRepo.Vcs.ToString());
            sourceControl.Clone(admDir, ".full-build", config.AdminRepo.Url);

            // reload config (after clone)
            config = ConfigManager.LoadConfig(admDir);
            ConfigManager.SaveConfig(admDir, config);

            // copy all files from binary repo
            var tip = sourceControl.Tip(admDir);
            var binDir = new DirectoryInfo(config.BinRepo);
            var binVersionDir = binDir.GetDirectory(tip);
            if (binVersionDir.Exists)
            {
                Console.WriteLine("Copying build output version {0}", tip);
                var targetBinDir = wsDir.GetDirectory("bin");
                targetBinDir.Create();
                foreach (var binFile in binVersionDir.EnumerateFiles())
                {
                    var targetFile = targetBinDir.GetFile(binFile.Name);
                    binFile.CopyTo(targetFile.FullName, true);
                }
            }
        }
    }
}
