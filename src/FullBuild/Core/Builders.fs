﻿//   Copyright 2014-2016 Pierre Chalamet
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

module Core.Builders
open System.IO
open IoHelpers
open Env
open Graph

let private checkErrorCode code out err =
    if code <> 0 then failwithf "Process failed with error %d" code

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
    let binDir = Env.GetFolder Folder.Bin
    let fsFile = binDir |> GetFile "BuildVersionAssemblyInfo.fs"
    File.WriteAllLines(fsFile.FullName, generateVersionFs version)

    let csFile = binDir |> GetFile "BuildVersionAssemblyInfo.cs"
    File.WriteAllLines(csFile.FullName, generateVersionCs version)


let buildSkip (viewFile : FileInfo) (config : string) (clean : bool) (multithread : bool) (version : string option) =
    ()

let buildMsbuild (viewFile : FileInfo) (config : string) (clean : bool) (multithread : bool) (version : string option) =
    match version with
    | Some givenVersion -> versionMsbuild givenVersion
    | _ -> ()

    let target = if clean then "Rebuild"
                 else "Build"


    let viewName = Path.GetFileNameWithoutExtension(viewFile.Name)
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let argTarget = sprintf "/t:%s /p:SolutionDir=%A /p:SolutionName=%A" target wsDir.FullName viewName
    let argMt = if multithread && not <| Env.IsMono () then "/m"
                else ""

    let argConfig = sprintf "/p:Configuration=%s" config
    let args = sprintf "/nologo %s %s %s %A" argTarget argMt argConfig viewFile.Name

    if Env.IsMono () then checkedExec "xbuild" args wsDir Map.empty
    else checkedExec "msbuild" args wsDir Map.empty

let chooseBuilder (builderType : BuilderType) msbuildBuilder skipBuilder =
    let builder = match builderType with
                  | BuilderType.MSBuild -> msbuildBuilder
                  | BuilderType.Skip -> skipBuilder
    builder

let BuildWithBuilder (builder : BuilderType) (viewFile : FileInfo) (config : string) (clean : bool) (multithread : bool) (version : string option) =
    (chooseBuilder builder buildMsbuild buildSkip) viewFile config clean multithread version

