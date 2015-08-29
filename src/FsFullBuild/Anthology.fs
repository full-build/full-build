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

type RepositoryName = private RepositoryName of string
with
    member this.Value = (fun (RepositoryName x) -> x)this
    static member Bind(name : string) = RepositoryName (name.ToLowerInvariant())

type RepositoryUrl = RepositoryUrl of string
with
    member this.Value = (fun (RepositoryUrl x) -> x)this

type Repository = 
    { Name : RepositoryName
      Vcs : VcsType
      Url : RepositoryUrl }

type ProjectRelativeFile = ProjectRelativeFile of string
with
    member this.Value = (fun (ProjectRelativeFile x) -> x)this

type FrameworkVersion = FrameworkVersion of string
with
    member this.Value = (fun (FrameworkVersion x) -> x)this

type ProjectRef = ProjectRef of Guid
with
    member this.Value = (fun (ProjectRef x) -> x)this
    static member Bind(guid : Guid) = ProjectRef guid

type Project = 
    { Repository : RepositoryName
      RelativeProjectFile : ProjectRelativeFile
      ProjectGuid : ProjectRef
      Output : AssemblyRef
      OutputType : OutputType
      FxTarget : FrameworkVersion
      AssemblyReferences : AssemblyRef set
      PackageReferences : PackageId set
      ProjectReferences : ProjectRef set }

type ApplicationName = ApplicationName of string

type Application = 
    { Name : ApplicationName
      Projects : ProjectRef set }

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
