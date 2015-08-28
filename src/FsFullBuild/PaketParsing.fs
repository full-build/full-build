module PaketParsing
open System
open Anthology
open System.IO
open IoHelpers




let ParseContent (lines : string seq) =
    seq {
        for line in lines do
            let items = line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
            match items.[0] with
            | "nuget" -> yield PackageRef.Bind items.[1]
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

let UpdateSources (sources : string list) =
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
            yield sprintf "nuget %s ~> %s" (package.Id.Print()) package.Version
    }

let AppendDependencies (packages : Package seq) = 
    let confDir = Env.WorkspaceConfigFolder ()
    let paketDep = confDir |> GetFile "paket.dependencies" 


    let content = GenerateDependenciesContent packages
    File.AppendAllLines (paketDep.FullName, content)

let RemoveDependenciesContent (lines : string seq) (packages : Set<PackageRef>) =
    seq {
        for line in lines do
            let items = line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
            match items.[0] with
            | "nuget" -> if Set.contains (PackageRef.Bind items.[1]) packages then ()
                         else yield line
            | _ -> yield line
    }

let RemoveDependencies (packages : Set<PackageRef>) =
    let confDir = Env.WorkspaceConfigFolder ()
    let paketDep = confDir |> GetFile "paket.dependencies" 
    let content = File.ReadAllLines (paketDep.FullName)
    let newContent = RemoveDependenciesContent content packages
    File.WriteAllLines (paketDep.FullName, newContent)
