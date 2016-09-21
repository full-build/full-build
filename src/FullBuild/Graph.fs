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



//let toGraph (antho : Anthology.Anthology) : Graph =    
//    // projectId -> project
//    let projectId2project = antho.Projects 
//                            |> Set.map (fun x -> x.ProjectId, x)
//                            |> Map
//    // find all roots
//    let projectIds = projectId2project 
//                     |> Seq.map (fun kvp -> kvp.Key) 
//                     |> Set
//    let referencedProjectIds = antho.Projects 
//                               |> Set.map (fun x -> x.ProjectReferences)
//                               |> Set.unionMany
//
//    let roots = Set.difference projectIds referencedProjectIds
//                |> Set.map (fun x -> projectId2project.[x]) 
//
//    let leafs = antho.Projects
//                |> Set.filter (fun x -> x.ProjectReferences = Set.empty)
//
//    // recursively construct projects using fold
//    let rec build refs (convertedProjects : Map<Anthology.ProjectId, BuildProject>) (project : Anthology.Project) : Map<Anthology.ProjectId, BuildProject> =
//        let convertedProjectIds = convertedProjects |> Seq.map (fun kvp -> kvp.Key) 
//                                                    |> Set
//        let dependencies = refs project
//        let projectsToConvert = Set.difference dependencies convertedProjectIds  
//                                |> Set.map (fun x -> projectId2project.[x])      
//        let newConvertedProjects = projectsToConvert 
//                                   |> Set.fold (build (refs)) convertedProjects
//        let newProject = { UnderlyingProject = project
//                           Dependencies = dependencies }
//        newConvertedProjects |> Map.add project.ProjectId newProject
//
//    let getReferences (project : Anthology.Project) = project.ProjectReferences
//    let getConsumers (project : Anthology.Project) = antho.Projects 
//                                                     |> Set.filter (fun x -> x.ProjectReferences |> Set.contains project.ProjectId)
//                                                     |> Set.map (fun x -> x.ProjectId)
//
//    let buildReferences = build getReferences
//    let buildConsumers = build getConsumers
//
//    let allConvertedReferences = roots |> Set.fold buildReferences Map.empty
//    let allConvertedConsumers = leafs |> Set.fold buildConsumers Map.empty
//
//    let allConvertedProjects = allConvertedReferences |> Map.map (fun k v -> { UnderlyingProject = v.UnderlyingProject
//                                                                               References = v.Dependencies
//                                                                               Consumers = allConvertedConsumers.[k].Dependencies } )
//
//    // gather all projects
//    let allProjects = allConvertedProjects |> Seq.map (fun kvp -> kvp.Value) |> Set
//
//    // gather all repositories
//    let repositoryId2repository = antho.Repositories |> Set.map (fun x -> x.Repository.Name, x) |> Map
//    let allRepositories = allProjects |> Seq.groupBy (fun x -> x.UnderlyingProject.Repository)
//                                      |> Seq.map (fun (rid, prjs) -> { UnderlyingRepository = repositoryId2repository.[rid]
//                                                                       Projects = prjs |> Set })
//                                      |> Set
//
//    // gather all apps
//    let allApps = antho.Applications |> Set.map (fun x -> { UnderlyingApplication = x
//                                                            Project = allConvertedProjects.[x.Project] })
//
//    // done !
//    let graph = { Projects = allConvertedProjects 
//                  Repositories = allRepositories 
//                  Applications = allApps }
//    graph





type [<RequireQualifiedAccess>] PackageVersion =
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

type Package =
    { Anthology : Anthology.Anthology
      Package : Anthology.PackageId }
    with
        member this.Name = this.Package.toString

type Assembly = 
    { Anthology : Anthology.Anthology
      Assembly : Anthology.AssemblyId }
    with
        member this.Name = this.Assembly.toString

type Application =
    { Anthology : Anthology.Anthology
      Application : Anthology.Application } 
    with
        member this.Name = this.Application.Name.toString

        member this.Publisher = match this.Application.Publisher with
                                | Anthology.PublisherType.Copy -> PublisherType.Copy
                                | Anthology.PublisherType.Zip -> PublisherType.Zip
                                | Anthology.PublisherType.Docker -> PublisherType.Docker

        member this.Project : Project =
            { Anthology = this.Anthology
              Project = this.Anthology.Projects |> Seq.find (fun x -> x.ProjectId = this.Application.Project) }

and Repository =
    { Anthology : Anthology.Anthology
      Repository : Anthology.BuildableRepository }
    with
        member this.Name = this.Repository.Repository.Name.toString

        member this.Builder = match this.Repository.Builder with
                              | Anthology.BuilderType.MSBuild -> BuilderType.MSBuild
                              | Anthology.BuilderType.Skip -> BuilderType.Skip

        member this.Projects =
            this.Anthology.Projects |> Set.filter (fun x -> x.Repository = this.Repository.Repository.Name)
                                    |> Set.map (fun x -> { Anthology = this.Anthology
                                                           Project = x })

and Project =
    { Anthology : Anthology.Anthology
      Project : Anthology.Project }
    with
        member this.Repository : Repository =
            { Anthology = this.Anthology
              Repository = this.Anthology.Repositories |> Seq.find (fun x -> x.Repository.Name = this.Project.Repository) }

        member this.Application : Application option =
            let app = this.Anthology.Applications |> Seq.tryFind (fun x -> x.Project = this.Project.ProjectId)
            match app with
            | Some x -> Some { Anthology = this.Anthology
                               Application = x }
            | _ -> None

        member this.ProjectReferences : Project seq =
            this.Anthology.Projects |> Set.filter (fun x -> this.Project.ProjectReferences |> Set.contains x.ProjectId) 
                                    |> Seq.map (fun x -> { Anthology = this.Anthology
                                                           Project = x })

        member this.ReferencedBy : Project seq =
            this.Anthology.Projects |> Set.filter (fun x -> x.ProjectReferences |> Set.contains this.Project.ProjectId) 
                                    |> Seq.map (fun x -> { Anthology = this.Anthology
                                                           Project = x })

        member this.RelativeProjectFile = this.Project.RelativeProjectFile.toString
        member this.UniqueProjectId = this.Project.UniqueProjectId.toString
        member this.Output = { Anthology = this.Anthology
                               Assembly = this.Project.Output }
        member this.ProjectId = this.Project.ProjectId.toString
        member this.OutputType = match this.Project.OutputType with
                                 | Anthology.OutputType.Dll -> OutputType.Dll
                                 | Anthology.OutputType.Exe -> OutputType.Exe
        member this.FxVersion = this.Project.FxVersion.toString
        member this.FxProfile = this.Project.FxProfile.toString
        member this.FxIdentifier = this.Project.FxIdentifier.toString
        member this.HasTests = this.Project.HasTests
        member this.AssemblyReferences = this.Project.AssemblyReferences |> Set.map (fun x -> { Anthology = this.Anthology
                                                                                                Assembly = x })
        member this.PackageReferences = this.Project.PackageReferences |> Set.map (fun x -> { Anthology = this.Anthology
                                                                                              Package = x })


and Graph =
    { Anthology : Anthology.Anthology }
    with
        static member from (antho : Anthology.Anthology) : Graph =
            { Anthology = antho }

        member this.Projects =
            this.Anthology.Projects |> Set.map (fun x -> { Anthology = this.Anthology
                                                           Project = x })

        member this.Applications =
            this.Anthology.Applications |> Set.map (fun x -> { Anthology = this.Anthology
                                                               Application = x })

        member this.Repositories =
            this.Anthology.Repositories |> Set.map (fun x -> { Anthology = this.Anthology
                                                               Repository = x })

