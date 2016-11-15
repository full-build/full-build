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

[<RequireQualifiedAccess>]
type PublisherType =
    | Copy
    | Zip
    | Docker
    | Nuget

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

    static member GetPackageDependencies (xnuspec : System.Xml.Linq.XDocument) =
        xnuspec.Descendants()
            |> Seq.filter (fun x -> x.Name.LocalName = "dependency" && (!> x.Attribute(NsNone + "exclude") : string) <> "Compile")
            |> Seq.map (fun x -> !> x.Attribute(NsNone + "id") : string)
            |> Seq.map Anthology.PackageId.from
            |> set

    static member GetFrameworkDependencies (xnuspec : System.Xml.Linq.XDocument) =
        xnuspec.Descendants()
            |> Seq.filter (fun x -> x.Name.LocalName = "frameworkAssembly")
            |> Seq.map (fun x -> !> x.Attribute(NsNone + "assemblyName") : string)
            |> Seq.map Anthology.AssemblyId.from
            |> set


    member this.Dependencies : Package set =
        let pkgsDir = Env.GetFolder Env.Folder.Package
        let pkgDir = pkgsDir |> IoHelpers.GetSubDirectory (this.Package.toString)
        let nuspecFile = pkgDir |> IoHelpers.GetFile (IoHelpers.AddExt IoHelpers.Extension.NuSpec (this.Package.toString))
        let xnuspec = System.Xml.Linq.XDocument.Load (nuspecFile.FullName)
        Package.GetPackageDependencies xnuspec |> Set.map (fun x -> { Graph = this.Graph
                                                                      Package = x })
        
    member this.FxAssemblies : Assembly set =
        let pkgsDir = Env.GetFolder Env.Folder.Package
        let pkgDir = pkgsDir |> IoHelpers.GetSubDirectory (this.Package.toString)
        let nuspecFile = pkgDir |> IoHelpers.GetFile (IoHelpers.AddExt IoHelpers.Extension.NuSpec (this.Package.toString))
        let xnuspec = System.Xml.Linq.XDocument.Load (nuspecFile.FullName)
        Package.GetFrameworkDependencies xnuspec |> Set.map (fun x -> { Graph = this.Graph; Assembly = x})

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
                            | Anthology.PublisherType.Nuget -> PublisherType.Nuget

    member this.Projects =
        this.Application.Projects |> Set.map (fun x -> this.Graph.ProjectMap.[x])

    member this.Delete () =
        let newAntho = { this.Graph.Anthology 
                         with Applications = this.Graph.Anthology.Applications |> Set.remove this.Application }
        Graph(newAntho)

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
        let buildableRepo = this.Graph.Anthology.Repositories |> Seq.tryFind (fun x -> x.Repository.Name = this.Repository.Name)
        match buildableRepo with
        | Some repo -> match repo.Builder with
                       | Anthology.BuilderType.MSBuild -> BuilderType.MSBuild
                       | Anthology.BuilderType.Skip -> BuilderType.Skip
        | _ -> BuilderType.Skip

    member this.Vcs = match this.Graph.Anthology.Vcs with
                      | Anthology.VcsType.Gerrit -> VcsType.Gerrit
                      | Anthology.VcsType.Git -> VcsType.Git
                      | Anthology.VcsType.Hg -> VcsType.Hg

    member this.Branch = match this.Repository.Branch with
                         | Some x -> x.toString
                         | None -> match this.Vcs with
                                   | VcsType.Gerrit | VcsType.Git -> "master"
                                   | VcsType.Hg -> "default"

    member this.Uri = this.Repository.Url.toString

    member this.Projects =
        let repositoryId = this.Repository.Name
        this.Graph.Anthology.Projects |> Set.filter (fun x -> x.Repository = repositoryId)
                                      |> Set.map (fun x -> this.Graph.ProjectMap.[x.ProjectId])

    member this.IsCloned =
        let wsDir = Env.GetFolder Env.Folder.Workspace
        let repoDir = wsDir |> IoHelpers.GetSubDirectory this.Name
        repoDir.Exists

    member this.Delete () =
        let repositoryId = this.Repository.Name
        let newAntho = { this.Graph.Anthology
                         with Repositories = this.Graph.Anthology.Repositories |> Set.filter (fun x -> x.Repository.Name = repositoryId) }
        Graph(newAntho)

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
        this.Graph.Anthology.Applications |> Set.filter (fun x -> x.Projects |> Set.contains projectId)
                                          |> Set.map (fun x -> this.Graph.ApplicationMap.[x.Name])

    member this.References =
        let referenceIds = this.Project.ProjectReferences
        referenceIds |> Set.map (fun x -> this.Graph.ProjectMap.[x])

    member this.ReferencedBy =
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
        sprintf "%s/%s/bin/%s.%s" repo path ass ext

    member this.UniqueProjectId = this.Project.UniqueProjectId.toString

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
        this.Project.AssemblyReferences |> Set.map (fun x -> this.Graph.AssemblyMap.[x])

    member this.PackageReferences = 
        this.Project.PackageReferences |> Set.map (fun x -> this.Graph.PackageMap.[x])

    static member CollectProjects (collector : Project -> Project set) (projects : Project set) =
        Set.fold (fun s t -> collector t |> Project.CollectProjects collector |> Set.union s) projects projects

    static member TransitiveReferences (seeds : Project set) : Project set =
        Project.CollectProjects (fun x -> x.References) seeds

    static member TransitiveReferencedBy (seeds : Project set) : Project set =
        Project.CollectProjects (fun x -> x.ReferencedBy) seeds

    static member Closure (seeds : Project set) : Project set =
        let rec exploreNext (node : Project) (next : Project -> Project set) (path : Project list) (boundaries : Project set) =
            let nextNodes = next node
            Set.fold (fun s n -> s + explore n next path s) boundaries nextNodes

        and explore (node : Project) (next : Project -> Project set) (path : Project list) (boundaries : Project set) =
            let currPath = node :: path
            if boundaries |> Set.contains node then
                currPath |> set
            else
                exploreNext node next currPath boundaries

        let refBoundaries = Set.fold (fun s t -> exploreNext t (fun x -> x.References) [t] s) seeds seeds
        let refByBoundaries = Set.fold (fun s t -> exploreNext t (fun x -> x.ReferencedBy) [t] s) refBoundaries seeds
        refByBoundaries

// =====================================================================================================

and [<Sealed>] Graph(anthology : Anthology.Anthology) =
    let mutable assemblyMap : System.Collections.Generic.IDictionary<Anthology.AssemblyId, Assembly> = null
    let mutable packageMap : System.Collections.Generic.IDictionary<Anthology.PackageId, Package> = null
    let mutable repositoryMap : System.Collections.Generic.IDictionary<Anthology.RepositoryId, Repository> = null
    let mutable applicationMap : System.Collections.Generic.IDictionary<Anthology.ApplicationId, Application> = null
    let mutable projectMap : System.Collections.Generic.IDictionary<Anthology.ProjectId, Project> = null
    let mutable packageMap : System.Collections.Generic.IDictionary<Anthology.PackageId, Package> = null

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
            repositoryMap <- anthology.Repositories |> Seq.map (fun x -> x.Repository.Name, { Graph = this; Repository = x.Repository})                                 
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

    member this.MinVersion = this.Anthology.MinVersion

    member this.MasterRepository = { Graph = this; Repository = this.Anthology.MasterRepository }

    member this.Repositories = this.RepositoryMap.Values |> set

    member this.Assemblies = this.AssemblyMap.Values |> set

    member this.Packages = this.PackageMap.Values |> set

    member this.Applications = this.ApplicationMap.Values |> set

    member this.Projects = this.ProjectMap.Values |> set

    member this.NuGets = this.Anthology.NuGets |> List.map (fun x -> x.toString)

    member this.TestRunner =
        match this.Anthology.Tester with
        | Anthology.TestRunnerType.NUnit -> TestRunnerType.NUnit

    member this.ArtifactsDir = this.Anthology.Artifacts

    member this.CreateApp name publisher (projects : Project set) =
        let pub = match publisher with
                  | PublisherType.Zip -> Anthology.PublisherType.Zip
                  | PublisherType.Copy -> Anthology.PublisherType.Copy
                  | PublisherType.Docker -> Anthology.PublisherType.Docker
                  | PublisherType.Nuget -> Anthology.PublisherType.Nuget
        let projectIds = projects |> Set.map (fun x -> Anthology.ProjectId.from x.Output.Name)
        let app = { Anthology.Application.Name = Anthology.ApplicationId.from name
                    Anthology.Application.Publisher = pub
                    Anthology.Application.Projects = projectIds }
        let newAntho = { anthology 
                         with Applications = anthology.Applications |> Set.add app }
        Graph(newAntho)

    member this.CreateNuGet (url : string) =
        let newAntho = { anthology
                         with NuGets = anthology.NuGets @ [Anthology.RepositoryUrl.from url] |> List.distinct }
        Graph(newAntho)

    member this.CreateRepo name (url : string) builder (branch : string option) =
        let repoBranch = match branch with
                         | Some x -> Some (Anthology.BranchId.from x)
                         | None -> None
        let repo = { Anthology.Name = Anthology.RepositoryId.from name
                     Anthology.Url = Anthology.RepositoryUrl.from url
                     Anthology.Branch = repoBranch }
        let repoBuilder = match builder with
                           | BuilderType.MSBuild -> Anthology.BuilderType.MSBuild
                           | BuilderType.Skip -> Anthology.BuilderType.Skip
        let buildableRepo = { Anthology.Repository = repo; Anthology.Builder = repoBuilder }
        let newAntho = { anthology
                         with Repositories = anthology.Repositories |> Set.add buildableRepo }
        Graph(newAntho)

    member this.Save () =
        Configuration.SaveAnthology this.Anthology


// =====================================================================================================


let from (antho : Anthology.Anthology) : Graph =
    Graph(antho)

let create (uri : string) (artifacts : string) vcs runner =
    let repo = { Anthology.Name = Anthology.RepositoryId.from Env.MASTER_REPO
                 Anthology.Url = Anthology.RepositoryUrl.from uri
                 Anthology.Branch = None }

    let anthoVcs = match vcs with
                   | VcsType.Gerrit -> Anthology.VcsType.Gerrit
                   | VcsType.Git -> Anthology.VcsType.Git
                   | VcsType.Hg -> Anthology.VcsType.Hg

    let anthoRunner = match runner with
                      | TestRunnerType.NUnit -> Anthology.TestRunnerType.NUnit

    let antho = { Anthology.Anthology.MinVersion = Env.FullBuildVersion().ToString()
                  Anthology.Anthology.Artifacts = artifacts
                  Anthology.Anthology.NuGets = []
                  Anthology.Anthology.MasterRepository = repo
                  Anthology.Anthology.Repositories = Set.empty
                  Anthology.Anthology.Projects = Set.empty
                  Anthology.Anthology.Applications = Set.empty
                  Anthology.Anthology.Tester = anthoRunner
                  Anthology.Anthology.Vcs = anthoVcs }
    from antho


let init uri vcs =
    create uri "dummy" vcs TestRunnerType.NUnit
