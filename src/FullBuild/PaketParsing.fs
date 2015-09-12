// Copyright (c) 2014-2015, Pierre Chalamet
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Pierre Chalamet nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL PIERRE CHALAMET BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
module PaketParsing
open System
open Anthology
open System.IO
open IoHelpers
open Collections



let ParseContent (lines : string seq) =
    seq {
        for line in lines do
            let items = line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
            match items.[0] with
            | "nuget" -> yield (PackageId.from items.[1])
            | _ -> ()
    }

let UpdateSourceContent (lines : string seq) (sources : string seq) =
    seq {
        for source in sources do
            yield sprintf "source %s" source

        for line in lines do
            let items = line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
            match items.[0] with
            | "source" -> ()
            | _ -> yield line
    }

let UpdateSources (sources : string seq) =
    let confDir = Env.WorkspaceConfigFolder ()
    let paketDep = confDir |> GetFile "paket.dependencies" 
    let oldContent = if paketDep.Exists then File.ReadAllLines (paketDep.FullName) |> Array.toSeq
                     else Seq.empty
    let content = UpdateSourceContent oldContent sources
    File.WriteAllLines (paketDep.FullName, content)

let ParsePaketDependencies () =
    let confDir = Env.WorkspaceConfigFolder ()
    let paketDep = confDir |> GetFile "paket.dependencies" 
    if paketDep.Exists then
        let lines = File.ReadAllLines (paketDep.FullName)
        let packageRefs =  ParseContent lines
        packageRefs |> Set
    else
        Set.empty

let GenerateDependenciesContent (packages : Package seq) =
    seq {
        for package in packages do
            match package.Version with
            | PackageVersion x -> yield sprintf "nuget %s ~> %s" (package.Id.toString) x
            | Unspecified -> yield sprintf "nuget %s" (package.Id.toString)
    }

let AppendDependencies (packages : Package seq) = 
    let confDir = Env.WorkspaceConfigFolder ()
    let paketDep = confDir |> GetFile "paket.dependencies" 


    let content = GenerateDependenciesContent packages
    File.AppendAllLines (paketDep.FullName, content)

let RemoveDependenciesContent (lines : string seq) (packages : PackageId set) =
    seq {
        for line in lines do
            let items = line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
            match items.[0] with
            | "nuget" -> if Set.contains (PackageId.from items.[1]) packages then ()
                         else yield line
            | _ -> yield line
    }

let RemoveDependencies (packages : PackageId set) =
    let confDir = Env.WorkspaceConfigFolder ()
    let paketDep = confDir |> GetFile "paket.dependencies" 
    let content = File.ReadAllLines (paketDep.FullName)
    let newContent = RemoveDependenciesContent content packages
    File.WriteAllLines (paketDep.FullName, newContent)
