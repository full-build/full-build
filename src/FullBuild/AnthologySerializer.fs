module AnthologySerializer

open Anthology
open System.IO
open System
open Collections
open System.Text

//
// file format:
//
// version 1
// repo <id>
//   type <type>
//   url url
// ...
// project <guid>
//   repo <id>
//   type dll/exe
//   out <assemblyName>
//   fx <id>
//   file <file>
//   assembly <id>
//   ...
//   package <id1>
//   ...
//   project <id1>
//   ...
// ...



let SerializeAnthology (antho : Anthology) =
    seq {
        yield "version 1"

        for repo in antho.Repositories do
            yield sprintf "repo %s" repo.Name.toString
            yield sprintf "  type %s" repo.Vcs.toString
            yield sprintf "  url %s" repo.Url.toString
        
        for project in antho.Projects do
            yield sprintf "project %s" project.ProjectGuid.toString
            yield sprintf "  repo %s" project.Repository.toString
            yield sprintf "  type %s" project.ProjectType.toString
            yield sprintf "  pe %s" project.OutputType.toString
            yield sprintf "  out %s" project.Output.toString
            yield sprintf "  fx %s" project.FxTarget.toString
            yield sprintf "  file %s" project.RelativeProjectFile.toString
            for ass in project.AssemblyReferences do
                yield sprintf "  assembly %s" ass.toString
            for pkg in project.PackageReferences do
                yield sprintf "  package %s" pkg.toString
            for prj in project.ProjectReferences do
                yield sprintf "  project %s" (prj.toString)
    }


let Try<'T> (f : Unit -> 'T) : 'T option =
    try
        Some (f())
    with 
        _ -> None

let (|MatchFileVersion|_|) (line : string) =
    let f () = let (version) = Sscanf.sscanf "version %d" line
               version
    Try f

let (|MatchRepository|_|) (line : string) =
    let f () = let (repo) = Sscanf.sscanf "repo %s" line
               RepositoryId.from repo
    Try f

let (|MatchRepositoryType|_|) (line : string) =
    let f () = let (vcsType) = Sscanf.sscanf "  type %s" line
               VcsType.from vcsType
    Try f

let (|MatchRepositoryUrl|_|) (line : string) =
    let f () = let (repoUrl) = Sscanf.sscanf "  url %s" line
               RepositoryUrl repoUrl
    Try f

let (|MatchProject|_|) (line : string) =
    let f () = let (sguid) = Sscanf.sscanf "project %s" line
               ProjectId.from (StringHelpers.ParseGuid sguid)
    Try f

let (|MatchProjectRepo|_|) (line : string) =
    let f () = let (repo) = Sscanf.sscanf "  repo %s" line
               RepositoryId.from repo
    Try f

let (|MatchProjectType|_|) (line : string) =
    let f () = let (sguid) = Sscanf.sscanf "  type %s" line
               ProjectType.from (StringHelpers.ParseGuid sguid)
    Try f

let (|MatchProjectOutType|_|) (line : string) =
    let f () = let (outType) = Sscanf.sscanf "  pe %s" line
               OutputType.from outType
    Try f

let (|MatchProjectOut|_|) (line : string) =
    let f () = let (projectOut) = Sscanf.sscanf "  out %s" line
               AssemblyId.from projectOut
    Try f

let (|MatchProjectFx|_|) (line : string) =
    let f () = let (fx) = Sscanf.sscanf "  fx %s" line
               FrameworkVersion fx
    Try f

let (|MatchProjectFile|_|) (line : string) =
    let f () = let (file) = Sscanf.sscanf "  file %s" line
               ProjectRelativeFile file
    Try f

let (|MatchProjectAssembly|_|) (line : string) =
    let f () = let (name) = Sscanf.sscanf "  assembly %s" line
               AssemblyId.from name
    Try f

let (|MatchProjectPackage|_|) (line : string) =
    let f () = let (name) = Sscanf.sscanf "  package %s" line
               PackageId.from name
    Try f

let (|MatchProjectRef|_|) (line : string) =
    let f () = let (sguid) = Sscanf.sscanf "  project %s" line
               ProjectId.from (StringHelpers.ParseGuid sguid)
    Try f


let rec deserializeAssemblies (lines : string list) =
    match lines with
    | (MatchProjectAssembly ass) :: tail -> let res = deserializeAssemblies tail 
                                            (res |> fst |> Set.add ass, res |> snd)
    | x -> (Set.empty, x)

let rec deserializePackages (lines : string list) =
    match lines with
    | (MatchProjectPackage pkg) :: tail -> let res = deserializePackages tail 
                                           (res |> fst |> Set.add pkg, res |> snd)
    | x -> (Set.empty, x)

let rec deserializeProjectRefs (lines : string list) =
    match lines with
    | (MatchProjectRef prj) :: tail -> let res = deserializeProjectRefs tail 
                                       (res |> fst |> Set.add prj, res |> snd)
    | x -> (Set.empty, x)

let rec deserializeRepositories (lines : string list) =
    match lines with
    | (MatchRepository name) 
        :: (MatchRepositoryType repoType) 
        :: (MatchRepositoryUrl url) 
        :: tail -> let res = deserializeRepositories tail
                   let repo = { Name=name; Vcs=repoType; Url=url }
                   (res |> fst |> Set.add repo, res |> snd)
    | x -> (Set.empty, x)

let rec deserializeProjects (lines : string list) =
    match lines with
    | (MatchProject prj)
        :: (MatchProjectRepo repo)
        :: (MatchProjectType prjType)
        :: (MatchProjectOutType outType)
        :: (MatchProjectOut prjOut)
        :: (MatchProjectFx fx)
        :: (MatchProjectFile file) :: tail -> let assemblies = deserializeAssemblies tail
                                              let packages = deserializePackages (assemblies |> snd)
                                              let projectRefs = deserializeProjectRefs (packages |> snd)
                                              let project =  { Repository=repo
                                                               RelativeProjectFile=file
                                                               ProjectGuid=prj
                                                               ProjectType=prjType
                                                               Output=prjOut
                                                               OutputType=outType
                                                               FxTarget=fx
                                                               AssemblyReferences=assemblies |> fst
                                                               PackageReferences=packages |> fst
                                                               ProjectReferences=projectRefs |> fst }
                                              let res = deserializeProjects (projectRefs |> snd)
                                              (res |> fst |> Set.add project, res |> snd)
    | x -> (Set.empty, x)
    

let rec DeserializeAnthologyV1 (lines : string list) : Anthology =
    let repos = deserializeRepositories lines
    let projects = deserializeProjects (repos |> snd)
    if List.empty <> (projects |> snd) then failwithf "Failed to parse %A" (projects |> snd)
    { Applications = Set.empty
      Repositories = repos |> fst
      Projects = projects |> fst }

let DeserializeAnthology (lines : string list) : Anthology =
    match lines with
    | (MatchFileVersion version) :: tail -> match version with
                                            | 1 -> DeserializeAnthologyV1 tail
                                            | x -> failwithf "Unknown file version %d" x
    | _ -> failwith "Unknown file format"

    
let Save (filename : FileInfo) (antho : Anthology) =
    let content = SerializeAnthology antho
    File.WriteAllLines(filename.FullName, content)

let Load (filename : FileInfo) : Anthology =
    let content = File.ReadAllLines (filename.FullName) |> Seq.toList
    DeserializeAnthology content
