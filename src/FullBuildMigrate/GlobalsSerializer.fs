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

module GlobalsSerializer

open Anthology
open System.IO
open System
open Collections


type private GlobalsConfig = FSharp.Configuration.YamlConfig<"Examples/globals.yaml">


let Serialize (artifacts : Globals) =
    let config = new GlobalsConfig()
    config.binaries <- artifacts.Binaries
    config.vcs <- artifacts.Vcs.toString
    config.minversion <- artifacts.MinVersion

    config.nugets.Clear()
    for nuget in artifacts.NuGets do
        let cnuget = GlobalsConfig.nugets_Item_Type()
        cnuget.nuget <- nuget.toString
        config.nugets.Add (cnuget)

    config.repositories.Clear()
    let repos = artifacts.Repositories
    for repo in repos do
        let crepo = GlobalsConfig.repositories_Item_Type()
        crepo.repo <- repo.Repository.Name.toString
        crepo.uri <- repo.Repository.Url.toString
        crepo.build <- repo.Builder.toString

        match repo.Repository.Branch with
        | None -> crepo.branch <- null
        | Some x -> crepo.branch <- x.toString
        config.repositories.Add crepo

    let cmainrepo = config.mainrepository
    cmainrepo.uri <- artifacts.MasterRepository.Url.toString

    // tester
    config.test <- artifacts.Tester.toString

    config.ToString()

let Deserialize (content) =
    let rec convertToNuGets (items : GlobalsConfig.nugets_Item_Type list) =
        match items with
        | [] -> []
        | x :: tail -> (RepositoryUrl.from (x.nuget)) :: convertToNuGets tail

    let convertToRepository (item : GlobalsConfig.mainrepository_Type) : Repository =
        { Url = RepositoryUrl.from (item.uri)
          Branch = None
          Name = RepositoryId.from Env.MASTER_REPO }

    let rec convertToBuildableRepositories (items : GlobalsConfig.repositories_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> let maybeBranch = if String.IsNullOrEmpty(x.branch) then None
                                         else x.branch |> BranchId.from |> Some
                       convertToBuildableRepositories tail |> Set.add { Repository = { Branch = maybeBranch
                                                                                       Url = RepositoryUrl.from (x.uri)
                                                                                       Name = RepositoryId.from x.repo }
                                                                        Builder = BuilderType.from x.build }

    let convertToTestRunner (item : string) =
        TestRunnerType.from item

    let config = new GlobalsConfig()
    config.LoadText content

    let repos = convertToBuildableRepositories (config.repositories |> List.ofSeq)
    let mainRepo = convertToRepository (config.mainrepository)
    { MinVersion = config.minversion
      Binaries = config.binaries
      Vcs = VcsType.from config.vcs
      NuGets = convertToNuGets (config.nugets |> List.ofSeq)
      MasterRepository = mainRepo
      Repositories = repos
      Tester = TestRunnerType.from config.test }

let Save (filename : FileInfo) (artifacts : Globals) =
    let content = Serialize artifacts
    File.WriteAllText(filename.FullName, content)

let Load (filename : FileInfo) : Globals =
    let content = File.ReadAllText (filename.FullName)
    Deserialize content
