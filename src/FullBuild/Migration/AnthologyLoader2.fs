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

module Migration.AnthologyLoader2

open Anthology
open System.IO
open System
open Collections
open StringHelpers

type private AnthologyConfig = FSharp.Configuration.YamlConfig<"Examples/anthology.yaml">

let Deserialize (content) =
    let rec convertToNuGets (items : AnthologyConfig.anthology_Type.nugets_Item_Type list) =
        match items with
        | [] -> []
        | x :: tail -> (RepositoryUrl.from (x.nuget)) :: convertToNuGets tail

    let convertToRepository (item : AnthologyConfig.anthology_Type.mainrepository_Type) : Repository =
        { Url = RepositoryUrl.from (item.uri)
          Branch = None
          Name = RepositoryId.from Env.MASTER_REPO }

    let rec convertToBuildableRepositories (items : AnthologyConfig.anthology_Type.repositories_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> let maybeBranch = if String.IsNullOrEmpty(x.branch) then None
                                         else x.branch |> BranchId.from |> Some
                       convertToBuildableRepositories tail |> Set.add { Repository = { Branch = maybeBranch
                                                                                       Url = RepositoryUrl.from (x.uri)
                                                                                       Name = RepositoryId.from x.repo }
                                                                        Builder = BuilderType.from x.build }

    let rec convertToAssemblies (items : AnthologyConfig.anthology_Type.projects_Item_Type.assemblies_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> convertToAssemblies tail |> Set.add (AssemblyId.from x.assembly)

    let rec convertToPackages (items : AnthologyConfig.anthology_Type.projects_Item_Type.packages_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> convertToPackages tail |> Set.add (PackageId.from x.package)

    let rec convertToProjectRefs (items : AnthologyConfig.anthology_Type.projects_Item_Type.projects_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> convertToProjectRefs tail |> Set.add (x.project |> ProjectId.from)

    let rec convertToProjects (items : AnthologyConfig.anthology_Type.projects_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> let ext = IoHelpers.GetExtension (FileInfo(x.out))
                       let out = Path.GetFileNameWithoutExtension(x.out)
                       let repo = IoHelpers.GetRootDirectory (x.file)
                       let file = IoHelpers.GetFilewithoutRootDirectory (x.file)
                       let hastests = x.tests
                       convertToProjects tail |> Set.add  { Repository = RepositoryId.from repo
                                                            RelativeProjectFile = ProjectRelativeFile file
                                                            UniqueProjectId = ProjectUniqueId.from (ParseGuid x.guid)
                                                            ProjectId = ProjectId.from out
                                                            Output = AssemblyId.from out
                                                            OutputType = OutputType.from ext
                                                            FxVersion = FxInfo.from x.fx.version
                                                            FxProfile = FxInfo.from x.fx.profile
                                                            FxIdentifier = FxInfo.from x.fx.identifier
                                                            HasTests = hastests
                                                            AssemblyReferences = convertToAssemblies (x.assemblies |> List.ofSeq)
                                                            PackageReferences = convertToPackages (x.packages |> List.ofSeq)
                                                            ProjectReferences = convertToProjectRefs (x.projects |> List.ofSeq) }

    let rec convertToApplications (items : AnthologyConfig.anthology_Type.apps_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> let appName = ApplicationId.from x.name
                       let publishType = PublisherType.from x.``type``
                       let projects = x.projects |> Seq.map (fun x -> ProjectId.from x.project) |> Set.ofSeq
                       let app = { Name = appName ; Publisher = publishType; Projects = projects }
                       convertToApplications tail |> Set.add app

    let convertToTestRunner (item : string) =
        TestRunnerType.from item

    let convertToBuilder (item : string) =
        BuilderType.from item

    let config = new AnthologyConfig()
    config.LoadText content

    let repos = convertToBuildableRepositories (config.anthology.repositories |> List.ofSeq)
    let mainRepo = convertToRepository (config.anthology.mainrepository)
    { MinVersion = config.anthology.minversion
      Binaries = config.anthology.artifacts
      Vcs = VcsType.from config.anthology.vcs
      NuGets = convertToNuGets (config.anthology.nugets |> List.ofSeq)
      MasterRepository = mainRepo
      Repositories = repos
      Projects = convertToProjects (config.anthology.projects |> List.ofSeq)
      Applications = convertToApplications (config.anthology.apps |> List.ofSeq)
      Tester = convertToTestRunner (config.anthology.test) }

let Load (filename : FileInfo) : Anthology =
    let content = File.ReadAllText (filename.FullName)
    Deserialize content
