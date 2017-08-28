//   Copyright 2014-2017 Pierre Chalamet
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

module Commands.Workspace

open System.IO
open FsHelpers
open Env
open XmlHelpers
open System.Linq
open System.Xml.Linq
open Collections
open System
open Graph

let pullMatchingBinaries () =
    let graph = Graph.load()
    let baselineFactory = Baselines.from graph
    match baselineFactory.FindMatchingBuildInfo() with 
    | Some buildInfo -> Core.BuildArtifacts.PullReferenceBinaries graph.ArtifactsDir buildInfo.BuildBranch (Some buildInfo.BuildNumber)
    | None -> printfn "No latest build found. The binaries were not pulled"
    

let Restore () =
    Core.Package.RestorePackages ()
    Core.Conversion.GenerateProjectArtifacts()


let private Install (nugets : string list) =
    nugets |> List.map Anthology.RepositoryUrl.from
           |> Core.Package.InstallPackages
    Core.Conversion.GenerateProjectArtifacts()


let Branch (branchInfo : CLI.Commands.BranchWorkspace) =
    let wsDir = Env.GetFolder Env.Folder.Workspace

    let switchToBranch (branch : string option) (repo : Repository) =
        async {
            let br = match branch with
                     | Some x -> x
                     | None -> repo.Branch
            return Tools.Vcs.Checkout wsDir repo br
                       |> IO.AndThen (fun () -> Tools.Vcs.Pull wsDir repo false) 
                       |> ConsoleHelpers.PrintOutput repo.Name
        }

    match branchInfo.Branch with
    | Some x -> let graph = Graph.load()
                let branch = (x = graph.MasterRepository.Branch) ? (None, Some x)
                let res1 = switchToBranch branch graph.MasterRepository |> Async.RunSynchronously

                let graph = Graph.load()
                let res2 = graph.Repositories |> Seq.filter (fun x -> x.IsCloned)
                                              |> Threading.ParExec (switchToBranch branch)
                let res = res1 |> Seq.singleton |> Seq.append res2
                if res |> Seq.exists (fun x -> x.Code <> 0) then
                    printfn "WARNING: failed to checkout some repositories"

                Configuration.SaveBranch x
                Restore()
                if branchInfo.Bin then pullMatchingBinaries ()
    | None -> let name = Configuration.LoadBranch()
              printfn "%s" name


let Create (createInfo : CLI.Commands.SetupWorkspace) =
    let wsDir = DirectoryInfo(createInfo.Path)
    wsDir.Create()
    if IsWorkspaceFolder wsDir then failwith "Workspace already exists"

    let currDir = Environment.CurrentDirectory
    try
        Environment.CurrentDirectory <- wsDir.FullName
        let graph = Graph.create createInfo.Type createInfo.MasterRepository createInfo.MasterArtifacts createInfo.SxS
        Tools.Vcs.Clone wsDir graph.MasterRepository true |> ConsoleHelpers.PrintOutput "Cloning master repository"
                                                          |> IO.CheckResponseCode
        graph.Save()

        Tools.Vcs.Ignore wsDir graph.MasterRepository

        Tools.Paket.UpdateSources List.empty
        Tools.Paket.PaketUpdate()

        // setup additional files for views to work correctly
        let installDir = Env.GetFolder Env.Folder.Installation
        let confDir = Env.GetFolder Env.Folder.Config
        let publishSource = installDir |> GetFile Env.FULLBUILD_TARGETS
        let publishTarget = confDir |> GetFile Env.FULLBUILD_TARGETS
        publishSource.CopyTo(publishTarget.FullName) |> ignore

        Configuration.SaveBranch graph.MasterRepository.Branch
    finally
        Environment.CurrentDirectory <- currDir

let Checkout (checkoutInfo : CLI.Commands.CheckoutVersion) =
    let tag = checkoutInfo.Version |> Baselines.BuildInfo.Parse
    
    // pull binaries
    let graph = Graph.load()
    Core.BuildArtifacts.PullReferenceBinaries graph.ArtifactsDir tag.BuildBranch (Some tag.BuildNumber)
    
    let graph = Graph.load()
    let baseline = Baselines.from graph
    let pulledBaseline = baseline.GetBaseline() |> Option.get

    let checkoutRepo wsDir (version : string) (repo : Repository) = async {
        let res = Tools.Vcs.Checkout wsDir repo version
        return res |> ConsoleHelpers.PrintOutput repo.Name
    }

    // checkout each repository now
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let branchResults = 
        pulledBaseline.Bookmarks
        |> Seq.filter (fun b -> b.Repository.IsCloned)
        |> Threading.ParExec (fun b -> checkoutRepo wsDir b.Version b.Repository)
    branchResults |> IO.CheckMultipleResponseCode

    Configuration.SaveBranch tag.BuildBranch
    Restore()
    


let Init (initInfo : CLI.Commands.InitWorkspace) =
    let wsDir = DirectoryInfo(initInfo.Path)
    wsDir.Create()
    if IsWorkspaceFolder wsDir then
        printf "[WARNING] Workspace already exists - skipping"
    else
        let graph = Graph.init initInfo.MasterRepository initInfo.Type
        Tools.Vcs.Clone wsDir graph.MasterRepository false |> ConsoleHelpers.PrintOutput "Cloning master repository"
                                                           |> IO.CheckResponseCode

        let currDir = Environment.CurrentDirectory
        try
            Environment.CurrentDirectory <- wsDir.FullName
            Configuration.SaveBranch graph.MasterRepository.Branch
        finally
            Environment.CurrentDirectory <- currDir



let consoleProgressBar max =
    MailboxProcessor.Start(fun inbox ->
        new String(' ', max) |> printf "[%s]\r["
        let rec loop n = async {
                let! msg = inbox.Receive()
                do printf "="
                if n = max then return printfn "]"
                return! n + 1 |> loop
            }
        loop 1)


let Pull (pullInfo : CLI.Commands.PullWorkspace) =
    let graph = Graph.load()
    let viewRepository = Views.from graph
    let wsDir = Env.GetFolder Env.Folder.Workspace

    // refresh graph just in case something has changed
    let graph = Graph.load()

    if pullInfo.Sources then
        let cloneRepo wsDir rebase (repo : Repository) = async {
            let res = Tools.Vcs.Pull wsDir repo rebase
            return res |> ConsoleHelpers.PrintOutput repo.Name
        }

        graph.MasterRepository
            |> cloneRepo wsDir pullInfo.Rebase
            |> Async.RunSynchronously
            |> IO.CheckResponseCode

        let selectedRepos = match pullInfo.View with
                             | None -> graph.Repositories
                             | Some viewName -> let view = viewRepository.Views |> Seq.find (fun x -> x.Name = viewName)
                                                let repos = view.Projects |> Set.map (fun x -> x.Repository)
                                                repos

        selectedRepos |> Seq.filter (fun x -> x.IsCloned)
                      |> Threading.ParExec (cloneRepo wsDir pullInfo.Rebase)
                      |> IO.CheckMultipleResponseCode
        Restore()

    if pullInfo.Bin then
        pullMatchingBinaries ()


let Exec (execInfo : CLI.Commands.Exec) =
    let branch = Configuration.LoadBranch()
    let graph = Graph.load()
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let execRepos = match execInfo.All with
                    | true -> graph.Repositories |> Set.add graph.MasterRepository
                    | _ -> graph.Repositories

    for repo in execRepos do
        let repoDir = wsDir |> GetSubDirectory repo.Name
        if repoDir.Exists then
            let vars = [ "FB_REPO_NAME", repo.Name
                         "FB_REPO_PATH", repoDir.FullName
                         "FB_REPO_URL", repo.Uri
                         "FB_REPO_BRANCH", repo.Branch
                         "FB_BRANCH", branch
                         "FB_WKS", wsDir.FullName ] |> Map.ofSeq
            let args = sprintf @"/c ""%s""" execInfo.Command

            try
                ConsoleHelpers.DisplayInfo repo.Name

                if Env.IsMono () then Exec.Exec "sh" ("-c " + args) repoDir vars |> IO.CheckResponseCode
                else Exec.Exec "cmd" args repoDir vars |> IO.CheckResponseCode
            with e -> printfn "*** %s" e.Message

let Clean () =
    printfn "DANGER ! You will lose all uncommitted changes. Do you want to continue [Yes to confirm] ?"
    let res = Console.ReadLine()
    if res = "Yes" then
        // rollback master repository but save back the old anthology
        // if the cleanup fails we can still continue again this operation
        // master repository will be cleaned again as final step
        let oldGraph = Graph.load()
        let wsDir = Env.GetFolder Env.Folder.Workspace
        Tools.Vcs.Clean wsDir oldGraph.MasterRepository
        let newGraph = Graph.load()
        oldGraph.Save()

        // remove repositories
        let reposToRemove = Set.difference oldGraph.Repositories newGraph.Repositories
        reposToRemove |> Seq.filter (fun x -> x.IsCloned)
                      |> Seq.iter (Tools.Vcs.Unclone wsDir)

        // clean existing repositories
        for repo in newGraph.Repositories do
            if repo.IsCloned then
                ConsoleHelpers.DisplayInfo repo.Name
                Tools.Vcs.Clean wsDir repo

        ConsoleHelpers.DisplayInfo newGraph.MasterRepository.Name
        Tools.Vcs.Clean wsDir newGraph.MasterRepository

let UpdateGuid (updInfo : CLI.Commands.UpdateGuids) =
    printfn "DANGER ! You will change all project guids for selected repository. Do you want to continue [Yes to confirm] ?"
    let res = Console.ReadLine()
    if res = "Yes" then
        let graph = Graph.load()
        let wsDir = Env.GetFolder Env.Folder.Workspace
        let selectedProjects = PatternMatching.FilterMatch (graph.Projects) (fun x -> sprintf "%s/%s" x.Repository.Name x.Output.Name) updInfo.Filters
        let projects = selectedProjects |> Set.filter (fun x -> x.Repository.IsCloned)
        for project in projects do
            let prjFile = wsDir |> GetFile project.ProjectFile
            let xdoc = XDocument.Load(prjFile.FullName)
            let xguid = xdoc.Descendants(NsMsBuild + "ProjectGuid").Single()
            let newGuid = Guid.NewGuid()
            xguid.Value <- newGuid.ToString("B")
            xdoc.Save(prjFile.FullName)

let History (historyInfo : CLI.Commands.History) =
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let graph = Graph.load()
    let baselineRepository = Baselines.from graph
    
    let diff = 
        let baseline = baselineRepository.GetSourcesBaseline() 
        match baselineRepository.GetBaseline () with 
        | Some previousBaseline -> previousBaseline - baseline
        | None -> baseline.Bookmarks

    let revisions = seq {
        // other repositories then
        for bookmark in diff do
            if bookmark.Repository.IsCloned then
                let revision = Tools.Vcs.Log wsDir bookmark.Repository bookmark.Version
                match revision with
                | [] -> ()
                | _ -> yield bookmark.Repository, revision
    }

    let histType = if historyInfo.Html then Generators.History.HistoryType.Html
                                       else Generators.History.HistoryType.Text

    Generators.History.Save histType revisions

let private index (convertInfo : CLI.Commands.ConvertRepositories) =
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let graph = Graph.load()
    let antho = graph.Anthology
    let globals = graph.Globals
    let repos = graph.Repositories |> Set.filter (fun x -> x.IsCloned)
    let selectedRepos = PatternMatching.FilterMatch repos (fun x -> x.Name) convertInfo.Filters
    if selectedRepos = Set.empty then failwith "Empty repository selection"

    let indexation = selectedRepos |> Core.Indexation.IndexWorkspace wsDir globals antho
    if convertInfo.Check then
        indexation |> fst |> Graph.from globals |> ignore
        indexation |> fst |> Core.Indexation.CheckAnthologyProjectsInRepository antho selectedRepos
    else
        indexation |> Core.Indexation.UpdatePackages globals
                   |> Core.Package.Simplify
                   |> Core.Indexation.SaveAnthologyProjectsInRepository antho selectedRepos
                   |> Configuration.SaveAnthology
        Install graph.NuGets

let convert (convertInfo : CLI.Commands.ConvertRepositories) =
    let graph = Graph.load()
    let repos = graph.Repositories |> Set.filter (fun x -> x.IsCloned) |> Set.filter (fun x -> x.Builder = BuilderType.MSBuild)
    let selectedRepos = PatternMatching.FilterMatch repos (fun x -> x.Name) convertInfo.Filters
    if selectedRepos = Set.empty then failwith "Empty repository selection"

    let builder2repos = selectedRepos |> Seq.groupBy (fun x -> x.Builder)
    for builder2repo in builder2repos do
        let (builder, repos) = builder2repo
        for repo in repos do
            ConsoleHelpers.DisplayInfo ("converting "+ repo.Name)
            Core.Conversion.Convert builder (Set.singleton repo) graph.SideBySide

    // setup additional files for views to work correctly
    let confDir = Env.GetFolder Env.Folder.Config
    let installDir = Env.GetFolder Env.Folder.Installation
    let publishSource = installDir |> GetFile Env.FULLBUILD_TARGETS
    let publishTarget = confDir |> GetFile Env.FULLBUILD_TARGETS
    publishSource.CopyTo(publishTarget.FullName, true) |> ignore


let Convert (convertInfo : CLI.Commands.ConvertRepositories) =
    if convertInfo.Reset |> not then
        convertInfo |> index
    if convertInfo.Check |> not then
        convertInfo |> convert


let CheckMinVersion () =
    try
        let fbVersion = Env.FullBuildVersion ()
        let artifacts = Configuration.LoadGlobals()
        let minVersion = System.Version.Parse artifacts.MinVersion

        if fbVersion < minVersion then
            failwithf "Minimum full-build version requirement: %s" artifacts.MinVersion
    with
        // we are probably not in a workspace
        _ -> ()


let Push (pushInfo : CLI.Commands.PushWorkspace) =
    let graph = Graph.load()
    let comment = pushInfo.Incremental ? ("incremental", "full")
    let baselineFactory = Baselines.from graph

    // copy bin content
    graph.Anthology |> Configuration.SaveConsolidatedAnthology    
    baselineFactory.UpdateBaseline pushInfo.Version
    Core.BuildArtifacts.Publish graph 

    // tag master repository
    baselineFactory.TagMasterRepository pushInfo.Version comment 
    
    // print tag information
    printfn "[pushed version] %s" pushInfo.Version


let Doctor () =
    if Doctor.Check() then failwith "Doctor found something wrong !"
    else printfn "Doctor says everything is alright !"
