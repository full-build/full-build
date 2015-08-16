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
module Anthology

open System
open Newtonsoft.Json
open FSharp.Collections

[<JsonConverter(typeof<Newtonsoft.Json.Converters.StringEnumConverter>)>]
type OutputType = 
    | Exe = 0
    | Dll = 1

type Application = 
    { Name : string
      Projects : Guid list }

type Assembly = 
    { AssemblyName : string }

type AssemblyRef = 
    { Target : string }
with
    static member Bind(name : string) =  { Target = name.ToLowerInvariant() }
    static member Bind(ass : Assembly) =  AssemblyRef.Bind(ass.AssemblyName)
    member this.Print () = this.Target


type Bookmark = 
    { Name : string
      Version : string }

type Package = 
    { Id : string
      Version : string
      TargetFramework : string }

type PackageRef = 
    { Target : string }
with
    static member Bind(id : string) : PackageRef = { Target = id.ToLowerInvariant() }
    static member Bind(pkg : Package) : PackageRef = PackageRef.Bind pkg.Id
    member this.Print () = this.Target



[<JsonConverter(typeof<Newtonsoft.Json.Converters.StringEnumConverter>)>]
type VcsType = 
    | Git = 0
    | Hg = 1

type Repository = 
    { Vcs : VcsType
      Name : string
      Url : string }

type RepositoryRef = 
    { Target : string }
with
    static member Bind(name : string) : RepositoryRef = { Target = name.ToLowerInvariant() }
    static member Bind(repo : Repository) : RepositoryRef = RepositoryRef.Bind repo.Name
    member this.Print () = this.Target







type Project = 
    { Repository : RepositoryRef
      RelativeProjectFile : string
      ProjectGuid : Guid
      AssemblyName : string
      OutputType : OutputType
      FxTarget : string
      AssemblyReferences : AssemblyRef list
      PackageReferences : PackageRef list
      ProjectReferences : ProjectRef list }
and ProjectRef = 
    { Target : Guid }
with
    static member Bind(guid : Guid) : ProjectRef = { Target = guid }
    static member Bind(prj : Project) : ProjectRef = ProjectRef.Bind(prj.ProjectGuid)
    member this.Print () = this.Target.ToString("D")

type Anthology = 
    { Applications : Application list
      Repositories : Repository list
      Bookmarks : Bookmark list 
      Packages : Package list
      Projects : Project list }

    


let (|ToRepository|) (vcsType : string, vcsUrl : string, vcsName : string) = 
    let vcs = match vcsType with
              | "git" -> VcsType.Git
              | "hg" -> VcsType.Hg
              | _ -> failwithf "Unknown vcs type %A" vcsType
    { Vcs = vcs
      Name = vcsName
      Url = vcsUrl }
