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
open System.IO
open Collections
open System.Reflection

type OutputType = 
    | Exe
    | Dll

type ApplicationName = ApplicationName of string
type ProjectGuid = ProjectGuid of Guid


type Application = 
    { Name : ApplicationName
      Projects : ProjectGuid set }

type AssemblyRef = private AssemblyRef of string
with
    member this.Value = (fun (AssemblyRef x) -> x)this
    static member Bind (name : string) = AssemblyRef (name.ToLowerInvariant())
    static member Bind (assName : AssemblyName) = AssemblyRef.Bind (assName.Name)
    static member Bind (file : FileInfo) =  AssemblyRef.Bind (Path.GetFileNameWithoutExtension(file.Name))

type BookmarkName = BookmarkName of string

type BookmarkVersion = BookmarkVersion of string

type Bookmark = 
    { Name : BookmarkName
      Version : BookmarkVersion }

type PackageVersion = PackageVersion of string
with
    member this.Value = (fun (PackageVersion x) -> x)this

type PackageFramework = PackageFramework of string
with
    member this.Value = (fun (PackageFramework x) -> x)this

type PackageId = PackageId of string
with
    member this.Value = (fun (PackageId x) -> x)this

type Package = 
    { Id : PackageId
      Version : PackageVersion }


type VcsType = 
    | Git
    | Hg

type Repository = 
    { Name : RepositoryName
      Vcs : VcsType
      Url : RepositoryUrl }
and RepositoryName = RepositoryName of string
with
    member this.Value = (fun (RepositoryName x) -> x)this
    static member Bind(name : string) = RepositoryName (name.ToLowerInvariant())
and RepositoryUrl = RepositoryUrl of string
with
    member this.Value = (fun (RepositoryUrl x) -> x)this



type Project = 
    { Repository : RepositoryName
      RelativeProjectFile : string
      ProjectGuid : Guid
      Output : AssemblyRef
      OutputType : OutputType
      FxTarget : string
      AssemblyReferences : AssemblyRef set
      PackageReferences : PackageId set
      ProjectReferences : ProjectRef set }
and ProjectRef = 
    { Target : Guid }
with
    static member Bind(guid : Guid) : ProjectRef = { Target = guid }
    static member Bind(prj : Project) : ProjectRef = ProjectRef.Bind(prj.ProjectGuid)
    member this.Print () = this.Target.ToString("D")

type Anthology = 
    { Applications : Application set
      Repositories : Repository set
      Bookmarks : Bookmark set 
      Projects : Project set }

    


let (|ToRepository|) (vcsType : string, vcsUrl : string, vcsName : string) = 
    let vcs = match vcsType with
              | "git" -> VcsType.Git
              | "hg" -> VcsType.Hg
              | _ -> failwithf "Unknown vcs type %A" vcsType
    { Vcs = vcs
      Name = RepositoryName vcsName
      Url = RepositoryUrl vcsUrl }
