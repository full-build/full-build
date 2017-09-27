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

module AnthologySerializer

open Anthology
open System.IO
open System
open Collections
open StringHelpers

type private AnthologyConfig = FSharp.Configuration.YamlConfig<"Examples/anthology.yaml">


let Serialize (anthology : Anthology) =
    let config = new AnthologyConfig()
    // apps
    config.apps.Clear ()
    for app in anthology.Applications do
        let capp = AnthologyConfig.apps_Item_Type()
        capp.name <- app.Name.toString
        capp.``type`` <- app.Publisher.toString
        capp.project <- app.Project.toString
        config.apps.Add (capp)
    // projects
    config.projects.Clear()
    for project in anthology.Projects do
        let cproject = AnthologyConfig.projects_Item_Type()
        cproject.guid <- project.UniqueProjectId.toString
        cproject.out <- sprintf "%s.%s" project.Output.toString project.OutputType.toString
        cproject.file <- sprintf @"%s\%s" project.Repository.toString project.RelativeProjectFile.toString
        cproject.tests <- project.HasTests
        cproject.assemblies.Clear ()
        for assembly in project.AssemblyReferences do
            let cass = AnthologyConfig.projects_Item_Type.assemblies_Item_Type()
            cass.assembly <- assembly.toString
            cproject.assemblies.Add (cass)
        cproject.packages.Clear ()
        for package in project.PackageReferences do
            let cpackage = AnthologyConfig.projects_Item_Type.packages_Item_Type()
            cpackage.package <- package.toString
            cproject.packages.Add (cpackage)
        cproject.projects.Clear ()
        for project in project.ProjectReferences do
            let cprojectref = AnthologyConfig.projects_Item_Type.projects_Item_Type()
            cprojectref.project <- project.toString
            cproject.projects.Add (cprojectref)
        config.projects.Add cproject
    config.ToString()

let Deserialize (content) =
    let rec convertToApplications (items : AnthologyConfig.apps_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> let appName = ApplicationId.from x.name
                       let publishType = PublisherType.from x.``type``
                       let project = ProjectId.from x.project
                       let app = { Name = appName ; Publisher = publishType; Project = project }
                       convertToApplications tail |> Set.add app

    let rec convertToAssemblies (items : AnthologyConfig.projects_Item_Type.assemblies_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> convertToAssemblies tail |> Set.add (AssemblyId.from x.assembly)

    let rec convertToPackages (items : AnthologyConfig.projects_Item_Type.packages_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> convertToPackages tail |> Set.add (PackageId.from x.package)

    let rec convertToProjectRefs (items : AnthologyConfig.projects_Item_Type.projects_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> convertToProjectRefs tail |> Set.add (x.project |> ProjectId.from)

    let rec convertToProjects (items : AnthologyConfig.projects_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> let ext = FsHelpers.GetExtension (FileInfo(x.out))
                       let out = Path.GetFileNameWithoutExtension(x.out)
                       let repo = FsHelpers.GetRootDirectory (x.file)
                       let file = FsHelpers.GetFilewithoutRootDirectory (x.file)
                       let hastests = x.tests
                       convertToProjects tail |> Set.add  { Repository = RepositoryId.from repo
                                                            RelativeProjectFile = ProjectRelativeFile file
                                                            UniqueProjectId = ProjectUniqueId.from (ParseGuid x.guid)
                                                            ProjectId = ProjectId.from out
                                                            Output = AssemblyId.from out
                                                            OutputType = OutputType.from ext
                                                            HasTests = hastests
                                                            AssemblyReferences = convertToAssemblies (x.assemblies |> List.ofSeq)
                                                            PackageReferences = convertToPackages (x.packages |> List.ofSeq)
                                                            ProjectReferences = convertToProjectRefs (x.projects |> List.ofSeq) }

    let convertToTestRunner (item : string) =
        TestRunnerType.from item

    let config = new AnthologyConfig()
    config.LoadText content

    { Applications = convertToApplications (config.apps |> List.ofSeq)
      Projects = convertToProjects (config.projects |> List.ofSeq) }

let Save (filename : FileInfo) (anthology : Anthology) =
    let content = Serialize anthology
    File.WriteAllText(filename.FullName, content)

let Load (filename : FileInfo) : Anthology =
    let content = File.ReadAllText (filename.FullName)
    Deserialize content
