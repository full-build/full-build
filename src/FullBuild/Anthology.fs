//   Copyright 2014-2017 Pierre Chalamet
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

[<RequireQualifiedAccess>]
type OutputType =
    | Exe
    | Dll
    | Database
with
     member this.toString = toString this
     static member from s = fromString<OutputType> s



type ViewId = ViewId of string
with
    member this.toString = (fun (ViewId x) -> x)this
    static member from s = ViewId s


type AssemblyId = private AssemblyId of string
with
    member this.toString = (fun (AssemblyId x) -> x)this
    static member from (name : string) = AssemblyId (name.ToLowerInvariant())
    static member from (assName : AssemblyName) = AssemblyId.from (assName.Name)
    static member from (file : FileInfo) =  AssemblyId.from (Path.GetFileNameWithoutExtension(file.Name))

[<RequireQualifiedAccess>]
type VcsType =
    | Gerrit
    | Git
    | Hg
    | Svn
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
    static member from (maybeUri : string) = RepositoryUrl maybeUri

[<RequireQualifiedAccess>]
type BuilderType =
    | MSBuild
    | Skip
with
     member this.toString = toString this
     static member from s = fromString<BuilderType> s

type TestRunnerType =
    | NUnit
    | Skip
with
     member this.toString = toString this
     static member from s = fromString<TestRunnerType> s

type Repository =
    { Url : RepositoryUrl
      Name : RepositoryId
      Branch : BranchId option
      Vcs : VcsType }

type BuildableRepository =
    { Repository : Repository
      Builder : BuilderType
      Tester : TestRunnerType }

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
    member this.toString = (fun (ProjectUniqueId x) -> x |> StringHelpers.toVSGuid) this
    static member from (guid : Guid) = ProjectUniqueId guid

type ProjectId = private ProjectId of string
with
    member this.toString = (fun (ProjectId x) -> x)this
    static member from (name : string) = ProjectId (name.ToLowerInvariant())


type ProjectType = private ProjectType of Guid
with
    member this.toString = (fun (ProjectType x) -> x |> StringHelpers.toVSGuid) this
    static member from (guid : Guid) = ProjectType guid

type Project =
    { Repository : RepositoryId
      RelativeProjectFile : ProjectRelativeFile
      UniqueProjectId : ProjectUniqueId
      Output : AssemblyId
      ProjectId : ProjectId
      OutputType : OutputType
      HasTests : bool
      ProjectReferences : ProjectId set }

type ApplicationId = private ApplicationId of string
with
    member this.toString = (fun (ApplicationId x) -> x)this
    static member from (name : string) = ApplicationId (name.ToLowerInvariant())

[<RequireQualifiedAccess>]
type PublisherType =
    | Copy
    | Zip
    | Docker
    | NuGet
with
     member this.toString = toString this
     static member from s = fromString<PublisherType> s

type Application =
    { Name : ApplicationId
      Publisher : PublisherType
      Project : ProjectId }

type Globals =
    { MinVersion : string
      Binaries : string
      SideBySide : bool
      NuGets : RepositoryUrl list
      MasterRepository : Repository
      Repositories : BuildableRepository set }


type Anthology =
    { Applications : Application set
      Projects : Project set }


type Baseline =
    { Incremental : bool
      Bookmarks : Bookmark set }

type View =
    { Name : string
      Filters : string set
      UpReferences : bool
      DownReferences : bool
      Modified : bool
      AppFilter : string option
      Tests : bool }
