module Indexation
open System.IO
open IoHelpers
open System.Linq
open System.Xml.Linq
open MsBuildHelpers
open Anthology

let FindKnownProjects (repoDir : DirectoryInfo) =
    [AddExt CsProj "*"
     AddExt VbProj "*"
     AddExt FsProj "*"] |> Seq.map (fun x -> repoDir.EnumerateFiles (x, SearchOption.AllDirectories)) 
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





type ConflictType =
    | SameGuid of Project*Project
    | SameOutput of Project*Project


let FindConflictsForProject (project1 : Project) (otherProjects : Project list) =
    seq {
        for project2 in otherProjects do
            if project1 <> project2 then
                if project1.ProjectGuid = project2.ProjectGuid && (project1.Repository <> project2.Repository || project1.RelativeProjectFile <> project2.RelativeProjectFile) then
                    yield SameGuid (project1, project2)
                else if project1.ProjectGuid <> project2.ProjectGuid && project1.Output = project2.Output then
                    yield SameOutput (project1, project2)
    }        

let rec FindConflicts (projects : Project list) =
    seq {
        match projects with
        | h :: t -> yield! FindConflictsForProject h t
                    yield! FindConflicts t
        | _ -> ()
    }
