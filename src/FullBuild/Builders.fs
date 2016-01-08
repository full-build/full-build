module Builders
open System.IO
open Anthology
open IoHelpers
open Env


let checkErrorCode err =
    if err <> 0 then failwithf "Process failed with error %d" err

let private checkedExec = 
    Exec.Exec checkErrorCode


let buildMsbuild (config : string) (target : string) (viewFile : FileInfo) (multithread : bool) =
    let wsDir = Env.GetFolder Env.Workspace
    let argTarget = sprintf "/t:%s" target
    let argMt = if multithread then "/m"
                else ""
    let argConfig = sprintf "/p:Configuration=%s" config
    let args = sprintf "/nologo %s %s %s %A" argTarget argMt argConfig viewFile.Name

    if Env.IsMono () then checkedExec "xbuild" args wsDir
    else checkedExec "msbuild" args wsDir


let buildFake (config : string) (target : string) (viewFile : FileInfo) (multithread : bool) =
    ()

let chooseBuilder (builderType : BuilderType) msbuildBuilder fakeBuild =
    let builder = match builderType with
                  | BuilderType.MSBuild -> msbuildBuilder
                  | BuilderType.Fake -> fakeBuild
    builder



let BuildWithBuilder (builder : BuilderType) =
    chooseBuilder builder buildMsbuild buildFake
