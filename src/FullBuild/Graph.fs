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

//[<CustomEquality; CustomComparison>]
[<CustomEquality; CustomComparison>]
type Package =
    { Graph : Graph
      Package :Anthology.PackageId }

    override this.Equals(other : System.Object) =
        System.Object.ReferenceEquals(this, other)

    override this.GetHashCode() : int =
        this.Package.GetHashCode()

    interface System.IComparable with
        member this.CompareTo(other) =
            match other with
            | :? Package as x -> System.Collections.Generic.Comparer<Anthology.PackageId>.Default.Compare(this.Package, x.Package)
            | _ -> failwith "Can't compare values with different types"

    member this.Name = this.Package.toString

and [<CustomEquality; CustomComparison>] Assembly = 
    { Graph : Graph
      Assembly : Anthology.AssemblyId }
    override this.Equals(other : System.Object) =
        System.Object.ReferenceEquals(this, other)

    override this.GetHashCode() : int =
        this.Assembly.GetHashCode()

    interface System.IComparable with
        member this.CompareTo(other) =
            match other with
            | :? Assembly as x -> System.Collections.Generic.Comparer<Anthology.AssemblyId>.Default.Compare(this.Assembly, x.Assembly)
            | _ -> failwith "Can't compare values with different types"

    member this.Name = this.Assembly.toString

and [<CustomEquality; CustomComparison>] Application =
    { Graph : Graph
      Application : Anthology.Application } 

    override this.Equals(other : System.Object) =
        System.Object.ReferenceEquals(this, other)

    override this.GetHashCode() : int =
        this.Application.GetHashCode()

    interface System.IComparable with
        member this.CompareTo(other) =
            match other with
            | :? Application as x -> System.Collections.Generic.Comparer<Anthology.ApplicationId>.Default.Compare(this.Application.Name, x.Application.Name)
            | _ -> failwith "Can't compare values with different types"

    member this.Name = this.Application.Name.toString

    member this.Publisher = match this.Application.Publisher with
                            | Anthology.PublisherType.Copy -> PublisherType.Copy
                            | Anthology.PublisherType.Zip -> PublisherType.Zip
                            | Anthology.PublisherType.Docker -> PublisherType.Docker

    member this.Projects =
        this.Application.Projects |> Seq.map (fun x -> this.Graph.ProjectMap.[x])

and [<CustomEquality; CustomComparison>] Repository =
    { Graph : Graph
      Repository : Anthology.BuildableRepository }

    override this.Equals(other : System.Object) =
        System.Object.ReferenceEquals(this, other)

    override this.GetHashCode() : int =
        this.Repository.GetHashCode()

    interface System.IComparable with
        member this.CompareTo(other) =
            match other with
            | :? Repository as x -> System.Collections.Generic.Comparer<Anthology.RepositoryId>.Default.Compare(this.Repository.Repository.Name, x.Repository.Repository.Name)
            | _ -> failwith "Can't compare values with different types"

    member this.Name : string = this.Repository.Repository.Name.toString

    member this.Builder = match this.Repository.Builder with
                          | Anthology.BuilderType.MSBuild -> BuilderType.MSBuild
                          | Anthology.BuilderType.Skip -> BuilderType.Skip

    member this.Vcs = match this.Graph.Anthology.Vcs with
                      | Anthology.VcsType.Gerrit -> VcsType.Gerrit
                      | Anthology.VcsType.Git -> VcsType.Git
                      | Anthology.VcsType.Hg -> VcsType.Hg

    member this.Branch = match this.Repository.Repository.Branch with
                         | Some x -> x.toString
                         | None -> match this.Vcs with
                                   | VcsType.Gerrit | VcsType.Git -> "master"
                                   | VcsType.Hg -> "default"

    member this.Uri = this.Repository.Repository.Url.toString

    member this.Projects =
        let repositoryId = this.Repository.Repository.Name
        this.Graph.Anthology.Projects |> Seq.filter (fun x -> x.Repository = repositoryId)
                                      |> Seq.map (fun x -> this.Graph.ProjectMap.[x.ProjectId])

and [<CustomEquality; CustomComparison>] Project =
    { Graph : Graph
      Project : Anthology.Project }

    override this.Equals(other : System.Object) =
        System.Object.ReferenceEquals(this, other)

    override this.GetHashCode() : int =
        this.Project.GetHashCode()

    interface System.IComparable with
        member this.CompareTo(other) =
            match other with
            | :? Project as x -> System.Collections.Generic.Comparer<Anthology.ProjectId>.Default.Compare(this.Project.ProjectId, x.Project.ProjectId)
            | _ -> failwith "Can't compare values with different types"

    member this.Repository =
        this.Graph.RepositoryMap.[this.Project.Repository]

    member this.Applications =
        let projectId = this.Project.ProjectId
        this.Graph.Anthology.Applications |> Seq.filter (fun x -> x.Projects |> Set.contains projectId)
                                          |> Seq.map (fun x -> this.Graph.ApplicationMap.[x.Name])

    member this.References =
        let referenceIds = this.Project.ProjectReferences
        referenceIds |> Seq.map (fun x -> this.Graph.ProjectMap.[x])

    member this.ReferencedBy =
        let projectId = this.Project.ProjectId
        this.Graph.Anthology.Projects |> Seq.filter (fun x -> x.ProjectReferences |> Set.contains projectId)
                                      |> Seq.map (fun x -> this.Graph.ProjectMap.[x.ProjectId])

    member this.RelativeProjectFile = this.Project.RelativeProjectFile.toString

    member this.UniqueProjectId = this.Project.UniqueProjectId.toString

    member this.Output = this.Graph.AssemblyMap.[this.Project.Output]

    member this.ProjectId = this.Project.ProjectId.toString

    member this.OutputType = match this.Project.OutputType with
                             | Anthology.OutputType.Dll -> OutputType.Dll
                             | Anthology.OutputType.Exe -> OutputType.Exe

    member this.FxVersion = match this.Project.FxVersion.toString with
                            | null -> None
                            | x -> Some x

    member this.FxProfile = match this.Project.FxProfile.toString with
                            | null -> None
                            | x -> Some x

    member this.FxIdentifier = match this.Project.FxIdentifier.toString with
                               | null -> None
                               | x -> Some x

    member this.HasTests = this.Project.HasTests

    member this.AssemblyReferences = 
        this.Project.AssemblyReferences |> Seq.map (fun x -> this.Graph.AssemblyMap.[x])

    member this.PackageReferences = 
        this.Project.PackageReferences |> Seq.map (fun x -> this.Graph.PackageMap.[x])


and [<Sealed>] Graph(anthology : Anthology.Anthology) =
    let mutable assemblyMap : System.Collections.Generic.IDictionary<Anthology.AssemblyId, Assembly> = null
    let mutable packageMap : System.Collections.Generic.IDictionary<Anthology.PackageId, Package> = null
    let mutable repositoryMap : System.Collections.Generic.IDictionary<Anthology.RepositoryId, Repository> = null
    let mutable applicationMap : System.Collections.Generic.IDictionary<Anthology.ApplicationId, Application> = null
    let mutable projectMap : System.Collections.Generic.IDictionary<Anthology.ProjectId, Project> = null

    member this.Anthology : Anthology.Anthology = anthology

    member this.PackageMap : System.Collections.Generic.IDictionary<Anthology.PackageId, Package> =
        if packageMap |> isNull then
            packageMap <- anthology.Projects |> Set.map (fun x -> x.PackageReferences)
                                             |> Set.unionMany
                                             |> Seq.map (fun x -> x, { Graph = this; Package = x})
                                             |> dict
        packageMap

    member this.AssemblyMap : System.Collections.Generic.IDictionary<Anthology.AssemblyId, Assembly> =
        if assemblyMap |> isNull then 
            let outputAss = anthology.Projects |> Seq.map (fun x -> x.Output)
                                               |> Set
            assemblyMap <- anthology.Projects |> Set.map (fun x -> x.AssemblyReferences)
                                              |> Set.unionMany
                                              |> Set.union outputAss
                                              |> Seq.map (fun x -> x, { Graph = this; Assembly = x})
                                              |> dict
        assemblyMap

    member this.RepositoryMap : System.Collections.Generic.IDictionary<Anthology.RepositoryId, Repository> =
        if repositoryMap |> isNull then 
            repositoryMap <- anthology.Repositories |> Seq.map (fun x -> x.Repository.Name, { Graph = this; Repository = x})                                 
                                                    |> dict
        repositoryMap

    member this.ApplicationMap : System.Collections.Generic.IDictionary<Anthology.ApplicationId, Application> =
        if applicationMap |> isNull then
            applicationMap <- anthology.Applications |> Seq.map (fun x -> x.Name, { Graph = this; Application = x } )
                                                     |> dict
        applicationMap

    member this.ProjectMap : System.Collections.Generic.IDictionary<Anthology.ProjectId, Project> = 
        if projectMap |> isNull then
            projectMap <- anthology.Projects |> Seq.map (fun x -> x.ProjectId, { Graph = this; Project = x } )
                                             |> dict
        projectMap

    member this.Repositories = this.RepositoryMap.Values |> seq

    member this.Assemblies = this.AssemblyMap.Values |> seq

    member this.Packages = this.PackageMap.Values |> seq

    member this.Applications = this.ApplicationMap.Values |> seq

    member this.Projects = this.ProjectMap.Values |> seq

let from (antho : Anthology.Anthology) : Graph =
    Graph(antho)
