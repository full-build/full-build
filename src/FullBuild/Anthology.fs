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

type ViewId = ViewId of string
with
    member this.Value = (fun (ViewId x) -> x)this


type OutputType = 
    | Exe
    | Dll

type AssemblyId = private AssemblyId of string
with
    member this.Value = (fun (AssemblyId x) -> x)this
    static member Bind (name : string) = AssemblyId (name.ToLowerInvariant())
    static member Bind (assName : AssemblyName) = AssemblyId.Bind (assName.Name)
    static member Bind (file : FileInfo) =  AssemblyId.Bind (Path.GetFileNameWithoutExtension(file.Name))

type PackageVersion = 
    | PackageVersion of string
    | Unspecified
with
    member this.Value = match this with
                        | PackageVersion x -> x
                        | Unspecified -> "<unspecified>"

type PackageFramework = PackageFramework of string
with
    member this.Value = (fun (PackageFramework x) -> x)this

type PackageId = private PackageId of string
with
    member this.Value = (fun (PackageId x) -> x)this
    static member Bind (id : string) = PackageId (id.ToLowerInvariant())

type Package = 
    { Id : PackageId
      Version : PackageVersion }

type VcsType = 
    | Git
    | Hg

type RepositoryId = private RepositoryId of string
with
    member this.Value = (fun (RepositoryId x) -> x)this
    static member Bind(name : string) = RepositoryId (name.ToLowerInvariant())

type RepositoryUrl = RepositoryUrl of string
with
    member this.Value = (fun (RepositoryUrl x) -> x)this

type Repository = 
    { Name : RepositoryId
      Vcs : VcsType
      Url : RepositoryUrl }

type BookmarkVersion = 
    | BookmarkVersion of string
    | Master
                       
type Bookmark = 
    { Repository : RepositoryId
      Version : BookmarkVersion }

type ProjectRelativeFile = ProjectRelativeFile of string
with
    member this.Value = (fun (ProjectRelativeFile x) -> x)this

type FrameworkVersion = FrameworkVersion of string
with
    member this.Value = (fun (FrameworkVersion x) -> x)this

type ProjectId = ProjectId of Guid
with
    member this.Value = (fun (ProjectId x) -> x)this
    static member Bind(guid : Guid) = ProjectId guid

type ProjectType = ProjectType of Guid
with
    member this.Value = (fun (ProjectType x) -> x)this
    static member Bind(guid : Guid) = ProjectType guid


type Project = 
    { Repository : RepositoryId
      RelativeProjectFile : ProjectRelativeFile
      ProjectGuid : ProjectId
      ProjectType : ProjectType
      Output : AssemblyId
      OutputType : OutputType
      FxTarget : FrameworkVersion
      AssemblyReferences : AssemblyId set
      PackageReferences : PackageId set
      ProjectReferences : ProjectId set }

type ApplicationId = private ApplicationId of string
with
    member this.Value = (fun (ApplicationId x) -> x)this
    static member Bind(name : string) = ApplicationId (name.ToLowerInvariant())

type Application = 
    { Name : ApplicationId
      Projects : ProjectId set }

type Anthology = 
    { Applications : Application set
      Repositories : Repository set
      Projects : Project set }

type Baseline = 
    { Bookmarks : Bookmark set  }

let (|ToRepository|) (vcsType : string, vcsName : string, vcsUrl : string) = 
    let vcs = match vcsType with
              | "git" -> VcsType.Git
              | "hg" -> VcsType.Hg
              | _ -> failwithf "Unknown vcs type %A" vcsType
    { Vcs = vcs
      Name = RepositoryId.Bind vcsName
      Url = RepositoryUrl vcsUrl }
