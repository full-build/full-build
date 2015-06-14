module Configuration

open System
open System.IO
open FileExtensions

type GlobalConfiguration = 
    { 
        BinRepo : string
        RepoType : string
        RepoUrl : string
        PackageGlobalCache : string
        NuGets : string list
    }

let GlobalIniFileFromFile (configFile : FileInfo) =
    let ini = new Mini.IniDocument(configFile.FullName);
    ini
    
let DefaultGlobalIniFilename () = 
    let userProfileDir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    let configFile = ".full-build" |> GetFile userProfileDir  
    configFile

let GlobalConfigurationFromFile globalIniFile =
    let globalIni = GlobalIniFileFromFile globalIniFile
    let fbSection = globalIni.["FullBuild"]
    let binRepo = fbSection.["BinRepo"].Value
    let repoType = fbSection.["RepoType"].Value
    let repoUrl = fbSection.["RepoUrl"].Value
    let packageGlobalCache = fbSection.["PackageGlobalCache"].Value

    let ngSection = globalIni.["NuGet"]
    let nugets = ngSection |> Seq.map (fun x -> x.Value) |> Seq.toList

    { BinRepo = binRepo; RepoType = repoType; RepoUrl = repoUrl; PackageGlobalCache = packageGlobalCache; NuGets = nugets }

let GlobalConfig : GlobalConfiguration = 
    let filename = DefaultGlobalIniFilename ()
    GlobalConfigurationFromFile filename
