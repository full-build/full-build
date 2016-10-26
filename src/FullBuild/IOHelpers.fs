//   Copyright 2014-2016 Pierre Chalamet
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

module IoHelpers
open System
open System.IO
open System.Xml.Linq


// http://ss64.com/nt/robocopy-exit.html
let private checkErrorCode (execResult:Exec.ExecResult) =
    if execResult.ResultCode > 7 then failwithf "Process failed with error %d" execResult.ResultCode

type Extension =
    | View
    | Solution
    | Targets
    | CsProj
    | FsProj
    | VbProj
    | NuSpec
    | Dgml
    | App
    | Exe
    | Dll
    | Zip
    | Config
    | Text
    | Html

let AddExt (ext : Extension) (fileName : string) : string =
    let sext = match ext with
               | View -> "view"
               | Solution -> "sln"
               | Targets -> "targets"
               | CsProj -> "csproj"
               | FsProj -> "fsproj"
               | VbProj -> "vbproj"
               | NuSpec -> "nuspec"
               | Dgml -> "dgml"
               | App -> "app"
               | Exe -> "exe"
               | Dll -> "dll"
               | Zip -> "zip"
               | Config -> "config"
               | Text -> "txt"
               | Html -> "html"
    sprintf "%s.%s" fileName sext

let ToUnix (f : string) : string =
    if f = null then f
    else f.Replace(@"\", "/")

let GetSubDirectory (subDir : string) (dir : DirectoryInfo) : DirectoryInfo =
    let newPath = Path.Combine(dir.FullName, subDir)
    DirectoryInfo (newPath)

let CreateSubDirectory (dirName : string) (parentDir : DirectoryInfo) : DirectoryInfo =
    let dir = parentDir |> GetSubDirectory dirName
    dir.Create ()
    dir

let GetFile (fileName : string) (dir : DirectoryInfo) : FileInfo =
    let fullFileName = Path.Combine(dir.FullName, fileName)
    FileInfo (fullFileName)

let rec private computeRelativePathInc (topDir : DirectoryInfo) (childDir : DirectoryInfo) (path : string) =
    if topDir.FullName = childDir.FullName then path |> ToUnix
    else
        let newPath = Path.Combine(childDir.Name, path)
        computeRelativePathInc topDir childDir.Parent newPath

let ComputeRelativeFilePath (dir : DirectoryInfo) (file : FileInfo) : string =
    let path = file.Name
    computeRelativePathInc dir file.Directory path

let ComputeRelativeDirPath (dir : DirectoryInfo) (target : DirectoryInfo) : string =
    let path = ""
    computeRelativePathInc dir target path

let rec genHops (count : int) (path : string) =
    match count with
    | 1 -> path
    | x -> genHops (count-1) ("../" + path)

let ComputeHops (file : string) : string =
    let count = file.Split('/').Length
    genHops count ""

let CurrentFolder() : DirectoryInfo =
    Directory.GetCurrentDirectory () |> DirectoryInfo




let CopyFolder (source : DirectoryInfo) (target : DirectoryInfo) (readOnly : bool) =
    let currDir = CurrentFolder()
    let setRead = if readOnly then "/A+:R"
                  else "/A-:R"

    let args = sprintf "%s /MIR /MT /NP /NFL /NDL /NJH /NJS %A %A" setRead source.FullName target.FullName
    Exec.Exec "robocopy.exe" args currDir Map.empty |> checkErrorCode

let GetExtension (file : FileInfo) =
    file.Extension.Replace(".", "")

let GetRootDirectory (file : string) =
    let idx = file.IndexOf('/')
    file.Substring(0, idx)

let GetFilewithoutRootDirectory (file : string) =
    let idx = file.IndexOf('/')
    file.Substring(idx+1)

let consoleLock = System.Object()
let DisplayHighlight s =
    let display () =
        let oldColor = Console.ForegroundColor
        Console.ForegroundColor <- ConsoleColor.Cyan
        Console.WriteLine("==> {0}", [|s|])
        Console.ForegroundColor <- oldColor

    lock consoleLock display

let Try action =
    try
        action()
    with
        _ -> ()


let FindKnownProjects (repoDir : DirectoryInfo) =
    [AddExt CsProj "*"
     AddExt VbProj "*"
     AddExt FsProj "*"] |> Seq.map (fun x -> repoDir.EnumerateFiles (x, SearchOption.AllDirectories))
                        |> Seq.concat

let EnumarateFiles (dir : DirectoryInfo) =
    dir.EnumerateFiles()

let SaveFileIfNecessary (file : FileInfo) (content : string) =
    let overwrite = (file.Exists |> not) || File.ReadAllText(file.FullName) <> content
    if overwrite then
        File.WriteAllText (file.FullName, content)

let XDocLoader (fileName : FileInfo) : XDocument option =
    if fileName.Exists then Some (XDocument.Load (fileName.FullName))
    else None

let XDocSaver (fileName : FileInfo) (xdoc : XDocument) =
    xdoc.ToString()
        |> SaveFileIfNecessary fileName

let ForceDelete (dir : DirectoryInfo) =
    if dir.Exists then dir.Delete(true)

let rec EnsureForceDelete (dir : DirectoryInfo) (limit : int) =
    try
        if dir.Exists then dir.Delete(true)
    with
        exn -> if 0 < limit then
                  printfn "Failure to delete folder %A on attempt %d" dir.FullName limit
                  System.Threading.Thread.Sleep(5 * 1000)
                  GC.Collect ()
                  EnsureForceDelete dir (limit - 1)
               else
                  reraise()
