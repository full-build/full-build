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

module Commands.Upgrade

open FsHelpers
open System.IO
open System.Diagnostics
open System.IO.Compression
open FSharp.Data

type GitRelease = JsonProvider<"Examples/ghreleasefeed.json">

let private getLatestReleaseUrl (verStatus : string) =
    let tag = sprintf "-%s" verStatus
    let path = @"https://api.github.com/repos/full-build/full-build/releases" 
    let result = Http.RequestString(path,
                                    customizeHttpRequest = fun x -> x.UserAgent<-"fullbuild"; x)
    let releases = GitRelease.Parse(result)
    let mostRecentRelease = releases |> Seq.find (fun x -> x.TagName.EndsWith(tag))
    (mostRecentRelease.Assets.[0].BrowserDownloadUrl, mostRecentRelease.Name)

let private downloadZip zipUrl =
    let response = Http.Request(zipUrl,
                                customizeHttpRequest = fun x -> x.UserAgent <- "fullbuild"; x)
    let zipFile = Path.GetTempFileName()
    match response.Body with
        | Binary bytes -> System.IO.File.WriteAllBytes(zipFile, bytes)
        | _ -> printfn "ERROR"
    new FileInfo(zipFile)

let private backupFile (file:FileInfo) =
    System.IO.File.Move(file.FullName, file.FullName + "_bkp")

let private deleteBackupFiles (dir:DirectoryInfo) =
    dir.GetFiles("*_bkp")|> Seq.iter (fun x -> File.Delete(x.FullName))

let private waitProcessToExit processId =
    try
        use processInfo = Process.GetProcessById(processId)
        if processInfo <> null then
            processInfo.WaitForExit()
    with
        | :? System.ArgumentException -> ()
        | _ -> reraise()

let private getSameFiles (firstDir:DirectoryInfo) (secondDir:DirectoryInfo) =
    firstDir.GetFiles() |> Array.where(fun x-> (secondDir |> FsHelpers.GetFile x.Name).Exists)

let Upgrade (tag : string) =
    let (zipUrl, ver) = getLatestReleaseUrl tag
    printfn "Upgrading to version %s (%s)" ver tag

    let installDir = Env.getInstallationFolder ()
    deleteBackupFiles installDir

    let downloadedZip = downloadZip zipUrl

    let unzipFolder = installDir |> GetSubDirectory "tmp"
    System.IO.Compression.ZipFile.ExtractToDirectory(downloadedZip.FullName, unzipFolder.FullName)

    getSameFiles installDir unzipFolder |> Seq.iter backupFile

    let destinationFilePath tempFilePath =
            let file = installDir |> GetFile (Path.GetFileName(tempFilePath))
            file.FullName

    Directory.EnumerateFiles(unzipFolder.FullName) |> Seq.iter (fun x -> File.Copy(x, destinationFilePath x, true))

    unzipFolder |> ForceDelete

    use currentProcess = Process.GetCurrentProcess()
    let processId = currentProcess.Id
    Exec.Spawn (installDir |> GetFile "fullbuild.exe").FullName (processId |> sprintf "upgrade %i") "runas"

let FinalizeUpgrade processId =
    waitProcessToExit processId
    Env.getInstallationFolder () |> deleteBackupFiles
    Env.getInstallationFolder () |> OsHelpers.RegisterSystemExtension
    let version = Env.FullBuildVersion() 
    printf "Updated to version %s" (version.ToString())
