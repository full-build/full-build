module ConfigurationTests

open NUnit.Framework
open FsUnit
open Configuration
open System.IO

[<Test>]
let CheckGlobalIniFilename () =
    let file = new FileInfo("GlobalConfig.ini")
    let config = GlobalConfigurationFromFile file

    config.BinRepo |> should equal "c:\BinRepo"
    config.RepoType |> should equal "git"
    config.RepoUrl |> should equal "https://github.com/pchalamet/full-build"
    config.PackageGlobalCache |> should equal "c:\PackageGlobalCache"
