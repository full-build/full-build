module Versionners
open System.IO
open Anthology
open IoHelpers
open Env





let generateVersionFs version =
    [|
        "namespace FullBuildVersion"
        "open System.Reflection"
        sprintf "[<assembly: AssemblyVersion(%A)>]" version
        "()"
    |]

let generateVersionCs version =
    [|
        "using System.Reflection;"
        sprintf "[assembly: AssemblyVersion(%A)]" version
    |]

let versionMsbuild version =
    let wsDir = Env.GetFolder Folder.Workspace
    let fsFile = wsDir |> GetFile "BuildVersionAssemblyInfo.fs"
    File.WriteAllLines(fsFile.FullName, generateVersionFs version)

    let csFile = wsDir |> GetFile "BuildVersionAssemblyInfo.cs"
    File.WriteAllLines(csFile.FullName, generateVersionCs version)


let versionFake version =
    ()


let chooseVersionner (builderType : BuilderType) msbuildVersionner fakeVersionner =
    let builder = match builderType with
                  | BuilderType.MSBuild -> versionMsbuild
                  | BuilderType.Fake -> versionFake
    builder


let VersionWithBuilder (builder : BuilderType) =
    chooseVersionner builder versionMsbuild versionFake

