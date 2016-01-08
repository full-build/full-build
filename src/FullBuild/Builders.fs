module Builders
open System.IO
open Anthology
open IoHelpers
open Env


let checkErrorCode err =
    if err <> 0 then failwithf "Process failed with error %d" err

let private checkedExec = 
    Exec.Exec checkErrorCode





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




let buildMsbuild (viewFile : FileInfo) (config : string) (clean : bool) (multithread : bool) (version : string) =
    versionMsbuild version

    let target = if clean then "Clean,Build"
                 else "Build"

    let wsDir = Env.GetFolder Env.Workspace
    let argTarget = sprintf "/t:%s" target
    let argMt = if multithread then "/m"
                else ""

    let argConfig = sprintf "/p:Configuration=%s" config
    let args = sprintf "/nologo %s %s %s %A" argTarget argMt argConfig viewFile.Name

    if Env.IsMono () then checkedExec "xbuild" args wsDir
    else checkedExec "msbuild" args wsDir


let buildFake (viewFile : FileInfo) (config : string) (clean : bool) (multithread : bool) (version : string) =
    ()

let chooseBuilder (builderType : BuilderType) msbuildBuilder fakeBuild =
    let builder = match builderType with
                  | BuilderType.MSBuild -> msbuildBuilder
                  | BuilderType.Fake -> fakeBuild
    builder



let BuildWithBuilder (builder : BuilderType) =
    chooseBuilder builder buildMsbuild buildFake
