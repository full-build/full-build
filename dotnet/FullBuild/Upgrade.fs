module Upgrade

open FSharp.Data
open System.IO
open System.IO.Compression

type Tags = JsonProvider<"""[
  {
    "url": "https://api.github.com/repos/full-build/full-build/releases/3072440",
    "assets_url": "https://api.github.com/repos/full-build/full-build/releases/3072440/assets",
    "upload_url": "https://uploads.github.com/repos/full-build/full-build/releases/3072440/assets{?name,label}",
    "html_url": "https://github.com/full-build/full-build/releases/tag/2.2.714",
    "id": 3072440,
    "tag_name": "2.2.714",
    "target_commitish": "master",
    "name": "full-build 2.2.714",
    "draft": false,
    "author": {
      "login": "pchalamet",
      "id": 1562400,
      "avatar_url": "https://avatars.githubusercontent.com/u/1562400?v=3",
      "gravatar_id": "",
      "url": "https://api.github.com/users/pchalamet",
      "html_url": "https://github.com/pchalamet",
      "followers_url": "https://api.github.com/users/pchalamet/followers",
      "following_url": "https://api.github.com/users/pchalamet/following{/other_user}",
      "gists_url": "https://api.github.com/users/pchalamet/gists{/gist_id}",
      "starred_url": "https://api.github.com/users/pchalamet/starred{/owner}{/repo}",
      "subscriptions_url": "https://api.github.com/users/pchalamet/subscriptions",
      "organizations_url": "https://api.github.com/users/pchalamet/orgs",
      "repos_url": "https://api.github.com/users/pchalamet/repos",
      "events_url": "https://api.github.com/users/pchalamet/events{/privacy}",
      "received_events_url": "https://api.github.com/users/pchalamet/received_events",
      "type": "User",
      "site_admin": false
    },
    "prerelease": true,
    "created_at": "2016-04-21T19:58:42Z",
    "published_at": "2016-04-21T20:01:47Z",
    "assets": [
      {
        "url": "https://api.github.com/repos/full-build/full-build/releases/assets/1582517",
        "id": 1582517,
        "name": "full-build-net45-anycpu-714.zip",
        "label": "",
        "uploader": {
          "login": "pchalamet",
          "id": 1562400,
          "avatar_url": "https://avatars.githubusercontent.com/u/1562400?v=3",
          "gravatar_id": "",
          "url": "https://api.github.com/users/pchalamet",
          "html_url": "https://github.com/pchalamet",
          "followers_url": "https://api.github.com/users/pchalamet/followers",
          "following_url": "https://api.github.com/users/pchalamet/following{/other_user}",
          "gists_url": "https://api.github.com/users/pchalamet/gists{/gist_id}",
          "starred_url": "https://api.github.com/users/pchalamet/starred{/owner}{/repo}",
          "subscriptions_url": "https://api.github.com/users/pchalamet/subscriptions",
          "organizations_url": "https://api.github.com/users/pchalamet/orgs",
          "repos_url": "https://api.github.com/users/pchalamet/repos",
          "events_url": "https://api.github.com/users/pchalamet/events{/privacy}",
          "received_events_url": "https://api.github.com/users/pchalamet/received_events",
          "type": "User",
          "site_admin": false
        },
        "content_type": "application/octet-stream",
        "state": "uploaded",
        "size": 1375047,
        "download_count": 2,
        "created_at": "2016-04-21T20:01:48Z",
        "updated_at": "2016-04-21T20:01:49Z",
        "browser_download_url": "https://github.com/full-build/full-build/releases/download/2.2.714/full-build-net45-anycpu-714.zip"
      }
    ],
    "tarball_url": "https://api.github.com/repos/full-build/full-build/tarball/2.2.714",
    "zipball_url": "https://api.github.com/repos/full-build/full-build/zipball/2.2.714",
    "body": "8828bfca54f50a196085c5631819dd5305a0baab view creation performance improvement #51"
  }]""">

let getLatestReleaseUrl () = 
    let path = @"https://api.github.com/repos/full-build/full-build/releases"
    let result = Http.RequestString(path,customizeHttpRequest=fun x->x.UserAgent<-"fullbuild";x)
    let tags = Tags.Parse(result)
    tags.[0].Assets.[0].BrowserDownloadUrl

let downloadZip zipUrl = 
    let response = Http.Request(zipUrl,customizeHttpRequest=fun x->x.UserAgent<-"fullbuild";x)
    let zipFile = Path.GetTempFileName()
    match response.Body with
        | Binary bytes -> System.IO.File.WriteAllBytes(zipFile, bytes)
        | _ -> printfn "ERROR" 
    new FileInfo(zipFile)

let backupFile (file:FileInfo) = 
        System.IO.File.Move(file.FullName, file.FullName+"_bkp")

let upgrade () =
    let installDir = Env.getInstallationFolder ()
    
    let backupFiles = installDir.GetFiles("*_bkp")

    if backupFiles.Length>0 then
        printfn "Cleaning installation folder from backup files"
        backupFiles |> Seq.iter(fun x->File.Delete(x.FullName))
    else
        printfn "Upgrading"
        
        let zipUrl = getLatestReleaseUrl ()
        let downloadedZip = downloadZip zipUrl
        
        installDir.GetFiles() |> Seq.iter(fun x->backupFile x)

        let unzipFolder = installDir.FullName+"/tmp" |> DirectoryInfo
        let archive = System.IO.Compression.ZipFile.ExtractToDirectory(downloadedZip.FullName, unzipFolder.FullName)
    
        let destinationFilePath tempFilePath = 
            Path.Combine(installDir.FullName, Path.GetFileName(tempFilePath))
        Directory.EnumerateFiles(unzipFolder.FullName) |> Seq.iter(fun x->File.Copy(x,destinationFilePath x,true))
    
        unzipFolder.Delete(true)

