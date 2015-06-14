module Configuration

open System
open System.IO
open FileExtensions
open WellknownFolders
open Types

type GlobalConfiguration = 
    { 
        BinRepo : string
        Repository : Repository
        PackageGlobalCache : string
        NuGets : string list
    }

type WorkspaceConfiguration =
    {
        Repositories : Repository list
    }

let IniDocFromFile (configFile : FileInfo) =
    let ini = new Mini.IniDocument(configFile.FullName);
    ini
    
let DefaultGlobalIniFilename () = 
    let userProfileDir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    let configFile = userProfileDir |> GetFile WORKSPACE_CONFIG_FILE
    configFile

let GlobalConfigurationFromFile file =
    let ini = IniDocFromFile file
    let fbSection = ini.["FullBuild"]
    let binRepo = fbSection.["BinRepo"].Value
    let repoType = fbSection.["RepoType"].Value
    let repoUrl = fbSection.["RepoUrl"].Value
    let packageGlobalCache = fbSection.["PackageGlobalCache"].Value
    let (ToRepository repo) = (repoType, repoUrl, ".full-build")

    let ngSection = ini.["NuGet"]
    let nugets = ngSection |> Seq.map (fun x -> x.Value) |> Seq.toList

    { BinRepo = binRepo; Repository = repo; PackageGlobalCache = packageGlobalCache; NuGets = nugets }


let DefaultWorkspaceIniFilename () =
    let wsDir = WorkspaceFolder ()
    let fbDir = wsDir |> GetSubDirectory WORKSPACE_CONFIG_FOLDER
    let wsConfigFile = fbDir |> GetFile "config"
    wsConfigFile

let RepositoryConfigurationFromSection (section : Mini.IniSection) =
    let name = section.Name
    let vcs = section.["vcs"].Value
    let url = section.["url"].Value
    let (ToRepository repo) = (vcs, url, name)
    repo

let WorkspaceConfigurationFromFile file =
    let ini = IniDocFromFile file
    let repositories = ini |> Seq.map (RepositoryConfigurationFromSection) |> Seq.toList
    { Repositories = repositories }

let GlobalConfig : GlobalConfiguration = 
    let filename = DefaultGlobalIniFilename ()
    GlobalConfigurationFromFile filename

let WorkspaceConfig () : WorkspaceConfiguration =
    let filename = DefaultWorkspaceIniFilename ()
    WorkspaceConfigurationFromFile filename

