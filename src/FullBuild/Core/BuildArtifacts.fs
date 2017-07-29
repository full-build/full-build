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

module Core.BuildArtifacts
open System.IO
open FsHelpers
open Graph
open Collections


let Publish (graph : Graph) =
    let baselines = Baselines.from graph
    let baseline = baselines.GetBaseline() |> Option.get
    
    let tag = baseline.Info.Format()
    let appDir = Env.GetFolder Env.Folder.AppOutput
    let versionDir = DirectoryInfo(graph.ArtifactsDir) |> GetSubDirectory tag
    let tmpVersionDir = DirectoryInfo(versionDir.FullName + ".tmp")
    let versionLine = sprintf "%s:%s:%s" baseline.Info.BuildNumber tag baseline.Info.BuildBranch

    try
        let doPublish = not versionDir.Exists
        if doPublish then
            if tmpVersionDir.Exists then
                tmpVersionDir.Delete(true)

            let sourceBinDir = Env.GetFolder Env.Folder.Bin
            let targetBinDir = tmpVersionDir |> GetSubDirectory Env.PUBLISH_BIN_FOLDER
            FsHelpers.CopyFolder sourceBinDir targetBinDir true

            let appTargetDir = tmpVersionDir |> GetSubDirectory Env.PUBLISH_APPS_FOLDER
            FsHelpers.CopyFolder appDir appTargetDir true

            tmpVersionDir.MoveTo(versionDir.FullName)
        else
            printfn "[WARNING] Build output already exists - skipping"

        let latestVersionFile = DirectoryInfo(graph.ArtifactsDir) |> GetFile "versions"

        File.AppendAllLines(latestVersionFile.FullName, [versionLine])
        for app in graph.Applications do
            let appArtifact = appDir |> GetFile app.Name
            if appArtifact.Exists then
                printfn "[appversion] %s" app.Name
                let versionFile = DirectoryInfo(graph.ArtifactsDir) |> GetFile (sprintf "%s.versions" app.Name)
                File.AppendAllLines(versionFile.FullName, [versionLine])
    with
        _ -> versionDir.Refresh ()
             if versionDir.Exists then versionDir.MoveTo(versionDir.FullName + ".failed")

             tmpVersionDir.Refresh()
             if tmpVersionDir.Exists then tmpVersionDir.Delete(true)

             reraise ()

let FetchVersionsForArtifact (graph : Graph) (app : Application) =
    let versionFile = DirectoryInfo(graph.ArtifactsDir) |> GetFile (sprintf "%s.versions" app.Name)
    let lines = System.IO.File.ReadAllLines (versionFile.FullName)
                |> Seq.map (fun x -> x.Split(':').[1])

    let tryParseTag x =
        try
            x |> Baselines.BuildInfo.Parse |> Some
        with
            _ -> None

    lines |> Seq.map tryParseTag
          |> Seq.choose id
          |> List.ofSeq


let PullReferenceBinaries (artifacts : string) branch version =
    let artifactDir = artifacts |> DirectoryInfo
    if artifactDir.Exists |> not then failwithf "Failure to access actifacts folder"

    let branchDir = artifactDir |> GetSubDirectory "fullbuild" |> GetSubDirectory branch
    if branchDir.Exists then
        let versionDir = 
            match version with
            | Some version -> 
                branchDir 
                |> GetSubDirectory version
            | None -> 
                branchDir.EnumerateDirectories() 
                |> Seq.where(fun d -> d.Name |> System.Version.TryParse |> fun (success, version) -> success)
                |> Seq.maxBy (fun d -> d.Name |> System.Version.Parse) 
        ConHelpers.DisplayInfo (sprintf "Copying binaries %s" versionDir.Name)
        let sourceBinDir = versionDir |> GetSubDirectory Env.PUBLISH_BIN_FOLDER
        let targetBinDir = Env.GetFolder Env.Folder.Bin
        FsHelpers.CopyFolder sourceBinDir targetBinDir false
    else
        ConHelpers.DisplayInfo "[WARNING] No reference binaries found"
