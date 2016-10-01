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

[<Sealed>]
type Package  = interface System.IComparable
with
    member Name : string

[<Sealed>] 
type Assembly = interface System.IComparable
with
    member Name : string

[<Sealed>]
type Application = interface System.IComparable
with
    member Name : string
    member Publisher : PublisherType
    member Projects: Project seq

and [<Sealed>] Repository = interface System.IComparable
with
    member Name : string
    member Builder : BuilderType
    member Projects: unit -> Project seq
    member Vcs : VcsType
    member Branch : string
    member Uri : string

and [<Sealed>] Project = interface System.IComparable
with
    member RelativeProjectFile : string
    member UniqueProjectId : string
    member Output : Assembly
    member ProjectId : string
    member OutputType : OutputType
    member FxVersion : string option
    member FxProfile : string option
    member FxIdentifier : string option
    member HasTests : bool
    member Repository: unit -> Repository
    member Applications: unit -> Application seq
    member ReferencedBy: unit -> Project seq
    member References: unit -> Project seq
    member AssemblyReferences: unit -> Assembly seq
    member PackageReferences: unit -> Package seq

type [<Sealed>] Graph =
    member Repositories : Repository seq  
    member Assemblies : Assembly seq    
    member Applications : Application seq
    member Projects : Project seq

val from : Anthology.Anthology -> Graph 
