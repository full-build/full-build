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

module ArtifactsSerializer

open Anthology
open System.IO
open System
open Collections


type Artifacts =
    { MinVersion : string
      Binaries : string
      NuGets : RepositoryUrl list
      Vcs : VcsType
      MasterRepository : Repository
      Repositories : BuildableRepository set
      Applications : Application set
      Tester : TestRunnerType }


type private ArtifactsConfig = FSharp.Configuration.YamlConfig<"artifacts.yaml">


let Serialize (artifacts : Artifacts) =
    let config = new ArtifactsConfig()
    config.artifacts.binaries <- artifacts.Binaries
    config.artifacts.vcs <- artifacts.Vcs.toString
    config.artifacts.minversion <- artifacts.MinVersion

    config.artifacts.nugets.Clear()
    for nuget in artifacts.NuGets do
        let cnuget = ArtifactsConfig.artifacts_Type.nugets_Item_Type()
        cnuget.nuget <- Uri(nuget.toString)
        config.artifacts.nugets.Add (cnuget)

    config.artifacts.repositories.Clear()
    let repos = artifacts.Repositories
    for repo in repos do
        let crepo = ArtifactsConfig.artifacts_Type.repositories_Item_Type()
        crepo.repo <- repo.Repository.Name.toString
        crepo.uri <- Uri (repo.Repository.Url.toString)
        crepo.build <- repo.Builder.toString

        match repo.Repository.Branch with
        | None -> crepo.branch <- null
        | Some x -> crepo.branch <- x.toString
        config.artifacts.repositories.Add crepo

    let cmainrepo = config.artifacts.mainrepository
    cmainrepo.uri <- Uri (artifacts.MasterRepository.Url.toString)

    config.artifacts.apps.Clear ()
    for app in artifacts.Applications do
        let capp = ArtifactsConfig.artifacts_Type.apps_Item_Type()
        capp.name <- app.Name.toString
        capp.``type`` <- app.Publisher.toString
        capp.projects.Clear ()
        for project in app.Projects do
            let cproject = ArtifactsConfig.artifacts_Type.apps_Item_Type.projects_Item_Type()
            cproject.project <- project.toString
            capp.projects.Add (cproject)
        config.artifacts.apps.Add (capp)

    config.artifacts.test <- artifacts.Tester.toString

    config.ToString()

let Deserialize (content) =
    let rec convertToNuGets (items : ArtifactsConfig.artifacts_Type.nugets_Item_Type list) =
        match items with
        | [] -> []
        | x :: tail -> (RepositoryUrl.from (x.nuget)) :: convertToNuGets tail

    let convertToRepository (item : ArtifactsConfig.artifacts_Type.mainrepository_Type) : Repository =
        { Url = RepositoryUrl.from (item.uri)
          Branch = None
          Name = RepositoryId.from Env.MASTER_REPO }

    let rec convertToBuildableRepositories (items : ArtifactsConfig.artifacts_Type.repositories_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> let maybeBranch = if String.IsNullOrEmpty(x.branch) then None
                                         else x.branch |> BranchId.from |> Some
                       convertToBuildableRepositories tail |> Set.add { Repository = { Branch = maybeBranch
                                                                                       Url = RepositoryUrl.from (x.uri)
                                                                                       Name = RepositoryId.from x.repo }
                                                                        Builder = BuilderType.from x.build }

    let rec convertToApplications (items : ArtifactsConfig.artifacts_Type.apps_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> let appName = ApplicationId.from x.name
                       let publishType = PublisherType.from x.``type``
                       let projects = x.projects |> Seq.map (fun x -> ProjectId.from x.project) |> Set
                       let app = { Name = appName ; Publisher = publishType; Projects = projects }
                       convertToApplications tail |> Set.add app

    let convertToTestRunner (item : string) =
        TestRunnerType.from item

    let config = new ArtifactsConfig()
    config.LoadText content

    let repos = convertToBuildableRepositories (config.artifacts.repositories |> List.ofSeq)
    let mainRepo = convertToRepository (config.artifacts.mainrepository)
    { MinVersion = config.artifacts.minversion
      Binaries = config.artifacts.binaries
      Vcs = VcsType.from config.artifacts.vcs
      NuGets = convertToNuGets (config.artifacts.nugets |> List.ofSeq)
      MasterRepository = mainRepo
      Repositories = repos
      Applications = convertToApplications (config.artifacts.apps |> List.ofSeq)
      Tester = convertToTestRunner (config.artifacts.test) }

let Save (filename : FileInfo) (artifacts : Artifacts) =
    let content = Serialize artifacts
    File.WriteAllText(filename.FullName, content)

let Load (filename : FileInfo) : Artifacts =
    let content = File.ReadAllText (filename.FullName)
    Deserialize content
