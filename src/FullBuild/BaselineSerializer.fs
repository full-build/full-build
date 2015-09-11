module BaselineSerializer

open Anthology
open System.IO
open System
open Collections
open System.Text

// file format:
// name1 version
// name2 version
// ...

let SerializeBaseline (baseline : Baseline) =
    seq {
        for bookmark in baseline.Bookmarks do
            let repoId = bookmark.Repository.Value

            match bookmark.Version with
            | BookmarkVersion version -> yield sprintf "commit %s %s" repoId version
            | Master -> ()
    }

let (|MatchRepository|) (line : string) =
    let (repo, version) = Sscanf.sscanf "commit %s %s" line
    { Repository = RepositoryId.Bind repo; Version = BookmarkVersion version }

let DeserializeBaseline (content : string list) : Baseline =
    let rec deserializeRepository (repoContent : string list) =
        match repoContent with
        | (MatchRepository repo) :: tail -> deserializeRepository tail |> Set.add repo
        | [] -> Set.empty

    { Bookmarks = deserializeRepository content }


    
let Save (filename : FileInfo) (baseline : Baseline) =
    let content = SerializeBaseline baseline
    File.WriteAllLines(filename.FullName, content)

let Load (filename : FileInfo) : Baseline =
    let content = File.ReadAllLines (filename.FullName) |> Seq.toList
    DeserializeBaseline content

