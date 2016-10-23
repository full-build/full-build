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

open Collections

[<RequireQualifiedAccess>] 
type PackageVersion =
    | PackageVersion of string
    | Unspecified

[<RequireQualifiedAccess>]
type OutputType =
    | Exe
    | Dll

[<RequireQualifiedAccess>]
type PublisherType =
    | Copy
    | Zip
    | Docker

[<RequireQualifiedAccess>]
type BuilderType =
    | MSBuild
    | Skip

[<RequireQualifiedAccess>]
type VcsType =
    | Gerrit
    | Git
    | Hg

[<RequireQualifiedAccess>]
type TestRunnerType =
    | NUnit

[<Sealed>]
type Package  = interface System.IComparable
with
    member Name : string
    member Dependencies: Package set
    member FxAssemblies: Assembly set

and [<Sealed>] Assembly = interface System.IComparable
with
    member Name : string

and [<Sealed>] Application = interface System.IComparable
with
    member Name : string
    member Publisher : PublisherType
    member Projects: Project set
    member Delete: unit
                -> Graph

and [<Sealed>] Repository = interface System.IComparable
with
    member Name : string
    member Builder : BuilderType
    member Projects: Project set
    member Vcs : VcsType
    member Branch : string
    member Uri : string
    member IsCloned: bool
    member Delete: unit
                -> Graph

and [<Sealed>] Project = interface System.IComparable
with
    member BinFile : string
    member ProjectFile : string
    member UniqueProjectId : string
    member Output : Assembly
    member ProjectId : string
    member OutputType : OutputType
    member FxVersion : string option
    member FxProfile : string option
    member FxIdentifier : string option
    member HasTests : bool
    member Repository:  Repository
    member Applications: Application set
    member ReferencedBy: Project set
    member References: Project set
    member AssemblyReferences: Assembly set
    member PackageReferences: Package set
    static member Closure: Project set
                        -> Project set
    static member TransitiveReferences: Project set
                                     -> Project set
    static member TransitiveReferencedBy: Project set
                                       -> Project set


and [<Sealed>] Graph =
    member MinVersion: string
    member MasterRepository : Repository
    member Repositories : Repository set  
    member Assemblies : Assembly set  
    member Applications : Application set
    member Projects : Project set
    member TestRunner : TestRunnerType
    member ArtifactsDir : string
    member NuGets : string list
    member Packages: Package set

    member CreateApp: name : string
                   -> publisher : PublisherType
                   -> projects : Project set
                   -> Graph

    member CreateNuGet: url : string
                     -> Graph

    member CreateRepo: name : string
                    -> url : string
                    -> builder : BuilderType
                    -> branch : string option
                    -> Graph

    member Save: unit
              -> unit

val from : Anthology.Anthology -> Graph 

val create: uri : string
         -> artifacts : string
         -> vcs : VcsType
         -> runner : TestRunnerType
         -> Graph

val init: uri : string
       -> vcs : VcsType
       -> Graph
