﻿//   Copyright 2014-2016 Pierre Chalamet
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

module Workspace

open System.IO
open IoHelpers
open Env
open Vcs
open Anthology
open MsBuildHelpers
open System.Linq
open System.Xml.Linq
open Collections
open System


let private checkErrorCode err =
    if err <> 0 then failwithf "Process failed with error %d" err

let private checkedExecWithVars = 
    Exec.ExecWithVars checkErrorCode



let Create (path : string) (uri : RepositoryUrl) (bin : string) (vcsType : VcsType) : Unit = 
    let wsDir = DirectoryInfo(path)
    wsDir.Create()
    if IsWorkspaceFolder wsDir then failwith "Workspace already exists"

    let currDir = Environment.CurrentDirectory
    try
        Environment.CurrentDirectory <- wsDir.FullName
        let repo = { Name = RepositoryId.from Env.MASTER_REPO; Url = uri; Branch = None }

        let antho = { Artifacts = bin
                      NuGets = []
                      MasterRepository = repo
                      Repositories = Set.empty
                      Projects = Set.empty 
                      Applications = Set.empty 
                      Tester = TestRunnerType.NUnit 
                      Vcs = vcsType }
        VcsClone wsDir vcsType true repo

        let confDir = Env.GetFolder Env.Folder.Config
        let anthoFile = confDir |> GetFile Env.ANTHOLOGY_FILENAME
        AnthologySerializer.Save anthoFile antho

        let baseline = { Bookmarks = Set.empty }
        let baselineFile = confDir |> GetFile Env.BASELINE_FILENAME
        BaselineSerializer.Save baselineFile baseline

        // setup additional files for views to work correctly
        let installDir = Env.GetFolder Env.Folder.Installation
        let publishSource = installDir |> GetFile Env.FULLBUILD_TARGETS
        let publishTarget = confDir |> GetFile Env.FULLBUILD_TARGETS
        publishSource.CopyTo(publishTarget.FullName) |> ignore

        Vcs.VcsIgnore wsDir vcsType repo
        Vcs.VcsCommit wsDir vcsType repo "setup"
    finally
        Environment.CurrentDirectory <- currDir


let ClonedRepositories (wsDir : DirectoryInfo) (repos : BuildableRepository set) =
    repos |> Set.map (fun x -> x.Repository)    
          |> Set.filter (fun x -> let repoDir = wsDir |> GetSubDirectory x.Name.toString
                                  repoDir.Exists)


let Push (branch : string option) buildnum = 
    let antho = Configuration.LoadAnthology ()
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let allRepos = antho.Repositories
    let clonedRepos = allRepos |> ClonedRepositories wsDir
    let bookmarks = Repo.CollectRepoHash wsDir antho.Vcs clonedRepos
    let baseline = { Bookmarks = bookmarks }
    Configuration.SaveBaseline baseline

    let mainRepo = antho.MasterRepository

    // commit
    Try (fun () -> Vcs.VcsCommit wsDir antho.Vcs mainRepo "bookmark")

    // copy bin content
    let hash = Vcs.VcsTip wsDir antho.Vcs mainRepo
    BuildArtifacts.Publish branch buildnum hash

let Checkout (version : BookmarkVersion) =
    // checkout repositories
    DisplayHighlight ".full-build"
    let antho = Configuration.LoadAnthology ()
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let mainRepo = antho.MasterRepository
    Vcs.VcsCheckout wsDir antho.Vcs mainRepo (Some version) false

    // checkout each repository now
    let antho = Configuration.LoadAnthology ()
    let baseline = Configuration.LoadBaseline ()
    let clonedRepos = antho.Repositories |> ClonedRepositories wsDir
    for repo in clonedRepos do
        DisplayHighlight repo.Name.toString
        let repoVersion = baseline.Bookmarks |> Seq.tryFind (fun x -> x.Repository = repo.Name)
        match repoVersion with
        | Some x -> Vcs.VcsCheckout wsDir antho.Vcs repo (Some x.Version) false
        | None -> Vcs.VcsCheckout wsDir antho.Vcs repo None false

    // update binaries with observable baseline
    BuildArtifacts.PullReferenceBinaries version.toString

let Branch (branch : BookmarkVersion option) =
    // checkout repositories
    DisplayHighlight ".full-build"
    let antho = Configuration.LoadAnthology ()
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let mainRepo = antho.MasterRepository
    try
        Vcs.VcsCheckout wsDir antho.Vcs mainRepo branch false
    with
        _ -> printfn "WARNING: No branch on .full-build repository. Is this intended ?"

    // checkout each repository now
    let antho = Configuration.LoadAnthology ()
    let clonedRepos = antho.Repositories |> ClonedRepositories wsDir
    for repo in clonedRepos do
        DisplayHighlight repo.Name.toString
        let repoVer = match branch with
                      | None -> match repo.Branch with
                                | None -> None
                                | Some x -> Some (BookmarkVersion.from x.toString)
                      | Some x -> Some x
        Vcs.VcsCheckout wsDir antho.Vcs repo repoVer true


let Pull (src : bool) (bin : bool) (rebase : bool) (view : ViewId option) =
    let antho = Configuration.LoadAnthology ()
    let wsDir = Env.GetFolder Env.Folder.Workspace

    if src then
        let mainRepo = antho.MasterRepository
        DisplayHighlight mainRepo.Name.toString
        Vcs.VcsPull rebase wsDir antho.Vcs mainRepo

        let antho = Configuration.LoadAnthology ()
        let clonedRepos = match view with
                          | None -> antho.Repositories |> ClonedRepositories wsDir
                          | Some viewName -> let repos = Configuration.LoadView viewName
                                                         |> View.FindViewProjects
                                                         |> Set.map (fun x -> x.Repository)
                                             antho.Repositories |> Set.map (fun x -> x.Repository)
                                                                |> Set.filter (fun x -> repos |> Set.contains x.Name)

        for repo in clonedRepos do
            DisplayHighlight repo.Name.toString

            let repoDir = wsDir |> GetSubDirectory repo.Name.toString
            if repoDir.Exists then
                Vcs.VcsPull rebase wsDir antho.Vcs repo

    if bin then
        BuildArtifacts.PullLatestReferenceBinaries ()


let Init (path : string) (uri : RepositoryUrl) (vcsType : VcsType) : Unit = 
    let wsDir = DirectoryInfo(path)
    wsDir.Create()
    if IsWorkspaceFolder wsDir then 
        printf "[WARNING] Workspace already exists - skipping"
    else
        let repo = { Name = RepositoryId.from Env.MASTER_REPO; Url = uri; Branch = None }
        VcsClone wsDir vcsType true repo
   
let Exec cmd master =
    let antho = Configuration.LoadAnthology()
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let repos = antho.Repositories |> Set.map (fun x -> x.Repository)
    let execRepos = match master with
                    | true -> repos |> Set.add antho.MasterRepository 
                    | _ -> repos

    for repo in execRepos do
        let repoDir = wsDir |> GetSubDirectory repo.Name.toString
        if repoDir.Exists then
            let vars = [ "FB_NAME", repo.Name.toString
                         "FB_PATH", repoDir.FullName
                         "FB_URL", repo.Url.toLocalOrUrl 
                         "FB_WKS", wsDir.FullName ] |> Map.ofSeq
            let args = sprintf @"/c ""%s""" cmd

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
        let oldAntho = Configuration.LoadAnthology ()
        let wsDir = Env.GetFolder Env.Folder.Workspace
        Vcs.VcsClean wsDir oldAntho.Vcs oldAntho.MasterRepository
        let newAntho = Configuration.LoadAnthology()
        Configuration.SaveAnthology oldAntho
         
        // remove repositories
        let reposToRemove = Set.difference oldAntho.Repositories newAntho.Repositories
        for repo in reposToRemove do
            let repoDir = wsDir |> GetSubDirectory repo.Repository.Name.toString
            if repoDir.Exists then repoDir.Delete(true)

        // clean existing repositories
        for repo in newAntho.Repositories do
            let repoDir = wsDir |> GetSubDirectory repo.Repository.Name.toString
            if repoDir.Exists then
                DisplayHighlight repo.Repository.Name.toString
                Vcs.VcsClean wsDir newAntho.Vcs repo.Repository

        DisplayHighlight newAntho.MasterRepository.Name.toString
        Vcs.VcsClean wsDir newAntho.Vcs newAntho.MasterRepository

let UpdateGuid (repo : RepositoryId) =
    printfn "DANGER ! You will change all project guids for selected repository. Do you want to continue [Yes to confirm] ?"
    let res = Console.ReadLine()
    if res = "Yes" then
        let antho = Configuration.LoadAnthology ()
        let wsDir = Env.GetFolder Env.Folder.Workspace
        let repoDir = wsDir |> GetSubDirectory repo.toString
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


let History (html : bool) =
    let header = html ? (htmlHeader, textHeader)
    let body = html ? (htmlBody, textBody)
    let footer = html ? (htmlFooter, textFooter)

    let antho = Configuration.LoadAnthology()
    let baseline = Configuration.LoadBaseline()


    let wsDir = Env.GetFolder Env.Folder.Workspace

    // header
    let baselineTip = Vcs.VcsTip wsDir antho.Vcs antho.MasterRepository
    header baselineTip

    // body
    let lastCommit = Vcs.VcsLastCommit wsDir antho.Vcs antho.MasterRepository "baseline"
    match lastCommit with 
    | Some version -> let revision = Vcs.VcsLog wsDir antho.Vcs antho.MasterRepository version
                      if revision <> null then 
                          body antho.MasterRepository.Name.toString revision
    | _ -> ()

    for bookmark in baseline.Bookmarks do
        let repoDir = wsDir |> GetSubDirectory bookmark.Repository.toString
        if repoDir.Exists then
            let repo = antho.Repositories |> Seq.find (fun x -> x.Repository.Name = bookmark.Repository)
            let revision = Vcs.VcsLog wsDir antho.Vcs repo.Repository bookmark.Version
            if revision <> null then 
                body repo.Repository.Name.toString revision

    footer ()

let availableRepositories (filters : RepositoryId set) =
    let antho = Configuration.LoadAnthology()
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let selectedRepos = Repo.FilterRepos filters |> Set.map (fun x -> x.Repository.Name)
    let cloneRepos = antho.Repositories |> Set.filter (fun x -> selectedRepos |> Set.contains x.Repository.Name)
    cloneRepos |> Set.filter (fun x -> let subDir = wsDir |> GetSubDirectory x.Repository.Name.toString
                                       subDir.Exists)


let Index (filters : RepositoryId set) =    
    let repos = filters
                |> availableRepositories
    repos |> Seq.iter (fun x -> IoHelpers.DisplayHighlight  x.Repository.Name.toString)

    repos
        |> Set.map (fun x -> x.Repository)
        |> Indexation.IndexWorkspace 
        |> Indexation.Optimize
        |> Package.Simplify
        |> Configuration.SaveAnthology



let Install () =
    Package.RestorePackages ()
    Conversion.GenerateProjectArtifacts()

let Convert (filters : RepositoryId set) = 
    let repos = filters
                |> availableRepositories
    repos |> Seq.iter (fun x -> IoHelpers.DisplayHighlight  x.Repository.Name.toString)

    let builder2repos = repos |> Seq.groupBy (fun x -> x.Builder)
    for builder2repo in builder2repos do
        let (builder, brepos) = builder2repo
        let repos = brepos |> Seq.map (fun x -> x.Repository.Name) |> Set.ofSeq
        Conversion.Convert builder repos

    // setup additional files for views to work correctly
    let confDir = Env.GetFolder Env.Folder.Config
    let installDir = Env.GetFolder Env.Folder.Installation
    let publishSource = installDir |> GetFile Env.FULLBUILD_TARGETS
    let publishTarget = confDir |> GetFile Env.FULLBUILD_TARGETS
    publishSource.CopyTo(publishTarget.FullName, true) |> ignore
