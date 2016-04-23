module Upgrade

open IoHelpers
open System.IO
open System.IO.Compression
open FSharp.Data

type Tags = JsonProvider<"ghreleasefeed.json">

let getLatestReleaseUrl () = 
    let path = @"https://api.github.com/repos/full-build/full-build/releases/latest"
    let result = Http.RequestString(path, 
                                    customizeHttpRequest = fun x -> x.UserAgent<-"fullbuild"; x)
    let tags = Tags.Parse(result)
    tags.[0].Assets.[0].BrowserDownloadUrl

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

let Upgrade () =
    let installDir = Env.getInstallationFolder ()
    
    let backupFiles = installDir.GetFiles("*_bkp")

    if backupFiles.Length>0 then
        printfn "Cleaning installation folder from backup files"
        backupFiles |> Seq.iter (fun x -> File.Delete(x.FullName))
    else
        printfn "Upgrading"
        
        let zipUrl = getLatestReleaseUrl ()
        let downloadedZip = downloadZip zipUrl
        
        installDir.GetFiles() |> Seq.iter backupFile

        let unzipFolder = installDir |> GetSubDirectory "tmp"
        let archive = System.IO.Compression.ZipFile.ExtractToDirectory(downloadedZip.FullName, unzipFolder.FullName)
    
        let destinationFilePath tempFilePath = 
                let file = installDir |> GetFile (Path.GetFileName(tempFilePath))
                file.FullName

        Directory.EnumerateFiles(unzipFolder.FullName) |> Seq.iter (fun x -> File.Copy(x, destinationFilePath x, true))

        unzipFolder |> ForceDelete

