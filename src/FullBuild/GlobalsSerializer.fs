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
    config.minversion <- artifacts.MinVersion
    config.sxs <- artifacts.SideBySide

    config.repositories.Clear()
    let repos = artifacts.Repositories
    for repo in repos do
        let crepo = GlobalsConfig.repositories_Item_Type()
        crepo.repo <- repo.Repository.Name.toString
        crepo.uri <- repo.Repository.Url.toString
        crepo.build <- repo.Builder.toString
        crepo.vcs <- repo.Repository.Vcs.toString
        crepo.test <- repo.Tester.toString

        match repo.Repository.Branch with
        | None -> crepo.branch <- null
        | Some x -> crepo.branch <- x.toString
        config.repositories.Add crepo

    let cmainrepo = config.mainrepository
    cmainrepo.uri <- artifacts.MasterRepository.Url.toString

    config.ToString()

let Deserialize (content) =
    let convertToRepository (item : GlobalsConfig.mainrepository_Type) : Repository =
        { Url = RepositoryUrl.from (item.uri)
          Branch = None
          Name = RepositoryId.from Env.MASTER_REPO
          Vcs = VcsType.from item.vcs }

    let rec convertToBuildableRepositories (items : GlobalsConfig.repositories_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> let maybeBranch = if String.IsNullOrEmpty(x.branch) then None
                                         else x.branch |> BranchId.from |> Some
                       convertToBuildableRepositories tail |> Set.add { Repository = { Branch = maybeBranch
                                                                                       Url = RepositoryUrl.from (x.uri)
                                                                                       Name = RepositoryId.from x.repo
                                                                                       Vcs = VcsType.from x.vcs }
                                                                        Builder = BuilderType.from x.build
                                                                        Tester = TestRunnerType.from x.test }

    let convertToTestRunner (item : string) =
        TestRunnerType.from item

    let config = new GlobalsConfig()
    config.LoadText content

    let repos = convertToBuildableRepositories (config.repositories |> List.ofSeq)
    let mainRepo = convertToRepository (config.mainrepository)
    { MinVersion = config.minversion
      Binaries = config.binaries
      SideBySide = config.sxs
      MasterRepository = mainRepo
      Repositories = repos }

let Save (filename : FileInfo) (artifacts : Globals) =
    let content = Serialize artifacts
    File.WriteAllText(filename.FullName, content)

let Load (filename : FileInfo) : Globals =
    let content = File.ReadAllText (filename.FullName)
    Deserialize content
