module Upgrade

open IoHelpers
open System.IO
open System.Diagnostics
open System.IO.Compression
open FSharp.Data

type GitRelease = JsonProvider<"ghreleasefeed.json">

let getLatestReleaseUrl () = 
    let path = @"https://api.github.com/repos/full-build/full-build/releases/latest"
    let result = Http.RequestString(path, 
                                    customizeHttpRequest = fun x -> x.UserAgent<-"fullbuild"; x)
    let releases = GitRelease.Parse(result)
    printfn "Latest version: %s" releases.Name
    releases.Assets.[0].BrowserDownloadUrl

let downloadZip zipUrl = 
    let response = Http.Request(zipUrl, 
                                customizeHttpRequest = fun x -> x.UserAgent <- "fullbuild"; x)
    let zipFile = Path.GetTempFileName()
    match response.Body with
        | Binary bytes -> System.IO.File.WriteAllBytes(zipFile, bytes)
        | _ -> printfn "ERROR" 
    new FileInfo(zipFile)

let backupFile (file:FileInfo) = 
    System.IO.File.Move(file.FullName, file.FullName + "_bkp")

let deleteBackupFiles (dir:DirectoryInfo) =
    dir.GetFiles("*_bkp")|> Seq.iter (fun x -> File.Delete(x.FullName))    

let waitProcessToExit processId = 
    try
        let processInfo = Process.GetProcessById(processId)
        if not (processInfo=null) then
            processInfo.WaitForExit()
    with
        | :? System.ArgumentException -> ()
        | _ -> reraise()

let FinalizeUpgrade processId =
    printfn "Cleaning installation folder from backup files"
    waitProcessToExit processId
    Env.getInstallationFolder () |> deleteBackupFiles

let getSameFiles (firstDir:DirectoryInfo) (secondDir:DirectoryInfo) =
    firstDir.GetFiles() |> Seq.where(fun x-> (secondDir |> IoHelpers.GetFile x.Name).Exists)
    
let Upgrade () =
    printfn "Upgrading"
    let installDir = Env.getInstallationFolder ()
    deleteBackupFiles installDir

    let zipUrl = getLatestReleaseUrl ()
    let downloadedZip = downloadZip zipUrl
        
    let unzipFolder = installDir |> GetSubDirectory "tmp"
    System.IO.Compression.ZipFile.ExtractToDirectory(downloadedZip.FullName, unzipFolder.FullName)
    
    getSameFiles installDir unzipFolder |> Seq.iter backupFile

    let destinationFilePath tempFilePath = 
            let file = installDir |> GetFile (Path.GetFileName(tempFilePath))
            file.FullName

    Directory.EnumerateFiles(unzipFolder.FullName) |> Seq.iter (fun x -> File.Copy(x, destinationFilePath x, true))

    unzipFolder |> ForceDelete

    let processId = Process.GetCurrentProcess().Id
    Exec.ExecWithArguments (installDir |> GetFile "fullbuild.exe").FullName (processId |> sprintf "upgrade %i")
