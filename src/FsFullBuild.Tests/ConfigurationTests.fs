module ConfigurationTests

open NUnit.Framework
open FsUnit
open Configuration
open System
open System.IO
open Types

[<Test>]
let CheckGlobalIniFilename () =
    let file = new FileInfo("GlobalConfig.ini")
    file.Exists |> should equal true
    let config = GlobalConfigurationFromFile file

    let expected = { BinRepo = "c:\BinRepo"
                     Repository = (Git, "full-build", "https://github.com/pchalamet/full-build")
                     PackageGlobalCache = "c:\PackageGlobalCache"
                     NuGets = ["https://www.nuget.org/api/v2/"; "https://www.nuget.org/api/v3/"] }

    config |> should equal expected

[<Test>]
let CheckDefaultConfigurationFile () =
    let file = DefaultGlobalIniFilename ()
    file.FullName.Contains(".full-build") |> should equal true



[<Test>]
let CheckWorkspaceIniFilename () =
    let file = new FileInfo("WorkspaceConfig.ini")
    file.Exists |> should equal true
    let config = WorkspaceConfigurationFromFile file

    let expected = { Repositories = [ (Git, "cassandra_sharp", "https://github.com/pchalamet/cassandra-sharp")
                                      (Git, "cassandra_sharp_contrib", "https://github.com/pchalamet/cassandra-sharp-contrib") ] }

    config |> should equal expected
