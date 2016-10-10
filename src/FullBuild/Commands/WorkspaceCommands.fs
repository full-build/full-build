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

module WorkspaceCommands

open System.IO
open IoHelpers
open Env
open MsBuildHelpers
open System.Linq
open System.Xml.Linq
open Collections
open System
open Graph


let private checkErrorCode err =
    if err <> 0 then failwithf "Process failed with error %d" err

let private checkedExecWithVars =
    Exec.ExecWithVars checkErrorCode



let Create (createInfo : Commands.SetupWorkspace) =
    let wsDir = DirectoryInfo(createInfo.Path)
    wsDir.Create()
    if IsWorkspaceFolder wsDir then failwith "Workspace already exists"

    let currDir = Environment.CurrentDirectory
    try
        Environment.CurrentDirectory <- wsDir.FullName
        let graph = Graph.create createInfo.MasterRepository createInfo.MasterArtifacts createInfo.Type TestRunnerType.NUnit
        Plumbing.Vcs.Clone wsDir graph.MasterRepository true
        graph.Save()

        let baseline = graph.CreateBaseline false
        baseline.Save()

        // setup additional files for views to work correctly
        let installDir = Env.GetFolder Env.Folder.Installation
        let confDir = Env.GetFolder Env.Folder.Config
        let publishSource = installDir |> GetFile Env.FULLBUILD_TARGETS
        let publishTarget = confDir |> GetFile Env.FULLBUILD_TARGETS
        publishSource.CopyTo(publishTarget.FullName) |> ignore

        Plumbing.Vcs.Ignore wsDir graph.MasterRepository
        Plumbing.Vcs.Commit wsDir graph.MasterRepository "setup"
    finally
        Environment.CurrentDirectory <- currDir



let Init (initInfo : Commands.InitWorkspace) =
    let wsDir = DirectoryInfo(initInfo.Path)
    wsDir.Create()
    if IsWorkspaceFolder wsDir then
        printf "[WARNING] Workspace already exists - skipping"
    else
        let graph = Graph.init initInfo.MasterRepository initInfo.Type
        Plumbing.Vcs.Clone wsDir graph.MasterRepository true


let Push (pushInfo : Commands.PushWorkspace) =
    let graph = Configuration.LoadAnthology () |> Graph.from
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let allRepos = graph.Repositories
    let newBaseline = graph.CreateBaseline pushInfo.Incremental
    newBaseline.Save()

    // commit
    let mainRepo = graph.MasterRepository
    Try (fun () -> Plumbing.Vcs.Commit wsDir mainRepo "bookmark")

    // copy bin content
    let hash = Plumbing.Vcs.Tip wsDir mainRepo
    Plumbing.BuildArtifacts.Publish pushInfo.Branch pushInfo.BuildNumber hash

let Checkout (checkoutInfo : Commands.CheckoutVersion) =
    // checkout repositories
    DisplayHighlight ".full-build"
    let graph = Configuration.LoadAnthology () |> Graph.from
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let mainRepo = graph.MasterRepository
    Plumbing.Vcs.Checkout wsDir mainRepo (Some checkoutInfo.Version) false

    // checkout each repository now
    let graph = Configuration.LoadAnthology () |> Graph.from
    let baseline = graph.Baseline
    let clonedRepos = graph.Repositories |> Set.filter (fun x -> x.IsCloned)
    for repo in clonedRepos do
        DisplayHighlight repo.Name
        let repoVersion = baseline.Bookmarks |> Seq.find (fun x -> x.Repository.Name = repo.Name)
        Plumbing.Vcs.Checkout wsDir repo (Some repoVersion.Version) false

    // update binaries with observable baseline
    Plumbing.BuildArtifacts.PullReferenceBinaries checkoutInfo.Version

let Branch (branchInfo : Commands.BranchWorkspace) =
    // checkout repositories
    DisplayHighlight ".full-build"
    let graph = Configuration.LoadAnthology () |> Graph.from
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let mainRepo = graph.MasterRepository
    try
        Plumbing.Vcs.Checkout wsDir mainRepo branchInfo.Branch false
    with
        _ -> printfn "WARNING: No branch on .full-build repository. Is this intended ?"

    // checkout each repository now
    let graph = Configuration.LoadAnthology () |> Graph.from
    let clonedRepos = graph.Repositories |> Set.filter (fun x -> x.IsCloned)
    for repo in clonedRepos do
        DisplayHighlight repo.Name
        Plumbing.Vcs.Checkout wsDir repo branchInfo.Branch true


let Install () =
    Package.RestorePackages ()
    Conversion.GenerateProjectArtifacts()


let Pull (pullInfo : Commands.PullWorkspace) =
    let graph = Configuration.LoadAnthology () |> Graph.from
    let wsDir = Env.GetFolder Env.Folder.Workspace

    if pullInfo.Src then
        let mainRepo = graph.MasterRepository
        DisplayHighlight mainRepo.Name
        Plumbing.Vcs.Pull wsDir mainRepo pullInfo.Rebase

        let clonedRepos = match pullInfo.View with
                          | None -> graph.Repositories |> Seq.filter (fun x -> x.IsCloned)
                          | Some viewName -> let view = graph.Views |> Seq.find (fun x -> x.Name = viewName)
                                             let repos = view.Projects |> Seq.map (fun x -> x.Repository)
                                                                       |> Seq.filter (fun x -> x.IsCloned)
                                             repos

        for repo in clonedRepos do
            DisplayHighlight repo.Name
            Plumbing.Vcs.Pull wsDir repo pullInfo.Rebase

        Install ()

    if pullInfo.Bin then
        Plumbing.BuildArtifacts.PullLatestReferenceBinaries ()


let Exec (execInfo : Commands.Exec) =
    let antho = Configuration.LoadAnthology()
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let repos = antho.Repositories |> Set.map (fun x -> x.Repository)
    let execRepos = match execInfo.All with
                    | true -> repos |> Set.add antho.MasterRepository
                    | _ -> repos

    for repo in execRepos do
        let repoDir = wsDir |> GetSubDirectory repo.Name.toString
        if repoDir.Exists then
            let vars = [ "FB_NAME", repo.Name.toString
                         "FB_PATH", repoDir.FullName
                         "FB_URL", repo.Url.toLocalOrUrl
                         "FB_WKS", wsDir.FullName ] |> Map.ofSeq
            let args = sprintf @"/c ""%s""" execInfo.Command

            try
                DisplayHighlight repo.Name.toString

                if Env.IsMono () then checkedExecWithVars "sh" ("-c " + args) repoDir vars
                else checkedExecWithVars "cmd" args repoDir vars
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
        Plumbing.Vcs.Clean wsDir oldGraph.MasterRepository
        let newAntho = Configuration.LoadAnthology() |> Graph.from
        oldGraph.Save()

        // remove repositories
        let reposToRemove = Set.difference oldGraph.Repositories newAntho.Repositories
        for repo in reposToRemove do
            if repo.IsCloned then Plumbing.Vcs.Unclone wsDir repo

        // clean existing repositories
        for repo in newAntho.Repositories do
            if repo.IsCloned then
                DisplayHighlight repo.Name
                Plumbing.Vcs.Clean wsDir repo

        DisplayHighlight newAntho.MasterRepository.Name
        Plumbing.Vcs.Clean wsDir newAntho.MasterRepository

let UpdateGuid (repo : string) =
    printfn "DANGER ! You will change all project guids for selected repository. Do you want to continue [Yes to confirm] ?"
    let res = Console.ReadLine()
    if res = "Yes" then
        let antho = Configuration.LoadAnthology ()
        let wsDir = Env.GetFolder Env.Folder.Workspace
        let repoDir = wsDir |> GetSubDirectory repo
        let projects = IoHelpers.FindKnownProjects repoDir
        for project in projects do
            let xdoc = XDocument.Load(project.FullName)
            let guid = xdoc.Descendants(NsMsBuild + "ProjectGuid").Single()
            guid.Value <- Guid.NewGuid().ToString("B")
            xdoc.Save(project.FullName)


let textHeader (version : string) =
    ()

let textFooter () =
    ()

let htmlHeader (version : string) =
    printfn "<html>"
    printfn "<body>"
    printfn "<h2>version %s</h2>" version

let htmlFooter () =
    printfn "</body>"

let textBody (repo : string) (content : string) =
    DisplayHighlight repo
    printfn "%s" content

let htmlBody (repo : string) (content : string) =
    printfn "<b>%s</b><br>" repo
    let htmlContent = content.Replace(System.Environment.NewLine, "<br>")
    printfn "%s<br><br>" htmlContent


let History (historyInfo : Commands.History) =
    let header = historyInfo.Html ? (htmlHeader, textHeader)
    let body = historyInfo.Html ? (htmlBody, textBody)
    let footer = historyInfo.Html ? (htmlFooter, textFooter)

    let graph = Configuration.LoadAnthology() |> Graph.from
    let baseline = graph.Baseline

    let wsDir = Env.GetFolder Env.Folder.Workspace

    // header
    let baselineTip = Plumbing.Vcs.Tip wsDir graph.MasterRepository
    header baselineTip

    // body
    let lastCommit = Plumbing.Vcs.LastCommit wsDir graph.MasterRepository "baseline"
    let revision = Plumbing.Vcs.Log wsDir graph.MasterRepository lastCommit
    body graph.MasterRepository.Name revision

    for bookmark in baseline.Bookmarks do
        if bookmark.Repository.IsCloned then
            let revision = Plumbing.Vcs.Log wsDir bookmark.Repository bookmark.Version
            body bookmark.Repository.Name revision

    footer ()

let Index (indexInfo : Commands.IndexRepositories) =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let repos = graph.Repositories |> Set.filter (fun x -> x.IsCloned)
    let selectedRepos = PatternMatching.FilterMatch repos (fun x -> x.Name) indexInfo.Filters
    selectedRepos |> Seq.iter (fun x -> IoHelpers.DisplayHighlight  x.Name)
    selectedRepos |> Indexation.IndexWorkspace
                  |> Indexation.Optimize
                  |> Package.Simplify
                  |> Configuration.SaveAnthology

let Convert (convertInfo : Commands.ConvertRepositories) =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let repos = graph.Repositories |> Set.filter (fun x -> x.IsCloned)
    let selectedRepos = PatternMatching.FilterMatch repos (fun x -> x.Name) convertInfo.Filters
    selectedRepos |> Seq.iter (fun x -> IoHelpers.DisplayHighlight  x.Name)

    let builder2repos = repos |> Seq.groupBy (fun x -> x.Builder)
    for builder2repo in builder2repos do
        let (builder, repos) = builder2repo
        Conversion.Convert builder (set repos)

    // setup additional files for views to work correctly
    let confDir = Env.GetFolder Env.Folder.Config
    let installDir = Env.GetFolder Env.Folder.Installation
    let publishSource = installDir |> GetFile Env.FULLBUILD_TARGETS
    let publishTarget = confDir |> GetFile Env.FULLBUILD_TARGETS
    publishSource.CopyTo(publishTarget.FullName, true) |> ignore
