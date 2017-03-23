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

module ProjectsSerializer

open Anthology
open System.IO
open Collections
open StringHelpers


type private ProjectsConfig = FSharp.Configuration.YamlConfig<"Examples/projects.yaml">



let Deserialize (content) =
    let rec convertToAssemblies (items : ProjectsConfig.projects_Item_Type.assemblies_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> convertToAssemblies tail |> Set.add (AssemblyId.from x.assembly)

    let rec convertToPackages (items : ProjectsConfig.projects_Item_Type.packages_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> convertToPackages tail |> Set.add (PackageId.from x.package)

    let rec convertToProjectRefs (items : ProjectsConfig.projects_Item_Type.projects_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> convertToProjectRefs tail |> Set.add (x.project |> ProjectId.from)

    let rec convertToProjects (items : ProjectsConfig.projects_Item_Type list) =
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

    let config = new ProjectsConfig()
    config.LoadText content

    convertToProjects (config.projects |> List.ofSeq)

let Load (filename : FileInfo) : Project set =
    if filename.Exists then
        let content = File.ReadAllText (filename.FullName)
        Deserialize content
    else
        Set.empty
