module Configuration

open System
open System.IO
open FileExtensions

//[FullBuild]
//BinRepo=c:\BinRepo
//RepoType=git
//RepoUrl=https://github.com/pchalamet/cassandra-sharp-full-build
//PackageGlobalCache=c:\PackageGlobalCache


type GlobalConfiguration = { BinRepo : string; RepoType : string; RepoUrl : string; PackageGlobalCache : string}


let GlobalIniFileFromFile (configFile : FileInfo) =
    let ini = new Mini.IniDocument(configFile.FullName);
    let fbSection = ini.["FullBuild"];
    fbSection
    
let GlobalIniFilename = 
    let userProfileDir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    let configFile = ".full-build" |> GetFile userProfileDir  
    configFile

let GlobalConfigurationFromFile (globalIniFile : FileInfo) : GlobalConfiguration =
    let globalIni = GlobalIniFileFromFile globalIniFile
    let binRepo = globalIni.["BinRepo"].Value
    let repoType = globalIni.["RepoType"].Value
    let repoUrl = globalIni.["RepoUrl"].Value
    let packageGlobalCache = globalIni.["PackageGlobalCache"].Value
    { BinRepo = binRepo; RepoType = repoType; RepoUrl = repoUrl; PackageGlobalCache = packageGlobalCache}

let GlobalConfiguration = GlobalConfigurationFromFile GlobalIniFilename
