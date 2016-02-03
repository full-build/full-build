//   Copyright 2014-2016 Pierre Chalamet
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

module Anthology

open System
open System.IO
open Collections
open System.Reflection
open StringHelpers


type ViewId = ViewId of string
with
    member this.toString = (fun (ViewId x) -> x)this


type OutputType = 
    | Exe
    | Dll
with
     member this.toString = toString this
     static member from s = fromString<OutputType> s



type AssemblyId = private AssemblyId of string
with
    member this.toString = (fun (AssemblyId x) -> x)this
    static member from (name : string) = AssemblyId (name.ToLowerInvariant())
    static member from (assName : AssemblyName) = AssemblyId.from (assName.Name)
    static member from (file : FileInfo) =  AssemblyId.from (Path.GetFileNameWithoutExtension(file.Name))

type PackageVersion = 
    | PackageVersion of string
    | Unspecified
with
    member this.toString = match this with
                           | PackageVersion x -> x
                           | Unspecified -> "<unspecified>"

type PackageFramework = PackageFramework of string
with
    member this.toString = (fun (PackageFramework x) -> x)this

type PackageId = private PackageId of string
with
    member this.toString = (fun (PackageId x) -> x)this
    static member from (id : string) = PackageId (id)

type Package = 
    { Id : PackageId
      Version : PackageVersion }

type VcsType = 
    | Gerrit
    | Git
    | Hg
with
     member this.toString = toString this
     static member from s = fromString<VcsType> s


type RepositoryId = private RepositoryId of string
with
    member this.toString = (fun (RepositoryId x) -> x)this
    static member from (name : string) = RepositoryId (name.ToLowerInvariant())

type BranchId = private BranchId of string
with
    member this.toString = (fun (BranchId x) -> x)this
    static member from (name : string) = BranchId (name)


type RepositoryUrl = private RepositoryUrl of string
with
    member this.toString = (fun (RepositoryUrl x) -> x)this
    member this.toLocalOrUrl = let uri = Uri(this.toString)
                               let sourceUri = match uri.Scheme with
                                               | x when x = Uri.UriSchemeFile -> uri.LocalPath
                                               | _ -> uri.ToString()
                               sourceUri

    static member from (maybeUri : Uri) = RepositoryUrl.from (maybeUri.ToString())
    static member from (maybeUri : string) = let uri = Uri(maybeUri.ToLowerInvariant())
                                             if uri.IsWellFormedOriginalString() then RepositoryUrl (maybeUri.ToLowerInvariant())
                                             else failwithf "Invalid uri %A" uri

type BuilderType =
    | MSBuild
    | Fake
with
     member this.toString = toString this
     static member from s = fromString<BuilderType> s

type Repository = 
    { Url : RepositoryUrl 
      Name : RepositoryId
      Branch : BranchId option }

type BuildableRepository =
    { Repository : Repository
      Sticky : bool 
      Builder : BuilderType }

type BookmarkVersion = BookmarkVersion of string
with
    member this.toString = (fun (BookmarkVersion x) -> x)this
    static member from (version : string) = BookmarkVersion (version)

type Bookmark = 
    { Repository : RepositoryId
      Version : BookmarkVersion }

type ProjectRelativeFile = ProjectRelativeFile of string
with
    member this.toString = (fun (ProjectRelativeFile x) -> x)this

type FrameworkVersion = FrameworkVersion of string
with
    member this.toString = (fun (FrameworkVersion x) -> x)this

type ProjectUniqueId = private ProjectUniqueId of Guid
with
    member this.toString = (fun (ProjectUniqueId x) -> x.ToString("D")) this
    static member from (guid : Guid) = ProjectUniqueId guid

type ProjectId = private ProjectId of string
with
    member this.toString = (fun (ProjectId x) -> x)this
    static member from (name : string) = ProjectId (name.ToLowerInvariant())


type ProjectType = private ProjectType of Guid
with
    member this.toString = (fun (ProjectType x) -> x.ToString("D")) this
    static member from (guid : Guid) = ProjectType guid


type Project = 
    { Repository : RepositoryId
      RelativeProjectFile : ProjectRelativeFile
      UniqueProjectId : ProjectUniqueId
      Output : AssemblyId
      ProjectId : ProjectId
      OutputType : OutputType
      FxTarget : FrameworkVersion
      AssemblyReferences : AssemblyId set
      PackageReferences : PackageId set
      ProjectReferences : ProjectId set }

type ApplicationId = private ApplicationId of string
with
    member this.toString = (fun (ApplicationId x) -> x)this
    static member from (name : string) = ApplicationId (name.ToLowerInvariant())


type PublisherType =
    | Copy
    | Zip
    | Fake
with
     member this.toString = toString this
     static member from s = fromString<PublisherType> s

type TestRunnerType =
    | NUnit
with
     member this.toString = toString this
     static member from s = fromString<TestRunnerType> s

type Application = 
    { Name : ApplicationId
      Publisher : PublisherType
      Project : ProjectId }

type Anthology = 
    { Artifacts : string
      NuGets : RepositoryUrl list 
      Vcs : VcsType
      MasterRepository : Repository
      Repositories : BuildableRepository set
      Projects : Project set 
      Applications : Application set 
      Tester : TestRunnerType }

type Baseline = 
    { Bookmarks : Bookmark set  }

type View =
    { Filters : string set
      Builder : BuilderType
      Parameters : string set }
