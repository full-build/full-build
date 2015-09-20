module ConfigurationSerializer

open System
open System.IO
open Anthology

type ConfigurationConfig = FSharp.Configuration.YamlConfig<"configuration.yaml">

let SerializeConfiguration (globalConfig : GlobalConfiguration) =
    let config = ConfigurationConfig()

    config.configuration.bin <- globalConfig.BinRepo
    config.configuration.uri <- Uri(globalConfig.Repository.Url.toString)
    config.configuration.``type`` <- globalConfig.Repository.Vcs.toString

    config.configuration.nugets.Clear()
    for nuget in globalConfig.NuGets do
        let cnuget = ConfigurationConfig.configuration_Type.nugets_Item_Type()
        cnuget.nuget <- Uri(nuget.toString)
        config.configuration.nugets.Add (cnuget)
    config.ToString()

let DeserializeConfiguration (content : string) =
    let rec convertToNuGets (items : ConfigurationConfig.configuration_Type.nugets_Item_Type list) =
        match items with
        | [] -> List.empty
        | x :: tail -> (RepositoryUrl.from (x.nuget)) :: convertToNuGets tail
    
    let convertToRepository (item : ConfigurationConfig.configuration_Type) =
        { Name = RepositoryId.from ".full-build"; Vcs=VcsType.from item.``type``; Url=RepositoryUrl.from (item.uri) }

    let config = ConfigurationConfig()
    config.LoadText content

    { BinRepo = config.configuration.bin
      Repository = convertToRepository config.configuration
      NuGets = convertToNuGets (config.configuration.nugets |> List.ofSeq) }

let Load (filename : FileInfo) =
    let content = File.ReadAllText (filename.FullName)
    DeserializeConfiguration content

