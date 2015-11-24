module AnthologySerializer

open Anthology
open System.IO
open System
open Collections
open StringHelpers

type private AnthologyConfig = FSharp.Configuration.YamlConfig<"anthology.yaml">


let Serialize (antho : Anthology) =
    let config = new AnthologyConfig()
    config.anthology.artifacts <- antho.Artifacts

    config.anthology.nugets.Clear()
    for nuget in antho.NuGets do
        let cnuget = AnthologyConfig.anthology_Type.nugets_Item_Type()
        cnuget.nuget <- Uri(nuget.toString)
        config.anthology.nugets.Add (cnuget)

    config.anthology.repositories.Clear()
    let repos = antho.Repositories |> Set.add antho.MasterRepository
    for repo in repos do
        let crepo = AnthologyConfig.anthology_Type.repositories_Item_Type()
        crepo.repo <- repo.Name.toString
        crepo.``type`` <- repo.Vcs.toString
        crepo.uri <- Uri (repo.Url.toString)
        config.anthology.repositories.Add crepo

    config.anthology.projects.Clear()
    for project in antho.Projects do
        let cproject = AnthologyConfig.anthology_Type.projects_Item_Type()
        cproject.guid <- project.UniqueProjectId.toString
        cproject.fx <- project.FxTarget.toString
        cproject.out <- sprintf "%s.%s" project.Output.toString project.OutputType.toString
        cproject.file <- sprintf "%s/%s" project.Repository.toString project.RelativeProjectFile.toString
        cproject.assemblies.Clear ()
        for assembly in project.AssemblyReferences do
            let cass = AnthologyConfig.anthology_Type.projects_Item_Type.assemblies_Item_Type()
            cass.assembly <- assembly.toString
            cproject.assemblies.Add (cass)
        cproject.packages.Clear ()
        for package in project.PackageReferences do
            let cpackage = AnthologyConfig.anthology_Type.projects_Item_Type.packages_Item_Type()
            cpackage.package <- package.toString
            cproject.packages.Add (cpackage)
        cproject.projects.Clear ()
        for project in project.ProjectReferences do
            let cprojectref = AnthologyConfig.anthology_Type.projects_Item_Type.projects_Item_Type()
            cprojectref.project <- project.toString
            cproject.projects.Add (cprojectref)
        config.anthology.projects.Add cproject

    config.anthology.apps.Clear ()
    for app in antho.Applications do
        let capp = AnthologyConfig.anthology_Type.apps_Item_Type()
        capp.name <- app.Name.toString
        capp.``type`` <- app.Publisher.toString
        capp.projects.Clear ()
        for project in app.Projects do
            let cproject = AnthologyConfig.anthology_Type.apps_Item_Type.projects_Item_Type()
            cproject.project <- project.toString
            capp.projects.Add (cproject)
        config.anthology.apps.Add (capp)

    config.anthology.tests.Clear ()
    for testRunner in antho.TestRunners do
        let crunner = AnthologyConfig.anthology_Type.tests_Item_Type ()
        crunner.name <- testRunner.toString
        config.anthology.tests.Add (crunner)

    config.ToString()

let Deserialize (content) =
    let rec convertToNuGets (items : AnthologyConfig.anthology_Type.nugets_Item_Type list) =
        match items with
        | [] -> List.empty
        | x :: tail -> (RepositoryUrl.from (x.nuget)) :: convertToNuGets tail

    let rec convertToRepositories (items : AnthologyConfig.anthology_Type.repositories_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> convertToRepositories tail |> Set.add { Name=RepositoryId.from x.repo; Vcs=VcsType.from x.``type``; Url=RepositoryUrl.from (x.uri)}

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
                       convertToProjects tail |> Set.add  { Repository = RepositoryId.from repo
                                                            RelativeProjectFile = ProjectRelativeFile file
                                                            UniqueProjectId = ProjectUniqueId.from (ParseGuid x.guid)
                                                            ProjectId = ProjectId.from out
                                                            Output = AssemblyId.from out
                                                            OutputType = OutputType.from ext
                                                            FxTarget = FrameworkVersion x.fx
                                                            AssemblyReferences = convertToAssemblies (x.assemblies |> List.ofSeq)
                                                            PackageReferences = convertToPackages (x.packages |> List.ofSeq)
                                                            ProjectReferences = convertToProjectRefs (x.projects |> List.ofSeq) }

    let rec convertToApplicationDependencies (items : AnthologyConfig.anthology_Type.apps_Item_Type.projects_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> let projectId = x.project |> ProjectId.from
                       convertToApplicationDependencies tail |> Set.add projectId

    let rec convertToApplications (items : AnthologyConfig.anthology_Type.apps_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> let appName = ApplicationId.from x.name
                       let publishType = PublisherType.from x.``type``
                       let projects = convertToApplicationDependencies (x.projects |> List.ofSeq)
                       let app = { Name = appName ; Publisher = publishType; Projects = projects }
                       convertToApplications tail |> Set.add app

    let rec convertToTestRunners (items : AnthologyConfig.anthology_Type.tests_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> let runner = TestRunner.from x.name
                       convertToTestRunners tail |> Set.add runner

    let config = new AnthologyConfig()
    config.LoadText content

    let repos = convertToRepositories (config.anthology.repositories |> List.ofSeq)
    let masterRepo = repos |> Seq.find (fun x -> x.Name = RepositoryId.from Env.MASTER_REPO)
    let otherRepos = Set.remove masterRepo repos
    { Artifacts = config.anthology.artifacts
      NuGets = convertToNuGets (config.anthology.nugets |> List.ofSeq)
      MasterRepository = masterRepo
      Repositories = otherRepos
      Projects = convertToProjects (config.anthology.projects |> List.ofSeq) 
      Applications = convertToApplications (config.anthology.apps |> List.ofSeq) 
      TestRunners = convertToTestRunners (config.anthology.tests |> List.ofSeq) }

let Save (filename : FileInfo) (antho : Anthology) =
    let content = Serialize antho
    File.WriteAllText(filename.FullName, content)

let Load (filename : FileInfo) : Anthology =
    let content = File.ReadAllText (filename.FullName)
    Deserialize content
