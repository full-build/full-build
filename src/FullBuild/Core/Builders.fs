//   Copyright 2014-2017 Pierre Chalamet
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
open FsHelpers
open Env
open Graph
open Exec

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

let writeVersionMsbuild version =
    let fsFile = Env.GetFsGlobalAssemblyInfoFileName()
    File.WriteAllLines(fsFile.FullName, generateVersionFs version)

    let csFile = Env.GetCsGlobalAssemblyInfoFileName()
    File.WriteAllLines(csFile.FullName, generateVersionCs version)

let buildSkip (viewFile : FileInfo) (config : string option) (clean : bool) (multithread : bool) (version : string) =
    ()


let buildMsbuild (viewFile : FileInfo) (config : string option) (clean : bool) (multithread : bool) (version : string) =
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let viewName = Path.GetFileNameWithoutExtension(viewFile.Name)
    let slnName = viewName + ".sln"

    writeVersionMsbuild version

    // restore first
    let rebuildArg = if clean then "--no-incremental" else ""
    let args = sprintf "build %s %A /p:SolutionDir=%A /p:SolutionName=%A" rebuildArg slnName wsDir.FullName viewFile.Name
    Exec "dotnet" args wsDir Map.empty |> IO.CheckResponseCode

    //let restoreArgs = sprintf "/nologo /t:Restore /p:SolutionDir=%s /p:SolutionName=%A %A" wsDir.FullName viewName viewFile.Name
    //Exec "msbuild" restoreArgs wsDir Map.empty |> IO.CheckResponseCode

    //let target = if clean then "Rebuild"
    //             else "Build"

    //let argTarget = sprintf "/t:%s /p:SolutionDir=%A /p:SolutionName=%A" target wsDir.FullName viewName
    //let argMt = if multithread then "/m"
    //            else ""

    //let argConfig = match config with
    //                | Some conf -> sprintf "/p:Configuration=%s" conf
    //                | _ -> ""

    //let args = sprintf "/nologo %s %s %s %A" argTarget argMt argConfig viewFile.Name

    //Exec "msbuild" args wsDir Map.empty |> IO.CheckResponseCode

let chooseBuilder (builderType : BuilderType) msbuildBuilder skipBuilder =
    let builder = match builderType with
                  | BuilderType.MSBuild -> msbuildBuilder
                  | BuilderType.Skip -> skipBuilder
    builder

let BuildWithBuilder (builder : BuilderType) (viewFile : FileInfo) (config : string option) (clean : bool) (multithread : bool) (version : string) =
    (chooseBuilder builder buildMsbuild buildSkip) viewFile config clean multithread version

