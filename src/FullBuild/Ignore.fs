module Ignore
open System.IO
open Collections

let IsFileIncluded (excludes : string set) (rootDir : DirectoryInfo) (file : FileInfo) =
    let relativeFile = file.FullName.Replace(rootDir.FullName, "") |> FsHelpers.ToUnix |> Set.singleton
    let res = PatternMatching.FilterMatch relativeFile id excludes
    res = Set.empty


let LoadFbIgnore (repoDir : DirectoryInfo) : string set =
    let excludeFile = repoDir |> FsHelpers.GetFile ".fbignore"
    let excludes = if excludeFile.Exists then 
                       System.IO.File.ReadAllLines(excludeFile.FullName)
                       |> Seq.map (fun x -> let idx = x.IndexOf('#')
                                            let s = if idx <> -1  then x.Substring(0, idx)
                                                                  else x
                                            s.Trim())
                       |> Seq.filter (fun x -> System.String.IsNullOrWhiteSpace(x) |> not)
                       |> Seq.map (fun x -> "*" + x)
                       |> Set
                   else Set.empty
    excludes
