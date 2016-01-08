//   Copyright 2014-2015 Pierre Chalamet
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



let Create (path : string) (uri : RepositoryUrl) (bin : string) = 
    let wsDir = DirectoryInfo(path)
    wsDir.Create()
    if IsWorkspaceFolder wsDir then failwith "Workspace already exists"
    let vcsType = Vcs.VcsDetermineType uri
    let repo = { Name = RepositoryId.from Env.MASTER_REPO; Url = uri; Vcs=vcsType; Branch = None }
    VcsClone wsDir true repo

    let antho = { Artifacts = bin
                  NuGets = []
                  MasterRepository = repo
                  Repositories = Set.empty
                  Projects = Set.empty 
                  Applications = Set.empty 
                  Tester = TestRunnerType.NUnit 
                  Builder = BuilderType.MSBuild }
    let confDir = Env.GetFolder Env.Config
    let anthoFile = confDir |> GetFile Env.ANTHOLOGY_FILENAME
    AnthologySerializer.Save anthoFile antho

    let baseline = { Bookmarks = Set.empty }
    let baselineFile = confDir |> GetFile Env.BASELINE_FILENAME
    BaselineSerializer.Save baselineFile baseline

    // setup additional files for views to work correctly
    let installDir = Env.GetFolder Env.Installation
    let publishSource = installDir |> GetFile Env.FULLBUILD_TARGETS
    let publishTarget = confDir |> GetFile Env.FULLBUILD_TARGETS
    publishSource.CopyTo(publishTarget.FullName) |> ignore

    Vcs.VcsIgnore wsDir repo
    Vcs.VcsCommit wsDir repo "setup"



let XDocumentLoader (fileName : FileInfo) =
    XDocument.Load fileName.FullName

let XDocumentSaver (fileName : FileInfo) (xdoc : XDocument) =
    xdoc.Save (fileName.FullName)

let Index () =
    let newAntho = Indexation.IndexWorkspace () |> Package.Simplify
    Configuration.SaveAnthology newAntho

let Convert () = 
    let antho = Configuration.LoadAnthology ()
    Conversion.GenerateProjects antho.Projects XDocumentSaver
    Conversion.ConvertProjects antho XDocumentLoader XDocumentSaver
    Conversion.RemoveUselessStuff antho

    // setup additional files for views to work correctly
    let confDir = Env.GetFolder Env.Config
    let installDir = Env.GetFolder Env.Installation
    let publishSource = installDir |> GetFile Env.FULLBUILD_TARGETS
    let publishTarget = confDir |> GetFile Env.FULLBUILD_TARGETS
    publishSource.CopyTo(publishTarget.FullName, true) |> ignore


let ClonedRepositories (wsDir : DirectoryInfo) (repos : Repository set) =
    repos |> Set.filter (fun x -> let repoDir = wsDir |> GetSubDirectory x.Name.toString
                                  repoDir.Exists)

let CollectRepoHash wsDir (repos : Repository set) =
    let getRepoHash (repo : Repository) =
        let tip = Vcs.VcsTip wsDir repo
        { Repository = repo.Name; Version = BookmarkVersion tip}

    repos |> Set.map getRepoHash


let Push buildnum = 
    let antho = Configuration.LoadAnthology ()
    let wsDir = Env.GetFolder Env.Workspace
    let allRepos = antho.Repositories
    let clonedRepos = allRepos |> ClonedRepositories wsDir
    let bookmarks = CollectRepoHash wsDir clonedRepos
    let baseline = { Bookmarks = bookmarks }
    Configuration.SaveBaseline baseline

    let mainRepo = antho.MasterRepository

    // commit
    Try (fun () -> Vcs.VcsCommit wsDir mainRepo "bookmark")

    // copy bin content
    let hash = Vcs.VcsTip wsDir mainRepo
    BuildArtifacts.Publish buildnum hash

let Checkout (version : BookmarkVersion) =
    // checkout repositories
    DisplayHighlight ".full-build"
    let antho = Configuration.LoadAnthology ()
    let wsDir = Env.GetFolder Env.Workspace
    let mainRepo = antho.MasterRepository
    Vcs.VcsCheckout wsDir mainRepo (Some version)

    // checkout each repository now
    let antho = Configuration.LoadAnthology ()
    let baseline = Configuration.LoadBaseline ()
    let clonedRepos = antho.Repositories |> ClonedRepositories wsDir
    for repo in clonedRepos do
        DisplayHighlight repo.Name.toString
        let repoVersion = baseline.Bookmarks |> Seq.tryFind (fun x -> x.Repository = repo.Name)
        match repoVersion with
        | Some x -> Vcs.VcsCheckout wsDir repo (Some x.Version)
        | None -> Vcs.VcsCheckout wsDir repo None

    // update binaries with observable baseline
    BuildArtifacts.PullReferenceBinaries version.toString

let Pull (src : bool) (bin : bool) =
    let antho = Configuration.LoadAnthology ()
    let wsDir = Env.GetFolder Env.Workspace

    if src then
        DisplayHighlight ".full-build"
        let mainRepo = antho.MasterRepository
        Vcs.VcsPull wsDir mainRepo

        let antho = Configuration.LoadAnthology ()
        let clonedRepos = antho.Repositories |> ClonedRepositories wsDir
        for repo in clonedRepos do
            DisplayHighlight repo.Name.toString

            let repoDir = wsDir |> GetSubDirectory repo.Name.toString
            if repoDir.Exists then
                Vcs.VcsPull wsDir repo

    if bin then
        BuildArtifacts.PullLatestReferenceBinaries ()


let Init (path : string) (uri : RepositoryUrl) = 
    let wsDir = DirectoryInfo(path)
    wsDir.Create()
    if IsWorkspaceFolder wsDir then 
        printf "[WARNING] Workspace already exists - skipping"
    else
        let vcsType = Vcs.VcsDetermineType uri
        let repo = { Name = RepositoryId.from Env.MASTER_REPO; Url = uri; Vcs=vcsType; Branch = None }
        VcsClone wsDir true repo

let Exec cmd =
    let antho = Configuration.LoadAnthology()
    let wsDir = Env.GetFolder Env.Workspace
    for repo in antho.Repositories do
        let repoDir = wsDir |> GetSubDirectory repo.Name.toString
        if repoDir.Exists then
            let vars = [ ("FB_NAME", repo.Name.toString)
                         ("FB_PATH", repoDir.FullName)
                         ("FB_URL", repo.Url.toLocalOrUrl) ] |> Map.ofSeq
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
        let wsDir = Env.GetFolder Env.Workspace
        Vcs.VcsClean wsDir oldAntho.MasterRepository
        let newAntho = Configuration.LoadAnthology()
        Configuration.SaveAnthology oldAntho
         
       // remove repositories
        let reposToRemove = Set.difference oldAntho.Repositories newAntho.Repositories
        for repo in reposToRemove do
            let repoDir = wsDir |> GetSubDirectory repo.Name.toString
            if repoDir.Exists then repoDir.Delete(true)

        // clean existing repositories
        for repo in newAntho.Repositories do
            let repoDir = wsDir |> GetSubDirectory repo.Name.toString
            if repoDir.Exists then
                Vcs.VcsClean wsDir repo

        Vcs.VcsClean wsDir newAntho.MasterRepository

let UpdateGuid (repo : RepositoryId) =
    printfn "DANGER ! You will lose all uncommitted changes. Do you want to continue [Yes to confirm] ?"
    let res = Console.ReadLine()
    if res = "Yes" then
        let antho = Configuration.LoadAnthology ()
        let wsDir = Env.GetFolder Env.Workspace
        let repoDir = wsDir |> GetSubDirectory repo.toString
        let projects = Indexation.FindKnownProjects repoDir
        for project in projects do
            let xdoc = XDocument.Load(project.FullName)
            let guid = xdoc.Descendants(NsMsBuild + "ProjectGuid").Single()
            guid.Value <- Guid.NewGuid().ToString("B")
            xdoc.Save(project.FullName)
