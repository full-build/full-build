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
open IoHelpers
open Graph
open Collections


let Publish (graph : Graph) (tagInfo : Tag.TagInfo) =
    let tag = Tag.Format tagInfo
    let appDir = Env.GetFolder Env.Folder.AppOutput
    let versionDir = DirectoryInfo(graph.ArtifactsDir) |> GetSubDirectory tag
    let tmpVersionDir = DirectoryInfo(versionDir.FullName + ".tmp")
    let versionLine = sprintf "%s" tag

    try
        let doPublish = not versionDir.Exists
        if doPublish then
            if tmpVersionDir.Exists then
                tmpVersionDir.Delete(true)

            let sourceBinDir = Env.GetFolder Env.Folder.Bin
            let targetBinDir = tmpVersionDir |> GetSubDirectory Env.PUBLISH_BIN_FOLDER
            IoHelpers.CopyFolder sourceBinDir targetBinDir true

            let appTargetDir = tmpVersionDir |> GetSubDirectory Env.PUBLISH_APPS_FOLDER
            IoHelpers.CopyFolder appDir appTargetDir true

            tmpVersionDir.MoveTo(versionDir.FullName)
        else
            printfn "[WARNING] Build output already exists - skipping"

        let latestVersionFile = DirectoryInfo(graph.ArtifactsDir) |> GetFile "versions"

        File.AppendAllLines(latestVersionFile.FullName, [versionLine])
        for app in appDir |> EnumerateChildren do
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

    lines |> Seq.map Tag.Parse
          |> List.ofSeq



let PullReferenceBinaries (artifacts : string) version =
    let artifactDir = artifacts |> DirectoryInfo

    let versionDir = artifactDir |> GetSubDirectory version
    if versionDir.Exists then
        DisplayHighlight (sprintf "Getting binaries %s" version)
        let sourceBinDir = versionDir |> GetSubDirectory Env.PUBLISH_BIN_FOLDER
        let targetBinDir = Env.GetFolder Env.Folder.Bin
        IoHelpers.CopyFolder sourceBinDir targetBinDir false
    else
        DisplayHighlight "[WARNING] No reference binaries found"
