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

// Graph model is as follow:
// - Repository references projects
// - Project references dependency projects, assemblies and packages
// - App references main project
// - Graph references all projects, all assemblies, all packages, all repositories
//
//      App1                   App2            <----+
//       |                      |                   |
//       |                      v                   |  G
//       |              +- Project3              <--+  r
//       |              |       ^                   |  a
//       v              v       |     Project4   <--+  p
//  Project1    Project2        |        ^          |  h
//      ^         ^             |        |          |
//      |         |             |        |          |
//      Repository1             Repository2     <---+

type Project =
    { UnderlyingProject : Anthology.Project
      ProjectReferences : Project set }

type Repository =
    { UnderlyingRepository : Anthology.BuildableRepository
      Projects : Project set }

type Application =
    { UnderlyingApplication : Anthology.Application
      Project : Project }

type Graph =
    { Repositories : Repository set
      Projects : Project set 
      Applications : Application set }


let toGraph (antho : Anthology.Anthology) : Graph =    
    // projectId -> project
    let projectId2project = antho.Projects 
                            |> Set.map (fun x -> x.ProjectId, x)
                            |> Map
    // find all roots
    let projectIds = projectId2project 
                     |> Seq.map (fun kvp -> kvp.Key) 
                     |> Set
    let referencedProjectIds = antho.Projects 
                               |> Set.map (fun x -> x.ProjectReferences)
                               |> Set.unionMany
    let roots = Set.difference projectIds referencedProjectIds
                |> Set.map (fun x -> projectId2project.[x]) 

    // recursively construct projects using fold
    let rec buildProjectHierarchy (convertedProjects : Map<Anthology.ProjectId, Project>) (project : Anthology.Project) =
        let convertedProjectIds = convertedProjects |> Seq.map (fun kvp -> kvp.Key) 
                                                    |> Set
        let projectsToConvert = Set.difference project.ProjectReferences convertedProjectIds  
                                |> Set.map (fun x -> projectId2project.[x])      
        let newConvertedProjects = projectsToConvert 
                                   |> Set.fold buildProjectHierarchy convertedProjects
        let newProject = { UnderlyingProject = project
                           ProjectReferences = project.ProjectReferences |> Set.map (fun x -> newConvertedProjects.[x]) }
        newConvertedProjects |> Map.add project.ProjectId newProject

    let allConvertedProjects  = roots 
                                |> Set.fold buildProjectHierarchy Map.empty

    // gather all projects
    let allProjects = allConvertedProjects |> Seq.map (fun kvp -> kvp.Value) |> Set

    // gather all repositories
    let repositoryId2repository = antho.Repositories |> Set.map (fun x -> x.Repository.Name, x) |> Map
    let allRepositories = allProjects |> Seq.groupBy (fun x -> x.UnderlyingProject.Repository)
                                      |> Seq.map (fun (rid, prjs) -> { UnderlyingRepository = repositoryId2repository.[rid]
                                                                       Projects = prjs |> Set })
                                      |> Set

    // gather all apps
    let allApps = antho.Applications |> Set.map (fun x -> { UnderlyingApplication = x
                                                            Project = allConvertedProjects.[x.Project] })

    // done !
    let graph = { Projects = allProjects 
                  Repositories = allRepositories 
                  Applications = allApps }
    graph
