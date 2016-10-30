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

module Commands.Workspace

open System.IO
open IoHelpers
open Env
open XmlHelpers
open System.Linq
open System.Xml.Linq
open Collections
open System
open Graph


let Create (createInfo : CLI.Commands.SetupWorkspace) =
    let wsDir = DirectoryInfo(createInfo.Path)
    wsDir.Create()
    if IsWorkspaceFolder wsDir then failwith "Workspace already exists"

    let currDir = Environment.CurrentDirectory
    try
        Environment.CurrentDirectory <- wsDir.FullName
        let graph = Graph.create createInfo.MasterRepository createInfo.MasterArtifacts createInfo.Type TestRunnerType.NUnit
        Tools.Vcs.Clone wsDir graph.MasterRepository true |> Exec.PrintOutput |> Exec.CheckResponseCode
        graph.Save()

        let baselineRepository = Baselines.from graph
        let baseline = baselineRepository.CreateBaseline false
        baseline.Save()

        // setup additional files for views to work correctly
        let installDir = Env.GetFolder Env.Folder.Installation
        let confDir = Env.GetFolder Env.Folder.Config
        let publishSource = installDir |> GetFile Env.FULLBUILD_TARGETS
        let publishTarget = confDir |> GetFile Env.FULLBUILD_TARGETS
        publishSource.CopyTo(publishTarget.FullName) |> ignore

        Tools.Vcs.Ignore wsDir graph.MasterRepository
        Tools.Vcs.Commit wsDir graph.MasterRepository "setup"
    finally
        Environment.CurrentDirectory <- currDir

let Init (initInfo : CLI.Commands.InitWorkspace) =
    let wsDir = DirectoryInfo(initInfo.Path)
    wsDir.Create()
    if IsWorkspaceFolder wsDir then
        printf "[WARNING] Workspace already exists - skipping"
    else
        let graph = Graph.init initInfo.MasterRepository initInfo.Type
        Tools.Vcs.Clone wsDir graph.MasterRepository true |> Exec.PrintOutput |> Exec.CheckResponseCode

let Push (pushInfo : CLI.Commands.PushWorkspace) =
    let graph = Configuration.LoadAnthology () |> Graph.from
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let allRepos = graph.Repositories
    let baselineRepository = Baselines.from graph
    let newBaseline = baselineRepository.CreateBaseline pushInfo.Incremental
    newBaseline.Save()

    // commit
    let mainRepo = graph.MasterRepository
    Try (fun () -> Tools.Vcs.Commit wsDir mainRepo "bookmark")

    // copy bin content
    let hash = Tools.Vcs.Tip wsDir mainRepo
    Core.BuildArtifacts.Publish graph pushInfo.Branch pushInfo.BuildNumber hash

let Checkout (checkoutInfo : CLI.Commands.CheckoutVersion) =
    // checkout repositories
    DisplayHighlight ".full-build"
    let graph = Configuration.LoadAnthology () |> Graph.from
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let mainRepo = graph.MasterRepository
    Tools.Vcs.Checkout wsDir mainRepo (Some checkoutInfo.Version) false

    // checkout each repository now
    let graph = Configuration.LoadAnthology () |> Graph.from
    let baselineRepository = Baselines.from graph
    let baseline = baselineRepository.Baseline
    let clonedRepos = graph.Repositories |> Set.filter (fun x -> x.IsCloned)
    for repo in clonedRepos do
        DisplayHighlight repo.Name
        let repoVersion = baseline.Bookmarks |> Seq.find (fun x -> x.Repository.Name = repo.Name)
        Tools.Vcs.Checkout wsDir repo (Some repoVersion.Version) false

    // update binaries with observable baseline
    Core.BuildArtifacts.PullReferenceBinaries graph checkoutInfo.Version

let Branch (branchInfo : CLI.Commands.BranchWorkspace) =
    // checkout repositories
    DisplayHighlight ".full-build"
    let graph = Configuration.LoadAnthology () |> Graph.from
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let mainRepo = graph.MasterRepository
    try
        Tools.Vcs.Checkout wsDir mainRepo branchInfo.Branch false
    with
        _ -> printfn "WARNING: No branch on .full-build repository. Is this intended ?"

    // checkout each repository now
    let graph = Configuration.LoadAnthology () |> Graph.from
    let clonedRepos = graph.Repositories |> Set.filter (fun x -> x.IsCloned)
    for repo in clonedRepos do
        DisplayHighlight repo.Name
        Tools.Vcs.Checkout wsDir repo branchInfo.Branch true

let Install () =
    Core.Package.RestorePackages ()
    Core.Conversion.GenerateProjectArtifacts()

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

let consoleLock = System.Object()

let printPull ((repo, execResult) : (Repository * Exec.ExecResult)) =
    lock consoleLock (fun () -> IoHelpers.DisplayHighlight repo.Name
                                execResult |> Exec.PrintOutput)

let private cloneRepo wsDir rebase (repo : Repository) = async {
    return (repo, Tools.Vcs.Pull wsDir repo rebase) |> printPull
}

let Pull (pullInfo : CLI.Commands.PullWorkspace) =
    let graph = Configuration.LoadAnthology () |> Graph.from
    let viewRepository = Views.from graph
    let wsDir = Env.GetFolder Env.Folder.Workspace

    // refresh graph just in case something has changed
    let graph = Configuration.LoadAnthology () |> Graph.from

    if pullInfo.Sources then
        graph.MasterRepository
            |> cloneRepo wsDir pullInfo.Rebase
            |> Async.RunSynchronously
            |> Exec.CheckResponseCode

        let selectedRepos = match pullInfo.View with
                             | None -> graph.Repositories
                             | Some viewName -> let view = viewName |> viewRepository.OpenView
                                                let repos = view.Projects |> Set.map (fun x -> x.Repository)
                                                repos
        let maxThrottle = pullInfo.Multithread ? (System.Environment.ProcessorCount*4, 1)

        let pullResults = selectedRepos
                            |> Seq.filter (fun x -> x.IsCloned)
                            |> Seq.map (cloneRepo wsDir pullInfo.Rebase)
                            |> Threading.throttle maxThrottle |> Async.Parallel |> Async.RunSynchronously

        pullResults |> Exec.CheckMultipleResponseCode

        Install ()

    if pullInfo.Bin then
        Core.BuildArtifacts.PullLatestReferenceBinaries graph

let Exec (execInfo : CLI.Commands.Exec) =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let execRepos = match execInfo.All with
                    | true -> graph.Repositories |> Set.add graph.MasterRepository
                    | _ -> graph.Repositories

    for repo in execRepos do
        let repoDir = wsDir |> GetSubDirectory repo.Name
        if repoDir.Exists then
            let vars = [ "FB_NAME", repo.Name
                         "FB_PATH", repoDir.FullName
                         "FB_URL", repo.Uri
                         "FB_WKS", wsDir.FullName ] |> Map.ofSeq
            let args = sprintf @"/c ""%s""" execInfo.Command

            try
                DisplayHighlight repo.Name

                if Env.IsMono () then Exec.Exec "sh" ("-c " + args) repoDir vars |> Exec.CheckResponseCode
                else Exec.Exec "cmd" args repoDir vars |> Exec.CheckResponseCode
            with e -> printfn "*** %s" e.Message

let Clean () =
    printfn "DANGER ! You will lose all uncommitted changes. Do you want to continue [Yes to confirm] ?"
    let res = Console.ReadLine()
    if res = "Yes" then
        // rollback master repository but save back the old anthology
        // if the cleanup fails we can still continue again this operation
        // master repository will be cleaned again as final step
        let oldGraph = Configuration.LoadAnthology () |> Graph.from
        let wsDir = Env.GetFolder Env.Folder.Workspace
        Tools.Vcs.Clean wsDir oldGraph.MasterRepository
        let newAntho = Configuration.LoadAnthology() |> Graph.from
        oldGraph.Save()

        // remove repositories
        let reposToRemove = Set.difference oldGraph.Repositories newAntho.Repositories
        for repo in reposToRemove do
            if repo.IsCloned then Tools.Vcs.Unclone wsDir repo

        // clean existing repositories
        for repo in newAntho.Repositories do
            if repo.IsCloned then
                DisplayHighlight repo.Name
                Tools.Vcs.Clean wsDir repo

        DisplayHighlight newAntho.MasterRepository.Name
        Tools.Vcs.Clean wsDir newAntho.MasterRepository

let UpdateGuid (updInfo : CLI.Commands.UpdateGuids) =
    printfn "DANGER ! You will change all project guids for selected repository. Do you want to continue [Yes to confirm] ?"
    let res = Console.ReadLine()
    if res = "Yes" then
        let graph = Configuration.LoadAnthology () |> Graph.from
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
    let graph = Configuration.LoadAnthology() |> Graph.from
    let baselineRepository = Baselines.from graph
    let baseline = baselineRepository.Baseline

    // newBaseline contains only cloned repositories
    // this means deltaBookmarks can contain non cloned repositories
    // before computing history, ensure repositories are correctly cloned
    let newBaseline = baselineRepository.CreateBaseline false
    let deltaBookmarks = baseline - newBaseline

    let wsDir = Env.GetFolder Env.Folder.Workspace

    // header
    let version = Tools.Vcs.Tip wsDir graph.MasterRepository

    // body
    let lastCommit = Tools.Vcs.LastCommit wsDir graph.MasterRepository "baseline"
    let revision = Tools.Vcs.Log wsDir graph.MasterRepository lastCommit.[0]

    let revisions = seq {
        // master repo
        match revision with
        | [] -> ()
        | _ -> yield graph.MasterRepository, revision

        // other repositories then
        for bookmark in deltaBookmarks do
            if bookmark.Repository.IsCloned then
                let revision = Tools.Vcs.Log wsDir bookmark.Repository bookmark.Version
                match revision with
                | [] -> ()
                | _ -> yield bookmark.Repository, revision
    }

    let histType = if historyInfo.Html then Generators.History.HistoryType.Html
                                       else Generators.History.HistoryType.Text

    Generators.History.Save histType version revisions

let Index (indexInfo : CLI.Commands.IndexRepositories) =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let repos = graph.Repositories |> Set.filter (fun x -> x.IsCloned)
    let selectedRepos = PatternMatching.FilterMatch repos (fun x -> x.Name) indexInfo.Filters
    if selectedRepos = Set.empty then printfn "WARNING: empty repository selection"

    selectedRepos |> Seq.iter (fun x -> IoHelpers.DisplayHighlight  x.Name)
    selectedRepos |> Core.Indexation.IndexWorkspace
                  |> Core.Indexation.Optimize
                  |> Core.Package.Simplify
                  |> Configuration.SaveAnthology

let Convert (convertInfo : CLI.Commands.ConvertRepositories) =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let repos = graph.Repositories |> Set.filter (fun x -> x.IsCloned)
    let selectedRepos = PatternMatching.FilterMatch repos (fun x -> x.Name) convertInfo.Filters
    if selectedRepos = Set.empty then printfn "WARNING: empty repository selection"

    selectedRepos |> Seq.iter (fun x -> IoHelpers.DisplayHighlight  x.Name)

    let builder2repos = repos |> Seq.groupBy (fun x -> x.Builder)
    for builder2repo in builder2repos do
        let (builder, repos) = builder2repo
        Core.Conversion.Convert builder (set repos)

    // setup additional files for views to work correctly
    let confDir = Env.GetFolder Env.Folder.Config
    let installDir = Env.GetFolder Env.Folder.Installation
    let publishSource = installDir |> GetFile Env.FULLBUILD_TARGETS
    let publishTarget = confDir |> GetFile Env.FULLBUILD_TARGETS
    publishSource.CopyTo(publishTarget.FullName, true) |> ignore

let CheckMinVersion () =
    try
        let fbVersion = Env.FullBuildVersion ()
        let graph = Configuration.LoadAnthology () |> Graph.from
        let minVersion = System.Version.Parse graph.MinVersion

        if fbVersion < minVersion then
            failwithf "Minimum full-build version requirement: %s" graph.MinVersion
    with
        // we are probably not in a workspace
        _ -> ()
