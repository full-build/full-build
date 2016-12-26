//   Copyright 2014-2016 Pierre Chalamet
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

module Tools.Paket
open System
open Anthology
open System.IO
open IoHelpers
open Collections

let private parseContent (lines : string seq) =
    seq {
        for line in lines do
            let items = line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
            if 0 < items.Length then
                match items.[0] with
                | "nuget" -> yield (PackageId.from items.[1])
                | _ -> ()
    }

let private updateSourceContent (lines : string seq) (sources : RepositoryUrl seq) =
    seq {
        for source in sources do
            let sourceUri = source.toLocalOrUrl
            yield sprintf "source %s" sourceUri

        for line in lines do
            let items = line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
            if 0 < items.Length then
                match items.[0] with
                | "source" -> ()
                | _ -> yield line
            else yield line
    }

let private generateDependenciesContent (packages : Package seq) =
    seq {
        for package in packages do
            match package.Version with
            | PackageVersion.PackageVersion x -> yield sprintf "nuget %s ~> %s" (package.Id.toString) x
            | PackageVersion.Unspecified -> yield sprintf "nuget %s" (package.Id.toString)
    }

let private removeDependenciesContent (lines : string seq) (packages : PackageId set) =
    seq {
        for line in lines do
            let items = line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
            if 0 < items.Length then
                match items.[0] with
                | "nuget" -> if Set.contains (PackageId.from items.[1]) packages then ()
                             else yield line
                | _ -> yield line
            else
                yield line
    }

let private executePaketCommand cmd =
    let confDir = Env.GetFolder Env.Folder.Config
    Exec.Exec "paket.exe" cmd confDir Map.empty |> Exec.CheckResponseCode

let UpdateSources (sources : RepositoryUrl list) =
    let confDir = Env.GetFolder Env.Folder.Config
    let paketDep = confDir |> GetFile "paket.dependencies"
    let oldContent = if paketDep.Exists then File.ReadAllLines (paketDep.FullName) |> Array.toSeq
                     else Seq.empty
    let content = updateSourceContent oldContent sources
    File.WriteAllLines (paketDep.FullName, content)

let ParsePaketDependencies () =
    let confDir = Env.GetFolder Env.Folder.Config
    let paketDep = confDir |> GetFile "paket.dependencies"
    if paketDep.Exists then
        let lines = File.ReadAllLines (paketDep.FullName)
        let packageRefs =  parseContent lines
        packageRefs |> Set
    else
        Set.empty

let AppendDependencies (packages : Package set) =
    if packages <> Set.empty then
        let confDir = Env.GetFolder Env.Folder.Config
        let paketDep = confDir |> GetFile "paket.dependencies"
        let content = generateDependenciesContent packages
        File.AppendAllLines (paketDep.FullName, content)

let RemoveDependencies (packages : PackageId set) =
    if packages <> Set.empty then
        let confDir = Env.GetFolder Env.Folder.Config
        let paketDep = confDir |> GetFile "paket.dependencies"
        let content = File.ReadAllLines (paketDep.FullName)
        let newContent = removeDependenciesContent content packages
        File.WriteAllLines (paketDep.FullName, newContent)


let PaketInstall () =
    executePaketCommand "install"

let PaketRestore () =
    executePaketCommand "restore"

let PaketUpdate () =
    executePaketCommand "update"

let PaketOutdated () =
    executePaketCommand "outdated"

let PaketInstalled () =
    executePaketCommand "show-installed-packages"
