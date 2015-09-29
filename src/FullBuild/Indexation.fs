module Indexation
open System.IO
open IoHelpers
open System.Linq
open System.Xml.Linq
open MsBuildHelpers
open Anthology

let private FindKnownProjects (repoDir : DirectoryInfo) =
    [AddExt "*" CsProj
     AddExt "*" VbProj
     AddExt "*" FsProj] |> Seq.map (fun x -> repoDir.EnumerateFiles (x, SearchOption.AllDirectories)) 
                        |> Seq.concat

let private ProjectCanBeProcessed (fileName : FileInfo) =
    let xdoc = XDocument.Load (fileName.FullName)
    let fbIgnore = !> xdoc.Descendants(NsMsBuild + "FullBuildIgnore").FirstOrDefault() : string
    match bool.TryParse(fbIgnore) with
    | (true, x) -> not <| x
    | _ -> true

let private ParseRepositoryProjects (parser) (repoRef : RepositoryId) (repoDir : DirectoryInfo) =
    repoDir |> FindKnownProjects 
            |> Seq.filter ProjectCanBeProcessed
            |> Seq.map (parser repoDir repoRef)

let ParseWorkspaceProjects (parser) (wsDir : DirectoryInfo) (repos : Repository seq) = 
    repos |> Seq.map (fun x -> GetSubDirectory x.Name.toString wsDir) 
          |> Seq.filter (fun x -> x.Exists) 
          |> Seq.map (fun x -> ParseRepositoryProjects parser (RepositoryId.from(x.Name)) x)
          |> Seq.concat
