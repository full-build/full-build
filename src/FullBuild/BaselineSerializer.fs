module BaselineSerializer

open Anthology
open System.IO
open System
open Collections
open System.Text

// file format:
//
// version 1
// repo name1
//   version version
// repo name2
//   version version
// ...

let SerializeBaseline (baseline : Baseline) =
    seq {
        yield "version 1"
        for bookmark in baseline.Bookmarks do
            let repoId = bookmark.Repository.toString

            match bookmark.Version with
            | BookmarkVersion version -> yield sprintf "repo %s" repoId
                                         yield sprintf "  version %s" version
            | Master -> ()
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

let (|MatchVersion|_|) (line : string) =
    let f () = let (repo) = Sscanf.sscanf "  version %s" line
               BookmarkVersion repo
    Try f

let rec deserializeRepositories (lines : string list) =
    match lines with
    | (MatchRepository name) 
      :: (MatchVersion version) :: tail -> let res = deserializeRepositories tail
                                           let repo = { Repository = name; Version = version }
                                           (res |> fst |> Set.add repo, res |> snd)
    | x -> (Set.empty, x)

let rec DeserializeBaselineV1 (lines : string list) : Baseline =
    let repos = deserializeRepositories lines
    if List.empty <> (repos |> snd) then failwithf "Failed to parse %A" (repos |> snd)
    { Bookmarks = repos |> fst }

let DeserializeBaseline (content : string list) : Baseline =
    match content with
    | (MatchFileVersion version) :: tail -> match version with
                                            | 1 -> DeserializeBaselineV1 tail
                                            | x -> failwithf "Unknown file version %d" x
    | _ -> failwith "Unknown file format"
    
let Save (filename : FileInfo) (baseline : Baseline) =
    let content = SerializeBaseline baseline
    File.WriteAllLines(filename.FullName, content)

let Load (filename : FileInfo) : Baseline =
    let content = File.ReadAllLines (filename.FullName) |> Seq.toList
    DeserializeBaseline content

