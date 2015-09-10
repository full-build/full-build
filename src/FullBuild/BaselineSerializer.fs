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
            | BookmarkVersion version -> yield sprintf "%s %s" repoId version
            | Master -> ()
    }


let Save (filename : FileInfo) (baseline : Baseline) =
    let content = SerializeBaseline baseline
    File.WriteAllLines(filename.FullName, content)



let (|MatchRepository|) (line : string) =
    let items = line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
    { Repository = RepositoryId.Bind items.[0]; Version = BookmarkVersion items.[1] }

let DeserializeBaseline (content : string list) : Baseline =
    let rec deserializeRepository (repoContent : string list) =
        match repoContent with
        | (MatchRepository repo) :: tail -> deserializeRepository tail |> Set.add repo
        | [] -> Set.empty

    { Bookmarks = deserializeRepository content }
    
let Load (filename : FileInfo) : Baseline =
    let content = File.ReadAllLines (filename.FullName) |> Seq.toList
    DeserializeBaseline content

