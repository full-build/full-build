module ConfigurationTests

open NUnit.Framework
open FsUnit
open Configuration
open System
open System.IO

[<Test>]
let CheckGlobalIniFilename () =
    let file = new FileInfo("GlobalConfig.ini")
    let config = GlobalConfigurationFromFile file

    let expected = { BinRepo = "c:\BinRepo"
                     RepoType = "git"
                     RepoUrl = "https://github.com/pchalamet/full-build"
                     PackageGlobalCache = "c:\PackageGlobalCache"
                     NuGets = ["https://www.nuget.org/api/v2/"; "https://www.nuget.org/api/v3/"] }

    config |> should equal expected

[<Test>]
let CheckDefaultConfigurationFile () =
    let file = DefaultGlobalIniFilename ()
    file.FullName.Contains(".full-build") |> should equal true
