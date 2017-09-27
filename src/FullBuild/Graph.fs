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

module Graph
open Collections
open XmlHelpers


#nowarn "0346" // GetHashCode missing

[<RequireQualifiedAccess>]
type PackageVersion =
    | PackageVersion of string
    | Unspecified

[<RequireQualifiedAccess>]
type OutputType =
    | Exe
    | Dll
    | Database

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
    | Git
    | Gerrit
    | Hg
    | Svn

[<RequireQualifiedAccess>]
type TestRunnerType =
    | NUnit
    | Skip


// =====================================================================================================

[<CustomEquality; CustomComparison>]
type Package =
    { Graph : Graph
      Package : Anthology.PackageId }
with
    override this.Equals(other : System.Object) = refEquals this other

    interface System.IComparable with
        member this.CompareTo(other) = compareTo this other (fun x -> x.Package)

    member this.Name = this.Package.toString

// =====================================================================================================

and [<CustomEquality; CustomComparison>] Assembly =
    { Graph : Graph
      Assembly : Anthology.AssemblyId }
with
    override this.Equals(other : System.Object) = refEquals this other

    interface System.IComparable with
        member this.CompareTo(other) = compareTo this other (fun x -> x.Assembly)

    member this.Name = this.Assembly.toString

// =====================================================================================================

and [<CustomEquality; CustomComparison>] Application =
    { Graph : Graph
      Application : Anthology.Application }
with
    override this.Equals(other : System.Object) = refEquals this other

    interface System.IComparable with
        member this.CompareTo(other) = compareTo this other (fun x -> x.Application.Name)

    member this.Name = this.Application.Name.toString

    member this.Publisher = match this.Application.Publisher with
                            | Anthology.PublisherType.Copy -> PublisherType.Copy
                            | Anthology.PublisherType.Zip -> PublisherType.Zip
                            | Anthology.PublisherType.Docker -> PublisherType.Docker

    member this.Project =
        this.Graph.ProjectMap.[this.Application.Project]

    member this.Delete () =
        let newAntho = { this.Graph.Anthology
                         with Applications = this.Graph.Anthology.Applications |> Set.remove this.Application }
        Graph(this.Graph.Globals, newAntho)

// =====================================================================================================

and [<CustomEquality; CustomComparison>] Repository =
    { Graph : Graph
      Repository : Anthology.Repository }
with
    override this.Equals(other : System.Object) = refEquals this other

    interface System.IComparable with
        member this.CompareTo(other) = compareTo this other (fun x -> x.Repository.Name)

    member this.Name : string = this.Repository.Name.toString

    member this.Builder =
        let buildableRepo = this.Graph.Globals.Repositories |> Seq.tryFind (fun x -> x.Repository.Name = this.Repository.Name)
        match buildableRepo with
        | Some repo -> match repo.Builder with
                       | Anthology.BuilderType.MSBuild -> BuilderType.MSBuild
                       | Anthology.BuilderType.Skip -> BuilderType.Skip
        | _ -> BuilderType.Skip

    member this.Vcs = match this.Repository.Vcs with
                      | Anthology.VcsType.Gerrit -> VcsType.Gerrit
                      | Anthology.VcsType.Git -> VcsType.Git
                      | Anthology.VcsType.Hg -> VcsType.Hg
                      | Anthology.VcsType.Svn -> VcsType.Svn

    member this.Tester =
        let buildableRepo = this.Graph.Globals.Repositories |> Seq.tryFind (fun x -> x.Repository.Name = this.Repository.Name)
        match buildableRepo with
        | Some repo -> match repo.Tester with
                       | Anthology.TestRunnerType.NUnit -> TestRunnerType.NUnit
                       | Anthology.TestRunnerType.Skip -> TestRunnerType.Skip
        | _ -> failwithf "Repository %A is not buildable" this.Repository.Name

    member this.Branch = match this.Repository.Branch with
                         | Some x -> x.toString
                         | None -> match this.Vcs with
                                   | VcsType.Gerrit | VcsType.Git -> "master"
                                   | VcsType.Hg -> "main" 
                                   | VcsType.Svn -> "unspecified"

    member this.Uri = this.Repository.Url.toString

    member this.Projects =
        try
            let repositoryId = this.Repository.Name
            this.Graph.Anthology.Projects |> Set.filter (fun x -> x.Repository = repositoryId)
                                          |> Set.map (fun x -> this.Graph.ProjectMap.[x.ProjectId])
        with
            _ -> failwithf "Failure to find projects for repository %A" this.Name

    member this.IsCloned =
        let wsDir = Env.GetFolder Env.Folder.Workspace
        let repoDir = wsDir |> FsHelpers.GetSubDirectory this.Name
        repoDir.Exists

    member this.References =
        try
            this.Projects |> Set.map (fun x -> x.References |> Set.map (fun y -> y.Repository))
                          |> Set.unionMany
                          |> Set.remove this
        with
            _ -> failwithf "Failure to find references for repository %A" this.Name

    member this.ReferencedBy =
        try
            this.Projects |> Set.map (fun x -> x.ReferencedBy |> Set.map (fun y -> y.Repository))
                          |> Set.unionMany
                          |> Set.remove this
        with
            _ -> failwithf "Failure to find referencedBy for repository %A" this.Name

    static member Closure (seeds : Repository set) =
        Algorithm.Closure seeds (fun x -> x.Name) (fun x -> x.References) (fun x -> x.ReferencedBy)

    member this.Delete () =
        let repositoryId = this.Repository.Name
        let newGlobals = { this.Graph.Globals
                           with Repositories = this.Graph.Globals.Repositories |> Set.filter (fun x -> x.Repository.Name <> repositoryId) }
        let newAntho = { this.Graph.Anthology
                         with Projects = this.Graph.Anthology.Projects |> Set.filter (fun x -> x.Repository <> repositoryId) }
        Graph(newGlobals, newAntho)

// =====================================================================================================

and [<CustomEquality; CustomComparison>] Project =
    { Graph : Graph
      Project : Anthology.Project }
with
    override this.Equals(other : System.Object) = refEquals this other

    interface System.IComparable with
        member this.CompareTo(other) = compareTo this other (fun x -> x.Project.ProjectId)

    member this.Repository =
        this.Graph.RepositoryMap.[this.Project.Repository]

    member this.Applications =
        let projectId = this.Project.ProjectId
        this.Graph.Anthology.Applications |> Set.filter (fun x -> x.Project = projectId)
                                          |> Set.map (fun x -> this.Graph.ApplicationMap.[x.Name])

    member this.References : Project set =
        let referenceIds = this.Project.ProjectReferences
        referenceIds |> Set.map (fun x -> this.Graph.ProjectMap.[x])

    member this.ReferencedBy : Project set =
        let projectId = this.Project.ProjectId
        this.Graph.Anthology.Projects |> Set.filter (fun x -> x.ProjectReferences |> Set.contains projectId)
                                      |> Set.map (fun x -> this.Graph.ProjectMap.[x.ProjectId])

    member this.ProjectFile =
        this.Project.RelativeProjectFile.toString

    member this.Output = this.Graph.AssemblyMap.[this.Project.Output]

    member this.BinFile =
        let repo = this.Repository.Name
        let path = System.IO.Path.GetDirectoryName(this.ProjectFile)
        let ass = this.Output.Name
        let ext = match this.OutputType with
                  | OutputType.Dll -> "dll"
                  | OutputType.Exe -> "exe"
                  | OutputType.Database -> "dacpac"
        sprintf "%s/%s/bin/%s.%s" repo path ass ext

    member this.UniqueProjectId = this.Project.UniqueProjectId.toString

    member this.ProjectId = this.Project.ProjectId.toString

    member this.OutputType = match this.Project.OutputType with
                             | Anthology.OutputType.Dll -> OutputType.Dll
                             | Anthology.OutputType.Exe -> OutputType.Exe
                             | Anthology.OutputType.Database -> OutputType.Database

    member this.HasTests = this.Project.HasTests

    member this.PackageReferences =
        this.Project.PackageReferences |> Set.map (fun x -> this.Graph.PackageMap.[x])

    static member CollectProjects (collector : Project -> Project set) (projects : Project set) =
        Set.fold (fun s t -> collector t |> Project.CollectProjects collector |> Set.union s) projects projects

    static member TransitiveReferences (seeds : Project set) : Project set =
        Project.CollectProjects (fun x -> x.References) seeds

    static member TransitiveReferencedBy (seeds : Project set) : Project set =
        Project.CollectProjects (fun x -> x.ReferencedBy) seeds

    static member Closure (seeds : Project set) : Project set =
        let repositories = seeds |> Set.map (fun x -> x.Repository)
                                 |> Repository.Closure
        let getRefs (x : Project) = x.References |> Set.filter (fun x -> repositories |> Set.contains x.Repository)
        let getRefBy (x : Project) = x.ReferencedBy |> Set.filter (fun x -> repositories |> Set.contains x.Repository)

        Algorithm.Closure seeds (fun x -> x.ProjectId) getRefs getRefBy

// =====================================================================================================

and [<Sealed>] Graph(globals : Anthology.Globals, anthology : Anthology.Anthology) =
    let mutable assemblyMap : System.Collections.Generic.IDictionary<Anthology.AssemblyId, Assembly> = null
    let mutable packageMap : System.Collections.Generic.IDictionary<Anthology.PackageId, Package> = null
    let mutable repositoryMap : System.Collections.Generic.IDictionary<Anthology.RepositoryId, Repository> = null
    let mutable applicationMap : System.Collections.Generic.IDictionary<Anthology.ApplicationId, Application> = null
    let mutable projectMap : System.Collections.Generic.IDictionary<Anthology.ProjectId, Project> = null
    let mutable packageMap : System.Collections.Generic.IDictionary<Anthology.PackageId, Package> = null

    member this.Anthology : Anthology.Anthology = anthology
    member this.Globals : Anthology.Globals = globals

    member this.PackageMap : System.Collections.Generic.IDictionary<Anthology.PackageId, Package> =
        if packageMap |> isNull then
            packageMap <- this.Anthology.Projects 
                                    |> Seq.map (fun x -> x.PackageReferences 
                                                                |> Seq.map (fun y -> y, { Graph = this; Package = y}))
                                    |> Seq.collect id
                                    |> dict
        packageMap

    member this.AssemblyMap : System.Collections.Generic.IDictionary<Anthology.AssemblyId, Assembly> =
        if assemblyMap |> isNull then
            assemblyMap <- anthology.Projects |> Seq.map (fun x -> x.Output)
                                              |> Seq.map (fun x -> x, { Graph = this; Assembly = x})
                                              |> dict
        assemblyMap

    member this.RepositoryMap : System.Collections.Generic.IDictionary<Anthology.RepositoryId, Repository> =
        if repositoryMap |> isNull then
            repositoryMap <- globals.Repositories |> Seq.map (fun x -> x.Repository.Name, { Graph = this; Repository = x.Repository})
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

    member this.SideBySide = globals.SideBySide

    member this.MinVersion = globals.MinVersion

    member this.MasterRepository = { Graph = this; Repository = globals.MasterRepository }

    member this.Repositories = this.RepositoryMap.Values |> set

    member this.Assemblies = this.AssemblyMap.Values |> set

    member this.Packages = this.PackageMap.Values |> set

    member this.Applications = this.ApplicationMap.Values |> set

    member this.Projects = this.ProjectMap.Values |> set

    member this.NuGets = globals.NuGets |> List.map (fun x -> x.toString)

    member this.ArtifactsDir = globals.Binaries

    member this.CreateApp name publisher (project : Project) =
        let pub = match publisher with
                  | PublisherType.Zip -> Anthology.PublisherType.Zip
                  | PublisherType.Copy -> Anthology.PublisherType.Copy
                  | PublisherType.Docker -> Anthology.PublisherType.Docker
        let app = { Anthology.Application.Name = Anthology.ApplicationId.from name
                    Anthology.Application.Publisher = pub
                    Anthology.Application.Project = Anthology.ProjectId.from project.ProjectId }
        let newAntho = { anthology
                         with Applications = anthology.Applications |> Set.add app }
        Graph(globals, newAntho)

    member this.CreateNuGet (url : string) =
        let newGlobals = { globals
                           with NuGets = globals.NuGets @ [Anthology.RepositoryUrl.from url] |> List.distinct }
        Graph(newGlobals, anthology)

    member this.CreateRepo name vcs (url : string) builder tester (branch : string option) =
        let repoBranch = match branch with
                         | Some x -> Some (Anthology.BranchId.from x)
                         | None -> None
        let repoVcs = match vcs with
                      | VcsType.Gerrit -> Anthology.VcsType.Gerrit
                      | VcsType.Git -> Anthology.VcsType.Git
                      | VcsType.Hg -> Anthology.VcsType.Hg
                      | VcsType.Svn -> Anthology.VcsType.Svn        
        let repo = { Anthology.Name = Anthology.RepositoryId.from name
                     Anthology.Url = Anthology.RepositoryUrl.from url
                     Anthology.Branch = repoBranch
                     Anthology.Vcs = repoVcs }
        let repoBuilder = match builder with
                          | BuilderType.MSBuild -> Anthology.BuilderType.MSBuild
                          | BuilderType.Skip -> Anthology.BuilderType.Skip
        let repoTester = match tester with
                         | TestRunnerType.NUnit -> Anthology.TestRunnerType.NUnit
                         | TestRunnerType.Skip -> Anthology.TestRunnerType.Skip
        let buildableRepo = { Anthology.Repository = repo; Anthology.Builder = repoBuilder; Anthology.Tester = repoTester }
        let newGlobals = { globals
                           with Repositories = globals.Repositories |> Set.add buildableRepo }
        Graph(newGlobals, anthology)

    member this.Save () =
        Configuration.SaveGlobals this.Globals
        Configuration.SaveAnthology this.Anthology


// =====================================================================================================


let from (globals : Anthology.Globals) (antho : Anthology.Anthology) : Graph =
    Graph (globals, antho)

let create vcs (uri : string) (artifacts : string) (sxs : bool) =
    let repoVcs = match vcs with
                  | VcsType.Git -> Anthology.VcsType.Git
                  | VcsType.Gerrit -> Anthology.VcsType.Gerrit
                  | VcsType.Hg -> Anthology.VcsType.Hg
                  | VcsType.Svn -> Anthology.VcsType.Svn
    let repo = { Anthology.Name = Anthology.RepositoryId.from Env.MASTER_REPO
                 Anthology.Url = Anthology.RepositoryUrl.from uri
                 Anthology.Branch = None
                 Anthology.Vcs = repoVcs }

    let globals = { Anthology.Globals.MinVersion = Env.FullBuildVersion().ToString()
                    Anthology.Globals.Binaries = artifacts
                    Anthology.Globals.SideBySide = sxs
                    Anthology.Globals.NuGets = []
                    Anthology.Globals.MasterRepository = repo
                    Anthology.Globals.Repositories = Set.empty }

    let antho =  { Anthology.Anthology.Projects = Set.empty
                   Anthology.Anthology.Applications = Set.empty}
    from globals antho


let init uri vcs =
    create vcs uri "dummy" false

let load () =
    let globals = Configuration.LoadGlobals()
    let anthology = Configuration.LoadAnthology()
    from globals anthology
