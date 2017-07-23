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

module Tools.Paket
open System
open Anthology
open System.IO
open IoHelpers
open Collections
open Exec

let rec private parseContent (group : GroupId) (lines : string list) =
    seq {
        match lines with
        | line :: tail -> let items = line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
                          if 0 < items.Length then
                              match items.[0] with
                              | "nuget" -> yield { Anthology.Package.Id = PackageId.from items.[1]
                                                   Anthology.Package.Group = group }
                              | "group" -> let newGroup = items.[1].ToLowerInvariant() |> GroupId.Named 
                                           yield! tail |> parseContent newGroup
                              | _ -> yield! tail |> parseContent group
        | [] -> ()
    }

let private updateSourceContent (lines : string seq) (sources : RepositoryUrl seq) =
    seq {
        for source in sources do
            let sourceUri = source.toString
            yield sprintf "source %s" sourceUri

        for line in lines do
            let items = line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
            if 0 < items.Length then
                match items.[0] with
                | "source" -> ()
                | _ -> yield line
            else yield line
    }

//let private generateDependenciesContent (packages : Package seq) =
//    seq {
//        for package in packages do
//            yield sprintf "nuget %s" (package.Id.toString)
//    }

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
    Exec "paket.exe" cmd confDir Map.empty |> CheckResponseCode

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
        let packageRefs = File.ReadAllLines (paketDep.FullName) 
                            |> List.ofArray
                            |> parseContent GroupId.Default
        packageRefs |> Set
    else
        Set.empty


let rec private appendToGroup (currentGroup : GroupId) (targetGroup : GroupId) (content : string list) (lines : string list) =
    seq {
        match lines with
        | line :: tail -> let items = line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
                          match items.[0] with
                          | "group" -> let nextGroup = GroupId.Named items.[1]
                                       if currentGroup = targetGroup then
                                           yield! content
                                           yield line
                                           yield! tail
                                       else
                                           yield line
                                           yield! tail |> appendToGroup nextGroup targetGroup content
                          | _ -> yield line
                                 yield! tail |> appendToGroup currentGroup targetGroup content
        | [] -> if currentGroup <> targetGroup then
                   match targetGroup with
                   | GroupId.Default -> ()
                   | GroupId.Named name -> yield sprintf "group %s" name

                
                yield! content
    }

    

let AppendDependencies (packages : Package set) =
    let group2pkgs = packages
                        |> Seq.groupBy (fun x -> x.Group)
                        |> Map
   
    let mutable currentGroup = Anthology.GroupId.Default
    let mutable currentLines = List.empty
    let mutable group2Lines = List.empty

    let confDir = Env.GetFolder Env.Folder.Config
    let paketDep = confDir |> GetFile "paket.dependencies"
    let lines = File.ReadAllLines(paketDep.FullName)

    for line in lines do
        let items = line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
        match items.[0] with
        | "group" -> match group2pkgs |> Map.tryFind currentGroup with
                     | Some groupLines -> currentLines <- currentLines @ (groupLines |> Seq.map (fun x -> x.Id.toString) |> List.ofSeq)
                     | None -> ()
                     group2Lines <- group2Lines @ [(currentGroup, currentLines)]
                     currentGroup <- GroupId.Named items.[1]
                     currentLines <- List.empty
        | _ -> currentLines <- currentLines @ [line]

    if packages <> Set.empty then
        let newContent = group2Lines |> Seq.map snd
                                     |> Seq.collect id
        File.WriteAllLines (paketDep.FullName, newContent)

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
