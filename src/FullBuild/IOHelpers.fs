//   Copyright 2014-2017 Pierre Chalamet
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


type Extension =
    | View
    | Solution
    | Targets
    | NuSpec
    | Dgml
    | App
    | Exe
    | Dll
    | Zip
    | Config
    | Text
    | Html

let GetExtensionString ext =
    match ext with
    | View -> "fbsln"
    | Solution -> "sln"
    | Targets -> "targets"
    | NuSpec -> "nuspec"
    | Dgml -> "dgml"
    | App -> "app"
    | Exe -> "exe"
    | Dll -> "dll"
    | Zip -> "zip"
    | Config -> "config"
    | Text -> "txt"
    | Html -> "html"

let AddExt (ext : Extension) (fileName : string) : string =
    ext |> GetExtensionString |> sprintf "%s.%s" fileName

let ToPlatformPath (f : string) : string =
    let sep = sprintf "%c" System.IO.Path.DirectorySeparatorChar
    if f |> isNull then f
    else f.Replace(@"\", sep) 

let ToWindows (f : string) : string =
    if f |> isNull then f
    else f.Replace(@"/", @"\")

let ToUnix (f : string) : string =
    if f |> isNull then f
    else f.Replace(@"\", @"/")

let MigratePath (path : string) =
    path.Replace("$(FBWorkspaceDir)", "$(SolutionDir)")

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
    if topDir.FullName = childDir.FullName then path |> ToWindows
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
    // http://ss64.com/nt/robocopy-exit.html
    let checkRobocopyErrorCode (execResult:Exec.ExecResult) =
        if execResult.ResultCode > 7 then failwithf "Process failed with error %d" execResult.ResultCode

    let currDir = CurrentFolder()
    let setRead = if readOnly then "/A+:R"
                  else "/A-:R"

    let args = sprintf "%A %A %s /MIR /NFL /NDL /NJH /NJS /nc /ns /np" source.FullName target.FullName setRead
    Exec.Exec "robocopy.exe" args currDir Map.empty |> checkRobocopyErrorCode

let GetExtension (file : FileInfo) =
    file.Extension.Replace(".", "")

let GetRootDirectory (file : string) =
    let idx = (file |> ToWindows).IndexOf('\\')
    file.Substring(0, idx)

let GetFilewithoutRootDirectory (file : string) =
    let idx = (file |> ToWindows).IndexOf('\\')
    file.Substring(idx+1)

let consoleLock = System.Object()

let ConsoleDisplay (c : ConsoleColor) (s : string) =
    let display () =        
        let oldColor = Console.ForegroundColor
        try
            Console.ForegroundColor <- c
            Console.WriteLine("- {0}", s)
        finally
            Console.ForegroundColor <- oldColor

    lock consoleLock display



let DisplayHighlight = ConsoleDisplay ConsoleColor.Cyan
let DisplayError = ConsoleDisplay ConsoleColor.Red


let Try action =
    try
        action()
    with
        _ -> ()


let FindKnownProjects (repoDir : DirectoryInfo) =
    [ "*.pssproj"
      "*.csproj"
      "*.vbproj"
      "*.fsproj" ] 
        |> Seq.collect (fun x -> repoDir.EnumerateFiles (x, SearchOption.AllDirectories))

let EnumerateChildren (dir : DirectoryInfo) =
    dir.EnumerateFileSystemInfos()

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
