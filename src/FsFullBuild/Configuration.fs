// Copyright (c) 2014-2015, Pierre Chalamet
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
module Configuration

open System
open System.IO
open FileHelpers
open Anthology

let private WORKSPACE_CONFIG_FILE = ".full-build"

type GlobalConfiguration = 
    { BinRepo : string
      Repository : Repository
      PackageGlobalCache : string
      NuGets : string list }

type WorkspaceConfiguration = 
    { Repositories : Repository list }

let IniDocFromFile(configFile : FileInfo) = 
    new Mini.IniDocument(configFile.FullName)

let DefaultGlobalIniFilename() = 
    let userProfileDir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
    let configFile = WORKSPACE_CONFIG_FILE |> GetFile userProfileDir
    configFile

let GlobalConfigurationFromFile file = 
    let ini = IniDocFromFile file
    let fbSection = ini.["FullBuild"]
    let binRepo = fbSection.["BinRepo"].Value
    let repoType = fbSection.["RepoType"].Value
    let repoUrl = fbSection.["RepoUrl"].Value
    let packageGlobalCache = fbSection.["PackageGlobalCache"].Value
    let (ToRepository repo) = (repoType, repoUrl, ".full-build")
    let ngSection = ini.["NuGet"]
    
    let nugets = ngSection |> Seq.map (fun x -> x.Value)
                           |> Seq.toList
    { BinRepo = binRepo
      Repository = repo
      PackageGlobalCache = packageGlobalCache
      NuGets = nugets }

let GlobalConfig : GlobalConfiguration = 
    let filename = DefaultGlobalIniFilename()
    GlobalConfigurationFromFile filename

