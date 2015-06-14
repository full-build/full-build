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



let GlobalIniFile =
    let userProfileDir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    let configFile = ".full-build" |> GetFile userProfileDir  
    let ini = new Mini.IniDocument(configFile.FullName);
    let fbSection = ini.["FullBuild"];
    fbSection


let GlobalConfiguration =
    let binRepo = GlobalIniFile.["BinRepo"].Value
    let repoType = GlobalIniFile.["RepoType"].Value
    let repoUrl = GlobalIniFile.["RepoUrl"].Value
    let packageGlobalCache = GlobalIniFile.["PackageGlobalCache"].Value
    { BinRepo = binRepo; RepoType = repoType; RepoUrl = repoUrl; PackageGlobalCache = packageGlobalCache}
