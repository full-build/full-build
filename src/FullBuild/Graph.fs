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

module Graph

open System
open System.IO
open Collections
open System.Reflection
open StringHelpers















[<RequireQualifiedAccess>]
type OutputType =
    | Exe
    | Dll
with
     member this.toString = toString this
     static member from s = fromString<OutputType> s



type Assembly = private AssemblyId of string
with
    member this.toString = (fun (AssemblyId x) -> x)this
    static member from (name : string) = AssemblyId (name.ToLowerInvariant())
    static member from (assName : AssemblyName) = Assembly.from (assName.Name)
    static member from (file : FileInfo) =  Assembly.from (Path.GetFileNameWithoutExtension(file.Name))

[<RequireQualifiedAccess>]
type PackageVersion =
    | PackageVersion of string
    | Unspecified
with
    member this.toString = match this with
                           | PackageVersion x -> x
                           | Unspecified -> "<unspecified>"

type PackageId = private PackageId of string
with
    member this.toString = (fun (PackageId x) -> x)this
    static member from (id : string) = PackageId (id)

type Package =
    { Id : PackageId
      Version : PackageVersion }

[<RequireQualifiedAccess>]
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

[<RequireQualifiedAccess>]
type BuilderType =
    | MSBuild
    | Skip
with
     member this.toString = toString this
     static member from s = fromString<BuilderType> s

type Repository =
    { Url : RepositoryUrl
      Name : RepositoryId
      Branch : BranchId option }

type BuildableRepository =
    { Repository : Repository
      Builder : BuilderType }

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

type FxInfo = private FxInfo of string option
with
    member this.toString = (fun (FxInfo x) -> match x with
                                              | None -> null
                                              | Some v -> v) this

    static member from (info : string) = String.IsNullOrEmpty(info) ? (None, Some info) |> FxInfo


type Project =
    { RelativeProjectFile : ProjectRelativeFile
      UniqueProjectId : ProjectUniqueId
      Output : Assembly
      ProjectId : ProjectId
      OutputType : OutputType
      FxVersion : FxInfo
      FxProfile : FxInfo
      FxIdentifier : FxInfo
      HasTests : bool
      AssemblyReferences : Assembly set
      PackageReferences : Package set
      ProjectReferences : Project set }


type ApplicationId = private ApplicationId of string
with
    member this.toString = (fun (ApplicationId x) -> x)this
    static member from (name : string) = ApplicationId (name.ToLowerInvariant())






[<RequireQualifiedAccess>]
type PublisherType =
    | Copy
    | Zip
    | Docker
with
     member this.toString = toString this
     static member from s = fromString<PublisherType> s

type Application =
    { Name : ApplicationId
      Publisher : PublisherType
      Project : Project }





type TestRunnerType =
    | NUnit
with
     member this.toString = toString this
     static member from s = fromString<TestRunnerType> s






// model is as follow:
//
//      App1                   App2
//       |                      |
//       |                      v
//       |              +- Project3
//       |              |       ^          
//       v              v       |     Project4
//  Project1    Project2        |        ^
//      ^         ^             |        |
//      |         |             |        |
//      Repository1             Repository2


type Graph =
    { Applications : Application set 
      Projects : Project set
      Packages : Package set
      Assemblies : Assembly set
    }



//let findRootProjects (antho : Anthology.Anthology) : Anthology.Project set =
//    let hasParent (prj : Anthology.Project) = 
//        antho.Projects |> Seq.exists (fun x -> x.ProjectReferences |> Set.contains prj.ProjectId)
//    antho.Projects |> Set.filter (not << hasParent)
//
//let buildProject (project : Anthology.Project) (convertedProjects: Map<ProjectId, Project>) : (Project, Map<ProjectId, Project>) =
        
    




let toGraph (antho : Anthology.Anthology) : Graph =
    // create projects hierarchy first

    let hasParent (prj : Anthology.Project) = 
        antho.Projects |> Seq.exists (fun x -> x.ProjectReferences |> Set.contains prj.ProjectId)

    let mutable projects : Map<ProjectId, Project> = Map.empty
    let mutable remainingProjects = antho.Projects
    while remainingProjects <> Set.empty do
        for project in remainingProjects do
            if project |> hasParent |> not then
                








    let rootProjects = findRootProjects antho
    
    { Applications = Set.empty }