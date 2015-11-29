//   Copyright 2014-2015 Pierre Chalamet
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


// http://ss64.com/nt/robocopy-exit.html
let private checkErrorCode err =
    if err > 7 then failwithf "Process failed with error %d" err

let private checkedExec = 
    Exec.Exec checkErrorCode


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

let rec private ComputeRelativePathInc (topDir : DirectoryInfo) (childDir : DirectoryInfo) (path : string) = 
    if topDir.FullName = childDir.FullName then path |> ToUnix
    else 
        let newPath = Path.Combine(childDir.Name, path)
        ComputeRelativePathInc topDir childDir.Parent newPath

let ComputeRelativePath (dir : DirectoryInfo) (file : FileInfo) : string = 
    let path = file.Name
    ComputeRelativePathInc dir file.Directory path


let CurrentFolder() : DirectoryInfo = 
    DirectoryInfo(System.Environment.CurrentDirectory)




let CopyFolder (source : DirectoryInfo) (target : DirectoryInfo) (readOnly : bool) =
    let currDir = CurrentFolder()
    let setRead = if readOnly then "/A+:R"
                  else "/A-:R"

    let args = sprintf "%s /MIR /MT /NP /NFL /NDL /NJH %A %A" setRead source.FullName target.FullName
    checkedExec "robocopy.exe" args currDir

let GetExtension (file : FileInfo) =
    file.Extension.Replace(".", "")

let GetRootDirectory (file : string) =
    let idx = file.IndexOf('/')
    file.Substring(0, idx)

let GetFilewithoutRootDirectory (file : string) =
    let idx = file.IndexOf('/')
    file.Substring(idx+1)
   
let DisplayHighlight s =
    let oldColor = Console.ForegroundColor
    Console.ForegroundColor <- ConsoleColor.Cyan
    printfn "==> %s" s
    Console.ForegroundColor <- oldColor
