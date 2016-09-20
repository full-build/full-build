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




[<RequireQualifiedAccess>]
type PackageVersion =
    | PackageVersion of string
    | Unspecified

and Package =
    { Anthology : Anthology.Anthology
      Package : Anthology.Application }
    with
        member this.Name : string = this.Package.Name
        member this.Version : PackageVersion = this.Package.PackageVers

    member Name : string
    member Version : PackageVersion

type Application =
    { Anthology : Anthology.Anthology
      Application : Anthology.Application } 
    with
        member this.UnderlyingApplication : Anthology.Application = this.Application

        member this.Project : Project =
            { Anthology = this.Anthology
              Project = this.Anthology.Projects |> Seq.find (fun x -> x.ProjectId = this.Application.Project) }

and Repository =
    { Anthology : Anthology.Anthology
      Repository : Anthology.BuildableRepository }
    with
        member this.UnderlyingRepository : Anthology.BuildableRepository = this.Repository

        member this.Projects : Project seq =
            this.Anthology.Projects |> Seq.filter (fun x -> x.Repository = this.Repository.Repository.Name)
                                    |> Seq.map (fun x -> { Anthology = this.Anthology
                                                           Project = x })

and Project =
    { Anthology : Anthology.Anthology
      Project : Anthology.Project }
    with
        member this.UnderlyingProject : Anthology.Project = this.Project

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

and Graph =
    { Anthology : Anthology.Anthology }
    with
        static member from (antho : Anthology.Anthology) : Graph =
            { Anthology = antho }

        member this.Projects : Project seq =
            this.Anthology.Projects |> Seq.map (fun x -> { Anthology = this.Anthology
                                                           Project = x })

        member this.Applications : Application seq =
            this.Anthology.Applications |> Seq.map (fun x -> { Anthology = this.Anthology
                                                               Application = x })

        member this.Repositories : Repository seq =
            this.Anthology.Repositories |> Seq.map (fun x -> { Anthology = this.Anthology
                                                               Repository = x })

        

//and [<Sealed>] Repository =
//    member UnderlyingRepository : unit -> Anthology.BuildableRepository    
//    member Projects : unit -> Project seq
//and [<Sealed>] Project =
//    member UnderlyingProject : unit -> Anthology.Project
//    member Repository : unit -> Repository
//    member Application : unit -> Application option
//    member Consumers : unit -> Project seq
//    member Dependencies : unit -> Project seq
//
//
//type Graph =
//    { Repositories : Repository set
//      Projects : Map<Anthology.ProjectId, Project>
//      Applications : Application set }
//
//
//    member Projects : unit -> Project seq
//    member Applications : unit -> Application seq    
//    member Repositories : unit -> Repository seq   
//    static member from : Anthology.Anthology -> Graph 
//
//
