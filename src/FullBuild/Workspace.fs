// Copyright (c) 2014-2015, Pierre Chalamet
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Pierre Chalamet nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL PIERRE CHALAMET BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
module Workspace

open System.IO
open IoHelpers
open Env
open Configuration
open Vcs
open Anthology
open MsBuildHelpers
open System.Linq
open System.Xml.Linq
open Collections
open System


let Init (path : string) (uri : RepositoryUrl) = 
    let wsDir = DirectoryInfo(path)
    wsDir.Create()
    if IsWorkspaceFolder wsDir then failwith "Workspace already exists"
    let vcsType = Vcs.VcsDetermineType uri
    let repo = { Name = RepositoryId.from Env.MASTER_REPO; Url = uri; Vcs=vcsType}
    VcsCloneRepo wsDir repo

let Create (path : string) (uri : RepositoryUrl) (bin : string) = 
    let wsDir = DirectoryInfo(path)
    wsDir.Create()
    if IsWorkspaceFolder wsDir then failwith "Workspace already exists"
    let vcsType = Vcs.VcsDetermineType uri
    let repo = { Name = RepositoryId.from Env.MASTER_REPO; Url = uri; Vcs=vcsType}
    VcsCloneRepo wsDir repo

    let antho = { Artifacts = bin
                  NuGets = []
                  MasterRepository = repo
                  Repositories = Set.empty
                  Projects = Set.empty 
                  Applications = Set.empty }
    let confDir = wsDir |> GetSubDirectory Env.MASTER_REPO
    let anthoFile = confDir |> GetFile Env.ANTHOLOGY_FILENAME
    AnthologySerializer.Save anthoFile antho

    let baseline = { Bookmarks = Set.empty }
    let baselineFile = confDir |> GetFile Env.BASELINE_FILENAME
    BaselineSerializer.Save baselineFile baseline

    Vcs.VcsIgnore wsDir repo
    Vcs.VcsCommit wsDir repo "setup"



let XDocumentLoader (fileName : FileInfo) =
    XDocument.Load fileName.FullName

let XDocumentSaver (fileName : FileInfo) (xdoc : XDocument) =
    xdoc.Save (fileName.FullName)

let TransformProjects (antho : Anthology) =
    Conversion.GenerateProjects antho.Projects XDocumentSaver
    Conversion.ConvertProjects antho XDocumentLoader XDocumentSaver


let Index () =
    let newAntho = Indexation.IndexWorkspace () |> Package.Simplify
    Configuration.SaveAnthology newAntho

let Convert () = 
    let antho = Configuration.LoadAnthology ()
    TransformProjects antho
    Conversion.RemoveUselessStuff antho

let ClonedRepositories (wsDir : DirectoryInfo) (repos : Repository set) =
    repos |> Set.filter (fun x -> let repoDir = wsDir |> GetSubDirectory x.Name.toString
                                  repoDir.Exists)

let CollectRepoHash wsDir (repos : Repository set) =
    let getRepoHash (repo : Repository) =
        let tip = Vcs.VcsTip wsDir repo
        { Repository = repo.Name; Version = BookmarkVersion tip}

    repos |> Set.map getRepoHash


let Try action =
    try
        action()
    with
        _ -> ()

let Push () = 
    let antho = Configuration.LoadAnthology ()
    let wsDir = Env.GetFolder Env.Workspace
    let clonedRepos = antho.Repositories |> ClonedRepositories wsDir
    let bookmarks = CollectRepoHash wsDir clonedRepos
    let baseline = { Bookmarks = bookmarks }
    Configuration.SaveBaseline baseline

    let mainRepo = antho.MasterRepository

    // commit
    Try (fun () -> Vcs.VcsCommit wsDir mainRepo "bookmark")

    // copy bin content
    let hash = Vcs.VcsTip wsDir mainRepo
    let versionDir = DirectoryInfo(antho.Artifacts) |> GetSubDirectory hash
    if versionDir.Exists then
        printfn "[WARNING] Build output already exists - skipping"
    else
        try
            let binTargetDir = versionDir |> GetSubDirectory Env.MSBUILD_BIN_OUTPUT
            let binDir = Env.GetFolder Env.Bin
            IoHelpers.CopyFolder binDir binTargetDir
            printfn "%s" hash

            let appTargetDir = versionDir |> GetSubDirectory Env.MSBUILD_APP_OUTPUT
            let appDir = Env.GetFolder Env.App
            IoHelpers.CopyFolder appDir appTargetDir
            printfn "%s" hash

            // publish
            Try (fun () -> Vcs.VcsPush wsDir mainRepo)
        with
            _ -> if versionDir.Exists then versionDir.Delete(true)

let Checkout (version : BookmarkVersion) =
    let antho = Configuration.LoadAnthology ()
    let wsDir = Env.GetFolder Env.Workspace
    let mainRepo = antho.MasterRepository
    Vcs.VcsCheckout wsDir mainRepo version

    // checkout repositories
    let antho = Configuration.LoadAnthology ()
    let baseline = Configuration.LoadBaseline ()
    let clonedRepos = antho.Repositories |> ClonedRepositories wsDir
    for repo in clonedRepos do
        let repoVersion = baseline.Bookmarks |> Seq.tryFind (fun x -> x.Repository = repo.Name)
        match repoVersion with
        | Some x -> Vcs.VcsCheckout wsDir repo x.Version
        | None -> Vcs.VcsCheckout wsDir repo Master

    // copy binaries from version
    let hash = match version with
               | BookmarkVersion x -> x
               | Master -> Vcs.VcsTip wsDir mainRepo

    let binDir = Env.GetFolder Env.Bin
    let versionDir = DirectoryInfo(antho.Artifacts) |> GetSubDirectory hash 
    let binSourceDir = versionDir |> GetSubDirectory Env.MSBUILD_BIN_OUTPUT
    IoHelpers.CopyFolder binSourceDir binDir

let Pull () =
    let antho = Configuration.LoadAnthology ()
    let wsDir = Env.GetFolder Env.Workspace
    let mainRepo = antho.MasterRepository
    Vcs.VcsPull wsDir mainRepo

    let antho = Configuration.LoadAnthology ()
    let clonedRepos = antho.Repositories |> ClonedRepositories wsDir
    for repo in clonedRepos do
        let repoDir = wsDir |> GetSubDirectory repo.Name.toString
        if repoDir.Exists then
            Vcs.VcsPull wsDir repo

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
                if Env.IsMono () then Exec.ExecWithArgs "sh" ("-c " + args) repoDir vars
                else Exec.ExecWithArgs "cmd" args repoDir vars
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
